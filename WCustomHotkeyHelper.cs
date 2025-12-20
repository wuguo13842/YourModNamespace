using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using WCustomHotkey;  // 直接引用WCustomHotkey.dll

namespace YourModNamespace //修改与你的 ModBehaviour.cs空间名相同
{
    public static class WCustomHotkeyHelper
    {
		// ============ 配置部分 ============
		private static readonly string MOD_NAME = typeof(ModBehaviour).Namespace;
		private static string localConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"{MOD_NAME}_WHotkeyConfig.txt");
		
        // ============ 事件 ============
        public static event Action<string, string> OnHotkeyUpdate;
		
		#region + 按键初始化维护
			// ============ 内部存储 ============
			private static Dictionary<string, HotkeyConfig> hotkeyConfigs = new Dictionary<string, HotkeyConfig>();
		
			// ============ 配置类 ============
			public class HotkeyConfig
			{
				public string Id;           // 内部标识符
				public string DefaultValue; // 默认键值
				public string DisplayName;  // 显示名称
			}
			
			public static void RegisterAndNotifyHotkeys()
			{
				// 语言检测逻辑
				SystemLanguage[] chineseLanguages = {
					SystemLanguage.Chinese,
					SystemLanguage.ChineseSimplified,
					SystemLanguage.ChineseTraditional
				};
				bool isChinese = Array.Exists(chineseLanguages, lang => lang == Application.systemLanguage);
				
				var configs = new[]
				{
					new HotkeyConfig { 
						Id = "AttackKey", 
						DefaultValue = "<Keyboard>/r", 
						DisplayName = isChinese ? "攻击" : "Attack" 
					},
					new HotkeyConfig { 
						Id = "JumpKey", 
						DefaultValue = "Key.Space", 
						DisplayName = isChinese ? "跳跃" : "Jump" 
					},
					new HotkeyConfig { 
						Id = "UseKey", 
						DefaultValue = "KeyCode.E", 
						DisplayName = isChinese ? "使用" : "Use" 
					},
					new HotkeyConfig { 
						Id = "ReloadKey", 
						DefaultValue = "<Keyboard>/t", 
						DisplayName = isChinese ? "装弹" : "Reload" 
					}
				};
				
				foreach (var config in configs)
				{
					hotkeyConfigs[config.Id] = config;
					RegisterHotkey(config.Id, config.DefaultValue, config.DisplayName);
					GetHotkey(config.Id);
				}
			}
		#endregion - 按键初始化维护
		
		#region + 周期管理
			// ============ 内部状态 ============
			private static bool isWCustomHotkeyAvailable = false;
		
			// ============ 初始化方法 ============
			
			/// <summary>
			/// 初始化WCustomHotkey系统
			/// </summary>
			public static void Initialize()
			{
				// 检测WCustomHotkey是否可用
				try
				{
					isWCustomHotkeyAvailable = CheckWCustomHotkeyAPIImpl();
				}
				catch
				{
					isWCustomHotkeyAvailable = false;
				}
				
				// 订阅WCustomHotkey事件
				if (isWCustomHotkeyAvailable)
				{		
					try
					{
						RegisterEventsImpl();
					}
					catch
					{
						isWCustomHotkeyAvailable = false;
					}
				}
				
				// 初始化读取赋予键值
				RegisterAndNotifyHotkeys();
			}
			
			/// <summary>
			/// 清理资源
			/// </summary>
			public static void Cleanup()
			{
				// 取消WCustomHotkey事件订阅
				if (isWCustomHotkeyAvailable)
				{
					try
					{
						UnregisterEventsImpl();
					}
					catch
					{
						// 忽略取消订阅时的异常
					}
				}
				
				// 清理统一事件订阅
				OnHotkeyUpdate = null;

				// 重置状态
				isWCustomHotkeyAvailable = false;
			}
			
			/// <summary>
			/// 取消注册事件
			/// </summary>
			public static void UnregisterEvents()
			{
				UnregisterEventsImpl();
			}
		#endregion - 周期管理
		
		#region + WCustomHotkey配置部分
			#region + 将WCustomHotkey类型引用隔离 *Impl() 方法中,防止异常终止扩散.
				private static bool CheckWCustomHotkeyAPIImpl()
				{
					// 尝试调用API来检查是否可用
					return  WCustomHotkeyAPI.IsModAvailable();
				}
				
				#region + ============ 三个核心API的展示 ============
					private static void RegisterEventsImpl()
					{
						WCustomHotkeyEvents.OnHotkeyChanged += HandleWCustomHotkeyEvent;
					}
					private static void UnregisterEventsImpl()
					{
						WCustomHotkeyEvents.OnHotkeyChanged -= HandleWCustomHotkeyEvent;
					}
					
					private static void RegisterHotkeyImpl(string saveName, string defaultHotkey, string displayName)
					{
						WCustomHotkeyAPI.RegisterHotkey(MOD_NAME, saveName, defaultHotkey, displayName);
					}	

					private static string GetHotkeyImpl(string saveName)
					{
						return WCustomHotkeyAPI.GetHotkey(MOD_NAME, saveName);
					}	
				#endregion - ============ 三个核心API的展示 ============
				
			#endregion - *Impl()隔离方法
			
		#endregion - WCustomHotkey配置部分
		
		#region + 配套与回退
			#region + 事件处理
				private static void HandleWCustomHotkeyEvent(object sender, HotkeyChangedEventArgs args)
				{
					if (args.ModName == MOD_NAME)
					{
						SaveLocalConfig(args.ActionName, args.NewHotkey);
						// 触发统一事件，传递(键名id, 键值)
						OnHotkeyUpdate?.Invoke(args.ActionName, args.NewHotkey);
					}
				}
			#endregion - 事件处理
		
			#region + 本地储存读取
				
				/// <summary>
				/// 保存到本地配置文件
				/// </summary>
				private static void SaveLocalConfig(string saveName, string hotkeyString)
				{
					try
					{
						// 如果文件不存在，直接创建
						if (!File.Exists(localConfigPath))
						{
							File.WriteAllText(localConfigPath, $"{saveName}={hotkeyString ?? string.Empty}");
							return;
						}
						
						// 读取文件所有行
						string[] lines = File.ReadAllLines(localConfigPath);
						bool keyFound = false;
						bool needUpdate = false;
						
						// 检查键是否存在且值是否变化
						for (int i = 0; i < lines.Length; i++)
						{
							if (string.IsNullOrWhiteSpace(lines[i]) || !lines[i].Contains("="))
								continue;
							
							string[] parts = lines[i].Split('=', 2);
							if (parts.Length == 2 && parts[0].Trim() == saveName)
							{
								keyFound = true;
								if (parts[1].Trim() != hotkeyString)
								{
									lines[i] = $"{saveName}={hotkeyString ?? string.Empty}";
									needUpdate = true;
								}
								break;
							}
						}
						
						// 如果键不存在，添加新行
						if (!keyFound)
						{
							List<string> newLines = new List<string>(lines);
							newLines.Add($"{saveName}={hotkeyString ?? string.Empty}");
							File.WriteAllLines(localConfigPath, newLines);
						}
						// 如果值变化，更新文件
						else if (needUpdate)
						{
							File.WriteAllLines(localConfigPath, lines);
						}
					}
					catch
					{
						// 配置文件保存失败
					}
				}
			
				/// <summary>
				/// 加载特定热键的本地配置（回退时调用）
				/// </summary>
				private static string LoadLocalConfig(string saveName)
				{
					if (!File.Exists(localConfigPath)) return null;
					
					try
					{
						string[] lines = File.ReadAllLines(localConfigPath);
						foreach (string line in lines)
						{
							if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
								continue;
								
							string[] parts = line.Split('=');
							if (parts.Length == 2 && parts[0].Trim() == saveName)
							{
								string hotkeyString = parts[1].Trim();
								OnHotkeyUpdate?.Invoke(saveName, hotkeyString);
								return hotkeyString;
							}
						}
					}
					catch
					{
						// 配置文件读取失败
					}
					return null;
				}
			#endregion - 本地储存读取
			
			/// <summary>
			/// 注册热键
			/// </summary>
			/// <param name="saveName">热键ID</param>
			/// <param name="defaultHotkey">默认热键值（任意格式字符串）</param>
			/// <param name="displayName">显示名称</param>
			public static void RegisterHotkey(string saveName, string defaultHotkey, string displayName)
			{
				if (isWCustomHotkeyAvailable)
				{
					// 调用API注册热键（只是注册，不保存本地）
					RegisterHotkeyImpl(saveName, defaultHotkey, displayName);
					return;
				}
			}
			
			/// <summary>
			/// 获取热键并触发更新事件
			/// </summary>
			/// <param name="saveName">热键ID</param>
			/// <returns>热键值的原始字符串</returns>
			public static string GetHotkey(string saveName)
			{
				 string hotkeyString = string.Empty;
				
				// 1. 优先从WCustomHotkey API获取
				if (isWCustomHotkeyAvailable)
				{
					hotkeyString = GetHotkeyImpl(saveName);
					
					if (!string.IsNullOrEmpty(hotkeyString))
					{
						// API获取成功，保存到本地并触发事件
						SaveLocalConfig(saveName, hotkeyString);
						// 触发统一事件，传递(键名id, 键值)
						OnHotkeyUpdate?.Invoke(saveName, hotkeyString);
						return hotkeyString;
					}
				}
				return LoadLocalConfig(saveName);
			}
		#endregion - 配套与回退
    }
}
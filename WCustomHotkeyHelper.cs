using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

// 直接引用WCustomHotkey.dll
using WCustomHotkey;

namespace YourModNamespace //修改与你的 ModBehaviour.cs空间名相同
{
    public static class WCustomHotkeyHelper
    {
        // ============ 配置部分 ============
        private static readonly string MOD_NAME = typeof(ModBehaviour).Namespace;
        // private static string localConfigPath;
        private static string localConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"{MOD_NAME}_WHotkeyConfig.txt");
		        // ============ 配置类 ============
        public class HotkeyConfig
        {
            public string Id;           // 内部标识符
            public string DefaultValue; // 默认键值
            public string DisplayName;  // 显示名称
        }
		
        // ============ 内部存储 ============
        private static Dictionary<string, HotkeyConfig> hotkeyConfigs = new Dictionary<string, HotkeyConfig>();
		
        // ============ 内部状态 ============
        private static bool isWCustomHotkeyAvailable = false;
        
        // 回退热键存储：保存原始字符串
        private static Dictionary<string, string> fallbackHotkeys = new Dictionary<string, string>();
        
        // ============ 统一事件接口 ============
        /// <summary>
        /// 热键更新事件 - WCustomHotkeyHelper与ModBehaviour通讯的唯一接口
        /// 参数：(键名id, 键值)
        /// </summary>
        public static event Action<string, string> OnHotkeyUpdate;
		
		#region 周期管理
        // ============ 初始化方法 ============
        
        /// <summary>
        /// 初始化WCustomHotkey系统
        /// </summary>
        public static void Initialize()
        {
            // // 初始化本地配置文件路径
            // string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // localConfigPath = Path.Combine(modDirectory, $"{MOD_NAME}_WHotkeyConfig.txt");
            
            // 检测WCustomHotkey是否可用
            try
            {
				isWCustomHotkeyAvailable = WCustomHotkeyAPI.IsModAvailable();
            }
            catch
            {
                isWCustomHotkeyAvailable = false;
            }
            
            // 订阅WCustomHotkey事件
			RegisterEvents();
			
			// 初始化读取赋予键值
			RegisterAndNotifyHotkeys();
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Cleanup()
        {
            // 取消WCustomHotkey事件订阅
			UnregisterEvents();
            
            // 清理统一事件订阅
            OnHotkeyUpdate = null;
            
            // 清理内存
            fallbackHotkeys.Clear();

            // 重置状态
            isWCustomHotkeyAvailable = false;
        }
        
        /// <summary>
        /// 注册事件
        /// </summary>
        public static void RegisterEvents()
        {
			if (isWCustomHotkeyAvailable)
			{
				WCustomHotkeyEvents.OnHotkeyChanged += HandleWCustomHotkeyEvent;
			}
        }
        
        /// <summary>
        /// 取消注册事件
        /// </summary>
        public static void UnregisterEvents()
        {
			WCustomHotkeyEvents.OnHotkeyChanged -= HandleWCustomHotkeyEvent;
        }
		#endregion
		
		#region 按键初始化维护与更新
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
				GetHotkey(config.Id, config.DefaultValue);
			}
		}
		#endregion
		
		#region 本地储存读取
        // ============ 本地配置文件系统 ============
        /// <summary>
        /// 加载特定热键的本地配置（回退时调用）
        /// </summary>
        private static string LoadLocalConfig(string saveName, string DefaultKey = "")
        {
            if (!File.Exists(localConfigPath)) return DefaultKey;
            
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
                        fallbackHotkeys[saveName] = hotkeyString;
                        OnHotkeyUpdate?.Invoke(saveName, hotkeyString);
						return hotkeyString;
                    }
                }
            }
            catch
            {
                // 配置文件读取失败
            }
			return DefaultKey;
        }
        
        /// <summary>
        /// 保存到本地配置文件
        /// </summary>
        private static void SaveLocalConfig(string saveName, string hotkeyString)
        {
            try
            {
                // 保存到内存字典
                fallbackHotkeys[saveName] = hotkeyString;
                
                // 保存到文件
                List<string> lines = new List<string>();
                foreach (var kvp in fallbackHotkeys)
                {
                    lines.Add($"{kvp.Key}={kvp.Value}");
                }
                File.WriteAllLines(localConfigPath, lines);
            }
            catch
            {
                // 配置文件保存失败
            }
        }
		#endregion
        
        // ============ 三个核心API的展示 ============
		
		#region 事件处理
		private static void HandleWCustomHotkeyEvent(object sender, HotkeyChangedEventArgs args)
		{
			if (args.ModName == MOD_NAME)
			{
				SaveLocalConfig(args.ActionName, args.NewHotkey);
				// 触发统一事件，传递(键名id, 键值)
				OnHotkeyUpdate?.Invoke(args.ActionName, args.NewHotkey);
			}
		}
		#endregion
        
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
                try
                {
                    // 调用API注册热键（只是注册，不保存本地）
                    WCustomHotkeyAPI.RegisterHotkey(MOD_NAME, saveName, defaultHotkey, displayName);
                    return;
                }
                catch
                {
                    // API调用失败，标记为不可用
                    isWCustomHotkeyAvailable = false;
                }
            }
        }
        
        /// <summary>
        /// 获取热键并触发更新事件
        /// </summary>
        /// <param name="saveName">热键ID</param>
        /// <returns>热键值的原始字符串</returns>
        public static string GetHotkey(string saveName, string DefaultKey = "")
        {
             string hotkeyString = string.Empty;
            
            // 1. 优先从WCustomHotkey API获取
            if (isWCustomHotkeyAvailable)
            {
                try
                {
                    hotkeyString = WCustomHotkeyAPI.GetHotkey(MOD_NAME, saveName);
                    
                    if (!string.IsNullOrEmpty(hotkeyString))
                    {
                        // API获取成功，保存到本地并触发事件
                        SaveLocalConfig(saveName, hotkeyString);
						// 触发统一事件，传递(键名id, 键值)
						OnHotkeyUpdate?.Invoke(saveName, hotkeyString);
                        return hotkeyString;
                    }
                }
                catch
                {
                    // API调用失败，标记为不可用
                    isWCustomHotkeyAvailable = false;
					return LoadLocalConfig(saveName, DefaultKey);
                }
            }
			return LoadLocalConfig(saveName, DefaultKey);
        }
    }
}

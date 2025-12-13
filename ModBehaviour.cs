using UnityEngine;
using Duckov.Modding;

namespace YourModNamespace
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // 热键变量（可以设置默认值）
        private string AttackKey = "<Keyboard>/r";
        private string JumpKey = "Key.Space";
        private string UseKey = "KeyCode.E";
        private string ReloadKey = "<Keyboard>/t";
        
        void Awake()
        {
            // 1. 初始化WCustomHotkey系统
            // 注意：Initialize()内部已经调用了RegisterAndNotifyHotkeys()
            WCustomHotkeyHelper.Initialize();
            
            // 2. 订阅统一事件
            WCustomHotkeyHelper.OnHotkeyUpdate += OnHotkeyUpdate;
		}
		
        void OnEnable()
        {
			
        }
        
        void OnDisable()
        {
            // 取消事件订阅
            WCustomHotkeyHelper.OnHotkeyUpdate -= OnHotkeyUpdate;
            
            // 注意：这里不调用Cleanup()，因为Helper是静态的
            // 只有在mod完全卸载时才清理
        }
        
        void OnDestroy()
        {
            // 只在mod完全卸载时清理
            WCustomHotkeyHelper.Cleanup();
        }
        
        private void OnHotkeyUpdate(string keyId, string keyValue)
        {
            // switch表达式：智能、高效、简洁
            if (!string.IsNullOrEmpty(keyValue))
            {
                _ = keyId switch
                {
                    "AttackKey" => AttackKey = keyValue,
                    "JumpKey" => JumpKey = keyValue,
                    "UseKey" => UseKey = keyValue,
                    "ReloadKey" => ReloadKey = keyValue,
                    _ => null // 忽略未知keyId
                };
            }
        }
        
        // 场景切换时重新加载（可选，根据实际需要）
        private void OnSceneLoaded()
        {
            // 如果需要场景切换后重新加载热键，取消下面一行的注释
            // WCustomHotkeyHelper.ReloadAllHotkeys();
        }
        
        // 手动重新加载（如果需要）
        public void ManualReloadHotkeys()
        {
            // 这个方法需要你在WCustomHotkeyHelper中添加
            // 暂时注释掉，如果你添加了ReloadAllHotkeys方法再取消注释
            // WCustomHotkeyHelper.ReloadAllHotkeys();
        }
        
        // 示例：如何检查热键是否被按下
        private void Update()
        {
            // 注意：这里的代码只是示例，实际的热键检测需要根据你获取的键值字符串来解析
            // 你可能需要将字符串转换为KeyCode或InputSystem的按键标识
            
            // 示例：检测攻击键（需要根据实际键值字符串来解析）
            // if (IsKeyPressed(attack))
            // {
            //     // 执行攻击逻辑
            // }
        }
        
        // 辅助方法：将字符串转换为按键检测
        private bool IsKeyPressed(string keyString)
        {
            // 这里需要根据你的键值字符串格式来解析
            // 例如："<Keyboard>/r" 或 "KeyCode.E"
            // 这只是一个示例框架
            
            // 你可以根据不同的格式来解析：
            if (keyString.StartsWith("<Keyboard>/"))
            {
                // 使用新的InputSystem
                string keyName = keyString.Substring(11); // 移除"<Keyboard>/"
                // 使用InputSystem检测按键
            }
            else if (keyString.StartsWith("KeyCode."))
            {
                // 使用旧的Input系统
                string keyName = keyString.Substring(8); // 移除"KeyCode."
                KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), keyName);
                return Input.GetKeyDown(keyCode);
            }
            
            return false;
        }
        
        // 获取热键当前值的方法（可选）
        // public string GetAttackKey() => attack;
        // public string GetJumpKey() => jump;
        // public string GetUseKey() => use;
        // public string GetReloadKey() => reload;
    }
}
WCustomHotkeyHelper.cs 关键示例总结
核心功能
1.热键管理桥梁：连接Mod与WCustomHotkey.dll系统
2.双重存储：优先API存储，失败时使用本地文件备份
3.统一事件：OnHotkeyUpdate事件简化热键更新通知

关键机制
1.自动容错：API不可用时自动切换到本地配置
2.生命周期管理：Initialize()初始化，Cleanup()清理
3.配置持久化：热键保存在[ModName]_WHotkeyConfig.txt

三个核心API
1.RegisterHotkey()：注册热键到系统
2.GetHotkey()：获取热键值并触发更新事件
3.事件处理：监听WCustomHotkey系统变更并同步

设计特点
·静态类，无需实例化
·与ModBehaviour松耦合
·支持多种热键格式（InputSystem和KeyCode）


WCustomHotkey 系统使用文档
📋 概述
WCustomHotkey 是一个 Unity Mod 热键管理系统，提供完整的热键注册、绑定、存储和事件通知功能。

🎯 核心特性
✅ 支持多种热键格式（Key、KeyCode、Keyboard）
✅ 可视化 UI 绑定界面
✅ 自动配置文件保存
✅ 实时热键变化事件通知
✅ 与 Unity Input System 集成

🔧 API 参考
1. 基础 API
注册热键
csharp
// 注册热键（仅UI显示）
bool success = WCustomHotkeyAPI.RegisterHotkey(
    "MyMod",          // Mod名称
    "Attack",         // 动作名称（保存用）
    "Key.R",          // 默认热键
    "攻击"             // 显示名称
);

// 注册热键（带KeyCode格式）
WCustomHotkeyAPI.RegisterHotkey(
    "MyMod", 
    "Jump", 
    KeyCode.Space,    // 使用Unity KeyCode
    "跳跃"
);
注册并创建InputAction
csharp
// 注册热键并立即创建InputAction（推荐）
WCustomHotkeyAPI.RegisterHotkeyWithAction(
    "MyMod",
    "SpecialSkill",
    "Key.Q",
    "特殊技能",
    context =>         // 按键回调
    {
        Debug.Log("释放特殊技能！");
        UseSpecialSkill();
    }
);
创建已有热键的InputAction
csharp
// 为已注册的热键创建InputAction
WCustomHotkeyAPI.CreateInputActionOnly(
    "MyMod",
    "Attack",
    context => OnAttack()
);
2. 获取热键 API
public static string GetHotkey(
	string modName, 
	string saveName, 
	string TextString = "")
	
参数说明
参数	类型	必填	说明
modName	string	✅	Mod的名称（如："MyMod"）
saveName	string	✅	动作的保存名称（如："Attack"）
TextString	string	❌	可选，指定返回格式（默认为空）
返回值
根据 TextString 参数返回不同格式的热键字符串

📝 使用示例
示例1：获取原始热键格式
csharp
// 不传TextString参数，返回注册时的原始格式
string hotkey = WCustomHotkeyAPI.GetHotkey("MyMod", "Attack");
// 可能返回："Key.R"、"KeyCode.F"、"<Keyboard>/q"
Debug.Log($"攻击键：{hotkey}");
示例2：获取KeyCode格式
csharp
// 指定"keycode"格式，返回KeyCode枚举的字符串
string keyCodeStr = WCustomHotkeyAPI.GetHotkey("MyMod", "Jump", "keycode");
// 返回："R"、"F"、"Space" 等（KeyCode枚举名）
KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), keyCodeStr);
示例3：获取Key格式
csharp
// 指定"key"格式，统一返回Key.xxx格式
string keyFormat = WCustomHotkeyAPI.GetHotkey("MyMod", "UseItem", "key");
// 总是返回："Key.R"、"Key.Space"、"Key.F1"
示例4：获取Keyboard格式
csharp
// 指定"keyboard"格式，返回Unity Input System格式
string keyboardFormat = WCustomHotkeyAPI.GetHotkey("MyMod", "Skill", "keyboard");
// 返回："<Keyboard>/r"、"<Keyboard>/space"、"<Keyboard>/f1"
示例5：获取显示文本
csharp
// 指定"text"格式，返回用于显示的文本
string displayText = WCustomHotkeyAPI.GetHotkey("MyMod", "Defend", "text");
// 返回："R"、"空格"、"Ctrl"、"F1"、"↑" 等
// 可直接用于UI显示
🔄 格式对照表
TextString参数	返回值示例	用途
空字符串	"Key.R", "KeyCode.F", "<Keyboard>/q"	获取注册时的原始格式
"keycode"	"R", "F", "Space"	转换为KeyCode枚举字符串
"key"	"Key.R", "Key.Space", "Key.F1"	统一为Key.xxx格式
"keyboard"	"<Keyboard>/r", "<Keyboard>/space"	Unity Input System格式
"text"	"R", "空格", "Ctrl", "F1"	UI显示的友好文本
⚠️ 注意事项
1. 大小写不敏感
csharp
// 以下都是等价的：
GetHotkey("MyMod", "Attack", "keycode");
GetHotkey("MyMod", "Attack", "KeyCode");
GetHotkey("MyMod", "Attack", "KEYCODE");
2. 无效格式处理
csharp
// 如果传入未知格式，会返回原始热键并记录日志
string result = GetHotkey("MyMod", "Attack", "unknown");
// 日志：[WCustomHotkey] 未知格式类型: unknown，返回原始热键
3. 空值处理
csharp
// 如果热键不存在，返回空字符串
string result = GetHotkey("NotExistMod", "NotExistAction");
if (string.IsNullOrEmpty(result))
{
    // 使用默认值
    result = "Key.F";
}

3. 热键转换器 (HotkeyConverter)
格式检测
csharp
string format = HotkeyConverter.DetectFormat("Key.R");
// 返回值："Key", "KeyCode", "Keyboard"
格式转换
csharp
// 任意格式 -> Keyboard格式
string keyboard = HotkeyConverter.ToKeyboardFormat("Key.R"); // "<Keyboard>/r"

// Keyboard格式 -> 原始格式
string original = HotkeyConverter.ToOriginalFormat("<Keyboard>/r", "Key"); // "Key.R"

// 任意格式 -> KeyCode
KeyCode keyCode = HotkeyConverter.ConvertToKeyCode("Key.R"); // KeyCode.R

// 提取基础键名
string keyName = HotkeyConverter.ExtractBaseKeyName("<Keyboard>/leftCtrl"); // "leftCtrl"
实用方法
csharp
// 判断是否为修饰键
bool isModifier = HotkeyConverter.IsModifierKey("ctrl"); // true

// 判断是否为功能键
bool isFunction = HotkeyConverter.IsFunctionKey("f1"); // true
4. 事件系统
热键变化事件（带参数）
csharp
// 订阅事件
WCustomHotkeyEvents.OnHotkeyChanged += OnHotkeyChanged;

// 事件处理方法
private void OnHotkeyChanged(object sender, HotkeyChangedEventArgs e)
{
    // e.ModName: Mod名称
    // e.ActionName: 动作名称  
    // e.NewHotkey: 新热键值
    
    if (e.ModName == "MyMod" && e.ActionName == "Attack")
    {
        Debug.Log($"攻击键已改为: {e.NewHotkey}");
        UpdateAttackLogic(e.NewHotkey);
    }
}

// 取消订阅
WCustomHotkeyEvents.OnHotkeyChanged -= OnHotkeyChanged;
简单热键变化事件
csharp
// 当任何热键变化时触发（无参数）
WCustomHotkeyEvents.OnHotkeyChangedSimple += () =>
{
    Debug.Log("有热键被修改了！");
    RefreshAllUI();
};
Mod内部事件
csharp
// 订阅ModBehaviour的内部事件
ModBehaviour.OnCustomHotkeyChanged += () =>
{
    Debug.Log("自定义热键变化");
};
5. 系统状态 API
检查系统就绪
csharp
bool isReady = WCustomHotkeyAPI.IsSystemReady;
bool isAvailable = WCustomHotkeyAPI.IsModAvailable();
设置日志
csharp
WCustomHotkeyAPI.SetLogger(message => Debug.Log($"[Hotkey] {message}"));
清理资源
csharp
// 清理所有InputAction资源
WCustomHotkeyAPI.Cleanup();
🎮 使用示例
示例1：完整技能系统
csharp
public class SkillSystem : MonoBehaviour
{
    void Start()
    {
        // 注册技能热键
        WCustomHotkeyAPI.RegisterHotkeyWithAction(
            "SkillMod",
            "Fireball",
            "Key.1",
            "火球术",
            context => CastFireball()
        );
        
        WCustomHotkeyAPI.RegisterHotkeyWithAction(
            "SkillMod", 
            "Heal",
            "Key.2",
            "治疗术", 
            context => CastHeal()
        );
        
        // 监听变化更新UI
        WCustomHotkeyEvents.OnHotkeyChanged += OnSkillKeyChanged;
    }
    
    private void OnSkillKeyChanged(object sender, HotkeyChangedEventArgs e)
    {
        if (e.ModName == "SkillMod")
        {
            UpdateSkillUI(e.ActionName, e.NewHotkey);
        }
    }
}
示例2：配置管理器
csharp
public class ConfigManager : MonoBehaviour
{
    void Start()
    {
        // 加载时获取配置
        string attackKey = WCustomHotkeyAPI.GetHotkey("MyMod", "Attack");
        KeyCode attackKeyCode = WCustomHotkeyAPI.GetHotkeyKeyCode("MyMod", "Attack");
        
        // 设置初始绑定
        SetupInputActions();
        
        // 监听配置变化
        WCustomHotkeyEvents.OnHotkeyChanged += OnConfigChanged;
    }
    
    private void OnConfigChanged(object sender, HotkeyChangedEventArgs e)
    {
        // 自动保存用户配置
        if (e.ModName == "MyMod")
        {
            SaveUserConfig();
        }
    }
}
示例3：实时UI更新
csharp
public class HotkeyDisplay : MonoBehaviour
{
    public TextMeshProUGUI attackKeyText;
    public TextMeshProUGUI jumpKeyText;
    
    void Start()
    {
        // 初始显示
        UpdateAllDisplays();
        
        // 监听变化
        WCustomHotkeyEvents.OnHotkeyChangedSimple += UpdateAllDisplays;
    }
    
    void UpdateAllDisplays()
    {
        attackKeyText.text = GetDisplayText("MyMod", "Attack");
        jumpKeyText.text = GetDisplayText("MyMod", "Jump");
    }
    
    string GetDisplayText(string mod, string action)
    {
        string hotkey = WCustomHotkeyAPI.GetHotkey(mod, action);
        return HotkeyConverter.ToDisplayText(hotkey);
    }
}
🔄 数据流图
text
用户操作 → UI绑定界面 → HotkeyManager → 配置文件
    ↓           ↓           ↓           ↓
InputSystem → 事件系统 → 其他Mod → 游戏逻辑
📝 格式对照表
格式类型	示例	用途
Key	Key.R, Key.Space	系统内部中间格式
KeyCode	KeyCode.R, KeyCode.Space	Unity传统输入系统
Keyboard	<Keyboard>/r, <Keyboard>/space	Unity Input System
显示文本	R, Space, Ctrl, F1, ↑	UI显示
⚠️ 注意事项
1. 内存管理
csharp
// ✅ 正确：配对订阅和取消
void OnEnable() => SubscribeEvents();
void OnDisable() => UnsubscribeEvents();

// ❌ 错误：忘记取消订阅导致内存泄漏
2. 线程安全
事件可能在任意线程触发
UI操作需要确保在主线程执行

3. 性能建议
避免在事件处理中做耗时操作
使用缓存减少重复转换
批量更新UI而不是单个更新

4. 错误处理
csharp
try
{
    string hotkey = WCustomHotkeyAPI.GetHotkey("MyMod", "Action");
    if (!string.IsNullOrEmpty(hotkey))
    {
        // 正常处理
    }
}
catch (Exception ex)
{
    Debug.LogError($"获取热键失败: {ex.Message}");
    // 使用默认值
}
🚀 快速开始
第一步：安装和初始化
csharp
void Awake()
{
    // 系统会自动初始化，无需手动调用
    // 只需确保ModBehaviour存在
}
第二步：注册热键
csharp
void Start()
{
    WCustomHotkeyAPI.RegisterHotkeyWithAction(
        "MyMod",
        "MainAction",
        "Key.F",
        "主要动作",
        context => DoMainAction()
    );
}
第三步：使用热键
csharp
void Update()
{
    // 方式1：使用InputAction（自动处理）
    // 方式2：实时获取
    KeyCode key = WCustomHotkeyAPI.GetHotkeyKeyCode("MyMod", "MainAction");
    if (Input.GetKeyDown(key))
    {
        DoMainAction();
    }
}
第四步：清理
csharp
void OnDestroy()
{
    WCustomHotkeyAPI.Cleanup();
    // 取消所有事件订阅
}
🎯 最佳实践
使用 RegisterHotkeyWithAction：一次性完成注册和绑定
监听 OnHotkeyChanged 事件：实时响应配置变化
使用 HotkeyConverter：统一格式转换
配对订阅和取消：防止内存泄漏
提供合理的默认值：增强健壮性

📈 版本兼容性
Unity 2020.3+ ✅
Unity Input System 1.3+ ✅
.NET Framework 4.7.2+ ✅
支持多Mod同时使用 ✅

文档版本：v1.0
最后更新：2025-12-11

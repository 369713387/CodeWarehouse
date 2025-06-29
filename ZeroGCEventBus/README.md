# ZeroGCEventBus 零GC事件总线系统

## 概述

ZeroGCEventBus 是一个专为Unity设计的高性能、零垃圾回收（Zero GC）、线程安全的事件总线系统。它提供了一个高效的事件通信机制，特别适用于需要高性能表现的游戏和应用程序。

### 主要特性

- **零GC分配**：避免运行时垃圾回收，提升性能
- **线程安全**：支持多线程环境下的事件处理
- **高性能**：采用预分配缓冲区和编译时类型检查
- **灵活配置**：支持多种缓冲区溢出策略
- **类型安全**：编译时类型检查，避免运行时错误
- **内存管理**：自动清理无效引用，防止内存泄漏

## 系统架构

### 核心组件

1. **ZeroGCEventBus**：主要的事件总线实现
2. **SafeZeroGCEventBusController**：安全控制器，提供额外的安全性和便利功能
3. **EventBuffer**：高性能事件缓冲区
4. **ListenerManager**：线程安全的监听器管理
5. **EventTypeRegistry**：事件类型注册和缓存

### 架构图

```
┌─────────────────────────────────────┐
│        应用层 (Application)          │
├─────────────────────────────────────┤
│    SafeZeroGCEventBusController     │  ← 安全控制器
├─────────────────────────────────────┤
│         ZeroGCEventBus              │  ← 核心事件总线
├─────────────────────────────────────┤
│  EventBuffer | ListenerManager      │  ← 缓冲区和监听器管理
├─────────────────────────────────────┤
│       EventTypeRegistry             │  ← 类型注册和缓存
└─────────────────────────────────────┘
```

## 快速开始

### 1. 定义事件

```csharp
public struct PlayerHealthChangedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int PlayerId;
    public int OldHealth;
    public int NewHealth;
    public Vector3 Position;
}
```

### 2. 订阅事件

```csharp
// 基础订阅
var handlerId = ZeroGCEventBus.Instance.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);

// 安全订阅（推荐）
var handlerId = SafeZeroGCEventBusController.SafeSubscribe<PlayerHealthChangedEvent>(OnHealthChanged, "玩家健康");
```

### 3. 发布事件

```csharp
var healthEvent = new PlayerHealthChangedEvent
{
    PlayerId = 1,
    OldHealth = 100,
    NewHealth = 80,
    Position = Vector3.zero
};

// 延迟发布（缓冲）
ZeroGCEventBus.Instance.Publish(healthEvent);

// 立即发布
ZeroGCEventBus.Instance.PublishImmediate(healthEvent);
```

### 4. 取消订阅

```csharp
// 基础取消订阅
ZeroGCEventBus.Instance.Unsubscribe(handlerId);

// 安全取消订阅（推荐）
SafeZeroGCEventBusController.SafeUnsubscribe(handlerId, "玩家健康");
```

## 核心功能详解

### 1. 事件定义

所有事件必须实现 `IZeroGCEvent` 接口：

```csharp
public interface IZeroGCEvent
{
    // 标记接口，无需实现任何方法
}
```

事件应该定义为结构体以避免GC分配：

```csharp
public struct ItemCollectedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int ItemId;
    public int PlayerId;
    public Vector3 Position;
    public float Value;
}
```

### 2. 缓冲区管理

系统支持多种缓冲区溢出策略：

```csharp
public enum BufferOverflowStrategy
{
    DropNewest,    // 丢弃最新事件（默认）
    DropOldest,    // 丢弃最旧事件
    Resize,        // 自动扩容
    LogWarning     // 记录警告并丢弃
}

// 配置默认缓冲区
ZeroGCEventBus.Instance.SetDefaultBufferConfig(512, BufferOverflowStrategy.Resize);
```

### 3. 性能监控

系统提供详细的性能统计：

```csharp
var stats = ZeroGCEventBus.Instance.GetPerformanceStats();
Debug.Log($"本帧处理事件: {stats.EventsProcessedThisFrame}");
Debug.Log($"平均处理时间: {stats.AverageProcessingTime:F3}ms");
Debug.Log($"注册事件类型: {stats.RegisteredEventTypes}");
```

## SafeZeroGCEventBusController 安全控制器

### 主要功能

1. **安全操作**：所有操作都包含异常处理和状态检查
2. **批量订阅管理**：支持按组管理事件订阅
3. **详细日志**：提供详细的操作日志
4. **自动清理**：自动管理无效订阅的清理

### 批量订阅管理

```csharp
// 订阅到指定组
var subscription = SafeZeroGCEventBusController.SubscribeToGroup<PlayerHealthChangedEvent>(
    OnHealthChanged, 
    "PlayerEvents", 
    "玩家健康事件"
);

// 取消整个组的订阅
int unsubscribed = SafeZeroGCEventBusController.UnsubscribeGroup("PlayerEvents");

// 清理无效订阅
int cleaned = SafeZeroGCEventBusController.CleanupInvalidSubscriptions("PlayerEvents");
```

### 统计和调试

```csharp
// 获取监听器数量
int listenerCount = SafeZeroGCEventBusController.GetListenerCount<PlayerHealthChangedEvent>();

// 获取订阅组信息
var groupsInfo = SafeZeroGCEventBusController.GetSubscriptionGroupsInfo();

// 打印统计信息
SafeZeroGCEventBusController.LogSubscriptionGroupsStats();
```

## 最佳实践

### 1. 事件设计

- **使用结构体**：避免GC分配
- **合理大小**：避免过大的事件数据
- **明确语义**：事件名称和字段要有明确的含义

```csharp
// ✅ 推荐
public struct PlayerDeathEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int PlayerId;
    public Vector3 DeathPosition;
    public int KillerId;
    public DeathCause Cause;
}

// ❌ 不推荐
public class PlayerEventData : ZeroGCEventBus.IZeroGCEvent
{
    public Dictionary<string, object> Data;
}
```

### 2. 订阅管理

```csharp
public class PlayerController : MonoBehaviour
{
    private ZeroGCEventBus.EventHandlerId<PlayerHealthChangedEvent> _healthEventId;
    
    void Start()
    {
        // 使用安全控制器订阅
        _healthEventId = SafeZeroGCEventBusController.SafeSubscribe<PlayerHealthChangedEvent>(
            OnHealthChanged, 
            "玩家血量变化"
        );
    }
    
    void OnDestroy()
    {
        // 确保取消订阅
        SafeZeroGCEventBusController.SafeUnsubscribe(_healthEventId, "玩家血量变化");
    }
}
```

### 3. 组管理

```csharp
public class UIManager : MonoBehaviour
{
    private const string UI_EVENT_GROUP = "UIEvents";
    
    void Start()
    {
        // 将所有UI相关事件订阅到同一组
        SafeZeroGCEventBusController.SubscribeToGroup<PlayerHealthChangedEvent>(
            UpdateHealthBar, UI_EVENT_GROUP);
        SafeZeroGCEventBusController.SubscribeToGroup<PlayerLevelUpEvent>(
            ShowLevelUpEffect, UI_EVENT_GROUP);
    }
    
    void OnDestroy()
    {
        // 一次性取消整个组的订阅
        SafeZeroGCEventBusController.UnsubscribeGroup(UI_EVENT_GROUP);
    }
}
```

### 4. 性能优化

```csharp
// 配置适当的缓冲区大小
ZeroGCEventBus.Instance.SetDefaultBufferConfig(1024, BufferOverflowStrategy.Resize);

// 定期清理无效监听器
void Update()
{
    if (Time.frameCount % 300 == 0) // 每5秒清理一次
    {
        SafeZeroGCEventBusController.CleanupInvalidListeners();
    }
}
```

## API 参考

### ZeroGCEventBus 主要方法

| 方法 | 描述 | 返回值 |
|------|------|--------|
| `Subscribe<T>(Action<T> handler)` | 订阅事件 | EventHandlerId<T> |
| `Unsubscribe<T>(EventHandlerId<T> handlerId)` | 取消订阅 | void |
| `Publish<T>(T eventData)` | 发布事件（延迟） | bool |
| `PublishImmediate<T>(T eventData)` | 立即发布事件 | void |
| `GetListenerCount<T>()` | 获取监听器数量 | int |
| `ClearEventBuffer<T>()` | 清空事件缓冲区 | void |
| `CleanupInvalidListeners()` | 清理无效监听器 | void |
| `GetPerformanceStats()` | 获取性能统计 | PerformanceStats |

### SafeZeroGCEventBusController 主要方法

| 方法 | 描述 | 返回值 |
|------|------|--------|
| `SafeSubscribe<T>(Action<T> handler, string eventName)` | 安全订阅事件 | EventHandlerId<T> |
| `SafeUnsubscribe<T>(EventHandlerId<T> handlerId, string eventName)` | 安全取消订阅 | bool |
| `SafePublish<T>(T eventData, string eventName)` | 安全发布事件 | bool |
| `SafePublishImmediate<T>(T eventData, string eventName)` | 安全立即发布 | bool |
| `SubscribeToGroup<T>(Action<T> handler, string groupName, string eventName)` | 订阅到组 | IEventSubscriptionInfo |
| `UnsubscribeGroup(string groupName)` | 取消组订阅 | int |
| `CleanupInvalidSubscriptions(string groupName)` | 清理组内无效订阅 | int |
| `GetSubscriptionGroupsInfo()` | 获取组信息 | Dictionary<string, int> |
| `PerformFullCleanup()` | 执行完整清理 | int |

## 示例代码

### 完整的游戏事件系统

```csharp
// 定义游戏事件
public struct GameStartEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int Level;
    public GameDifficulty Difficulty;
}

public struct PlayerScoreEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int PlayerId;
    public int Score;
    public int ComboCount;
}

// 游戏管理器
public class GameManager : MonoBehaviour
{
    private const string GAME_EVENT_GROUP = "GameEvents";
    
    void Start()
    {
        // 订阅游戏事件
        SafeZeroGCEventBusController.SubscribeToGroup<GameStartEvent>(
            OnGameStart, GAME_EVENT_GROUP);
        SafeZeroGCEventBusController.SubscribeToGroup<PlayerScoreEvent>(
            OnPlayerScore, GAME_EVENT_GROUP);
    }
    
    public void StartGame(int level, GameDifficulty difficulty)
    {
        var gameStartEvent = new GameStartEvent
        {
            Level = level,
            Difficulty = difficulty
        };
        
        SafeZeroGCEventBusController.SafePublish(gameStartEvent, "游戏开始");
    }
    
    private void OnGameStart(GameStartEvent eventData)
    {
        Debug.Log($"游戏开始：级别 {eventData.Level}，难度 {eventData.Difficulty}");
    }
    
    private void OnPlayerScore(PlayerScoreEvent eventData)
    {
        Debug.Log($"玩家 {eventData.PlayerId} 得分：{eventData.Score}");
    }
    
    void OnDestroy()
    {
        SafeZeroGCEventBusController.UnsubscribeGroup(GAME_EVENT_GROUP);
    }
}
```

## 调试和故障排除

### 常见问题

1. **事件未被处理**
   - 检查是否正确订阅了事件
   - 确认事件类型是否匹配
   - 检查事件总线是否已初始化

2. **性能问题**
   - 检查缓冲区配置是否合适
   - 监控事件处理时间
   - 清理无效的监听器

3. **内存泄漏**
   - 确保及时取消订阅
   - 使用安全控制器的自动清理功能
   - 定期执行完整清理

### 调试技巧

```csharp
// 启用详细日志
#if UNITY_EDITOR
ZeroGCEventBus.Instance.LogPerformanceStats();
SafeZeroGCEventBusController.LogSubscriptionGroupsStats();
#endif

// 监控性能
void Update()
{
    if (Input.GetKeyDown(KeyCode.F1))
    {
        var stats = ZeroGCEventBus.Instance.GetPerformanceStats();
        Debug.Log($"事件处理统计：{stats.EventsProcessedThisFrame} 事件/帧");
    }
}
```

## 注意事项

1. **线程安全**：虽然系统是线程安全的，但建议在主线程中使用
2. **事件顺序**：延迟发布的事件会在下一帧处理，立即发布会同步处理
3. **缓冲区大小**：根据实际需要调整缓冲区大小，避免内存浪费
4. **清理策略**：定期清理无效监听器，保持系统性能

## 许可证

此项目使用 MIT 许可证。详情请参见 LICENSE 文件。

---

更多详细信息和高级用法，请参阅源代码中的示例和注释。 
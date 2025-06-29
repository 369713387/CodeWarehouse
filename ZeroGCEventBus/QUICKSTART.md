# ZeroGCEventBus 快速入门指南

## 5分钟快速上手

### 第1步：导入系统

确保 `ZeroGCEventBus` 文件夹已添加到您的Unity项目中。

### 第2步：定义您的第一个事件

```csharp
using Framework.ZeroGCEventBus;

public struct PlayerHealthChangedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int PlayerId;
    public int NewHealth;
    public int MaxHealth;
}
```

### 第3步：订阅事件

```csharp
using Framework.ZeroGCEventBus;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    private ZeroGCEventBus.EventHandlerId<PlayerHealthChangedEvent> _healthEventId;
    
    void Start()
    {
        // 安全订阅事件
        _healthEventId = SafeZeroGCEventBusController.SafeSubscribe<PlayerHealthChangedEvent>(
            OnPlayerHealthChanged, 
            "玩家血量UI更新"
        );
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        Debug.Log($"玩家 {eventData.PlayerId} 血量变化: {eventData.NewHealth}/{eventData.MaxHealth}");
        // 更新UI显示
        UpdateHealthBar(eventData.NewHealth, eventData.MaxHealth);
    }
    
    private void UpdateHealthBar(int current, int max)
    {
        // 实现血量条更新逻辑
    }
    
    void OnDestroy()
    {
        // 取消订阅
        SafeZeroGCEventBusController.SafeUnsubscribe(_healthEventId, "玩家血量UI更新");
    }
}
```

### 第4步：发布事件

```csharp
public class Player : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // 发布血量变化事件
        var healthEvent = new PlayerHealthChangedEvent
        {
            PlayerId = GetInstanceID(),
            NewHealth = currentHealth,
            MaxHealth = maxHealth
        };
        
        SafeZeroGCEventBusController.SafePublish(healthEvent, "玩家受伤");
    }
}
```

## 常用模式

### 1. 组管理模式（推荐）

适用于需要管理多个相关事件订阅的场景：

```csharp
public class GameManager : MonoBehaviour
{
    private const string GAME_EVENTS = "GameEvents";
    
    void Start()
    {
        // 将相关事件订阅到同一组
        SafeZeroGCEventBusController.SubscribeToGroup<PlayerHealthChangedEvent>(
            OnPlayerHealth, GAME_EVENTS, "玩家血量");
        SafeZeroGCEventBusController.SubscribeToGroup<PlayerLevelUpEvent>(
            OnPlayerLevelUp, GAME_EVENTS, "玩家升级");
        SafeZeroGCEventBusController.SubscribeToGroup<GameStateEvent>(
            OnGameState, GAME_EVENTS, "游戏状态");
    }
    
    void OnDestroy()
    {
        // 一次性取消整个组的订阅
        SafeZeroGCEventBusController.UnsubscribeGroup(GAME_EVENTS);
    }
    
    private void OnPlayerHealth(PlayerHealthChangedEvent eventData) { /* 处理逻辑 */ }
    private void OnPlayerLevelUp(PlayerLevelUpEvent eventData) { /* 处理逻辑 */ }
    private void OnGameState(GameStateEvent eventData) { /* 处理逻辑 */ }
}
```

### 2. 立即发布模式

适用于需要同步处理的重要事件：

```csharp
public void PlayerDied()
{
    var deathEvent = new PlayerDeathEvent
    {
        PlayerId = GetInstanceID(),
        DeathTime = Time.time,
        Position = transform.position
    };
    
    // 立即发布，同步处理
    SafeZeroGCEventBusController.SafePublishImmediate(deathEvent, "玩家死亡");
}
```

### 3. 批量清理模式

在场景切换或游戏结束时使用：

```csharp
public class SceneManager : MonoBehaviour
{
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 执行完整清理
            SafeZeroGCEventBusController.PerformFullCleanup();
        }
    }
}
```

## 常见事件定义模板

### 游戏状态事件

```csharp
public struct GameStateChangedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public GameState OldState;
    public GameState NewState;
    public float TransitionTime;
}

public enum GameState { Menu, Playing, Paused, GameOver }
```

### 物品相关事件

```csharp
public struct ItemCollectedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int ItemId;
    public int PlayerId;
    public Vector3 Position;
    public ItemType Type;
    public int Quantity;
}

public struct ItemUsedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int ItemId;
    public int PlayerId;
    public ItemType Type;
    public bool Success;
}
```

### UI事件

```csharp
public struct ButtonClickEvent : ZeroGCEventBus.IZeroGCEvent
{
    public string ButtonName;
    public int PlayerId;
    public Vector2 ClickPosition;
}

public struct MenuChangedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public string FromMenu;
    public string ToMenu;
    public float TransitionDuration;
}
```

## 性能优化技巧

### 1. 配置缓冲区

```csharp
void Awake()
{
    // 根据游戏需求调整缓冲区大小
    // 高频事件场景建议使用自动扩容策略
    SafeZeroGCEventBusController.SetDefaultBufferConfig(
        1024, 
        ZeroGCEventBus.BufferOverflowStrategy.Resize
    );
}
```

### 2. 定期清理

```csharp
void Update()
{
    // 定期清理（可选，系统会自动清理）
    if (Time.frameCount % 300 == 0) // 每5秒
    {
        SafeZeroGCEventBusController.CleanupInvalidListeners();
    }
}
```

### 3. 性能监控

```csharp
void Update()
{
    // 按F1键显示性能统计
    if (Input.GetKeyDown(KeyCode.F1))
    {
        var stats = ZeroGCEventBus.Instance.GetPerformanceStats();
        Debug.Log($"事件处理统计：{stats.EventsProcessedThisFrame} 事件/帧, " +
                  $"平均处理时间：{stats.AverageProcessingTime:F2}ms");
    }
}
```

## 调试技巧

### 1. 启用详细日志

```csharp
#if UNITY_EDITOR
void Start()
{
    // 打印订阅组统计
    SafeZeroGCEventBusController.LogSubscriptionGroupsStats();
}
#endif
```

### 2. 监控特定事件

```csharp
void Update()
{
    // 监控特定事件的监听器数量
    int healthListeners = SafeZeroGCEventBusController.GetListenerCount<PlayerHealthChangedEvent>();
    if (healthListeners == 0)
    {
        Debug.LogWarning("没有监听器订阅玩家健康事件！");
    }
}
```

### 3. 缓冲区状态检查

```csharp
void LogBufferStatus()
{
    var stats = ZeroGCEventBus.Instance.GetPerformanceStats();
    if (stats.EventsProcessedThisFrame > 100)
    {
        Debug.LogWarning($"高事件负载：{stats.EventsProcessedThisFrame} 事件/帧");
    }
}
```

## 常见问题解决

### Q: 事件没有被处理？

```csharp
// 检查列表：
// 1. 确认已订阅事件
var listenerCount = SafeZeroGCEventBusController.GetListenerCount<YourEventType>();
Debug.Log($"监听器数量: {listenerCount}");

// 2. 确认事件总线可用
if (!SafeZeroGCEventBusController.IsEventBusAvailable)
{
    Debug.LogError("事件总线不可用！");
}

// 3. 检查事件类型是否匹配
// 确保发布和订阅使用相同的事件类型
```

### Q: 性能问题？

```csharp
// 检查缓冲区配置
SafeZeroGCEventBusController.SetDefaultBufferConfig(
    2048, // 增加缓冲区大小
    ZeroGCEventBus.BufferOverflowStrategy.DropOldest // 或使用其他策略
);

// 定期清理
SafeZeroGCEventBusController.PerformFullCleanup();
```

### Q: 内存泄漏？

```csharp
// 确保正确取消订阅
void OnDestroy()
{
    // 方式1：单个取消订阅
    SafeZeroGCEventBusController.SafeUnsubscribe(_eventId);
    
    // 方式2：组取消订阅
    SafeZeroGCEventBusController.UnsubscribeGroup("GroupName");
    
    // 方式3：使用 using 语句（自动清理）
    // var subscription = SafeZeroGCEventBusController.SubscribeToGroup(...);
    // subscription.Dispose(); // 自动调用
}
```

## 完整示例项目

```csharp
// === 事件定义 ===
public struct ScoreChangedEvent : ZeroGCEventBus.IZeroGCEvent
{
    public int PlayerId;
    public int NewScore;
    public int ScoreDelta;
}

// === 分数管理器 ===
public class ScoreManager : MonoBehaviour
{
    private const string SCORE_EVENTS = "ScoreEvents";
    private int currentScore = 0;
    
    void Start()
    {
        SafeZeroGCEventBusController.SubscribeToGroup<ScoreChangedEvent>(
            OnScoreChanged, SCORE_EVENTS, "分数变化");
    }
    
    public void AddScore(int points)
    {
        int oldScore = currentScore;
        currentScore += points;
        
        var scoreEvent = new ScoreChangedEvent
        {
            PlayerId = 1,
            NewScore = currentScore,
            ScoreDelta = points
        };
        
        SafeZeroGCEventBusController.SafePublish(scoreEvent, "加分");
    }
    
    private void OnScoreChanged(ScoreChangedEvent eventData)
    {
        Debug.Log($"分数变化: +{eventData.ScoreDelta}, 总分: {eventData.NewScore}");
    }
    
    void OnDestroy()
    {
        SafeZeroGCEventBusController.UnsubscribeGroup(SCORE_EVENTS);
    }
}

// === UI管理器 ===
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    private ZeroGCEventBus.EventHandlerId<ScoreChangedEvent> _scoreEventId;
    
    void Start()
    {
        _scoreEventId = SafeZeroGCEventBusController.SafeSubscribe<ScoreChangedEvent>(
            UpdateScoreDisplay, "分数UI更新");
    }
    
    private void UpdateScoreDisplay(ScoreChangedEvent eventData)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {eventData.NewScore}";
        }
    }
    
    void OnDestroy()
    {
        SafeZeroGCEventBusController.SafeUnsubscribe(_scoreEventId, "分数UI更新");
    }
}
```

## 下一步

- 阅读 [完整文档](README.md) 了解更多高级功能
- 查看 [技术架构文档](ARCHITECTURE.md) 了解实现原理
- 运行示例场景学习更多用法
- 根据项目需求定制事件系统

现在您已经掌握了ZeroGCEventBus的基本用法，可以开始在项目中使用这个高性能的事件系统了！ 
# AudioManagerExtensions - 音频管理器扩展

## 概述

AudioManagerExtensions 是对 JSAM AudioManager 的扩展，提供了异步音频播放、高级音频控制和对象池优化功能。

## 主要特性

### 🎵 异步音频播放
- 支持 `async/await` 模式的音频播放
- 自动处理播放生命周期
- 支持取消令牌 (CancellationToken)
- 播放完成自动回调

### 🎛️ 高级音频控制
- 提供 `AudioPlaybackController` 进行精细控制
- 支持暂停、恢复、停止操作
- 实时播放进度监控
- 播放状态查询

### 🔄 对象池优化
- 基于 `ObjectPool<T>` 的高性能对象池
- 自动创建和回收 AudioPlaybackController
- 减少 GC 压力和对象创建开销
- 智能池大小管理

## 快速开始

### 1. 初始化对象池

```csharp
void Start()
{
    // 初始化音频控制器对象池（池大小10，最大50）
    AudioManagerExtensions.InitializeAudioControllerPool(10, 50);
    
    // 预热对象池
    AudioManagerExtensions.WarmupAudioControllerPool(5);
}
```

### 2. 异步音频播放

```csharp
// 基本异步播放
await AudioManagerExtensions.PlaySoundAsync(MySoundEnum.ExplosionSound);

// 带位置的异步播放
await AudioManagerExtensions.PlaySoundAsync(MySoundEnum.FootstepSound, transform);

// 带完成回调的异步播放
await AudioManagerExtensions.PlaySoundAsync(MySoundEnum.DialogSound, 
    onComplete: () => Debug.Log("对话播放完成"));

// 可取消的异步播放
using var cts = new CancellationTokenSource();
await AudioManagerExtensions.PlaySoundAsync(MySoundEnum.LongSound, 
    cancellationToken: cts.Token);
```

### 3. 高级音频控制

```csharp
// 获取播放控制器
var controller = AudioManagerExtensions.PlaySoundWithController(MySoundEnum.MusicSound);

if (controller != null)
{
    // 监控播放状态
    Debug.Log($"播放进度: {controller.Progress:P1}");
    Debug.Log($"剩余时间: {controller.RemainingTime:F1}秒");
    
    // 控制播放
    controller.Pause();
    controller.Resume();
    controller.Stop();
    
    // 等待播放完成
    await controller.WaitForCompletion();
}
```

## API 参考

### AudioManagerExtensions 静态方法

#### 对象池管理
```csharp
// 初始化对象池
static void InitializeAudioControllerPool(int poolSize = 10, int maxSize = -1)

// 预热对象池
static void WarmupAudioControllerPool(int count = 5)

// 清空对象池
static void ClearAudioControllerPool()

// 获取统计信息
static string GetAudioControllerPoolStats()

// 手动回收控制器
static bool ReleaseController(AudioPlaybackController controller)
```

#### 异步播放方法
```csharp
// 枚举音效异步播放
static async UniTask PlaySoundAsync<T>(T sound, Transform transform = null, 
    Action onComplete = null, CancellationToken cancellationToken = default) where T : Enum

// SoundFileObject异步播放
static async UniTask PlaySoundAsync(SoundFileObject soundFile, Transform transform = null,
    Action onComplete = null, CancellationToken cancellationToken = default)

// 指定位置异步播放
static async UniTask PlaySoundAsync<T>(T sound, Vector3 position,
    Action onComplete = null, CancellationToken cancellationToken = default) where T : Enum

static async UniTask PlaySoundAsync(SoundFileObject soundFile, Vector3 position,
    Action onComplete = null, CancellationToken cancellationToken = default)
```

#### 控制器播放方法
```csharp
// 获取播放控制器
static AudioPlaybackController PlaySoundWithController<T>(T sound, Transform transform = null,
    Action onComplete = null) where T : Enum

static AudioPlaybackController PlaySoundWithController(SoundFileObject soundFile, Transform transform = null,
    Action onComplete = null)

// 指定位置获取控制器
static AudioPlaybackController PlaySoundWithController<T>(T sound, Vector3 position,
    Action onComplete = null) where T : Enum

static AudioPlaybackController PlaySoundWithController(SoundFileObject soundFile, Vector3 position,
    Action onComplete = null)
```

### AudioPlaybackController 属性和方法

#### 属性
```csharp
bool IsPlaying { get; }        // 是否正在播放
bool IsPaused { get; }         // 是否暂停
bool IsCompleted { get; }      // 是否完成
bool IsCancelled { get; }      // 是否取消
bool IsActive { get; }         // 是否活跃
float Progress { get; }        // 播放进度 (0-1)
float RemainingTime { get; }   // 剩余时间
string StatusDescription { get; } // 状态描述
```

#### 方法
```csharp
// 控制方法
void Stop(bool stopInstantly = true)
bool Pause()
bool Resume()
bool TogglePlayPause()

// 等待方法
async UniTask WaitForCompletion()

// 对象池方法
void ReturnToPool()           // 手动回收到池
void ReturnToPoolAfter(float delay) // 延迟回收
```

## 对象池系统

### 架构说明
新的对象池系统基于 `ObjectPool<AudioPlaybackController>` 实现，提供了以下优势：

1. **类型安全**: 直接管理 AudioPlaybackController 对象
2. **高性能**: 避免 GameObject 组件查找开销
3. **自动管理**: 智能的获取、释放和销毁策略
4. **内存优化**: 有效减少 GC 分配

### 生命周期管理
```csharp
创建 -> 获取 -> 使用 -> 自动回收 -> 重用
 ↓      ↓      ↓        ↓        ↓
Pool   Get   Play   Complete  Release
```

### 统计信息
```csharp
// 查看对象池状态
Debug.Log(AudioManagerExtensions.GetAudioControllerPoolStats());

// 输出示例：
// ObjectPool<AudioPlaybackController>: Total=10, Active=2, Inactive=8, MaxSize=50
```

## 性能优化建议

### 1. 合理设置池大小
```csharp
// 根据游戏需求调整池大小
// 小型游戏: 5-10
// 中型游戏: 10-20  
// 大型游戏: 20-50
AudioManagerExtensions.InitializeAudioControllerPool(15, 30);
```

### 2. 预热对象池
```csharp
// 在游戏开始时预热，避免运行时创建延迟
AudioManagerExtensions.WarmupAudioControllerPool(5);
```

### 3. 监控池状态
```csharp
// 定期检查池使用情况
void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        Debug.Log(AudioManagerExtensions.GetAudioControllerPoolStats());
    }
}
```

## 测试和调试

### 使用 AudioManagerExtensionsTest
1. 将 `AudioManagerExtensionsTest` 组件添加到场景中的 GameObject
2. 设置测试音效文件
3. 配置测试参数
4. 运行场景查看测试结果

### 调试快捷键
- `P` 键: 显示对象池统计信息
- GUI 按钮: 手动控制池操作

### 常见问题排查
1. **控制器获取失败**: 检查对象池是否已初始化
2. **音效播放失败**: 确认 AudioManager 已正确设置
3. **内存泄漏**: 监控对象池统计，确保对象正确回收

## 依赖项

- Unity 2021.3 或更高版本
- JSAM (Jacky's Simple Audio Manager)
- UniTask (用于异步操作)
- YiFun.Pool 对象池系统

## 升级说明

### 从 GameObjectPool 到 ObjectPool
此版本将底层对象池从 `GameObjectPool` 升级为 `ObjectPool<AudioPlaybackController>`：

**主要变化:**
1. **初始化方法**: 不再需要预制体参数，系统自动创建控制器
2. **回收机制**: AudioPlaybackController 重写了 ReturnToPool 方法
3. **性能提升**: 直接管理控制器对象，避免 GameObject 查找开销

**向后兼容:**
- 所有公开 API 保持不变
- 现有代码无需修改
- 自动处理新的对象池类型

### 最佳实践
- 游戏启动时初始化对象池
- 使用统计信息监控池性能
- 根据实际使用情况调整池大小
- 定期清理不需要的对象池 
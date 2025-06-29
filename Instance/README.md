# Double Check Locking 双检查锁单例模式

这个实现提供了两种线程安全的单例模式，使用 Double Check Locking 机制确保高性能和线程安全。

## 文件结构

```
Assets/github/Instance/
├── Singleton.cs          # 普通 C# 类单例基类
├── MonoSingleton.cs     # Unity MonoBehaviour 单例基类
├── SingletonExample.cs  # 使用示例
├── SingletonTest.cs     # 测试脚本
└── README.md           # 使用说明
```

## 特性

### 🔒 线程安全
- 使用 Double Check Locking 双检查锁机制
- 使用 `volatile` 关键字确保内存可见性
- 防止多线程环境下的竞争条件

### ⚡ 高性能
- 第一次检查避免不必要的锁操作
- 只在需要创建实例时才进入临界区
- 最小化锁的持有时间

### 🛡️ 安全保护
- 防止反射创建多个实例
- 提供实例销毁机制
- Unity 版本支持应用程序退出检测

## 使用方法

### 1. 普通 C# 类单例 (Singleton<T>)

适用于不需要继承 MonoBehaviour 的管理类：

```csharp
public class GameManager : Singleton<GameManager>, IDisposable
{
    private int _score = 0;

    public int Score
    {
        get => _score;
        set => _score = value;
    }

    protected override void Initialize()
    {
        Debug.Log("GameManager initialized");
    }

    protected override void Cleanup()
    {
        Debug.Log("GameManager cleanup");
        Dispose();
    }

    public void Dispose()
    {
        // 清理资源
        _score = 0;
    }

    public void AddScore(int points)
    {
        _score += points;
    }
}

// 使用方式
var gameManager = GameManager.Instance;
gameManager.AddScore(100);
```

### 2. Unity MonoBehaviour 单例 (MonoSingleton<T>)

适用于需要 Unity 生命周期的组件：

```csharp
public class AudioManager : MonoSingleton<AudioManager>
{
    [SerializeField] private AudioSource _audioSource;

    protected override void Initialize()
    {
        Debug.Log("AudioManager initialized");
        
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    protected override void Cleanup()
    {
        Debug.Log("AudioManager cleanup");
        
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (_audioSource != null)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
        }
    }
}

// 使用方式
var audioManager = AudioManager.Instance;
audioManager.PlaySound(myAudioClip);
```

## API 参考

### Singleton<T> 基类

| 成员 | 类型 | 描述 |
|------|------|------|
| `Instance` | 静态属性 | 获取单例实例 |
| `HasInstance` | 静态属性 | 检查实例是否已创建 |
| `DestroyInstance()` | 静态方法 | 销毁单例实例 |
| `Initialize()` | 虚方法 | 初始化回调，供子类重写 |
| `Cleanup()` | 虚方法 | 清理回调，供子类重写 |

### MonoSingleton<T> 基类

继承自 `Singleton<T>` 的所有成员，额外提供：

| 成员 | 类型 | 描述 |
|------|------|------|
| `Awake()` | 虚方法 | Unity 生命周期方法 |
| `OnDestroy()` | 虚方法 | Unity 生命周期方法 |
| `OnApplicationQuit()` | 虚方法 | 应用程序退出时调用 |

## Double Check Locking 原理

```csharp
public static T Instance
{
    get
    {
        // 第一次检查：避免不必要的锁
        if (_instance == null)
        {
            lock (_lock)
            {
                // 第二次检查：防止多线程重复创建
                if (_instance == null)
                {
                    _instance = new T();
                }
            }
        }
        return _instance;
    }
}
```

1. **第一次检查**：如果实例已存在，直接返回，避免进入锁
2. **获取锁**：只有在实例不存在时才获取锁
3. **第二次检查**：在锁内再次检查，防止其他线程已经创建了实例
4. **创建实例**：确保只创建一次

## 测试

使用 `SingletonTest` 脚本进行测试：

1. 将 `SingletonTest` 脚本添加到场景中的 GameObject
2. 运行场景，查看控制台输出
3. 使用游戏界面中的按钮进行交互测试

测试包括：
- 基本功能测试
- 单例特性验证
- 多线程安全测试
- 实例生命周期管理

## 注意事项

### 对于 Singleton<T>
- 确保子类有无参构造函数
- 如果需要资源清理，实现 `IDisposable` 接口
- 重写 `Initialize()` 和 `Cleanup()` 方法进行自定义初始化

### 对于 MonoSingleton<T>
- 实例会自动设置 `DontDestroyOnLoad`
- 场景中的重复实例会被自动销毁
- 应用程序退出时会阻止新实例创建

### 最佳实践
- 单例应该是无状态的或状态变化很少
- 避免在单例中存储大量数据
- 合理使用 `DestroyInstance()` 进行资源清理
- 在多线程环境中使用时要注意线程安全

## 性能特性

- ✅ 高性能：第一次检查避免锁开销
- ✅ 线程安全：双检查锁机制
- ✅ 内存安全：volatile 确保可见性
- ✅ 延迟加载：只在需要时创建实例
- ✅ 防御性编程：多重保护机制 
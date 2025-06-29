# YiFun 对象池系统 (GameObjectPool)

## 简介

YiFun对象池系统是一个高效的Unity游戏对象池化解决方案，旨在通过重用对象来减少频繁的实例化和销毁操作，从而提升游戏性能。该系统支持GameObject池化和泛型对象池化，具有完整的生命周期管理和调试功能。

## 核心特性

- 🚀 **高性能**: 通过对象重用减少GC压力
- 🎯 **类型安全**: 支持泛型对象池和GameObject专用池
- 🔧 **易于使用**: 简单的API和组件化设计
- 📊 **调试友好**: 内置统计信息和调试日志
- 🎨 **灵活配置**: 支持预加载、大小限制等配置选项
- 🔄 **生命周期管理**: 完整的对象获取和回收生命周期

## 系统架构

```
YiFun.Pool命名空间
├── IPoolable                    # 基础池化接口
├── IGameObjectPoolable         # GameObject池化接口
├── ObjectPool<T>               # 泛型对象池
├── GameObjectPool              # GameObject专用池
├── PoolManager                 # 单例池管理器
├── PoolableComponent           # 可池化组件基类
└── SimplePoolableComponent    # 简单池化组件实现
```

## 快速开始

### 1. 基础使用

```csharp
// 获取池管理器实例
var poolManager = PoolManager.Instance;

// 从指定池获取GameObject
GameObject obj = poolManager.Get("BulletPool");

// 使用完毕后回收对象
poolManager.Release(obj);
```

### 2. 创建可池化组件

```csharp
using YiFun.Pool;

public class Bullet : PoolableComponent
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    
    protected override void OnSpawnImplementation()
    {
        // 对象从池中获取时的初始化逻辑
        GetComponent<Rigidbody>().velocity = transform.forward * speed;
        
        // 设置自动回收
        ReturnToPoolAfter(lifetime);
    }
    
    protected override void OnRecycleImplementation()
    {
        // 对象回收时的清理逻辑
        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}
```

### 3. 设置GameObjectPool

在Unity Inspector中：
1. 创建空GameObject并添加`GameObjectPool`组件
2. 设置池名称和预制体引用
3. 配置预加载数量和最大大小
4. （可选）通过`PoolManager.Instance.RegisterPool(pool)`注册到管理器

## 详细API参考

### IPoolable 接口

```csharp
public interface IPoolable
{
    void OnSpawn();           // 从池中获取时调用
    void OnRecycle();         // 回收到池中时调用
    bool IsInUse { get; set; } // 是否正在使用
}
```

### IGameObjectPoolable 接口

```csharp
public interface IGameObjectPoolable : IPoolable
{
    GameObject GameObject { get; }  // GameObject引用
    Transform Transform { get; }    // Transform引用
}
```

### PoolManager 主要方法

```csharp
// 注册GameObject对象池
bool RegisterPool(GameObjectPool pool)

// 从指定池获取GameObject
GameObject Get(string poolName, Transform parent = null, bool worldPositionStays = false)

// 从指定池获取GameObject并返回组件
T Get<T>(string poolName, Transform parent = null, bool worldPositionStays = false) where T : Component

// 回收GameObject到对应池
bool Release(GameObject obj)

// 创建类型化对象池
ObjectPool<T> CreateTypedPool<T>(...)

// 预热所有池
void WarmupAll(int count)

// 清空所有池
void ClearAll()
```

### GameObjectPool 主要方法

```csharp
// 从池中获取GameObject
GameObject Get(Transform parent = null, bool worldPositionStays = false)

// 从池中获取GameObject并返回组件
T Get<T>(Transform parent = null, bool worldPositionStays = false) where T : Component

// 回收GameObject到池
bool Release(GameObject obj)

// 预热对象池
void Warmup(int count)

// 清空对象池
void Clear()

// 获取统计信息
string GetStatsString()
```

### ObjectPool<T> 主要方法

```csharp
// 从池中获取对象
T Get()

// 回收对象到池
bool Release(T obj)

// 预热对象池
void Warmup(int count)

// 清空对象池
void Clear()

// 统计属性
int CountAll      // 总对象数
int CountActive   // 活跃对象数
int CountInactive // 池中可用对象数
```

### PoolableComponent 抽象类

```csharp
public abstract class PoolableComponent : MonoBehaviour, IGameObjectPoolable
{
    // 需要子类实现的抽象方法
    protected abstract void OnSpawnImplementation();
    protected abstract void OnRecycleImplementation();
    
    // 可重写的虚方法
    protected virtual void ResetState();    // 重置对象状态
    protected virtual void CleanupState();  // 清理对象状态
    
    // 便捷方法
    void ReturnToPool();                    // 立即回收到池
    void ReturnToPoolAfter(float delay);   // 延迟回收到池
}
```

## 使用示例

### 示例1：子弹系统

```csharp
// 子弹类
public class Bullet : PoolableComponent
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 10f;
    
    protected override void OnSpawnImplementation()
    {
        // 设置速度
        GetComponent<Rigidbody>().velocity = transform.forward * speed;
        
        // 5秒后自动回收
        ReturnToPoolAfter(5f);
    }
    
    protected override void OnRecycleImplementation()
    {
        // 停止移动
        GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 造成伤害
            other.GetComponent<Health>().TakeDamage(damage);
            
            // 立即回收
            ReturnToPool();
        }
    }
}

// 武器类
public class Gun : MonoBehaviour
{
    [SerializeField] private string bulletPoolName = "BulletPool";
    [SerializeField] private Transform firePoint;
    
    public void Fire()
    {
        // 从池中获取子弹
        var bullet = PoolManager.Instance.Get<Bullet>(bulletPoolName, firePoint);
        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;
        }
    }
}
```

### 示例2：特效系统

```csharp
public class ParticleEffect : PoolableComponent
{
    private ParticleSystem particles;
    
    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
    }
    
    protected override void OnSpawnImplementation()
    {
        particles.Play();
        
        // 根据粒子系统持续时间自动回收
        ReturnToPoolAfter(particles.main.duration);
    }
    
    protected override void OnRecycleImplementation()
    {
        particles.Stop();
        particles.Clear();
    }
}
```

### 示例3：UI元素池化

```csharp
public class UIListItem : PoolableComponent
{
    [SerializeField] private Text titleText;
    [SerializeField] private Image iconImage;
    
    protected override void OnSpawnImplementation()
    {
        // UI元素显示时的初始化
        gameObject.SetActive(true);
    }
    
    protected override void OnRecycleImplementation()
    {
        // 清理UI数据
        titleText.text = "";
        iconImage.sprite = null;
        gameObject.SetActive(false);
    }
    
    public void SetData(string title, Sprite icon)
    {
        titleText.text = title;
        iconImage.sprite = icon;
    }
}
```

### 示例4：泛型对象池

```csharp
// 自定义数据类
public class GameData
{
    public int id;
    public string name;
    public float value;
    
    public void Reset()
    {
        id = 0;
        name = "";
        value = 0f;
    }
}

// 使用泛型对象池
public class DataManager : MonoBehaviour
{
    private ObjectPool<GameData> dataPool;
    
    private void Start()
    {
        // 创建泛型对象池
        dataPool = new ObjectPool<GameData>(
            createFunc: () => new GameData(),
            onGet: data => Debug.Log($"获取数据对象: {data.id}"),
            onRelease: data => data.Reset(),
            preloadCount: 10
        );
    }
    
    public GameData GetData()
    {
        return dataPool.Get();
    }
    
    public void ReleaseData(GameData data)
    {
        dataPool.Release(data);
    }
}
```

## 配置选项

### GameObjectPool 配置

```csharp
[Header("池配置")]
[SerializeField] private string _poolName = "GameObjectPool";     // 池名称
[SerializeField] private GameObject _prefab;                      // 预制体
[SerializeField] private int _preloadCount = 5;                   // 预加载数量
[SerializeField] private int _maxSize = 50;                       // 最大大小
[SerializeField] private bool _autoExpand = true;                 // 自动扩展
[SerializeField] private Transform _poolRoot;                     // 池根节点
```

### PoolManager 配置

```csharp
[Header("池管理器设置")]
[SerializeField] private bool _enableDebugLogging = true;         // 启用调试日志
[SerializeField] private bool _autoRegisterPoolsInChildren = true; // 自动注册子对象池
```

## 性能优化建议

1. **合理设置预加载数量**: 根据实际需求预加载对象，避免运行时频繁创建
2. **控制池大小**: 设置合理的最大池大小，防止内存占用过多
3. **及时回收**: 确保对象使用完毕后及时回收到池中
4. **避免频繁获取释放**: 对于生命周期很短的对象，考虑复用策略
5. **使用对象重置**: 在OnRecycle中重置对象状态，确保对象可以被正确复用

## 调试功能

### 统计信息

```csharp
// 获取单个池的统计信息
string stats = gameObjectPool.GetStatsString();

// 获取所有池的统计信息
string allStats = PoolManager.Instance.GetAllStatsString();
```

### Unity编辑器工具

- **Tools/Pool Manager/Show Stats**: 显示所有池的统计信息
- **Tools/Pool Manager/Clear All Pools**: 清空所有对象池

### Inspector调试信息

GameObjectPool组件在Inspector中显示：
- 总对象数量
- 活跃对象数量
- 非活跃对象数量

## 常见问题

### Q: 对象没有被正确回收怎么办？
A: 检查对象是否实现了IPoolable接口，确保调用了正确的Release方法。

### Q: 如何处理对象的引用和依赖？
A: 在OnRecycle方法中清理所有引用，在OnSpawn方法中重新设置必要的引用。

### Q: 池化对象的生命周期是什么？
A: 创建 -> OnSpawn -> 使用 -> OnRecycle -> 回池 -> (循环)

### Q: 如何避免内存泄漏？
A: 确保在OnRecycle中清理所有事件订阅、协程和临时引用。

## 版本历史

- **v1.0**: 基础对象池功能
- **v1.1**: 添加泛型对象池支持
- **v1.2**: 增强调试功能和统计信息
- **v1.3**: 优化性能和内存管理

## 许可证

本项目采用MIT许可证，详见LICENSE文件。 
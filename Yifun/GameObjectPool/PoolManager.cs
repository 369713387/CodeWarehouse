using System.Collections.Generic;
using UnityEngine;

namespace YiFun.Pool
{
    /// <summary>
    /// 对象池管理器 - 单例模式
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        #region 单例
        private static PoolManager _instance;
        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PoolManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("PoolManager");
                        _instance = go.AddComponent<PoolManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("池管理器设置")]
        [SerializeField] private bool _enableDebugLogging = true;
        [SerializeField] private bool _autoRegisterPoolsInChildren = true;

        [Header("统计信息")]
        [SerializeField, ReadOnly] private int _totalPoolCount;
        [SerializeField, ReadOnly] private int _totalObjectCount;
        [SerializeField, ReadOnly] private int _totalActiveCount;

        private readonly Dictionary<string, GameObjectPool> _gameObjectPools = new Dictionary<string, GameObjectPool>();
        private readonly Dictionary<System.Type, object> _typedPools = new Dictionary<System.Type, object>();

        #region Unity生命周期
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateStatistics();
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 注册GameObject对象池
        /// </summary>
        /// <param name="pool">要注册的对象池</param>
        /// <returns>是否注册成功</returns>
        public bool RegisterPool(GameObjectPool pool)
        {
            if (pool == null)
            {
                LogError("尝试注册null对象池");
                return false;
            }

            string poolName = pool.PoolName;
            if (_gameObjectPools.ContainsKey(poolName))
            {
                LogWarning($"对象池 '{poolName}' 已存在，将覆盖原有池");
            }

            _gameObjectPools[poolName] = pool;
            Log($"注册GameObject对象池: {poolName}");
            return true;
        }

        /// <summary>
        /// 注销GameObject对象池
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <returns>是否注销成功</returns>
        public bool UnregisterPool(string poolName)
        {
            if (_gameObjectPools.Remove(poolName))
            {
                Log($"注销GameObject对象池: {poolName}");
                return true;
            }
            
            LogWarning($"未找到要注销的对象池: {poolName}");
            return false;
        }

        /// <summary>
        /// 获取GameObject对象池
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <returns>对象池实例</returns>
        public GameObjectPool GetGameObjectPool(string poolName)
        {
            _gameObjectPools.TryGetValue(poolName, out var pool);
            return pool;
        }

        /// <summary>
        /// 从指定池中获取GameObject
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <param name="parent">父对象</param>
        /// <param name="worldPositionStays">是否保持世界坐标</param>
        /// <returns>GameObject实例</returns>
        public GameObject Get(string poolName, Transform parent = null, bool worldPositionStays = false)
        {
            var pool = GetGameObjectPool(poolName);
            if (pool == null)
            {
                LogError($"未找到名为 '{poolName}' 的对象池");
                return null;
            }

            return pool.Get(parent, worldPositionStays);
        }

        /// <summary>
        /// 从指定池中获取GameObject并返回组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="poolName">池名称</param>
        /// <param name="parent">父对象</param>
        /// <param name="worldPositionStays">是否保持世界坐标</param>
        /// <returns>组件实例</returns>
        public T Get<T>(string poolName, Transform parent = null, bool worldPositionStays = false) where T : Component
        {
            var pool = GetGameObjectPool(poolName);
            if (pool == null)
            {
                LogError($"未找到名为 '{poolName}' 的对象池");
                return null;
            }

            return pool.Get<T>(parent, worldPositionStays);
        }

        /// <summary>
        /// 将GameObject回收到对应的池中
        /// </summary>
        /// <param name="obj">要回收的GameObject</param>
        /// <returns>是否成功回收</returns>
        public bool Release(GameObject obj)
        {
            if (obj == null)
            {
                LogWarning("尝试回收null GameObject");
                return false;
            }

            // 通过父对象查找对应的池
            var poolComponent = obj.GetComponentInParent<GameObjectPool>();
            if (poolComponent != null)
            {
                return poolComponent.Release(obj);
            }

            // 如果找不到，尝试在所有池中查找
            foreach (var pool in _gameObjectPools.Values)
            {
                if (pool.Release(obj))
                {
                    return true;
                }
            }

            LogWarning($"无法找到GameObject '{obj.name}' 对应的对象池");
            return false;
        }

        /// <summary>
        /// 创建和注册类型化对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="createFunc">创建函数</param>
        /// <param name="onGet">获取回调</param>
        /// <param name="onRelease">释放回调</param>
        /// <param name="onDestroy">销毁回调</param>
        /// <param name="maxSize">最大大小</param>
        /// <param name="preloadCount">预加载数量</param>
        /// <returns>对象池实例</returns>
        public ObjectPool<T> CreateTypedPool<T>(
            System.Func<T> createFunc,
            System.Action<T> onGet = null,
            System.Action<T> onRelease = null,
            System.Action<T> onDestroy = null,
            int maxSize = -1,
            int preloadCount = 0) where T : class
        {
            var pool = new ObjectPool<T>(createFunc, onGet, onRelease, onDestroy, maxSize, preloadCount);
            _typedPools[typeof(T)] = pool;
            Log($"创建类型化对象池: {typeof(T).Name}");
            return pool;
        }

        /// <summary>
        /// 获取类型化对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象池实例</returns>
        public ObjectPool<T> GetTypedPool<T>() where T : class
        {
            _typedPools.TryGetValue(typeof(T), out var pool);
            return pool as ObjectPool<T>;
        }

        /// <summary>
        /// 预热所有对象池
        /// </summary>
        /// <param name="count">预热数量</param>
        public void WarmupAll(int count)
        {
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.Warmup(count);
            }
            Log($"预热所有对象池，每个池 {count} 个对象");
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _gameObjectPools.Values)
            {
                pool.Clear();
            }

            foreach (var pool in _typedPools.Values)
            {
                if (pool is ObjectPool<object> objPool)
                {
                    objPool.Clear();
                }
            }

            Log("清空所有对象池");
        }

        /// <summary>
        /// 获取所有对象池的统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetAllStatsString()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 对象池管理器统计 ===");
            stats.AppendLine($"GameObject池数量: {_gameObjectPools.Count}");
            stats.AppendLine($"类型化池数量: {_typedPools.Count}");
            stats.AppendLine($"总对象数量: {_totalObjectCount}");
            stats.AppendLine($"活跃对象数量: {_totalActiveCount}");
            stats.AppendLine();

            stats.AppendLine("=== GameObject池详情 ===");
            foreach (var kvp in _gameObjectPools)
            {
                stats.AppendLine($"[{kvp.Key}] {kvp.Value.GetStatsString()}");
            }

            return stats.ToString();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            Log("对象池管理器初始化");

            if (_autoRegisterPoolsInChildren)
            {
                AutoRegisterChildPools();
            }
        }

        /// <summary>
        /// 自动注册子对象中的对象池
        /// </summary>
        private void AutoRegisterChildPools()
        {
            var childPools = GetComponentsInChildren<GameObjectPool>();
            foreach (var pool in childPools)
            {
                RegisterPool(pool);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            _totalPoolCount = _gameObjectPools.Count + _typedPools.Count;
            _totalObjectCount = 0;
            _totalActiveCount = 0;

            foreach (var pool in _gameObjectPools.Values)
            {
                _totalObjectCount += pool.TotalCount;
                _totalActiveCount += pool.ActiveCount;
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        private void Log(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[PoolManager] {message}");
            }
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.LogWarning($"[PoolManager] {message}");
            }
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[PoolManager] {message}");
        }
        #endregion

        #region 编辑器支持
#if UNITY_EDITOR
        [System.Serializable]
        public class ReadOnlyAttribute : PropertyAttribute { }

        [UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
        {
            public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
            {
                GUI.enabled = false;
                UnityEditor.EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }
        }

        [UnityEditor.MenuItem("Tools/Pool Manager/Show Stats")]
        private static void ShowStats()
        {
            if (Application.isPlaying && Instance != null)
            {
                Debug.Log(Instance.GetAllStatsString());
            }
            else
            {
                Debug.LogWarning("对象池管理器仅在运行时可用");
            }
        }

        [UnityEditor.MenuItem("Tools/Pool Manager/Clear All Pools")]
        private static void ClearAllPools()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.ClearAll();
            }
            else
            {
                Debug.LogWarning("对象池管理器仅在运行时可用");
            }
        }
#endif
        #endregion
    }
} 
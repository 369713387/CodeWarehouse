//using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YiFun;
using YiFun.Pool;

namespace YiFun.Pool.Examples
{
    /// <summary>
    /// 对象池测试和使用示例
    /// </summary>
    public class PoolTest : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private GameObject _testPrefab;
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private int _spawnCount = 10;
        [SerializeField] private float _spawnInterval = 0.5f;
        [SerializeField] private string _poolName = "TestPool";

        [Header("测试控制")]
        [SerializeField] private bool _autoTest = true;
        [SerializeField] private bool _usePoolManager = true;

        private GameObjectPool _gameObjectPool;
        private ObjectPool<TestObject> _typedPool;
        private float _lastSpawnTime;

        #region Unity生命周期
        private void Start()
        {
            InitializePools();
            
            if (_autoTest)
            {
                StartTesting();
            }
        }

        private void Update()
        {
            if (_autoTest && Time.time - _lastSpawnTime >= _spawnInterval)
            {
                SpawnTestObject();
                _lastSpawnTime = Time.time;
            }
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializePools()
        {
            if (_usePoolManager)
            {
                InitializeWithPoolManager();
            }
            else
            {
                InitializeDirectly();
            }

            InitializeTypedPool();
        }

        /// <summary>
        /// 使用PoolManager初始化
        /// </summary>
        private void InitializeWithPoolManager()
        {
            var poolManager = PoolManager.Instance;
            
            // 创建GameObject对象池
            var poolGO = new GameObject($"Pool_{_poolName}");
            poolGO.transform.SetParent(transform);
            
            _gameObjectPool = poolGO.AddComponent<GameObjectPool>();
            
            // 通过反射设置私有字段（仅用于测试）
            var poolNameField = typeof(GameObjectPool).GetField("_poolName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var prefabField = typeof(GameObjectPool).GetField("_prefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            poolNameField?.SetValue(_gameObjectPool, _poolName);
            prefabField?.SetValue(_gameObjectPool, _testPrefab);
            
            _gameObjectPool.InitializePool();
            poolManager.RegisterPool(_gameObjectPool);
        }

        /// <summary>
        /// 直接初始化
        /// </summary>
        private void InitializeDirectly()
        {
            var poolGO = new GameObject($"Pool_{_poolName}");
            poolGO.transform.SetParent(transform);
            
            _gameObjectPool = poolGO.AddComponent<GameObjectPool>();
            
            // 同样需要通过反射设置
            var poolNameField = typeof(GameObjectPool).GetField("_poolName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var prefabField = typeof(GameObjectPool).GetField("_prefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            poolNameField?.SetValue(_gameObjectPool, _poolName);
            prefabField?.SetValue(_gameObjectPool, _testPrefab);
            
            _gameObjectPool.InitializePool();
        }

        /// <summary>
        /// 初始化类型化对象池
        /// </summary>
        private void InitializeTypedPool()
        {
            _typedPool = new ObjectPool<TestObject>(
                createFunc: () => new TestObject("TypedObject"),
                onGet: obj => Debug.Log($"获取类型化对象: {obj.Name}"),
                onRelease: obj => Debug.Log($"释放类型化对象: {obj.Name}"),
                preloadCount: 5
            );
        }
        #endregion

        #region 测试方法
        /// <summary>
        /// 开始测试
        /// </summary>
        private void StartTesting()
        {
            Debug.Log("开始对象池测试...");
            _lastSpawnTime = Time.time;
        }

        /// <summary>
        /// 生成测试对象
        /// </summary>
        private void SpawnTestObject()
        {
            if (_gameObjectPool == null) return;

            GameObject obj;
            
            if (_usePoolManager)
            {
                obj = PoolManager.Instance.Get(_poolName, _spawnParent);
            }
            else
            {
                obj = _gameObjectPool.Get(_spawnParent);
            }

            if (obj != null)
            {
                // 设置随机位置
                obj.transform.position = _spawnParent.position + Random.insideUnitSphere * 5f;
                
                // 如果对象有PoolableComponent，设置自动回收
                var poolable = obj.GetComponent<SimplePoolableComponent>();
                if (poolable != null)
                {
                    poolable.ReturnToPoolAfter(3f);
                }
                else
                {
                    // 否则手动延迟回收
                    StartCoroutine(DelayedReturnCoroutine(obj, 3f));
                }
            }
        }

        /// <summary>
        /// 延迟回收协程
        /// </summary>
        private System.Collections.IEnumerator DelayedReturnCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (obj != null)
            {
                if (_usePoolManager)
                {
                    PoolManager.Instance.Release(obj);
                }
                else
                {
                    _gameObjectPool.Release(obj);
                }
            }
        }

        /// <summary>
        /// 测试类型化对象池
        /// </summary>
        private void TestTypedPool()
        {
            var obj1 = _typedPool.Get();
            var obj2 = _typedPool.Get();
            
            Debug.Log($"从类型化池获取两个对象: {obj1.Name}, {obj2.Name}");
            
            _typedPool.Release(obj1);
            _typedPool.Release(obj2);
            
            Debug.Log("已释放两个对象到类型化池");
        }
        #endregion

        #region 按钮测试方法（用于Inspector）
        [ContextMenu("生成单个对象")]
        public void SpawnSingleObject()
        {
            SpawnTestObject();
        }

        [ContextMenu("生成多个对象")]
        public void SpawnMultipleObjects()
        {
            for (int i = 0; i < _spawnCount; i++)
            {
                SpawnTestObject();
            }
        }

        [ContextMenu("测试类型化对象池")]
        public void TestTypedPoolButton()
        {
            TestTypedPool();
        }

        [ContextMenu("显示统计信息")]
        public void ShowStats()
        {
            if (_usePoolManager && PoolManager.Instance != null)
            {
                Debug.Log(PoolManager.Instance.GetAllStatsString());
            }
            else if (_gameObjectPool != null)
            {
                Debug.Log(_gameObjectPool.GetStatsString());
            }

            if (_typedPool != null)
            {
                Debug.Log(_typedPool.GetStatsString());
            }
        }

        [ContextMenu("清空所有池")]
        public void ClearAllPools()
        {
            if (_usePoolManager && PoolManager.Instance != null)
            {
                PoolManager.Instance.ClearAll();
            }
            else if (_gameObjectPool != null)
            {
                _gameObjectPool.Clear();
            }

            _typedPool?.Clear();
            
            Debug.Log("已清空所有对象池");
        }
        #endregion
    }

    /// <summary>
    /// 测试用的简单类
    /// </summary>
    public class TestObject
    {
        public string Name { get; private set; }
        public float CreatedTime { get; private set; }

        public TestObject(string name)
        {
            Name = name;
            CreatedTime = Time.time;
        }

        public override string ToString()
        {
            return $"TestObject({Name}, Created: {CreatedTime:F2})";
        }
    }
}

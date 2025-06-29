using System;
using System.Collections.Generic;
using UnityEngine;

namespace YiFun.Pool
{
    /// <summary>
    /// GameObject对象池
    /// </summary>
    public class GameObjectPool : MonoBehaviour
    {
        [Header("池配置")]
        [SerializeField] private string _poolName = "GameObjectPool";
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _preloadCount = 5;
        [SerializeField] private int _maxSize = 50;
        [SerializeField] private bool _autoExpand = true;
        [SerializeField] private Transform _poolRoot;

        [Header("调试信息")]
        [SerializeField, ReadOnly] private int _totalCount;
        [SerializeField, ReadOnly] private int _activeCount;
        [SerializeField, ReadOnly] private int _inactiveCount;

        private ObjectPool<GameObject> _pool;
        private readonly Dictionary<GameObject, IGameObjectPoolable> _poolableComponents = new Dictionary<GameObject, IGameObjectPoolable>();

        #region 公开属性
        /// <summary>
        /// 预制体引用
        /// </summary>
        public GameObject Prefab => _prefab;

        /// <summary>
        /// 池名称
        /// </summary>
        public string PoolName => _poolName;

        /// <summary>
        /// 总对象数量
        /// </summary>
        public int TotalCount => _pool?.CountAll ?? 0;

        /// <summary>
        /// 活跃对象数量
        /// </summary>
        public int ActiveCount => _pool?.CountActive ?? 0;

        /// <summary>
        /// 非活跃对象数量
        /// </summary>
        public int InactiveCount => _pool?.CountInactive ?? 0;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializePool();
        }

        private void Update()
        {
            // 更新调试信息
            _totalCount = TotalCount;
            _activeCount = ActiveCount;
            _inactiveCount = InactiveCount;
        }

        private void OnDestroy()
        {
            _pool?.Clear();
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 初始化对象池
        /// </summary>
        public void InitializePool()
        {
            if (_pool != null) return;

            if (_prefab == null)
            {
                Debug.LogError($"GameObjectPool [{_poolName}]: 预制体未设置");
                return;
            }

            // 设置池根节点
            if (_poolRoot == null)
            {
                var poolRootGO = new GameObject($"Pool_Root_{_poolName}");
                poolRootGO.transform.SetParent(transform);
                _poolRoot = poolRootGO.transform;
            }

            // 创建对象池
            _pool = new ObjectPool<GameObject>(
                createFunc: CreateGameObject,
                onGet: OnGetFromPool,
                onRelease: OnReleaseToPool,
                onDestroy: OnDestroyGameObject,
                maxSize: _autoExpand ? -1 : _maxSize,
                preloadCount: _preloadCount
            );

            Debug.Log($"GameObjectPool [{_poolName}] 初始化完成，预加载 {_preloadCount} 个对象");
        }

        /// <summary>
        /// 从池中获取GameObject
        /// </summary>
        /// <param name="parent">父对象Transform</param>
        /// <param name="worldPositionStays">是否保持世界坐标</param>
        /// <returns>GameObject实例</returns>
        public GameObject Get(Transform parent = null, bool worldPositionStays = false)
        {
            if (_pool == null)
            {
                Debug.LogError($"GameObjectPool [{_poolName}]: 对象池未初始化");
                return null;
            }

            var obj = _pool.Get();
            
            if (parent != null)
            {
                obj.transform.SetParent(parent, worldPositionStays);
            }

            return obj;
        }

        /// <summary>
        /// 从池中获取GameObject并返回指定组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="parent">父对象Transform</param>
        /// <param name="worldPositionStays">是否保持世界坐标</param>
        /// <returns>组件实例</returns>
        public T Get<T>(Transform parent = null, bool worldPositionStays = false) where T : Component
        {
            var obj = Get(parent, worldPositionStays);
            return obj?.GetComponent<T>();
        }

        /// <summary>
        /// 将GameObject回收到池中
        /// </summary>
        /// <param name="obj">要回收的GameObject</param>
        /// <returns>是否成功回收</returns>
        public bool Release(GameObject obj)
        {
            if (_pool == null)
            {
                Debug.LogError($"GameObjectPool [{_poolName}]: 对象池未初始化");
                return false;
            }

            return _pool.Release(obj);
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        /// <param name="count">预热数量</param>
        public void Warmup(int count)
        {
            _pool?.Warmup(count);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            _pool?.Clear();
            _poolableComponents.Clear();
        }

        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetStatsString()
        {
            return _pool?.GetStatsString() ?? "Pool not initialized";
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 创建GameObject
        /// </summary>
        /// <returns>新的GameObject实例</returns>
        private GameObject CreateGameObject()
        {
            var obj = Instantiate(_prefab, _poolRoot);
            obj.name = $"{_prefab.name}_{_poolableComponents.Count}";

            // 查找IGameObjectPoolable组件
            var poolableComponent = obj.GetComponent<IGameObjectPoolable>();
            if (poolableComponent != null)
            {
                _poolableComponents[obj] = poolableComponent;
            }

            return obj;
        }

        /// <summary>
        /// 从池中获取对象时的回调
        /// </summary>
        /// <param name="obj">对象实例</param>
        private void OnGetFromPool(GameObject obj)
        {
            obj.SetActive(true);
            
            if (_poolableComponents.TryGetValue(obj, out var poolable))
            {
                poolable.IsInUse = true;
                poolable.OnSpawn();
            }
        }

        /// <summary>
        /// 对象回收到池中时的回调
        /// </summary>
        /// <param name="obj">对象实例</param>
        private void OnReleaseToPool(GameObject obj)
        {
            if (_poolableComponents.TryGetValue(obj, out var poolable))
            {
                poolable.IsInUse = false;
                poolable.OnRecycle();
            }

            obj.transform.SetParent(_poolRoot);
            obj.SetActive(false);
        }

        /// <summary>
        /// 销毁对象时的回调
        /// </summary>
        /// <param name="obj">对象实例</param>
        private void OnDestroyGameObject(GameObject obj)
        {
            _poolableComponents.Remove(obj);
            
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
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
#endif
        #endregion
    }
} 
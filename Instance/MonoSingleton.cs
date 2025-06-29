using UnityEngine;

namespace Framework.Instance
{
    /// <summary>
    /// Unity MonoBehaviour 单例模式基类，使用 Double Check Locking 双检查锁机制确保线程安全
    /// </summary>
    /// <typeparam name="T">继承此类的具体 MonoBehaviour 类型</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // 使用 volatile 关键字确保多线程环境下的内存可见性
        private static volatile T _instance;
        
        // 锁对象，用于同步访问
        private static readonly object _lock = new object();
        
        // 标记应用程序是否正在退出
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                // 如果应用程序正在退出，返回 null
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                // 第一次检查：如果实例已存在，直接返回，避免进入锁提高性能
                if (_instance == null)
                {
                    // 进入临界区
                    lock (_lock)
                    {
                        // 第二次检查：防止多线程情况下重复创建实例
                        if (_instance == null)
                        {
                            // 尝试在场景中查找已存在的实例
                            _instance = FindObjectOfType<T>();

                            if (_instance == null)
                            {
                                // 创建新的 GameObject 并添加组件
                                GameObject singletonObject = new GameObject();
                                _instance = singletonObject.AddComponent<T>();
                                singletonObject.name = typeof(T).Name + " (Singleton)";

                                // 标记为不销毁对象，在场景切换时保持存在
                                DontDestroyOnLoad(singletonObject);

                                Debug.Log($"[MonoSingleton] An instance of {typeof(T)} is created with DontDestroyOnLoad.");
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 检查实例是否已创建
        /// </summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// 手动销毁单例实例
        /// </summary>
        public static void DestroyInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    if (_instance.gameObject != null)
                    {
                        DestroyImmediate(_instance.gameObject);
                    }
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// Unity Awake 生命周期方法
        /// </summary>
        protected virtual void Awake()
        {
            // 确保只有一个实例存在
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                // 如果已经存在实例且不是当前对象，销毁当前对象
                Debug.LogWarning($"[MonoSingleton] Another instance of {typeof(T)} is being created. Destroying the new one.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity OnDestroy 生命周期方法
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                Cleanup();
                _instance = null;
            }
        }

        /// <summary>
        /// Unity OnApplicationQuit 生命周期方法
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// 虚方法，供子类重写进行初始化操作
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// 虚方法，供子类重写进行清理操作
        /// </summary>
        protected virtual void Cleanup()
        {
            
        }
    }
} 
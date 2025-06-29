using System;

namespace Framework.Instance
{
    /// <summary>
    /// 通用单例模式基类，使用 Double Check Locking 双检查锁机制确保线程安全
    /// </summary>
    /// <typeparam name="T">继承此类的具体类型</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        // 使用 volatile 关键字确保多线程环境下的内存可见性
        private static volatile T _instance;
        
        // 锁对象，用于同步访问
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                // 第一次检查：如果实例已存在，直接返回，避免进入锁提高性能
                if (_instance == null)
                {
                    // 进入临界区
                    lock (_lock)
                    {
                        // 第二次检查：防止多线程情况下重复创建实例
                        if (_instance == null)
                        {
                            _instance = new T();
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
        /// 销毁单例实例
        /// </summary>
        public static void DestroyInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    // 如果实例实现了 IDisposable 接口，调用 Dispose 方法
                    if (_instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _instance = null;
                }
            }
        }

        /// <summary>
        /// 构造函数，防止外部直接实例化
        /// </summary>
        protected Singleton()
        {
            // 防止通过反射创建多个实例
            if (_instance != null)
            {
                throw new InvalidOperationException($"An instance of {typeof(T).Name} already exists. Use {typeof(T).Name}.Instance to access it.");
            }
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
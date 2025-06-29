using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Pool
{
    /// <summary>
    /// 通用对象池基类
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> where T : class
    {
        #region 私有字段
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly HashSet<T> _activeObjects = new HashSet<T>();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly int _maxSize;
        #endregion

        #region 公开属性
        /// <summary>
        /// 池中对象总数
        /// </summary>
        public int CountAll => _pool.Count + _activeObjects.Count;

        /// <summary>
        /// 活跃对象数量
        /// </summary>
        public int CountActive => _activeObjects.Count;

        /// <summary>
        /// 池中可用对象数量
        /// </summary>
        public int CountInactive => _pool.Count;

        /// <summary>
        /// 最大池大小
        /// </summary>
        public int MaxSize => _maxSize;
        #endregion

        #region 构造函数
        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="createFunc">创建对象的函数</param>
        /// <param name="onGet">获取对象时的回调</param>
        /// <param name="onRelease">释放对象时的回调</param>
        /// <param name="onDestroy">销毁对象时的回调</param>
        /// <param name="maxSize">最大池大小，-1表示无限制</param>
        /// <param name="preloadCount">预加载数量</param>
        public ObjectPool(
            Func<T> createFunc,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int maxSize = -1,
            int preloadCount = 0)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxSize = maxSize;

            // 预加载对象
            for (int i = 0; i < preloadCount; i++)
            {
                var obj = _createFunc();
                _onRelease?.Invoke(obj);
                _pool.Push(obj);
            }
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = _createFunc();
            }

            _activeObjects.Add(obj);
            _onGet?.Invoke(obj);
            
            return obj;
        }

        /// <summary>
        /// 将对象回收到池中
        /// </summary>
        /// <param name="obj">要回收的对象</param>
        /// <returns>是否成功回收</returns>
        public bool Release(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("ObjectPool: 尝试释放null对象");
                return false;
            }

            if (!_activeObjects.Remove(obj))
            {
                Debug.LogWarning("ObjectPool: 尝试释放未被池管理的对象或已释放的对象");
                return false;
            }

            // 检查池大小限制
            if (_maxSize > 0 && _pool.Count >= _maxSize)
            {
                _onDestroy?.Invoke(obj);
                return true;
            }

            _onRelease?.Invoke(obj);
            _pool.Push(obj);
            return true;
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            // 销毁池中的对象
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                _onDestroy?.Invoke(obj);
            }

            // 警告活跃对象
            if (_activeObjects.Count > 0)
            {
                Debug.LogWarning($"ObjectPool: 清空时仍有 {_activeObjects.Count} 个活跃对象未回收");
                foreach (var obj in _activeObjects)
                {
                    _onDestroy?.Invoke(obj);
                }
                _activeObjects.Clear();
            }
        }

        /// <summary>
        /// 预热对象池（创建指定数量的对象）
        /// </summary>
        /// <param name="count">预热数量</param>
        public void Warmup(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = _createFunc();
                _onRelease?.Invoke(obj);
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatsString()
        {
            return $"ObjectPool<{typeof(T).Name}>: Total={CountAll}, Active={CountActive}, Inactive={CountInactive}, MaxSize={(_maxSize > 0 ? _maxSize.ToString() : "Unlimited")}";
        }
        #endregion
    }
}

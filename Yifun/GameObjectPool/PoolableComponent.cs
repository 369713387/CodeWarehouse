using UnityEngine;

namespace YiFun.Pool
{
    /// <summary>
    /// 可池化组件基类
    /// </summary>
    public abstract class PoolableComponent : MonoBehaviour, IGameObjectPoolable
    {
        [Header("池化设置")]
        [SerializeField] private bool _debugMode = false;

        private bool _isInUse = false;

        #region IGameObjectPoolable实现
        /// <summary>
        /// GameObject引用
        /// </summary>
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Transform引用
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// 是否正在使用中
        /// </summary>
        public bool IsInUse 
        { 
            get => _isInUse; 
            set => _isInUse = value; 
        }

        /// <summary>
        /// 从对象池中获取对象时调用
        /// </summary>
        public virtual void OnSpawn()
        {
            if (_debugMode)
                Debug.Log($"[{gameObject.name}] OnSpawn called");

            // 重置对象状态
            ResetState();
            
            // 调用子类实现
            OnSpawnImplementation();
        }

        /// <summary>
        /// 对象回收到池中时调用
        /// </summary>
        public virtual void OnRecycle()
        {
            if (_debugMode)
                Debug.Log($"[{gameObject.name}] OnRecycle called");

            // 调用子类实现
            OnRecycleImplementation();
            
            // 清理对象状态
            CleanupState();
        }
        #endregion

        #region 抽象方法 - 子类必须实现
        /// <summary>
        /// 对象获取时的具体实现（子类重写）
        /// </summary>
        protected abstract void OnSpawnImplementation();

        /// <summary>
        /// 对象回收时的具体实现（子类重写）
        /// </summary>
        protected abstract void OnRecycleImplementation();
        #endregion

        #region 虚方法 - 子类可重写
        /// <summary>
        /// 重置对象状态（子类可重写）
        /// </summary>
        protected virtual void ResetState()
        {
            // 重置Transform
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // 重置Rigidbody（如果存在）
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 重置Rigidbody2D（如果存在）
            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.velocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
        }

        /// <summary>
        /// 清理对象状态（子类可重写）
        /// </summary>
        protected virtual void CleanupState()
        {
            // 停止所有协程
            StopAllCoroutines();

            // 取消所有Invoke调用
            CancelInvoke();
        }
        #endregion

        #region 便捷方法
        /// <summary>
        /// 将自己回收到对象池
        /// </summary>
        public virtual void ReturnToPool()
        {
            var poolManager = PoolManager.Instance;
            if (poolManager != null)
            {
                poolManager.Release(gameObject);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 无法找到PoolManager，对象将被销毁");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 延迟回收到对象池
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        public virtual void ReturnToPoolAfter(float delay)
        {
            Invoke(nameof(ReturnToPool), delay);
        }
        #endregion

        #region Unity生命周期
        /// <summary>
        /// 当对象被启用时调用
        /// </summary>
        protected virtual void OnEnable()
        {
            // 子类可重写
        }

        /// <summary>
        /// 当对象被禁用时调用
        /// </summary>
        protected virtual void OnDisable()
        {
            // 子类可重写
        }
        #endregion
    }

    /// <summary>
    /// 简单的可池化组件实现（用于测试或简单用例）
    /// </summary>
    public class SimplePoolableComponent : PoolableComponent
    {
        [Header("简单池化组件")]
        [SerializeField] private float _autoReturnDelay = -1f; // -1表示不自动回收

        protected override void OnSpawnImplementation()
        {
            // 如果设置了自动回收时间，则延迟回收
            if (_autoReturnDelay > 0)
            {
                ReturnToPoolAfter(_autoReturnDelay);
            }
        }

        protected override void OnRecycleImplementation()
        {
            // 简单实现，不需要特殊处理
        }
    }
} 
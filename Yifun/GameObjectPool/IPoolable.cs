using UnityEngine;

namespace YiFun.Pool
{
    /// <summary>
    /// 对象池可回收对象接口
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从对象池中获取对象时调用
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 对象回收到池中时调用
        /// </summary>
        void OnRecycle();

        /// <summary>
        /// 对象是否正在使用中
        /// </summary>
        bool IsInUse { get; set; }
    }

    /// <summary>
    /// GameObject对象池可回收对象接口
    /// </summary>
    public interface IGameObjectPoolable : IPoolable
    {
        /// <summary>
        /// 获取GameObject引用
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// 获取Transform引用
        /// </summary>
        Transform Transform { get; }
    }
} 
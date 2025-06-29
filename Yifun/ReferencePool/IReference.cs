using System;

namespace FSMFrame
{
    /// <summary>
    /// 引用接口。
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// 清理引用。
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 支持自定义初始化的引用接口
    /// </summary>
    /// <typeparam name="T">初始化参数类型</typeparam>
    public interface IReference<in T> : IReference
    {
        /// <summary>
        /// 初始化引用
        /// </summary>
        /// <param name="param">初始化参数</param>
        void Initialize(T param);
    }

    /// <summary>
    /// 可验证的引用接口
    /// </summary>
    public interface IValidatable : IReference
    {
        /// <summary>
        /// 验证引用是否有效
        /// </summary>
        bool IsValid { get; }
    }
}

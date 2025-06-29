using System;
using System.Runtime.CompilerServices;

namespace FSMFrame
{
    /// <summary>
    /// 引用池扩展方法
    /// </summary>
    public static class ReferencePoolExtensions
    {
        /// <summary>
        /// 获取并初始化引用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Acquire<T, TParam>(TParam param) where T : class, IReference<TParam>, new()
        {
            var reference = ReferencePool.Acquire<T>();
            reference.Initialize(param);
            return reference;
        }

        /// <summary>
        /// 安全释放引用（自动null检查）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SafeRelease<T>(ref T reference) where T : class, IReference
        {
            if (reference != null)
            {
                ReferencePool.Release(reference);
                reference = null;
            }
        }

        /// <summary>
        /// 预热引用池
        /// </summary>
        public static void Warmup<T>(int count) where T : class, IReference, new()
        {
            ReferencePool.Add<T>(count);
        }

        /// <summary>
        /// 批量获取引用
        /// </summary>
        public static T[] AcquireBatch<T>(int count) where T : class, IReference, new()
        {
            var references = new T[count];
            for (int i = 0; i < count; i++)
            {
                references[i] = ReferencePool.Acquire<T>();
            }
            return references;
        }

        /// <summary>
        /// 批量释放引用
        /// </summary>
        public static void ReleaseBatch<T>(T[] references) where T : class, IReference
        {
            if (references == null) return;
            
            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] != null)
                {
                    ReferencePool.Release(references[i]);
                    references[i] = null;
                }
            }
        }
    }
} 
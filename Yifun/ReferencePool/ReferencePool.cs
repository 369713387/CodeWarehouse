﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Framework.Pool
{
    /// <summary>
    /// 引用池。
    /// </summary>
    public static partial class ReferencePool
    {
        // 使用 ConcurrentDictionary 替代 Dictionary + lock
        private static readonly ConcurrentDictionary<Type, ReferenceCollection> s_ReferenceCollections = new ConcurrentDictionary<Type, ReferenceCollection>();
        
        // 缓存类型的工厂方法
        private static readonly ConcurrentDictionary<Type, Func<IReference>> s_TypeFactories = new ConcurrentDictionary<Type, Func<IReference>>();
        
        private static bool m_EnableStrictCheck = false;

        /// <summary>
        /// 获取或设置是否开启强制检查。
        /// </summary>
        public static bool EnableStrictCheck
        {
            get
            {
                return m_EnableStrictCheck;
            }
            set
            {
                m_EnableStrictCheck = value;
            }
        }

        /// <summary>
        /// 获取引用池的数量。
        /// </summary>
        public static int Count
        {
            get
            {
                return s_ReferenceCollections.Count;
            }
        }

        /// <summary>
        /// 获取所有引用池的信息。
        /// </summary>
        /// <returns>所有引用池的信息。</returns>
        public static ReferencePoolInfo[] GetAllReferencePoolInfos()
        {
            var collections = s_ReferenceCollections.ToArray();
            var results = new ReferencePoolInfo[collections.Length];
            
            for (int i = 0; i < collections.Length; i++)
            {
                var kvp = collections[i];
                results[i] = new ReferencePoolInfo(
                    kvp.Key, 
                    kvp.Value.UnusedReferenceCount, 
                    kvp.Value.UsingReferenceCount, 
                    kvp.Value.AcquireReferenceCount, 
                    kvp.Value.ReleaseReferenceCount, 
                    kvp.Value.AddReferenceCount, 
                    kvp.Value.RemoveReferenceCount);
            }

            return results;
        }

        /// <summary>
        /// 清除所有引用池。
        /// </summary>
        public static void ClearAll()
        {
            foreach (var referenceCollection in s_ReferenceCollections.Values)
            {
                referenceCollection.RemoveAll();
            }
            s_ReferenceCollections.Clear();
            s_TypeFactories.Clear();
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <returns>引用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Acquire<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Acquire<T>();
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        /// <returns>引用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReference Acquire(Type referenceType)
        {
            InternalCheckReferenceType(referenceType);
            return GetReferenceCollection(referenceType).Acquire();
        }

        /// <summary>
        /// 将引用归还引用池。
        /// </summary>
        /// <param name="reference">引用。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Release(IReference reference)
        {
            if (reference == null)
            {
                //Log.Error("Reference is invalid.");
                return;
            }

            Type referenceType = reference.GetType();
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).Release(reference);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">追加数量。</param>
        public static void Add<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        /// <param name="count">追加数量。</param>
        public static void Add(Type referenceType, int count)
        {
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).Add(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量。</param>
        public static void Remove<T>(int count) where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        /// <param name="count">移除数量。</param>
        public static void Remove(Type referenceType, int count)
        {
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除所有的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public static void RemoveAll<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 从引用池中移除所有的引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        public static void RemoveAll(Type referenceType)
        {
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).RemoveAll();
        }

        private static void InternalCheckReferenceType(Type referenceType)
        {
            if (!m_EnableStrictCheck)
            {
                return;
            }

            if (referenceType == null)
            {
                //Log.Error("Reference type is invalid.");
            }

            if (!referenceType.IsClass || referenceType.IsAbstract)
            {
                //Log.Error("Reference type is not a non-abstract class type.");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                //Log.Error($"Reference type '{referenceType.FullName}' is invalid.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReferenceCollection GetReferenceCollection(Type referenceType)
        {
            return s_ReferenceCollections.GetOrAdd(referenceType, type => new ReferenceCollection(type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<IReference> GetOrCreateFactory(Type referenceType)
        {
            return s_TypeFactories.GetOrAdd(referenceType, type => 
                () => (IReference)Activator.CreateInstance(type));
        }
    }
}

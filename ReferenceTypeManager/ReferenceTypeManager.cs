 using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Framework.ZeroGCEventBus
{
    /// <summary>
    /// 引用类型管理器
    /// 通过将引用类型映射为ID，在事件中只传递ID，从而避免GC分配
    /// </summary>
    public static class ReferenceTypeManager
    {
        #region 核心数据结构
        
        // 使用ConcurrentDictionary确保线程安全
        private static readonly ConcurrentDictionary<int, WeakReference> _references = new ConcurrentDictionary<int, WeakReference>();
        private static readonly ConcurrentDictionary<object, int> _reverseMap = new ConcurrentDictionary<object, int>();
        private static volatile int _nextId = 1;
        
        // 性能统计
        private static volatile int _totalReferences = 0;
        private static volatile int _deadReferences = 0;
        private static volatile int _cleanupCount = 0;
        
        #endregion

        #region 公共API

        /// <summary>
        /// 获取或创建引用类型的ID
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="obj">要获取ID的对象</param>
        /// <returns>对象的唯一ID，null对象返回0</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOrCreateId<T>(T obj) where T : class
        {
            if (obj == null) return 0;
            
            // 先尝试获取已存在的ID
            if (_reverseMap.TryGetValue(obj, out int existingId))
            {
                return existingId;
            }
            
            // 创建新的ID
            int newId = System.Threading.Interlocked.Increment(ref _nextId);
            
            // 双重检查锁定模式避免重复创建
            if (_reverseMap.TryAdd(obj, newId))
            {
                _references.TryAdd(newId, new WeakReference(obj));
                System.Threading.Interlocked.Increment(ref _totalReferences);
                return newId;
            }
            
            // 如果添加失败，说明其他线程已经添加了，返回已存在的ID
            return _reverseMap.TryGetValue(obj, out int concurrentId) ? concurrentId : 0;
        }

        /// <summary>
        /// 通过ID获取引用类型对象
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="id">对象ID</param>
        /// <returns>引用类型对象，如果对象已被GC回收则返回null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetReference<T>(int id) where T : class
        {
            if (id == 0) return null;
            
            if (_references.TryGetValue(id, out WeakReference weakRef))
            {
                var target = weakRef.Target;
                if (target != null)
                {
                    return target as T;
                }
                
                // 对象已被回收，标记为死亡引用
                System.Threading.Interlocked.Increment(ref _deadReferences);
            }
            
            return null;
        }

        /// <summary>
        /// 批量获取引用类型对象
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="ids">ID数组</param>
        /// <param name="results">结果数组，长度必须与ids相同</param>
        /// <returns>成功获取的对象数量</returns>
        public static int GetReferences<T>(int[] ids, T[] results) where T : class
        {
            if (ids == null || results == null || ids.Length != results.Length)
                return 0;
            
            int successCount = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                results[i] = GetReference<T>(ids[i]);
                if (results[i] != null)
                    successCount++;
            }
            
            return successCount;
        }

        /// <summary>
        /// 检查ID是否有效（对应的对象是否还存活）
        /// </summary>
        /// <param name="id">对象ID</param>
        /// <returns>true表示对象仍然存活</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(int id)
        {
            if (id == 0) return false;
            
            return _references.TryGetValue(id, out WeakReference weakRef) && weakRef.IsAlive;
        }

        /// <summary>
        /// 强制移除指定ID的引用
        /// </summary>
        /// <param name="id">要移除的ID</param>
        /// <returns>true表示成功移除</returns>
        public static bool RemoveReference(int id)
        {
            if (id == 0) return false;
            
            bool removed = false;
            
            // 移除正向映射
            if (_references.TryRemove(id, out WeakReference weakRef))
            {
                removed = true;
                
                // 移除反向映射
                if (weakRef.IsAlive && weakRef.Target != null)
                {
                    _reverseMap.TryRemove(weakRef.Target, out _);
                }
            }
            
            if (removed)
            {
                System.Threading.Interlocked.Decrement(ref _totalReferences);
            }
            
            return removed;
        }

        #endregion

        #region 内存管理

        /// <summary>
        /// 清理已死亡的引用
        /// </summary>
        /// <param name="forceFullCleanup">是否强制进行完整清理</param>
        /// <returns>清理的引用数量</returns>
        public static int CleanupDeadReferences(bool forceFullCleanup = false)
        {
            // 简单的启发式：如果死亡引用数量超过总引用的20%，或者强制清理
            if (!forceFullCleanup && _deadReferences < _totalReferences * 0.2f)
            {
                return 0;
            }
            
            var deadIds = new List<int>();
            var deadObjects = new List<object>();
            
            // 查找死亡的正向引用
            foreach (var kvp in _references)
            {
                if (!kvp.Value.IsAlive)
                {
                    deadIds.Add(kvp.Key);
                }
            }
            
            // 查找死亡的反向引用
            foreach (var kvp in _reverseMap)
            {
                if (!_references.TryGetValue(kvp.Value, out WeakReference weakRef) || !weakRef.IsAlive)
                {
                    deadObjects.Add(kvp.Key);
                }
            }
            
            // 清理死亡引用
            int cleanedCount = 0;
            
            foreach (int id in deadIds)
            {
                if (_references.TryRemove(id, out _))
                {
                    cleanedCount++;
                }
            }
            
            foreach (object obj in deadObjects)
            {
                _reverseMap.TryRemove(obj, out _);
            }
            
            // 更新统计数据
            System.Threading.Interlocked.Add(ref _totalReferences, -cleanedCount);
            System.Threading.Interlocked.Add(ref _deadReferences, -cleanedCount);
            System.Threading.Interlocked.Increment(ref _cleanupCount);
            
            return cleanedCount;
        }

        /// <summary>
        /// 清理所有引用
        /// </summary>
        public static void ClearAll()
        {
            _references.Clear();
            _reverseMap.Clear();
            _totalReferences = 0;
            _deadReferences = 0;
            _nextId = 1;
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 引用类型管理器性能统计
        /// </summary>
        public struct PerformanceStats
        {
            public int TotalReferences;
            public int DeadReferences;
            public int CleanupCount;
            public float DeadReferenceRatio;
            public int NextId;
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计数据</returns>
        public static PerformanceStats GetPerformanceStats()
        {
            int total = _totalReferences;
            int dead = _deadReferences;
            
            return new PerformanceStats
            {
                TotalReferences = total,
                DeadReferences = dead,
                CleanupCount = _cleanupCount,
                DeadReferenceRatio = total > 0 ? (float)dead / total : 0f,
                NextId = _nextId
            };
        }

        /// <summary>
        /// 记录性能统计信息到Unity控制台
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogPerformanceStats()
        {
            var stats = GetPerformanceStats();
            Debug.Log($"[ReferenceTypeManager] 统计信息:\n" +
                     $"总引用数: {stats.TotalReferences}\n" +
                     $"死亡引用数: {stats.DeadReferences}\n" +
                     $"死亡引用比例: {stats.DeadReferenceRatio:P2}\n" +
                     $"清理次数: {stats.CleanupCount}\n" +
                     $"下一个ID: {stats.NextId}");
        }

        #endregion

        #region Unity集成

        /// <summary>
        /// Unity启动时初始化
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 注册应用程序暂停/恢复事件
            Application.focusChanged += OnApplicationFocusChanged;
            
            // 在编辑器中注册域重载事件
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif
        }

        private static void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                // 应用程序获得焦点时清理死亡引用
                CleanupDeadReferences(true);
            }
        }

#if UNITY_EDITOR
        private static void OnBeforeAssemblyReload()
        {
            // 域重载前清理所有引用
            ClearAll();
        }
#endif

        #endregion
    }
}
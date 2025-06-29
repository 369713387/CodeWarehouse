using Framework.ZeroGCEventBus;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Framework.ZeroGCEventBus
{
    /// <summary>
    /// 安全的ZeroGC事件总线控制器 - 提供额外的安全性和便利功能
    /// </summary>
    public static class SafeZeroGCEventBusController
    {
        #region 私有字段
        
        private static readonly ConcurrentDictionary<string, List<IEventSubscriptionInfo>> _subscriptionGroups
            = new ConcurrentDictionary<string, List<IEventSubscriptionInfo>>();
        
        private static readonly object _groupLock = new object();
        
        #endregion

        #region 基础安全操作

        /// <summary>
        /// 检查事件总线实例是否可用
        /// </summary>
        public static bool IsEventBusAvailable => ZeroGCEventBus.Instance != null;

        /// <summary>
        /// 安全地订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="eventName">事件名称（用于日志）</param>
        /// <returns>事件处理器ID，如果失败返回无效ID</returns>
        public static ZeroGCEventBus.EventHandlerId<T> SafeSubscribe<T>(Action<T> handler, string eventName = null) 
            where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            if (handler == null)
            {
                Debug.LogWarning($"[SafeEventBus] {eventName ?? typeof(T).Name}事件处理器为null，无法订阅");
                return default;
            }

            if (!IsEventBusAvailable)
            {
                Debug.LogWarning($"[SafeEventBus] ZeroGCEventBus.Instance为null，无法订阅{eventName ?? typeof(T).Name}事件");
                return default;
            }

            try
            {
                var handlerId = ZeroGCEventBus.Instance.Subscribe<T>(handler);
                if (handlerId.IsValid)
                {
                    Debug.Log($"[SafeEventBus] {eventName ?? typeof(T).Name}事件订阅成功 (ID: {handlerId.ToInt()})");
                }
                else
                {
                    Debug.LogWarning($"[SafeEventBus] {eventName ?? typeof(T).Name}事件订阅失败：返回无效ID");
                }
                return handlerId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] {eventName ?? typeof(T).Name}事件订阅失败: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 安全地取消事件订阅
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handlerId">事件处理器ID</param>
        /// <param name="eventName">事件名称（用于日志）</param>
        /// <returns>是否成功取消订阅</returns>
        public static bool SafeUnsubscribe<T>(ZeroGCEventBus.EventHandlerId<T> handlerId, string eventName = null) 
            where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            if (!handlerId.IsValid)
            {
                Debug.LogWarning($"[SafeEventBus] {eventName ?? typeof(T).Name}事件ID无效，跳过取消订阅");
                return false;
            }

            if (!IsEventBusAvailable)
            {
                Debug.LogWarning($"[SafeEventBus] ZeroGCEventBus.Instance为null，无法取消订阅{eventName ?? typeof(T).Name}事件");
                return false;
            }
            
            try
            {
                ZeroGCEventBus.Instance.Unsubscribe(handlerId);
                Debug.Log($"[SafeEventBus] {eventName ?? typeof(T).Name}事件取消订阅成功 (ID: {handlerId.ToInt()})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] {eventName ?? typeof(T).Name}事件取消订阅失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全地发布事件（延迟处理）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="eventName">事件名称（用于日志）</param>
        /// <returns>是否成功发布</returns>
        public static bool SafePublish<T>(T eventData, string eventName = null) 
            where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            if (!IsEventBusAvailable)
            {
                Debug.LogWarning($"[SafeEventBus] ZeroGCEventBus.Instance为null，无法发布{eventName ?? typeof(T).Name}事件");
                return false;
            }

            try
            {
                bool success = ZeroGCEventBus.Instance.Publish(eventData);
                if (success)
                {
                    Debug.Log($"[SafeEventBus] {eventName ?? typeof(T).Name}事件发布成功");
                }
                else
                {
                    Debug.LogWarning($"[SafeEventBus] {eventName ?? typeof(T).Name}事件发布失败：缓冲区可能已满");
                }
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] {eventName ?? typeof(T).Name}事件发布失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全地立即发布事件（无缓冲）
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="eventName">事件名称（用于日志）</param>
        /// <returns>是否成功发布</returns>
        public static bool SafePublishImmediate<T>(T eventData, string eventName = null) 
            where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            if (!IsEventBusAvailable)
            {
                Debug.LogWarning($"[SafeEventBus] ZeroGCEventBus.Instance为null，无法立即发布{eventName ?? typeof(T).Name}事件");
                return false;
            }

            try
            {
                ZeroGCEventBus.Instance.PublishImmediate(eventData);
                Debug.Log($"[SafeEventBus] {eventName ?? typeof(T).Name}事件立即发布成功");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] {eventName ?? typeof(T).Name}事件立即发布失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 批量订阅管理

        /// <summary>
        /// 事件订阅信息接口
        /// </summary>
        public interface IEventSubscriptionInfo : IDisposable
        {
            string EventName { get; }
            Type EventType { get; }
            bool IsValid { get; }
            bool Unsubscribe();
        }

        /// <summary>
        /// 事件订阅信息实现
        /// </summary>
        private class EventSubscriptionInfo<T> : IEventSubscriptionInfo 
            where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            private ZeroGCEventBus.EventHandlerId<T> _handlerId;
            private readonly string _eventName;
            private bool _disposed = false;

            public EventSubscriptionInfo(ZeroGCEventBus.EventHandlerId<T> handlerId, string eventName)
            {
                _handlerId = handlerId;
                _eventName = eventName ?? typeof(T).Name;
            }

            public string EventName => _eventName;
            public Type EventType => typeof(T);
            public bool IsValid => !_disposed && _handlerId.IsValid;

            public bool Unsubscribe()
            {
                if (_disposed || !_handlerId.IsValid) return false;
                
                bool success = SafeUnsubscribe(_handlerId, _eventName);
                if (success)
                {
                    _handlerId = default;
                    _disposed = true;
                }
                return success;
            }

            public void Dispose()
            {
                Unsubscribe();
            }
        }

        /// <summary>
        /// 订阅事件并加入指定组
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="groupName">组名</param>
        /// <param name="eventName">事件名称</param>
        /// <returns>订阅信息</returns>
        public static IEventSubscriptionInfo SubscribeToGroup<T>(Action<T> handler, string groupName, string eventName = null)
            where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            var handlerId = SafeSubscribe<T>(handler, eventName);
            if (!handlerId.IsValid) return null;

            var subscriptionInfo = new EventSubscriptionInfo<T>(handlerId, eventName ?? typeof(T).Name);
            
            lock (_groupLock)
            {
                if (!_subscriptionGroups.TryGetValue(groupName, out var group))
                {
                    group = new List<IEventSubscriptionInfo>();
                    _subscriptionGroups[groupName] = group;
                }
                group.Add(subscriptionInfo);
            }

            Debug.Log($"[SafeEventBus] 事件{subscriptionInfo.EventName}已添加到组'{groupName}'");
            return subscriptionInfo;
        }

        /// <summary>
        /// 取消订阅指定组的所有事件
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <returns>成功取消订阅的数量</returns>
        public static int UnsubscribeGroup(string groupName)
        {
            lock (_groupLock)
            {
                if (!_subscriptionGroups.TryGetValue(groupName, out var group))
                {
                    Debug.LogWarning($"[SafeEventBus] 未找到事件组'{groupName}'");
                    return 0;
                }

                int successCount = 0;
                foreach (var subscription in group)
                {
                    if (subscription?.Unsubscribe() == true)
                    {
                        successCount++;
                    }
                }

                _subscriptionGroups.TryRemove(groupName, out _);
                Debug.Log($"[SafeEventBus] 事件组'{groupName}'取消订阅完成，成功取消{successCount}个事件");
                return successCount;
            }
        }

        /// <summary>
        /// 清理指定组中的无效订阅
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <returns>清理的数量</returns>
        public static int CleanupInvalidSubscriptions(string groupName)
        {
            lock (_groupLock)
            {
                if (!_subscriptionGroups.TryGetValue(groupName, out var group))
                {
                    return 0;
                }

                int removedCount = 0;
                for (int i = group.Count - 1; i >= 0; i--)
                {
                    if (group[i] == null || !group[i].IsValid)
                    {
                        group[i]?.Dispose();
                        group.RemoveAt(i);
                        removedCount++;
                    }
                }

                Debug.Log($"[SafeEventBus] 事件组'{groupName}'清理了{removedCount}个无效订阅");
                return removedCount;
            }
        }

        /// <summary>
        /// 清理所有组中的无效订阅
        /// </summary>
        /// <returns>清理的总数量</returns>
        public static int CleanupAllInvalidSubscriptions()
        {
            int totalRemoved = 0;
            var groupNames = new List<string>(_subscriptionGroups.Keys);
            
            foreach (var groupName in groupNames)
            {
                totalRemoved += CleanupInvalidSubscriptions(groupName);
            }
            
            return totalRemoved;
        }

        #endregion

        #region 统计和调试功能

        /// <summary>
        /// 获取事件监听器数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>监听器数量，-1表示获取失败</returns>
        public static int GetListenerCount<T>() where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            if (!IsEventBusAvailable) return -1;
            
            try
            {
                return ZeroGCEventBus.Instance.GetListenerCount<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] 获取{typeof(T).Name}监听器数量失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 清空指定类型事件的缓冲区
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>是否成功</returns>
        public static bool ClearEventBuffer<T>() where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            if (!IsEventBusAvailable) return false;
            
            try
            {
                ZeroGCEventBus.Instance.ClearEventBuffer<T>();
                Debug.Log($"[SafeEventBus] {typeof(T).Name}事件缓冲区已清空");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] 清空{typeof(T).Name}事件缓冲区失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理事件总线中的无效监听器
        /// </summary>
        /// <returns>是否成功</returns>
        public static bool CleanupInvalidListeners()
        {
            if (!IsEventBusAvailable) return false;
            
            try
            {
                ZeroGCEventBus.Instance.CleanupInvalidListeners();
                Debug.Log("[SafeEventBus] 事件总线无效监听器清理完成");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] 清理无效监听器失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取订阅组信息
        /// </summary>
        /// <returns>组信息字典</returns>
        public static Dictionary<string, int> GetSubscriptionGroupsInfo()
        {
            var info = new Dictionary<string, int>();
            
            lock (_groupLock)
            {
                foreach (var kvp in _subscriptionGroups)
                {
                    int validCount = 0;
                    foreach (var subscription in kvp.Value)
                    {
                        if (subscription?.IsValid == true)
                            validCount++;
                    }
                    info[kvp.Key] = validCount;
                }
            }
            
            return info;
        }

        /// <summary>
        /// 打印所有订阅组的统计信息
        /// </summary>
        public static void LogSubscriptionGroupsStats()
        {
            var groupsInfo = GetSubscriptionGroupsInfo();
            
            if (groupsInfo.Count == 0)
            {
                Debug.Log("[SafeEventBus] 没有活跃的订阅组");
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[SafeEventBus] 订阅组统计:");
            
            foreach (var kvp in groupsInfo)
            {
                sb.AppendLine($"  - {kvp.Key}: {kvp.Value} 个有效订阅");
            }
            
            Debug.Log(sb.ToString());
        }

        #endregion

        #region 配置和管理

        /// <summary>
        /// 安全地设置默认缓冲区配置
        /// </summary>
        /// <param name="capacity">缓冲区容量</param>
        /// <param name="strategy">溢出策略</param>
        /// <returns>是否成功设置</returns>
        public static bool SetDefaultBufferConfig(int capacity, ZeroGCEventBus.BufferOverflowStrategy strategy)
        {
            if (!IsEventBusAvailable)
            {
                Debug.LogWarning("[SafeEventBus] 无法设置缓冲区配置：事件总线不可用");
                return false;
            }

            try
            {
                ZeroGCEventBus.Instance.SetDefaultBufferConfig(capacity, strategy);
                Debug.Log($"[SafeEventBus] 缓冲区配置已更新：容量={capacity}, 策略={strategy}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafeEventBus] 设置缓冲区配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行完整的清理操作
        /// </summary>
        /// <returns>清理的项目总数</returns>
        public static int PerformFullCleanup()
        {
            int totalCleaned = 0;
            
            // 清理订阅组中的无效订阅
            totalCleaned += CleanupAllInvalidSubscriptions();
            
            // 清理事件总线中的无效监听器
            if (CleanupInvalidListeners())
            {
                totalCleaned++;
            }
            
            Debug.Log($"[SafeEventBus] 完整清理完成，总共清理了{totalCleaned}个项目");
            return totalCleaned;
        }

        /// <summary>
        /// 销毁所有订阅组（通常在应用退出时调用）
        /// </summary>
        public static void DestroyAllSubscriptionGroups()
        {
            lock (_groupLock)
            {
                foreach (var group in _subscriptionGroups.Values)
                {
                    foreach (var subscription in group)
                    {
                        subscription?.Dispose();
                    }
                }
                _subscriptionGroups.Clear();
                Debug.Log("[SafeEventBus] 所有订阅组已销毁");
            }
        }

        #endregion
    }
}

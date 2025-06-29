using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Framework.ZeroGCEventBus
{
    /// <summary>
    /// 零GC事件总线 - 高性能、线程安全的事件系统
    /// </summary>
    public class ZeroGCEventBus : Framework.Instance.MonoSingleton<ZeroGCEventBus>
    {
        #region 核心数据结构

        /// <summary>
        /// 缓冲区溢出处理策略
        /// </summary>
        public enum BufferOverflowStrategy
        {
            DropNewest,    // 丢弃最新事件（默认）
            DropOldest,    // 丢弃最旧事件
            Resize,        // 自动扩容
            LogWarning     // 记录警告并丢弃
        }

        /// <summary>
        /// 类型安全的事件处理器ID
        /// </summary>
        public readonly struct EventHandlerId<T> : IEquatable<EventHandlerId<T>> 
            where T : struct, IZeroGCEvent
        {
            private readonly int _id;
            internal EventHandlerId(int id) => _id = id;
            
            public bool IsValid => _id > 0;
            public bool Equals(EventHandlerId<T> other) => _id == other._id;
            public override bool Equals(object obj) => obj is EventHandlerId<T> other && Equals(other);
            public override int GetHashCode() => _id;
            public static bool operator ==(EventHandlerId<T> left, EventHandlerId<T> right) => left.Equals(right);
            public static bool operator !=(EventHandlerId<T> left, EventHandlerId<T> right) => !left.Equals(right);
            
            internal int ToInt() => _id;
        }

        /// <summary>
        /// 事件类型缓存 - 编译时生成ID避免运行时类型检查
        /// </summary>
        private static class EventTypeCache<T> where T : struct, IZeroGCEvent
        {
            public static readonly int TypeId = EventTypeRegistry.RegisterType<T>();
        }

        /// <summary>
        /// 事件类型注册器
        /// </summary>
        private static class EventTypeRegistry
        {
            private static int _nextTypeId = 0;
            private static readonly Dictionary<Type, int> _typeToId = new Dictionary<Type, int>();
            private static readonly object _registryLock = new object();

            public static int RegisterType<T>() where T : struct, IZeroGCEvent
            {
                var type = typeof(T);
                lock (_registryLock)
                {
                    if (!_typeToId.TryGetValue(type, out int typeId))
                    {
                        typeId = ++_nextTypeId; // 从1开始，0保留给无效ID
                        _typeToId[type] = typeId;
                    }
                    return typeId;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int GetTypeId<T>() where T : struct, IZeroGCEvent
            {
                return EventTypeCache<T>.TypeId;
            }
        }

        /// <summary>
        /// 事件处理器处理委托缓存
        /// </summary>
        private static class ProcessorCache<T> where T : struct, IZeroGCEvent
        {
            public static readonly Action<ZeroGCEventBus, int> Processor = ProcessEvents;
            
            private static void ProcessEvents(ZeroGCEventBus bus, int typeId)
            {
                if (bus._eventBuffers.TryGetValue(typeId, out var bufferObj) &&
                    bus._listenerManagers.TryGetValue(typeId, out var managerObj))
                {
                    var buffer = (EventBuffer<T>)bufferObj;
                    var manager = (ListenerManager<T>)managerObj;
                    
                    while (buffer.TryDequeue(out T eventData))
                    {
                        manager.NotifyAll(ref eventData);
                        Interlocked.Increment(ref bus._eventsProcessedThisFrame);
                        Interlocked.Increment(ref bus._totalEventsProcessed);
                    }
                }
            }
        }

        /// <summary>
        /// 零GC事件接口
        /// </summary>
        public interface IZeroGCEvent
        {
            
        }

        /// <summary>
        /// 监听器管理器接口
        /// </summary>
        private interface IListenerManager : System.IDisposable
        {
            int ListenerCount { get; }
            void ClearInvalidHandlers();
        }

        /// <summary>
        /// 轻量级事件处理器包装
        /// </summary>
        private readonly struct EventHandlerWrapper<T> where T : struct, IZeroGCEvent
        {
            private readonly Action<T> _handler;
            private readonly int _handlerId;
            private readonly WeakReference _targetRef; // 弱引用防止内存泄漏

            public EventHandlerWrapper(Action<T> handler, int handlerId)
            {
                _handler = handler;
                _handlerId = handlerId;
                _targetRef = handler?.Target != null ? new WeakReference(handler.Target) : null;
            }

            public bool IsValid 
            {
                get
                {
                    if (_handler == null) return false;
                    if (_targetRef == null) return true; // 静态方法
                    return _targetRef.IsAlive; // 检查目标对象是否还存活
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Handle(ref T eventData)
            {
                if (IsValid)
                {
                    _handler.Invoke(eventData);
                }
            }

            public int HandlerId => _handlerId;
        }

        #endregion

        #region 内存管理

        /// <summary>
        /// 高性能事件缓冲区 - 支持动态扩容和多种溢出策略
        /// </summary>
        private class EventBuffer<T> where T : struct, IZeroGCEvent
        {
            private T[] _buffer;
            private volatile int _writeIndex;
            private volatile int _readIndex;
            private int _capacity;
            private readonly BufferOverflowStrategy _overflowStrategy;
            private readonly object _resizeLock = new object();

            public EventBuffer(int capacity = 256, BufferOverflowStrategy overflowStrategy = BufferOverflowStrategy.DropNewest)
            {
                _capacity = capacity;
                _buffer = new T[capacity];
                _writeIndex = 0;
                _readIndex = 0;
                _overflowStrategy = overflowStrategy;
            }

            public bool TryEnqueue(ref T eventData)
            {
                int currentWrite = _writeIndex;
                int nextWriteIndex = (currentWrite + 1) % _capacity;
                
                if (nextWriteIndex == _readIndex)
                {
                    // 缓冲区满了，根据策略处理
                    return HandleBufferFull(ref eventData);
                }

                _buffer[currentWrite] = eventData;
                _writeIndex = nextWriteIndex;
                return true;
            }

            private bool HandleBufferFull(ref T eventData)
            {
                switch (_overflowStrategy)
                {
                    case BufferOverflowStrategy.DropNewest:
                        return false;
                        
                    case BufferOverflowStrategy.DropOldest:
                        // 丢弃最旧的事件，为新事件腾出空间
                        if (TryDequeue(out _))
                        {
                            return TryEnqueue(ref eventData);
                        }
                        return false;
                        
                    case BufferOverflowStrategy.Resize:
                        lock (_resizeLock)
                        {
                            if ((_writeIndex + 1) % _capacity == _readIndex)
                            {
                                ResizeBuffer();
                                return TryEnqueue(ref eventData);
                            }
                        }
                        return TryEnqueue(ref eventData);
                        
                    case BufferOverflowStrategy.LogWarning:
                        Debug.LogWarning($"[ZeroGCEventBus] 事件缓冲区满，事件类型: {typeof(T).Name}");
                        return false;
                        
                    default:
                        return false;
                }
            }

            private void ResizeBuffer()
            {
                int newCapacity = _capacity * 2;
                T[] newBuffer = new T[newCapacity];
                
                int count = Count;
                for (int i = 0; i < count; i++)
                {
                    int readIndex = (_readIndex + i) % _capacity;
                    newBuffer[i] = _buffer[readIndex];
                }
                
                _buffer = newBuffer;
                _capacity = newCapacity;
                _readIndex = 0;
                _writeIndex = count;
            }

            public bool TryDequeue(out T eventData)
            {
                int currentRead = _readIndex;
                if (currentRead == _writeIndex)
                {
                    eventData = default;
                    return false;
                }

                eventData = _buffer[currentRead];
                _readIndex = (currentRead + 1) % _capacity;
                return true;
            }

            private int Count => (_writeIndex - _readIndex + _capacity) % _capacity;

            public void Clear()
            {
                _writeIndex = 0;
                _readIndex = 0;
            }
        }

        /// <summary>
        /// 线程安全的监听器管理器
        /// </summary>
        private class ListenerManager<T> : IListenerManager where T : struct, IZeroGCEvent
        {
            private readonly List<EventHandlerWrapper<T>> _handlers;
            private readonly Queue<int> _freeSlots;
            private readonly ReaderWriterLockSlim _lock;
            private int _nextHandlerId;
            private int _validHandlerCount;

            public ListenerManager()
            {
                _handlers = new List<EventHandlerWrapper<T>>(32);
                _freeSlots = new Queue<int>();
                _lock = new ReaderWriterLockSlim();
                _nextHandlerId = 1; // 从1开始，0保留给无效ID
                _validHandlerCount = 0;
            }

            public int Subscribe(Action<T> handler)
            {
                if (handler == null) return 0;

                _lock.EnterWriteLock();
                try
                {
                    int handlerId = _nextHandlerId++;
                    var wrapper = new EventHandlerWrapper<T>(handler, handlerId);

                    if (_freeSlots.Count > 0)
                    {
                        int index = _freeSlots.Dequeue();
                        _handlers[index] = wrapper;
                    }
                    else
                    {
                        _handlers.Add(wrapper);
                    }

                    _validHandlerCount++;
                    return handlerId;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void Unsubscribe(int handlerId)
            {
                if (handlerId <= 0) return;

                _lock.EnterWriteLock();
                try
                {
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        if (_handlers[i].HandlerId == handlerId)
                        {
                            _handlers[i] = default;
                            _freeSlots.Enqueue(i);
                            _validHandlerCount--;
                            break;
                        }
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void NotifyAll(ref T eventData)
            {
                _lock.EnterReadLock();
                try
                {
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        if (_handlers[i].IsValid)
                        {
                            _handlers[i].Handle(ref eventData);
                        }
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            public int ListenerCount => _validHandlerCount;

            public void ClearInvalidHandlers()
            {
                _lock.EnterWriteLock();
                try
                {
                    int validCount = 0;
                    for (int i = 0; i < _handlers.Count; i++)
                    {
                        if (_handlers[i].IsValid)
                        {
                            validCount++;
                        }
                        else if (_handlers[i].HandlerId > 0)
                        {
                            _handlers[i] = default;
                            _freeSlots.Enqueue(i);
                        }
                    }
                    _validHandlerCount = validCount;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void Dispose()
            {
                if (_lock != null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        // 清空所有处理器
                        _handlers.Clear();
                        _freeSlots.Clear();
                        _validHandlerCount = 0;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                        _lock.Dispose();
                    }
                }
            }
        }

        #endregion

        #region 事件管理

        // 全局事件缓冲区字典 - 每种事件类型一个缓冲区
        private readonly ConcurrentDictionary<int, object> _eventBuffers = new ConcurrentDictionary<int, object>();

        // 全局监听器管理器字典
        private readonly ConcurrentDictionary<int, object> _listenerManagers = new ConcurrentDictionary<int, object>();

        // 事件处理器缓存
        private readonly ConcurrentDictionary<int, Action<ZeroGCEventBus, int>> _processors = new ConcurrentDictionary<int, Action<ZeroGCEventBus, int>>();

        // 性能统计
        private volatile int _eventsProcessedThisFrame;
        private volatile int _totalEventsProcessed;
        private volatile float _averageProcessingTime;
        private System.Diagnostics.Stopwatch _processingTimer;
        
        // 配置
        private BufferOverflowStrategy _defaultBufferStrategy = BufferOverflowStrategy.DropNewest;
        private int _defaultBufferCapacity = 256;

        #endregion

        #region 公共API

        /// <summary>
        /// 设置默认缓冲区配置
        /// </summary>
        public void SetDefaultBufferConfig(int capacity, BufferOverflowStrategy strategy)
        {
            _defaultBufferCapacity = Math.Max(capacity, 16);
            _defaultBufferStrategy = strategy;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventHandlerId<T> Subscribe<T>(Action<T> handler) where T : struct, IZeroGCEvent
        {
            if (handler == null) return default;

            int typeId = EventTypeRegistry.GetTypeId<T>();
            var manager = GetOrCreateListenerManager<T>(typeId);
            int handlerId = manager.Subscribe(handler);
            
            return new EventHandlerId<T>(handlerId);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unsubscribe<T>(EventHandlerId<T> handlerId) where T : struct, IZeroGCEvent
        {
            if (!handlerId.IsValid) return;

            int typeId = EventTypeRegistry.GetTypeId<T>();
            if (_listenerManagers.TryGetValue(typeId, out object managerObj))
            {
                ((ListenerManager<T>)managerObj).Unsubscribe(handlerId.ToInt());
            }
        }

        /// <summary>
        /// 发布事件 - 延迟处理版本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Publish<T>(T eventData) where T : struct, IZeroGCEvent
        {
            int typeId = EventTypeRegistry.GetTypeId<T>();
            var buffer = GetOrCreateEventBuffer<T>(typeId);
            return buffer.TryEnqueue(ref eventData);
        }

        /// <summary>
        /// 立即发布事件 - 无缓冲版本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PublishImmediate<T>(T eventData) where T : struct, IZeroGCEvent
        {
            int typeId = EventTypeRegistry.GetTypeId<T>();
            if (_listenerManagers.TryGetValue(typeId, out object managerObj))
            {
                var manager = (ListenerManager<T>)managerObj;
                T localData = eventData;
                manager.NotifyAll(ref localData);
            }
        }

        /// <summary>
        /// 清空指定类型的事件缓冲区
        /// </summary>
        public void ClearEventBuffer<T>() where T : struct, IZeroGCEvent
        {
            int typeId = EventTypeRegistry.GetTypeId<T>();
            if (_eventBuffers.TryGetValue(typeId, out object bufferObj))
            {
                ((EventBuffer<T>)bufferObj).Clear();
            }
        }

        /// <summary>
        /// 获取监听器数量
        /// </summary>
        public int GetListenerCount<T>() where T : struct, IZeroGCEvent
        {
            int typeId = EventTypeRegistry.GetTypeId<T>();
            if (_listenerManagers.TryGetValue(typeId, out object managerObj))
            {
                return ((IListenerManager)managerObj).ListenerCount;
            }
            return 0;
        }

        /// <summary>
        /// 清理无效的监听器
        /// </summary>
        public void CleanupInvalidListeners()
        {
            foreach (var manager in _listenerManagers.Values)
            {
                ((IListenerManager)manager).ClearInvalidHandlers();
            }
        }

        #endregion

        #region 内部方法

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EventBuffer<T> GetOrCreateEventBuffer<T>(int typeId) where T : struct, IZeroGCEvent
        {
            return (EventBuffer<T>)_eventBuffers.GetOrAdd(typeId, _ => 
                new EventBuffer<T>(_defaultBufferCapacity, _defaultBufferStrategy));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ListenerManager<T> GetOrCreateListenerManager<T>(int typeId) where T : struct, IZeroGCEvent
        {
            var manager = (ListenerManager<T>)_listenerManagers.GetOrAdd(typeId, _ => new ListenerManager<T>());
            
            // 同时注册处理器
            _processors.TryAdd(typeId, ProcessorCache<T>.Processor);
            
            return manager;
        }

        /// <summary>
        /// 处理所有缓冲的事件
        /// </summary>
        private void ProcessAllEvents()
        {
            _processingTimer.Restart();
            _eventsProcessedThisFrame = 0;

            // 使用缓存的处理器，
            foreach (var kvp in _processors)
            {
                int typeId = kvp.Key;
                var processor = kvp.Value;
                
                if (_eventBuffers.ContainsKey(typeId))
                {
                    processor(this, typeId);
                }
            }

            _processingTimer.Stop();
            UpdatePerformanceStats();
        }

        private void UpdatePerformanceStats()
        {
            float currentProcessingTime = (float)_processingTimer.Elapsed.TotalMilliseconds;
            _averageProcessingTime = (_averageProcessingTime * 0.9f) + (currentProcessingTime * 0.1f);
        }

        #endregion

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();
            _processingTimer = new System.Diagnostics.Stopwatch();
            Debug.Log("[ZeroGCEventBus] 零GC事件总线初始化完成 - 优化版本");
        }

        protected void Update()
        {
            ProcessAllEvents();
            
            // 定期清理无效监听器（每60帧一次）
            if (Time.frameCount % 60 == 0)
            {
                CleanupInvalidListeners();
            }
            
            // 定期清理引用类型管理器中的死亡引用（每300帧一次，约5秒）
            if (Time.frameCount % 300 == 0)
            {
                ReferenceTypeManager.CleanupDeadReferences();
            }
        }

        protected override void Cleanup()
        {
            // 清理所有缓冲区
            _eventBuffers.Clear();
            
            // 释放监听器管理器
            foreach (var manager in _listenerManagers.Values)
            {
                if (manager is IListenerManager disposableManager)
                {
                    try
                    {
                        disposableManager.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[ZeroGCEventBus] 释放监听器管理器时发生错误: {ex.Message}");
                    }
                }
            }
            _listenerManagers.Clear();
            _processors.Clear();
            
            base.Cleanup();
        }

        #endregion

        #region 调试和性能监控

        public struct PerformanceStats
        {
            public int EventsProcessedThisFrame;
            public int TotalEventsProcessed;
            public float AverageProcessingTime;
            public int RegisteredEventTypes;
            public int TotalListeners;
            public int TotalBufferCapacity;
        }

        public PerformanceStats GetPerformanceStats()
        {
            int totalListeners = 0;
            int totalBufferCapacity = 0;
            
            foreach (var manager in _listenerManagers.Values)
            {
                totalListeners += ((IListenerManager)manager).ListenerCount;
            }
            
            foreach (var buffer in _eventBuffers.Values)
            {
                // 简化处理，实际可以通过接口获取更精确的容量信息
                totalBufferCapacity += _defaultBufferCapacity;
            }

            return new PerformanceStats
            {
                EventsProcessedThisFrame = _eventsProcessedThisFrame,
                TotalEventsProcessed = _totalEventsProcessed,
                AverageProcessingTime = _averageProcessingTime,
                RegisteredEventTypes = _eventBuffers.Count,
                TotalListeners = totalListeners,
                TotalBufferCapacity = totalBufferCapacity
            };
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogPerformanceStats()
        {
            var stats = GetPerformanceStats();
            Debug.Log($"[EventBus] 本帧: {stats.EventsProcessedThisFrame}, " +
                      $"总计: {stats.TotalEventsProcessed}, " +
                      $"平均时间: {stats.AverageProcessingTime:F2}ms, " +
                      $"事件类型: {stats.RegisteredEventTypes}, " +
                      $"监听器: {stats.TotalListeners}, " +
                      $"缓冲区容量: {stats.TotalBufferCapacity}");
        }

        #endregion
    }
}
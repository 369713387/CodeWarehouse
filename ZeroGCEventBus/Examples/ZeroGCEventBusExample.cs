using System;
using System.Collections;
using UnityEngine;
using Framework.ZeroGCEventBus;

namespace Framework.ZeroGCEventBus.Examples
{
    /// <summary>
    /// ZeroGCEventBus 使用示例
    /// 演示各种使用场景和最佳实践
    /// </summary>
    public class ZeroGCEventBusExample : MonoBehaviour
    {
        #region 示例事件定义
        
        /// <summary>
        /// 玩家健康值变化事件
        /// </summary>
        public struct PlayerHealthChangedEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public int PlayerId;
            public int OldHealth;
            public int NewHealth;
            public Vector3 Position;
        }
        
        /// <summary>
        /// 物品收集事件
        /// </summary>
        public struct ItemCollectedEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public int ItemId;
            public int PlayerId;
            public Vector3 Position;
            public float Value;
        }
        
        /// <summary>
        /// 游戏状态变化事件
        /// </summary>
        public struct GameStateChangedEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public GameState OldState;
            public GameState NewState;
            public float TransitionTime;
        }
        
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver
        }
        
        /// <summary>
        /// UI按钮点击事件
        /// </summary>
        public struct UIButtonClickEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public string ButtonName;
            public int PlayerId;
        }
        
        #endregion

        #region 组件引用和配置
        
        [Header("配置")]
        [SerializeField] private int bufferCapacity = 512;
        [SerializeField] private ZeroGCEventBus.BufferOverflowStrategy overflowStrategy = ZeroGCEventBus.BufferOverflowStrategy.DropNewest;
        [SerializeField] private bool enablePerformanceLogging = true;
        [SerializeField] private float performanceLogInterval = 5f;
        
        [Header("测试参数")]
        [SerializeField] private int testPlayerId = 1;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private bool autoTest = false;
        [SerializeField] private float testInterval = 1f;
        
        // 事件订阅ID存储
        private ZeroGCEventBus.EventHandlerId<PlayerHealthChangedEvent> _healthEventId;
        private ZeroGCEventBus.EventHandlerId<ItemCollectedEvent> _itemEventId;
        private ZeroGCEventBus.EventHandlerId<GameStateChangedEvent> _gameStateEventId;
        private ZeroGCEventBus.EventHandlerId<UIButtonClickEvent> _buttonEventId;
        
        // 测试数据
        private int _currentHealth = 100;
        private GameState _currentGameState = GameState.Menu;
        private int _itemIdCounter = 1;
        
        #endregion

        #region Unity生命周期
        
        void Start()
        {
            InitializeEventBus();
            SubscribeToEvents();
            
            if (autoTest)
            {
                StartCoroutine(AutoTestCoroutine());
            }
            
            if (enablePerformanceLogging)
            {
                StartCoroutine(PerformanceLogCoroutine());
            }
            
            // 演示配置变更
            DemonstrateConfiguration();
        }
        
        void Update()
        {
            // 演示手动事件发布
            HandleKeyboardInput();
            
            if (enablePerformanceLogging && Time.frameCount % 300 == 0)
            {
                LogCurrentStats();
            }
        }
        
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion

        #region 事件总线初始化和配置
        
        private void InitializeEventBus()
        {
            // 配置默认缓冲区设置
            ZeroGCEventBus.Instance.SetDefaultBufferConfig(bufferCapacity, overflowStrategy);
            
            Debug.Log($"[EventBus示例] 事件总线初始化完成，缓冲区容量: {bufferCapacity}, 溢出策略: {overflowStrategy}");
        }
        
        private void DemonstrateConfiguration()
        {
            // 演示不同场景下的配置调整
            Debug.Log("[EventBus示例] 演示配置调整");
            
            // 模拟进入战斗场景
            ZeroGCEventBus.Instance.SetDefaultBufferConfig(1024, ZeroGCEventBus.BufferOverflowStrategy.Resize);
            Debug.Log("[EventBus示例] 战斗场景配置: 容量1024, 自动扩容");
            
            // 等待一帧后恢复默认配置
            StartCoroutine(RestoreDefaultConfig());
        }
        
        private IEnumerator RestoreDefaultConfig()
        {
            yield return null;
            ZeroGCEventBus.Instance.SetDefaultBufferConfig(bufferCapacity, overflowStrategy);
            Debug.Log("[EventBus示例] 恢复默认配置");
        }
        
        #endregion

        #region 事件订阅
        
        private void SubscribeToEvents()
        {
            // 订阅健康值变化事件
            _healthEventId = ZeroGCEventBus.Instance.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            
            // 订阅物品收集事件
            _itemEventId = ZeroGCEventBus.Instance.Subscribe<ItemCollectedEvent>(OnItemCollected);
            
            // 订阅游戏状态变化事件
            _gameStateEventId = ZeroGCEventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            
            // 订阅UI按钮点击事件
            _buttonEventId = ZeroGCEventBus.Instance.Subscribe<UIButtonClickEvent>(OnButtonClicked);
            
            Debug.Log("[EventBus示例] 所有事件订阅完成");
        }
        
        private void UnsubscribeFromEvents()
        {
            // 取消所有订阅
            SafeZeroGCEventBusController.SafeUnsubscribe(_healthEventId);
            SafeZeroGCEventBusController.SafeUnsubscribe(_itemEventId);
            SafeZeroGCEventBusController.SafeUnsubscribe(_gameStateEventId);
            SafeZeroGCEventBusController.SafeUnsubscribe(_buttonEventId);
            Debug.Log("[EventBus示例] 所有事件取消订阅完成");
        }
        
        #endregion

        #region 事件处理器
        
        private void OnPlayerHealthChanged(PlayerHealthChangedEvent eventData)
        {
            Debug.Log($"[玩家健康] 玩家{eventData.PlayerId} 健康值变化: {eventData.OldHealth} -> {eventData.NewHealth}");
            
            if (eventData.NewHealth <= 0)
            {
                Debug.Log($"[玩家健康] 玩家{eventData.PlayerId} 死亡!");
                
                // 连锁事件：玩家死亡时改变游戏状态
                var gameOverEvent = new GameStateChangedEvent
                {
                    OldState = _currentGameState,
                    NewState = GameState.GameOver,
                    TransitionTime = Time.time
                };
                
                // 使用立即发布避免递归问题
                ZeroGCEventBus.Instance.PublishImmediate(gameOverEvent);
            }
        }
        
        private void OnItemCollected(ItemCollectedEvent eventData)
        {
            Debug.Log($"[物品收集] 玩家{eventData.PlayerId} 收集了物品{eventData.ItemId}，价值: {eventData.Value}");
            
            // 连锁事件：收集物品时可能恢复健康值
            if (eventData.Value > 0 && _currentHealth < maxHealth)
            {
                int oldHealth = _currentHealth;
                _currentHealth = Mathf.Min(maxHealth, _currentHealth + (int)eventData.Value);
                
                var healthEvent = new PlayerHealthChangedEvent
                {
                    PlayerId = eventData.PlayerId,
                    OldHealth = oldHealth,
                    NewHealth = _currentHealth,
                    Position = eventData.Position
                };
                
                ZeroGCEventBus.Instance.Publish(healthEvent);
            }
        }
        
        private void OnGameStateChanged(GameStateChangedEvent eventData)
        {
            Debug.Log($"[游戏状态] 状态变化: {eventData.OldState} -> {eventData.NewState}");
            _currentGameState = eventData.NewState;
        }
        
        private void OnButtonClicked(UIButtonClickEvent eventData)
        {
            Debug.Log($"[UI] 按钮点击: {eventData.ButtonName}, 玩家: {eventData.PlayerId}");
            
            // 根据按钮类型执行不同操作
            switch (eventData.ButtonName)
            {
                case "StartGame":
                    PublishGameStateChange(GameState.Playing);
                    break;
                case "PauseGame":
                    PublishGameStateChange(GameState.Paused);
                    break;
                case "Heal":
                    SimulateHealing();
                    break;
            }
        }
        
        #endregion

        #region 事件发布示例
        
        /// <summary>
        /// 模拟玩家受伤
        /// </summary>
        [ContextMenu("模拟受伤")]
        public void SimulateDamage()
        {
            if (_currentHealth <= 0) return;
            
            int damage = UnityEngine.Random.Range(10, 30);
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            
            var healthEvent = new PlayerHealthChangedEvent
            {
                PlayerId = testPlayerId,
                OldHealth = oldHealth,
                NewHealth = _currentHealth,
                Position = transform.position
            };
            
            // 使用延迟发布
            bool success = ZeroGCEventBus.Instance.Publish(healthEvent);
            if (!success)
            {
                Debug.LogWarning("[EventBus示例] 健康事件发布失败，缓冲区可能已满");
            }
        }
        
        /// <summary>
        /// 模拟治疗
        /// </summary>
        [ContextMenu("模拟治疗")]
        public void SimulateHealing()
        {
            if (_currentHealth >= maxHealth) return;
            
            int healing = UnityEngine.Random.Range(15, 25);
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + healing);
            
            var healthEvent = new PlayerHealthChangedEvent
            {
                PlayerId = testPlayerId,
                OldHealth = oldHealth,
                NewHealth = _currentHealth,
                Position = transform.position
            };
            
            // 使用立即发布
            ZeroGCEventBus.Instance.PublishImmediate(healthEvent);
        }
        
        /// <summary>
        /// 模拟物品收集
        /// </summary>
        [ContextMenu("模拟物品收集")]
        public void SimulateItemCollection()
        {
            var itemEvent = new ItemCollectedEvent
            {
                ItemId = _itemIdCounter++,
                PlayerId = testPlayerId,
                Position = transform.position + UnityEngine.Random.insideUnitSphere,
                Value = UnityEngine.Random.Range(5f, 20f)
            };
            
            ZeroGCEventBus.Instance.Publish(itemEvent);
        }
        
        /// <summary>
        /// 发布游戏状态变化
        /// </summary>
        private void PublishGameStateChange(GameState newState)
        {
            var stateEvent = new GameStateChangedEvent
            {
                OldState = _currentGameState,
                NewState = newState,
                TransitionTime = Time.time
            };
            
            ZeroGCEventBus.Instance.Publish(stateEvent);
        }
        
        /// <summary>
        /// 模拟按钮点击
        /// </summary>
        public void SimulateButtonClick(string buttonName)
        {
            var buttonEvent = new UIButtonClickEvent
            {
                ButtonName = buttonName,
                PlayerId = testPlayerId
            };
            
            ZeroGCEventBus.Instance.Publish(buttonEvent);
        }
        
        #endregion

        #region 键盘输入处理
        
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                SimulateHealing();
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                SimulateDamage();
            }
            
            if (Input.GetKeyDown(KeyCode.I))
            {
                SimulateItemCollection();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SimulateButtonClick("StartGame");
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SimulateButtonClick("PauseGame");
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SimulateButtonClick("Heal");
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                LogCurrentStats();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearAllBuffers();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                PerformStressTest();
            }
        }
        
        #endregion

        #region 自动测试和性能监控
        
        private IEnumerator AutoTestCoroutine()
        {
            Debug.Log("[EventBus示例] 开始自动测试");
            
            while (autoTest && Application.isPlaying)
            {
                // 随机执行不同操作
                int action = UnityEngine.Random.Range(0, 4);
                switch (action)
                {
                    case 0:
                        SimulateDamage();
                        break;
                    case 1:
                        SimulateHealing();
                        break;
                    case 2:
                        SimulateItemCollection();
                        break;
                    case 3:
                        SimulateButtonClick("TestButton");
                        break;
                }
                
                yield return new WaitForSeconds(testInterval);
            }
        }
        
        private IEnumerator PerformanceLogCoroutine()
        {
            while (enablePerformanceLogging && Application.isPlaying)
            {
                yield return new WaitForSeconds(performanceLogInterval);
                LogCurrentStats();
            }
        }
        
        #endregion

        #region 工具方法
        
        /// <summary>
        /// 记录当前统计信息
        /// </summary>
        [ContextMenu("显示性能统计")]
        public void LogCurrentStats()
        {
            var stats = ZeroGCEventBus.Instance.GetPerformanceStats();
            
            Debug.Log($"[EventBus统计] " +
                     $"本帧事件: {stats.EventsProcessedThisFrame}, " +
                     $"总计事件: {stats.TotalEventsProcessed}, " +
                     $"平均处理时间: {stats.AverageProcessingTime:F3}ms, " +
                     $"事件类型数: {stats.RegisteredEventTypes}, " +
                     $"监听器数: {stats.TotalListeners}, " +
                     $"缓冲区容量: {stats.TotalBufferCapacity}");
            
            // 显示具体事件类型的监听器数量
            LogSpecificEventStats();
        }
        
        private void LogSpecificEventStats()
        {
            int healthListeners = ZeroGCEventBus.Instance.GetListenerCount<PlayerHealthChangedEvent>();
            int itemListeners = ZeroGCEventBus.Instance.GetListenerCount<ItemCollectedEvent>();
            int stateListeners = ZeroGCEventBus.Instance.GetListenerCount<GameStateChangedEvent>();
            int buttonListeners = ZeroGCEventBus.Instance.GetListenerCount<UIButtonClickEvent>();
            
            Debug.Log($"[监听器详情] " +
                     $"健康事件: {healthListeners}, " +
                     $"物品事件: {itemListeners}, " +
                     $"状态事件: {stateListeners}, " +
                     $"按钮事件: {buttonListeners}");
        }
        
        /// <summary>
        /// 清空所有缓冲区
        /// </summary>
        [ContextMenu("清空所有缓冲区")]
        public void ClearAllBuffers()
        {
            ZeroGCEventBus.Instance.ClearEventBuffer<PlayerHealthChangedEvent>();
            ZeroGCEventBus.Instance.ClearEventBuffer<ItemCollectedEvent>();
            ZeroGCEventBus.Instance.ClearEventBuffer<GameStateChangedEvent>();
            ZeroGCEventBus.Instance.ClearEventBuffer<UIButtonClickEvent>();
            
            Debug.Log("[EventBus示例] 所有事件缓冲区已清空");
        }
        
        /// <summary>
        /// 手动清理无效监听器
        /// </summary>
        [ContextMenu("清理无效监听器")]
        public void CleanupListeners()
        {
            ZeroGCEventBus.Instance.CleanupInvalidListeners();
            Debug.Log("[EventBus示例] 无效监听器清理完成");
        }
        
        /// <summary>
        /// 压力测试
        /// </summary>
        [ContextMenu("执行压力测试")]
        public void PerformStressTest()
        {
            StartCoroutine(StressTestCoroutine());
        }
        
        private IEnumerator StressTestCoroutine()
        {
            Debug.Log("[EventBus示例] 开始压力测试 - 将在1秒内发送1000个事件");
            
            var startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 1000; i++)
            {
                var healthEvent = new PlayerHealthChangedEvent
                {
                    PlayerId = i % 10,
                    OldHealth = 100,
                    NewHealth = 99,
                    Position = Vector3.zero
                };
                
                ZeroGCEventBus.Instance.Publish(healthEvent);
                
                if (i % 100 == 0)
                {
                    yield return null; // 让出一帧
                }
            }
            
            var endTime = Time.realtimeSinceStartup;
            Debug.Log($"[EventBus示例] 压力测试完成，耗时: {(endTime - startTime) * 1000:F2}ms");
            
            // 等待一帧让事件处理完
            yield return null;
            LogCurrentStats();
        }
        
        #endregion

        #region GUI调试界面
        
        void OnGUI()
        {
            if (!enablePerformanceLogging) return;
            
            var rect = new Rect(10, 10, 300, 200);
            GUI.Box(rect, "ZeroGCEventBus 调试面板");
            
            var stats = ZeroGCEventBus.Instance.GetPerformanceStats();
            
            GUI.Label(new Rect(20, 35, 280, 20), $"本帧事件: {stats.EventsProcessedThisFrame}");
            GUI.Label(new Rect(20, 55, 280, 20), $"总计事件: {stats.TotalEventsProcessed}");
            GUI.Label(new Rect(20, 75, 280, 20), $"处理时间: {stats.AverageProcessingTime:F3}ms");
            GUI.Label(new Rect(20, 95, 280, 20), $"事件类型: {stats.RegisteredEventTypes}");
            GUI.Label(new Rect(20, 115, 280, 20), $"监听器数: {stats.TotalListeners}");
            GUI.Label(new Rect(20, 135, 280, 20), $"当前血量: {_currentHealth}/{maxHealth}");
            GUI.Label(new Rect(20, 155, 280, 20), $"游戏状态: {_currentGameState}");
            
            if (GUI.Button(new Rect(20, 175, 80, 25), "受伤"))
            {
                SimulateDamage();
            }
            
            if (GUI.Button(new Rect(110, 175, 80, 25), "治疗"))
            {
                SimulateHealing();
            }
            
            if (GUI.Button(new Rect(200, 175, 80, 25), "收集"))
            {
                SimulateItemCollection();
            }
        }
        
        #endregion
    }

    #region 辅助类 - 事件订阅管理器
    
    /// <summary>
    /// 事件订阅管理器 - 简化多个订阅的管理
    /// </summary>
    public class EventSubscriptionManager : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<IEventSubscription> _subscriptions = 
            new System.Collections.Generic.List<IEventSubscription>();
        
        /// <summary>
        /// 添加事件订阅
        /// </summary>
        public ZeroGCEventBus.EventHandlerId<T> Subscribe<T>(Action<T> handler) where T : struct, ZeroGCEventBus.IZeroGCEvent
        {
            var handlerId = ZeroGCEventBus.Instance.Subscribe(handler);
            _subscriptions.Add(new EventSubscription<T>(handlerId));
            return handlerId;
        }
        
        /// <summary>
        /// 清理所有订阅
        /// </summary>
        public void UnsubscribeAll()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }
        
        void OnDestroy()
        {
            UnsubscribeAll();
        }
    }
    
    /// <summary>
    /// 事件订阅接口
    /// </summary>
    public interface IEventSubscription : IDisposable
    {
    }
    
    /// <summary>
    /// 具体的事件订阅实现
    /// </summary>
    public class EventSubscription<T> : IEventSubscription where T : struct, ZeroGCEventBus.IZeroGCEvent
    {
        private ZeroGCEventBus.EventHandlerId<T> _handlerId;
        private bool _disposed = false;
        
        public EventSubscription(ZeroGCEventBus.EventHandlerId<T> handlerId)
        {
            _handlerId = handlerId;
        }
        
        public void Dispose()
        {
            if (!_disposed && _handlerId.IsValid)
            {
                ZeroGCEventBus.Instance.Unsubscribe(_handlerId);
                _handlerId = default;
                _disposed = true;
            }
        }
    }
    
    #endregion
} 
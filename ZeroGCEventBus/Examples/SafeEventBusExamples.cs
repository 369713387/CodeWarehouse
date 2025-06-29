using UnityEngine;
using Framework.ZeroGCEventBus;
using static Framework.ZeroGCEventBus.SafeZeroGCEventBusController;

namespace Framework.ZeroGCEventBus.Examples
{
    /// <summary>
    /// SafeZeroGCEventBusController 使用示例
    /// </summary>
    public class SafeEventBusExamples : MonoBehaviour
    {
        #region 示例事件定义

        public struct PlayerHealthEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public int PlayerId;
            public int NewHealth;
            public int MaxHealth;
        }

        public struct PlayerLevelUpEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public int PlayerId;
            public int NewLevel;
            public int SkillPoints;
        }

        public struct GameStateEvent : ZeroGCEventBus.IZeroGCEvent
        {
            public string StateName;
            public float StateTime;
        }

        #endregion

        #region 字段

        [Header("测试配置")]
        [SerializeField] private int testPlayerId = 1;
        [SerializeField] private bool enableVerboseLogging = true;

        // 单独管理的订阅
        private ZeroGCEventBus.EventHandlerId<PlayerHealthEvent> _healthEventId;

        // 组管理的订阅
        private const string PLAYER_EVENT_GROUP = "PlayerEvents";
        private const string GAME_EVENT_GROUP = "GameEvents";

        #endregion

        #region Unity 生命周期

        void Start()
        {
            DemonstrateBasicSafeOperations();
            DemonstrateGroupSubscriptions();
            DemonstrateConfigurationAndStats();
        }

        void Update()
        {
            // 键盘输入演示
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                DemonstrateEventPublishing();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                DemonstrateCleanupOperations();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                LogCurrentStats();
            }
        }

        void OnDestroy()
        {
            // 安全清理所有订阅
            SafeUnsubscribe(_healthEventId, "玩家健康");
            UnsubscribeGroup(PLAYER_EVENT_GROUP);
            UnsubscribeGroup(GAME_EVENT_GROUP);
        }

        #endregion

        #region 基础安全操作演示

        [ContextMenu("1. 基础安全操作")]
        public void DemonstrateBasicSafeOperations()
        {
            Debug.Log("=== 基础安全操作演示 ===");

            // 检查事件总线可用性
            if (!IsEventBusAvailable)
            {
                Debug.LogError("事件总线不可用！");
                return;
            }

            // 安全订阅事件
            _healthEventId = SafeSubscribe<PlayerHealthEvent>(OnPlayerHealthChanged, "玩家健康");

            // 安全发布事件
            var healthEvent = new PlayerHealthEvent
            {
                PlayerId = testPlayerId,
                NewHealth = 85,
                MaxHealth = 100
            };

            SafePublish(healthEvent, "玩家健康变化");

            // 立即发布事件
            var criticalHealthEvent = new PlayerHealthEvent
            {
                PlayerId = testPlayerId,
                NewHealth = 15,
                MaxHealth = 100
            };

            SafePublishImmediate(criticalHealthEvent, "玩家危险健康");
        }

        #endregion

        #region 组订阅管理演示

        [ContextMenu("2. 组订阅管理")]
        public void DemonstrateGroupSubscriptions()
        {
            Debug.Log("=== 组订阅管理演示 ===");

            // 订阅到玩家事件组
            SubscribeToGroup<PlayerLevelUpEvent>(OnPlayerLevelUp, PLAYER_EVENT_GROUP, "玩家升级");
            SubscribeToGroup<PlayerHealthEvent>(OnPlayerHealthInGroup, PLAYER_EVENT_GROUP, "玩家健康（组）");

            // 订阅到游戏状态事件组
            SubscribeToGroup<GameStateEvent>(OnGameStateChanged, GAME_EVENT_GROUP, "游戏状态");

            Debug.Log($"玩家事件组订阅完成，按键1测试事件发布");
        }

        #endregion

        #region 配置和统计演示

        [ContextMenu("3. 配置和统计")]
        public void DemonstrateConfigurationAndStats()
        {
            Debug.Log("=== 配置和统计演示 ===");

            // 设置缓冲区配置
            SetDefaultBufferConfig(512, ZeroGCEventBus.BufferOverflowStrategy.Resize);

            // 获取监听器数量
            int healthListeners = GetListenerCount<PlayerHealthEvent>();
            int levelListeners = GetListenerCount<PlayerLevelUpEvent>();
            int gameListeners = GetListenerCount<GameStateEvent>();

            Debug.Log($"监听器统计 - 健康事件: {healthListeners}, 升级事件: {levelListeners}, 游戏状态: {gameListeners}");

            // 打印订阅组统计
            LogSubscriptionGroupsStats();
        }

        #endregion

        #region 事件发布演示

        [ContextMenu("4. 事件发布测试 (按键1)")]
        public void DemonstrateEventPublishing()
        {
            Debug.Log("=== 事件发布演示 ===");

            // 发布各种类型的事件
            var levelUpEvent = new PlayerLevelUpEvent
            {
                PlayerId = testPlayerId,
                NewLevel = 5,
                SkillPoints = 3
            };

            var gameStateEvent = new GameStateEvent
            {
                StateName = "Battle",
                StateTime = Time.time
            };

            var healthEvent = new PlayerHealthEvent
            {
                PlayerId = testPlayerId,
                NewHealth = UnityEngine.Random.Range(20, 100),
                MaxHealth = 100
            };

            // 发布事件
            SafePublish(levelUpEvent, "玩家升级");
            SafePublish(gameStateEvent, "游戏状态变化");
            SafePublish(healthEvent, "随机健康变化");

            Debug.Log("所有测试事件已发布");
        }

        #endregion

        #region 清理操作演示

        [ContextMenu("5. 清理操作 (按键2)")]
        public void DemonstrateCleanupOperations()
        {
            Debug.Log("=== 清理操作演示 ===");

            // 清理无效订阅
            int cleaned = CleanupAllInvalidSubscriptions();
            Debug.Log($"清理了 {cleaned} 个无效订阅");

            // 清理无效监听器
            bool success = CleanupInvalidListeners();
            Debug.Log($"清理无效监听器: {(success ? "成功" : "失败")}");

            // 执行完整清理
            int totalCleaned = PerformFullCleanup();
            Debug.Log($"完整清理完成，总共清理了 {totalCleaned} 个项目");
        }

        #endregion

        #region 统计信息

        [ContextMenu("6. 当前统计 (按键3)")]
        public void LogCurrentStats()
        {
            Debug.Log("=== 当前统计信息 ===");

            // 事件监听器统计
            Debug.Log($"PlayerHealthEvent 监听器: {GetListenerCount<PlayerHealthEvent>()}");
            Debug.Log($"PlayerLevelUpEvent 监听器: {GetListenerCount<PlayerLevelUpEvent>()}");
            Debug.Log($"GameStateEvent 监听器: {GetListenerCount<GameStateEvent>()}");

            // 订阅组统计
            LogSubscriptionGroupsStats();

            // 组详细信息
            var groupsInfo = GetSubscriptionGroupsInfo();
            foreach (var kvp in groupsInfo)
            {
                Debug.Log($"组 '{kvp.Key}': {kvp.Value} 个有效订阅");
            }
        }

        #endregion

        #region 事件处理器

        private void OnPlayerHealthChanged(PlayerHealthEvent eventData)
        {
            Debug.Log($"[单独订阅] 玩家{eventData.PlayerId}健康变化: {eventData.NewHealth}/{eventData.MaxHealth}");
            
            if (eventData.NewHealth < 20)
            {
                Debug.LogWarning($"玩家{eventData.PlayerId}健康危险！");
            }
        }

        private void OnPlayerHealthInGroup(PlayerHealthEvent eventData)
        {
            if (enableVerboseLogging)
            {
                Debug.Log($"[组订阅] 玩家{eventData.PlayerId}健康: {eventData.NewHealth}/{eventData.MaxHealth}");
            }
        }

        private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
        {
            Debug.Log($"[组订阅] 玩家{eventData.PlayerId}升级到{eventData.NewLevel}级，获得{eventData.SkillPoints}技能点");

            // 连锁事件：升级时恢复健康
            var healthEvent = new PlayerHealthEvent
            {
                PlayerId = eventData.PlayerId,
                NewHealth = 100,
                MaxHealth = 100
            };

            SafePublishImmediate(healthEvent, "升级回血");
        }

        private void OnGameStateChanged(GameStateEvent eventData)
        {
            Debug.Log($"[组订阅] 游戏状态变为: {eventData.StateName} (时间: {eventData.StateTime:F2})");
        }

        #endregion

        #region 压力测试

        [ContextMenu("7. 压力测试")]
        public void PerformStressTest()
        {
            Debug.Log("=== 压力测试开始 ===");

            const int eventCount = 1000;
            float startTime = Time.realtimeSinceStartup;

            // 大量发布事件
            for (int i = 0; i < eventCount; i++)
            {
                var healthEvent = new PlayerHealthEvent
                {
                    PlayerId = i % 10,
                    NewHealth = UnityEngine.Random.Range(1, 100),
                    MaxHealth = 100
                };

                SafePublish(healthEvent);
            }

            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"压力测试完成：发布{eventCount}个事件，耗时{(endTime - startTime) * 1000:F2}ms");

            // 统计信息
            LogCurrentStats();
        }

        #endregion

        #region 高级功能演示

        [ContextMenu("8. 高级功能演示")]
        public void DemonstrateAdvancedFeatures()
        {
            Debug.Log("=== 高级功能演示 ===");

            // 清空特定事件的缓冲区
            ClearEventBuffer<PlayerHealthEvent>();
            Debug.Log("玩家健康事件缓冲区已清空");

            // 动态调整缓冲区配置
            SetDefaultBufferConfig(1024, ZeroGCEventBus.BufferOverflowStrategy.DropOldest);
            Debug.Log("缓冲区配置已更新为1024容量，丢弃最旧策略");

            // 批量取消订阅演示
            SubscribeToGroup<GameStateEvent>(OnGameStateChanged, "TempGroup", "临时游戏状态");
            int unsubscribed = UnsubscribeGroup("TempGroup");
            Debug.Log($"临时组取消订阅完成，共{unsubscribed}个事件");
        }

        #endregion

        #region GUI显示

        void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("SafeEventBus 控制面板", GUI.skin.box);

            if (GUILayout.Button("1. 基础操作演示"))
                DemonstrateBasicSafeOperations();

            if (GUILayout.Button("2. 组订阅演示"))
                DemonstrateGroupSubscriptions();

            if (GUILayout.Button("3. 发布事件"))
                DemonstrateEventPublishing();

            if (GUILayout.Button("4. 清理操作"))
                DemonstrateCleanupOperations();

            if (GUILayout.Button("5. 当前统计"))
                LogCurrentStats();

            if (GUILayout.Button("6. 压力测试"))
                PerformStressTest();

            GUILayout.Label($"事件总线状态: {(IsEventBusAvailable ? "可用" : "不可用")}");

            GUILayout.EndArea();
        }

        #endregion
    }
} 
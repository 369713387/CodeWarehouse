using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// ET ECS使用示例
    /// 展示ET ECS的核心设计理念：
    /// 1. 逻辑与数据完全分离
    /// 2. 组件代替继承
    /// 3. 类型分发代替虚函数
    /// 4. 扩展方法实现逻辑
    /// 5. 树状结构的生命周期管理
    /// </summary>
    public class ETECSExample : MonoBehaviour
    {
        // 场景根实体，管理所有游戏对象的生命周期
        private Entity sceneRoot;
        
        // 示例单位
        private Entity player;
        private Entity monster;
        private Entity npc;
        
        void Start()
        {
            Debug.Log("=== ET ECS 示例开始 ===");
            
            // 创建场景根实体，体现ET的树状结构设计
            CreateScene();
            
            // 创建不同类型的Unit，体现"统一为Unit，用组件区分"的设计
            CreateUnits();
            
            // 演示组件操作
            DemonstrateComponentOperations();
            
            // 演示扩展方法的使用
            DemonstrateExtensionMethods();
            
            // 演示类型分发
            DemonstrateTypeDispatch();
            
            // 演示组件挂组件的层级管理
            DemonstrateHierarchicalComponents();
        }
        
        void Update()
        {
            // 更新所有单位的移动
            UpdateUnits();
        }
        
        void OnDestroy()
        {
            // 体现ET的生命周期管理：只需要释放根实体，所有子实体会自动释放
            sceneRoot?.Dispose();
            Debug.Log("=== ET ECS 示例结束 ===");
        }
        
        /// <summary>
        /// 创建场景
        /// </summary>
        private void CreateScene()
        {
            sceneRoot = new Entity();
            Debug.Log("场景根实体创建完成");
        }
        
        /// <summary>
        /// 创建不同类型的Unit
        /// 体现ET中所有玩家、怪物都是Unit，通过组件区分类型的设计
        /// </summary>
        private void CreateUnits()
        {
            Debug.Log("\n--- 创建Unit示例 ---");
            
            // 创建玩家 - 所有单位都用同一个CreateUnit方法，体现统一性
            player = sceneRoot.CreateUnit(UnitType.Player, 1001, new Vector3(0, 0, 0));
            Debug.Log($"创建玩家: {player.GetComponent<UnitTypeComponent>().Name}");
            
            // 创建怪物 - 同样是Unit，但组件不同
            monster = sceneRoot.CreateUnit(UnitType.Monster, 2001, new Vector3(10, 0, 0));
            Debug.Log($"创建怪物: {monster.GetComponent<UnitTypeComponent>().Name}");
            
            // 创建NPC
            npc = sceneRoot.CreateUnit(UnitType.NPC, 3001, new Vector3(-5, 0, 0));
            Debug.Log($"创建NPC: {npc.GetComponent<UnitTypeComponent>().Name}");
        }
        
        /// <summary>
        /// 演示组件操作
        /// 体现ET中"一个Entity不能加两个相同的Component"等设计
        /// </summary>
        private void DemonstrateComponentOperations()
        {
            Debug.Log("\n--- 组件操作示例 ---");
            
            // 检查组件存在性
            Debug.Log($"玩家是否有血量组件: {player.HasComponent<HealthComponent>()}");
            Debug.Log($"玩家是否有AI组件: {player.HasComponent<AIComponent>()}");
            
            // 获取组件数据（体现数据和逻辑分离）
            var playerHealth = player.GetComponent<HealthComponent>();
            Debug.Log($"玩家当前血量: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
            
            var monsterAI = monster.GetComponent<AIComponent>();
            Debug.Log($"怪物AI状态: {monsterAI.CurrentState}");
            
            // 尝试添加重复组件（会失败）
            var duplicateHealth = player.AddComponent<HealthComponent>();
            Debug.Log($"尝试添加重复血量组件结果: {duplicateHealth != null}");
        }
        
        /// <summary>
        /// 演示扩展方法的使用
        /// 体现ET中所有逻辑都通过扩展方法实现的设计
        /// </summary>
        private void DemonstrateExtensionMethods()
        {
            Debug.Log("\n--- 扩展方法示例 ---");
            
            // 移动逻辑 - 通过扩展方法实现，不在组件内部
            player.MoveTo(new Vector3(5, 0, 0));
            monster.MoveTo(new Vector3(5, 0, 5));
            
            // 血量操作 - 同样通过扩展方法
            player.TakeDamage(20);
            monster.Heal(10);
            
            // AI操作
            monster.SetAITarget(player);
        }
        
        /// <summary>
        /// 演示类型分发
        /// 体现ET中"分发代替虚函数"的设计理念
        /// </summary>
        private void DemonstrateTypeDispatch()
        {
            Debug.Log("\n--- 类型分发示例 ---");
            
            // 攻击分发 - 不同类型的Unit有不同的攻击逻辑
            player.Attack(monster);
            monster.Attack(player);            
            
            // AI更新分发 - 只有有AI组件的Unit才会更新AI
            player.UpdateAI();  // 玩家没有AI组件，不会有反应
            monster.UpdateAI(); // 怪物有AI组件，会执行AI逻辑
            
            // 模拟死亡事件分发
            monster.TakeDamage(1000); // 造成致命伤害，触发死亡分发
        }
        
        /// <summary>
        /// 演示组件挂组件的层级管理
        /// 体现ET中组件也可以挂载子组件的设计
        /// </summary>
        private void DemonstrateHierarchicalComponents()
        {
            Debug.Log("\n--- 层级组件管理示例 ---");
            
            // 获取怪物的AI组件
            var monsterAI = monster.GetComponent<AIComponent>();
            if (monsterAI != null)
            {
                // AI组件下面挂载了AIStopComponent，体现层级管理
                var aiStop = monsterAI.GetComponent<AIStopComponent>();
                if (aiStop != null)
                {
                    Debug.Log($"AI停止组件检查间隔: {aiStop.CheckInterval}秒");
                    Debug.Log($"玩家检测范围: {aiStop.PlayerDetectRange}米");
                }
                
                // 可以为AI组件动态添加更多子组件
                // 比如巡逻路径组件、攻击冷却组件等
                Debug.Log("AI组件的层级生命周期管理：当AI组件被移除时，其所有子组件也会自动移除");
            }
        }
        
        /// <summary>
        /// 更新所有单位
        /// </summary>
        private void UpdateUnits()
        {
            // 更新玩家移动
            if (player != null && !player.IsDisposed)
            {
                player.UpdateMovement(Time.deltaTime);
                player.UpdateAI(); // 玩家通常没有AI，但调用是安全的
            }
            
            // 更新怪物移动和AI
            if (monster != null && !monster.IsDisposed)
            {
                monster.UpdateMovement(Time.deltaTime);
                monster.UpdateAI();
            }
            
            // 更新NPC
            if (npc != null && !npc.IsDisposed)
            {
                npc.UpdateMovement(Time.deltaTime);
                npc.UpdateAI(); // NPC可能有简单的AI
            }
        }
        
        /// <summary>
        /// GUI按钮测试
        /// </summary>
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            
            GUILayout.Label("ET ECS 测试面板", GUILayout.Height(30));
            
            if (GUILayout.Button("玩家受到伤害", GUILayout.Height(30)))
            {
                player?.TakeDamage(Random.Range(10, 30));
            }
            
            if (GUILayout.Button("怪物受到伤害", GUILayout.Height(30)))
            {
                monster?.TakeDamage(Random.Range(10, 30));
            }
            
            if (GUILayout.Button("玩家治疗", GUILayout.Height(30)))
            {
                player?.Heal(Random.Range(20, 40));
            }
            
            if (GUILayout.Button("玩家攻击怪物", GUILayout.Height(30)))
            {
                if (player != null && monster != null)
                {
                    if (player.CanAttack())
                    {
                        player.Attack(monster);
                    }
                    else
                    {
                        Debug.Log("玩家没有攻击能力！");
                    }
                }
            }
            
            if (GUILayout.Button("怪物攻击玩家", GUILayout.Height(30)))
            {
                if (monster != null && player != null)
                {
                    if (monster.CanAttack())
                    {
                        monster.Attack(player);
                    }
                    else
                    {
                        Debug.Log("怪物没有攻击能力！");
                    }
                }
            }
            
            if (GUILayout.Button("NPC尝试攻击玩家", GUILayout.Height(30)))
            {
                if (npc != null && player != null)
                {
                    if (npc.CanAttack())
                    {
                        npc.Attack(player);
                    }
                    else
                    {
                        Debug.Log("NPC没有攻击能力，无法攻击！");
                    }
                }
            }
            
            if (GUILayout.Button("显示攻击能力信息", GUILayout.Height(30)))
            {
                ShowAttackCapabilities();
            }
            
            if (GUILayout.Button("显示攻击冷却状态", GUILayout.Height(30)))
            {
                ShowAttackCooldownStatus();
            }
            
            if (GUILayout.Button("给NPC添加攻击能力", GUILayout.Height(30)))
            {
                DynamicAddAttackToNPC();
            }
            
            if (GUILayout.Button("移除NPC攻击能力", GUILayout.Height(30)))
            {
                DynamicRemoveAttackFromNPC();
            }
            
            if (GUILayout.Button("重新创建怪物", GUILayout.Height(30)))
            {
                monster?.Dispose(); // 体现生命周期管理
                monster = sceneRoot.CreateUnit(UnitType.Monster, 2002, new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10)));
            }
            
            if (GUILayout.Button("测试距离检查", GUILayout.Height(30)))
            {
                TestAttackDistance();
            }
            
            if (GUILayout.Button("玩家移动到攻击范围", GUILayout.Height(30)))
            {
                if (player != null && monster != null && player.CanAttack())
                {
                    player.MoveToAttackRange(monster);
                }
                else
                {
                    Debug.Log("玩家或怪物不存在，或玩家没有攻击能力！");
                }
            }
            
            if (GUILayout.Button("强制攻击测试", GUILayout.Height(30)))
            {
                if (player != null && monster != null)
                {
                    player.ForceAttack(monster);
                }
                else
                {
                    Debug.Log("玩家或怪物不存在！");
                }
            }
            
            if (GUILayout.Button("显示场景信息", GUILayout.Height(30)))
            {
                ShowSceneInfo();
            }
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 显示攻击能力信息
        /// </summary>
        private void ShowAttackCapabilities()
        {
            Debug.Log("\n--- 单位攻击能力信息 ---");
            
            if (player != null)
            {
                Debug.Log($"玩家攻击能力: {player.CanAttack()}");
                if (player.CanAttack())
                {
                    var attack = player.GetAttackComponent();
                    Debug.Log($"  攻击力: {attack.AttackPower}, 攻击范围: {attack.AttackRange}, 冷却时间: {attack.AttackCooldown}秒");
                    Debug.Log($"  暴击率: {attack.CriticalChance}%, 暴击倍数: {attack.CriticalMultiplier}倍");
                    Debug.Log($"  攻击类型: {attack.AttackType}, 伤害类型: {attack.DamageType}");
                }
            }
            
            if (monster != null)
            {
                Debug.Log($"怪物攻击能力: {monster.CanAttack()}");
                if (monster.CanAttack())
                {
                    var attack = monster.GetAttackComponent();
                    Debug.Log($"  攻击力: {attack.AttackPower}, 攻击范围: {attack.AttackRange}, 冷却时间: {attack.AttackCooldown}秒");
                    Debug.Log($"  暴击率: {attack.CriticalChance}%, 暴击倍数: {attack.CriticalMultiplier}倍");
                    Debug.Log($"  攻击类型: {attack.AttackType}, 伤害类型: {attack.DamageType}");
                }
            }
            
            if (npc != null)
            {
                Debug.Log($"NPC攻击能力: {npc.CanAttack()}");
                if (npc.CanAttack())
                {
                    var attack = npc.GetAttackComponent();
                    Debug.Log($"  攻击力: {attack.AttackPower}, 攻击范围: {attack.AttackRange}, 冷却时间: {attack.AttackCooldown}秒");
                    Debug.Log($"  暴击率: {attack.CriticalChance}%, 暴击倍数: {attack.CriticalMultiplier}倍");
                    Debug.Log($"  攻击类型: {attack.AttackType}, 伤害类型: {attack.DamageType}");
                }
                else
                {
                    Debug.Log("  NPC没有攻击组件，无法进行攻击");
                }
            }
        }
        
        /// <summary>
        /// 显示攻击冷却状态
        /// </summary>
        private void ShowAttackCooldownStatus()
        {
            Debug.Log("\n--- 攻击冷却状态 ---");
            
            if (player != null && player.CanAttack())
            {
                var attack = player.GetAttackComponent();
                float timeSinceLastAttack = Time.time - attack.LastAttackTime;
                bool canAttack = attack.CanAttack;
                float remainingCooldown = canAttack ? 0f : attack.AttackCooldown - timeSinceLastAttack;
                
                Debug.Log($"玩家 {player.GetComponent<UnitTypeComponent>().Name}:");
                Debug.Log($"  距离上次攻击: {timeSinceLastAttack:F2}秒");
                Debug.Log($"  可以攻击: {canAttack}");
                if (!canAttack)
                {
                    Debug.Log($"  剩余冷却时间: {remainingCooldown:F2}秒");
                }
            }
            
            if (monster != null && monster.CanAttack())
            {
                var attack = monster.GetAttackComponent();
                float timeSinceLastAttack = Time.time - attack.LastAttackTime;
                bool canAttack = attack.CanAttack;
                float remainingCooldown = canAttack ? 0f : attack.AttackCooldown - timeSinceLastAttack;
                
                Debug.Log($"怪物 {monster.GetComponent<UnitTypeComponent>().Name}:");
                Debug.Log($"  距离上次攻击: {timeSinceLastAttack:F2}秒");
                Debug.Log($"  可以攻击: {canAttack}");
                if (!canAttack)
                {
                    Debug.Log($"  剩余冷却时间: {remainingCooldown:F2}秒");
                }
            }
            
            if (npc != null && npc.CanAttack())
            {
                var attack = npc.GetAttackComponent();
                float timeSinceLastAttack = Time.time - attack.LastAttackTime;
                bool canAttack = attack.CanAttack;
                float remainingCooldown = canAttack ? 0f : attack.AttackCooldown - timeSinceLastAttack;
                
                Debug.Log($"NPC {npc.GetComponent<UnitTypeComponent>().Name}:");
                Debug.Log($"  距离上次攻击: {timeSinceLastAttack:F2}秒");
                Debug.Log($"  可以攻击: {canAttack}");
                if (!canAttack)
                {
                    Debug.Log($"  剩余冷却时间: {remainingCooldown:F2}秒");
                }
            }
        }
        
        /// <summary>
        /// 动态给NPC添加攻击能力
        /// 体现ET ECS的动态组件管理特性
        /// </summary>
        private void DynamicAddAttackToNPC()
        {
            if (npc == null)
            {
                Debug.Log("NPC不存在！");
                return;
            }
            
            if (npc.CanAttack())
            {
                Debug.Log("NPC已经具备攻击能力！");
                return;
            }
            
            // 动态添加攻击组件
            var attackComponent = npc.AddComponent<AttackComponent>();
            attackComponent.AttackPower = 15;
            attackComponent.AttackRange = 1.0f;
            attackComponent.AttackCooldown = 2.0f;
            attackComponent.CriticalChance = 3;
            attackComponent.CriticalMultiplier = 1.2f;
            attackComponent.AttackType = AttackType.Melee;
            attackComponent.DamageType = DamageType.Physical;
            
            Debug.Log($"已为NPC {npc.GetComponent<UnitTypeComponent>().Name} 添加攻击能力！");
            Debug.Log($"攻击力: {attackComponent.AttackPower}, 攻击范围: {attackComponent.AttackRange}");
        }
        
        /// <summary>
        /// 动态移除NPC的攻击能力
        /// 体现ET ECS的动态组件管理特性
        /// </summary>
        private void DynamicRemoveAttackFromNPC()
        {
            if (npc == null)
            {
                Debug.Log("NPC不存在！");
                return;
            }
            
            if (!npc.CanAttack())
            {
                Debug.Log("NPC本来就没有攻击能力！");
                return;
            }
            
            // 动态移除攻击组件
            npc.RemoveComponent<AttackComponent>();
            
            Debug.Log($"已移除NPC {npc.GetComponent<UnitTypeComponent>().Name} 的攻击能力！");
        }
        
        /// <summary>
        /// 显示场景信息
        /// 体现ET的树状结构管理
        /// </summary>
        private void ShowSceneInfo()
        {
            Debug.Log("\n--- 场景信息 ---");
            Debug.Log($"场景根实体ID: {sceneRoot.Id}");
            
            var children = sceneRoot.GetChildren();
            Debug.Log($"场景中的Unit数量: {children.Length}");
            
            foreach (var child in children)
            {
                if (child.HasComponent<UnitTypeComponent>())
                {
                    var unitType = child.GetComponent<UnitTypeComponent>();
                    var health = child.GetComponent<HealthComponent>();
                    var movement = child.GetComponent<MovementComponent>();
                    
                    Debug.Log($"Unit: {unitType.Name}, 类型: {unitType.UnitType}, 血量: {health.CurrentHealth}/{health.MaxHealth}, 位置: {movement.Position}");
                    Debug.Log($"  攻击能力: {child.CanAttack()}");
                }
            }
        }
        
        /// <summary>
        /// 测试攻击距离检查
        /// 体现ET ECS中攻击系统的完整逻辑
        /// </summary>
        private void TestAttackDistance()
        {
            Debug.Log("\n--- 攻击距离测试 ---");
            
            if (player == null || monster == null)
            {
                Debug.Log("玩家或怪物不存在，无法测试距离！");
                return;
            }
            
            var playerMovement = player.GetComponent<MovementComponent>();
            var monsterMovement = monster.GetComponent<MovementComponent>();
            var playerAttack = player.GetAttackComponent();
            var monsterAttack = monster.GetAttackComponent();
            
            if (playerMovement != null && monsterMovement != null)
            {
                float distance = Vector3.Distance(playerMovement.Position, monsterMovement.Position);
                Debug.Log($"玩家位置: {playerMovement.Position}");
                Debug.Log($"怪物位置: {monsterMovement.Position}");
                Debug.Log($"两者距离: {distance:F2}米");
                
                if (playerAttack != null)
                {
                    Debug.Log($"玩家攻击范围: {playerAttack.AttackRange}米");
                    Debug.Log($"玩家能否攻击怪物: {distance <= playerAttack.AttackRange}");
                }
                
                if (monsterAttack != null)
                {
                    Debug.Log($"怪物攻击范围: {monsterAttack.AttackRange}米");
                    Debug.Log($"怪物能否攻击玩家: {distance <= monsterAttack.AttackRange}");
                }
                
                // 测试移动到攻击范围
                if (playerAttack != null && distance > playerAttack.AttackRange)
                {
                    Vector3 direction = (monsterMovement.Position - playerMovement.Position).normalized;
                    Vector3 attackPosition = monsterMovement.Position - direction * (playerAttack.AttackRange * 0.9f);
                    Debug.Log($"建议玩家移动到: {attackPosition} 以便攻击怪物");
                }
            }
        }
    }
}

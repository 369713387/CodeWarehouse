using System;
using System.Collections.Generic;
using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// 类型分发器
    /// 体现了ET中"分发代替虚函数"的设计理念
    /// 根据不同的UnitType分发到不同的Handler处理，替代面向对象的虚函数
    /// </summary>
    public static class TypeDispatcher
    {
        // 死亡处理器字典
        private static Dictionary<UnitType, Action<Entity>> deathHandlers = new Dictionary<UnitType, Action<Entity>>();
        
        // AI更新处理器字典
        private static Dictionary<UnitType, Action<Entity>> aiUpdateHandlers = new Dictionary<UnitType, Action<Entity>>();
        
        // 攻击处理器字典
        private static Dictionary<UnitType, Action<Entity, Entity>> attackHandlers = new Dictionary<UnitType, Action<Entity, Entity>>();
        
        static TypeDispatcher()
        {
            RegisterHandlers();
        }
        
        /// <summary>
        /// 注册所有处理器
        /// 这里体现了ET中根据类型分发的理念，而不是使用继承和虚函数
        /// </summary>
        private static void RegisterHandlers()
        {
            // 注册死亡处理器
            deathHandlers[UnitType.Player] = HandlePlayerDeath;
            deathHandlers[UnitType.Monster] = HandleMonsterDeath;
            deathHandlers[UnitType.NPC] = HandleNPCDeath;
            deathHandlers[UnitType.Pet] = HandlePetDeath;
            
            // 注册AI更新处理器
            aiUpdateHandlers[UnitType.Monster] = HandleMonsterAI;
            aiUpdateHandlers[UnitType.Pet] = HandlePetAI;
            
            // ⭐ 只为有攻击能力的类型注册攻击处理器
            attackHandlers[UnitType.Player] = HandlePlayerAttack;
            attackHandlers[UnitType.Monster] = HandleMonsterAttack;
            attackHandlers[UnitType.NPC] = HandleNPCAttack; // NPC在获得攻击组件后也能使用分发
            // attackHandlers[UnitType.Pet] = HandlePetAttack; // 如果宠物也能攻击的话
        }
        
        /// <summary>
        /// 分发死亡事件
        /// 传统OOP会为每个子类写一个OnDeath虚函数
        /// ET中统一用这个分发方法，根据UnitType调用不同的处理器
        /// </summary>
        public static void DispatchDeath(Entity unit)
        {
            if (!unit.IsValidUnit())
                return;
                
            UnitType unitType = unit.GetUnitType();
            if (deathHandlers.TryGetValue(unitType, out Action<Entity> handler))
            {
                handler(unit);
            }
            else
            {
                Debug.LogWarning($"未找到UnitType: {unitType} 的死亡处理器");
            }
        }
        
        /// <summary>
        /// 分发AI更新事件
        /// </summary>
        public static void DispatchAIUpdate(Entity unit)
        {
            if (!unit.IsValidUnit() || unit.IsDead())
                return;
                
            UnitType unitType = unit.GetUnitType();
            if (aiUpdateHandlers.TryGetValue(unitType, out Action<Entity> handler))
            {
                handler(unit);
            }
        }
        
        /// <summary>
        /// 分发攻击事件
        /// </summary>
        public static void DispatchAttack(Entity attacker, Entity target)
        {
            if (!attacker.IsValidUnit() || !target.IsValidUnit())
                return;
                
            UnitType attackerType = attacker.GetUnitType();
            if (attackHandlers.TryGetValue(attackerType, out Action<Entity, Entity> handler))
            {
                handler(attacker, target);
            }
        }
        
        #region 具体的处理器实现
        
        /// <summary>
        /// 玩家死亡处理器
        /// 不同类型的死亡逻辑写在不同的方法中，而不是虚函数
        /// </summary>
        private static void HandlePlayerDeath(Entity player)
        {
            Debug.Log($"玩家死亡: {player.GetComponent<UnitTypeComponent>().Name}");
            
            var healthComponent = player.GetComponent<HealthComponent>();
            healthComponent.IsDead = true;
            healthComponent.CurrentHealth = 0;
            
            // 玩家死亡特有逻辑
            // 比如掉落物品、复活倒计时、经验惩罚等
            
            // 停止移动
            var movementComponent = player.GetComponent<MovementComponent>();
            if (movementComponent != null)
            {
                movementComponent.IsMoving = false;
            }
        }

        /// <summary>
        /// 怪物死亡处理器
        /// </summary>
        private static void HandleMonsterDeath(Entity monster)
        {
            Debug.Log($"怪物死亡: {monster.GetComponent<UnitTypeComponent>().Name}");

            var healthComponent = monster.GetComponent<HealthComponent>();
            healthComponent.IsDead = true;
            healthComponent.CurrentHealth = 0;

            // 怪物死亡特有逻辑
            // 比如掉落战利品、给玩家经验、刷新计时等

            // 停止AI
            var aiComponent = monster.GetComponent<AIComponent>();
            if (aiComponent != null)
            {
                aiComponent.ChangeState(AIState.Dead);
            }

            // 延迟销毁怪物
            // 实际项目中可能用定时器组件
            //MonoBehaviour.Destroy(monster as UnityEngine.Object, 5f);
            monster.Dispose();
        }
        
        /// <summary>
        /// NPC死亡处理器
        /// </summary>
        private static void HandleNPCDeath(Entity npc)
        {
            Debug.Log($"NPC死亡: {npc.GetComponent<UnitTypeComponent>().Name}");
            
            // NPC死亡逻辑
            // 通常NPC不会死亡，或者有特殊的复活机制
        }
        
        /// <summary>
        /// 宠物死亡处理器
        /// </summary>
        private static void HandlePetDeath(Entity pet)
        {
            Debug.Log($"宠物死亡: {pet.GetComponent<UnitTypeComponent>().Name}");
            
            // 宠物死亡逻辑
            // 比如回到宠物空间、复活药剂等
        }
        
        /// <summary>
        /// 怪物AI更新处理器
        /// </summary>
        private static void HandleMonsterAI(Entity monster)
        {
            var aiComponent = monster.GetComponent<AIComponent>();
            if (aiComponent == null || !aiComponent.ShouldUpdate)
                return;
                
            aiComponent.LastUpdateTime = Time.time;
            
            // 检查是否需要停止AI更新（性能优化）
            var aiStopComponent = aiComponent.GetComponent<AIStopComponent>();
            if (aiStopComponent != null && aiStopComponent.ShouldCheck)
            {
                aiStopComponent.LastCheckTime = Time.time;
                // 检查周围是否有玩家，没有的话停止AI更新
                // 这里简化处理，实际项目中会查询附近的玩家
                aiStopComponent.IsAIStopped = false; // 假设有玩家在附近
            }
            
            if (aiStopComponent?.IsAIStopped == true)
                return;
                
            // 根据当前状态执行AI逻辑
            switch (aiComponent.CurrentState)
            {
                case AIState.Idle:
                    // 待机状态：检测周围敌人
                    break;
                case AIState.Chase:
                    // 追击状态：向目标移动
                    break;
                case AIState.Attack:
                    // 攻击状态：攻击目标
                    break;
                case AIState.Return:
                    // 返回状态：返回出生点
                    break;
            }
        }
        
        /// <summary>
        /// 宠物AI更新处理器
        /// </summary>
        private static void HandlePetAI(Entity pet)
        {
            // 宠物AI逻辑，通常跟随主人
        }
        
        /// <summary>
        /// 玩家攻击处理器
        /// </summary>
        private static void HandlePlayerAttack(Entity player, Entity target)
        {
            Debug.Log($"玩家攻击: {player.GetComponent<UnitTypeComponent>().Name} -> {target.GetComponent<UnitTypeComponent>().Name}");
            
            // 玩家攻击逻辑
            // 计算伤害、技能效果、暴击等
        }
        
        /// <summary>
        /// 怪物攻击处理器
        /// </summary>
        private static void HandleMonsterAttack(Entity monster, Entity target)
        {
            Debug.Log($"怪物攻击: {monster.GetComponent<UnitTypeComponent>().Name} -> {target.GetComponent<UnitTypeComponent>().Name}");
            
            // 怪物攻击逻辑
            // 通常比玩家攻击简单一些
        }
        
        /// <summary>
        /// NPC攻击处理器
        /// 只有在NPC动态获得攻击组件后才会被调用
        /// </summary>
        private static void HandleNPCAttack(Entity npc, Entity target)
        {
            Debug.Log($"NPC攻击: {npc.GetComponent<UnitTypeComponent>().Name} -> {target.GetComponent<UnitTypeComponent>().Name}");
            
            // NPC攻击逻辑
            // 通常NPC不会主动攻击，但获得攻击能力后可以反击
            Debug.Log("这是一个具备攻击能力的特殊NPC！");
        }
        
        #endregion
    }
}

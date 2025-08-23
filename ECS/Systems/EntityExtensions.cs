using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// Entity扩展方法
    /// 体现了ET中用扩展方法实现逻辑与数据分离的设计理念
    /// 所有逻辑都写在扩展方法中，而不是写在Entity或Component类内部
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// 创建Unit的工厂方法
        /// 统一创建Unit，通过组件区分不同类型
        /// </summary>
        public static Entity CreateUnit(this Entity parent, UnitType unitType, int configId, Vector3 position)
        {
            Entity unit = new Entity();
            unit.SetParent(parent);
            
            // 添加单位类型组件
            var unitTypeComponent = unit.AddComponent<UnitTypeComponent>();
            unitTypeComponent.UnitType = unitType;
            unitTypeComponent.ConfigId = configId;
            unitTypeComponent.Name = $"{unitType}_{configId}";
            unitTypeComponent.Level = 1;
            
            // 添加血量组件
            var healthComponent = unit.AddComponent<HealthComponent>();
            healthComponent.MaxHealth = 100;
            healthComponent.CurrentHealth = 100;
            healthComponent.IsDead = false;
            
            // 添加移动组件
            var movementComponent = unit.AddComponent<MovementComponent>();
            movementComponent.Position = position;
            movementComponent.TargetPosition = position;
            movementComponent.MoveSpeed = 5f;
            movementComponent.StopDistance = 0.5f;
            movementComponent.IsMoving = false;
            
            // 根据单位类型添加特定组件
            switch (unitType)
            {
                case UnitType.Monster:
                    unit.AddMonsterComponents();
                    break;
                case UnitType.Player:
                    unit.AddPlayerComponents();
                    break;
                case UnitType.NPC:
                    unit.AddNPCComponents();
                    break;
            }
            
            return unit;
        }
        
        /// <summary>
        /// 为怪物添加特定组件
        /// </summary>
        private static void AddMonsterComponents(this Entity unit)
        {
            // 添加AI组件
            var aiComponent = unit.AddComponent<AIComponent>();
            aiComponent.CurrentState = AIState.Idle;
            aiComponent.UpdateInterval = 0.1f;
            aiComponent.DetectRange = 10f;
            aiComponent.AttackRange = 2f;
            aiComponent.ReturnRange = 20f;
            aiComponent.SpawnPosition = unit.GetComponent<MovementComponent>().Position;
            
            // 为AI组件添加停止管理组件（体现组件挂组件的层级管理）
            var aiStopComponent = aiComponent.AddComponent<AIStopComponent>();
            aiStopComponent.CheckInterval = 2f;
            aiStopComponent.PlayerDetectRange = 50f;
            
            // 怪物也可以攻击，添加攻击组件
            var attackComponent = unit.AddComponent<AttackComponent>();
            attackComponent.AttackPower = 20;
            attackComponent.AttackRange = 2.0f;
            attackComponent.AttackCooldown = 1.5f;
            attackComponent.CriticalChance = 5;
            attackComponent.CriticalMultiplier = 1.3f;
            attackComponent.AttackType = AttackType.Melee;
            attackComponent.DamageType = DamageType.Physical;
        }
        
        /// <summary>
        /// 为玩家添加特定组件
        /// </summary>
        private static void AddPlayerComponents(this Entity unit)
        {
            // 玩家可以攻击，添加攻击组件
            var attackComponent = unit.AddComponent<AttackComponent>();
            attackComponent.AttackPower = 25;
            attackComponent.AttackRange = 1.5f;
            attackComponent.AttackCooldown = 1.0f;
            attackComponent.CriticalChance = 10;
            attackComponent.CriticalMultiplier = 1.5f;
            attackComponent.AttackType = AttackType.Melee;
            attackComponent.DamageType = DamageType.Physical;
            
            // 玩家特有组件可以在这里添加
            // 比如背包组件、技能组件等
        }
        
        /// <summary>
        /// 为NPC添加特定组件
        /// </summary>
        private static void AddNPCComponents(this Entity unit)
        {
            // NPC无法攻击，不添加AttackComponent
            // 只添加NPC特有组件
            // 比如对话组件、任务组件等
            
            Debug.Log($"NPC {unit.GetComponent<UnitTypeComponent>().Name} 创建完成，无攻击能力");
        }
        
        /// <summary>
        /// 检查Unit是否有效
        /// </summary>
        public static bool IsValidUnit(this Entity unit)
        {
            return unit != null && 
                   !unit.IsDisposed && 
                   unit.HasComponent<UnitTypeComponent>() && 
                   unit.HasComponent<HealthComponent>();
        }
        
        /// <summary>
        /// 获取Unit类型
        /// </summary>
        public static UnitType GetUnitType(this Entity unit)
        {
            var unitTypeComponent = unit.GetComponent<UnitTypeComponent>();
            return unitTypeComponent?.UnitType ?? UnitType.Player;
        }
        
        /// <summary>
        /// 检查Unit是否死亡
        /// </summary>
        public static bool IsDead(this Entity unit)
        {
            var healthComponent = unit.GetComponent<HealthComponent>();
            return healthComponent?.IsDead ?? false;
        }
        
        /// <summary>
        /// 检查单位是否具备攻击能力
        /// </summary>
        public static bool CanAttack(this Entity unit)
        {
            return unit.HasComponent<AttackComponent>();
        }
        
        /// <summary>
        /// 获取攻击组件
        /// </summary>
        public static AttackComponent GetAttackComponent(this Entity unit)
        {
            return unit.GetComponent<AttackComponent>();
        }
        
        /// <summary>
        /// 设置攻击力
        /// </summary>
        public static void SetAttackPower(this Entity unit, int attackPower)
        {
            var attackComponent = unit.GetComponent<AttackComponent>();
            if (attackComponent != null)
            {
                attackComponent.AttackPower = attackPower;
            }
        }
    }
}

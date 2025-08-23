using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// 游戏逻辑扩展方法
    /// 体现了ET中所有逻辑都通过扩展方法实现的设计理念
    /// </summary>
    public static class GameLogicExtensions
    {
        #region 血量相关逻辑
        
        /// <summary>
        /// 造成伤害
        /// 逻辑与数据分离，不在HealthComponent内部实现
        /// </summary>
        public static void TakeDamage(this Entity unit, int damage, Entity attacker = null)
        {
            var healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent == null || healthComponent.IsDead)
                return;
                
            healthComponent.CurrentHealth = Mathf.Max(0, healthComponent.CurrentHealth - damage);
            
            Debug.Log($"{unit.GetComponent<UnitTypeComponent>().Name} 受到 {damage} 点伤害，剩余血量: {healthComponent.CurrentHealth}");
            
            // 检查是否死亡
            if (healthComponent.CurrentHealth <= 0 && !healthComponent.IsDead)
            {
                // 使用类型分发处理死亡，而不是虚函数
                TypeDispatcher.DispatchDeath(unit);
            }
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
        public static void Heal(this Entity unit, int healAmount)
        {
            var healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent == null || healthComponent.IsDead)
                return;
                
            int oldHealth = healthComponent.CurrentHealth;
            healthComponent.CurrentHealth = Mathf.Min(healthComponent.MaxHealth, healthComponent.CurrentHealth + healAmount);
            
            int actualHeal = healthComponent.CurrentHealth - oldHealth;
            if (actualHeal > 0)
            {
                Debug.Log($"{unit.GetComponent<UnitTypeComponent>().Name} 恢复了 {actualHeal} 点血量");
            }
        }
        
        /// <summary>
        /// 设置最大血量
        /// </summary>
        public static void SetMaxHealth(this Entity unit, int maxHealth)
        {
            var healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent == null)
                return;
                
            healthComponent.MaxHealth = maxHealth;
            // 如果当前血量超过新的最大血量，则调整当前血量
            if (healthComponent.CurrentHealth > maxHealth)
            {
                healthComponent.CurrentHealth = maxHealth;
            }
        }
        
        #endregion
        
        #region 移动相关逻辑
        
        /// <summary>
        /// 设置移动目标
        /// </summary>
        public static void MoveTo(this Entity unit, Vector3 targetPosition)
        {
            var movementComponent = unit.GetComponent<MovementComponent>();
            if (movementComponent == null || unit.IsDead())
                return;
                
            movementComponent.TargetPosition = targetPosition;
            movementComponent.Direction = (targetPosition - movementComponent.Position).normalized;
            movementComponent.IsMoving = true;
            
            Debug.Log($"{unit.GetComponent<UnitTypeComponent>().Name} 开始移动到 {targetPosition}");
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public static void StopMovement(this Entity unit)
        {
            var movementComponent = unit.GetComponent<MovementComponent>();
            if (movementComponent == null)
                return;
                
            movementComponent.IsMoving = false;
            movementComponent.Direction = Vector3.zero;
        }
        
        /// <summary>
        /// 更新移动（在Update中调用）
        /// </summary>
        public static void UpdateMovement(this Entity unit, float deltaTime)
        {
            var movementComponent = unit.GetComponent<MovementComponent>();
            if (movementComponent == null || !movementComponent.IsMoving || unit.IsDead())
                return;
                
            // 检查是否到达目标
            if (movementComponent.HasReachedTarget)
            {
                movementComponent.Position = movementComponent.TargetPosition;
                unit.StopMovement();
                return;
            }
            
            // 移动
            Vector3 moveDistance = movementComponent.Direction * movementComponent.MoveSpeed * deltaTime;
            movementComponent.Position += moveDistance;
        }
        
        /// <summary>
        /// 瞬移到指定位置
        /// </summary>
        public static void Teleport(this Entity unit, Vector3 position)
        {
            var movementComponent = unit.GetComponent<MovementComponent>();
            if (movementComponent == null)
                return;
                
            movementComponent.Position = position;
            movementComponent.TargetPosition = position;
            unit.StopMovement();
            
            Debug.Log($"{unit.GetComponent<UnitTypeComponent>().Name} 瞬移到 {position}");
        }
        
        #endregion
        
        #region AI相关逻辑
        
        /// <summary>
        /// 更新AI
        /// </summary>
        public static void UpdateAI(this Entity unit)
        {
            if (!unit.IsValidUnit() || unit.IsDead())
                return;
                
            // 使用类型分发更新AI，不同类型的Unit有不同的AI逻辑
            TypeDispatcher.DispatchAIUpdate(unit);
        }
        
        /// <summary>
        /// 设置AI目标
        /// </summary>
        public static void SetAITarget(this Entity unit, Entity target)
        {
            var aiComponent = unit.GetComponent<AIComponent>();
            if (aiComponent == null)
                return;
                
            aiComponent.Target = target;
            aiComponent.TargetId = target?.Id ?? 0;
            
            if (target != null)
            {
                Debug.Log($"{unit.GetComponent<UnitTypeComponent>().Name} 设置AI目标: {target.GetComponent<UnitTypeComponent>().Name}");
            }
        }
        
        /// <summary>
        /// 清除AI目标
        /// </summary>
        public static void ClearAITarget(this Entity unit)
        {
            var aiComponent = unit.GetComponent<AIComponent>();
            if (aiComponent == null)
                return;
                
            aiComponent.Target = null;
            aiComponent.TargetId = 0;
        }
        
        #endregion
        
        #region 攻击相关逻辑
        
        /// <summary>
        /// 攻击目标
        /// </summary>
        public static void Attack(this Entity attacker, Entity target)
        {
            if (!attacker.IsValidUnit() || !target.IsValidUnit() || 
                attacker.IsDead() || target.IsDead())
                return;
                
            // ⭐ 关键检查：只有具备AttackComponent的单位才能攻击
            var attackComponent = attacker.GetComponent<AttackComponent>();
            if (attackComponent == null)
            {
                Debug.Log($"{attacker.GetComponent<UnitTypeComponent>().Name} 没有攻击能力，无法攻击！");
                return;
            }
            
            // 检查攻击冷却
            if (!attackComponent.CanAttack)
            {
                Debug.Log($"{attacker.GetComponent<UnitTypeComponent>().Name} 攻击冷却中，无法攻击！");
                return;
            }
            
            // 检查攻击距离
            var attackerMovement = attacker.GetComponent<MovementComponent>();
            var targetMovement = target.GetComponent<MovementComponent>();
            
            if (attackerMovement != null && targetMovement != null)
            {
                float distance = Vector3.Distance(attackerMovement.Position, targetMovement.Position);
                
                if (distance > attackComponent.AttackRange)
                {
                    Debug.Log($"{attacker.GetComponent<UnitTypeComponent>().Name} 距离目标太远，无法攻击");
                    return;
                }
            }
            
            // 记录攻击时间
            attackComponent.LastAttackTime = Time.time;
            
            // 使用类型分发处理攻击，不同类型的攻击逻辑不同
            TypeDispatcher.DispatchAttack(attacker, target);
            
            // 计算并造成伤害
            int damage = attackComponent.GetActualDamage();
            target.TakeDamage(damage, attacker);
        }
        
        /// <summary>
        /// 检查是否在攻击范围内
        /// </summary>
        public static bool IsInAttackRange(this Entity attacker, Entity target)
        {
            var attackComponent = attacker.GetComponent<AttackComponent>();
            if (attackComponent == null)
                return false;
                
            var attackerMovement = attacker.GetComponent<MovementComponent>();
            var targetMovement = target.GetComponent<MovementComponent>();
            
            if (attackerMovement == null || targetMovement == null)
                return false;
                
            float distance = Vector3.Distance(attackerMovement.Position, targetMovement.Position);
            return distance <= attackComponent.AttackRange;
        }
        
        /// <summary>
        /// 获取到目标的距离
        /// </summary>
        public static float GetDistanceTo(this Entity unit, Entity target)
        {
            var unitMovement = unit.GetComponent<MovementComponent>();
            var targetMovement = target.GetComponent<MovementComponent>();
            
            if (unitMovement == null || targetMovement == null)
                return float.MaxValue;
                
            return Vector3.Distance(unitMovement.Position, targetMovement.Position);
        }
        
        /// <summary>
        /// 移动到攻击范围内
        /// </summary>
        public static void MoveToAttackRange(this Entity attacker, Entity target)
        {
            var attackComponent = attacker.GetComponent<AttackComponent>();
            if (attackComponent == null)
            {
                Debug.Log($"{attacker.GetComponent<UnitTypeComponent>().Name} 没有攻击能力，无法移动到攻击范围！");
                return;
            }
            
            var attackerMovement = attacker.GetComponent<MovementComponent>();
            var targetMovement = target.GetComponent<MovementComponent>();
            
            if (attackerMovement == null || targetMovement == null)
                return;
                
            // 计算移动到攻击范围边缘的位置
            Vector3 direction = (targetMovement.Position - attackerMovement.Position).normalized;
            Vector3 attackPosition = targetMovement.Position - direction * (attackComponent.AttackRange * 0.8f);
            
            attacker.MoveTo(attackPosition);
            Debug.Log($"{attacker.GetComponent<UnitTypeComponent>().Name} 移动到攻击范围内: {attackPosition}");
        }
        
        /// <summary>
        /// 强制攻击（忽略距离和冷却）
        /// 主要用于测试
        /// </summary>
        public static void ForceAttack(this Entity attacker, Entity target)
        {
            if (!attacker.IsValidUnit() || !target.IsValidUnit() || 
                attacker.IsDead() || target.IsDead())
                return;
                
            var attackComponent = attacker.GetComponent<AttackComponent>();
            if (attackComponent == null)
            {
                Debug.Log($"{attacker.GetComponent<UnitTypeComponent>().Name} 没有攻击能力，无法强制攻击！");
                return;
            }
            
            // 重置攻击时间，强制可以攻击
            attackComponent.LastAttackTime = Time.time - attackComponent.AttackCooldown;
            
            Debug.Log($"强制攻击：{attacker.GetComponent<UnitTypeComponent>().Name} -> {target.GetComponent<UnitTypeComponent>().Name}");
            
            // 使用类型分发处理攻击
            TypeDispatcher.DispatchAttack(attacker, target);
            
            // 计算并造成伤害
            int damage = attackComponent.GetActualDamage();
            target.TakeDamage(damage, attacker);
            
            // 更新攻击时间
            attackComponent.LastAttackTime = Time.time;
        }
        
        #endregion
    }
}

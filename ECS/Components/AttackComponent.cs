using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// 攻击组件 - 纯数据，不包含逻辑
    /// 只有具备攻击能力的单位才会挂载此组件
    /// 体现了ET中"组件代替继承"的设计理念
    /// </summary>
    public class AttackComponent : Entity
    {
        public int AttackPower;         // 攻击力
        public float AttackRange;       // 攻击范围
        public float AttackCooldown;    // 攻击冷却时间
        public float LastAttackTime;    // 上次攻击时间
        public int CriticalChance;      // 暴击率（百分比）
        public float CriticalMultiplier; // 暴击倍数
        
        // 攻击类型相关
        public AttackType AttackType;   // 攻击类型
        public DamageType DamageType;   // 伤害类型
        
        /// <summary>
        /// 是否可以攻击（冷却时间检查）
        /// </summary>
        public bool CanAttack => Time.time - LastAttackTime >= AttackCooldown;
        
        /// <summary>
        /// 计算实际伤害（包含暴击）
        /// </summary>
        public int GetActualDamage()
        {
            int damage = AttackPower;
            
            // 暴击计算
            if (Random.Range(0, 100) < CriticalChance)
            {
                damage = Mathf.RoundToInt(damage * CriticalMultiplier);
                Debug.Log("暴击！");
            }
            
            return damage;
        }
    }
    
    /// <summary>
    /// 攻击类型枚举
    /// </summary>
    public enum AttackType
    {
        Melee = 1,      // 近战
        Ranged = 2,     // 远程
        Magic = 3,      // 魔法
    }
    
    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    public enum DamageType
    {
        Physical = 1,   // 物理伤害
        Magic = 2,      // 魔法伤害
        Pure = 3,       // 纯粹伤害
    }
}

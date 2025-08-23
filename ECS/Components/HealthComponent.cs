using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// 血量组件 - 纯数据，不包含逻辑
    /// 体现了ET中EC是纯数据的设计理念
    /// </summary>
    public class HealthComponent : Entity
    {
        public int MaxHealth;       // 最大血量
        public int CurrentHealth;   // 当前血量
        public bool IsDead;         // 是否死亡
        
        /// <summary>
        /// 血量百分比（只读属性）
        /// </summary>
        public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
        
        /// <summary>
        /// 是否满血
        /// </summary>
        public bool IsFullHealth => CurrentHealth >= MaxHealth;
        
        /// <summary>
        /// 是否濒死（血量低于20%）
        /// </summary>
        public bool IsLowHealth => HealthPercent < 0.2f;
    }
}

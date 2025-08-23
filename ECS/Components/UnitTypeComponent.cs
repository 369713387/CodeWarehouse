using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// 单位类型枚举
    /// 用于区分不同类型的Unit，替代继承和多态
    /// </summary>
    public enum UnitType
    {
        Player = 1,     // 玩家
        Monster = 2,    // 怪物
        NPC = 3,        // NPC
        Pet = 4,        // 宠物
    }
    
    /// <summary>
    /// 单位类型组件
    /// 所有玩家、怪物都是Unit，通过这个组件来区分类型
    /// 体现了ET中"组件代替继承"的设计理念
    /// </summary>
    public class UnitTypeComponent : Entity
    {
        public UnitType UnitType;
        public int ConfigId;        // 配置表ID，用于读取配置数据
        public string Name;         // 单位名称
        public int Level;          // 等级
        
        /// <summary>
        /// 检查是否为玩家
        /// </summary>
        public bool IsPlayer => UnitType == UnitType.Player;
        
        /// <summary>
        /// 检查是否为怪物
        /// </summary>
        public bool IsMonster => UnitType == UnitType.Monster;
        
        /// <summary>
        /// 检查是否为NPC
        /// </summary>
        public bool IsNPC => UnitType == UnitType.NPC;
        
        /// <summary>
        /// 检查是否为宠物
        /// </summary>
        public bool IsPet => UnitType == UnitType.Pet;
    }
}

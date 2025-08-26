using UnityEngine;
using CodeWarehouse.GAS;

namespace CodeWarehouse.GAS.Examples
{
    /// <summary>
    /// GameplayTag 系统使用示例
    /// 展示了如何在游戏中使用 GameplayTag 进行状态管理、技能系统等
    /// </summary>
    public class GameplayTagExample : MonoBehaviour
    {
        [Header("角色状态标签")]
        public GameplayTagContainer characterTags = new GameplayTagContainer();

        [Header("技能标签")]
        public GameplayTagContainer skillTags = new GameplayTagContainer();

        [Header("测试标签")]
        [SerializeField] private string testTagName = "Character.State.Combat.Attacking";

        private void Start()
        {
            // 初始化示例
            InitializeExampleTags();
            
            // 演示基础用法
            DemonstrateBasicUsage();
            
            // 演示层级查询
            DemonstrateHierarchicalQueries();
            
            // 演示容器操作
            DemonstrateContainerOperations();
            
            // 演示角色状态管理
            DemonstrateCharacterStateManagement();
            
            // 演示技能系统
            DemonstrateSkillSystem();
        }

        /// <summary>
        /// 初始化示例标签
        /// </summary>
        private void InitializeExampleTags()
        {
            Debug.Log("=== 初始化示例标签 ===");

            // 注册一些常用的游戏标签
            var manager = GameplayTagManager.Instance;
            
            // 角色状态标签
            manager.RegisterTags(
                "Character.State.Idle",
                "Character.State.Moving",
                "Character.State.Combat.Attacking",
                "Character.State.Combat.Defending",
                "Character.State.Combat.Stunned",
                "Character.State.Dead"
            );

            // 技能标签
            manager.RegisterTags(
                "Skill.Attack.Melee",
                "Skill.Attack.Ranged",
                "Skill.Magic.Fire",
                "Skill.Magic.Ice",
                "Skill.Magic.Lightning",
                "Skill.Buff.Strength",
                "Skill.Buff.Speed",
                "Skill.Debuff.Poison",
                "Skill.Debuff.Slow"
            );

            // 装备标签
            manager.RegisterTags(
                "Equipment.Weapon.Sword",
                "Equipment.Weapon.Bow",
                "Equipment.Armor.Helmet",
                "Equipment.Armor.Chest"
            );

            Debug.Log($"注册了 {manager.RegisteredTagCount} 个标签");
        }

        /// <summary>
        /// 演示基础用法
        /// </summary>
        private void DemonstrateBasicUsage()
        {
            Debug.Log("\n=== 基础用法演示 ===");

            // 创建标签的几种方式
            GameplayTag tag1 = new GameplayTag("Character.State.Debuff.Poison");
            GameplayTag tag2 = "Character.State.Debuff.Bleed"; // 隐式转换
            GameplayTag tag3 = GameplayTagManager.Instance.GetTag("Character.State.Debuff.Burn");

            Debug.Log($"标签1: {tag1}");
            Debug.Log($"标签2: {tag2}");
            Debug.Log($"标签3: {tag3}");

            // 标签比较
            Debug.Log($"tag1 == tag2: {tag1 == tag2}");
            Debug.Log($"tag1.Equals(tag2): {tag1.Equals(tag2)}");

            // 标签属性
            Debug.Log($"标签1完整名称: {tag1.Name}");
            Debug.Log($"标签1简称: {tag1.ShortName}");
            Debug.Log($"标签1层级深度: {tag1.Depth}");
            Debug.Log($"标签1祖先: [{string.Join(", ", tag1.AncestorNames)}]");
        }

        /// <summary>
        /// 演示层级查询
        /// </summary>
        private void DemonstrateHierarchicalQueries()
        {
            Debug.Log("\n=== 层级查询演示 ===");

            GameplayTag fullTag = "Character.State.Debuff.Poison";
            GameplayTag parentTag = "Character.State.Debuff";
            GameplayTag rootTag = "Character";

            Debug.Log($"完整标签: {fullTag}");
            Debug.Log($"父标签: {parentTag}");
            Debug.Log($"根标签: {rootTag}");

            // 层级查询
            Debug.Log($"fullTag.HasTag(parentTag): {fullTag.HasTag(parentTag)}");
            Debug.Log($"fullTag.HasTag(rootTag): {fullTag.HasTag(rootTag)}");
            Debug.Log($"fullTag.IsDescendantOf(parentTag): {fullTag.IsDescendantOf(parentTag)}");

            // 获取父标签
            Debug.Log($"fullTag的父标签: {fullTag.GetParent()}");
            Debug.Log($"层级0的祖先: {fullTag.GetAncestorAtLevel(0)}");
            Debug.Log($"层级1的祖先: {fullTag.GetAncestorAtLevel(1)}");
        }

        /// <summary>
        /// 演示容器操作
        /// </summary>
        private void DemonstrateContainerOperations()
        {
            Debug.Log("\n=== 容器操作演示 ===");

            // 创建容器
            var container1 = new GameplayTagContainer(
                "Character.State.Combat.Attacking",
                "Skill.Buff.Strength",
                "Equipment.Weapon.Sword"
            );

            var container2 = GameplayTagContainer.FromStrings(
                "Character.State.Combat.Defending",
                "Skill.Buff.Speed",
                "Equipment.Armor.Helmet"
            );

            Debug.Log($"容器1: {container1}");
            Debug.Log($"容器2: {container2}");

            // 查询操作
            Debug.Log($"容器1包含攻击状态: {container1.HasTag("Character.State.Combat.Attacking")}");
            Debug.Log($"容器1包含任何战斗状态: {container1.HasTagExact("Character.State.Combat")}");

            // 合并容器
            var mergedContainer = container1 + container2;
            Debug.Log($"合并后的容器: {mergedContainer}");

            // 差集操作
            container1.AddTag("Common.Tag");
            container2.AddTag("Common.Tag");
            var diffContainer = container1 - container2;
            Debug.Log($"差集容器: {diffContainer}");
        }

        /// <summary>
        /// 演示角色状态管理
        /// </summary>
        private void DemonstrateCharacterStateManagement()
        {
            Debug.Log("\n=== 角色状态管理演示 ===");

            var character = new CharacterStateExample();
            
            // 设置初始状态
            character.SetState("Character.State.Idle");
            Debug.Log($"角色当前状态: {character.GetCurrentState()}");

            // 进入战斗
            character.SetState("Character.State.Combat.Attacking");
            Debug.Log($"角色当前状态: {character.GetCurrentState()}");
            Debug.Log($"角色是否在战斗中: {character.IsInCombat()}");

            // 添加Buff
            character.AddBuff("Skill.Buff.Strength");
            character.AddBuff("Skill.Buff.Speed");
            Debug.Log($"角色当前Buff: {character.GetActiveBuffs()}");

            // 添加Debuff
            character.AddDebuff("Skill.Debuff.Poison");
            Debug.Log($"角色是否中毒: {character.IsPoisoned()}");

            // 移除效果
            character.RemoveBuff("Skill.Buff.Speed");
            Debug.Log($"移除速度Buff后: {character.GetActiveBuffs()}");
        }

        /// <summary>
        /// 演示技能系统
        /// </summary>
        private void DemonstrateSkillSystem()
        {
            Debug.Log("\n=== 技能系统演示 ===");

            var skillSystem = new SkillSystemExample();

            // 添加技能
            skillSystem.LearnSkill("Skill.Attack.Melee");
            skillSystem.LearnSkill("Skill.Magic.Fire");
            skillSystem.LearnSkill("Skill.Magic.Ice");

            Debug.Log($"已学习的技能: {skillSystem.GetLearnedSkills()}");

            // 检查技能类型
            Debug.Log($"是否学习了攻击技能: {skillSystem.HasSkillType("Skill.Attack")}");
            Debug.Log($"是否学习了魔法技能: {skillSystem.HasSkillType("Skill.Magic")}");

            // 获取特定类型的技能
            Debug.Log($"已学习的魔法技能: {skillSystem.GetSkillsByType("Skill.Magic")}");
        }

        /// <summary>
        /// Unity Inspector 按钮：测试标签查询
        /// </summary>
        [ContextMenu("Test Tag Query")]
        public void TestTagQuery()
        {
            if (string.IsNullOrEmpty(testTagName))
            {
                Debug.LogWarning("请输入测试标签名称");
                return;
            }

            var testTag = new GameplayTag(testTagName);
            Debug.Log($"测试标签: {testTag}");
            Debug.Log($"简称: {testTag.ShortName}");
            Debug.Log($"深度: {testTag.Depth}");
            Debug.Log($"祖先: [{string.Join(", ", testTag.AncestorNames)}]");

            // 测试角色是否拥有此标签
            bool hasTag = characterTags.HasTagExact(testTag);
            Debug.Log($"角色是否拥有此标签: {hasTag}");
        }
    }

    /// <summary>
    /// 角色状态管理示例
    /// </summary>
    public class CharacterStateExample
    {
        private GameplayTag _currentState;
        private readonly GameplayTagContainer _activeBuffs = new GameplayTagContainer();
        private readonly GameplayTagContainer _activeDebuffs = new GameplayTagContainer();

        public void SetState(string stateName)
        {
            _currentState = new GameplayTag(stateName);
        }

        public GameplayTag GetCurrentState()
        {
            return _currentState;
        }

        public bool IsInCombat()
        {
            return _currentState.HasTag("Character.State.Combat");
        }

        public void AddBuff(string buffName)
        {
            _activeBuffs.AddTag(buffName);
        }

        public void RemoveBuff(string buffName)
        {
            _activeBuffs.RemoveTag(buffName);
        }

        public void AddDebuff(string debuffName)
        {
            _activeDebuffs.AddTag(debuffName);
        }

        public void RemoveDebuff(string debuffName)
        {
            _activeDebuffs.RemoveTag(debuffName);
        }

        public bool IsPoisoned()
        {
            return _activeDebuffs.HasTag("Skill.Debuff.Poison");
        }

        public GameplayTagContainer GetActiveBuffs()
        {
            return _activeBuffs;
        }

        public GameplayTagContainer GetActiveDebuffs()
        {
            return _activeDebuffs;
        }
    }

    /// <summary>
    /// 技能系统示例
    /// </summary>
    public class SkillSystemExample
    {
        private readonly GameplayTagContainer _learnedSkills = new GameplayTagContainer();

        public void LearnSkill(string skillName)
        {
            _learnedSkills.AddTag(skillName);
        }

        public void ForgetSkill(string skillName)
        {
            _learnedSkills.RemoveTag(skillName);
        }

        public bool HasSkill(string skillName)
        {
            return _learnedSkills.HasTag(skillName);
        }

        public bool HasSkillType(string skillType)
        {
            return _learnedSkills.HasTagExact(skillType);
        }

        public GameplayTagContainer GetLearnedSkills()
        {
            return _learnedSkills;
        }

        public string GetSkillsByType(string skillType)
        {
            var typeTag = new GameplayTag(skillType);
            var matchingSkills = _learnedSkills.GetTagsWithParent(typeTag);
            return $"[{string.Join(", ", matchingSkills)}]";
        }
    }
}

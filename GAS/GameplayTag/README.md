# GameplayTag System

一个高性能的层级标签系统，专为Unity游戏开发设计。基于知乎文章《GameplayTag 设计思想与实现》的设计理念实现。

## 核心特性

### 🚀 高性能设计
- **值类型结构体** - 避免GC压力，支持栈内存分配
- **预计算哈希码** - 所有比较操作都基于整数比较，极快的运行时性能
- **避免装箱** - 实现 `IEquatable<T>` 接口，避免值类型装箱

### 🌳 层级关系支持
- **点号分隔的层级结构** - 如 `Character.State.Combat.Attacking`
- **强大的层级查询** - 支持精确匹配和层级匹配
- **祖先关系查询** - 快速检查父子关系

### 📦 完整的生态系统
- **GameplayTag** - 核心标签结构体
- **GameplayTagContainer** - 标签容器管理类
- **GameplayTagManager** - 全局标签管理器
- **完整的示例和测试** - 开箱即用

## 快速开始

### 基础用法

```csharp
using CodeWarehouse.GAS;

// 创建标签
GameplayTag tag = new GameplayTag("Character.State.Combat.Attacking");
GameplayTag tag2 = "Character.State.Idle"; // 隐式转换

// 标签比较 - 基于哈希码，极快
bool isEqual = tag == tag2;

// 获取标签信息
Debug.Log($"完整名称: {tag.Name}");        // Character.State.Combat.Attacking
Debug.Log($"简称: {tag.ShortName}");       // Attacking
Debug.Log($"深度: {tag.Depth}");           // 4
Debug.Log($"祖先: {string.Join(", ", tag.AncestorNames)}"); // Character, Character.State, Character.State.Combat
```

### 层级查询

```csharp
GameplayTag fullTag = "Character.State.Combat.Attacking";
GameplayTag parentTag = "Character.State.Combat";
GameplayTag rootTag = "Character";

// 层级查询 - 检查是否拥有指定标签
bool hasCombat = fullTag.HasTag(parentTag);     // true
bool hasCharacter = fullTag.HasTag(rootTag);    // true
bool hasSelf = fullTag.HasTag(fullTag);         // true

// 反向查询
bool isDescendant = fullTag.IsDescendantOf(parentTag); // true

// 获取父标签
GameplayTag parent = fullTag.GetParent(); // Character.State.Combat
```

### 容器管理

```csharp
// 创建容器
var container = new GameplayTagContainer(
    "Character.State.Combat.Attacking",
    "Skill.Buff.Strength",
    "Equipment.Weapon.Sword"
);

// 查询操作
bool hasAttacking = container.HasTag("Character.State.Combat.Attacking"); // 精确匹配
bool hasCombat = container.HasTagExact("Character.State.Combat");         // 层级匹配

// 容器操作
container.AddTag("Skill.Magic.Fire");
container.RemoveTag("Equipment.Weapon.Sword");

// 集合操作
var container2 = new GameplayTagContainer("Skill.Buff.Speed", "Character.State.Moving");
var merged = container + container2;    // 合并
var diff = container - container2;      // 差集
```

### 全局管理器

```csharp
var manager = GameplayTagManager.Instance;

// 注册标签
manager.RegisterTag("Character.State.Combat.Attacking");
manager.RegisterTags("Skill.Magic.Fire", "Skill.Magic.Ice", "Skill.Magic.Lightning");

// 获取标签
GameplayTag tag = manager.GetTag("Character.State.Combat.Attacking");

// 验证标签
var validation = manager.ValidateTagName("Character.State.Combat.Attacking");
if (validation.IsValid)
{
    Debug.Log("标签名称有效");
}

// 查询功能
var magicSkills = manager.GetTagsByCategory("Skill");
var searchResults = manager.SearchTags("Combat");
```

## 实际应用示例

### 角色状态管理

```csharp
public class Character : MonoBehaviour
{
    private GameplayTag _currentState;
    private GameplayTagContainer _activeEffects = new GameplayTagContainer();

    public void SetState(string stateName)
    {
        _currentState = new GameplayTag(stateName);
    }

    public bool IsInCombat()
    {
        return _currentState.HasTag("Character.State.Combat");
    }

    public void AddBuff(string buffName)
    {
        _activeEffects.AddTag(buffName);
    }

    public bool HasBuff(string buffType)
    {
        return _activeEffects.HasTagExact(buffType);
    }
}
```

### 技能系统

```csharp
public class SkillSystem : MonoBehaviour
{
    private GameplayTagContainer _learnedSkills = new GameplayTagContainer();
    private GameplayTagContainer _blockedSkillTypes = new GameplayTagContainer();

    public bool CanUseSkill(string skillName)
    {
        var skill = new GameplayTag(skillName);
        
        // 检查是否学习了这个技能
        if (!_learnedSkills.HasTag(skill))
            return false;

        // 检查技能类型是否被阻止
        foreach (var blockedType in _blockedSkillTypes)
        {
            if (skill.HasTag(blockedType))
                return false;
        }

        return true;
    }

    public void BlockSkillType(string skillType)
    {
        _blockedSkillTypes.AddTag(skillType);
    }
}
```

### 装备系统

```csharp
public class Equipment : MonoBehaviour
{
    private GameplayTagContainer _equippedItems = new GameplayTagContainer();
    private GameplayTagContainer _providedBonuses = new GameplayTagContainer();

    public void EquipItem(string itemTag, string[] bonuses)
    {
        _equippedItems.AddTag(itemTag);
        _providedBonuses.AddTags(bonuses);
    }

    public bool HasEquipmentType(string equipmentType)
    {
        return _equippedItems.HasTagExact(equipmentType);
    }

    public bool HasBonus(string bonusType)
    {
        return _providedBonuses.HasTagExact(bonusType);
    }
}
```

## 性能特点

### 创建性能
- 标签创建时进行一次性预计算
- 运行时无字符串操作开销
- 支持编译时常量优化

### 比较性能
- 所有比较操作都是 O(1) 整数比较
- 无字符串比较开销
- 无GC分配

### 内存效率
- 值类型结构体，栈内存分配
- 预计算数据缓存在结构体内
- HashSet 容器避免重复标签

## 设计原则

### 1. 性能优先
所有设计决策都优先考虑运行时性能，通过预计算来换取运行时速度。

### 2. 类型安全
使用强类型而非字符串常量，减少拼写错误。

### 3. 易用性
提供直观的API和丰富的辅助方法。

### 4. 可扩展性
支持层级结构，便于添加新的标签类型。

## 常见标签约定

```csharp
// 角色状态
"Character.State.Idle"
"Character.State.Moving"
"Character.State.Combat.Attacking"
"Character.State.Combat.Defending"
"Character.State.Combat.Stunned"
"Character.State.Dead"

// 技能
"Skill.Attack.Melee"
"Skill.Attack.Ranged"
"Skill.Magic.Fire"
"Skill.Magic.Ice"
"Skill.Buff.Strength"
"Skill.Debuff.Poison"

// 装备
"Equipment.Weapon.Sword"
"Equipment.Weapon.Bow"
"Equipment.Armor.Helmet"
"Equipment.Armor.Chest"

// 游戏机制
"Gameplay.Condition.LowHealth"
"Gameplay.Condition.HighMana"
"Gameplay.Event.LevelUp"
"Gameplay.Event.QuestComplete"
```

## 最佳实践

### 1. 标签命名
- 使用 PascalCase 命名
- 从通用到具体的层级结构
- 保持命名的一致性

### 2. 性能优化
- 在游戏启动时预注册常用标签
- 避免在运行时频繁创建新标签
- 使用容器来管理多个相关标签

### 3. 代码组织
- 将相关标签定义集中管理
- 使用常量或配置文件定义标签
- 为不同系统创建专门的标签管理器

## 文件结构

```
Assets/CodeWarehouse/GAS/
├── GameplayTag.cs              # 核心标签结构体
├── GameplayTagContainer.cs     # 标签容器类
├── GameplayTagManager.cs       # 全局管理器
├── Examples/
│   └── GameplayTagExample.cs   # 使用示例
├── Tests/
│   └── GameplayTagTests.cs     # 单元测试
└── README.md                   # 本文档
```

## 系统要求

- Unity 2019.4 或更高版本
- C# 8.0 支持（用于 `^` 索引操作符）
- .NET Standard 2.0

## 扩展功能

该系统还可以进一步扩展：

- **序列化支持** - 为网络同步和存档
- **编辑器工具** - 可视化标签管理界面
- **数据驱动配置** - 从外部文件加载标签定义
- **国际化支持** - 多语言标签显示
- **调试工具** - 运行时标签状态监控

## 许可证

此代码基于知乎文章的设计理念实现，遵循MIT许可证。可自由用于商业和非商业项目。

## 参考资料

- [知乎文章：GameplayTag 设计思想与实现](https://zhuanlan.zhihu.com/p/1943422604221317244)
- Unity官方文档
- C# 性能最佳实践

---

如有问题或建议，请通过Issues或Pull Requests与我们联系。

# ET ECS 框架示例

## 简介

这是一个基于ET（Entity-Task）框架设计理念的ECS（Entity-Component-System）示例实现。该示例展示了ET框架的核心设计原则：**逻辑与数据的完全分离**。

## 核心设计理念

### 1. 逻辑与数据完全分离
- **EC（Entity-Component）是纯数据**：所有组件只包含数据，不包含逻辑方法
- **S（System）为逻辑**：所有逻辑通过扩展方法实现，与数据完全分离

### 2. 组件代替继承
- 所有游戏单位（玩家、怪物、NPC）都统一为 `Entity`
- 通过添加不同的组件来区分不同类型的单位
- 避免了传统OOP中复杂的继承层次结构

### 3. 类型分发代替虚函数
- 使用 `TypeDispatcher` 根据 `UnitType` 分发到不同的处理器
- 替代传统OOP中的虚函数机制
- 更容易维护和重构

### 4. 树状结构的生命周期管理
- 所有 `Entity` 都有父子关系
- 父实体释放时，子实体自动释放
- 完美解决了对象生命周期管理问题

## 文件结构

```
ETECS/
├── Core/
│   └── Entity.cs                    # 核心Entity类
├── Components/
│   ├── UnitTypeComponent.cs         # 单位类型组件
│   ├── HealthComponent.cs           # 血量组件
│   ├── MovementComponent.cs         # 移动组件
│   ├── AIComponent.cs               # AI组件
│   └── AttackComponent.cs           # 攻击组件（新增）
├── Systems/
│   ├── EntityExtensions.cs         # Entity扩展方法
│   ├── TypeDispatcher.cs           # 类型分发器
│   └── GameLogicExtensions.cs      # 游戏逻辑扩展方法
├── Examples/
│   └── ETECSExample.cs             # 使用示例
└── README.md                       # 本文档
```

## 核心概念

### Entity（实体）
- 所有游戏对象的基类
- 本质上也是一个Component，可以挂载其他Component
- 提供生命周期管理和父子关系管理

### Component（组件）
- 纯数据容器，不包含逻辑
- 每个Entity只能有一个同类型的Component
- 组件也可以挂载子组件（层级管理）

### System（系统）
- 通过扩展方法实现
- 处理具体的游戏逻辑
- 与数据完全分离

## 使用示例

### 1. 创建Unit

```csharp
// 创建场景根实体
Entity sceneRoot = new Entity();

// 创建玩家
Entity player = sceneRoot.CreateUnit(UnitType.Player, 1001, Vector3.zero);

// 创建怪物
Entity monster = sceneRoot.CreateUnit(UnitType.Monster, 2001, new Vector3(10, 0, 0));
```

### 2. 组件操作

```csharp
// 获取组件
var healthComponent = player.GetComponent<HealthComponent>();
var movementComponent = player.GetComponent<MovementComponent>();

// 检查组件存在性
bool hasAI = monster.HasComponent<AIComponent>();

// 添加/移除组件
var newComponent = player.AddComponent<SomeComponent>();
player.RemoveComponent<SomeComponent>();
```

### 3. 逻辑操作（通过扩展方法）

```csharp
// 移动逻辑
player.MoveTo(new Vector3(5, 0, 0));
player.UpdateMovement(Time.deltaTime);

// 血量逻辑
player.TakeDamage(20);
player.Heal(30);

// 攻击逻辑
player.Attack(monster);

// AI逻辑
monster.UpdateAI();
monster.SetAITarget(player);
```

### 4. 类型分发

```csharp
// 不同类型的Unit会分发到不同的处理器
TypeDispatcher.DispatchDeath(unit);      // 死亡处理
TypeDispatcher.DispatchAIUpdate(unit);   // AI更新
TypeDispatcher.DispatchAttack(attacker, target); // 攻击处理
```

### 5. 攻击系统（组件化能力控制）

```csharp
// 检查攻击能力
bool canAttack = unit.CanAttack();

// 攻击目标（自动检查距离、冷却、攻击能力）
player.Attack(monster);

// 动态添加攻击能力
var attackComponent = npc.AddComponent<AttackComponent>();
attackComponent.AttackPower = 15;
attackComponent.AttackRange = 1.0f;

// 动态移除攻击能力
npc.RemoveComponent<AttackComponent>();

// 距离和范围检查
bool inRange = player.IsInAttackRange(monster);
float distance = player.GetDistanceTo(monster);

// 移动到攻击范围
player.MoveToAttackRange(monster);
```

### 6. 生命周期管理

```csharp
// 创建副本场景
Entity dungeonScene = new Entity();
dungeonScene.SetParent(sceneRoot);

// 在副本中创建怪物
Entity boss = dungeonScene.CreateUnit(UnitType.Monster, 9999, Vector3.zero);

// 副本结束时，只需释放场景实体，所有子实体会自动释放
dungeonScene.Dispose(); // boss和其他所有子实体都会自动释放
```

## 设计优势

### 1. 易于重构
- 逻辑与数据分离，修改逻辑不影响数据结构
- 可以轻松重构几万行的函数而不用担心破坏数据

### 2. 灵活的组合
- 通过组件组合而非继承来实现功能
- 可以动态添加/移除组件来改变对象行为

### 3. 清晰的架构
- 树状结构让整个游戏架构一目了然
- 每个对象的职责明确，依赖关系清楚

### 4. 高性能
- 避免了虚函数调用的开销
- 可以方便地实现对象池等优化

### 5. 易于调试
- 所有逻辑都在扩展方法中，容易定位问题
- 组件的状态一目了然

## 与传统OOP的对比

| 传统OOP | ET ECS |
|---------|---------|
| 继承层次 | 组件组合 |
| 虚函数 | 类型分发 |
| 逻辑与数据耦合 | 逻辑与数据分离 |
| 难以重构 | 易于重构 |
| 复杂的接口设计 | 简单的扩展方法 |

## 运行示例

1. 将 `ETECSExample.cs` 脚本挂载到场景中的任意GameObject上
2. 运行场景
3. 查看Console输出，了解ET ECS的工作原理
4. 使用GUI面板测试各种功能

## 注意事项

1. **组件字段直接public**：ET推荐组件成员直接public，但要遵循"不直接访问别人组件成员变量"的原则
2. **避免继承**：除了Entity，避免使用继承，用组件代替
3. **生命周期管理**：始终通过父实体来管理子实体的生命周期
4. **扩展方法**：所有逻辑都应该通过扩展方法实现

## 攻击系统设计详解

本示例特别展示了如何通过组件化设计实现"玩家可以攻击，NPC无法攻击"的需求：

### 设计思路
1. **组件代替继承**：不通过继承不同类来区分攻击能力，而是通过`AttackComponent`组件
2. **动态能力管理**：可以运行时动态添加/移除攻击能力
3. **完整的攻击逻辑**：包含距离检查、冷却管理、伤害计算、暴击系统
4. **类型分发处理**：不同类型的单位有不同的攻击逻辑实现

### 核心特性
- **玩家**：默认具备攻击能力，攻击力25，攻击范围1.5米
- **怪物**：默认具备攻击能力，攻击力20，攻击范围2.0米  
- **NPC**：默认无攻击能力，但可动态添加
- **攻击冷却**：防止无限制攻击
- **距离检查**：超出范围无法攻击
- **暴击系统**：支持暴击率和暴击倍数

### 实际应用场景
```csharp
// 场景1：商人NPC突然变成敌对，获得攻击能力
npc.AddComponent<AttackComponent>();

// 场景2：玩家被诅咒，失去攻击能力
player.RemoveComponent<AttackComponent>();

// 场景3：检查是否可以攻击
if (unit.CanAttack() && unit.IsInAttackRange(target))
{
    unit.Attack(target);
}
```

## 扩展方向

1. **网络同步**：可以扩展为支持网络同步的ECS系统
2. **序列化**：添加组件的序列化/反序列化支持
3. **性能优化**：实现对象池、批处理等优化
4. **更多组件**：添加更多游戏功能组件（技能、背包、任务等）
5. **攻击系统增强**：添加攻击动画、特效、连击、技能等

## 总结

ET ECS通过"逻辑与数据分离"的设计理念，解决了传统OOP在游戏开发中遇到的重构困难、代码耦合等问题。虽然看起来放弃了OOP的一些特性（继承、多态），但实际上是用更简单、更直接的方式（组件、分发）来解决同样的问题，让代码更容易理解和维护。

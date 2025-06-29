# 引用类型管理器 (ReferenceTypeManager)

## 概述

引用类型管理器是一个高性能的Unity工具，专门设计用于**零GC事件总线系统**。它通过将引用类型对象映射为唯一的整数ID，实现在事件传递过程中避免GC分配，从而提升游戏性能。

### 核心特性

- **零GC分配**: 事件中传递ID而非对象引用，避免GC压力
- **弱引用管理**: 使用WeakReference确保对象可被正常回收
- **线程安全**: 使用ConcurrentDictionary保证多线程环境下的安全性
- **自动清理**: 智能检测并清理已回收对象的引用
- **丰富扩展**: 针对Unity各类组件和资源提供专用扩展方法
- **性能监控**: 内置性能统计和监控功能

## 核心架构

### 数据结构
```csharp
// 正向映射：ID -> WeakReference
ConcurrentDictionary<int, WeakReference> _references

// 反向映射：Object -> ID  
ConcurrentDictionary<object, int> _reverseMap

// ID生成器
volatile int _nextId
```

### 内存管理策略
- 使用弱引用（WeakReference）存储对象，允许GC正常回收
- 智能清理机制：死亡引用超过20%时自动触发清理
- Unity集成：应用焦点变化和域重载时自动清理

## API 参考

### 核心API

#### 获取或创建ID
```csharp
public static int GetOrCreateId<T>(T obj) where T : class
```
- 为引用类型对象获取或创建唯一ID
- 线程安全，支持并发调用
- null对象返回0

#### 通过ID获取对象
```csharp
public static T GetReference<T>(int id) where T : class
```
- 通过ID获取对应的引用类型对象
- 对象已被回收时返回null
- 支持类型转换

#### 批量操作
```csharp
public static int GetReferences<T>(int[] ids, T[] results) where T : class
```
- 批量获取多个对象，提升性能
- 返回成功获取的对象数量

#### 有效性检查
```csharp
public static bool IsValid(int id)
```
- 检查ID对应的对象是否仍然存活
- 高性能的存活状态检查

### 扩展方法API

#### GameObject和Component
```csharp
// GameObject
gameObject.ToReferenceId()                    // 获取GameObject的ID
ReferenceTypeExtensions.FromReferenceId(id)   // 通过ID获取GameObject

// Component
component.ToReferenceId()                     // 获取Component的ID
ReferenceTypeExtensions.GetComponent<T>(id)   // 通过ID获取Component

// Transform
transform.ToReferenceId()                     // 获取Transform的ID
ReferenceTypeExtensions.GetTransform(id)      // 通过ID获取Transform
```

#### UI组件专用扩展
```csharp
// UI组件
rectTransform.ToUIReferenceId()               // RectTransform
canvas.ToUIReferenceId()                      // Canvas
canvasGroup.ToUIReferenceId()                 // CanvasGroup
ReferenceTypeExtensions.GetUIComponent<T>(id) // 获取UI组件
```

#### 物理组件扩展  
```csharp
// 物理组件
collider.ToPhysicsReferenceId()               // Collider
rigidbody.ToPhysicsReferenceId()              // Rigidbody
collision.ToPhysicsReferenceId()              // Collision
ReferenceTypeExtensions.GetPhysicsComponent<T>(id) // 获取物理组件
```

#### 资源类型扩展
```csharp
// Unity资源
material.ToAssetReferenceId()                 // Material
texture.ToAssetReferenceId()                  // Texture
sprite.ToAssetReferenceId()                   // Sprite
audioClip.ToAssetReferenceId()                // AudioClip
mesh.ToAssetReferenceId()                     // Mesh
ReferenceTypeExtensions.GetAsset<T>(id)       // 获取资源
```

#### ScriptableObject扩展
```csharp
// ScriptableObject
scriptableObject.ToScriptableObjectId()       // 获取ScriptableObject的ID
ReferenceTypeExtensions.GetScriptableObject<T>(id) // 通过ID获取ScriptableObject
```

### 引用容器类型

#### ReferenceIdContainer
用于存储固定数量的引用（最多3个）：
```csharp
public struct ReferenceIdContainer
{
    public int PrimaryId;      // 主要引用
    public int SecondaryId;    // 次要引用  
    public int TertiaryId;     // 第三引用
    
    // 便利方法
    public T GetPrimary<T>() where T : class
    public T GetSecondary<T>() where T : class
    public T GetTertiary<T>() where T : class
    
    // 有效性检查
    public bool HasPrimary { get; }
    public bool HasSecondary { get; }
    public bool HasTertiary { get; }
}
```

#### ReferenceIdArray
用于存储动态数量的引用：
```csharp
public struct ReferenceIdArray
{
    public int[] Ids;
    
    // 获取所有引用
    public T[] GetReferences<T>() where T : class
    public List<T> GetReferencesList<T>() where T : class
    
    // 统计信息
    public int Count { get; }
    public bool IsEmpty { get; }
    public int GetValidCount()
}
```

#### ReferenceIdDictionary
用于存储键值对形式的引用：
```csharp
public struct ReferenceIdDictionary
{
    public KeyValuePair[] Pairs;
    
    // 获取引用
    public T GetReference<T>(string key) where T : class
    public Dictionary<string, T> GetReferenceDictionary<T>() where T : class
    
    // 查询方法
    public bool ContainsKey(string key)
    public int Count { get; }
    public bool IsEmpty { get; }
}
```

#### HierarchyReferenceContainer
用于存储层级结构的引用：
```csharp
public struct HierarchyReferenceContainer
{
    public int ParentId;       // 父对象ID
    public int[] ChildrenIds;  // 子对象ID数组
    public int[] SiblingIds;   // 兄弟对象ID数组
    
    // 获取层级对象
    public T GetParent<T>() where T : class
    public T[] GetChildren<T>() where T : class
    public T[] GetSiblings<T>() where T : class
    public List<T> GetValidChildren<T>() where T : class
    
    // 层级信息
    public bool HasParent { get; }
    public bool HasChildren { get; }
    public bool HasSiblings { get; }
    public int ChildrenCount { get; }
    public int SiblingsCount { get; }
}
```

## 使用示例

### 1. 基础使用
```csharp
// 获取GameObject的ID
GameObject player = GameObject.FindWithTag("Player");
int playerId = player.ToReferenceId();

// 在事件中传递ID
public struct PlayerMoveEvent : IZeroGCEvent
{
    public int PlayerId;
    public Vector3 NewPosition;
    
    // 便利方法获取实际对象
    public GameObject GetPlayer() => ReferenceTypeExtensions.FromReferenceId(PlayerId);
}

// 创建事件
var moveEvent = new PlayerMoveEvent
{
    PlayerId = player.ToReferenceId(),
    NewPosition = newPos
};
```

### 2. 攻击系统示例
```csharp
public struct PlayerAttackEvent : IZeroGCEvent
{
    public int AttackerId;     // 攻击者GameObject的ID
    public int TargetId;       // 目标GameObject的ID  
    public int WeaponId;       // 武器ScriptableObject的ID
    public Vector3 AttackPosition;
    public float Damage;
    
    // 便利方法
    public GameObject GetAttacker() => ReferenceTypeExtensions.FromReferenceId(AttackerId);
    public GameObject GetTarget() => ReferenceTypeExtensions.FromReferenceId(TargetId);
    public Weapon GetWeapon() => ReferenceTypeExtensions.GetScriptableObject<Weapon>(WeaponId);
    
    // 静态创建方法
    public static PlayerAttackEvent Create(GameObject attacker, GameObject target, 
                                         Weapon weapon, Vector3 position, float damage)
    {
        return new PlayerAttackEvent
        {
            AttackerId = attacker.ToReferenceId(),
            TargetId = target.ToReferenceId(),
            WeaponId = weapon.ToScriptableObjectId(),
            AttackPosition = position,
            Damage = damage
        };
    }
}
```

### 3. UI交互示例
```csharp
public struct UIInteractionEvent : IZeroGCEvent
{
    public int UIElementId;    // UI元素ID
    public int CanvasId;       // 画布ID
    public UIInteractionType InteractionType;
    public Vector2 ScreenPosition;
    
    // 便利方法
    public GameObject GetUIElement() => ReferenceTypeExtensions.FromReferenceId(UIElementId);
    public Canvas GetCanvas() => ReferenceTypeExtensions.GetUIComponent<Canvas>(CanvasId);
    
    public static UIInteractionEvent Create(GameObject uiElement, UIInteractionType type, Vector2 screenPos)
    {
        var canvas = uiElement.GetComponentInParent<Canvas>();
        
        return new UIInteractionEvent
        {
            UIElementId = uiElement.ToReferenceId(),
            CanvasId = canvas?.ToUIReferenceId() ?? 0,
            InteractionType = type,
            ScreenPosition = screenPos
        };
    }
}
```

### 4. 多目标技能示例
```csharp
public struct MultiTargetSkillEvent : IZeroGCEvent
{
    public int CasterId;              // 施法者ID
    public int SkillDataId;           // 技能数据ID
    public ReferenceIdArray TargetIds; // 目标ID数组
    public Vector3 CastPosition;
    public float SkillPower;
    
    // 便利方法
    public GameObject GetCaster() => ReferenceTypeExtensions.FromReferenceId(CasterId);
    public SkillData GetSkillData() => ReferenceTypeExtensions.GetScriptableObject<SkillData>(SkillDataId);
    public List<GameObject> GetValidTargets() => TargetIds.GetReferencesList<GameObject>();
    
    public static MultiTargetSkillEvent Create(GameObject caster, SkillData skillData, 
                                             GameObject[] targets, Vector3 position, float power)
    {
        return new MultiTargetSkillEvent
        {
            CasterId = caster.ToReferenceId(),
            SkillDataId = skillData.ToScriptableObjectId(),
            TargetIds = new ReferenceIdArray(targets.ToReferenceIds()),
            CastPosition = position,
            SkillPower = power
        };
    }
}
```

### 5. 物理碰撞示例
```csharp
public struct PhysicsCollisionEvent : IZeroGCEvent
{
    public int ColliderId;     // 碰撞器ID
    public int RigidbodyId;    // 刚体ID
    public Vector3 CollisionPoint;
    public CollisionType Type;
    
    // 便利方法
    public Collider GetCollider() => ReferenceTypeExtensions.GetPhysicsComponent<Collider>(ColliderId);
    public Rigidbody GetRigidbody() => ReferenceTypeExtensions.GetPhysicsComponent<Rigidbody>(RigidbodyId);
    
    public static PhysicsCollisionEvent Create(Collision collision, CollisionType type)
    {
        return new PhysicsCollisionEvent
        {
            ColliderId = collision.collider.ToPhysicsReferenceId(),
            RigidbodyId = collision.rigidbody?.ToPhysicsReferenceId() ?? 0,
            CollisionPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : Vector3.zero,
            Type = type
        };
    }
}
```

## 内存管理

### 自动清理机制
```csharp
// 手动触发清理
int cleanedCount = ReferenceTypeManager.CleanupDeadReferences();

// 强制完整清理
int cleanedCount = ReferenceTypeManager.CleanupDeadReferences(forceFullCleanup: true);

// 清理所有引用
ReferenceTypeManager.ClearAll();
```

### 性能监控
```csharp
// 获取性能统计
var stats = ReferenceTypeManager.GetPerformanceStats();
Debug.Log($"总引用数: {stats.TotalReferences}");
Debug.Log($"死亡引用数: {stats.DeadReferences}");
Debug.Log($"死亡比例: {stats.DeadReferenceRatio:P2}");

// 在Unity编辑器中记录统计信息
ReferenceTypeManager.LogPerformanceStats();
```

## 最佳实践

### 1. 事件设计原则
- 在事件结构体中只存储ID，不存储对象引用
- 提供便利方法来获取实际对象
- 使用静态创建方法简化事件构造

### 2. 类型选择指南
- **GameObject/Component**: 使用基础扩展方法
- **UI组件**: 使用`ToUIReferenceId()`和`GetUIComponent<T>()`
- **物理组件**: 使用`ToPhysicsReferenceId()`和`GetPhysicsComponent<T>()`
- **资源类型**: 使用`ToAssetReferenceId()`和`GetAsset<T>()`
- **ScriptableObject**: 使用`ToScriptableObjectId()`和`GetScriptableObject<T>()`

### 3. 容器选择指南
- **固定少量引用**: 使用`ReferenceIdContainer`
- **动态数量引用**: 使用`ReferenceIdArray`
- **键值对引用**: 使用`ReferenceIdDictionary`
- **层级结构**: 使用`HierarchyReferenceContainer`

### 4. 性能优化建议
- 避免频繁的ID查找，可以缓存获取的对象
- 使用批量操作API提升批处理性能
- 定期检查性能统计，适时触发清理
- 在合适的时机调用`CleanupDeadReferences()`

### 5. 错误处理
```csharp
// 安全的对象获取
GameObject player = ReferenceTypeExtensions.FromReferenceId(playerId);
if (player != null)
{
    // 对象仍然存在，可以安全使用
    player.transform.position = newPosition;
}
else
{
    // 对象已被销毁，需要处理
    Debug.LogWarning($"Player with ID {playerId} no longer exists");
}

// 检查ID有效性
if (ReferenceTypeManager.IsValid(playerId))
{
    // ID有效，对象存在
    var player = ReferenceTypeExtensions.FromReferenceId(playerId);
    // 使用player对象...
}
```

## 注意事项

### ⚠️ 重要提醒

1. **对象生命周期**: ID不会阻止对象被垃圾回收，需要确保对象在使用期间保持有效
2. **线程安全**: 虽然内部操作是线程安全的，但获取的对象引用在多线程环境下仍需谨慎使用
3. **内存泄漏**: 虽然使用弱引用，但反向映射可能延迟对象回收，定期清理很重要
4. **ID重用**: 系统会自动处理ID重用，但不建议长期持有无效ID

### 🚀 性能提示

1. **批量操作**: 对于大量对象，优先使用批量API
2. **缓存策略**: 频繁访问的对象可以缓存引用，减少ID查找
3. **清理时机**: 在场景切换、关卡结束等时机主动清理
4. **监控统计**: 定期检查性能统计，优化使用模式

### 🔧 调试技巧

1. **统计监控**: 使用`LogPerformanceStats()`监控内存使用情况
2. **有效性检查**: 使用`IsValid()`在调试时验证ID状态  
3. **清理日志**: 清理操作会返回清理数量，可用于调试
4. **编辑器集成**: 在Unity编辑器中会自动处理域重载

## 总结

引用类型管理器是零GC事件总线系统的核心组件，通过智能的ID映射机制，有效解决了事件系统中的GC分配问题。配合丰富的扩展方法和容器类型，可以灵活应对各种复杂的游戏场景需求。

正确使用引用类型管理器，可以显著提升游戏性能，特别是在高频事件通信的场景下。建议结合性能监控功能，持续优化使用策略，以获得最佳的性能表现。 
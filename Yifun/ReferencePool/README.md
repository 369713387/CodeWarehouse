# 引用池框架 (ReferencePool Framework)

## 概述

引用池框架是一个高性能的对象池系统，用于管理和复用实现了 `IReference` 接口的对象，避免频繁的内存分配和垃圾回收。

## 主要特性

### 🚀 性能优化
- **并发安全**: 使用 `ConcurrentDictionary` 和 `ConcurrentQueue` 实现无锁并发
- **工厂缓存**: 缓存类型工厂方法，避免重复反射调用
- **内联优化**: 关键方法使用 `AggressiveInlining` 优化
- **原子操作**: 统计计数器使用 `Interlocked` 操作保证线程安全

### 💡 功能增强
- **初始化支持**: 支持参数化初始化的引用对象
- **批量操作**: 支持批量获取和释放引用
- **预热机制**: 支持预先分配引用对象
- **安全释放**: 自动null检查的安全释放方法

### 📊 监控诊断
- **使用统计**: 详细的获取、释放、添加、移除统计
- **泄漏检测**: 自动检测潜在的内存泄漏
- **使用报告**: 生成详细的使用情况报告

## 基础使用

### 1. 定义引用类

```csharp
public class MyReference : IReference
{
    public string Data { get; set; }
    public int Value { get; set; }
    
    public void Clear()
    {
        Data = null;
        Value = 0;
    }
}
```

### 2. 基本操作

```csharp
// 获取引用
var reference = ReferencePool.Acquire<MyReference>();
reference.Data = "Hello World";
reference.Value = 42;

// 使用引用...

// 释放引用
ReferencePool.Release(reference);
```

## 高级使用

### 1. 参数化初始化

```csharp
public class ConfigurableReference : IReference<string>
{
    public string Config { get; private set; }
    
    public void Initialize(string config)
    {
        Config = config;
    }
    
    public void Clear()
    {
        Config = null;
    }
}

// 使用扩展方法获取并初始化
var reference = ReferencePoolExtensions.Acquire<ConfigurableReference, string>("my-config");
```

### 2. 批量操作

```csharp
// 批量获取
var references = ReferencePoolExtensions.AcquireBatch<MyReference>(10);

// 批量释放
ReferencePoolExtensions.ReleaseBatch(references);
```

### 3. 预热池

```csharp
// 预热引用池，预先创建50个对象
ReferencePoolExtensions.Warmup<MyReference>(50);
```

### 4. 安全释放

```csharp
MyReference reference = ReferencePool.Acquire<MyReference>();
// ... 使用 reference ...

// 安全释放并置空
ReferencePoolExtensions.SafeRelease(ref reference);
// reference 现在为 null
```

## 监控和诊断

### 1. 获取使用报告

```csharp
string report = ReferencePoolMonitor.GetUsageReport();
Console.WriteLine(report);
```

输出示例：
```
=== Reference Pool Usage Report ===
Total Pools: 2

Type: MyReference
  Unused: 15, Using: 5
  Acquired: 100, Released: 95
  Added: 50, Removed: 0

Type: ConfigurableReference
  Unused: 8, Using: 2
  Acquired: 20, Released: 18
  Added: 10, Removed: 0
```

### 2. 检查内存泄漏

```csharp
var leaks = ReferencePoolMonitor.CheckLeaks();
foreach (var leak in leaks)
{
    Console.WriteLine($"Type {leak.Key.Name} has {leak.Value} potential leaks");
}
```

## 配置选项

### 启用严格检查

```csharp
// 启用严格检查（性能会有所下降）
ReferencePool.EnableStrictCheck = true;
```

严格检查模式会：
- 验证引用类型的合法性
- 检测重复释放的引用
- 提供更详细的错误信息

## 最佳实践

### 1. 正确实现 Clear 方法
```csharp
public void Clear()
{
    // 清理所有字段到默认状态
    stringField = null;
    intField = 0;
    listField?.Clear(); // 清理集合但保留容器
    objectField = null;
}
```

### 2. 避免循环引用
```csharp
// ❌ 错误：可能导致循环引用
public class BadReference : IReference
{
    public BadReference Parent { get; set; }
    public List<BadReference> Children { get; set; }
    
    public void Clear()
    {
        // 没有正确清理引用
    }
}

// ✅ 正确：清理所有引用
public class GoodReference : IReference
{
    public GoodReference Parent { get; set; }
    public List<GoodReference> Children { get; set; }
    
    public void Clear()
    {
        Parent = null;
        Children?.Clear(); // 清理列表内容
    }
}
```

### 3. 使用 using 模式
```csharp
public class ReferenceScope : IDisposable
{
    private readonly List<IReference> references = new List<IReference>();
    
    public T Acquire<T>() where T : class, IReference, new()
    {
        var reference = ReferencePool.Acquire<T>();
        references.Add(reference);
        return reference;
    }
    
    public void Dispose()
    {
        foreach (var reference in references)
        {
            ReferencePool.Release(reference);
        }
        references.Clear();
    }
}

// 使用
using (var scope = new ReferenceScope())
{
    var ref1 = scope.Acquire<MyReference>();
    var ref2 = scope.Acquire<MyReference>();
    // 作用域结束时自动释放所有引用
}
```

## 性能建议

1. **预热常用类型**: 在应用启动时预热经常使用的引用类型
2. **避免过度释放**: 不要重复释放同一个引用
3. **及时释放**: 不再使用的引用应及时释放回池中
4. **监控使用**: 定期检查引用池使用情况，发现潜在问题
5. **合理配置**: 根据需要启用或禁用严格检查模式

## 迁移指南

从旧版本迁移到优化版本：

1. **无需修改现有代码**: 所有原有API保持兼容
2. **移除显式锁**: 如果你的代码中有对引用池的额外锁定，可以移除
3. **更新错误处理**: 新版本的错误处理更加完善
4. **使用新特性**: 逐步采用新的扩展方法和监控功能

## 注意事项

1. **线程安全**: 框架本身是线程安全的，但引用对象的使用仍需注意线程安全
2. **内存泄漏**: 忘记释放引用会导致内存泄漏，使用监控工具定期检查
3. **类型限制**: 只能池化实现了 `IReference` 接口的类型
4. **性能权衡**: 严格检查模式提供更好的调试体验但会影响性能 
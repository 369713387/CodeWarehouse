using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

/*
 * =====================================================
 * Dictionary和List装箱验证脚本
 * =====================================================
 * 
 * 目的：验证当struct被用作Dictionary的键或在List.Contains()中查找时的装箱问题
 * 
 * 装箱原理：
 * 1. 当struct实现IEquatable<T>时，Dictionary和List会优先调用强类型的Equals(T other)方法
 * 2. 当struct未实现IEquatable<T>时，只能调用Equals(object obj)方法，导致装箱
 * 3. 装箱会在堆上创建引用类型副本，产生GC分配
 * 
 * 测试对比：
 * - Vec2 (装箱版本): 未实现IEquatable<Vec2>，总是产生装箱
 * - Vec2Optimized (优化版本): 实现IEquatable<Vec2Optimized>，避免装箱
 * 
 * 使用方法：
 * - 按Space键: 单次测试，查看控制台日志观察哪个Equals方法被调用
 * - 按B键: 批量测试，观察GC分配差异
 * - 按C键: 性能对比测试，查看性能差异
 * 
 * 预期结果：
 * - 装箱版本会显示"调用object"日志，产生GC分配
 * - 优化版本会显示调用强类型Equals方法的日志，无GC分配
 */

public class DictionaryTest : MonoBehaviour
{
    [Header("装箱版本 - 未实现IEquatable<T> (现有Vec2)")]
    public Dictionary<Vec2, object> dicBoxing = new Dictionary<Vec2, object>();
    public List<Vec2> listBoxing = new List<Vec2>();
    
    [Header("优化版本 - 实现了IEquatable<T>")]
    public Dictionary<Vec2Optimized, object> dic = new Dictionary<Vec2Optimized, object>();
    public List<Vec2Optimized> list = new List<Vec2Optimized>();

    private Vec2 matchKeyBoxing = new Vec2(1,1,10);
    private Vec2Optimized matchKey = new Vec2Optimized(1,1,10);
    
    [Header("测试控制")]
    public int testIterations = 1000;
    
    void Start()
    {
        Debug.Log("=== Dictionary和List装箱测试 ===");
        Debug.Log("按 Space - 单次测试");
        Debug.Log("按 B - 批量测试(观察GC分配)");
        Debug.Log("按 C - 对比测试");
        
        InitializeTestData();
    }
    
    void InitializeTestData()
    {
        List<Vec2Optimized> keys = new List<Vec2Optimized>()
        {
            new Vec2Optimized(2,1,10), new Vec2Optimized(3,1,10), new Vec2Optimized(4,1,10),
            new Vec2Optimized(5,1,10), new Vec2Optimized(6,1,10), new Vec2Optimized(1,2,10),
            new Vec2Optimized(1,3,10), new Vec2Optimized(1,4,10), new Vec2Optimized(1,5,10),
            new Vec2Optimized(1,6,10), new Vec2Optimized(1,1,10), new Vec2Optimized(2,2,10),
            new Vec2Optimized(3,3,10), new Vec2Optimized(4,4,10), new Vec2Optimized(5,5,10),
            new Vec2Optimized(6,6,10),
        };
        
        List<Vec2> keysBoxing = new List<Vec2>()
        {
            new Vec2(2,1,10), new Vec2(3,1,10), new Vec2(4,1,10),
            new Vec2(5,1,10), new Vec2(6,1,10), new Vec2(1,2,10),
            new Vec2(1,3,10), new Vec2(1,4,10), new Vec2(1,5,10),
            new Vec2(1,6,10), new Vec2(1,1,10), new Vec2(2,2,10),
            new Vec2(3,3,10), new Vec2(4,4,10), new Vec2(5,5,10),
            new Vec2(6,6,10),
        };
        
        // 初始化Dictionary和List
        foreach (var item in keys)
        {
            dic.Add(item, UnityEngine.Random.Range(0,10));
        }
        list = dic.Keys.ToList();
        
        foreach (var item in keysBoxing)
        {
            dicBoxing.Add(item, UnityEngine.Random.Range(0,10));
        }
        listBoxing = dicBoxing.Keys.ToList();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SingleTest();
        }
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            BatchTest();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ComparisonTest();
        }
    }
    
    void SingleTest()
    {
        Debug.Log("\n=== 单次测试 ===");
        
        Debug.Log("1. 测试装箱版本(现有Vec2，未实现IEquatable<T>):");
        bool found3 = dicBoxing.ContainsKey(matchKeyBoxing);
        bool found4 = listBoxing.Contains(matchKeyBoxing);
        Debug.Log($"Dictionary找到: {found3}, List找到: {found4}");
        
        Debug.Log("\n2. 测试优化版本(Vec2Optimized，实现IEquatable<T>):");
        bool found1 = dic.ContainsKey(matchKey);
        bool found2 = list.Contains(matchKey);
        Debug.Log($"Dictionary找到: {found1}, List找到: {found2}");
    }
    
    async void BatchTest()
    {
        Debug.Log("\n=== 批量测试 - 观察GC分配 ===");
        
        // 测试装箱版本
        Debug.Log("测试装箱版本(现有Vec2)...");
        Profiler.BeginSample("Boxing Version Test");
        long beforeGC = GC.GetTotalMemory(false);
        
        for (int i = 0; i < testIterations; i++)
        {
            dicBoxing.ContainsKey(matchKeyBoxing);
            listBoxing.Contains(matchKeyBoxing);
        }

        long afterGC = GC.GetTotalMemory(false);
        Profiler.EndSample();
        Debug.Log($"装箱版本 GC分配: {afterGC - beforeGC} bytes");

        await Task.Delay(200);

        // 强制GC，清理内存
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // 测试优化版本
        Debug.Log("\n测试优化版本(Vec2Optimized)...");
        Profiler.BeginSample("Optimized Version Test");
        beforeGC = GC.GetTotalMemory(false);
        
        for (int i = 0; i < testIterations; i++)
        {
            dic.ContainsKey(matchKey);
            list.Contains(matchKey);
        }
        
        afterGC = GC.GetTotalMemory(false);
        Profiler.EndSample();
        Debug.Log($"优化版本 GC分配: {afterGC - beforeGC} bytes");
    }
    
    void ComparisonTest()
    {
        Debug.Log("\n=== 性能对比测试 ===");
        
        // Dictionary测试 - 装箱版本
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < testIterations; i++)
        {
            dicBoxing.ContainsKey(matchKeyBoxing);
        }
        watch.Stop();
        long boxingDicTime = watch.ElapsedTicks;
        
        // Dictionary测试 - 优化版本
        watch.Restart();
        for (int i = 0; i < testIterations; i++)
        {
            dic.ContainsKey(matchKey);
        }
        watch.Stop();
        long optimizedDicTime = watch.ElapsedTicks;
        
        // List测试 - 装箱版本
        watch.Restart();
        for (int i = 0; i < testIterations; i++)
        {
            listBoxing.Contains(matchKeyBoxing);
        }
        watch.Stop();
        long boxingListTime = watch.ElapsedTicks;
        
        // List测试 - 优化版本
        watch.Restart();
        for (int i = 0; i < testIterations; i++)
        {
            list.Contains(matchKey);
        }
        watch.Stop();
        long optimizedListTime = watch.ElapsedTicks;
        
        Debug.Log($"Dictionary测试 ({testIterations}次):");
        Debug.Log($"  装箱版本(Vec2): {boxingDicTime} ticks");
        Debug.Log($"  优化版本(Vec2Optimized): {optimizedDicTime} ticks");
        Debug.Log($"  性能差异: {(double)boxingDicTime / optimizedDicTime:F2}x");
        
        Debug.Log($"\nList测试 ({testIterations}次):");
        Debug.Log($"  装箱版本(Vec2): {boxingListTime} ticks");
        Debug.Log($"  优化版本(Vec2Optimized): {optimizedListTime} ticks");
        Debug.Log($"  性能差异: {(double)boxingListTime / optimizedListTime:F2}x");
    }
}


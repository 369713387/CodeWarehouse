using System;
using UnityEngine;

namespace FSMFrame
{
    /// <summary>
    /// 引用池使用示例
    /// </summary>
    public class ReferencePoolExample : MonoBehaviour
    {
        // 示例引用类
        public class GameObjectReference : IReference
        {
            public string Name { get; set; }
            public Vector3 Position { get; set; }
            public int Health { get; set; }
            
            public void Clear()
            {
                Name = null;
                Position = Vector3.zero;
                Health = 0;
            }
        }
        
        // 带参数初始化的引用类
        public class ConfigurableGameObject : IReference<string>
        {
            public string PrefabName { get; private set; }
            public Vector3 SpawnPosition { get; set; }
            
            public void Initialize(string prefabName)
            {
                PrefabName = prefabName;
                SpawnPosition = Vector3.zero;
            }
            
            public void Clear()
            {
                PrefabName = null;
                SpawnPosition = Vector3.zero;
            }
        }

        private void Start()
        {
            // 启用严格检查（开发阶段推荐）
            ReferencePool.EnableStrictCheck = true;
            
            // 预热引用池
            Debug.Log("=== 预热引用池 ===");
            ReferencePoolExtensions.Warmup<GameObjectReference>(10);
            
            // 基础使用示例
            BasicUsageExample();
            
            // 高级使用示例
            AdvancedUsageExample();
            
            // 批量操作示例
            BatchOperationExample();
            
            // 监控示例
            MonitoringExample();
        }

        private void BasicUsageExample()
        {
            Debug.Log("\n=== 基础使用示例 ===");
            
            // 获取引用
            var gameObj = ReferencePool.Acquire<GameObjectReference>();
            gameObj.Name = "Enemy";
            gameObj.Position = new Vector3(10, 0, 5);
            gameObj.Health = 100;
            
            Debug.Log($"创建游戏对象: {gameObj.Name} at {gameObj.Position} with {gameObj.Health} HP");
            
            // 释放引用
            ReferencePool.Release(gameObj);
            Debug.Log("游戏对象已释放回引用池");
        }

        private void AdvancedUsageExample()
        {
            Debug.Log("\n=== 高级使用示例 ===");
            
            // 参数化初始化
            var configObj = ReferencePoolExtensions.Acquire<ConfigurableGameObject, string>("EnemyPrefab");
            configObj.SpawnPosition = new Vector3(5, 0, 10);
            
            Debug.Log($"创建可配置对象: {configObj.PrefabName} at {configObj.SpawnPosition}");
            
            // 安全释放
            ReferencePoolExtensions.SafeRelease(ref configObj);
            Debug.Log($"安全释放后，configObj 是否为 null: {configObj == null}");
        }

        private void BatchOperationExample()
        {
            Debug.Log("\n=== 批量操作示例 ===");
            
            // 批量获取
            var enemies = ReferencePoolExtensions.AcquireBatch<GameObjectReference>(5);
            
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].Name = $"Enemy_{i}";
                enemies[i].Position = new Vector3(i * 2, 0, 0);
                enemies[i].Health = 100 + i * 10;
                
                Debug.Log($"批量创建: {enemies[i].Name} at {enemies[i].Position}");
            }
            
            // 批量释放
            ReferencePoolExtensions.ReleaseBatch(enemies);
            Debug.Log("批量释放完成");
        }

        private void MonitoringExample()
        {
            Debug.Log("\n=== 监控示例 ===");
            
            // 创建一些引用用于测试
            var ref1 = ReferencePool.Acquire<GameObjectReference>();
            var ref2 = ReferencePool.Acquire<GameObjectReference>();
            var ref3 = ReferencePool.Acquire<ConfigurableGameObject>();
            
            // 只释放部分引用，模拟使用中的状态
            ReferencePool.Release(ref1);
            
            // 获取使用报告
            string report = ReferencePoolMonitor.GetUsageReport();
            Debug.Log($"引用池使用报告:\n{report}");
            
            // 检查潜在泄漏
            var leaks = ReferencePoolMonitor.CheckLeaks();
            if (leaks.Count > 0)
            {
                Debug.LogWarning("检测到潜在泄漏:");
                foreach (var leak in leaks)
                {
                    Debug.LogWarning($"  类型 {leak.Key.Name}: {leak.Value} 个可能的泄漏");
                }
            }
            else
            {
                Debug.Log("未检测到内存泄漏");
            }
            
            // 清理
            ReferencePool.Release(ref2);
            ReferencePool.Release(ref3);
        }

        private void OnDestroy()
        {
            // 应用结束时清理所有引用池
            ReferencePool.ClearAll();
            Debug.Log("引用池已清理");
        }

        // Unity Inspector 中的测试按钮
        [ContextMenu("Test Reference Pool Performance")]
        private void TestPerformance()
        {
            Debug.Log("\n=== 性能测试 ===");
            
            int testCount = 10000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 测试获取和释放性能
            for (int i = 0; i < testCount; i++)
            {
                var reference = ReferencePool.Acquire<GameObjectReference>();
                reference.Name = $"Test_{i}";
                reference.Health = i;
                ReferencePool.Release(reference);
            }
            
            stopwatch.Stop();
            Debug.Log($"完成 {testCount} 次获取/释放操作，耗时: {stopwatch.ElapsedMilliseconds} ms");
            Debug.Log($"平均每次操作: {(double)stopwatch.ElapsedMilliseconds / testCount:F4} ms");
            
            // 显示最终统计
            var finalReport = ReferencePoolMonitor.GetUsageReport();
            Debug.Log($"最终报告:\n{finalReport}");
        }

        [ContextMenu("Clear All Pools")]
        private void ClearAllPools()
        {
            ReferencePool.ClearAll();
            Debug.Log("所有引用池已清理");
        }
    }
} 
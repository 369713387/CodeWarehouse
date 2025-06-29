using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace Framework.Instance
{
    /// <summary>
    /// 单例模式测试脚本
    /// </summary>
    public class SingletonTest : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private int _threadCount = 10;
        [SerializeField] private int _operationsPerThread = 100;

        private void Start()
        {
            Debug.Log("=== 开始单例模式测试 ===");
            
            // 测试普通单例
            TestSingleton();
            
            // 测试 MonoBehaviour 单例
            TestMonoSingleton();
            
            // 测试线程安全
            TestThreadSafety();
        }

        /// <summary>
        /// 测试普通单例模式
        /// </summary>
        private void TestSingleton()
        {
            Debug.Log("--- 测试普通单例模式 ---");
            
            // 获取 GameManager 实例
            var gameManager = GameManager.Instance;
            Debug.Log($"GameManager 实例创建: {gameManager != null}");
            
            // 测试功能
            gameManager.AddScore(100);
            gameManager.AddScore(50);
            Debug.Log($"当前分数: {gameManager.Score}");
            
            // 测试单例特性 - 多次获取应该是同一个实例
            var gameManager2 = GameManager.Instance;
            Debug.Log($"两次获取是同一个实例: {ReferenceEquals(gameManager, gameManager2)}");
            
            // 测试 ConfigManager
            var configManager = ConfigManager.Instance;
            configManager.UpdateConfig("TestPlayer", 0.8f, true);
            Debug.Log($"配置管理器玩家名称: {configManager.PlayerName}");
        }

        /// <summary>
        /// 测试 MonoBehaviour 单例模式
        /// </summary>
        private void TestMonoSingleton()
        {
            Debug.Log("--- 测试 MonoBehaviour 单例模式 ---");
            
            // 获取 AudioManager 实例
            var audioManager = AudioManager.Instance;
            Debug.Log($"AudioManager 实例创建: {audioManager != null}");
            
            // 测试单例特性
            var audioManager2 = AudioManager.Instance;
            Debug.Log($"两次获取是同一个实例: {ReferenceEquals(audioManager, audioManager2)}");
            
            // 检查是否设置了 DontDestroyOnLoad
            if (audioManager != null)
            {
                Debug.Log($"AudioManager GameObject 名称: {audioManager.gameObject.name}");
            }
        }

        /// <summary>
        /// 测试线程安全性
        /// </summary>
        private async void TestThreadSafety()
        {
            Debug.Log("--- 测试线程安全性 ---");
            
            // 用于存储所有获取到的实例
            var instances = new ConcurrentBag<GameManager>();
            
            // 创建多个任务同时访问单例
            var tasks = new Task[_threadCount];
            
            for (int i = 0; i < _threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < _operationsPerThread; j++)
                    {
                        // 获取单例实例
                        var instance = GameManager.Instance;
                        instances.Add(instance);
                        
                        // 模拟一些操作
                        instance.AddScore(1);
                        
                        // 添加一些延迟来增加竞争条件
                        Thread.Sleep(1);
                    }
                    
                    Debug.Log($"线程 {threadId} 完成");
                });
            }
            
            // 等待所有任务完成
            await Task.WhenAll(tasks);
            
            // 验证所有实例都是同一个
            var firstInstance = instances.FirstOrDefault();
            bool allSameInstance = instances.All(instance => ReferenceEquals(instance, firstInstance));
            
            Debug.Log($"线程安全测试结果:");
            Debug.Log($"- 总操作数: {instances.Count}");
            Debug.Log($"- 所有实例都相同: {allSameInstance}");
            Debug.Log($"- 最终分数: {firstInstance?.Score}");
            
            // 测试 HasInstance 方法
            Debug.Log($"GameManager.HasInstance: {GameManager.HasInstance}");
            Debug.Log($"AudioManager.HasInstance: {AudioManager.HasInstance}");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            GUILayout.Label("单例模式测试控制面板", GUI.skin.label);
            
            if (GUILayout.Button("重新测试普通单例"))
            {
                TestSingleton();
            }
            
            if (GUILayout.Button("重新测试 Mono 单例"))
            {
                TestMonoSingleton();
            }
            
            if (GUILayout.Button("重新测试线程安全"))
            {
                TestThreadSafety();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("销毁 GameManager"))
            {
                GameManager.DestroyInstance();
                Debug.Log("GameManager 已销毁");
            }
            
            if (GUILayout.Button("销毁 AudioManager"))
            {
                AudioManager.DestroyInstance();
                Debug.Log("AudioManager 已销毁");
            }
            
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Debug.Log("SingletonTest 被销毁");
        }
    }
} 
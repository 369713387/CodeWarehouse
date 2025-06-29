using System.Collections;
using UnityEngine;
using JSAM;

/// <summary>
/// AudioManagerExtensions测试脚本
/// 用于验证新的ObjectPool实现
/// </summary>
public class AudioManagerExtensionsTest : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private SoundFileObject _testSoundFile;
    [SerializeField] private bool _autoTest = true;
    [SerializeField] private float _testInterval = 2f;
    [SerializeField] private int _poolSize = 10;

    [Header("调试信息")]
    [SerializeField] private bool _showPoolStats = true;

    private Coroutine _testCoroutine;

    void Start()
    {
        // 初始化音频控制器对象池
        AudioManagerExtensions.InitializeAudioControllerPool(_poolSize);
        
        // 预热对象池
        AudioManagerExtensions.WarmupAudioControllerPool(5);

        if (_autoTest)
        {
            StartTest();
        }
    }

    void Update()
    {
        if (_showPoolStats && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log(AudioManagerExtensions.GetAudioControllerPoolStats());
        }
    }

    public void StartTest()
    {
        if (_testCoroutine != null)
        {
            StopCoroutine(_testCoroutine);
        }
        _testCoroutine = StartCoroutine(TestRoutine());
    }

    public void StopTest()
    {
        if (_testCoroutine != null)
        {
            StopCoroutine(_testCoroutine);
            _testCoroutine = null;
        }
    }

    private IEnumerator TestRoutine()
    {
        while (true)
        {
            // 测试异步播放
            TestAsyncPlayback();
            
            yield return new WaitForSeconds(_testInterval / 2f);
            
            // 测试控制器播放
            TestControllerPlayback();
            
            yield return new WaitForSeconds(_testInterval / 2f);
        }
    }

    private async void TestAsyncPlayback()
    {
        if (_testSoundFile == null) return;

        Debug.Log("[Test] 开始异步播放测试");
        
        try
        {
            await AudioManagerExtensions.PlaySoundAsync(_testSoundFile, transform, 
                onComplete: () => Debug.Log("[Test] 异步播放完成"));
            
            Debug.Log("[Test] 异步播放任务完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Test] 异步播放出错: {ex.Message}");
        }
    }

    private void TestControllerPlayback()
    {
        if (_testSoundFile == null) return;

        Debug.Log("[Test] 开始控制器播放测试");
        
        var controller = AudioManagerExtensions.PlaySoundWithController(_testSoundFile, transform, 
            onComplete: () => Debug.Log("[Test] 控制器播放完成"));
        
        if (controller != null)
        {
            Debug.Log($"[Test] 获得控制器: {controller.gameObject.name}, 状态: {controller.StatusDescription}");
            
            // 启动协程来监控控制器状态
            StartCoroutine(MonitorController(controller));
        }
        else
        {
            Debug.LogError("[Test] 无法获得播放控制器");
        }
    }

    private IEnumerator MonitorController(AudioPlaybackController controller)
    {
        float monitorTime = 0f;
        
        while (controller != null && controller.IsActive && monitorTime < 10f)
        {
            Debug.Log($"[Test] 控制器状态: {controller.StatusDescription}, 进度: {controller.Progress:P1}");
            
            yield return new WaitForSeconds(0.5f);
            monitorTime += 0.5f;
        }
        
        if (controller != null)
        {
            Debug.Log($"[Test] 控制器监控结束: {controller.StatusDescription}");
        }
    }

    [ContextMenu("显示对象池统计")]
    public void ShowPoolStats()
    {
        Debug.Log("=== 音频控制器对象池统计 ===");
        Debug.Log(AudioManagerExtensions.GetAudioControllerPoolStats());
    }

    [ContextMenu("预热对象池")]
    public void WarmupPool()
    {
        AudioManagerExtensions.WarmupAudioControllerPool(5);
        ShowPoolStats();
    }

    [ContextMenu("清空对象池")]
    public void ClearPool()
    {
        AudioManagerExtensions.ClearAudioControllerPool();
        ShowPoolStats();
    }

    [ContextMenu("测试单次播放")]
    public void TestSinglePlay()
    {
        TestControllerPlayback();
    }

    void OnDestroy()
    {
        StopTest();
    }

    void OnGUI()
    {
        if (!_showPoolStats) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("音频控制器对象池测试");
        
        if (GUILayout.Button("显示统计信息"))
        {
            ShowPoolStats();
        }
        
        if (GUILayout.Button("预热对象池"))
        {
            WarmupPool();
        }
        
        if (GUILayout.Button("清空对象池"))
        {
            ClearPool();
        }
        
        if (GUILayout.Button("测试单次播放"))
        {
            TestSinglePlay();
        }
        
        GUILayout.Label($"测试运行中: {(_testCoroutine != null ? "是" : "否")}");
        
        if (_testCoroutine == null)
        {
            if (GUILayout.Button("开始自动测试"))
            {
                StartTest();
            }
        }
        else
        {
            if (GUILayout.Button("停止自动测试"))
            {
                StopTest();
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
} 
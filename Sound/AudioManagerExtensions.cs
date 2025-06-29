using System;
using System.Threading;
using UnityEngine;
using JSAM;
using Cysharp.Threading.Tasks;
using YiFun.Pool;

public static class AudioManagerExtensions
{
    #region 常量和配置
    private const int DEFAULT_POOL_SIZE = 10;
    private const bool ENABLE_POOL_DEBUG = true;
    #endregion

    #region 静态字段
    private static ObjectPool<AudioPlaybackController> _audioControllerPool;
    private static bool _poolInitialized = false;
    #endregion
    /// <summary>
    /// 播放音效并返回一个可等待的UniTask，支持播放结束回调和提前终止
    /// </summary>
    /// <typeparam name="T">音效枚举类型</typeparam>
    /// <param name="sound">要播放的音效枚举</param>
    /// <param name="transform">播放位置的Transform（可选）</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <param name="cancellationToken">用于提前终止播放的取消令牌</param>
    /// <returns>返回一个UniTask，当音效播放完成时完成</returns>
    public static async UniTask PlaySoundAsync<T>(T sound, Transform transform = null, Action onComplete = null, CancellationToken cancellationToken = default) where T : Enum
    {
        var helper = AudioManager.PlaySound(sound, transform);
        if (helper == null) return;

        await WaitForSoundCompletion(helper, onComplete, cancellationToken);
    }

    /// <summary>
    /// 播放音效并返回一个可等待的UniTask，支持播放结束回调和提前终止
    /// </summary>
    /// <param name="soundFile">要播放的音效文件对象</param>
    /// <param name="transform">播放位置的Transform（可选）</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <param name="cancellationToken">用于提前终止播放的取消令牌</param>
    /// <returns>返回一个UniTask，当音效播放完成时完成</returns>
    public static async UniTask PlaySoundAsync(SoundFileObject soundFile, Transform transform = null, Action onComplete = null, CancellationToken cancellationToken = default)
    {
        var helper = AudioManager.PlaySound(soundFile, transform);
        if (helper == null) return;

        await WaitForSoundCompletion(helper, onComplete, cancellationToken);
    }

    /// <summary>
    /// 播放音效并返回一个可等待的UniTask，支持播放结束回调和提前终止
    /// </summary>
    /// <typeparam name="T">音效枚举类型</typeparam>
    /// <param name="sound">要播放的音效枚举</param>
    /// <param name="position">播放位置的世界坐标</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <param name="cancellationToken">用于提前终止播放的取消令牌</param>
    /// <returns>返回一个UniTask，当音效播放完成时完成</returns>
    public static async UniTask PlaySoundAsync<T>(T sound, Vector3 position, Action onComplete = null, CancellationToken cancellationToken = default) where T : Enum
    {
        var helper = AudioManager.PlaySound(sound, position);
        if (helper == null) return;

        await WaitForSoundCompletion(helper, onComplete, cancellationToken);
    }

    /// <summary>
    /// 播放音效并返回一个可等待的UniTask，支持播放结束回调和提前终止
    /// </summary>
    /// <param name="soundFile">要播放的音效文件对象</param>
    /// <param name="position">播放位置的世界坐标</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <param name="cancellationToken">用于提前终止播放的取消令牌</param>
    /// <returns>返回一个UniTask，当音效播放完成时完成</returns>
    public static async UniTask PlaySoundAsync(SoundFileObject soundFile, Vector3 position, Action onComplete = null, CancellationToken cancellationToken = default)
    {
        var helper = AudioManager.PlaySound(soundFile, position);
        if (helper == null) return;

        await WaitForSoundCompletion(helper, onComplete, cancellationToken);
    }

    /// <summary>
    /// 等待音效播放完成
    /// </summary>
    /// <param name="helper">音效通道助手</param>
    /// <param name="onComplete">播放完成时的回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回一个UniTask</returns>
    private static async UniTask WaitForSoundCompletion(SoundChannelHelper helper, Action onComplete, CancellationToken cancellationToken)
    {
        if (helper?.AudioSource == null) return;

        try
        {
            // 等待音效播放完成或被取消
            await UniTask.WaitUntil(() => 
            {
                return helper.AudioSource == null || !helper.AudioSource.isPlaying;
            }, cancellationToken: cancellationToken);

            // 播放完成回调
            onComplete?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // 如果被取消，停止音效播放
            if (helper?.AudioSource != null && helper.AudioSource.isPlaying)
            {
                helper.Stop(true);
            }
            throw; // 重新抛出取消异常
        }
    }

    #region 对象池初始化
    /// <summary>
    /// 初始化音频控制器对象池
    /// </summary>
    /// <param name="poolSize">池大小</param>
    /// <param name="maxSize">最大池大小</param>
    public static void InitializeAudioControllerPool(int poolSize = DEFAULT_POOL_SIZE, int maxSize = -1)
    {
        if (_poolInitialized)
        {
            Debug.LogWarning("[AudioManagerExtensions] 音频控制器对象池已经初始化");
            return;
        }

        var poolManager = PoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogError("[AudioManagerExtensions] 无法找到PoolManager实例");
            return;
        }

        // 创建AudioPlaybackController对象池
        _audioControllerPool = poolManager.CreateTypedPool<AudioPlaybackController>(
            createFunc: CreateAudioController,
            onGet: OnGetController,
            onRelease: OnReleaseController,
            onDestroy: OnDestroyController,
            maxSize: maxSize,
            preloadCount: poolSize
        );

        if (_audioControllerPool != null)
        {
            _poolInitialized = true;
            if (ENABLE_POOL_DEBUG)
                Debug.Log($"[AudioManagerExtensions] 音频控制器对象池初始化成功，池大小: {poolSize}");
        }
        else
        {
            Debug.LogError("[AudioManagerExtensions] 创建音频控制器对象池失败");
        }
    }

    /// <summary>
    /// 自动初始化音频控制器对象池（如果尚未初始化）
    /// </summary>
    private static void EnsurePoolInitialized()
    {
        if (_poolInitialized) return;

        InitializeAudioControllerPool();
    }

    /// <summary>
    /// 创建AudioPlaybackController实例
    /// </summary>
    /// <returns>新的AudioPlaybackController实例</returns>
    private static AudioPlaybackController CreateAudioController()
    {
        var go = new GameObject("AudioPlaybackController");
        var controller = go.AddComponent<AudioPlaybackController>();
        
        // 设置父对象到PoolManager下
        var poolManager = PoolManager.Instance;
        if (poolManager != null)
        {
            go.transform.SetParent(poolManager.transform);
        }
        
        go.SetActive(false);
        
        if (ENABLE_POOL_DEBUG)
            Debug.Log("[AudioManagerExtensions] 创建新的AudioPlaybackController");
        
        return controller;
    }

    /// <summary>
    /// 从对象池获取控制器时的回调
    /// </summary>
    /// <param name="controller">控制器实例</param>
    private static void OnGetController(AudioPlaybackController controller)
    {
        if (controller != null)
        {
            controller.gameObject.SetActive(true);
            controller.OnSpawn();
        }
    }

    /// <summary>
    /// 控制器回收到对象池时的回调
    /// </summary>
    /// <param name="controller">控制器实例</param>
    private static void OnReleaseController(AudioPlaybackController controller)
    {
        if (controller != null)
        {
            controller.OnRecycle();
            controller.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 销毁控制器时的回调
    /// </summary>
    /// <param name="controller">控制器实例</param>
    private static void OnDestroyController(AudioPlaybackController controller)
    {
        if (controller != null && controller.gameObject != null)
        {
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
        }
    }
    #endregion

    #region 优化的播放控制器方法
    /// <summary>
    /// 播放音效并返回一个可以手动控制的PlaybackController（使用对象池）
    /// </summary>
    /// <typeparam name="T">音效枚举类型</typeparam>
    /// <param name="sound">要播放的音效枚举</param>
    /// <param name="transform">播放位置的Transform（可选）</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <returns>返回一个PlaybackController，可以用来控制播放</returns>
    public static AudioPlaybackController PlaySoundWithController<T>(T sound, Transform transform = null, Action onComplete = null) where T : Enum
    {
        EnsurePoolInitialized();
        
        var helper = AudioManager.PlaySound(sound, transform);
        if (helper == null)
        {
            Debug.LogWarning($"[AudioManagerExtensions] 播放音效失败: {sound}");
            return null;
        }

        return GetControllerFromPool(helper, onComplete, transform);
    }

    /// <summary>
    /// 播放音效并返回一个可以手动控制的PlaybackController（使用对象池）
    /// </summary>
    /// <param name="soundFile">要播放的音效文件对象</param>
    /// <param name="transform">播放位置的Transform（可选）</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <returns>返回一个PlaybackController，可以用来控制播放</returns>
    public static AudioPlaybackController PlaySoundWithController(SoundFileObject soundFile, Transform transform = null, Action onComplete = null)
    {
        EnsurePoolInitialized();
        
        var helper = AudioManager.PlaySound(soundFile, transform);
        if (helper == null)
        {
            Debug.LogWarning($"[AudioManagerExtensions] 播放音效失败: {soundFile?.name}");
            return null;
        }

        return GetControllerFromPool(helper, onComplete, transform);
    }

    /// <summary>
    /// 播放音效并返回一个可以手动控制的PlaybackController（使用对象池，带位置）
    /// </summary>
    /// <typeparam name="T">音效枚举类型</typeparam>
    /// <param name="sound">要播放的音效枚举</param>
    /// <param name="position">播放位置的世界坐标</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <returns>返回一个PlaybackController，可以用来控制播放</returns>
    public static AudioPlaybackController PlaySoundWithController<T>(T sound, Vector3 position, Action onComplete = null) where T : Enum
    {
        EnsurePoolInitialized();
        
        var helper = AudioManager.PlaySound(sound, position);
        if (helper == null)
        {
            Debug.LogWarning($"[AudioManagerExtensions] 播放音效失败: {sound}");
            return null;
        }

        return GetControllerFromPool(helper, onComplete, null, position);
    }

    /// <summary>
    /// 播放音效并返回一个可以手动控制的PlaybackController（使用对象池，带位置）
    /// </summary>
    /// <param name="soundFile">要播放的音效文件对象</param>
    /// <param name="position">播放位置的世界坐标</param>
    /// <param name="onComplete">播放完成时的回调（可选）</param>
    /// <returns>返回一个PlaybackController，可以用来控制播放</returns>
    public static AudioPlaybackController PlaySoundWithController(SoundFileObject soundFile, Vector3 position, Action onComplete = null)
    {
        EnsurePoolInitialized();
        
        var helper = AudioManager.PlaySound(soundFile, position);
        if (helper == null)
        {
            Debug.LogWarning($"[AudioManagerExtensions] 播放音效失败: {soundFile?.name}");
            return null;
        }

        return GetControllerFromPool(helper, onComplete, null, position);
    }

    /// <summary>
    /// 从对象池获取AudioPlaybackController
    /// </summary>
    /// <param name="helper">音频通道助手</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="parent">父对象</param>
    /// <param name="position">世界坐标位置（可选）</param>
    /// <returns>AudioPlaybackController实例</returns>
    private static AudioPlaybackController GetControllerFromPool(SoundChannelHelper helper, Action onComplete, Transform parent, Vector3? position = null)
    {
        if (_audioControllerPool == null)
        {
            Debug.LogError("[AudioManagerExtensions] 音频控制器对象池未初始化");
            return null;
        }

        // 从对象池获取控制器
        var controller = _audioControllerPool.Get();
        if (controller == null)
        {
            Debug.LogError("[AudioManagerExtensions] 无法从对象池获取AudioPlaybackController");
            return null;
        }

        // 设置位置
        if (position.HasValue)
        {
            controller.transform.position = position.Value;
        }
        else if (parent != null)
        {
            controller.transform.position = parent.position;
            controller.transform.SetParent(parent);
        }

        // 初始化控制器
        controller.Initialize(helper, onComplete);

        if (ENABLE_POOL_DEBUG)
            Debug.Log($"[AudioManagerExtensions] 从对象池获取AudioPlaybackController: {controller.gameObject.name}");

        return controller;
    }

    /// <summary>
    /// 将AudioPlaybackController回收到对象池
    /// </summary>
    /// <param name="controller">要回收的控制器</param>
    /// <returns>是否成功回收</returns>
    public static bool ReleaseController(AudioPlaybackController controller)
    {
        if (_audioControllerPool == null)
        {
            Debug.LogWarning("[AudioManagerExtensions] 音频控制器对象池未初始化");
            return false;
        }

        if (controller == null)
        {
            Debug.LogWarning("[AudioManagerExtensions] 尝试回收null控制器");
            return false;
        }

        return _audioControllerPool.Release(controller);
    }
    #endregion

    #region 对象池管理方法
    /// <summary>
    /// 获取音频控制器对象池统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public static string GetAudioControllerPoolStats()
    {
        if (!_poolInitialized || _audioControllerPool == null)
            return "音频控制器对象池未初始化";

        return _audioControllerPool.GetStatsString();
    }

    /// <summary>
    /// 预热音频控制器对象池
    /// </summary>
    /// <param name="count">预热数量</param>
    public static void WarmupAudioControllerPool(int count = 5)
    {
        EnsurePoolInitialized();
        
        if (_audioControllerPool != null)
        {
            _audioControllerPool.Warmup(count);
            if (ENABLE_POOL_DEBUG)
                Debug.Log($"[AudioManagerExtensions] 预热音频控制器对象池: {count} 个对象");
        }
    }

    /// <summary>
    /// 清空音频控制器对象池
    /// </summary>
    public static void ClearAudioControllerPool()
    {
        if (!_poolInitialized || _audioControllerPool == null) return;

        _audioControllerPool.Clear();
        if (ENABLE_POOL_DEBUG)
            Debug.Log("[AudioManagerExtensions] 清空音频控制器对象池");
    }
    #endregion
}


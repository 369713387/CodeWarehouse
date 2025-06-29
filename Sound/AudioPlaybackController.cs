using System;
using System.Threading;
using UnityEngine;
using JSAM;
using Cysharp.Threading.Tasks;
using YiFun.Pool;

/// <summary>
/// 可池化的音频播放控制器，提供更灵活的音频控制功能
/// </summary>
public class AudioPlaybackController : PoolableComponent
{
    [Header("音频控制器设置")]
    [SerializeField] private bool _enableDebugLog = true;

    private SoundChannelHelper _helper;
    private Action _onComplete;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isCompleted;
    private bool _isCancelled;
    private bool _isPaused;
    private float _totalLength;

    #region 池化接口实现
    protected override void OnSpawnImplementation()
    {
        // 对象从池中获取时调用
        ResetController();
        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] 从池中获取: {gameObject.name}");
    }

    protected override void OnRecycleImplementation()
    {
        // 对象回收到池中时调用
        CleanupController();
        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] 回收到池中: {gameObject.name}");
    }

    protected override void CleanupState()
    {
        base.CleanupState();
        CleanupController();
    }
    #endregion

    #region 公开属性
    /// <summary>
    /// 是否正在播放（不包括暂停状态）
    /// </summary>
    public bool IsPlaying => _helper?.AudioSource != null && _helper.AudioSource.isPlaying && !_isCompleted && !_isCancelled && !_isPaused;

    /// <summary>
    /// 是否处于暂停状态
    /// </summary>
    public bool IsPaused => _isPaused && !_isCompleted && !_isCancelled;

    /// <summary>
    /// 是否已完成播放
    /// </summary>
    public bool IsCompleted => _isCompleted;

    /// <summary>
    /// 是否已被取消
    /// </summary>
    public bool IsCancelled => _isCancelled;

    /// <summary>
    /// 是否处于活跃状态（正在播放或暂停，但未完成也未取消）
    /// </summary>
    public bool IsActive => !_isCompleted && !_isCancelled;

    /// <summary>
    /// 获取当前播放状态的描述
    /// </summary>
    public string StatusDescription
    {
        get
        {
            if (_isCancelled) return "已取消";
            if (_isCompleted) return "已完成";
            if (_isPaused) return "已暂停";
            if (IsPlaying) return "播放中";
            return "未知状态";
        }
    }

    /// <summary>
    /// 获取当前播放进度 (0-1)
    /// </summary>
    public float Progress
    {
        get
        {
            if (_helper?.AudioSource == null || _helper.AudioSource.clip == null)
                return 0f;
            
            return _helper.AudioSource.time / _helper.AudioSource.clip.length;
        }
    }

    /// <summary>
    /// 获取剩余播放时间（秒）
    /// </summary>
    public float RemainingTime
    {
        get
        {
            if (_helper?.AudioSource == null || _helper.AudioSource.clip == null)
                return 0f;
            
            return Mathf.Max(0f, _helper.AudioSource.clip.length - _helper.AudioSource.time);
        }
    }
    #endregion

    #region 初始化方法
    /// <summary>
    /// 初始化音频播放控制器
    /// </summary>
    /// <param name="helper">音频通道助手</param>
    /// <param name="onComplete">完成回调</param>
    public void Initialize(SoundChannelHelper helper, Action onComplete = null)
    {
        _helper = helper;
        _onComplete = onComplete;
        _cancellationTokenSource = new CancellationTokenSource();
        _isCompleted = false;
        _isCancelled = false;
        _isPaused = false;
        _totalLength = _helper?.AudioSource?.clip?.length ?? 0f;

        // 开始监听播放完成
        if (_helper?.AudioSource != null)
        {
            MonitorPlayback().Forget();
        }

        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] 初始化完成，音频长度: {_totalLength:F1}s");
    }
    #endregion

    #region 控制方法
    /// <summary>
    /// 停止播放
    /// </summary>
    /// <param name="stopInstantly">是否立即停止</param>
    public void Stop(bool stopInstantly = true)
    {
        if (_isCancelled || _isCompleted) return;

        _isCancelled = true;
        _isPaused = false; // 停止时清除暂停状态
        _cancellationTokenSource?.Cancel();
        
        if (_helper?.AudioSource != null)
        {
            _helper.Stop(stopInstantly);
        }

        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] 停止播放: {gameObject.name}");

        // 延迟回收到池中
        ReturnToPoolAfter(0.1f);
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    /// <returns>返回暂停操作是否成功</returns>
    public bool Pause()
    {
        if (_helper?.AudioSource == null || _isCancelled || _isCompleted || _isPaused)
            return false;

        if (_helper.AudioSource.isPlaying)
        {
            _helper.AudioSource.Pause();
            _isPaused = true;
            if (_enableDebugLog)
                Debug.Log($"[AudioPlaybackController] 音频已暂停，当前播放时间: {_helper.AudioSource.time:F1}s");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 恢复播放
    /// </summary>
    /// <returns>返回恢复操作是否成功</returns>
    public bool Resume()
    {
        if (_helper?.AudioSource == null || _isCancelled || _isCompleted || !_isPaused)
            return false;

        _helper.AudioSource.UnPause();
        _isPaused = false;
        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] 音频已恢复播放，当前播放时间: {_helper.AudioSource.time:F1}s");
        return true;
    }

    /// <summary>
    /// 切换播放状态（播放/暂停）
    /// </summary>
    /// <returns>返回当前是否正在播放</returns>
    public bool TogglePlayPause()
    {
        if (_isPaused)
        {
            Resume();
            return true;
        }
        else if (IsPlaying)
        {
            Pause();
            return false;
        }
        return false;
    }
    #endregion

    #region 等待方法
    /// <summary>
    /// 等待播放完成的UniTask
    /// </summary>
    /// <returns>返回一个UniTask</returns>
    public async UniTask WaitForCompletion()
    {
        if (_isCompleted || _isCancelled) return;

        try
        {
            await UniTask.WaitUntil(() => _isCompleted || _isCancelled, 
                cancellationToken: _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // 被手动取消
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 重置控制器状态
    /// </summary>
    private void ResetController()
    {
        _helper = null;
        _onComplete = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _isCompleted = false;
        _isCancelled = false;
        _isPaused = false;
        _totalLength = 0f;
    }

    /// <summary>
    /// 清理控制器资源
    /// </summary>
    private void CleanupController()
    {
        // 停止播放
        if (_helper?.AudioSource != null && _helper.AudioSource.isPlaying)
        {
            _helper.Stop(true);
        }

        // 取消异步操作
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        // 清理引用
        _helper = null;
        _onComplete = null;
    }

    /// <summary>
    /// 监听播放状态
    /// </summary>
    private async UniTaskVoid MonitorPlayback()
    {
        try
        {
            // 监听播放直到真正完成（排除暂停状态）
            await UniTask.WaitUntil(() => 
            {
                if (_helper?.AudioSource == null || _isCancelled)
                    return true;

                // 如果正在暂停，继续等待
                if (_isPaused)
                    return false;

                // 检查是否真的播放完成了（到达了音频末尾）
                if (!_helper.AudioSource.isPlaying)
                {
                    // 如果不是暂停状态且播放时间接近总长度，认为播放完成
                    float currentTime = _helper.AudioSource.time;
                    float clipLength = _helper.AudioSource.clip?.length ?? _totalLength;
                    
                    // 允许0.1秒的误差，因为音频可能不会精确播放到最后
                    bool reachedEnd = currentTime >= clipLength - 0.1f;
                    
                    if (reachedEnd)
                    {
                        if (_enableDebugLog)
                            Debug.Log($"[AudioPlaybackController] 音频播放完成，播放时长: {currentTime:F1}s / {clipLength:F1}s");
                        return true;
                    }
                }

                return false;
            }, cancellationToken: _cancellationTokenSource.Token);

            // 只有在未被取消的情况下才标记为完成
            if (!_isCancelled)
            {
                _isCompleted = true;
                _isPaused = false;
                if (_enableDebugLog)
                    Debug.Log("[AudioPlaybackController] 播放监听完成，触发回调");
                
                _onComplete?.Invoke();

                // 播放完成后自动回收到池中
                ReturnToPoolAfter(0.1f);
            }
        }
        catch (OperationCanceledException)
        {
            if (_enableDebugLog)
                Debug.Log("[AudioPlaybackController] 播放监听被取消");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AudioPlaybackController] 监听播放时发生错误: {ex.Message}");
        }
    }
    #endregion

    #region Unity生命周期
    protected override void OnEnable()
    {
        base.OnEnable();
        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] OnEnable: {gameObject.name}");
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CleanupController();
        if (_enableDebugLog)
            Debug.Log($"[AudioPlaybackController] OnDisable: {gameObject.name}");
    }
    #endregion

    #region 对象池支持
    /// <summary>
    /// 重写基类的回收方法，使用AudioManagerExtensions的对象池
    /// </summary>
    public override void ReturnToPool()
    {
        // 使用AudioManagerExtensions的专用回收方法
        if (!AudioManagerExtensions.ReleaseController(this))
        {
            // 如果回收失败，使用基类的默认实现
            base.ReturnToPool();
        }
    }
    #endregion
} 
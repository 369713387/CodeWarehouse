using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

public class Cat : MonoBehaviour
{
    private CancellationTokenSource _cancellationTokenSource;
    private AudioPlaybackController _currentController;

    async UniTask Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        await UniTask.Delay(500);
        PlaySound();
    }

    void OnDestroy()
    {
        // 清理资源
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _currentController?.Stop();
    }

    [Button("播放音频")]
    void PlaySound()
    {        
        //方式一： 新的异步播放方式，带有播放结束回调
        //PlaySoundAsync().Forget();

        //方式二： 使用控制器方式播放，提供更多控制功能
        PlaySoundWithController();
    }

    void StopSound()
    {
        _currentController?.Stop();
    }

    /// <summary>
    /// 异步播放音效的示例方法
    /// </summary>
    private async UniTaskVoid PlaySoundAsync()
    {
        try
        {
            // 方式1: 使用简单的异步播放，带有完成回调
            await AudioManagerExtensions.PlaySoundAsync(
                CatSounds.alerte, 
                transform, 
                onComplete: () => Debug.Log("猫叫声播放完成！"),
                cancellationToken: _cancellationTokenSource.Token
            );
        }
        catch (OperationCanceledException)
        {
            Debug.Log("猫叫声播放被取消");
        }
    }

    /// <summary>
    /// 使用控制器播放音效的示例方法
    /// </summary>
    public void PlaySoundWithController()
    {
        // 停止当前播放
        _currentController?.Stop();

        // 方式2: 使用控制器方式播放，提供更多控制功能（现在使用对象池）
        _currentController = AudioManagerExtensions.PlaySoundWithController(
            CatSounds.alerte, 
            transform, 
            onComplete: () => 
            {
                Debug.Log($"猫叫声播放完成！播放进度: {_currentController?.Progress:P}");
                Debug.Log("音频控制器将自动回收到对象池");
            }
        );

        if (_currentController != null)
        {
            // 可以获取播放信息
            Debug.Log($"开始播放猫叫声，预计播放时间: {_currentController.RemainingTime}秒");

            // 启动一个监听任务
            MonitorPlayback().Forget();
        }
        else
        {
            Debug.LogError("无法创建音频控制器");
        }
    }

    /// <summary>
    /// 监听播放进度的示例
    /// </summary>
    private async UniTaskVoid MonitorPlayback()
    {
        if (_currentController == null) return;

        while (_currentController.IsActive)
        {
            string status = "";
            if (_currentController.IsPlaying)
                status = "播放中";
            else if (_currentController.IsPaused)
                status = "已暂停";
            else if (_currentController.IsCompleted)
                status = "已完成";
            else if (_currentController.IsCancelled)
                status = "已取消";

            Debug.Log($"状态: {status} | 播放进度: {_currentController.Progress:P} | 剩余时间: {_currentController.RemainingTime:F1}秒");
            await UniTask.Delay(500, cancellationToken: _cancellationTokenSource.Token);
        }

        if (_currentController.IsCompleted)
        {
            Debug.Log("播放正常完成");
        }
        else if (_currentController.IsCancelled)
        {
            Debug.Log("播放被取消");
        }
    }

    [Button("停止当前播放")]
    /// <summary>
    /// 手动停止当前播放
    /// </summary>
    public void StopCurrentSound()
    {
        _currentController?.Stop();
        Debug.Log("停止当前播放");
    }

    [Button("暂停当前播放")]
    /// <summary>
    /// 暂停当前播放
    /// </summary>
    public void PauseCurrentSound()
    {
        if (_currentController != null)
        {
            bool success = _currentController.Pause();
            if (success)
                Debug.Log($"暂停成功，当前进度: {_currentController.Progress:P}");
            else
                Debug.Log("暂停失败：音频可能已完成、取消或已处于暂停状态");
        }
        else
        {
            Debug.Log("没有正在播放的音频");
        }
    }

    [Button("恢复当前播放")]
    /// <summary>
    /// 恢复当前播放
    /// </summary>
    public void ResumeCurrentSound()
    {
        if (_currentController != null)
        {
            bool success = _currentController.Resume();
            if (success)
                Debug.Log($"恢复播放成功，当前进度: {_currentController.Progress:P}");
            else
                Debug.Log("恢复失败：音频可能已完成、取消或不在暂停状态");
        }
        else
        {
            Debug.Log("没有音频控制器");
        }
    }

    [Button("切换播放/暂停")]
    /// <summary>
    /// 切换播放/暂停状态
    /// </summary>
    public void TogglePlayPause()
    {
        if (_currentController != null)
        {
            bool isPlaying = _currentController.TogglePlayPause();
            Debug.Log($"切换成功，当前状态: {(isPlaying ? "播放中" : "已暂停")}");
        }
        else
        {
            Debug.Log("没有音频控制器");
        }
    }

    [Button("显示当前状态")]
    /// <summary>
    /// 显示当前音频状态
    /// </summary>
    public void ShowCurrentStatus()
    {
        if (_currentController != null)
        {
            Debug.Log($"音频状态: {_currentController.StatusDescription}");
            Debug.Log($"播放进度: {_currentController.Progress:P}");
            Debug.Log($"剩余时间: {_currentController.RemainingTime:F1}秒");
            Debug.Log($"IsPlaying: {_currentController.IsPlaying}");
            Debug.Log($"IsPaused: {_currentController.IsPaused}");
            Debug.Log($"IsActive: {_currentController.IsActive}");
            Debug.Log($"IsCompleted: {_currentController.IsCompleted}");
            Debug.Log($"IsCancelled: {_currentController.IsCancelled}");
            Debug.Log($"GameObject: {_currentController.gameObject.name}");
            Debug.Log($"IsInUse: {_currentController.IsInUse}");
        }
        else
        {
            Debug.Log("没有音频控制器");
        }
    }

    [Button("显示对象池统计")]
    /// <summary>
    /// 显示音频控制器对象池统计信息
    /// </summary>
    public void ShowPoolStats()
    {
        string stats = AudioManagerExtensions.GetAudioControllerPoolStats();
        Debug.Log($"对象池统计信息:\n{stats}");
    }

    [Button("预热对象池")]
    /// <summary>
    /// 预热音频控制器对象池
    /// </summary>
    public void WarmupPool()
    {
        AudioManagerExtensions.WarmupAudioControllerPool(5);
        Debug.Log("对象池预热完成");
    }

    [Button("清空对象池")]
    /// <summary>
    /// 清空音频控制器对象池
    /// </summary>
    public void ClearPool()
    {
        AudioManagerExtensions.ClearAudioControllerPool();
        Debug.Log("对象池已清空");
    }

    [Button("手动回收控制器")]
    /// <summary>
    /// 手动将当前控制器回收到对象池
    /// </summary>
    public void ManualReturnController()
    {
        if (_currentController != null)
        {
            _currentController.ReturnToPool();
            _currentController = null;
            Debug.Log("手动回收控制器到对象池");
        }
        else
        {
            Debug.Log("没有控制器可回收");
        }
    }

    /// <summary>
    /// 取消所有音效播放
    /// </summary>
    public void CancelAllSounds()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 连续播放多个音效的示例
    /// </summary>
    public async UniTaskVoid PlaySequentialSounds()
    {
        try
        {
            Debug.Log("开始连续播放音效");

            // 播放第一个音效并等待完成
            await AudioManagerExtensions.PlaySoundAsync(
                CatSounds.alerte, 
                transform,
                onComplete: () => Debug.Log("第一个音效播放完成"),
                cancellationToken: _cancellationTokenSource.Token
            );

            // 等待1秒
            await UniTask.Delay(1000, cancellationToken: _cancellationTokenSource.Token);

            // 播放第二个音效
            await AudioManagerExtensions.PlaySoundAsync(
                CatSounds.alerte, 
                transform,
                onComplete: () => Debug.Log("第二个音效播放完成"),
                cancellationToken: _cancellationTokenSource.Token
            );

            Debug.Log("所有音效播放完成");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("连续播放被取消");
        }
    }
}

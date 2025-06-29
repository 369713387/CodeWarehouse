using UnityEngine;
using System;

namespace Framework.Instance
{
    /// <summary>
    /// 普通 C# 类单例模式使用示例
    /// </summary>
    public class GameManager : Singleton<GameManager>, IDisposable
    {
        private int _score = 0;
        private bool _disposed = false;

        public int Score
        {
            get => _score;
            set => _score = value;
        }

        protected override void Initialize()
        {
            Debug.Log("GameManager initialized");
        }

        protected override void Cleanup()
        {
            Debug.Log("GameManager cleanup");
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // 清理资源
                _score = 0;
                _disposed = true;
                Debug.Log("GameManager disposed");
            }
        }

        public void AddScore(int points)
        {
            _score += points;
            Debug.Log($"Score updated: {_score}");
        }
    }

    /// <summary>
    /// Unity MonoBehaviour 单例模式使用示例
    /// </summary>
    public class AudioManager : MonoSingleton<AudioManager>
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _audioClips;

        protected override void Initialize()
        {
            Debug.Log("AudioManager initialized");
            
            // 确保有 AudioSource 组件
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        protected override void Cleanup()
        {
            Debug.Log("AudioManager cleanup");
            
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        public void PlaySound(int clipIndex)
        {
            if (_audioSource != null && _audioClips != null && clipIndex < _audioClips.Length)
            {
                _audioSource.clip = _audioClips[clipIndex];
                _audioSource.Play();
            }
        }

        public void StopSound()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }
    }

    /// <summary>
    /// 配置管理器单例示例
    /// </summary>
    public class ConfigManager : Singleton<ConfigManager>
    {
        private string _configPath = "config.json";
        
        public string PlayerName { get; set; } = "Player";
        public float MasterVolume { get; set; } = 1.0f;
        public bool IsFullScreen { get; set; } = false;

        protected override void Initialize()
        {
            Debug.Log("ConfigManager initialized");
            LoadConfig();
        }

        protected override void Cleanup()
        {
            Debug.Log("ConfigManager cleanup");
            SaveConfig();
        }

        private void LoadConfig()
        {
            // 模拟配置加载
            Debug.Log("Loading configuration...");
        }

        private void SaveConfig()
        {
            // 模拟配置保存
            Debug.Log("Saving configuration...");
        }

        public void UpdateConfig(string playerName, float volume, bool fullScreen)
        {
            PlayerName = playerName;
            MasterVolume = volume;
            IsFullScreen = fullScreen;
            
            Debug.Log($"Config updated - Player: {PlayerName}, Volume: {MasterVolume}, FullScreen: {IsFullScreen}");
        }
    }
} 
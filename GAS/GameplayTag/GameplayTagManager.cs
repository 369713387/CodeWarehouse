using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CodeWarehouse.GAS
{
    /// <summary>
    /// GameplayTagManager 是用于管理所有 GameplayTag 的全局单例管理器
    /// 提供标签注册、验证、查询和持久化功能
    /// </summary>
    public class GameplayTagManager : MonoBehaviour
    {
        #region 单例

        private static GameplayTagManager _instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static GameplayTagManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameplayTagManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameplayTagManager");
                        _instance = go.AddComponent<GameplayTagManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 字段

        [Header("标签配置")]
        [SerializeField, Tooltip("预定义的标签列表")]
        private List<string> _predefinedTags = new List<string>();

        [SerializeField, Tooltip("是否启用严格模式（只允许预定义的标签）")]
        private bool _strictMode = false;

        [SerializeField, Tooltip("标签配置文件路径")]
        private string _configFilePath = "GameplayTags.json";

        // 运行时数据
        private readonly HashSet<GameplayTag> _registeredTags = new HashSet<GameplayTag>();
        private readonly Dictionary<string, GameplayTag> _nameToTagMap = new Dictionary<string, GameplayTag>();
        private readonly Dictionary<string, List<GameplayTag>> _categoryToTagsMap = new Dictionary<string, List<GameplayTag>>();

        #endregion

        #region 属性

        /// <summary>
        /// 是否启用严格模式
        /// </summary>
        public bool StrictMode 
        { 
            get => _strictMode; 
            set => _strictMode = value; 
        }

        /// <summary>
        /// 已注册的标签数量
        /// </summary>
        public int RegisteredTagCount => _registeredTags.Count;

        /// <summary>
        /// 获取所有已注册的标签
        /// </summary>
        public IReadOnlyCollection<GameplayTag> RegisteredTags => _registeredTags;

        /// <summary>
        /// 获取所有标签类别
        /// </summary>
        public IReadOnlyCollection<string> Categories => _categoryToTagsMap.Keys;

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SaveTagsToFile();
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void Initialize()
        {
            LoadTagsFromFile();
            RegisterPredefinedTags();
        }

        /// <summary>
        /// 注册预定义的标签
        /// </summary>
        private void RegisterPredefinedTags()
        {
            foreach (var tagName in _predefinedTags)
            {
                if (!string.IsNullOrEmpty(tagName))
                {
                    RegisterTag(tagName);
                }
            }
        }

        #endregion

        #region 标签注册

        /// <summary>
        /// 注册一个标签
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>注册的标签</returns>
        public GameplayTag RegisterTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                Debug.LogWarning("Cannot register empty or null tag name");
                return GameplayTag.Empty;
            }

            // 检查是否已经注册
            if (_nameToTagMap.TryGetValue(tagName, out var existingTag))
            {
                return existingTag;
            }

            var newTag = new GameplayTag(tagName);
            
            // 添加到注册表
            _registeredTags.Add(newTag);
            _nameToTagMap[tagName] = newTag;

            // 添加到分类映射
            var category = GetTagCategory(tagName);
            if (!_categoryToTagsMap.TryGetValue(category, out var categoryTags))
            {
                categoryTags = new List<GameplayTag>();
                _categoryToTagsMap[category] = categoryTags;
            }
            categoryTags.Add(newTag);

            Debug.Log($"Registered GameplayTag: {tagName}");
            return newTag;
        }

        /// <summary>
        /// 批量注册标签
        /// </summary>
        /// <param name="tagNames">标签名称数组</param>
        public void RegisterTags(params string[] tagNames)
        {
            if (tagNames != null)
            {
                foreach (var tagName in tagNames)
                {
                    RegisterTag(tagName);
                }
            }
        }

        /// <summary>
        /// 注销一个标签
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>是否成功注销</returns>
        public bool UnregisterTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            if (!_nameToTagMap.TryGetValue(tagName, out var tag))
                return false;

            _registeredTags.Remove(tag);
            _nameToTagMap.Remove(tagName);

            // 从分类映射中移除
            var category = GetTagCategory(tagName);
            if (_categoryToTagsMap.TryGetValue(category, out var categoryTags))
            {
                categoryTags.Remove(tag);
                if (categoryTags.Count == 0)
                {
                    _categoryToTagsMap.Remove(category);
                }
            }

            Debug.Log($"Unregistered GameplayTag: {tagName}");
            return true;
        }

        #endregion

        #region 标签查询

        /// <summary>
        /// 获取标签
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>如果找到返回标签，否则返回空标签</returns>
        public GameplayTag GetTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return GameplayTag.Empty;

            if (_nameToTagMap.TryGetValue(tagName, out var tag))
                return tag;

            // 如果不是严格模式，允许创建新标签
            if (!_strictMode)
            {
                return RegisterTag(tagName);
            }

            Debug.LogWarning($"Tag '{tagName}' not found and strict mode is enabled");
            return GameplayTag.Empty;
        }

        /// <summary>
        /// 尝试获取标签
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <param name="tag">输出标签</param>
        /// <returns>是否找到标签</returns>
        public bool TryGetTag(string tagName, out GameplayTag tag)
        {
            tag = GetTag(tagName);
            return !tag.IsEmpty;
        }

        /// <summary>
        /// 检查标签是否已注册
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>是否已注册</returns>
        public bool IsTagRegistered(string tagName)
        {
            return !string.IsNullOrEmpty(tagName) && _nameToTagMap.ContainsKey(tagName);
        }

        /// <summary>
        /// 检查标签是否已注册
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>是否已注册</returns>
        public bool IsTagRegistered(GameplayTag tag)
        {
            return !tag.IsEmpty && _registeredTags.Contains(tag);
        }

        /// <summary>
        /// 获取指定分类下的所有标签
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>分类下的标签集合</returns>
        public IEnumerable<GameplayTag> GetTagsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return Enumerable.Empty<GameplayTag>();

            if (_categoryToTagsMap.TryGetValue(category, out var tags))
                return tags.AsReadOnly();

            return Enumerable.Empty<GameplayTag>();
        }

        /// <summary>
        /// 获取标签的分类
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>分类名称</returns>
        private string GetTagCategory(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return "Unknown";

            var firstDotIndex = tagName.IndexOf('.');
            if (firstDotIndex > 0)
                return tagName.Substring(0, firstDotIndex);

            return tagName;
        }

        /// <summary>
        /// 搜索标签
        /// </summary>
        /// <param name="searchTerm">搜索词</param>
        /// <param name="exactMatch">是否精确匹配</param>
        /// <returns>匹配的标签集合</returns>
        public IEnumerable<GameplayTag> SearchTags(string searchTerm, bool exactMatch = false)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Enumerable.Empty<GameplayTag>();

            if (exactMatch)
            {
                return _registeredTags.Where(tag => 
                    string.Equals(tag.Name, searchTerm, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return _registeredTags.Where(tag => 
                    tag.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        /// <summary>
        /// 获取标签的所有子标签
        /// </summary>
        /// <param name="parentTag">父标签</param>
        /// <returns>子标签集合</returns>
        public IEnumerable<GameplayTag> GetChildTags(GameplayTag parentTag)
        {
            if (parentTag.IsEmpty)
                return Enumerable.Empty<GameplayTag>();

            return _registeredTags.Where(tag => 
                tag != parentTag && tag.HasTag(parentTag));
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证标签名称是否有效
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>验证结果</returns>
        public TagValidationResult ValidateTagName(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return new TagValidationResult(false, "Tag name cannot be null or empty");

            if (tagName.Trim() != tagName)
                return new TagValidationResult(false, "Tag name cannot have leading or trailing whitespace");

            if (tagName.Contains(".."))
                return new TagValidationResult(false, "Tag name cannot contain consecutive dots");

            if (tagName.StartsWith(".") || tagName.EndsWith("."))
                return new TagValidationResult(false, "Tag name cannot start or end with a dot");

            // 检查字符有效性
            foreach (char c in tagName)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
                {
                    return new TagValidationResult(false, $"Tag name contains invalid character: '{c}'");
                }
            }

            if (_strictMode && !IsTagRegistered(tagName))
                return new TagValidationResult(false, "Tag is not registered and strict mode is enabled");

            return new TagValidationResult(true, "Tag name is valid");
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 保存标签到文件
        /// </summary>
        public void SaveTagsToFile()
        {
            try
            {
                var tagData = new TagConfigData
                {
                    tags = _registeredTags.Select(t => t.Name).ToArray(),
                    strictMode = _strictMode
                };

                var json = JsonUtility.ToJson(tagData, true);
                var filePath = Path.Combine(Application.persistentDataPath, _configFilePath);
                File.WriteAllText(filePath, json);

                Debug.Log($"Saved {_registeredTags.Count} tags to {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save tags to file: {e.Message}");
            }
        }

        /// <summary>
        /// 从文件加载标签
        /// </summary>
        public void LoadTagsFromFile()
        {
            try
            {
                var filePath = Path.Combine(Application.persistentDataPath, _configFilePath);
                if (!File.Exists(filePath))
                    return;

                var json = File.ReadAllText(filePath);
                var tagData = JsonUtility.FromJson<TagConfigData>(json);

                if (tagData?.tags != null)
                {
                    foreach (var tagName in tagData.tags)
                    {
                        RegisterTag(tagName);
                    }
                }

                _strictMode = tagData?.strictMode ?? false;

                Debug.Log($"Loaded {tagData?.tags?.Length ?? 0} tags from {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load tags from file: {e.Message}");
            }
        }

        #endregion

        #region 编辑器支持

#if UNITY_EDITOR
        /// <summary>
        /// 获取所有标签名称（编辑器用）
        /// </summary>
        public string[] GetAllTagNames()
        {
            return _nameToTagMap.Keys.ToArray();
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        [ContextMenu("Reload Configuration")]
        public void ReloadConfiguration()
        {
            _registeredTags.Clear();
            _nameToTagMap.Clear();
            _categoryToTagsMap.Clear();
            Initialize();
        }

        /// <summary>
        /// 清除所有注册的标签
        /// </summary>
        [ContextMenu("Clear All Tags")]
        public void ClearAllTags()
        {
            _registeredTags.Clear();
            _nameToTagMap.Clear();
            _categoryToTagsMap.Clear();
        }
#endif

        #endregion
    }

    /// <summary>
    /// 标签验证结果
    /// </summary>
    [Serializable]
    public struct TagValidationResult
    {
        public bool IsValid;
        public string Message;

        public TagValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }
    }

    /// <summary>
    /// 标签配置数据
    /// </summary>
    [Serializable]
    internal class TagConfigData
    {
        public string[] tags;
        public bool strictMode;
    }
}

using System;
using System.Linq;

namespace CodeWarehouse.GAS
{
    /// <summary>
    /// GameplayTag 是一个带有层级关系的标签系统，使用点号分隔的字符串表示层级
    /// 例如：Character.State.Debuff.Poison
    /// 设计为高性能的值类型，避免GC压力，所有比较操作都基于预计算的哈希码
    /// </summary>
    [Serializable]
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        #region 字段

        private readonly string _name;                    // 完整的标签字符串
        private readonly int _hashCode;                   // 预计算的哈希码，用于快速比较
        private readonly string _shortName;               // 标签的最后一部分（简称）
        private readonly string[] _ancestorNames;         // 所有祖先标签的字符串数组
        private readonly int[] _ancestorHashCodes;        // 所有祖先标签的哈希码数组

        #endregion

        #region 属性

        /// <summary>
        /// 完整的标签名称
        /// </summary>
        public string Name => _name ?? string.Empty;

        /// <summary>
        /// 预计算的哈希码，用于高效比较
        /// </summary>
        public int HashCode => _hashCode;

        /// <summary>
        /// 标签的简称（最后一个部分）
        /// </summary>
        public string ShortName => _shortName ?? string.Empty;

        /// <summary>
        /// 所有祖先标签的名称数组
        /// </summary>
        public string[] AncestorNames => _ancestorNames ?? Array.Empty<string>();

        /// <summary>
        /// 是否为空标签
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(_name);

        /// <summary>
        /// 标签的层级深度
        /// </summary>
        public int Depth => _ancestorNames?.Length + 1 ?? 0;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 - 预计算所有关键数据以优化运行时性能
        /// </summary>
        /// <param name="name">标签名称，使用点号分隔层级</param>
        public GameplayTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _name = string.Empty;
                _hashCode = 0;
                _shortName = string.Empty;
                _ancestorNames = Array.Empty<string>();
                _ancestorHashCodes = Array.Empty<int>();
                return;
            }

            // 保存完整名称并计算哈希码
            _name = name;
            _hashCode = name.GetHashCode();

            // 解析层级结构
            var tags = name.Split('.');

            // 保存简称（最后一个部分）
            _shortName = tags[^1]; // C# 8.0 语法，获取最后一个元素

            // 构建祖先信息
            if (tags.Length > 1)
            {
                _ancestorNames = new string[tags.Length - 1];
                _ancestorHashCodes = new int[tags.Length - 1];

                var ancestorTag = string.Empty;
                for (int i = 0; i < tags.Length - 1; i++)
                {
                    // 构建祖先标签路径
                    if (i == 0)
                        ancestorTag = tags[i];
                    else
                        ancestorTag += "." + tags[i];

                    // 存储祖先名称和哈希码
                    _ancestorNames[i] = ancestorTag;
                    _ancestorHashCodes[i] = ancestorTag.GetHashCode();
                }
            }
            else
            {
                _ancestorNames = Array.Empty<string>();
                _ancestorHashCodes = Array.Empty<int>();
            }
        }

        #endregion

        #region 比较操作

        /// <summary>
        /// 实现 IEquatable&lt;GameplayTag&gt; 接口，避免装箱操作
        /// </summary>
        public bool Equals(GameplayTag other) => this == other;

        /// <summary>
        /// 重写 Equals 方法
        /// </summary>
        public override bool Equals(object obj) => obj is GameplayTag tag && this == tag;

        /// <summary>
        /// 重写 GetHashCode 方法，返回预计算的哈希码
        /// </summary>
        public override int GetHashCode() => HashCode;

        /// <summary>
        /// 相等运算符重载 - 基于哈希码的快速比较
        /// </summary>
        public static bool operator ==(GameplayTag x, GameplayTag y)
        {
            if (x.HashCode != y.HashCode)
            {
                return false;
            }
            return string.Equals(x.Name, y.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// 不等运算符重载
        /// </summary>
        public static bool operator !=(GameplayTag x, GameplayTag y) => x.HashCode != y.HashCode;

        #endregion

        #region 层级查询

        /// <summary>
        /// 检查当前标签是否拥有指定的标签
        /// 即指定的标签是否是当前标签本身，或者是它的祖先之一
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果拥有指定标签则返回true</returns>
        public readonly bool HasTag(in GameplayTag tag)
        {
            // 如果查询的是空标签，总是返回false
            if (tag.IsEmpty)
                return false;

            // 检查是否是祖先
            if (_ancestorHashCodes != null)
            {
                foreach (var ancestorHashCode in _ancestorHashCodes)
                {
                    if (ancestorHashCode == tag.HashCode)
                        return true;
                }
            }

            // 检查是否是自身
            return this == tag;
        }

        /// <summary>
        /// 检查当前标签是否是指定标签的子标签
        /// 这是 HasTag 的语法糖形式，让代码更易读
        /// </summary>
        /// <param name="other">父标签</param>
        /// <returns>如果是子标签则返回true</returns>
        public readonly bool IsDescendantOf(in GameplayTag other) => other.HasTag(this);

        /// <summary>
        /// 检查当前标签是否拥有指定名称的标签
        /// </summary>
        /// <param name="tagName">要检查的标签名称</param>
        /// <returns>如果拥有指定标签则返回true</returns>
        public readonly bool HasTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            return HasTag(new GameplayTag(tagName));
        }

        /// <summary>
        /// 获取父标签（如果存在）
        /// </summary>
        /// <returns>父标签，如果不存在则返回空标签</returns>
        public readonly GameplayTag GetParent()
        {
            if (_ancestorNames == null || _ancestorNames.Length == 0)
                return default; // 返回空标签

            return new GameplayTag(_ancestorNames[^1]); // 最后一个祖先就是直接父标签
        }

        /// <summary>
        /// 获取指定层级的祖先标签
        /// </summary>
        /// <param name="level">层级（0表示根标签）</param>
        /// <returns>指定层级的祖先标签，如果不存在则返回空标签</returns>
        public readonly GameplayTag GetAncestorAtLevel(int level)
        {
            if (_ancestorNames == null || level < 0 || level >= _ancestorNames.Length)
                return default;

            return new GameplayTag(_ancestorNames[level]);
        }

        #endregion

        #region 字符串操作

        /// <summary>
        /// 重写 ToString 方法
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// 隐式转换为字符串
        /// </summary>
        public static implicit operator string(GameplayTag tag) => tag.Name;

        /// <summary>
        /// 隐式转换从字符串
        /// </summary>
        public static implicit operator GameplayTag(string name) => new GameplayTag(name);

        #endregion

        #region 常量

        /// <summary>
        /// 空标签常量
        /// </summary>
        public static readonly GameplayTag Empty = new GameplayTag(string.Empty);

        /// <summary>
        /// 标签分隔符
        /// </summary>
        public const char Separator = '.';

        #endregion
    }
}

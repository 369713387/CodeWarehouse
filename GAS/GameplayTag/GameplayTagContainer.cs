using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeWarehouse.GAS
{
    /// <summary>
    /// GameplayTagContainer 是用于管理多个 GameplayTag 的高性能容器
    /// 内部使用 HashSet 来避免重复标签并提供 O(1) 的查找性能
    /// </summary>
    [Serializable]
    public class GameplayTagContainer : IEnumerable<GameplayTag>, IEquatable<GameplayTagContainer>
    {
        #region 字段

        private readonly HashSet<GameplayTag> _tags;

        #endregion

        #region 属性

        /// <summary>
        /// 容器中标签的数量
        /// </summary>
        public int Count => _tags?.Count ?? 0;

        /// <summary>
        /// 容器是否为空
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// 获取容器中所有标签的只读集合
        /// </summary>
        public IReadOnlyCollection<GameplayTag> Tags => _tags ?? new HashSet<GameplayTag>();

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public GameplayTagContainer()
        {
            _tags = new HashSet<GameplayTag>();
        }

        /// <summary>
        /// 使用标签数组构造
        /// </summary>
        /// <param name="tags">初始标签数组</param>
        public GameplayTagContainer(params GameplayTag[] tags) : this()
        {
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (!tag.IsEmpty)
                        _tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// 使用标签集合构造
        /// </summary>
        /// <param name="tags">初始标签集合</param>
        public GameplayTagContainer(IEnumerable<GameplayTag> tags) : this()
        {
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (!tag.IsEmpty)
                        _tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// 使用字符串数组构造
        /// </summary>
        /// <param name="tagNames">标签名称数组</param>
        public GameplayTagContainer(params string[] tagNames) : this()
        {
            if (tagNames != null)
            {
                foreach (var tagName in tagNames)
                {
                    if (!string.IsNullOrEmpty(tagName))
                        _tags.Add(new GameplayTag(tagName));
                }
            }
        }

        #endregion

        #region 标签管理

        /// <summary>
        /// 添加标签到容器
        /// </summary>
        /// <param name="tag">要添加的标签</param>
        /// <returns>如果成功添加（之前不存在）返回true</returns>
        public bool AddTag(GameplayTag tag)
        {
            if (tag.IsEmpty)
                return false;

            return _tags.Add(tag);
        }

        /// <summary>
        /// 添加标签到容器
        /// </summary>
        /// <param name="tagName">要添加的标签名称</param>
        /// <returns>如果成功添加（之前不存在）返回true</returns>
        public bool AddTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            return AddTag(new GameplayTag(tagName));
        }

        /// <summary>
        /// 添加多个标签到容器
        /// </summary>
        /// <param name="tags">要添加的标签数组</param>
        public void AddTags(params GameplayTag[] tags)
        {
            if (tags != null)
            {
                foreach (var tag in tags)
                    AddTag(tag);
            }
        }

        /// <summary>
        /// 添加多个标签到容器
        /// </summary>
        /// <param name="tags">要添加的标签集合</param>
        public void AddTags(IEnumerable<GameplayTag> tags)
        {
            if (tags != null)
            {
                foreach (var tag in tags)
                    AddTag(tag);
            }
        }

        /// <summary>
        /// 从容器中移除标签
        /// </summary>
        /// <param name="tag">要移除的标签</param>
        /// <returns>如果成功移除（之前存在）返回true</returns>
        public bool RemoveTag(GameplayTag tag)
        {
            return _tags.Remove(tag);
        }

        /// <summary>
        /// 从容器中移除标签
        /// </summary>
        /// <param name="tagName">要移除的标签名称</param>
        /// <returns>如果成功移除（之前存在）返回true</returns>
        public bool RemoveTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            return RemoveTag(new GameplayTag(tagName));
        }

        /// <summary>
        /// 从容器中移除多个标签
        /// </summary>
        /// <param name="tags">要移除的标签数组</param>
        public void RemoveTags(params GameplayTag[] tags)
        {
            if (tags != null)
            {
                foreach (var tag in tags)
                    RemoveTag(tag);
            }
        }

        /// <summary>
        /// 清空容器中的所有标签
        /// </summary>
        public void Clear()
        {
            _tags.Clear();
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 检查容器是否包含指定标签（精确匹配）
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果包含返回true</returns>
        public bool HasTag(GameplayTag tag)
        {
            return _tags.Contains(tag);
        }

        /// <summary>
        /// 检查容器是否包含指定标签（精确匹配）
        /// </summary>
        /// <param name="tagName">要检查的标签名称</param>
        /// <returns>如果包含返回true</returns>
        public bool HasTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            return HasTag(new GameplayTag(tagName));
        }

        /// <summary>
        /// 检查容器是否包含任何指定的标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果包含任何一个返回true</returns>
        public bool HasAnyTag(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;

            return tags.Any(tag => HasTag(tag));
        }

        /// <summary>
        /// 检查容器是否包含所有指定的标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果包含所有标签返回true</returns>
        public bool HasAllTags(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
                return true;

            return tags.All(tag => HasTag(tag));
        }

        /// <summary>
        /// 检查容器是否拥有指定标签（支持层级匹配）
        /// 例如，容器中有"Character.State.Debuff.Poison"，查询"Character.State.Debuff"会返回true
        /// </summary>
        /// <param name="tag">要检查的标签</param>
        /// <returns>如果拥有返回true</returns>
        public bool HasTagExact(GameplayTag tag)
        {
            if (tag.IsEmpty)
                return false;

            // 检查是否有任何标签拥有这个标签
            return _tags.Any(ownedTag => ownedTag.HasTag(tag));
        }

        /// <summary>
        /// 检查容器是否拥有指定标签（支持层级匹配）
        /// </summary>
        /// <param name="tagName">要检查的标签名称</param>
        /// <returns>如果拥有返回true</returns>
        public bool HasTagExact(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return false;

            return HasTagExact(new GameplayTag(tagName));
        }

        /// <summary>
        /// 获取所有匹配指定父标签的子标签
        /// </summary>
        /// <param name="parentTag">父标签</param>
        /// <returns>匹配的子标签集合</returns>
        public IEnumerable<GameplayTag> GetTagsWithParent(GameplayTag parentTag)
        {
            if (parentTag.IsEmpty)
                return Enumerable.Empty<GameplayTag>();

            return _tags.Where(tag => tag.HasTag(parentTag));
        }

        /// <summary>
        /// 获取所有以指定前缀开始的标签
        /// </summary>
        /// <param name="prefix">前缀</param>
        /// <returns>匹配的标签集合</returns>
        public IEnumerable<GameplayTag> GetTagsWithPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return Enumerable.Empty<GameplayTag>();

            return _tags.Where(tag => tag.Name.StartsWith(prefix));
        }

        #endregion

        #region 集合操作

        /// <summary>
        /// 合并另一个标签容器
        /// </summary>
        /// <param name="other">要合并的容器</param>
        public void Union(GameplayTagContainer other)
        {
            if (other != null && other._tags != null)
            {
                foreach (var tag in other._tags)
                    _tags.Add(tag);
            }
        }

        /// <summary>
        /// 移除另一个标签容器中包含的所有标签
        /// </summary>
        /// <param name="other">要移除的容器</param>
        public void Except(GameplayTagContainer other)
        {
            if (other != null && other._tags != null)
            {
                foreach (var tag in other._tags)
                    _tags.Remove(tag);
            }
        }

        /// <summary>
        /// 保留与另一个标签容器的交集
        /// </summary>
        /// <param name="other">要求交集的容器</param>
        public void Intersect(GameplayTagContainer other)
        {
            if (other == null || other._tags == null)
            {
                Clear();
                return;
            }

            _tags.IntersectWith(other._tags);
        }

        #endregion

        #region 比较操作

        /// <summary>
        /// 检查两个容器是否相等
        /// </summary>
        public bool Equals(GameplayTagContainer other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (Count != other.Count)
                return false;

            return _tags.SetEquals(other._tags);
        }

        /// <summary>
        /// 重写 Equals 方法
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as GameplayTagContainer);
        }

        /// <summary>
        /// 重写 GetHashCode 方法
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var tag in _tags)
            {
                hash ^= tag.GetHashCode();
            }
            return hash;
        }

        #endregion

        #region IEnumerable 实现

        /// <summary>
        /// 获取枚举器
        /// </summary>
        public IEnumerator<GameplayTag> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        /// <summary>
        /// 获取枚举器（非泛型）
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region 运算符重载

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(GameplayTagContainer left, GameplayTagContainer right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(GameplayTagContainer left, GameplayTagContainer right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 合并运算符
        /// </summary>
        public static GameplayTagContainer operator +(GameplayTagContainer left, GameplayTagContainer right)
        {
            var result = new GameplayTagContainer();
            if (left != null)
                result.Union(left);
            if (right != null)
                result.Union(right);
            return result;
        }

        /// <summary>
        /// 差集运算符
        /// </summary>
        public static GameplayTagContainer operator -(GameplayTagContainer left, GameplayTagContainer right)
        {
            if (left == null)
                return new GameplayTagContainer();

            var result = new GameplayTagContainer(left._tags);
            if (right != null)
                result.Except(right);
            return result;
        }

        #endregion

        #region 字符串表示

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        public override string ToString()
        {
            if (IsEmpty)
                return "Empty GameplayTagContainer";

            return $"GameplayTagContainer({Count} tags): [{string.Join(", ", _tags.Select(t => t.Name))}]";
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 创建空容器
        /// </summary>
        public static GameplayTagContainer Empty => new GameplayTagContainer();

        /// <summary>
        /// 从字符串数组创建容器
        /// </summary>
        public static GameplayTagContainer FromStrings(params string[] tagNames)
        {
            return new GameplayTagContainer(tagNames);
        }

        /// <summary>
        /// 从标签数组创建容器
        /// </summary>
        public static GameplayTagContainer FromTags(params GameplayTag[] tags)
        {
            return new GameplayTagContainer(tags);
        }

        #endregion
    }
}

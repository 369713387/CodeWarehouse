using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Framework.ZeroGCEventBus
{
    /// <summary>
    /// 引用类型扩展方法，简化ID化操作
    /// </summary>
    public static class ReferenceTypeExtensions
    {
        #region GameObject扩展

        /// <summary>
        /// 获取GameObject的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToReferenceId(this GameObject gameObject)
        {
            return ReferenceTypeManager.GetOrCreateId(gameObject);
        }

        /// <summary>
        /// 通过ID获取GameObject
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject FromReferenceId(int id)
        {
            return ReferenceTypeManager.GetReference<GameObject>(id);
        }

        #endregion

        #region Component扩展

        /// <summary>
        /// 获取Component的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToReferenceId<T>(this T component) where T : Component
        {
            return ReferenceTypeManager.GetOrCreateId(component);
        }

        /// <summary>
        /// 通过ID获取Component
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponent<T>(int id) where T : Component
        {
            return ReferenceTypeManager.GetReference<T>(id);
        }

        #endregion

        #region Transform扩展

        /// <summary>
        /// 获取Transform的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToReferenceId(this Transform transform)
        {
            return ReferenceTypeManager.GetOrCreateId(transform);
        }

        /// <summary>
        /// 通过ID获取Transform
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform GetTransform(int id)
        {
            return ReferenceTypeManager.GetReference<Transform>(id);
        }

        #endregion

        #region UI组件特化扩展

        /// <summary>
        /// 获取RectTransform的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToUIReferenceId(this RectTransform rectTransform)
        {
            return ReferenceTypeManager.GetOrCreateId(rectTransform);
        }

        /// <summary>
        /// 获取Canvas的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToUIReferenceId(this Canvas canvas)
        {
            return ReferenceTypeManager.GetOrCreateId(canvas);
        }

        /// <summary>
        /// 获取CanvasGroup的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToUIReferenceId(this CanvasGroup canvasGroup)
        {
            return ReferenceTypeManager.GetOrCreateId(canvasGroup);
        }

        /// <summary>
        /// 通过ID获取UI组件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetUIComponent<T>(int id) where T : Component
        {
            return ReferenceTypeManager.GetReference<T>(id);
        }

        #endregion

        #region 物理组件扩展
        /// <summary>
        /// 获取Collider的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToPhysicsReferenceId(this Collision collision)
        {
            return ReferenceTypeManager.GetOrCreateId(collision);
        }
        
        
        /// <summary>
        /// 获取Collider的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToPhysicsReferenceId(this Collider collider)
        {
            return ReferenceTypeManager.GetOrCreateId(collider);
        }

        /// <summary>
        /// 获取Collider2D的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToPhysicsReferenceId(this Collider2D collider2D)
        {
            return ReferenceTypeManager.GetOrCreateId(collider2D);
        }

        /// <summary>
        /// 获取Rigidbody的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToPhysicsReferenceId(this Rigidbody rigidbody)
        {
            return ReferenceTypeManager.GetOrCreateId(rigidbody);
        }

        /// <summary>
        /// 获取Rigidbody2D的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToPhysicsReferenceId(this Rigidbody2D rigidbody2D)
        {
            return ReferenceTypeManager.GetOrCreateId(rigidbody2D);
        }

        /// <summary>
        /// 通过ID获取物理组件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetPhysicsComponent<T>(int id) where T : Component
        {
            return ReferenceTypeManager.GetReference<T>(id);
        }

        #endregion

        #region Unity资源类型扩展

        /// <summary>
        /// 获取Material的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Material material)
        {
            return ReferenceTypeManager.GetOrCreateId(material);
        }

        /// <summary>
        /// 获取Texture的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Texture texture)
        {
            return ReferenceTypeManager.GetOrCreateId(texture);
        }

        /// <summary>
        /// 获取Texture2D的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Texture2D texture2D)
        {
            return ReferenceTypeManager.GetOrCreateId(texture2D);
        }

        /// <summary>
        /// 获取Sprite的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Sprite sprite)
        {
            return ReferenceTypeManager.GetOrCreateId(sprite);
        }

        /// <summary>
        /// 获取Mesh的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Mesh mesh)
        {
            return ReferenceTypeManager.GetOrCreateId(mesh);
        }

        /// <summary>
        /// 获取AnimationClip的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this AnimationClip clip)
        {
            return ReferenceTypeManager.GetOrCreateId(clip);
        }

        /// <summary>
        /// 获取RuntimeAnimatorController的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this RuntimeAnimatorController controller)
        {
            return ReferenceTypeManager.GetOrCreateId(controller);
        }

        /// <summary>
        /// 获取AudioClip的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this AudioClip audioClip)
        {
            return ReferenceTypeManager.GetOrCreateId(audioClip);
        }

        /// <summary>
        /// 获取Shader的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Shader shader)
        {
            return ReferenceTypeManager.GetOrCreateId(shader);
        }

        /// <summary>
        /// 获取Font的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToAssetReferenceId(this Font font)
        {
            return ReferenceTypeManager.GetOrCreateId(font);
        }

        /// <summary>
        /// 通过ID获取资源对象
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetAsset<T>(int id) where T : UnityEngine.Object
        {
            return ReferenceTypeManager.GetReference<T>(id);
        }

        #endregion

        #region ScriptableObject扩展

        /// <summary>
        /// 获取ScriptableObject的ID
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToScriptableObjectId<T>(this T scriptableObject) where T : ScriptableObject
        {
            return ReferenceTypeManager.GetOrCreateId(scriptableObject);
        }

        /// <summary>
        /// 通过ID获取ScriptableObject
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetScriptableObject<T>(int id) where T : ScriptableObject
        {
            return ReferenceTypeManager.GetReference<T>(id);
        }

        #endregion

        #region 通用引用类型扩展

        /// <summary>
        /// 获取任意引用类型的ID（通过静态方法避免扩展方法冲突）
        /// 注意：对于Unity对象，推荐使用具体的扩展方法或GetUnityObjectReferenceId
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetReferenceId<T>(T obj) where T : class
        {
            // 对于Unity对象类型，使用专门的方法处理
            if (obj is UnityEngine.Object unityObj)
            {
                return GetUnityObjectReferenceId(unityObj);
            }
            return ReferenceTypeManager.GetOrCreateId(obj);
        }

        /// <summary>
        /// 获取Unity Object类型的ID（推荐用于所有Unity对象）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUnityObjectReferenceId(UnityEngine.Object obj)
        {
            return ReferenceTypeManager.GetOrCreateId(obj);
        }

        /// <summary>
        /// 获取非Unity对象的引用ID（用于纯C#对象）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetObjectReferenceId<T>(T obj) where T : class
        {
            if (obj is UnityEngine.Object)
            {
                throw new System.ArgumentException("Use GetUnityObjectReferenceId for Unity objects");
            }
            return ReferenceTypeManager.GetOrCreateId(obj);
        }

        /// <summary>
        /// 批量获取引用类型ID
        /// </summary>
        public static int[] ToReferenceIds<T>(this T[] objects) where T : class
        {
            if (objects == null) return new int[0];
            
            int[] ids = new int[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                ids[i] = ReferenceTypeManager.GetOrCreateId(objects[i]);
            }
            return ids;
        }

        /// <summary>
        /// 批量获取引用类型ID（List版本）
        /// </summary>
        public static List<int> ToReferenceIds<T>(this List<T> objects) where T : class
        {
            if (objects == null) return new List<int>();
            
            var ids = new List<int>(objects.Count);
            for (int i = 0; i < objects.Count; i++)
            {
                ids.Add(ReferenceTypeManager.GetOrCreateId(objects[i]));
            }
            return ids;
        }

        #endregion
    }

    /// <summary>
    /// 引用ID容器 - 用于在事件中存储多个引用
    /// </summary>
    [System.Serializable]
    public struct ReferenceIdContainer
    {
        public int PrimaryId;
        public int SecondaryId;
        public int TertiaryId;
        
        public ReferenceIdContainer(int primary, int secondary = 0, int tertiary = 0)
        {
            PrimaryId = primary;
            SecondaryId = secondary;
            TertiaryId = tertiary;
        }
        
        public T GetPrimary<T>() where T : class => ReferenceTypeManager.GetReference<T>(PrimaryId);
        public T GetSecondary<T>() where T : class => ReferenceTypeManager.GetReference<T>(SecondaryId);
        public T GetTertiary<T>() where T : class => ReferenceTypeManager.GetReference<T>(TertiaryId);
        
        public bool HasPrimary => PrimaryId != 0 && ReferenceTypeManager.IsValid(PrimaryId);
        public bool HasSecondary => SecondaryId != 0 && ReferenceTypeManager.IsValid(SecondaryId);
        public bool HasTertiary => TertiaryId != 0 && ReferenceTypeManager.IsValid(TertiaryId);
    }

    /// <summary>
    /// 引用ID数组容器 - 用于存储动态数量的引用
    /// </summary>
    [System.Serializable]
    public struct ReferenceIdArray
    {
        public int[] Ids;
        
        public ReferenceIdArray(params int[] ids)
        {
            Ids = ids ?? new int[0];
        }
        
        public ReferenceIdArray(params object[] objects)
        {
            if (objects == null)
            {
                Ids = new int[0];
                return;
            }
            
            Ids = new int[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                Ids[i] = ReferenceTypeManager.GetOrCreateId(objects[i]);
            }
        }
        
        public T[] GetReferences<T>() where T : class
        {
            if (Ids == null) return new T[0];
            
            T[] results = new T[Ids.Length];
            ReferenceTypeManager.GetReferences(Ids, results);
            return results;
        }
        
        public List<T> GetReferencesList<T>() where T : class
        {
            var references = GetReferences<T>();
            var list = new List<T>();
            
            foreach (var reference in references)
            {
                if (reference != null)
                    list.Add(reference);
            }
            
            return list;
        }
        
        public int Count => Ids?.Length ?? 0;
        
        public bool IsEmpty => Count == 0;
        
        public int GetValidCount()
        {
            if (Ids == null) return 0;
            
            int validCount = 0;
            foreach (int id in Ids)
            {
                if (ReferenceTypeManager.IsValid(id))
                    validCount++;
            }
            return validCount;
        }
    }

    /// <summary>
    /// 引用ID字典容器 - 用于存储键值对形式的引用
    /// </summary>
    [System.Serializable]
    public struct ReferenceIdDictionary
    {
        [System.Serializable]
        public struct KeyValuePair
        {
            public string Key;
            public int ReferenceId;
            
            public KeyValuePair(string key, int referenceId)
            {
                Key = key;
                ReferenceId = referenceId;
            }
        }
        
        public KeyValuePair[] Pairs;
        
        public ReferenceIdDictionary(Dictionary<string, object> dictionary)
        {
            if (dictionary == null)
            {
                Pairs = new KeyValuePair[0];
                return;
            }
            
            Pairs = new KeyValuePair[dictionary.Count];
            int index = 0;
            foreach (var kvp in dictionary)
            {
                int id = kvp.Value != null ? ReferenceTypeManager.GetOrCreateId(kvp.Value) : 0;
                Pairs[index++] = new KeyValuePair(kvp.Key, id);
            }
        }
        
        public T GetReference<T>(string key) where T : class
        {
            if (Pairs == null) return null;
            
            foreach (var pair in Pairs)
            {
                if (pair.Key == key)
                {
                    return ReferenceTypeManager.GetReference<T>(pair.ReferenceId);
                }
            }
            return null;
        }
        
        public Dictionary<string, T> GetReferenceDictionary<T>() where T : class
        {
            var result = new Dictionary<string, T>();
            if (Pairs == null) return result;
            
            foreach (var pair in Pairs)
            {
                var reference = ReferenceTypeManager.GetReference<T>(pair.ReferenceId);
                if (reference != null)
                {
                    result[pair.Key] = reference;
                }
            }
            return result;
        }
        
        public bool ContainsKey(string key)
        {
            if (Pairs == null) return false;
            
            foreach (var pair in Pairs)
            {
                if (pair.Key == key)
                    return true;
            }
            return false;
        }
        
        public int Count => Pairs?.Length ?? 0;
        public bool IsEmpty => Count == 0;
    }

    /// <summary>
    /// 层级结构引用容器 - 用于存储父子关系的引用
    /// </summary>
    [System.Serializable]
    public struct HierarchyReferenceContainer
    {
        public int ParentId;
        public int[] ChildrenIds;
        public int[] SiblingIds;
        
        public HierarchyReferenceContainer(object parent, params object[] children)
        {
            ParentId = parent != null ? ReferenceTypeManager.GetOrCreateId(parent) : 0;
            
            if (children != null && children.Length > 0)
            {
                ChildrenIds = new int[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    ChildrenIds[i] = children[i] != null ? ReferenceTypeManager.GetOrCreateId(children[i]) : 0;
                }
            }
            else
            {
                ChildrenIds = new int[0];
            }
            
            SiblingIds = new int[0];
        }
        
        public HierarchyReferenceContainer(object parent, object[] children, object[] siblings)
        {
            ParentId = parent != null ? ReferenceTypeManager.GetOrCreateId(parent) : 0;
            
            if (children != null && children.Length > 0)
            {
                ChildrenIds = new int[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    ChildrenIds[i] = children[i] != null ? ReferenceTypeManager.GetOrCreateId(children[i]) : 0;
                }
            }
            else
            {
                ChildrenIds = new int[0];
            }
            
            if (siblings != null && siblings.Length > 0)
            {
                SiblingIds = new int[siblings.Length];
                for (int i = 0; i < siblings.Length; i++)
                {
                    SiblingIds[i] = siblings[i] != null ? ReferenceTypeManager.GetOrCreateId(siblings[i]) : 0;
                }
            }
            else
            {
                SiblingIds = new int[0];
            }
        }
        
        public T GetParent<T>() where T : class => ReferenceTypeManager.GetReference<T>(ParentId);
        
        public T[] GetChildren<T>() where T : class
        {
            if (ChildrenIds == null) return new T[0];
            
            T[] results = new T[ChildrenIds.Length];
            ReferenceTypeManager.GetReferences(ChildrenIds, results);
            return results;
        }
        
        public T[] GetSiblings<T>() where T : class
        {
            if (SiblingIds == null) return new T[0];
            
            T[] results = new T[SiblingIds.Length];
            ReferenceTypeManager.GetReferences(SiblingIds, results);
            return results;
        }
        
        public List<T> GetValidChildren<T>() where T : class
        {
            var children = GetChildren<T>();
            var validChildren = new List<T>();
            
            foreach (var child in children)
            {
                if (child != null)
                    validChildren.Add(child);
            }
            
            return validChildren;
        }
        
        public bool HasParent => ParentId != 0 && ReferenceTypeManager.IsValid(ParentId);
        public bool HasChildren => ChildrenIds != null && ChildrenIds.Length > 0;
        public bool HasSiblings => SiblingIds != null && SiblingIds.Length > 0;
        public int ChildrenCount => ChildrenIds?.Length ?? 0;
        public int SiblingsCount => SiblingIds?.Length ?? 0;
    }
} 
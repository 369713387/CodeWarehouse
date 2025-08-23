using System;
using System.Collections.Generic;
using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// ET ECS核心Entity类
    /// Entity本质上也是一个Component，可以挂载其他Component
    /// 数据和逻辑完全分离，Entity只存储数据
    /// </summary>
    public class Entity : IDisposable
    {
        public long Id { get; private set; }
        public Entity Parent { get; private set; }
        public bool IsDisposed { get; private set; }
        
        // 子实体管理
        private Dictionary<long, Entity> children = new Dictionary<long, Entity>();
        // 组件管理 - 每个类型只能有一个组件
        private Dictionary<Type, Entity> components = new Dictionary<Type, Entity>();
        
        private static long idGenerator = 1;
        
        public Entity()
        {
            Id = idGenerator++;
        }
        
        /// <summary>
        /// 设置父实体，实现树状结构管理
        /// </summary>
        public void SetParent(Entity parent)
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this.Id);
            }
            
            Parent = parent;
            if (parent != null)
            {
                parent.AddChild(this);
            }
        }
        
        /// <summary>
        /// 添加子实体
        /// </summary>
        private void AddChild(Entity child)
        {
            children[child.Id] = child;
        }
        
        /// <summary>
        /// 移除子实体
        /// </summary>
        private void RemoveChild(long id)
        {
            children.Remove(id);
        }
        
        /// <summary>
        /// 添加组件（组件本质上也是Entity）
        /// 一个Entity不能添加两个相同类型的Component
        /// </summary>
        public T AddComponent<T>() where T : Entity, new()
        {
            Type type = typeof(T);
            if (components.ContainsKey(type))
            {
                Debug.LogError($"Entity已经包含组件类型: {type.Name}");
                return null;
            }
            
            T component = new T();
            component.SetParent(this);
            components[type] = component;
            
            // 触发组件添加事件
            this.OnComponentAdded(component);
            
            return component;
        }
        
        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>() where T : Entity
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out Entity component))
            {
                return component as T;
            }
            return null;
        }
        
        /// <summary>
        /// 移除组件
        /// </summary>
        public void RemoveComponent<T>() where T : Entity
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out Entity component))
            {
                components.Remove(type);
                component.Dispose();
            }
        }
        
        /// <summary>
        /// 检查是否有指定组件
        /// </summary>
        public bool HasComponent<T>() where T : Entity
        {
            return components.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// 获取所有子实体
        /// </summary>
        public Entity[] GetChildren()
        {
            Entity[] result = new Entity[children.Count];
            children.Values.CopyTo(result, 0);
            return result;
        }
        
        /// <summary>
        /// 根据ID获取子实体
        /// </summary>
        public Entity GetChild(long id)
        {
            children.TryGetValue(id, out Entity child);
            return child;
        }
        
        /// <summary>
        /// 生命周期管理 - 释放时会自动释放所有子实体和组件
        /// 体现了ET的树状结构和自动生命周期管理
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
                
            IsDisposed = true;
            
            // 先释放所有组件
            foreach (var component in components.Values)
            {
                component.Dispose();
            }
            components.Clear();
            
            // 再释放所有子实体
            foreach (var child in children.Values)
            {
                child.Dispose();
            }
            children.Clear();
            
            // 从父实体中移除自己
            if (Parent != null)
            {
                Parent.RemoveChild(this.Id);
                Parent = null;
            }
            
            this.OnDestroy();
        }
        
        /// <summary>
        /// 组件添加时的回调，可以在子类中重写
        /// </summary>
        protected virtual void OnComponentAdded(Entity component)
        {
            // 子类可以重写此方法来处理组件添加逻辑
        }
        
        /// <summary>
        /// 销毁时的回调，可以在子类中重写
        /// </summary>
        protected virtual void OnDestroy()
        {
            // 子类可以重写此方法来处理销毁逻辑
        }
    }
}

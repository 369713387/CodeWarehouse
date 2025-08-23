using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// 移动组件 - 纯数据，不包含逻辑
    /// 移动相关的逻辑通过扩展方法实现
    /// </summary>
    public class MovementComponent : Entity
    {
        public Vector3 Position;        // 当前位置
        public Vector3 TargetPosition;  // 目标位置
        public Vector3 Direction;       // 移动方向
        public float MoveSpeed;         // 移动速度
        public bool IsMoving;           // 是否正在移动
        public float StopDistance;      // 停止距离
        
        /// <summary>
        /// 到目标位置的距离
        /// </summary>
        public float DistanceToTarget => Vector3.Distance(Position, TargetPosition);
        
        /// <summary>
        /// 是否到达目标位置
        /// </summary>
        public bool HasReachedTarget => DistanceToTarget <= StopDistance;
    }
}

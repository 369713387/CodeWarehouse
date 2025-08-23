using UnityEngine;

namespace ETECS
{
    /// <summary>
    /// AI状态枚举
    /// </summary>
    public enum AIState
    {
        Idle = 1,       // 待机
        Patrol = 2,     // 巡逻
        Chase = 3,      // 追击
        Attack = 4,     // 攻击
        Return = 5,     // 返回
        Dead = 6,       // 死亡
    }
    
    /// <summary>
    /// AI组件 - 纯数据，不包含逻辑
    /// AI逻辑通过扩展方法和类型分发实现
    /// </summary>
    public class AIComponent : Entity
    {
        public AIState CurrentState;    // 当前AI状态
        public AIState LastState;       // 上一个AI状态
        public float StateTimer;        // 状态计时器
        public float UpdateInterval;    // AI更新间隔
        public float LastUpdateTime;    // 上次更新时间
        
        // AI参数
        public float DetectRange;       // 检测范围
        public float AttackRange;       // 攻击范围
        public float ReturnRange;       // 返回范围
        public Vector3 SpawnPosition;   // 出生位置
        
        // 目标相关
        public Entity Target;           // 当前目标
        public long TargetId;          // 目标ID（用于网络同步）
        
        /// <summary>
        /// 是否应该更新AI
        /// 体现了ET中性能优化的理念：怪物周围没人时停止AI更新
        /// </summary>
        public bool ShouldUpdate => Time.time - LastUpdateTime >= UpdateInterval;
        
        /// <summary>
        /// 是否有目标
        /// </summary>
        public bool HasTarget => Target != null && !Target.IsDisposed;
        
        /// <summary>
        /// 切换AI状态
        /// </summary>
        public void ChangeState(AIState newState)
        {
            if (CurrentState != newState)
            {
                LastState = CurrentState;
                CurrentState = newState;
                StateTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// AI停止组件 - 演示组件挂组件的层级管理
    /// 这个组件挂载在AIComponent上，管理AI的启停逻辑
    /// 当怪物周围没有玩家时，这个组件会停止AI更新以节省性能
    /// </summary>
    public class AIStopComponent : Entity
    {
        public float CheckInterval = 2f;    // 检查间隔
        public float LastCheckTime;         // 上次检查时间
        public float PlayerDetectRange = 50f; // 玩家检测范围
        public bool IsAIStopped;            // AI是否已停止
        
        /// <summary>
        /// 是否应该检查周围玩家
        /// </summary>
        public bool ShouldCheck => Time.time - LastCheckTime >= CheckInterval;
    }
}

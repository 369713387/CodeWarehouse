using System;
using Sirenix.OdinInspector;

namespace FSMFrame
{
    public abstract class FSMStateBase : IFSMState,IReference
    {
        /// <summary>
        /// 状态类型
        /// </summary>
        [LabelText("状态类型")]
        public EnemyState StateTypes;

        /// <summary>
        /// 状态名称
        /// </summary>
        [LabelText("状态名称")]
        public string StateName;

        /// <summary>
        /// 状态的优先级，值越大，优先级越高。
        /// </summary>
        [LabelText("状态的优先级")]
        public int Priority;

        public FSMStateBase()
        {
        }

        public void SetData(EnemyState stateTypes, string stateName, int priority)
        {
            StateTypes = stateTypes;
            StateName = stateName;
            this.Priority = priority;
        }

        public virtual bool TryEnter(FSMStateMachineBase stackFsmComponent)
        {
            if (stackFsmComponent.CheckConflictState(GetConflictStateTypeses()))
            {
                return false;
            }

            return true;
        }

        public abstract EnemyState GetConflictStateTypeses();

        public abstract void OnEnter(FSMStateMachineBase stackFsmComponent);

        public abstract void OnExit(FSMStateMachineBase stackFsmComponent);

        /// <summary>
        /// 状态移除时调用
        /// </summary>
        /// <param name="stackFsmComponent"></param>
        public abstract void OnRemoved(FSMStateMachineBase stackFsmComponent);

        public virtual void Clear()
        {
        }
    }
}

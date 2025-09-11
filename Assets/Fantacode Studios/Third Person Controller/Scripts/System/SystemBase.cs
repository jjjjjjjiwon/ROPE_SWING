using System;
using System.Collections.Generic;
using UnityEngine;
namespace FS_ThirdPerson
{
    public class SystemBase : MonoBehaviour
    {
        /// <summary>
        /// Called when the script instance is being loaded.
        /// 스크립트 인스턴스가 로드될 때 호출된다.
        /// </summary>
        public virtual void HandleAwake() { }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// 스크립트가 활성화된 그 프레임에, 모든 Update 메서드가 처음 호출되기 직전에 호출된다.
        /// </summary>
        public virtual void HandleStart() { }

        /// <summary>
        /// Called every fixed framerate frame, if the MonoBehaviour is enabled.
        /// 이 함수는 MonoBehaviour가 활성화된 상태에서, 고정된 시간 간격(프레임)마다 반복적으로 실행됩니다.
        /// </summary>
        public virtual void HandleFixedUpdate() { }

        /// <summary>
        /// Called every frame, if the MonoBehaviour is enabled.
        /// 이 함수는 MonoBehaviour가 활성 상태일 때, 게임의 각 프레임마다 반복해서 실행됩니다.
        /// </summary>
        public virtual void HandleUpdate() { }

        /// <summary>
        /// Called when the animator moves.
        /// </summary>
        /// <param name="animator">The animator that moved.</param>
        /// 플레이어가 움직일 경우 게임 오브젝트 위치와 방향을 업데이트함
        public virtual void HandleOnAnimatorMove(Animator animator)
        {
            if (animator.deltaPosition != Vector3.zero)
                transform.position += animator.deltaPosition;
            transform.rotation *= animator.deltaRotation;
        }

        /// <summary>
        /// Gets or sets the priority of the system. Higher numbers indicate higher priority.
        /// 시스템의 우선순위를 가져오거나 설정합니다. 숫자가 클수록 더 높은 우선순위를 의미하며, 해당 시스템이 먼저 처리됩니다.
        /// </summary>
        public virtual float Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether this system is in focus.
        /// 이 속성은 시스템이 현재 포커스를 가지고 있는지(활성 상태인지) 여부를 나타내며, 값을 읽거나 변경할 수 있습니다.
        /// </summary>
        public bool IsInFocus { get; set; }

        /// <summary>
        /// Gets the state of the system.
        /// 시스템의 상태를 가져옵니다
        /// </summary>
        [field: SerializeField]
        public virtual SystemState State { get; } = SystemState.Other;

        /// <summary>
        /// Gets the sub-state of the system.
        /// 시스템의 하위 상태(sub-state)를 반환한다
        /// </summary>
        public virtual SubSystemState SubState { get; } = SubSystemState.None;

        /// <summary>
        /// Focuses this script, making it the only one to run. All other updates are discarded.
        /// 이 메서드는 이 스크립트에 포커스를 맞추어서, 다른 스크립트들의 업데이트는 무시하고 이 스크립트만 실행하게 만든다
        /// </summary>
        /// <param name="executeStates">If set to <c>true</c>, execute states.</param>
        public void FocusScript() => IsInFocus = true;

        /// <summary>
        /// Unfocuses this script, allowing other updates to run.
        /// 스크립의 포커스 해제, 다른 업데이트들이 실행되도록 한다
        /// </summary>
        public void UnFocusScript() => IsInFocus = false;

        /// <summary>
        /// Checks if the specified method is overridden in a derived class.
        /// 지정된 메서드가 파생 클래스에서 재정의되었는지 확인합니다.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <returns><c>true</c> if the method is overridden; otherwise, <c>false</c>.</returns>
        /// methodName이라는 메서드가 SystemBaseType에서 직접 선언되지 않았다면 (즉, 자식 클래스 등 다른 곳에서 재정의되었다면)true를 반환합니다.
        public bool HasOverrided(string methodName) => SystemBaseType.GetMethod(methodName).DeclaringType != SystemBaseType;
        

        ///// <summary>
        ///// Gets a value indicating whether execution states are being executed.
        ///// </summary>
        //public bool ExecuteStates { get; private set; }

        ///// <summary>
        ///// Gets the execution states of the system.
        ///// </summary>
        //public virtual List<SystemState> ExecutionStates => new List<SystemState> { State };

        /// <summary>
        /// Called when the system is entered.
        /// 시스템이 시작되거나 활성화될 때 이 함수가 호출됩니다.
        /// </summary>
        public virtual void EnterSystem() { }

        /// <summary>
        /// Called when the system is exited.
        /// 시스템이 종료되거나 비활성화될 때 이 함수가 호출됩니다.
        /// </summary>
        public virtual void ExitSystem() { }


        /// <summary>
        /// Called to reset the fighter.
        /// 캐릭터(파이터)를 초기 상태로 되돌리기 위해 호출되는 함수입니다.
        /// </summary>
        public virtual void OnResetFighter() { }

        /// <summary>
        /// Gets or sets the action to be called when the state is entered.
        /// 상태에 진입했을 때 실행될 함수를 읽거나 지정합니다.
        /// </summary>
        /// !!!!!!!!!!!!!!!!!!!!중요!!!!!!!!!!!!
        /// OnStateEntered라는 이름의 함수 저장 공간과 접근자(get/set)를 가진 프로퍼티가 있는 것
        public Action OnStateEntered { get; set; }

        /// <summary>
        /// Gets or sets the action to be called when the state is exited.
        /// 상태가 끝날 때 실행할 함수를 등록하거나 가져옵니다.
        /// </summary>
        public Action OnStateExited { get; set; }

        // SystemBaseType은 SystemBase 클래스의 타입 정보를 담고 있는 정적 읽기 전용 변수입니다.
        private static readonly Type SystemBaseType = typeof(SystemBase);
    }
    public class EquippableSystemBase : SystemBase
    {
        //호출할 때마다 new List<Type>() — 즉, 빈 타입 리스트를 새로 생성해서 반환합니다.
        public virtual List<Type> EquippableItems => new List<Type>();
        
        public List<SystemBase> SystemExclusionList => new List<SystemBase>();

        //public bool HasEquippableItem => EquippableItems.Count > 0;
    }
}
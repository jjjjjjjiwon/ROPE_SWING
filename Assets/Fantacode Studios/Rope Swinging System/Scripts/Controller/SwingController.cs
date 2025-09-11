using FS_Core;
using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


namespace FS_ThirdPerson
{
    // 애니메이터 파라미터 이름을 문자열 대신 해시값(int) 로 저장해둔 곳
    public static partial class AnimatorParameters
    {
        public static int SwingLeftHandIk = Animator.StringToHash("SwingLeftHandIk");
        public static int SwingRightHandIk = Animator.StringToHash("SwingRightHandIk");
        public static int SwingIkPriority = Animator.StringToHash("SwingIkPriority");
    }

    //애니메이션 클립이나 상태의 이름을 문자열로 저장해둔 곳.
    public static partial class AnimationNames
    {
        public static string SwingActions = "Swing Actions";
        public static string SwingLand = "Swing Land";
        public static string SwingClimbUp = "Swing Climb Up";
        public static string SwingClimbDown = "Swing Climb Down";
    }
}
    
namespace FS_SwingSystem
{
    public class SwingController : EquippableSystemBase
    {
        // *** 스윙 가능 여부 ***
        bool enableSwing = false;

        //값이 true이면, 캐릭터는 로프 없이도 스윙 동작을 사용할 수 있습니다.
        [Tooltip("If true, the character will be able to swing event without equipping the rope")]
        public bool swingWithoutEquip = false;

        //값을 true로 설정하면, 캐릭터가 스윙한 후 로프를 자동으로 해제합니다.
        [Tooltip("If true, the rope will be unequipped after the swing action is completed.")]
        public bool unEquipAfterSwing = false;

        // ***거리 및 애니메이션***

        //스윙에 필요한 최소 거리입니다.
        [Tooltip("The minimum distance required for the swing")]
        public float minDistance = 2f;

        //로프를 잡고 있을 때 사용하는 애니메이션 클립입니다.
        [Tooltip("The animation clip used when holding the rope.")]
        public AnimationClip hookHoldingClip;

        // *** 로프 관련 설정 ***

        // Rope settings
        //오른손으로 로프를 잡을 때 기준이 되는 위치 정보입니다.
        [Tooltip("Transform for holding the rope with the right hand.")]
        public Transform ropeHoldTransformRight;

        //왼손으로 로프를 잡을 때 기준이 되는 위치 정보입니다.
        [Tooltip("Transform for holding the rope with the left hand.")]
        public Transform ropeHoldTransformLeft;

        //몸체에 로프를 연결할 때 기준이 되는 위치 정보입니다.
        [Tooltip("Transform for attaching the rope to the body.")]
        public Transform ropeAttachTransformInBody;

        //[Tooltip("Hook Object")]
        //public HookItem hookObject;

        //로프의 두께 또는 넓이를 나타내는 값입니다.
        [Tooltip("The radius of the rope.")]
        public float ropeRadius = .025f;

        //로프의 외형에 사용되는 머티리얼입니다.
        [Tooltip("Material used for the rope's appearance.")]
        public Material ropeMaterial;

        //로프 세그먼트의 해상도입니다.  세그먼트 : 분할
        [Tooltip("Resolution of the rope's segments.")]
        public int ropeResolution = 50;

        // 로프의 최대 길이입니다.
        [Tooltip("Maximum length of the rope.")]
        public float ropeLength = 10f;

        // 로프의 너비
        [Tooltip("Width of the rope.")]
        public float ropeWidth = 0.1f;
        
        // 로프 움직임에 적용되는 감쇠량입니다.
        [Tooltip("Amount of dampening applied to the rope's movement.")]
        public float dampening = 0.5f;

        // *** 로프 던지기 ***

        //로프를 던질 때, 얼마나 높이 던지는지를 나타내는 값입니다.
        [Tooltip("Height of the rope throw.")]
        public float throwHeight = 1f;

        //로프를 던지는 동작이 얼마나 오래 지속되는지를 나타내는 값입니다.
        [Tooltip("Duration of the rope throw.")]
        public float throwDuration = 0.5f;

        // *** 스윙 동작 설정 ***

        // Swing settings
        //플레이어가 그네나 줄을 탈 때, 움직임을 만드는 힘의 크기입니다
        [Tooltip("The force applied to the player during swinging.")]
        public float swingForce = 4f;

        //스윙 시 회전 속도
        [Tooltip("The speed of rotation while swinging.")]
        public float swingRotationSpeed = 100f;

        //캐릭터가 로프를 올라가는 속도입니다.
        [Tooltip("The speed at which the character climbs up the rope.")]
        public float climbSpeed = 3;
        
        //스윙에서 착지할 때 적용되는 앞쪽 힘에 대한 배율입니다.
        [Tooltip("Multiplier for the forward force applied upon landing from a swing.")]
        public float forwardLandForceMultiplier = 1f;

        //스윙에서 착지할 때 적용되는 위쪽 힘에 대한 배율입니다.
        [Tooltip("Multiplier for the upward force applied upon landing from a swing.")]
        public float upwardLandForceMultiplier = 2f;

        //스윙하는 동안 적용되는 감쇠량입니다.
        [Tooltip("Amount of damping applied while swinging.")]
        public float damping = 0.5f;

        //스윙하는 동안 적용되는 중력 힘입니다.
        [Tooltip("The gravity force applied during swinging.")]
        public float gravity = -20f;

        //스윙 중 충돌 시 적용되는 마찰력입니다.
        [Tooltip("Friction applied during collisions while swinging.")]
        public float collisionFriction = .2f;

        // *** 조준선 설정 ***

        // Crosshair settings
        //플레이어가 그네 후크를 조준할 때 화면에 나타나는 십자선 모양 UI(크로스헤어)의 원본 오브젝트입니다.
        [Tooltip("Prefab used for the crosshair when aiming the swing hook.")]
        public GameObject crosshairPrefab;

        //십자선의 크기
        [Tooltip("Size of the crosshair.")]
        public float crosshairSize = .3f;

        // *** 카메라 흔들림 ***

        // Camera shake settings
        //스윙 시작 시 카메라 흔들림의 양입니다.
        [Tooltip("Amount of camera shake when the swing start")]
        public float cameraShakeAmount = .2f;

        //스윙 시작 시 카메라 흔들림의 지속 시간입니다.
        [Tooltip("Duration of camera shake when the swing start")]
        public float cameraShakeDuration = .3f;

        // *** 변수 & 필드

        // 로프의 총길이
        float totalRopeLength = 10f;

        // This variable will store the reference of the swing hook item, even when it's not equipped
        // 이 변수는 플레이어가 스윙 후크를 장착하지 않았을 때도 그 아이템 정보를 계속 가지고 있습니다.
        EquippableItem availableSwingHook;

        // *** Events ***

        public UnityEvent RopeReleased; // 로프가 풀렸을 때 실행되는 이벤트
        public UnityEvent RopeHooked; // 로프가 걸렸을 때 실행되는 이벤트
        public UnityEvent SwingStarted; // 스윙 동작이 시작될 때 실행되는 이벤트
        public UnityEvent ExitedFromSwing; // 스윙을 종료했을 때 실행되는 이벤트

        public bool debug;

        // *** 컴포턴트 참조 변수들 ***

        Animator animator; // 애니메이션 제어기
        LocomotionICharacter player; // 플레이어 캐릭터 이동 관련 인터페이스
        EnvironmentScanner environmentScanner; // 주변 환경 체크용 스캐너
        PlayerController playerController; // 플레이어 입력 및 제어
        ItemEquipper itemEquipper; // 아이템 착용관련 시스템
        LocomotionInputManager locomotionInput; // 이동 입력 매니저
        SwingRope rope; // 스윙용 로프 객체
        SwingData SwingData; // 스윙 관련 데이터 클래스
        AnimGraph animGraph; // 애니메이션 그래프
        GameObject crosshairObj; // 조준선 UI 오브젝트
        Crosshair crosshair; // 조준선 Ui 컴포넌트

        // *** 로프/스윙 관련 벡터 및 변수 ***

        Vector3 pivotPoint; // 로프 스윙의 회전 중심점
        float rotateSpeed = 15; // 회전 속도
        Vector3 velocity; // 현재 속도 벡터
        Vector3 prevVelocity; // 이전 프레임 속도
        Vector3 maxHeightPoint; // 스윙 중 최대 높이 위치
        Vector3 swingDirection; // 스윙 방향 벡터
        float maxSwingAngle = 60f; // 스윙할 수 있는 최대 각도
        private Vector3 ropeVector; // 로프 벡터(로프 방향 및 길이 표현)
        private CharacterController characterController; // 캐릭터 컨트롤러 컴포넌트(플레이어 이동 물리 처리)

        // *** 상수 관련 변수 & 프로퍼티 ***

        public bool InSwing { get; private set; } // 현재 스윙 중인지 여부
        public bool InAction { get; private set; } // 현재 액션 상태인지 여부(스윙, 점프 등)
        public bool IsFalling { get; private set; } // 낙하 중인지 여부
        bool isClimbing; // 로프를 타고 올라가는 중인지

        //bool isHoldingClipPlaying;

        // *** 상속받은 멤버 ***
        
        // 이 클래스가 현재 담당하는 상태 : 스윙 상태
        public override SystemState State { get; } = SystemState.Swing;
        // 이 시스템이 착용할 수 있는 아이템 리스트에 SwingHookItem만 포함
        public override List<Type> EquippableItems => new List<Type>() { typeof(SwingHookItem) };

        //public override List<SystemState> ExecutionStates => new List<SystemState>() { SystemState.Locomotion, SystemState.Swing, SystemState.Parkour }; 


        public SwingHookItem currentHookItem
        {
            get
            {
                // 만약 EquippedItem이 SwingHookItem이면, 그걸 shooterWeaponData에 담아서 반환하고, 아니면 null 반환해줘.
                return (itemEquipper?.EquippedItem is SwingHookItem shooterWeaponData)
                    ? shooterWeaponData
                    : null;
            }
        }
        // 오른손에 아이템이 장착되어 있다면, 그 아이템 오브젝트가 SwingHookObject 타입일 수도 있다
        public SwingHookObject CurrentHookRight => itemEquipper?.EquippedItemRight as SwingHookObject;
        // 왼손에 아이템이 장착되어 있다면, 그 아이템 오브젝트가 SwingHookObject 타입일 수도 있다
        public SwingHookObject CurrentHookLeft => itemEquipper?.EquippedItemLeft as SwingHookObject;
        // 현재 장착된 아이템 오브젝트를 SwingHookObject타입으로 반환// 장착된 아이템 오브젝트가 SwingHookObject 타입이면 그 오브젝트를 반환합니다.
        public SwingHookObject CurrentHookItem => itemEquipper.EquippedItemObject as SwingHookObject;



        private void Start()
        {
            totalRopeLength = ropeLength; // 로프의 전체 길이 값을 ropeLength으로 초기화
            // 오브젝트에 붙은 LocomotionICharacter 컴포넌트를 찾아서 player 변수에 넣는다
            player = GetComponent<LocomotionICharacter>();
            animator = player.Animator; // player가 가진 애니메이터 컴포넌트를 가져와서 animator에 저장
            // 환경 정보를 탐색하는 컴포넌트를 찾아서 저장
            environmentScanner = GetComponent<EnvironmentScanner>();
            // 플레이어 입력을 처리하는 컴포넌트를 가져온다
            locomotionInput = GetComponent<LocomotionInputManager>();
            // CharacterController 컴포넌트를 찾아 저장한다
            characterController = GetComponent<CharacterController>();
            // 아이템 장착/해제 시스템을 담당하는 컴포넌트를 가져온다
            itemEquipper = GetComponent<ItemEquipper>();
            // 애니메이션 그래프를 다루는 컴포넌트를 찾는다
            animGraph = GetComponent<AnimGraph>();
            // 플레이어 조작에 관련된 컴포넌틑를 가져온다
            playerController = GetComponent<PlayerController>();

            // 만약 오른손 로프 잡는 위치가 지정 안 됐다면, 애니메이터에서 오른손 뼈 위치를 찾아 저장한다
            if (ropeHoldTransformRight == null)
                ropeHoldTransformRight = animator.GetBoneTransform(HumanBodyBones.RightHand);
            // 만약 왼손 로프 잡는 위치가 지정 안 됐다면, 애니메이터에서 오른손 뼈 위치를 찾아 저장한다
            if (ropeHoldTransformLeft == null)
                ropeHoldTransformLeft = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            // 만약 로프가 몸체에 붙는 위치가 지정 안 됐다면, Hips 뼈 위치를 찾아서 저장
            if (ropeAttachTransformInBody == null)
                ropeAttachTransformInBody = animator.GetBoneTransform(HumanBodyBones.Hips);

            // 아이템 장착 시 EquipItem 메서드가 호출되도록 이벤트에 등록
            itemEquipper.OnEquip += EquipItem;
            // 아이템 해제 시 UnEquipItem 메서드가 호출되도록 이벤트에 등록
            itemEquipper.OnUnEquip += UnEquipItem;

            SetCrosshair(); // SetCrosshair초기화

            // 장착 가능한 아이템 목록에서 스윙 후크 아이템을 찾아서 availableSwingHook에 저장
            // itemEquipper.equippableItems에 첫번째 아이템을 SwingHookItem인지 검사해서 availableSwingHook에 넣는다
            availableSwingHook = itemEquipper.equippableItems.FirstOrDefault(i => i is SwingHookItem);
            // availableSwingHook가 있으면 스윙 기능을 활성화 한다
            enableSwing = availableSwingHook != null;
        }


        private void LateUpdate()
        {
            if(rope != null)
                rope.RopeUpdate();
        }

        public override void HandleUpdate()
        {
            if (Time.timeScale == 0) return;

            if (InSwing)
            {
                ControlVelecity();
                UpdateSwingPhysics();
                HandleCollisions();
            }
        }

        private void Update()
        {
            HandleSwingInputs();

            if (playerController.CurrentSystemState == SystemState.Locomotion || playerController.IsInAir)
            {
                if ((swingWithoutEquip && availableSwingHook != null) || currentHookItem != null)
                {

                    if (enableSwing && !InAction)
                    {
                        SwingHandler();
                    }
                }
            }
        }

        void SwingHandler()
        {
            crosshair.size = crosshairSize;
            SwingData = environmentScanner.GetSwingLedgeData(totalRopeLength - 1, minDistance, ropeHoldTransformRight, true);
            if (SwingData.hasLedge)
            {
                var dir = (SwingData.hookPosition - playerController.cameraGameObject.transform.position).normalized;
                var offset = dir * crosshair.GetMeshBoundsSize();
                crosshairObj.transform.position = SwingData.hookPosition - offset;
                crosshairObj.SetActive(true);
            }
            else
                crosshairObj.SetActive(false);


            if (HookInputHolding && SwingData.hasLedge)
            {
                StartCoroutine(ControlSwingAction());
            }
        }

        IEnumerator ControlSwingAction()
        {
            bool equippedForAction = false;

            if (currentHookItem == null)
            {
                if (availableSwingHook == null) yield break;

                itemEquipper.EquipItem(availableSwingHook);
                yield return new WaitUntil(() => !itemEquipper.IsChangingItem);

                equippedForAction = true;
            }

            if (InAction || currentHookItem == null) yield break;
            InAction = true;
            var currentFocusedScript = playerController.FocusedScript;
            playerController.PreventRotation = true;
            itemEquipper.PreventItemSwitching = true;
            itemEquipper.PreventItemUnEquip = true;
            yield return SetRotation(SwingData.forwardDirection, rotateSpeed);
            crosshairObj.SetActive(false);
            RopeReleased?.Invoke();

            // Play rope throwing animation
            animGraph.Crossfade(currentHookItem.ropeThrowingClip, currentHookItem.ropeThrowingClip, mask: Mask.Arm);
            yield return new WaitForSeconds(.3f);

            pivotPoint = SwingData.hookPosition;

            // Start rope throwing
            rope.StartThrow(ropeHoldTransformRight, pivotPoint);

            // Wait for the rope to reach the hook point.
            while (!rope.HasReachedTarget())
            {
                var hp = SwingData.hookPosition;
                hp.y = transform.position.y;
                var direction = (hp - transform.position).normalized;
                var dir = Quaternion.LookRotation(direction);
                transform.rotation = dir;

                if (playerController.FocusedScript != null && currentFocusedScript != playerController.FocusedScript && playerController.FocusedScript != this)
                {
                    RetractRope();
                    if (unEquipAfterSwing && equippedForAction)
                        itemEquipper.UnEquipItem();
                    yield break;
                }
                yield return null;
            }
            rope.SetRopeState(RopeState.Normal);

            // Rope hooked
            RopeHooked?.Invoke();

            animGraph.StopLoopingAnimations(false);


            playerController.PreventRotation = false;
            playerController.PreventFallingFromLedge = false;

            float currLengthToHook = SwingData.distance;
            float currLengthToHookWhileIsGrounded = SwingData.distance;
            var prevPos = transform.position;


            var initialVelocity = Vector3.zero;


            itemEquipper.PreventItemUnEquip = true;

            while (true)
            {
                // The initial velocity to apply at the start of the swing.
                initialVelocity = transform.position - prevPos;

                // Continuously updating currLengthToHook as the player moves.
                currLengthToHook = Vector3.Distance(ropeHoldTransformRight.position, pivotPoint);

                if (CheckGround())
                {
                    currLengthToHookWhileIsGrounded = currLengthToHook;

                    // Check if the player is moving away from the hook point by a distance greater than the total rope length.
                    // If so, the rope will retract.
                    if (currLengthToHook < minDistance || currLengthToHook >= rope.totalRopeLength || HookReleaseDown)
                    {
                        //Retract rope
                        RetractRope();
                        yield break;
                    }
                }
                else
                {
                    if (currLengthToHook > currLengthToHookWhileIsGrounded + .5f)
                        break;
                }

                prevPos = transform.position;
                yield return null;
            }


            playerController.PreventFallingFromLedge = true;
            ropeLength = currLengthToHook + .5f;
            rope.SetHookRopeAsStraight();
            rope.SetRopeState(RopeState.Swinging);
            initialVelocity.y = 0;

            //Apply initial velocity
            velocity = initialVelocity.normalized * swingForce;


            // Pause all systems and focus on the swing.
            player.OnStartSystem(this);
            playerController.IsInAir = false;

            // Enable hand holding IK for swinging
            handIk = true;

            // Applying camera shake
            playerController.OnStartCameraShake?.Invoke(cameraShakeAmount, cameraShakeDuration);

            // Play swinging animation blend tree
            animator.CrossFadeInFixedTime(AnimationNames.SwingActions, 0.2f);
            InitializeSwingOriginPosition(ropeLength);
            InSwing = true;

            SwingStarted?.Invoke();

            bool isSwingingAtLowVelocity = false;
            bool isFalling = false;
            // Wait until the player presses the jump key or the character is grounded
            while (!locomotionInput.JumpKeyDown && !CheckGround())
            {
                var curVelocityAngle = Vector3.Angle(Vector3.down, (maxHeightPoint - pivotPoint).normalized);
                var curAngle = Vector3.Angle(Vector3.down, ropeVector);
                isFalling = prevPos.y > transform.position.y;
                isSwingingAtLowVelocity = curAngle < 15 && curVelocityAngle < 15;
                if (isSwingingAtLowVelocity)
                    HandleClimbing();
                prevPos = transform.position;
                yield return null;
            }
            isClimbing = false;
            ExitFromSwing();
            ExitedFromSwing?.Invoke();

            // Disable hand holding IK
            handIk = false;

            // Applying camera shake
            playerController.OnStartCameraShake?.Invoke(cameraShakeAmount, cameraShakeDuration);

            // If the player is grounded, crossfade to the "Landing" animation and exit the swing.
            // Otherwise, calculate the landing momentum and perform the landing with that momentum.
            if (CheckGround())
            {
                StartCoroutine(CrossFadeAsync(AnimationNames.LandAndStepForward, .2f, false));
                player.SetCurrentVelocity(0, Vector3.zero);
            }
            else
            {
                HandleLandingMomentum(!isSwingingAtLowVelocity && !isFalling);
            }
            
            yield return new WaitForSeconds(.2f);
            if (unEquipAfterSwing && equippedForAction)
                itemEquipper.UnEquipItem();
        }

        bool CheckGround()
        {
            return player.CheckIsGrounded();
        }

        public void ExitFromSwing()
        {
            rope.SetRopeState(RopeState.Normal);
            rope.ResetRope();
            InSwing = false;
            playerController.PreventRotation = false;
            animGraph.PlayLoopingAnimation(hookHoldingClip, mask: Mask.RightHand, isActAsAnimatorOutput: true);
            crosshairObj.SetActive(false);
            InAction = false;
            player.OnEndSystem(this);
            itemEquipper.PreventItemUnEquip = false;
            itemEquipper.PreventItemSwitching = false;
        }

        void RetractRope()
        {
            ExitFromSwing();
            enableSwing = false;
            StartCoroutine(AsyncUtil.RunAfterDelay(.5f, () => { enableSwing = true; }));
        }

        IEnumerator SetRotation(Vector3 lookDir, float rotateSpeed)
        {
            var dir = Quaternion.LookRotation(lookDir);
            float rotation = 0;
            var angle = Quaternion.Angle(transform.rotation, dir);
            while (angle > .1f && angle > rotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, dir, rotateSpeed * Time.deltaTime * 50);
                yield return null;
                angle = Quaternion.Angle(transform.rotation, dir);
                rotation += rotateSpeed * Time.deltaTime * 50;
            }
            transform.rotation = dir;
        }

        IEnumerator CrossFadeAsync(string anim, float crossFadeTime = .2f, bool enableRootmotion = false, Action onComplete = null)
        {
            if (enableRootmotion)
                EnableRootMotion();
            animator.CrossFadeInFixedTime(anim, crossFadeTime);
            yield return null;
            //while (animator.IsInTransition(0))
            //{
            //    yield return null;
            //}
            var animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f;

            while (timer <= animState.length)
            {
                timer += Time.deltaTime * animator.speed;
                yield return null;
            }
            if (enableRootmotion)
                ResetRootMotion();
            onComplete?.Invoke();
        }

        void SetCrosshair()
        {
            crosshairObj = Instantiate(crosshairPrefab);
            crosshairObj.name = crosshairPrefab.name;
            crosshair = crosshairObj.AddComponent<Crosshair>();
            crosshairObj.transform.parent = this.transform.parent;
            crosshairObj.transform.localScale = Vector3.zero;
            crosshairObj.SetActive(false);
        }

        #region Landing

        void HandleLandingMomentum(bool playLandAnimation = true)
        {
            // Get the current direction of movement, excluding the y value.
            var currDir = velocity.normalized;
            currDir.y = 0;

            InSwing = false;

            // Calculate the origin point of the swing by adjusting the pivotPoint down by the rope's length.
            var originPoint = (pivotPoint - Vector3.up * rope.hookRopeLength);
            var currentPlayerPos = transform.position;
            currentPlayerPos.y = originPoint.y; 

            // Set the maximum height of the swing and adjust it to match the originPoint's y level.
            var maxHeight = maxHeightPoint;
            maxHeight.y = originPoint.y;

            // Calculate the distance between the player's current position and the max height.
            var dist = Vector3.Distance(maxHeight, currentPlayerPos);

            // Determine the target landing position based on the current swing velocity
            var targetPosition = transform.position + currDir.normalized * dist * forwardLandForceMultiplier;
            targetPosition.y = originPoint.y; 

            // Calculate the height difference for calculate landing velocity
            var height = rope.currentRopeHoldPointTransform.position.y - originPoint.y;

            // Flag to determine whether to play the landing animation.
            


            // If the player is falling, prevent the upward landing jump force
            if (velocity.y < 0 || !playLandAnimation)
            {
                height = 0;
                //playLandAnimation = false;
            }

            // Smoothly rotate the player towards the landing direction.
            StartCoroutine(SetRotation(currDir, 5));

            // If the landing animation should play, crossfade to the land animation and exit from the swing.
            if (playLandAnimation)
            {
                StartCoroutine(CrossFadeAsync(AnimationNames.SwingLand, .2f, false));
                player.SetCurrentVelocity(velocity.y * upwardLandForceMultiplier, velocity * forwardLandForceMultiplier);
            }
            else
            {
                // Otherwise, crossfade to the falling animation and exit from the swing.
                StartCoroutine(CrossFadeAsync(AnimationNames.FallTree, .2f, false));
                player.SetCurrentVelocity(velocity.y * upwardLandForceMultiplier * 0.5f, velocity * forwardLandForceMultiplier * 0.5f);
            }

        }

        #endregion

        #region Swinging Controller

        void InitializeSwingOriginPosition(float length)
        {
            ropeVector = Vector3.down * length;
        }

        void ControlVelecity()
        {
            float inputX = RopeClimbModifierHolding ? 0: locomotionInput.DirectionInput.x;
            float inputY = RopeClimbModifierHolding ? 0 : locomotionInput.DirectionInput.y;


            var tarRot = playerController.cameraGameObject.transform.forward;
            tarRot.y = 0;
            if (!IsFalling)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(tarRot), Time.deltaTime * swingRotationSpeed);
            Vector3 cameraForward = Vector3.ProjectOnPlane(playerController.cameraGameObject.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(playerController.cameraGameObject.transform.right, Vector3.up).normalized;
            swingDirection = Vector3.MoveTowards(swingDirection, (cameraRight * inputX + cameraForward * inputY).normalized, Time.deltaTime);

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float dotProduct = Vector3.Dot(swingDirection, horizontalVelocity.normalized);

            float currentAngle = Vector3.Angle(Vector3.down, ropeVector);
            if (currentAngle < maxSwingAngle && dotProduct >= 0 || horizontalVelocity.magnitude < 0.1f)
            {
                velocity += swingDirection * swingForce * Time.deltaTime;
            }

        }

        void UpdateSwingPhysics()
        {
            velocity += Vector3.up * gravity * Time.deltaTime;
            Vector3 newPosition = transform.position + velocity * Time.deltaTime;
            Vector3 newRopeVector = newPosition - pivotPoint;

            newRopeVector = newRopeVector.normalized * ropeLength;
            newPosition = pivotPoint + newRopeVector;

            velocity = (newPosition - transform.position) / Time.deltaTime;

            float currentAngle = Vector3.Angle(Vector3.down, newRopeVector);


            if ((velocity.y < 0 && swingDirection == Vector3.zero) || (currentAngle > maxSwingAngle * .7f))
            {
                velocity *= (1 - Time.deltaTime * damping);
            }


            Vector3 newPos = transform.position + velocity * Time.deltaTime;
            if (prevVelocity.y > 0 && velocity.y < 0)
            {
                maxHeightPoint = newPos;
            }


            characterController.Move(velocity * Time.deltaTime);
            prevVelocity = velocity;

            ropeVector = newRopeVector;



            var maxSwingLength = Mathf.Clamp(rope.hookRopeLength * ((maxSwingAngle + gravity) * Mathf.Deg2Rad), 0.1f, 3);

            var currentLen = Vector3.Distance(transform.position, pivotPoint + Vector3.down * rope.hookRopeLength);

            var percentage = currentLen / maxSwingLength * .5f;

            var dir = (transform.position - pivotPoint + Vector3.down * rope.hookRopeLength).normalized;
            var v = transform.InverseTransformDirection(dir).normalized * percentage;


            animator.SetFloat("x", v.x, .1f, Time.deltaTime);
            animator.SetFloat("y", v.z, .1f, Time.deltaTime);

        }

        void HandleCollisions()
        {
            if (Physics.SphereCast(animator.GetBoneTransform(HumanBodyBones.Hips).position, characterController.radius, velocity.normalized, out RaycastHit hit, characterController.radius, environmentScanner.ObstacleLayer))
            {
                Vector3 reflection = Vector3.Reflect(velocity.normalized, hit.normal);
                reflection += Vector3.up * 0.5f;
                velocity = reflection.normalized * velocity.magnitude * collisionFriction;
                Vector3 pushDirection = (transform.position - hit.point).normalized;
                characterController.Move(pushDirection * velocity.magnitude * Time.deltaTime);
            }

            //GizmosExtend.drawSphereCast(animator.GetBoneTransform(HumanBodyBones.Hips).position, characterController.radius, velocity.normalized, characterController.radius, Color.red);
        }

        void HandleClimbing()
        {
            float climbMovement = 0;
            if (RopeClimbModifierHolding && !isClimbing)
            {
                var climbInputMultiplier = locomotionInput.DirectionInput.y;
                if (climbInputMultiplier > 0 && ropeLength > minDistance + 1.5f)
                    StartClimb(AnimationNames.SwingClimbUp);
                else if (climbInputMultiplier < 0 && ropeLength < totalRopeLength - .5f)
                    StartClimb(AnimationNames.SwingClimbDown);
            }

            if (isClimbing)
            {
                climbMovement = (rope.hookRopeLength > minDistance && rope.hookRopeLength < totalRopeLength) ? climbSpeed * animator.deltaPosition.y : 0;
            }
            ropeLength = Mathf.Clamp(ropeLength - climbMovement, minDistance, totalRopeLength);
        }

        void StartClimb(string animation)
        {
            rope.SetRopeState(RopeState.Climbing);
            isClimbing = true;
            handIk = false;
            StartCoroutine(CrossFadeAsync(animation, onComplete: () =>
            {
                if (isClimbing)
                {
                    isClimbing = false;
                    handIk = true;
                    rope.SetRopeState(RopeState.Swinging);
                }
            }));
        }

        #endregion

        #region rootmotion

        bool prevRootMotionVal;

        public void EnableRootMotion()
        {
            prevRootMotionVal = player.UseRootMotion;
            player.UseRootMotion = true;
        }
        public void ResetRootMotion()
        {
            player.UseRootMotion = prevRootMotionVal;
        }

        public override void HandleOnAnimatorMove(Animator animator)
        {
            transform.rotation *= animator.deltaRotation;
            characterController.Move(animator.deltaPosition);
        }

        #endregion

        #region IK

        bool handIk;

        private void OnAnimatorIK(int layerIndex)
        {
            if (handIk)
            {
                var dirToHookFromLefthand = (SwingData.hookPosition - ropeHoldTransformLeft.position).normalized;
                var offset = (ropeHoldTransformRight.position - animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                animator.SetIKPosition(AvatarIKGoal.RightHand, ropeHoldTransformLeft.transform.position + dirToHookFromLefthand * .25f - offset);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            }

            if (isClimbing)
            {
                var rightHandIkWeight = animator.GetFloat(AnimatorParameters.SwingRightHandIk);
                var leftHandIkWeight = animator.GetFloat(AnimatorParameters.SwingLeftHandIk);
                var swingIkPriority = animator.GetFloat(AnimatorParameters.SwingIkPriority);


                // If the priority is zero, the left hand is currently holding the rope, meaning the other hand is attempting to reach for a higher point on the rope.
                rope.currentRopeHoldPointTransform = swingIkPriority == 0 ? ropeHoldTransformLeft : ropeHoldTransformRight;


                var dirToHookFromLefthand = (SwingData.hookPosition - ropeHoldTransformLeft.position).normalized;
                var offset = (ropeHoldTransformRight.position - animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                animator.SetIKPosition(AvatarIKGoal.RightHand, ropeHoldTransformLeft.transform.position + dirToHookFromLefthand * .25f - offset);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIkWeight);

                var dirToHookFromRighthand = (SwingData.hookPosition - ropeHoldTransformRight.position).normalized;
                offset = (ropeHoldTransformLeft.position - animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, ropeHoldTransformRight.transform.position + dirToHookFromRighthand * .25f - offset);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIkWeight);
            }
        }

        #endregion

        #region input manager

#if inputsystem
        FSSystemsInputAction input;

        private void OnEnable()
        {
            input = new FSSystemsInputAction();
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }
#endif

        [Tooltip("Key to throw the rope hook.")]
        public KeyCode hookInput = KeyCode.F;

        [Tooltip("Key to release the rope hook.")]
        public KeyCode hookReleaseInput = KeyCode.F;

        [Tooltip("Hold this key to climb up or down the rope.")]
        public KeyCode ropeClimbModifier = KeyCode.LeftShift;

        

        [Tooltip("The button to throw the rope hook.")]
        public string hookInputButton;

        [Tooltip("The button to release the rope hook.")]
        public string hookReleaseInputButton;

        [Tooltip("Hold this button to climb up or down the rope.")]
        public string ropeClimbModifierButton;


        public bool HookInputHolding { get; private set; }
        public bool HookReleaseDown { get; private set; }
        public bool RopeClimbModifierHolding { get; private set; }


        void HandleSwingInputs()
        {

#if inputsystem
            HookInputHolding = input.Swing.Hook.inProgress;
            HookReleaseDown = input.Swing.HookRelease.WasPerformedThisFrame();
            RopeClimbModifierHolding = input.Swing.ClimbModifier.inProgress;
#else
            HookInputHolding = Input.GetKey(hookInput) || (!string.IsNullOrEmpty(hookInputButton) && Input.GetButton(hookInputButton));
            HookReleaseDown = Input.GetKeyDown(hookReleaseInput) || (!string.IsNullOrEmpty(hookReleaseInputButton) && Input.GetButtonDown(hookReleaseInputButton));
            RopeClimbModifierHolding = Input.GetKey(ropeClimbModifier) || (!string.IsNullOrEmpty(ropeClimbModifierButton) && Input.GetButton(ropeClimbModifierButton));
#endif
        }

        #endregion

        #region Equip And UnEquip

        public void EquipItem(EquippableItem equippableItem)
        {
            if (equippableItem is SwingHookItem)
            {
                rope = new SwingRope(ropeRadius, ropeWidth, ropeMaterial, throwDuration, ropeLength, dampening, ropeResolution, throwHeight, this.transform, ropeAttachTransformInBody, ropeHoldTransformLeft, ropeHoldTransformRight, CurrentHookItem.ropeHookPoint, CurrentHookItem.transform);
                //SetCrosshair();
                enableSwing = true;
                //player.OnFocusSystem(this);

                availableSwingHook = equippableItem as SwingHookItem;
            }
        }
        public void UnEquipItem()
        {
            if (currentHookItem is SwingHookItem)
            {
                if (rope != null)
                {
                    rope.DeleteRope();
                    rope = null;
                }
                //Destroy(crosshairObj);
                //player.OnUnFocusSystem(this);

                availableSwingHook = itemEquipper.equippableItems.FirstOrDefault(i => i is SwingHookItem);
                enableSwing = availableSwingHook != null;
                crosshairObj.SetActive(false);
            }
        }


        #endregion
    }
}
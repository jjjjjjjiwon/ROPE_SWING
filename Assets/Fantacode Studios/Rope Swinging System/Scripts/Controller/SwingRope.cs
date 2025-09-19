using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace FS_SwingSystem
{
    public enum RopeState { Normal, Swinging, Throwing, Climbing}

    public class SwingRope
    {
        public float ropeRadius = 0.025f;
        public Material ropeMaterial;
        public int hookRopeResolution = 50;
        public float totalRopeLength = 5f;
        public float ropeWidth = 0.1f;
        public float dampening = 0.8f;
        public float smoothingFactor = 0.1f;
        public float throwHeight = 1f;
        public float throwDuration = 2f;

        public Transform ropeHoldTransformLeft;
        public Transform ropeHoldTransformRight;
        public Transform ropeAttachPointToHook;
        public Transform ropeAttachTransformInBody;
        public Transform hookObject;

        public float hookRopeLength = 5f;

        private int holdRopeResolution = 50;
        private float holdRopelength = .1f;

        public RopeState ropeState = RopeState.Normal;

        private Vector3[] hookRopePositions;
        private Vector3[] hookRopePreviousPositions;
        private Vector3[] hookRopeVelocities;

        private Vector3[] holdRopePositions;
        private Vector3[] holdRopePreviousPositions;
        private Vector3[] holdRopeVelocities;

        private LineRenderer hookRope;
        private LineRenderer holdRope;

        private Vector3 hookPoint;
        private float initialRopeLength;
        private float targetRopeLength;
        private int constraintIterations = 50;
        private float colliderRadius = 0.05f;
        private Transform playerTransform;



        public Transform currentRopeHoldPointTransform;
        Vector3 defaultHookPos = Vector3.zero;
        Quaternion defaultHookRot = Quaternion.identity;
        Transform defaultHookObjectParent;
        bool hooked;

        public SwingRope(float ropeRadius, float ropeWidth, Material ropeMaterial, float throwDuration, float ropeLength, float dampening, int ropeResolution, float throwHeight, Transform transform, Transform ropeAttachTransformInBody, Transform ropeHoldTransformLeft, Transform ropeHoldTransformRight, Transform ropeAttachPointToHook, Transform hookObject)
        {
            this.totalRopeLength = ropeLength;
            this.ropeWidth = ropeWidth;
            this.dampening = dampening;
            this.ropeRadius = ropeRadius;
            this.ropeMaterial = ropeMaterial;
            this.throwDuration = throwDuration;
            this.hookRopeResolution = ropeResolution;
            this.throwHeight = throwHeight;
            this.playerTransform = transform;
            this.ropeAttachTransformInBody = ropeAttachTransformInBody;
            this.ropeHoldTransformRight = ropeHoldTransformRight;
            this.ropeHoldTransformLeft = ropeHoldTransformLeft;
            this.ropeAttachPointToHook = ropeAttachPointToHook;
            this.hookObject = hookObject;
            currentRopeHoldPointTransform = ropeHoldTransformRight;
            if (hookObject != null)
            {
                defaultHookObjectParent = hookObject.transform.parent;
                defaultHookPos = hookObject.transform.localPosition;
                defaultHookRot = hookObject.transform.localRotation;
            }
            InitLineRenderer(transform);
        }

        public void InitLineRenderer(Transform transform)
        {
            var lineRendererObject = new GameObject("Rope");
            lineRendererObject.transform.parent = transform.parent;
            hookRope = lineRendererObject.AddComponent<LineRenderer>();
            hookRope.material = ropeMaterial;
            hookRope.numCapVertices = 1;
            hookRope.startWidth = ropeRadius;
            hookRope.endWidth = ropeRadius;
            hookRope.positionCount = 0;

            var physicsLineObject = new GameObject("PhysicsRope");
            physicsLineObject.transform.parent = transform.parent;
            holdRope = physicsLineObject.AddComponent<LineRenderer>();
            holdRope.material = ropeMaterial;
            holdRope.numCapVertices = 1;
            holdRope.startWidth = ropeRadius;
            holdRope.endWidth = ropeRadius;
            holdRope.positionCount = 0;

            InitializeHoldRope();
        }

        public void DeleteRope()
        {
            GameObject.Destroy(holdRope.gameObject);
            GameObject.Destroy(hookRope.gameObject);
        }

        public void RopeUpdate()
        {
            // ropeState가 RopeState.Climbing 상황이 아니라면, 
            // ropeState가 Swinging일 경우 왼손으로 아니면 오른손으로 잡는 포인트를 지정
            // 스윙상태가 아닐때 로프를 잡는 손의 위치를 변경한다
            if (ropeState != RopeState.Climbing)
                currentRopeHoldPointTransform = ropeState == RopeState.Swinging ? ropeHoldTransformLeft : ropeHoldTransformRight;

            // hookRope의 점 개수가 0보다 크면, 로프가 그려질수 있는 상태라면
            if (hookRope.positionCount > 0)
            {
                // 현재 로프를 잡는위치와 hookPoint 사이의 거리를 hookRopeLength에 넣어라
                hookRopeLength = Vector3.Distance(currentRopeHoldPointTransform.position, hookPoint);
                // ropeState가 Swinging 또는 Climbing 중일 경우
                if (ropeState == RopeState.Swinging || ropeState == RopeState.Climbing)
                {
                    // 로프의 첫 번째 점을 현재잡는 위치로 설정 
                    hookRopePositions[0] = currentRopeHoldPointTransform.position;
                    // 로프의 두 번째 점을 로프가 몸체에 붙은 위치로 설정
                    hookRopePositions[1] = ropeAttachPointToHook.position;
                }
                else
                {
                    // SimulateHookRopePhysics을 호출
                    SimulateHookRopePhysics();
                }
                // 
                UpdateHookRopeLineRenderer();
            }

            // 로프를 잡고있는 상황이면
            if (holdRope.positionCount > 0)
            {
                // SimulateHoldRopePhysics 실행
                SimulateHoldRopePhysics();
                // UpdateHoldRopeLineRenderer 실행
                UpdateHoldRopeLineRenderer();
            }
        }

        // 로프 라인 렌더러 업데이트
        void UpdateHookRopeLineRenderer()
        {
            // hookRope에 라인 렌더러가 있다, 라인 렌더러로 hookRopePositions(Vector3 배열)값으로 선을 연결하라
            hookRope.SetPositions(hookRopePositions);
        }

        // 로프를 직선 상태로 설정
        public void SetHookRopeAsStraight()
        {
            // hookRope에 그릴 점의 개수는 2개
            hookRope.positionCount = 2;
            // 점 좌표를 저장할 배열의 크기를 2로 초기화
            hookRopePositions = new Vector3[2];
        }

        // 로프의 물리 시뮬레이션
        // 로프의 움직임(자연스러움) + 충돌 반응 + 거리 유지"를 모두 처리하는 핵심 메서드
        void SimulateHookRopePhysics()
        {
            // segmentLength는 로프 전체 길이를 점 개수 - 1로 나눈 값
            // 점들 사이 간격을 균일하게 유지하기 위한 기준 길이입니다.
            float segmentLength = hookRopeLength / (hookRopeResolution - 1);
            // hookRopePositions 배열의 내용을, hookRopePreviousPositions 배열로, hookRopeResolution 개수 만큼 복사
            // 현재 로프 점 위치를 이전 위치 배열에 복사하여, 다음 물리 계산에서 이전 위치 정보를 사용하기 위한 코드
            System.Array.Copy(hookRopePositions, hookRopePreviousPositions, hookRopeResolution);
            
            // hookRopeResolution의 양 끝 점을 제외한 만큼 반복 
            for (int i = 1; i < hookRopeResolution - 1; i++)
            {
                // hookRopeVelocities[i]점의 속도를 velocity에 저장
                // 점에 움직임을 계산하기 위해
                Vector3 velocity = hookRopeVelocities[i];
                // velocity에 (중력 * 시간간격)을 더해서 중력 가속도 적용, (기존 속도에 중력 영향을 누적시키는 것)
                // 중력 가속도를 적용해서 자연스러운 낙하를 위해
                velocity += Physics.gravity * Time.unscaledDeltaTime;

                // hookRopePositions[i] 위치를 중심으로 colliderRadius 반경 내의 모든 충돌체를 찾아서 hitColliders 배열에 저장
                // 로프 점이 주변 환경과 충돌하는지 검사 용도
                Collider[] hitColliders = Physics.OverlapSphere(hookRopePositions[i], colliderRadius);
                // 이번 프레임에 충돌이 발생했는지 체크하는 플래그
                bool collision = false;
                // 주변에 있는 모든 충돌체들을 한 개씩 차례대로 검사
                foreach (var hitCollider in hitColliders)
                {
                    // 플레이어 몸체는 충돌 검사에서 제외
                    // 로프가 플레이어 자신과 충돌하는 것을 방지하기 위해
                    if (hitCollider.gameObject != playerTransform.gameObject)
                    {
                        // hitCollider가 MeshCollider인지 체크
                        // MeshCollider를 포함해도 통과는 되기에
                        if (hitCollider is MeshCollider)
                        {
                            // MeshCollider로 변환
                            // hitCollider가 MeshCollider를 포함만 한 상태일수도 있으니
                            var c = hitCollider as MeshCollider;
                            // c가 MeshCollider의 convex형태가 아니라면 넘어가라
                            // convex형태가 Non-convex형태보다 정확성은 떨어지지만 자연스러운 동작을 위한 선택
                            if (!c.convex)
                                continue;
                        }
                        // hitCollider 표면에서 로프점 hookRopePositions[i]와 가장 가까운 점을 구함
                        Vector3 collisionPoint = hitCollider.ClosestPoint(hookRopePositions[i]);
                        // .normalized이라는 정규화를 걸쳐 표면점에서 로프점으로 향하는 방향, 방향을 찾기 위해
                        Vector3 normal = (hookRopePositions[i] - collisionPoint).normalized;

                        // Vector3.Reflect: 입사각=반사각 물리 법칙으로 반사 방향 계산
                        // (1f - dampening): 에너지 손실로 점점 느려지게 함
                        // 충돌 시 물리적 반사 계산 (감쇠로 점점 안정화)
                        velocity = Vector3.Reflect(velocity, normal) * (1f - dampening);
                        // collisionPoint(표면점) + normal(바깥방향) * colliderRadius(거리) = 충돌체에서 안전하게 떨어진 새로운 로프 위치
                        // 로프 점을 충돌 표면에서 바깥쪽으로 밀어내어 관통 방지
                        hookRopePositions[i] = collisionPoint + normal * colliderRadius;
                        collision = true;
                        break;
                    }
                }

                // 충돌이 없다면
                if (!collision)
                {
                    // 로프 위치 += 현재 속도 × 시간
                    // 충돌이 없어 위치를 이동 시켜 줘야 하니
                    hookRopePositions[i] += velocity * Time.unscaledDeltaTime;
                }

                // 속도를 줄이는
                // 없으면 무한히 움직임
                velocity *= 1f - dampening;
                // i의 속도 저장
                hookRopeVelocities[i] = velocity;
            }

            for (int iteration = 0; iteration < constraintIterations; iteration++)
            {
                for (int i = 0; i < hookRopeResolution - 1; i++)
                {
                    // 로프 i, 로프 i + 1 사이의 거리
                    Vector3 delta = hookRopePositions[i + 1] - hookRopePositions[i];
                    // delta 벡터의 길이, 즉 두 점 사이의 실제 거리
                    float currentDistance = delta.magnitude;
                    // 두 점 사이 거리가 segmentLength가 되도록 비율로 계산
                    float correction = (currentDistance - segmentLength) / currentDistance;
                    // 두 점 위치를 보정할 벡터 계산
                    Vector3 correctionVector = delta * correction;

                    if (i > 0)
                    {
                        // 두 점 사이 거리 보정: i점과 i+1점을 반대 방향으로 이동
                        hookRopePositions[i] += correctionVector;
                        hookRopePositions[i + 1] -= correctionVector;
                    }
                    else
                    {
                        // 첫 점(i=0)은 고정: i+1점만 이동, 이동량을 2배로 적용
                        hookRopePositions[i + 1] -= correctionVector * 2;
                    }
                }

                // 로프의 시작점은 항상 잡히는 위치에 고정시킨다
                hookRopePositions[0] = currentRopeHoldPointTransform.position;
                // 로프의 끝점은 항상 후크 위치에 고정시킨다
                hookRopePositions[hookRopeResolution - 1] = ropeAttachPointToHook.position;
            }

            for (int i = 1; i < hookRopeResolution - 1; i++)
            {
                // 로프가 단순한 뼈대처럼 각지지 않고, 실제 로프처럼 자연스럽게 곡선을 유지하도록 보정하는 smoothing 처리
                // Lerp(현재 위치, (왼쪽 위치 + 오른쪽 위치)/2, smoothingFactor)
                hookRopePositions[i] = Vector3.Lerp(hookRopePositions[i], (hookRopePositions[i - 1] + hookRopePositions[i + 1]) * 0.5f, smoothingFactor);
            }

            for (int i = 1; i < hookRopeResolution - 1; i++)
            {
                // velocity = (현재위치 - 이전위치) / 걸린시간;
                //로프 점의 현재 속도를 구해서 나중에 물리 시뮬레이션(중력 적용, 감속, 충돌 처리 등)에 쓰려고 기록하는 단계
                hookRopeVelocities[i] = (hookRopePositions[i] - hookRopePreviousPositions[i]) / Time.unscaledDeltaTime;
            }
        }

        // holdRopePositions 배열에 있는 점들을 이용해 LineRenderer에 선을 그리는 함수        
        void UpdateHoldRopeLineRenderer()
        {
            // LineRenderer(holdRope)에 holdRopePositions 배열의 좌표들을 설정
            holdRope.SetPositions(holdRopePositions);
        }

        // 
        void SimulateHoldRopePhysics()
        {
            // 현재 훅 지점과 플레이어 사이의 거리로 로프 길이 계산 (여유분 0.1 추가)
            holdRopelength = Vector3.Distance(currentRopeHoldPointTransform.position, ropeAttachTransformInBody.position) + .1f;
            // // 로프 전체 길이를 구간 수로 나누어 각 세그먼트 길이 계산, 각 segment별 길이 계산 
            float segmentLength = holdRopelength / (holdRopeResolution - 1);
            // 물리 계산 전 현재 위치를 이전 위치로 백업
            System.Array.Copy(holdRopePositions, holdRopePreviousPositions, holdRopeResolution);

            for (int i = 1; i < holdRopeResolution - 1; i++)
            {
                // 현재 로프 점의 속도 가져오기 (이후 중력, 충돌 등 적용 예정)
                Vector3 velocity = holdRopeVelocities[i];
                // 중력 가속도를 시간에 비례해서 속도에 누적 적용
                velocity += Physics.gravity * Time.unscaledDeltaTime;
                // 현재 속도로 로프 점 위치를 실제 이동 (위치 = 위치 + 속도×시간)
                holdRopePositions[i] += velocity * Time.unscaledDeltaTime;
            
                velocity *= 1f - dampening;
                holdRopeVelocities[i] = velocity;
            }

            for (int iteration = 0; iteration < constraintIterations; iteration++)
            {
                for (int i = 0; i < holdRopeResolution - 1; i++)
                {
                    Vector3 delta = holdRopePositions[i + 1] - holdRopePositions[i];
                    float currentDistance = delta.magnitude;
                    float correction = (currentDistance - segmentLength) / currentDistance;
                    Vector3 correctionVector = delta * correction * .9f;

                    if (i > 0)
                    {
                        holdRopePositions[i] += correctionVector;
                        holdRopePositions[i + 1] -= correctionVector;
                    }
                    else
                    {
                        holdRopePositions[i + 1] -= correctionVector * 2;
                    }
                }

                holdRopePositions[0] = ropeAttachTransformInBody.position;
                holdRopePositions[holdRopeResolution - 1] = hooked ? currentRopeHoldPointTransform.position : ropeAttachPointToHook.position;
            }

            for (int i = 1; i < holdRopeResolution - 1; i++)
            {
                holdRopePositions[i] = Vector3.Lerp(holdRopePositions[i], (holdRopePositions[i - 1] + holdRopePositions[i + 1]) * 0.5f, smoothingFactor);
            }

            for (int i = 1; i < holdRopeResolution - 1; i++)
            {
                holdRopeVelocities[i] = (holdRopePositions[i] - holdRopePreviousPositions[i]) / Time.unscaledDeltaTime;
            }
        }
        private void InitializeHoldRope()
        {
            holdRopePositions = new Vector3[holdRopeResolution];
            holdRopePreviousPositions = new Vector3[holdRopeResolution];
            holdRopeVelocities = new Vector3[holdRopeResolution];

            for (int i = 0; i < holdRopeResolution; i++)
            {
                float t = (float)i / (holdRopeResolution - 1);
                holdRopePositions[i] = Vector3.Lerp(ropeAttachTransformInBody.position, currentRopeHoldPointTransform.position, t);
                holdRopePreviousPositions[i] = holdRopePositions[i];
                holdRopeVelocities[i] = Vector3.zero;
            }

            holdRope.positionCount = holdRopeResolution;
        }



        public void StartThrow(Transform startPoint, Vector3 endPoint)
        {
            currentRopeHoldPointTransform = startPoint;
            hookRopeLength = Vector3.Distance(startPoint.position, endPoint);
            hookPoint = endPoint;
            InitializeRope();
            initialRopeLength = 0;
            targetRopeLength = hookRopeLength;
            ThrowRopeCoroutine();
        }

        private void InitializeRope()
        {
            hookRopePositions = new Vector3[hookRopeResolution];
            hookRopePreviousPositions = new Vector3[hookRopeResolution];
            hookRopeVelocities = new Vector3[hookRopeResolution];

            for (int i = 0; i < hookRopeResolution; i++)
            {
                float t = (float)i / (hookRopeResolution - 1);
                hookRopePositions[i] = Vector3.Lerp(currentRopeHoldPointTransform.position, hookPoint, t);
                hookRopePreviousPositions[i] = hookRopePositions[i];
                hookRopeVelocities[i] = Vector3.zero;
            }
            hookRope.positionCount = hookRopeResolution;
        }

        private async void ThrowRopeCoroutine()
        {
            hooked = true;
            float timeElapsed = 0f;
            SetRopeState(RopeState.Throwing);
            Vector3 startPos = currentRopeHoldPointTransform.position;
            var hookPos = hookPoint;
            if (hookObject != null)
            {
                hookObject.parent = null;
            }
            while (timeElapsed < throwDuration && !HasReachedTarget())
            {
                timeElapsed += Time.unscaledDeltaTime;
                float t = timeElapsed / throwDuration;
                Vector3 currentPos = CalculateParabolicPath(startPos, hookPos, throwHeight, t);
                hookPoint = currentPos;
                if (hookObject != null)
                    hookObject.position = hookPoint;

                AdjustRopeLength(t);
                await Task.Yield();
            }
            if(ropeState == RopeState.Throwing)
                SetRopeState(RopeState.Normal);
            hookPoint = hookPos;
        }

        Vector3 CalculateParabolicPath(Vector3 start, Vector3 end, float height, float t)
        {
            t = Mathf.Clamp01(t);

            Vector3 linearPos = Vector3.Lerp(start, end, t);

            float arc = height * 4 * (t - t * t);

            return new Vector3(linearPos.x, linearPos.y + arc, linearPos.z);
        }

        private void AdjustRopeLength(float t)
        {
            hookRopeLength = Mathf.Lerp(initialRopeLength, targetRopeLength, t);
        }




        public void ResetRope()
        {
            hooked = false;
            if (hookObject != null)
            {
                hookObject.parent = defaultHookObjectParent;
                hookObject.localPosition = defaultHookPos;
                hookObject.localRotation = defaultHookRot;
            }
            hookRope.positionCount = 0;
            hookRopePositions = new Vector3[0];

            hookRopePositions = new Vector3[0];
            hookRopePreviousPositions = new Vector3[0];
            hookRopeVelocities = new Vector3[0];
            SetRopeState(RopeState.Normal);
        }

        public bool HasReachedTarget()
        {
            return ropeState != RopeState.Throwing;
        }

        public void SetRopeState(RopeState newState)
        {
            ropeState = newState;
        }
    }

}
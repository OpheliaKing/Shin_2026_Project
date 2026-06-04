using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Shin
{
    public partial class CharacterBase
    {
        protected Vector2 IntendedMoveDirection { get; private set; }
        protected Vector3 IntendedLookDirection { get; private set; }

        [SerializeField, Min(0f)]
        private float _rotationLerpSpeed = 14f;

        [SerializeField]
        [Tooltip("수평 이동 시 막는 레이어. Ground 레이어는 Init 시 자동 제외됩니다.")]
        private LayerMask _movementObstructionMask = ~0;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("이 값보다 위를 향한 표면(바닥)은 수평 이동 막힘 판정에서 제외합니다.")]
        private float _floorNormalThreshold = 0.35f;

        [SerializeField, Min(0f)]
        [Tooltip("수평 이동 검사용 캡슐 캐스트 바닥을 올립니다. MeshCollider 바닥에 걸려 이동이 막힐 때 조절합니다.")]
        private float _obstructionCastBottomLift = 0.2f;

        [SerializeField, Min(0f)]
        [Tooltip("발 높이 근처에서 맞은 충돌은 바닥으로 간주합니다. (MeshCollider 삼각형 노멀 보정)")]
        private float _floorContactHeightTolerance = 0.25f;

        [SerializeField]
        private SerializedDictionary<MOVEMENT_STATE, float> _movementSpeed = new SerializedDictionary<MOVEMENT_STATE, float>
        {
            { MOVEMENT_STATE.WALK, 5f },
            { MOVEMENT_STATE.RUN, 7f },
            { MOVEMENT_STATE.DASH, 10f },
        };

        private MOVEMENT_STATE _movementState = MOVEMENT_STATE.WALK;
        private Rigidbody _rigidbody;
        private CapsuleCollider _movementCapsule;
        private Vector3 _requestedWorldVelocity;

        partial void InitMovement()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _movementCapsule = GetComponent<CapsuleCollider>();
            ApplyMovementObstructionMaskExcludingGround();

            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void ApplyMovementObstructionMaskExcludingGround()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                return;
            }

            _movementObstructionMask &= ~(1 << groundLayer);
        }

        private void FixedUpdate()
        {
            ApplyRequestedMovement();
            ApplyAINavMeshPositionConstraint();
            ApplyPendingRotation();
        }

        partial void ApplyAINavMeshPositionConstraint();

        /// <summary>
        /// 월드 XZ 평면상의 이동 방향(x, z)을 받아 이동 요청만 갱신합니다. 위치/회전 적용은 <see cref="FixedUpdate"/>에서 처리합니다.
        /// </summary>
        public void Move(Vector2 worldHorizontalDirection)
        {
            if (!CharacterState.IsMoveAble())
            {
                _requestedWorldVelocity = Vector3.zero;
                return;
            }

            if (worldHorizontalDirection.sqrMagnitude < 1e-8f)
            {
                _requestedWorldVelocity = Vector3.zero;
                ChangeCharacterState(CHARACTER_STATE.IDLE);
                return;
            }

            ChangeCharacterState(CHARACTER_STATE.MOVE);
            Vector3 moveDirection = new Vector3(worldHorizontalDirection.x, 0f, worldHorizontalDirection.y).normalized;
            _requestedWorldVelocity = moveDirection * GetMovementSpeed();
        }

        private void ApplyRequestedMovement()
        {
            if (_requestedWorldVelocity.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Vector3 delta = _requestedWorldVelocity * Time.fixedDeltaTime;

            if (_rigidbody != null)
            {
                Vector3 resolvedDelta = CharacterAIState == CHARACTER_AI_STATE.AI
                    ? ResolveAIDelta(delta)
                    : ResolveMovementDelta(delta);
                Vector3 nextPosition = _rigidbody.position + resolvedDelta;
                _rigidbody.MovePosition(nextPosition);
            }
            else
            {
                transform.position += delta;
            }
        }

        private void ApplyPendingRotation()
        {
            Vector3 lookDirection = IntendedLookDirection;
            if (lookDirection.sqrMagnitude < 1e-8f && _requestedWorldVelocity.sqrMagnitude >= 1e-8f)
            {
                lookDirection = _requestedWorldVelocity.normalized;
            }

            if (lookDirection.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float t = Mathf.Clamp01(Time.fixedDeltaTime * _rotationLerpSpeed);
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, t);

            if (_rigidbody != null)
            {
                _rigidbody.MoveRotation(newRotation);
            }
            else
            {
                transform.rotation = newRotation;
            }
        }

        private Vector3 ResolveMovementDelta(Vector3 delta)
        {
            if (delta.sqrMagnitude < 1e-8f)
            {
                return delta;
            }

            if (!TryGetObstructionCastParameters(out Vector3 bottom, out Vector3 top, out float radius, out float feetY))
            {
                return delta;
            }

            const float skin = 0.02f;
            Vector3 direction = delta.normalized;
            float distance = delta.magnitude;

            if (!TryGetNearestObstructionHit(bottom, top, radius, feetY, direction, distance + skin, out RaycastHit hit))
            {
                return delta;
            }

            Vector3 safeMove = direction * Mathf.Max(0f, hit.distance - skin);
            Vector3 remaining = delta - safeMove;
            if (remaining.sqrMagnitude < 1e-8f)
            {
                return safeMove;
            }

            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            slide.y = 0f;
            if (slide.sqrMagnitude < 1e-8f)
            {
                return safeMove;
            }

            slide = slide.normalized * slide.magnitude;
            Vector3 slideOriginBottom = bottom + safeMove;
            Vector3 slideOriginTop = top + safeMove;

            if (TryGetNearestObstructionHit(
                    slideOriginBottom,
                    slideOriginTop,
                    radius,
                    feetY,
                    slide.normalized,
                    slide.magnitude + skin,
                    out RaycastHit slideHit))
            {
                slide = slide.normalized * Mathf.Max(0f, slideHit.distance - skin);
            }

            return safeMove + slide;
        }

        private bool TryGetNearestObstructionHit(
            Vector3 bottom,
            Vector3 top,
            float radius,
            float feetY,
            Vector3 direction,
            float maxDistance,
            out RaycastHit nearestHit)
        {
            nearestHit = default;
            float nearestDistance = float.MaxValue;
            bool found = false;

            RaycastHit[] hits = Physics.CapsuleCastAll(
                bottom,
                top,
                radius,
                direction,
                maxDistance,
                _movementObstructionMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsMovementObstructionHit(hits[i], feetY))
                {
                    continue;
                }

                if (hits[i].distance < nearestDistance)
                {
                    nearestDistance = hits[i].distance;
                    nearestHit = hits[i];
                    found = true;
                }
            }

            return found;
        }

        private bool IsMovementObstructionHit(RaycastHit hit, float feetY)
        {
            if (hit.collider == null)
            {
                return false;
            }

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                return false;
            }

            if (IsFloorContact(hit, feetY))
            {
                return false;
            }

            return true;
        }

        private bool IsFloorContact(RaycastHit hit, float feetY)
        {
            if (hit.normal.y > _floorNormalThreshold)
            {
                return true;
            }

            if (hit.point.y <= feetY + _floorContactHeightTolerance && hit.normal.y > 0.15f)
            {
                return true;
            }

            if (hit.distance <= 0.01f && hit.normal.y >= 0f)
            {
                return true;
            }

            return false;
        }

        private bool TryGetObstructionCastParameters(out Vector3 bottom, out Vector3 top, out float radius, out float feetY)
        {
            bottom = default;
            top = default;
            radius = 0f;
            feetY = 0f;

            CapsuleCollider capsule = _movementCapsule;
            if (capsule == null)
            {
                return false;
            }

            Transform capsuleTransform = capsule.transform;
            Vector3 center = capsuleTransform.TransformPoint(capsule.center);
            float scaledRadius = capsule.radius * Mathf.Max(capsuleTransform.lossyScale.x, capsuleTransform.lossyScale.z);
            float scaledHeight = capsule.height * capsuleTransform.lossyScale.y;
            float halfHeight = Mathf.Max(scaledHeight * 0.5f - scaledRadius, 0f);

            Vector3 up = capsuleTransform.up;
            Vector3 feet = center - up * halfHeight;
            feetY = feet.y;
            bottom = feet + up * _obstructionCastBottomLift;
            top = center + up * halfHeight;

            float minCapsuleAxis = scaledRadius * 2f + 0.05f;
            if (Vector3.Distance(bottom, top) < minCapsuleAxis)
            {
                top = bottom + up * minCapsuleAxis;
            }

            radius = scaledRadius;
            return true;
        }

        /// <summary>
        /// 애니메이션용 입력 방향(-1~1). (플레이어: 카메라 기준 입력, AI: 자신이 정의한 입력 축)
        /// </summary>
        protected void SetIntendedMoveDirection(Vector2 inputDirection)
        {
            IntendedMoveDirection = Vector2.ClampMagnitude(inputDirection, 1f);
        }

        /// <summary>
        /// 회전용 바라볼 방향(월드). y는 무시하고 XZ로만 처리합니다.
        /// </summary>
        protected void SetIntendedLookDirection(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            IntendedLookDirection = worldDirection.sqrMagnitude < 1e-8f ? Vector3.zero : worldDirection.normalized;
        }

        /// <summary>
        /// look 입력량에 따라 지정 축 기준으로 Transform을 회전시킵니다.
        /// </summary>
        protected void RotateByLookInput(Transform target, float inputDelta, Vector3 rotationAxis, float degreesPerInputUnit, Space relativeTo = Space.World)
        {
            Transform rotateTarget = target != null ? target : transform;
            if (Mathf.Abs(inputDelta) < 1e-8f || rotationAxis.sqrMagnitude < 1e-8f)
            {
                return;
            }

            rotateTarget.Rotate(rotationAxis.normalized, inputDelta * degreesPerInputUnit, relativeTo);
        }

        public void SetMovementState(MOVEMENT_STATE state)
        {
            _movementState = state;
        }

        public float GetMovementSpeed()
        {
            if (_movementSpeed.TryGetValue(_movementState, out float speed))
            {
                return speed;
            }
            return 0;
        }
    }

    public enum MOVEMENT_STATE
    {
        WALK,
        RUN,
        DASH,
    }
}

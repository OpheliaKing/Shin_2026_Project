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
        private SerializedDictionary<MOVEMENT_STATE, float> _movementSpeed = new SerializedDictionary<MOVEMENT_STATE, float>
        {
            { MOVEMENT_STATE.WALK, 5f },
            { MOVEMENT_STATE.RUN, 7f },
            { MOVEMENT_STATE.DASH, 10f },
        };

        private MOVEMENT_STATE _movementState = MOVEMENT_STATE.WALK;

        /// <summary>
        /// 월드 XZ 평면상의 이동 방향(x, z)을 받아 캐릭터 위치를 갱신합니다.
        /// </summary>
        public void Move(Vector2 worldHorizontalDirection)
        {
            if (!CharacterState.IsMoveAble())
            {
                return;
            }

            if (worldHorizontalDirection.sqrMagnitude < 1e-8f)
            {
                ChangeCharacterState(CHARACTER_STATE.IDLE);
                return;
            }

            ChangeCharacterState(CHARACTER_STATE.MOVE);
            Vector3 moveDirection = new Vector3(worldHorizontalDirection.x, 0f, worldHorizontalDirection.y).normalized;
            transform.position += moveDirection * GetMovementSpeed() * Time.deltaTime;
            RotateTowards(GetCurrentLookDirectionOr(moveDirection));
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

        private Vector3 GetCurrentLookDirectionOr(Vector3 fallbackWorldMoveDirection)
        {
            return IntendedLookDirection.sqrMagnitude < 1e-8f ? fallbackWorldMoveDirection : IntendedLookDirection;
        }

        private void RotateTowards(Vector3 worldMoveDirection)
        {
            if (worldMoveDirection.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection, Vector3.up);
            float t = Mathf.Clamp01(Time.deltaTime * _rotationLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        /// <summary>
        /// look 입력량에 따라 지정 축 기준으로 Transform을 회전시킵니다.
        /// </summary>
        /// <param name="target">회전 대상. null이면 this.transform.</param>
        /// <param name="inputDelta">해당 축에 매핑된 입력량(예: 마우스 X → input.x).</param>
        /// <param name="rotationAxis">회전 축(월드/로컬은 <paramref name="relativeTo"/> 참고).</param>
        /// <param name="degreesPerInputUnit">입력 1당 회전 각도(도).</param>
        /// <param name="relativeTo">회전 기준 좌표계.</param>
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

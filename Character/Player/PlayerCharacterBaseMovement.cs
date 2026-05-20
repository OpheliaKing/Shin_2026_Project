using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase
    {
        private Vector2 _moveInput;

        public void MoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        /// <summary>
        /// 플레이어 입력(좌우·전후)을 카메라가 보는 방향 기준으로 바꾼 뒤 <see cref="Move"/>로 넘깁니다.
        /// </summary>
        public void MoveFromCameraRelativeInput(Vector2 input)
        {
            // 애니메이션은 입력 축 기준(전/후/좌/우)으로 처리
            SetIntendedMoveDirection(input);

            // 회전은 항상 카메라가 보는 방향(Yaw) 기준
            if (input.sqrMagnitude >= 1e-8f && CurrentFocusCamera != null)
            {
                Vector3 cameraForward = CurrentFocusCamera.transform.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude >= 1e-8f)
                {
                    SetIntendedLookDirection(cameraForward.normalized);
                }
            }

            Vector2 worldMoveDirection = ToWorldHorizontalFromCameraRelativeInput(input);
            Move(worldMoveDirection);
        }

        private Vector2 ToWorldHorizontalFromCameraRelativeInput(Vector2 input)
        {
            if (input.sqrMagnitude < 1e-8f)
            {
                return Vector2.zero;
            }

            if (CurrentFocusCamera != null)
            {
                Vector3 cameraForward = CurrentFocusCamera.transform.forward;
                Vector3 cameraRight = CurrentFocusCamera.transform.right;
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();

                Vector3 moveDirection = (cameraRight * input.x + cameraForward * input.y).normalized;
                return new Vector2(moveDirection.x, moveDirection.z);
            }

            Vector3 worldAxis = new Vector3(input.x, 0f, input.y).normalized;
            return new Vector2(worldAxis.x, worldAxis.z);
        }

        public void Shift_Left_Input(float value)
        {
            Debug.Log("Shift_Left_Input: " + value);

            MOVEMENT_STATE state = value > 0f ? MOVEMENT_STATE.RUN : MOVEMENT_STATE.WALK;

            SetMovementState(state);
        }
    }
}

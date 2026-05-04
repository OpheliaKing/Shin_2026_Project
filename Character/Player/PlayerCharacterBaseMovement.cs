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
            Move(ToWorldHorizontalFromCameraRelativeInput(input));
        }

        private Vector2 ToWorldHorizontalFromCameraRelativeInput(Vector2 input)
        {
            if (input.sqrMagnitude < 1e-8f)
            {
                return Vector2.zero;
            }

            if (_camera != null)
            {
                Vector3 cameraForward = _camera.transform.forward;
                Vector3 cameraRight = _camera.transform.right;
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
    }
}

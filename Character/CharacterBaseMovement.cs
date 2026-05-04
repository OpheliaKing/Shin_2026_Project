using UnityEngine;

namespace Shin
{
    public partial class CharacterBase
    {
        [SerializeField] private float _moveSpeed = 5f;

        /// <summary>
        /// 월드 XZ 평면상의 이동 방향(x, z)을 받아 캐릭터 위치를 갱신합니다.
        /// </summary>
        public void Move(Vector2 worldHorizontalDirection)
        {
            if (worldHorizontalDirection.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Vector3 moveDirection = new Vector3(worldHorizontalDirection.x, 0f, worldHorizontalDirection.y).normalized;
            transform.position += moveDirection * _moveSpeed * Time.deltaTime;
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

            if (_cameraTransform != null)
            {
                Vector3 cameraForward = _cameraTransform.forward;
                Vector3 cameraRight = _cameraTransform.right;
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

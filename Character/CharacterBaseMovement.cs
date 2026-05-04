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
    }
}

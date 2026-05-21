using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using Unity.Cinemachine;



namespace Shin
{
    public class PlayerCamera : MonoBehaviour
    {
        private Transform _pivot;
        public Transform Pivot
        {
            get
            {
                if (_pivot == null)
                {
                    switch (_pivotType)
                    {
                        case PLAYER_CAMERA_ROTATE_TYPE.ONESELF:
                            _pivot = transform;
                            break;
                        case PLAYER_CAMERA_ROTATE_TYPE.PARENT:
                            _pivot = transform.parent;
                            break;
                    }
                }
                return _pivot;
            }
        }
        [SerializeField] private PLAYER_CAMERA_ROTATE_TYPE _pivotType = PLAYER_CAMERA_ROTATE_TYPE.ONESELF;
        [SerializeField] private float _horizontalSensitivity = 0.15f;
        [SerializeField] private float _verticalSensitivity = 0.15f;
        [SerializeField, Range(-89f, 89f)] private float _minPitch = -80f;
        [SerializeField, Range(-89f, 89f)] private float _maxPitch = 80f;

        private float _pitchDegrees;
        private float _yawDegrees;

        private void Awake()
        {
            Transform pivot = Pivot;
            if (pivot != null)
            {
                Vector3 e = pivot.eulerAngles;
                _pitchDegrees = NormalizeEulerDegrees(e.x);
                _yawDegrees = NormalizeEulerDegrees(e.y);
            }
            else
            {
                Vector3 euler = transform.localEulerAngles;
                _pitchDegrees = NormalizeEulerDegrees(euler.x);
            }
        }

        /// <summary>
        /// 수평: 피봇이 월드 Y 기준 회전(Yaw). 수직: 피봇이 Yaw 이후 오른쪽 축 기준 회전(Pitch). 카메라 객체 로컬 X는 건드리지 않음.
        /// </summary>
        public void MoveCamera(Vector2 lookDelta)
        {
            if (lookDelta.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Transform pivot = Pivot;
            _pitchDegrees -= lookDelta.y * _verticalSensitivity;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees, _minPitch, _maxPitch);

            if (pivot != null)
            {
                _yawDegrees += lookDelta.x * _horizontalSensitivity;
                Quaternion yawQ = Quaternion.AngleAxis(_yawDegrees, Vector3.up);
                Vector3 rightAxis = yawQ * Vector3.right;
                Quaternion pitchQ = Quaternion.AngleAxis(_pitchDegrees, rightAxis);
                pivot.rotation = pitchQ * yawQ;
            }
            else
            {
                transform.localRotation = Quaternion.Euler(_pitchDegrees, transform.localEulerAngles.y, transform.localEulerAngles.z);
            }
        }

        private static float NormalizeEulerDegrees(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }

        public PLAYER_CAMERA_ROTATE_TYPE GetPivotType()
        {
            return _pivotType;
        }

        public float HorizontalSensitivity => _horizontalSensitivity;
        public float VerticalSensitivity => _verticalSensitivity;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float PitchDegrees => _pitchDegrees;

        /// <summary>수직 시선 각도(도). <see cref="_pitchDegrees"/>와 동일합니다.</summary>
        public float RotateX => _pitchDegrees;

        /// <summary>캐릭터 애니메이션용 수직 시선만 갱신합니다(피봇 회전 없음).</summary>
        public void ApplyVerticalLookInput(float lookDeltaY)
        {
            if (Mathf.Abs(lookDeltaY) < 1e-8f)
            {
                return;
            }

            _pitchDegrees -= lookDeltaY * _verticalSensitivity;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees, _minPitch, _maxPitch);
        }

        /// <summary>
        /// <see cref="RotateX"/>를 UpperY로 변환합니다. 90° → -1, -90° → 1, 0° → 0.
        /// </summary>
        public float GetUpperYAnimationValue()
        {
            return PitchDegreesToUpperY(_pitchDegrees);
        }

        public static float PitchDegreesToUpperY(float pitchDegrees, float verticalLimitDegrees = 90f)
        {
            if (Mathf.Approximately(verticalLimitDegrees, 0f))
            {
                return 0f;
            }

            return Mathf.Clamp(-pitchDegrees / verticalLimitDegrees, -1f, 1f);
        }
    }
}

public enum PLAYER_CAMERA_TYPE
{
    DEFAULT,
    SHOOT_ZOOM
}

public enum PLAYER_CAMERA_ROTATE_TYPE
{
    NONE,
    ONESELF,
    PARENT,
    CHARACTER,
}
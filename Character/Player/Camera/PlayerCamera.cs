using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using Unity.Cinemachine;
using System;



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

        [SerializeField] private PlayerCameraRotateCharacterData _rotateCharacterData;
        public PlayerCameraRotateCharacterData RotateCharacterData
        {
            get
            {
                return _rotateCharacterData;
            }
        }

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
        /// 수평 입력 → Yaw(Y), 수직 입력 → Pitch(X).
        /// </summary>
        public void MoveCamera(Vector2 lookDelta)
        {
            if (lookDelta.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Transform pivot = Pivot;
            if (pivot != null)
            {
                ApplyRotationToPivot(pivot, lookDelta);
            }
            else
            {
                ApplyRotationToPivot(transform, lookDelta);
            }
        }

        /// <summary>
        /// <see cref="PlayerCameraRotateCharacterData.RotateParent"/>가 true이면 부모 Transform을 피봇으로 회전합니다.
        /// </summary>
        public void MoveCamera(Vector2 lookDelta, PlayerCameraRotateCharacterData rotateData)
        {
            Vector2 filteredLookDelta = FilterLookDelta(lookDelta, rotateData);
            if (filteredLookDelta.sqrMagnitude < 1e-8f)
            {
                return;
            }

            if (rotateData.RotateParent)
            {
                Transform parent = transform.parent;
                if (parent == null)
                {
                    return;
                }

                ApplyRotationToPivot(parent, filteredLookDelta);
                return;
            }

            if (!rotateData.RotateCameraX && !rotateData.RotateCameraY)
            {
                return;
            }

            Transform pivot = Pivot;
            if (pivot != null)
            {
                ApplyRotationToPivot(pivot, filteredLookDelta);
            }
            else
            {
                ApplyRotationToPivot(transform, filteredLookDelta);
            }
        }

        /// <summary>수평 입력(x) → Yaw(Y), 수직 입력(y) → Pitch(X).</summary>
        private void ApplyRotationToPivot(Transform pivot, Vector2 lookDelta)
        {
            SyncRotationStateFromPivot(pivot);

            _pitchDegrees -= lookDelta.y * _verticalSensitivity;
            _pitchDegrees = Mathf.Clamp(_pitchDegrees, _minPitch, _maxPitch);
            _yawDegrees += lookDelta.x * _horizontalSensitivity;

            Quaternion yawQ = Quaternion.AngleAxis(_yawDegrees, Vector3.up);
            Vector3 rightAxis = yawQ * Vector3.right;
            Quaternion pitchQ = Quaternion.AngleAxis(_pitchDegrees, rightAxis);
            pivot.rotation = pitchQ * yawQ;
        }

        private void SyncRotationStateFromPivot(Transform pivot)
        {
            Vector3 e = pivot.eulerAngles;
            _pitchDegrees = NormalizeEulerDegrees(e.x);
            _yawDegrees = NormalizeEulerDegrees(e.y);
        }

        private static Vector2 FilterLookDelta(Vector2 lookDelta, PlayerCameraRotateCharacterData rotateData)
        {
            bool parentOnly = rotateData.RotateParent && !rotateData.RotateCameraX && !rotateData.RotateCameraY;

            if (rotateData.RotateParent)
            {
                return new Vector2(
                    rotateData.RotateCameraX || parentOnly ? lookDelta.x : 0f,
                    rotateData.RotateCameraY || parentOnly ? lookDelta.y : 0f);
            }

            return new Vector2(
                rotateData.RotateCameraX ? lookDelta.x : 0f,
                rotateData.RotateCameraY ? lookDelta.y : 0f);
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

[Serializable]
public struct PlayerCameraRotateCharacterData
{
    public bool RotateCameraX;
    public bool RotateCameraY;
    public bool RotateParent;

    public bool IsEmpty()
    {
        return !RotateCameraX && !RotateCameraY && !RotateParent;
    }
}

public enum PLAYER_CAMERA_ROTATE_TYPE_CHARACTER
{

}
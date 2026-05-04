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
                    _pivot = transform.parent;
                }
                return _pivot;
            }
        }
        [SerializeField] private float _horizontalSensitivity = 0.15f;
        [SerializeField] private float _verticalSensitivity = 0.15f;
        [SerializeField, Range(-89f, 89f)] private float _minPitch = -80f;
        [SerializeField, Range(-89f, 89f)] private float _maxPitch = 80f;

        private float _pitchDegrees;
        private float _yawDegrees;


        #region CinemachineCamera

        [SerializeField]
        private SerializedDictionary<PLAYER_CAMERA_TYPE, CinemachineCamera> _cinemachineCameraSettings;

        #endregion

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
    }
}

public enum PLAYER_CAMERA_TYPE
{
    DEFAULT,
    SHOOT_ZOOM
}



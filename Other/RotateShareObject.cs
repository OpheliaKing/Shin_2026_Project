using UnityEngine;

namespace Shin
{
    public class RotateShareObject : MonoBehaviour
    {
        [SerializeField]
        private Transform _target;

        [SerializeField]
        private ROTATION_SHARE_AXIS _axis = ROTATION_SHARE_AXIS.Y;

        [SerializeField]
        private ROTATION_COORDINATE_SPACE _coordinateSpace = ROTATION_COORDINATE_SPACE.Local;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public ROTATION_SHARE_AXIS Axis
        {
            get => _axis;
            set => _axis = value;
        }

        /// <summary>회전 적용·조회 시 사용할 좌표계(로컬 / 월드).</summary>
        public ROTATION_COORDINATE_SPACE CoordinateSpace
        {
            get => _coordinateSpace;
            set => _coordinateSpace = value;
        }

        /// <summary>인스펙터에 설정된 <see cref="CoordinateSpace"/> 기준 선택 축 회전 각도(도).</summary>
        public float GetAxisRotateDegrees()
        {
            return GetAxisRotateDegrees(_coordinateSpace);
        }

        /// <summary>지정 좌표계 기준 선택 축 회전 각도(도).</summary>
        public float GetAxisRotateDegrees(ROTATION_COORDINATE_SPACE coordinateSpace)
        {
            Transform target = GetTargetOrDefault();
            Vector3 euler = coordinateSpace == ROTATION_COORDINATE_SPACE.Local
                ? target.localEulerAngles
                : target.eulerAngles;

            return NormalizeEulerDegrees(GetAxisComponent(euler, _axis));
        }

        public void AddRotation(float degrees)
        {
            ApplyRotationDelta(degrees, _coordinateSpace);
        }

        public void AddRotation(float degrees, ROTATION_COORDINATE_SPACE coordinateSpace)
        {
            ApplyRotationDelta(degrees, coordinateSpace);
        }

        public void SubtractRotation(float degrees)
        {
            ApplyRotationDelta(-degrees, _coordinateSpace);
        }

        public void SubtractRotation(float degrees, ROTATION_COORDINATE_SPACE coordinateSpace)
        {
            ApplyRotationDelta(-degrees, coordinateSpace);
        }

        public void ApplyRotationDelta(float deltaDegrees)
        {
            ApplyRotationDelta(deltaDegrees, _coordinateSpace);
        }

        public void ApplyRotationDelta(float deltaDegrees, ROTATION_COORDINATE_SPACE coordinateSpace)
        {
            if (Mathf.Approximately(deltaDegrees, 0f))
            {
                return;
            }

            Transform target = GetTargetOrDefault();
            Vector3 axis = GetAxisVector(_axis);
            target.Rotate(axis, deltaDegrees, ToUnitySpace(coordinateSpace));
        }

        private Transform GetTargetOrDefault()
        {
            return _target != null ? _target : transform;
        }

        private static Space ToUnitySpace(ROTATION_COORDINATE_SPACE coordinateSpace)
        {
            return coordinateSpace == ROTATION_COORDINATE_SPACE.Local
                ? Space.Self
                : Space.World;
        }

        private static Vector3 GetAxisVector(ROTATION_SHARE_AXIS axis)
        {
            return axis switch
            {
                ROTATION_SHARE_AXIS.X => Vector3.right,
                ROTATION_SHARE_AXIS.Y => Vector3.up,
                ROTATION_SHARE_AXIS.Z => Vector3.forward,
                _ => Vector3.up,
            };
        }

        private static float GetAxisComponent(Vector3 eulerAngles, ROTATION_SHARE_AXIS axis)
        {
            return axis switch
            {
                ROTATION_SHARE_AXIS.X => eulerAngles.x,
                ROTATION_SHARE_AXIS.Y => eulerAngles.y,
                ROTATION_SHARE_AXIS.Z => eulerAngles.z,
                _ => eulerAngles.y,
            };
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

    public enum ROTATION_SHARE_AXIS
    {
        X,
        Y,
        Z,
    }

    public enum ROTATION_COORDINATE_SPACE
    {
        Local,
        World,
    }
}

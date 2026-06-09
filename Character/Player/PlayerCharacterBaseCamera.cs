using UnityEngine;
using AYellowpaper.SerializedCollections;
using Unity.Cinemachine;

namespace Shin
{
    public partial class PlayerCharacterBase
    {
        #region CinemachineCamera

        [SerializeField]
        private SerializedDictionary<PLAYER_CAMERA_TYPE, CinemachineCamera> _cinemachineCameraSettings;
        private PlayerCamera _defaultFocusCamera;
        protected PlayerCamera DefaultFocusCamera
        {
            get
            {
                if( _defaultFocusCamera == null)
                {
                    _defaultFocusCamera = Camera.main.GetComponent<PlayerCamera>();
                }
                return _defaultFocusCamera;
            }
        }


        #endregion


        private PlayerCamera _currentFocusCamera;

        public PlayerCamera CurrentFocusCamera
        {
            get
            {
                if (_currentFocusCamera == null)
                {
                    _currentFocusCamera = GetComponentInChildren<PlayerCamera>();
                }
                return _currentFocusCamera;
            }
        }

        private void CameraInit()
        {
            _currentFocusCamera = DefaultFocusCamera;
            if (_currentFocusCamera == null)
            {
                Debug.Log("Not Found PlayerCamera!!!");
            }

            var defaultCameraParent = GetComponentInChildren<TargetFollowObject>();
            if (defaultCameraParent == null)
            {
                Debug.Log("Not Found TargetFollowObject!!!");
            }

            defaultCameraParent.SetTarget(transform);
            defaultCameraParent.transform.parent = null;
        }

        public void MoveCamera(Vector2 input)
        {
            if (!IsPlayerInputAllowed)
            {
                return;
            }

            if (CurrentFocusCamera != null)
            {
                if (CurrentFocusCamera.GetPivotType() == PLAYER_CAMERA_ROTATE_TYPE.CHARACTER)
                {
                    // 캐릭터 Y축 회전 (상하 기울임은 애니메이션 처리, 카메라는 고정)
                    RotateByLookInput(
                        transform,
                        input.x,
                        Vector3.up,
                        CurrentFocusCamera.HorizontalSensitivity);

                    CurrentFocusCamera.ApplyVerticalLookInput(input.y);
                    UpdateUpperYAnimationFromCameraPitch();

                    MoveFocusCameraByRotateCharacterData(input);
                }
                else
                {
                    CurrentFocusCamera.MoveCamera(input);
                }
            }
        }

        public void ActiveCamera(PLAYER_CAMERA_TYPE cameraType, bool isActive)
        {
            if (_cinemachineCameraSettings.TryGetValue(cameraType, out CinemachineCamera camera))
            {
                camera.gameObject.SetActive(isActive);

                if (isActive)
                {
                    PlayerCamera playerCamera = camera.GetComponent<PlayerCamera>();
                    if (playerCamera != null)
                    {
                        SetCurrentFocusCamera(playerCamera);
                    }
                }
                else
                {
                    SetCurrentFocusCamera(DefaultFocusCamera);
                }
            }
        }

        public void SetCurrentFocusCamera(PlayerCamera camera)
        {
            _currentFocusCamera = camera;
        }

        private void UpdateUpperYAnimationFromCameraPitch()
        {
            if (CurrentFocusCamera == null)
            {
                return;
            }

            SetAnimatorFloat(ANIM_PARAM_UPPER_Y, CurrentFocusCamera.GetUpperYAnimationValue());
        }

        private void MoveFocusCameraByRotateCharacterData(Vector2 input)
        {
            if (CurrentFocusCamera == null)
            {
                return;
            }

            PlayerCameraRotateCharacterData rotateData = CurrentFocusCamera.RotateCharacterData;
            if (rotateData.IsEmpty())
            {
                return;
            }

            CurrentFocusCamera.MoveCamera(input, rotateData);
        }
    }
}

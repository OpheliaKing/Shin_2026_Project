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

        #endregion

        
        private PlayerCamera _camera;

        public PlayerCamera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = GetComponentInChildren<PlayerCamera>();
                }
                return _camera;
            }
        }

        private void CameraInit()
        {
            _camera = GetComponentInChildren<PlayerCamera>();
            if (_camera == null)
            {
                Debug.Log("Not Found PlayerCamera!!!");
            }

            var cameraParent = GetComponentInChildren<TargetFollowObject>();
            if (cameraParent == null)
            {
                Debug.Log("Not Found TargetFollowObject!!!");
            }

            cameraParent.SetTarget(transform);
            cameraParent.transform.parent = null;
        }

        public void MoveCamera(Vector2 input)
        {
            if (Camera != null)
            {
                Camera.MoveCamera(input);
            }
        }
    }
}

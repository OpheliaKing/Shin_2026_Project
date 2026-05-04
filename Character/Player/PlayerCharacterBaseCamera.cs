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

        public void MoveCamera(Vector2 input)
        {
            if (Camera != null)
            {
                Camera.MoveCamera(input);
            }
        }
    }
}

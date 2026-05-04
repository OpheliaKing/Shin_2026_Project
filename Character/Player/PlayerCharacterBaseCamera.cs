using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase
    {
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

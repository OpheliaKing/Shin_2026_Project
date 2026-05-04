using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase : CharacterBase
    {
        [SerializeField] private Transform _cameraTransform;

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        [SerializeField]
        private Vector2 _input;

        private void Update()
        {
            MoveFromCameraRelativeInput(_input);
        }
    }
}


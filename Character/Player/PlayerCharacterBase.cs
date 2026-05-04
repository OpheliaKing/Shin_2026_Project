using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase : CharacterBase
    {
        private void Update()
        {
            MoveFromCameraRelativeInput(_moveInput);
        }
    }
}


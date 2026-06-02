using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase : CharacterBase
    {

        protected override void Init()
        {
            base.Init();
            CameraInit();
        }
        protected override void Update()
        {
            base.Update();
            MoveFromCameraRelativeInput(_moveInput);
        }
    }
}


using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase : CharacterBase
    {

        protected override void Init()
        {
            base.Init();
            CameraInit();
            EnsureDefaultPlayerAIState();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsPlayerInputAllowed)
            {
                ClearPlayerControlInput();
                return;
            }

            MoveFromCameraRelativeInput(_moveInput);
        }

        protected override void OnEnterState(CHARACTER_STATE previousState, CHARACTER_STATE currentState)
        {
            base.OnEnterState(previousState, currentState);

            if (currentState == CHARACTER_STATE.DIE)
            {
                ClearPlayerControlInput();
            }
        }

        protected override void OnInputBlocked()
        {
            ClearPlayerControlInput();
        }
    }
}


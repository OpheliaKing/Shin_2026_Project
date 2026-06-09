using UnityEngine;
using UnityEngine.InputSystem;


namespace Shin
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerCharacterBase _player;

        private PlayerCharacterBase Player
        {
            get
            {
                if (_player == null)
                {
                    _player = GetComponent<PlayerCharacterBase>();
                }
                return _player;
            }
        }

        public void OnMove(InputValue value)
        {
            if (!TryGetInputTarget(out PlayerCharacterBase player))
            {
                return;
            }

            Debug.Log("OnMove: " + value.Get<Vector2>());
            player.MoveInput(value.Get<Vector2>());
        }

        public void OnClick_Left(InputValue value)
        {
            if (!TryGetInputTarget(out PlayerCharacterBase player))
            {
                return;
            }

            var ispressed = value.Get<float>() > 0f;
            Debug.Log("OnClick_Left: " + ispressed);
            player.AttackInput(INPUT_TYPE.LEFT_CLICK, ispressed);
        }

        public void OnClick_Right(InputValue value)
        {
            if (!TryGetInputTarget(out PlayerCharacterBase player))
            {
                return;
            }

            var ispressed = value.Get<float>() > 0f;
            Debug.Log("OnClick_Right: " + ispressed);
            player.AttackInput(INPUT_TYPE.RIGHT_CLICK, ispressed);
        }

        public void OnShift_Left(InputValue value)
        {
            if (!TryGetInputTarget(out PlayerCharacterBase player))
            {
                return;
            }

            player.Shift_Left_Input(value.Get<float>());
        }

        public void OnMove_Camera(InputValue value)
        {
            if (!TryGetInputTarget(out PlayerCharacterBase player))
            {
                return;
            }

            player.MoveCamera(value.Get<Vector2>());
        }

        private bool TryGetInputTarget(out PlayerCharacterBase player)
        {
            player = Player;
            if (player == null)
            {
                return false;
            }

            if (!player.IsPlayerInputAllowed)
            {
                player.ClearPlayerControlInput();
                return false;
            }

            return true;
        }
    }

}

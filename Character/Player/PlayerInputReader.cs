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
            if (Player != null)
            {
                Debug.Log("OnMove: " + value.Get<Vector2>());
                Player.MoveInput(value.Get<Vector2>());
            }
        }

        public void OnClick_Left(InputValue value)
        {
            if (Player != null)
            {
                Debug.Log("OnClick_Left: ");
            }
        }

        public void OnClick_Right(InputValue value)
        {

        }

        public void OnShift_Left(InputValue value)
        {
            if (Player != null)
            {
                Player.Shift_Left_Input(value.Get<float>());
            }
        }

        public void OnMove_Camera(InputValue value)
        {
            if (Player != null)
            {
                Player.MoveCamera(value.Get<Vector2>());
            }
        }
    }

}

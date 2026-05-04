using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputCheck : MonoBehaviour
{
        public void OnMove(InputValue value)
        {
            Debug.Log("OnMove: " + value.Get<Vector2>());
        }

        public void OnClick_Left(InputValue value)
        {
            Debug.Log("OnClick_Left: ");
        }
}

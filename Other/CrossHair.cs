using UnityEngine;
using UnityEngine.InputSystem;

namespace Shin
{
    [RequireComponent(typeof(RectTransform))]
    public class CrossHair : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private RectTransform _canvasRectTransform;
        private Camera _uiCamera;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            _canvasRectTransform = canvas.transform as RectTransform;
            _uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private void LateUpdate()
        {
            if (!TryGetMouseScreenPosition(out Vector2 mouseScreenPosition))
            {
                return;
            }

            if (_canvasRectTransform == null)
            {
                _rectTransform.position = mouseScreenPosition;
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRectTransform,
                    mouseScreenPosition,
                    _uiCamera,
                    out Vector2 localPoint))
            {
                _rectTransform.localPosition = localPoint;
            }
        }

        private static bool TryGetMouseScreenPosition(out Vector2 screenPosition)
        {
            if (Mouse.current == null)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
    }
}

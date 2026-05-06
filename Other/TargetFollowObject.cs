using UnityEngine;

namespace Shin
{
    public class TargetFollowObject : MonoBehaviour
    {
        [SerializeField]
        private Transform _target;
        private void LateUpdate()
        {
            if (_target != null)
            {
                transform.position = _target.position;
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

    }

}

using UnityEngine;

namespace Shin
{
    public class ManagerBase : MonoBehaviour
    {
        /// <summary>
        /// <paramref name="parent"/> 하위에서 매니저를 찾고, 없으면 자식 GameObject를 만들어 컴포넌트를 붙입니다.
        /// </summary>
        public static T GetOrCreate<T>(Transform parent, ref T manager) where T : ManagerBase
        {
            if (manager != null)
            {
                return manager;
            }

            manager = parent.GetComponentInChildren<T>(true);
            if (manager != null)
            {
                return manager;
            }

            var childObject = new GameObject(typeof(T).Name);
            childObject.transform.SetParent(parent, false);
            manager = childObject.AddComponent<T>();
            return manager;
        }
    }
}

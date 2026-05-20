using UnityEngine;

namespace Shin
{
    /// <summary>
    /// <c>class MyManager : SingtonObject&lt;MyManager&gt;</c> 형태로 상속하면 씬에 하나만 존재하는 싱글톤으로 동작합니다.
    /// </summary>
    public abstract class SingtonObject<T> : MonoBehaviour where T : SingtonObject<T>
    {
        private static T _instance;
        private static bool _applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        /// <summary>true이면 씬 전환 후에도 파괴되지 않습니다.</summary>
        protected virtual bool PersistAcrossScenes => false;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;

            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnSingletonAwake();
        }

        /// <summary>싱글톤으로 등록된 뒤 한 번 호출됩니다. Awake 대신 오버라이드할 때 사용합니다.</summary>
        protected virtual void OnSingletonAwake()
        {
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}

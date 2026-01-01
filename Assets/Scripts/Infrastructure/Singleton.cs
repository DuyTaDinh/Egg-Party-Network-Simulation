using UnityEngine;

namespace Core
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T m_instance;

        public static T Instance
        {
            get
            {
                if (!m_instance)
                {
                    m_instance = FindFirstObjectByType<T>();
                }

                return m_instance;
            }
        }

        protected virtual void Awake()
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
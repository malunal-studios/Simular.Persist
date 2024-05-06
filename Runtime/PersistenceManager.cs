using System.Collections;
using UnityEngine;

namespace Simular.Persist {
    /// <summary>
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistenceManager : MonoBehaviour {
    #region Singleton Implementation
        private static PersistenceManager m_Singleton;

        /// <summary>
        /// Provides access to the singleton instance of <c>PersistenceManager</c>
        /// that can be used to perform other operations.
        /// </summary>
        public static PersistenceManager Singleton =>
            FindExistingOrCreateInstance();


        /// <summary>
        /// Checks if this <c>PersistenceManager</c> has been created.
        /// </summary>
        /// <remarks>
        /// Becomes true after Unity has instantiated it within the current
        /// scene. It will remain true throughout the course of the application
        /// unless destroyed.
        /// </remarks>
        public bool IsAwakened { get; private set; }


        /// <summary>
        /// Checks if this <c>PersistenceManager</c> has been started.
        /// </summary>
        /// <remarks>
        /// Becomes true after Unity has issued the first update to this
        /// instance. It will remain true throughout the course of the
        /// application unless destroyed.
        /// </remarks>
        public bool IsStarted { get; private set; }

        
        /// <summary>
        /// Checks if this <c>PersistenceManager</c> has been destroyed.
        /// </summary>
        /// <remarks>
        /// This may become true if the instance was destroyed erroneously, or
        /// the application is quitting. Otherwise, it should almost always be
        /// false.
        /// </remarks>
        public bool IsDestroyed { get; private set; }


        private static PersistenceManager FindExistingOrCreateInstance() {
            if (m_Singleton != null)
                return m_Singleton;

            if (m_Singleton.IsDestroyed)
                return null;

            var existingInstances = FindObjectsByType<PersistenceManager>(FindObjectsSortMode.None);
            if (existingInstances != null && existingInstances.Length > 0)
                return existingInstances[0];
            
            var newInstance = new GameObject("PersistenceManager (Singleton)");
            return newInstance.AddComponent<PersistenceManager>();
        }

    #region Unity Events
        private void Awake() {
            var thisInstance = GetComponent<PersistenceManager>();
            if (m_Singleton == null) {
                m_Singleton = thisInstance;
                DontDestroyOnLoad(m_Singleton.gameObject);
            }
        
            if (IsAwakened)
                return;
            
            SingletonAwakened();
            IsAwakened = true;
        }

        private void Start() {
            if (IsStarted)
                return;
            
            SingletonStarted();
            IsStarted = true;
        }

        private void OnDestroy() {
            if (this != m_Singleton)
                return;
            
            SingletonDestroyed();
            IsDestroyed = true;
        }

        private void OnEnable() {
            if (this == m_Singleton)
                SingletonEnabled();
        }

        private void OnDisable() {
            if (this == m_Singleton)
                SingletonDisabled();
        }
    #endregion /* Unity Events */
    #endregion /* Singleton Implementation */

        [Header("Auto Save")]
        [SerializeField]
        [InspectorName("Enabled")]
        [Tooltip("Whether auto save functionality should be enabled.")]
        private bool m_AutoSaveEnabled;

        [SerializeField]
        [InspectorName("Interval")]
        [Tooltip("The interval in seconds at which auto saves should take place.")]
        private float     m_AutoSaveInterval;
        private Coroutine m_AutoSaveCoroutine;


        [Header("Persister")]
        [SerializeField]
        [InspectorName("Settings")]
        private Persister.Settings m_PersisterSettings;
        private Persister          m_Persister;
        private PersistenceReader  m_PersistencReader;
        private PersistenceWriter  m_PersistencWriter;


        /// <summary>
        /// Provides access to the persister that this
        /// <c>PersistenceManager</c> is managing.
        /// </summary>
        public Persister Persister => m_Persister;

        /// <summary>
        /// Provides access to the persistence reader for the given data being
        /// managed by this <c>PersistenceManager</c>.
        /// </summary>
        public PersistenceReader Reader => m_PersistencReader;

        /// <summary>
        /// Provides access to the persistence writer for the given data being
        /// managed by this <c>PersistenceManager</c>.
        /// </summary>
        public PersistenceWriter Writer => m_PersistencWriter;


        protected virtual void SingletonAwakened() {
            m_Persister = new Persister(m_PersisterSettings);
            m_PersistencReader = new PersistenceReader(m_Persister);
            m_PersistencWriter = new PersistenceWriter(m_Persister);
        }

        protected virtual void SingletonStarted() {
        }

        protected virtual void SingletonDestroyed() {
        }

        protected virtual void SingletonEnabled() {
            if (m_AutoSaveCoroutine != null)
                StopCoroutine(m_AutoSaveCoroutine);
            m_AutoSaveCoroutine = StartCoroutine(HandleAutoSaving());
        }

        protected virtual void SingletonDisabled() {
            StopCoroutine(m_AutoSaveCoroutine);
            m_AutoSaveCoroutine = null;
        }


        private IEnumerator HandleAutoSaving() {
            while (true) {
                yield return new WaitForSecondsRealtime(m_AutoSaveInterval);
                m_Persister.Flush();
            }
        }
    }
}
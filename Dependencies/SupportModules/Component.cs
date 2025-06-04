using UnityEngine;

#if SM_Il2Cpp
using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
#endif

namespace MelonLoader.Support
{
    internal class SM_Component : MonoBehaviour
    {
        private bool isQuitting;

#if SM_Il2Cpp
        public SM_Component(IntPtr value) : base(value) { }
#endif

        internal static void Create()
        {
            if (Main.component != null)
                return;

            Main.obj = new GameObject();
            DontDestroyOnLoad(Main.obj);
            Main.obj.hideFlags = HideFlags.DontSave;

#if SM_Il2Cpp
            ClassInjector.RegisterTypeInIl2Cpp<SM_Component>();
            Main.component = Main.obj.AddComponent(Il2CppType.Of<SM_Component>()).TryCast<SM_Component>();
#else
            Main.component = (SM_Component)Main.obj.AddComponent(typeof(SM_Component));
#endif

            ComponentSiblingFix.SetAsLastSibling(Main.obj.transform);
        }

        void Start()
        {
            if ((Main.component != null) && (Main.component != this))
                return;

            ComponentSiblingFix.SetAsLastSibling(transform);
            Main.Interface.OnApplicationLateStart();
        }

        void Awake()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            foreach (var queuedCoroutine in SupportModule_To.QueuedCoroutines)
#if SM_Il2Cpp
                StartCoroutine(new Il2CppSystem.Collections.IEnumerator(new MonoEnumeratorWrapper(queuedCoroutine).Pointer));
#else
                StartCoroutine(queuedCoroutine);
#endif
            SupportModule_To.QueuedCoroutines.Clear();
        }

        void Update()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            isQuitting = false;
            ComponentSiblingFix.SetAsLastSibling(transform);

            SceneHandler.OnUpdate();
            Main.Interface.Update();
        }

        void OnDestroy()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            if (!isQuitting)
            {
                Create();
                return;
            }

            OnApplicationDefiniteQuit();
        }

        void OnApplicationQuit()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            isQuitting = true;
            Main.Interface.Quit();
        }

        void OnApplicationDefiniteQuit()
        {
            Main.Interface.DefiniteQuit();
        }

        void FixedUpdate()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            Main.Interface.FixedUpdate();
        }

        void LateUpdate()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            Main.Interface.LateUpdate();
        }

        void OnGUI()
        {
            if ((Main.component == null) || (Main.component != this))
                return;

            Main.Interface.OnGUI();
        }
    }
}
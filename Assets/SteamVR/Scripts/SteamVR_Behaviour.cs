﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_2017_2_OR_NEWER
    using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace Valve.VR
{
    public class SteamVR_Behaviour : MonoBehaviour
    {
        private const string openVRDeviceName = "OpenVR";

        private static SteamVR_Behaviour _instance;
        public static SteamVR_Behaviour instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize();
                }

                return _instance;
            }
        }

        public bool initializeSteamVROnAwake = true;

        [HideInInspector]
        public bool forcingInitialization = false;

        [HideInInspector]
        public SteamVR_Render steamvr_render;

        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject steamVRObject = null;

                SteamVR_Render renderInstance = GameObject.FindObjectOfType<SteamVR_Render>();
                if (renderInstance != null)
                    steamVRObject = renderInstance.gameObject;

                SteamVR_Behaviour behaviourInstance = GameObject.FindObjectOfType<SteamVR_Behaviour>();
                if (behaviourInstance != null)
                    steamVRObject = behaviourInstance.gameObject;

                if (steamVRObject == null)
                {
                    GameObject objectInstance = new GameObject("[SteamVR]");
                    _instance = objectInstance.AddComponent<SteamVR_Behaviour>();
                    _instance.steamvr_render = objectInstance.AddComponent<SteamVR_Render>();
                }
                else
                {
                    if (behaviourInstance == null)
                        behaviourInstance = steamVRObject.AddComponent<SteamVR_Behaviour>();
                    if (renderInstance == null)
                        behaviourInstance.steamvr_render = steamVRObject.AddComponent<SteamVR_Render>();

                    _instance = behaviourInstance;
                }
            }
        }

        protected void Awake()
        {
            SteamVR_Input.PreInitialize();

            if (initializeSteamVROnAwake)
                InitializeSteamVR();
        }

        public void InitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            if (forceUnityVRToOpenVR)
            {
                forcingInitialization = true;

                if (initializeCoroutine != null)
                    StopCoroutine(initializeCoroutine);

                if (XRSettings.loadedDeviceName == openVRDeviceName)
                    EnableOpenVR();
                else
                    initializeCoroutine = StartCoroutine(DoInitializeSteamVR(forceUnityVRToOpenVR));
            }
            else
            {
                SteamVR.Initialize(false);
            }
        }

        private Coroutine initializeCoroutine;
        private IEnumerator DoInitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            XRSettings.LoadDeviceByName(openVRDeviceName);
            yield return null;
            EnableOpenVR();
        }

        private void EnableOpenVR()
        {
            XRSettings.enabled = true;
            SteamVR.Initialize(false);
            initializeCoroutine = null;
            forcingInitialization = false;
        }

#if UNITY_2017_1_OR_NEWER
        protected void OnEnable()
        {
		    Application.onBeforeRender += OnBeforeRender;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Listen(OnQuit);
        }
        protected void OnDisable()
        {
		    Application.onBeforeRender -= OnBeforeRender;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Remove(OnQuit);
        }
	    protected void OnBeforeRender() 
        { 
            PreCull();
        }
#else
        protected void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Listen(OnQuit);
        }
        protected void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Remove(OnQuit);
        }
        protected void OnCameraPreCull(Camera cam)
        {
            if (!cam.stereoEnabled)
                return;

            PreCull();
        }
#endif

        protected static int lastFrameCount = -1;
        protected void PreCull()
        {
            // Only update poses on the first camera per frame.
            if (Time.frameCount != lastFrameCount)
            {
                lastFrameCount = Time.frameCount;

                SteamVR_Input.OnPreCull();
            }
        }

        protected void FixedUpdate()
        {
            SteamVR_Input.FixedUpdate();
        }

        protected void LateUpdate()
        {
            SteamVR_Input.LateUpdate();
        }

        protected void Update()
        {
            SteamVR_Input.Update();
        }

        protected void OnQuit(VREvent_t vrEvent)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
		    Application.Quit();
#endif
        }
    }
}

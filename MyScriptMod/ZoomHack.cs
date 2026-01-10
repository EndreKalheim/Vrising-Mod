using UnityEngine;

namespace MyScriptMod
{
    public class ZoomHack : MonoBehaviour
    {
        public static ZoomHack Instance; 
        public bool Enabled = false;
        public float CurrentFov = 60f;
        private Camera _mainCam;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!Enabled) return;
            
            if (_mainCam == null) _mainCam = Camera.main;

            if (_mainCam != null)
            {
                // Force FOV to the slider value
                _mainCam.fieldOfView = CurrentFov;
                
                // V-Rising specific: also unlock the zoom distance if possible
                _mainCam.farClipPlane = 1000f; 
            }
        }
    }
}
using UnityEngine;

namespace MyScriptMod
{
    public class ZoomHack : MonoBehaviour
    {
        public bool Enabled = false;
        public float CurrentFov = 60f;
        private Camera _mainCam;

        private void Update()
        {
            if (!Enabled) return;

            if (_mainCam == null) _mainCam = Camera.main;
            
            if (_mainCam != null)
            {
                _mainCam.fieldOfView = CurrentFov;
            }
        }
    }
}
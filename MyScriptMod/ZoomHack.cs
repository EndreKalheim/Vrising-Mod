using UnityEngine;

namespace MyScriptMod
{
    public class ZoomHack : MonoBehaviour
    {
        public bool Enabled = false;
        
        // --- Main Camera FOV ---
        public float CurrentFov = 60f;
        
        // --- Minimap Zoom ---
        public bool EnableMinimapZoom = true;
        public float MinimapSize = 15f; // Default is usually around 5-10
        
        private Camera _mainCam;
        private Camera _minimapCam;
        private float _lastUpdate;

        private void Update()
        {
            if (!Enabled) return;

            // Performance: Only search for cameras once every second, not every frame
            if (Time.time - _lastUpdate > 1.0f)
            {
                FindCameras();
                _lastUpdate = Time.time;
            }

            ApplyZoom();
        }

        private void FindCameras()
        {
            if (_mainCam == null) _mainCam = Camera.main;

            // Try to find the Minimap camera by name or tag
            if (_minimapCam == null)
            {
                foreach (var cam in Camera.allCameras)
                {
                    // V Rising usually names it "MinimapCamera" or it renders to a specific texture
                    if (cam.name.ToLower().Contains("minimap") || cam.name.Contains("MapCamera"))
                    {
                        _minimapCam = cam;
                        break;
                    }
                }
            }
        }

        private void ApplyZoom()
        {
            // 1. Main Camera FOV
            if (_mainCam != null)
            {
                if (Mathf.Abs(_mainCam.fieldOfView - CurrentFov) > 0.1f)
                {
                    _mainCam.fieldOfView = CurrentFov;
                }
            }

            // 2. Minimap Zoom (Orthographic Size)
            if (EnableMinimapZoom && _minimapCam != null)
            {
                // Minimaps are usually Orthographic. We change size to zoom out.
                // Higher Size = Zoomed Out further.
                if (_minimapCam.orthographic)
                {
                    if (Mathf.Abs(_minimapCam.orthographicSize - MinimapSize) > 0.1f)
                    {
                        _minimapCam.orthographicSize = MinimapSize;
                    }
                }
            }
        }
    }
}
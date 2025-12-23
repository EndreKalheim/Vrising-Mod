using System;
using UnityEngine;
using BepInEx;
using System.Collections;

namespace MyScriptMod
{
    public class ZoomHack : MonoBehaviour
    {
        public bool EnableZoomHack = false;
        public float MaxZoomValue = 20f; 
        
        public string DebugStatus = "Initializing...";

        private Camera _minimapCamera;

        private void Update()
        {
            if (!EnableZoomHack)
            {
                 DebugStatus = "Disabled";
                 return;
            }

            // Find camera lazily
            if (_minimapCamera == null)
            {
                var cams = Camera.allCameras;
                DebugStatus = $"Scanning {cams.Length} cameras...";
                foreach (var cam in cams)
                {
                    if (cam.name.IndexOf("minimap", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        cam.name.IndexOf("MapCamera", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _minimapCamera = cam;
                        break;
                    }
                }
                
                if (_minimapCamera == null)
                {
                    DebugStatus = "Camera Not Found! (Try entering/exiting map)";
                    return;
                }
            }

            if (_minimapCamera != null)
            {
                DebugStatus = $"Optimized: Found {_minimapCamera.name}";
                // Force size if enabled
                // We use >= check to start allowing zoom out, but we act as an override
                // If checking < MaxZoomValue, it prevents Zoom In.
                // We actually want to Clamp the MINIMUM viewing area? (Zoom Out = Larger Size)
                // If we want to allow Zoom Out to 50, but game caps at 10.
                // The game likely sets size = clamp(input, 2, 10).
                // We want to overwrite size = clamp(input, 2, 50).
                
                // Since we can't intercept the input easily, we just FORCE the size to our slider value.
                // This means the Slider BECOMES the zoom wheel.
                _minimapCamera.orthographicSize = MaxZoomValue;
            }
        }
    }
}

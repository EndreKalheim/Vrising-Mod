using UnityEngine;

namespace MyScriptMod
{
    public class Menu : MonoBehaviour
    {
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(50, 50, 400, 600);
        private int _currentTab = 0;
        public static bool UnlockUI = false; 

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home)) _isVisible = !_isVisible;
        }

        private void OnGUI()
        {
            if (!_isVisible) return;
            GUI.backgroundColor = Color.black;
            _windowRect = GUI.Window(0, _windowRect, (GUI.WindowFunction)DrawWindow, "V-MOD PREMIER");
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ESP & AIM")) _currentTab = 0;
            if (GUILayout.Button("SETTINGS & UI")) _currentTab = 1;
            GUILayout.EndHorizontal();

            if (_currentTab == 0) DrawEspTab();
            else DrawSettingsTab();
        }

        private void DrawEspTab()
        {
            var esp = GetComponent<EspFeature>();
            if (esp == null) return;

            GUILayout.Label("<b>Visuals (F1)</b>");
            esp.IsEspActive = GUILayout.Toggle(esp.IsEspActive, "Enable ESP");
            esp.ShowTracers = GUILayout.Toggle(esp.ShowTracers, "Center Tracers (Red)");
            
            // Range logic removed - Infinite scan enabled
            GUILayout.Label($"Self-Remove Radius: {esp.SelfCylinderRadius:F1}m");
            esp.SelfCylinderRadius = GUILayout.HorizontalSlider(esp.SelfCylinderRadius, 0.5f, 10f);
            
            esp.ShowDebug = GUILayout.Toggle(esp.ShowDebug, "Show Debug Circle (Yellow)");

            esp.ShowBoxes = GUILayout.Toggle(esp.ShowBoxes, "2D Red Boxes");

            GUILayout.Space(10);
            GUILayout.Label("<b>Top-Down Aim (F2)</b>");
            esp.EnableAimAssist = GUILayout.Toggle(esp.EnableAimAssist, "Enable Mouse-to-Hitbox");
            GUILayout.Label($"Aim Smoothness: {esp.AimSmoothness:F0}");
            esp.AimSmoothness = GUILayout.HorizontalSlider(esp.AimSmoothness, 1f, 30f);
        }

        private void DrawSettingsTab()
        {
            // --- COOLDOWN SECTION RESTORED ---
            GUILayout.Label("<b>Cooldown Overlay</b>");
            var cd = GetComponent<CooldownOverlay>();
            if (cd != null)
            {
                cd.ShowOverlay = GUILayout.Toggle(cd.ShowOverlay, "Show Cooldown Icons");
                UnlockUI = GUILayout.Toggle(UnlockUI, "Unlock UI (Drag Icons)");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Camera</b>");
            var zoom = GetComponent<ZoomHack>();
            if (zoom != null)
            {
                zoom.Enabled = GUILayout.Toggle(zoom.Enabled, "Enable ZoomHack");
                GUILayout.Label($"FOV: {zoom.CurrentFov:F0}");
                zoom.CurrentFov = GUILayout.HorizontalSlider(zoom.CurrentFov, 40f, 140f);
            }
        }
    }
}
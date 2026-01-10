using UnityEngine;
using BepInEx;

namespace MyScriptMod
{
    public class Menu : MonoBehaviour
    {
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(50, 50, 400, 600);
        private int _currentTab = 0;
        public static bool UnlockUI = false; 

        private void Start()
        {
             Plugin.Logger.LogInfo("Menu Initialized");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home)) _isVisible = !_isVisible;
        }

        private void OnGUI()
        {
            if (!_isVisible) return;
            
            GUI.backgroundColor = Color.black;
            GUI.contentColor = Color.white; 
            _windowRect = GUI.Window(9999, _windowRect, (GUI.WindowFunction)DrawWindow, "V-MOD PREMIER");
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
            esp.IsEspActive = GUILayout.Toggle(esp.IsEspActive, "Enable ESP (Master Switch)"); 
            esp.ShowTracers = GUILayout.Toggle(esp.ShowTracers, "Center Tracers (Red)");
            esp.ShowBoxes = GUILayout.Toggle(esp.ShowBoxes, "2D Red Boxes");

            GUILayout.Space(10);
            GUILayout.Label("<b>Top-Down Aim (F2)</b>");
            esp.EnableAimAssist = GUILayout.Toggle(esp.EnableAimAssist, "Enable Mouse-to-Hitbox");
            if (esp.EnableAimAssist)
            {
                GUILayout.Label($"Aim Smoothness: {esp.AimSmoothness:F1}");
                esp.AimSmoothness = GUILayout.HorizontalSlider(esp.AimSmoothness, 1f, 30f);
            }
            
            GUILayout.Space(5);
            esp.EnablePrediction = GUILayout.Toggle(esp.EnablePrediction, "Enable Prediction (Leading)");
            if (esp.EnablePrediction)
            {
                GUILayout.Label($"Prediction Scale: {esp.PredictionScale:F2}");
                esp.PredictionScale = GUILayout.HorizontalSlider(esp.PredictionScale, 0f, 2.0f);
            }
            GUILayout.Label($"Aim Circle Size: {esp.AimFov:F0}px");
            esp.AimFov = GUILayout.HorizontalSlider(esp.AimFov, 50f, 1000f);

            GUILayout.Space(10);
            GUILayout.Label("<b>Lists Management</b>");
            GUILayout.Label($"Friends: {esp.FriendList.Count} | Ignored: {esp.IgnoreList.Count}");
        }

        private void DrawSettingsTab()
        {
            GUILayout.Label("<b>Cooldown Overlay</b>");
            
            // Switch to Singleton Access to fix Crash
            var cd = CooldownOverlay.Instance;
            if (cd != null)
            {
                cd.ShowOverlay = GUILayout.Toggle(cd.ShowOverlay, "Show Cooldown Icons");
                UnlockUI = GUILayout.Toggle(UnlockUI, "Unlock UI (Drag Icons)");
                
                if (GUILayout.Button("Reset Overlay Position")) {
                     cd.ResetPosition();
                }
            }
            else
            {
                 GUILayout.Label("Cooldown Component Missing");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Camera</b>");
            
            // Switch to Singleton Access
            var zoom = ZoomHack.Instance;
            if (zoom != null)
            {
                zoom.Enabled = GUILayout.Toggle(zoom.Enabled, "Enable ZoomHack");
                GUILayout.Label($"FOV: {zoom.CurrentFov:F0}");
                zoom.CurrentFov = GUILayout.HorizontalSlider(zoom.CurrentFov, 40f, 140f);
            }
        }
    }
}
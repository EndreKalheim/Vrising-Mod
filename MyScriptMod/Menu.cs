using UnityEngine;
using System.Collections.Generic;

namespace MyScriptMod
{
    public class Menu : MonoBehaviour
    {
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(50, 50, 400, 550); // Made slightly wider
        private int _currentTab = 0;

        // Script Creator State
        private string _newScriptName = "My New Macro";
        private KeyCode _newScriptKey = KeyCode.None;
        private bool _isRecordingKey = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home)) _isVisible = !_isVisible;
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            try {
                GUI.backgroundColor = Color.black;
                _windowRect = GUI.Window(0, _windowRect, (GUI.WindowFunction)DrawWindow, "V Rising Mod Menu");
            } catch (System.Exception) { }
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ESP")) _currentTab = 0;
            if (GUILayout.Button("Macros")) _currentTab = 1;
            if (GUILayout.Button("Scripts")) _currentTab = 2;
            if (GUILayout.Button("Settings")) _currentTab = 3;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            switch (_currentTab)
            {
                case 0: DrawEspTab(); break;
                case 1: DrawMacroTab(); break;
                case 2: DrawScriptCreatorTab(); break;
                case 3: DrawSettingsTab(); break;
            }
        }

        private void DrawEspTab()
        {
            var esp = GetComponent<EspFeature>();
            if (esp == null) return;

            GUILayout.Label("<b>ESP Visuals</b>");
            esp.IsEspActive = GUILayout.Toggle(esp.IsEspActive, "Enable Master ESP");
            esp.ShowTracers = GUILayout.Toggle(esp.ShowTracers, "Show Tracers (Center)");
            esp.ShowBoxes = GUILayout.Toggle(esp.ShowBoxes, "Show 2D Boxes");
            
            GUILayout.Space(5);
            GUILayout.Label($"Max Distance: {Mathf.RoundToInt(esp.ScanRadius)}m");
            esp.ScanRadius = GUILayout.HorizontalSlider(esp.ScanRadius, 10f, 400f);

            GUILayout.Space(5);
            GUILayout.Label($"Ignore Local Player Radius: {esp.IgnoreRadius:F1}m");
            GUILayout.Label("<color=grey>(Increase this if you see lines on yourself)</color>");
            esp.IgnoreRadius = GUILayout.HorizontalSlider(esp.IgnoreRadius, 0.5f, 10f);
        }

        private void DrawMacroTab()
        {
            GUILayout.Label("<b>Standard Macro (L Key)</b>");
            GUILayout.Label($"O+9 Delay: {Plugin.DelayAfterO9.Value:F2}");
            Plugin.DelayAfterO9.Value = GUILayout.HorizontalSlider(Plugin.DelayAfterO9.Value, 0.1f, 1.0f);
        }

        private void DrawScriptCreatorTab()
        {
            GUILayout.Label("<b>Add New Script</b>");
            _newScriptName = GUILayout.TextField(_newScriptName);

            if (GUILayout.Button(_isRecordingKey ? "Press any key..." : $"Trigger Key: {_newScriptKey}"))
            {
                _isRecordingKey = true;
            }

            if (_isRecordingKey && Event.current.isKey)
            {
                _newScriptKey = Event.current.keyCode;
                _isRecordingKey = false;
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Create & Save Script"))
            {
                var newProfile = new ScriptProfile { 
                    Name = _newScriptName, 
                    TriggerKey = (int)_newScriptKey 
                };
                newProfile.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.Wait, Duration = 1.0f });
                
                ScriptManager.Instance.Profiles.Add(newProfile);
                ScriptManager.Instance.SaveScripts();
                _newScriptName = "New Script";
                _newScriptKey = KeyCode.None;
            }

            GUILayout.Space(15);
            GUILayout.Label("<b>Current Scripts:</b>");
            foreach (var p in ScriptManager.Instance.Profiles)
            {
                GUILayout.Label($"- {p.Name} [{ (KeyCode)p.TriggerKey }]");
            }
        }

        private void DrawSettingsTab()
        {
            GUILayout.Label("<b>Overlay Settings</b>");
            
            // Cooldown Overlay Control
            var cd = GetComponent<CooldownOverlay>();
            if (cd != null)
            {
                cd.ShowOverlay = GUILayout.Toggle(cd.ShowOverlay, "Show Cooldown Overlay");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Camera / ZoomHack</b>");
            
            // ZoomHack Control
            var zoom = GetComponent<ZoomHack>();
            if (zoom != null)
            {
                GUILayout.Label("<b>Camera & Map</b>");
                zoom.Enabled = GUILayout.Toggle(zoom.Enabled, "Enable Camera Mods");
                
                // Main FOV
                GUILayout.Label($"Game FOV: {Mathf.RoundToInt(zoom.CurrentFov)}");
                zoom.CurrentFov = GUILayout.HorizontalSlider(zoom.CurrentFov, 60f, 140f);

                GUILayout.Space(5);

                // Minimap Zoom
                zoom.EnableMinimapZoom = GUILayout.Toggle(zoom.EnableMinimapZoom, "Enable Minimap Zoom");
                GUILayout.Label($"Minimap Zoom: {zoom.MinimapSize:F1}");
                // 5 is usually close, 20 is far
                zoom.MinimapSize = GUILayout.HorizontalSlider(zoom.MinimapSize, 5f, 30f); 
            }

            GUILayout.Space(20);
            GUILayout.Label("<b>System</b>");
            if (GUILayout.Button("RESTART: Re-load All Configs"))
            {
                ScriptManager.Instance.LoadScripts();
                Plugin.Instance.Config.Reload();
            }
        }
    }
}
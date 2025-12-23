using UnityEngine;
using System.Collections.Generic;

namespace MyScriptMod
{
    public class Menu : MonoBehaviour
    {
        private bool _isVisible = false;
        private Rect _windowRect = new Rect(50, 50, 350, 500);
        private int _currentTab = 0;

        // Script Creator State
        private string _newScriptName = "My New Macro";
        private KeyCode _newScriptKey = KeyCode.None;
        private bool _isRecordingKey = false;

        private void Update()
        {
            // Changed to Home key as requested
            if (Input.GetKeyDown(KeyCode.Home)) _isVisible = !_isVisible;
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            // Using a try-catch block here helps prevent the entire game from 
            // lagging out if the IMGUI layout mismatches during a frame.
            try {
                _windowRect = GUI.Window(0, _windowRect, (GUI.WindowFunction)DrawWindow, "Final Macro - V Rising");
            } catch (System.Exception) {
                // Ignore layout errors to prevent crashing
            }
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ESP")) _currentTab = 0;
            if (GUILayout.Button("Macros")) _currentTab = 1;
            if (GUILayout.Button("Script Creator")) _currentTab = 2;
            if (GUILayout.Button("Settings")) _currentTab = 3;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Using ScrollView prevents the "Control Count" error when lists get long
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

            esp.IsEspActive = GUILayout.Toggle(esp.IsEspActive, "Enable ESP Master");
            esp.ShowTracers = GUILayout.Toggle(esp.ShowTracers, "Show Line Tracers");
            esp.ShowBoxes = GUILayout.Toggle(esp.ShowBoxes, "Show Boxes");
            
            GUILayout.Label($"Scan Range: {Mathf.RoundToInt(esp.ScanRadius)}m");
            esp.ScanRadius = GUILayout.HorizontalSlider(esp.ScanRadius, 10f, 300f);
        }

        private void DrawMacroTab()
        {
            GUILayout.Label("Standard Macro (L Key)");
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
                // Adding a default wait action so the script isn't empty
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
            if (GUILayout.Button("DEBUG: Reset Menu Position"))
            {
                _windowRect = new Rect(50, 50, 350, 500);
            }

            if (GUILayout.Button("RESTART: Re-load All Configs"))
            {
                ScriptManager.Instance.LoadScripts();
                Plugin.Instance.Config.Reload();
            }

            if (GUILayout.Button("Close Menu (Home)")) _isVisible = false;
        }
    }
}
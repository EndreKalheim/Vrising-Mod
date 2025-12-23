using System;
using UnityEngine;
using BepInEx;

namespace MyScriptMod
{
    public class Menu : MonoBehaviour
    {
        private bool _showMenu = false;
        private Rect _windowRect = new Rect(20, 20, 650, 600); 
        private int _selectedTab = 0;
        private string[] _tabs = { "ESP", "Scripts", "Cooldowns", "Misc" };

        private ScriptManager _scriptManager;
        private EspFeature _espFeature;
        private CooldownOverlay _cooldownOverlay;
        private ZoomHack _zoomHack;

        // Script Editing State
        private ScriptProfile _selectedProfile = null;
        private Vector2 _scrollPos;
        
        // Filter UI State
        private string newFilterText = "";
        private string newCompText = "";

        private void Start()
        {
            _scriptManager = ScriptManager.Instance;
            _espFeature = GetComponent<EspFeature>(); 
            _cooldownOverlay = GetComponent<CooldownOverlay>();
            _zoomHack = GetComponent<ZoomHack>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.Home))
            {
                _showMenu = !_showMenu;
            }

            if (_scriptManager == null) _scriptManager = ScriptManager.Instance;
            if (_espFeature == null) _espFeature = FindObjectOfType<EspFeature>();
            if (_cooldownOverlay == null) _cooldownOverlay = FindObjectOfType<CooldownOverlay>();
            if (_zoomHack == null) _zoomHack = FindObjectOfType<ZoomHack>();
        }

        private void OnGUI()
        {
            if (!_showMenu) return;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            _windowRect = GUI.Window(0, _windowRect, (GUI.WindowFunction)DrawWindow, "My Mod Menu");
        }

        private void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (GUILayout.Toggle(_selectedTab == i, _tabs[i], "Button"))
                {
                    _selectedTab = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawEspTab(); break;
                case 1: DrawScriptsTab(); break;
                case 2: DrawCooldownTab(); break;
                case 3: DrawMiscTab(); break;
            }
        }

        private void DrawMiscTab()
        {
            if (_zoomHack == null)
            {
                GUILayout.Label("ZoomHack component not found!");
                return;
            }

            GUILayout.Label("<b>Minimap Zoom Extended</b>");
            _zoomHack.EnableZoomHack = GUILayout.Toggle(_zoomHack.EnableZoomHack, "Enable Extended Zoom");

            if (_zoomHack.EnableZoomHack)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Max Zoom: {_zoomHack.MaxZoomValue:F1}");
                _zoomHack.MaxZoomValue = GUILayout.HorizontalSlider(_zoomHack.MaxZoomValue, 10f, 50f);
                GUILayout.EndHorizontal();
                
                GUILayout.Label("Note: Higher values let you see further on the minimap.");
            }
        }
        
        private void DrawCooldownTab()
        {
            if (_cooldownOverlay == null)
            {
                GUILayout.Label("Cooldown Overlay not found!");
                return;
            }

            _cooldownOverlay.ShowOverlay = GUILayout.Toggle(_cooldownOverlay.ShowOverlay, "Enable Overlay");
            
            GUILayout.Space(10);
            GUILayout.Label("Manual Trackers Configuration");
            
            foreach (var tracker in _cooldownOverlay.Trackers)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(tracker.Label, GUILayout.Width(80));
                
                GUILayout.Label("Duration:", GUILayout.Width(60));
                float.TryParse(GUILayout.TextField(tracker.Duration.ToString("F1"), GUILayout.Width(40)), out tracker.Duration);
                
                GUILayout.Label("Key:", GUILayout.Width(30));
                GUILayout.Label(tracker.TriggerKey.ToString());
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Label("Use Numpad 7, 8, 9, + to trigger timers.");
        }

        private void DrawEspTab()
        {
            if (_espFeature == null)
            {
                GUILayout.Label("ESP Feature not found!");
                return;
            }

            _espFeature.IsEspActive = GUILayout.Toggle(_espFeature.IsEspActive, "Enable ESP");
            
            if (_espFeature.IsEspActive)
            {
                GUILayout.Space(10);
                GUILayout.Label("Visual Settings");
                _espFeature.ShowTracers = GUILayout.Toggle(_espFeature.ShowTracers, "Show Tracers");
                if (_espFeature.ShowTracers)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Origin:", GUILayout.Width(50));
                    if (GUILayout.Toggle(_espFeature.TracerOrigin == EspFeature.TracerOriginMode.Bottom, "Bottom", "Button")) 
                        _espFeature.TracerOrigin = EspFeature.TracerOriginMode.Bottom;
                    if (GUILayout.Toggle(_espFeature.TracerOrigin == EspFeature.TracerOriginMode.Center, "Center", "Button")) 
                        _espFeature.TracerOrigin = EspFeature.TracerOriginMode.Center;
                    GUILayout.EndHorizontal();
                }

                _espFeature.ShowBoxes = GUILayout.Toggle(_espFeature.ShowBoxes, "Show Boxes");
                
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Scan Radius: {_espFeature.ScanRadius:F0}m");
                _espFeature.ScanRadius = GUILayout.HorizontalSlider(_espFeature.ScanRadius, 10f, 200f);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Update Interval: {_espFeature.UpdateInterval:F2}s");
                _espFeature.UpdateInterval = GUILayout.HorizontalSlider(_espFeature.UpdateInterval, 0.1f, 2.0f);
                GUILayout.EndHorizontal();

                // Name Filters
                GUILayout.Space(10);
                GUILayout.Label("Entity Filters");
                _espFeature.UseFilters = GUILayout.Toggle(_espFeature.UseFilters, "Enable Name Filters (Partial Match)");

                if (_espFeature.UseFilters)
                {
                    GUILayout.BeginHorizontal();
                    newFilterText = GUILayout.TextField(newFilterText, GUILayout.Width(120));
                    if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrEmpty(newFilterText))
                    {
                        if (!_espFeature.FilterKeywords.Contains(newFilterText))
                            _espFeature.FilterKeywords.Add(newFilterText);
                        newFilterText = "";
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical("box");
                    for (int i = 0; i < _espFeature.FilterKeywords.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(_espFeature.FilterKeywords[i]);
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            _espFeature.FilterKeywords.RemoveAt(i);
                            i--;
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (_espFeature.FilterKeywords.Count == 0) GUILayout.Label("- No Name Filters -");
                    GUILayout.EndVertical();
                }

                GUILayout.Space(5);
                
                // Component Filters
                _espFeature.UseComponentFilter = GUILayout.Toggle(_espFeature.UseComponentFilter, "Enable Component Filters (Exact Class Name)");
                
                if (_espFeature.UseComponentFilter)
                {
                    GUILayout.BeginHorizontal();
                    newCompText = GUILayout.TextField(newCompText, GUILayout.Width(120));
                    if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrEmpty(newCompText))
                    {
                        if (!_espFeature.RequiredComponents.Contains(newCompText))
                            _espFeature.RequiredComponents.Add(newCompText);
                        newCompText = "";
                    }
                    GUILayout.EndHorizontal();

                     GUILayout.BeginVertical("box");
                    for (int i = 0; i < _espFeature.RequiredComponents.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(_espFeature.RequiredComponents[i]);
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            _espFeature.RequiredComponents.RemoveAt(i);
                            i--;
                        }
                        GUILayout.EndHorizontal();
                    }
                     if (_espFeature.RequiredComponents.Count == 0) GUILayout.Label("- No Component Filters -");
                    GUILayout.EndVertical();
                }
            }
        }

        private void DrawScriptsTab()
        {
            if (_scriptManager == null) return;

            GUILayout.BeginHorizontal();
            
            // Left Panel
            GUILayout.BeginVertical(GUILayout.Width(150));
            GUILayout.Label("Scripts:");
            
            foreach (var profile in _scriptManager.Profiles)
            {
                if (GUILayout.Button(profile.Name))
                {
                    _selectedProfile = profile;
                }
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Create New"))
            {
                var newProfile = new ScriptProfile { Name = "New Script " + (_scriptManager.Profiles.Count + 1) };
                _scriptManager.Profiles.Add(newProfile);
                _selectedProfile = newProfile;
            }
            
            if (GUILayout.Button("Save All"))
            {
                _scriptManager.SaveScripts();
            }
            GUILayout.EndVertical();

            // Right Panel
            GUILayout.BeginVertical("box");
            if (_selectedProfile != null)
            {
                DrawScriptEditor(_selectedProfile);
            }
            else
            {
                GUILayout.Label("Select a script to edit.");
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawScriptEditor(ScriptProfile profile)
        {
            GUILayout.Label($"Editing: {profile.Name}");
            profile.Name = GUILayout.TextField(profile.Name);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Trigger Key:");
            string keyName = ((KeyCode)profile.TriggerKey).ToString();
            if (GUILayout.Button(keyName))
            {
                profile.TriggerKey = (int)KeyCode.None;
            }
            try {
                string input = GUILayout.TextField(((KeyCode)profile.TriggerKey).ToString());
                if (Enum.TryParse<KeyCode>(input, true, out var result)) profile.TriggerKey = (int)result;
            } catch {}
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Actions:");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            
            for (int i = 0; i < profile.Actions.Count; i++)
            {
                var action = profile.Actions[i];
                GUILayout.BeginHorizontal("box");
                
                GUILayout.Label($"{i + 1}. {action.Type}", GUILayout.Width(80));

                if (action.Type == ScriptAction.ActionType.KeyPress)
                {
                     string k = ((KeyCode)action.KeyCodeValue).ToString();
                     string newK = GUILayout.TextField(k, GUILayout.Width(60));
                     if (Enum.TryParse<KeyCode>(newK, true, out var r)) action.KeyCodeValue = (int)r;
                     
                     GUILayout.Label("Dur:", GUILayout.Width(30));
                     float.TryParse(GUILayout.TextField(action.Duration.ToString("F2"), GUILayout.Width(40)), out action.Duration);
                }
                else if (action.Type == ScriptAction.ActionType.Wait)
                {
                    GUILayout.Label("Seconds:", GUILayout.Width(60));
                    float.TryParse(GUILayout.TextField(action.Duration.ToString("F2"), GUILayout.Width(40)), out action.Duration);
                }
                else if (action.Type == ScriptAction.ActionType.MouseClick)
                {
                    GUILayout.Label("Btn (0=L,1=R):", GUILayout.Width(90));
                    int.TryParse(GUILayout.TextField(action.MouseButton.ToString(), GUILayout.Width(20)), out action.MouseButton);
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    profile.Actions.RemoveAt(i);
                    i--;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Key")) profile.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.KeyPress, KeyCodeValue = (int)KeyCode.K, Duration = 0.1f });
            if (GUILayout.Button("+ Wait")) profile.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.Wait, Duration = 0.5f });
            if (GUILayout.Button("+ Click")) profile.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.MouseClick, MouseButton = 0, Duration = 0.1f });
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            if (GUILayout.Button("Delete Script", GUILayout.Width(100)))
            {
                _scriptManager.Profiles.Remove(profile);
                _selectedProfile = null;
            }
        }
    }
}

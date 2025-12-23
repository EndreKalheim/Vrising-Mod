using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using System.Text.Json; 

namespace MyScriptMod
{
    public class ScriptAction
    {
        public enum ActionType { KeyPress, Wait, MouseClick }

        public ActionType Type;
        public int KeyCodeValue; // cast to KeyCode
        public float Duration;
        public int MouseButton; // 0, 1, 2
        public bool IsDown; 
    }

    public class ScriptProfile
    {
        public string Name = "New Script";
        public int TriggerKey = (int)KeyCode.None;
        public List<ScriptAction> Actions = new List<ScriptAction>();
    }

    public class ScriptDatabase
    {
        public List<ScriptProfile> Scripts = new List<ScriptProfile>();
    }

    public class ScriptManager : MonoBehaviour
    {
        public static ScriptManager Instance;
        public List<ScriptProfile> Profiles = new List<ScriptProfile>();
        private string _savePath;

        private void Awake()
        {
            Instance = this;
            _savePath = Path.Combine(Paths.ConfigPath, "MyScriptMod_Scripts.json");
            LoadScripts();
        }

        private void Update()
        {
            foreach (var profile in Profiles)
            {
                if (profile.TriggerKey != (int)KeyCode.None)
                {
                    if (Input.GetKeyDown((KeyCode)profile.TriggerKey))
                    {
                        StartCoroutine(ExecuteScript(profile).WrapToIl2Cpp());
                    }
                }
            }
        }

        public void SaveScripts()
        {
            try 
            {
                ScriptDatabase db = new ScriptDatabase { Scripts = Profiles };
                var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
                string json = JsonSerializer.Serialize(db, options);
                File.WriteAllText(_savePath, json);
                Debug.Log($"Scripts saved to {_savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save scripts: {e.Message}");
            }
        }

        public void LoadScripts()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    var options = new JsonSerializerOptions { IncludeFields = true };
                    ScriptDatabase db = JsonSerializer.Deserialize<ScriptDatabase>(json, options);
                    if (db != null && db.Scripts != null)
                    {
                        Profiles = db.Scripts;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load scripts: {e.Message}");
                }
            }
            
            if (Profiles.Count == 0)
            {
                var example = new ScriptProfile { Name = "Example Macro", TriggerKey = (int)KeyCode.K };
                example.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.KeyPress, KeyCodeValue = (int)KeyCode.O, Duration = 0.1f });
                example.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.Wait, Duration = 0.5f });
                example.Actions.Add(new ScriptAction { Type = ScriptAction.ActionType.MouseClick, MouseButton = 0, Duration = 0.1f });
                Profiles.Add(example);
            }
        }

        private IEnumerator ExecuteScript(ScriptProfile profile)
        {
            Plugin.Logger.LogInfo($"Executing Script: {profile.Name}");

            foreach (var action in profile.Actions)
            {
                switch (action.Type)
                {
                    case ScriptAction.ActionType.Wait:
                        yield return new WaitForSeconds(action.Duration);
                        break;

                    case ScriptAction.ActionType.KeyPress:
                        // Convert KeyCode to ScanCode (Example: 'O' is 0x18)
                        ushort sc = (ushort)Win32.GetScanCode((KeyCode)action.KeyCodeValue); 
                        Win32.SendScanCode(sc, true);
                        yield return new WaitForSeconds(action.Duration);
                        Win32.SendScanCode(sc, false);
                        break;

                    case ScriptAction.ActionType.MouseClick:
                        uint flagDown = 0; 
                        uint flagUp = 0;
                        uint data = 0;

                        if (action.MouseButton == 3) { flagDown = Win32.MOUSEEVENTF_XDOWN; flagUp = Win32.MOUSEEVENTF_XUP; data = Win32.XBUTTON1; } // Mouse 4
                        else if (action.MouseButton == 0) { flagDown = 0x0002; flagUp = 0x0004; } // Left
                        else if (action.MouseButton == 1) { flagDown = 0x0008; flagUp = 0x0010; } // Right

                        if (flagDown != 0)
                        {
                            Win32.SendMouseInput(flagDown, data);
                            yield return new WaitForSeconds(action.Duration > 0 ? action.Duration : 0.05f);
                            Win32.SendMouseInput(flagUp, data);
                        }
                        break;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using Unity.Entities;
using Unity.Collections;
using ProjectM;
using ProjectM.Network;

namespace MyScriptMod
{
    public class CooldownTracker
    {
        public int ID;
        public string Label; // "Q", "E", "SPC"
        public int SlotIndex; // Index in AbilityGroupSlotBuffer
        public Rect WindowRect;
        
        // Dynamic State
        public float RemainingTime;
        public float MaxDuration;
        public string AbilityName; // Debug info
    }

    public class CooldownData
    {
        public string Name;
        public float Remaining;
        public float MaxDuration;
    }

    public class CooldownOverlay : MonoBehaviour
    {
        public static CooldownOverlay Instance;
        public bool ShowOverlay = true;
        
        public List<CooldownTracker> Trackers = new List<CooldownTracker>();
        private string configPath => System.IO.Path.Combine(Application.persistentDataPath, "MyScriptMod_Cooldowns_V2.txt");

        public bool ShowDebug = false;
        private string _debugLog = "";

        private World _clientWorld;
        private EntityManager _entityManager;
        private Entity _localPlayerEntity = Entity.Null;
        private string[] _slotNames = new string[] {"", "", "", "", "", "", "", "", "", ""}; 

        private void Awake()
        {
             Instance = this;
        }

        private void Start()
        {
            LoadConfig();
            
            // If config didn't load (empty list), create defaults
            if (Trackers.Count == 0)
            {
                // Slot Indices: W1=1, W2=4, Spc=2, R=5, C=6, Ult=7
                float cx = Screen.width / 2f;
                float cy = Screen.height - 150f;

                Trackers.Add(new CooldownTracker { ID=101, Label="1", SlotIndex=0, WindowRect=new Rect(cx-180, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID=102, Label="Q", SlotIndex=1,  WindowRect=new Rect(cx-120, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID=103, Label="E", SlotIndex=4,  WindowRect=new Rect(cx-60, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID=104, Label="SPC", SlotIndex=2,WindowRect=new Rect(cx, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID=105, Label="R", SlotIndex=5,  WindowRect=new Rect(cx+60, cy, 60, 60) }); 
                Trackers.Add(new CooldownTracker { ID=106, Label="C", SlotIndex=6,  WindowRect=new Rect(cx+120, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID=107, Label="ULT", SlotIndex=7,WindowRect=new Rect(cx+180, cy, 60, 60) });
            }
        }

        private void OnDestroy()
        {
            SaveConfig();
        }

        private void SaveConfig()
        {
            List<string> lines = new List<string>();
            foreach(var t in Trackers)
            {
                lines.Add($"{t.ID}:{t.WindowRect.x}:{t.WindowRect.y}");
            }
            try {
                System.IO.File.WriteAllText(configPath, string.Join("|", lines));
            } catch {}
        }

        private void LoadConfig()
        {
            if (!System.IO.File.Exists(configPath)) return;
            try {
                string data = System.IO.File.ReadAllText(configPath);
                string[] items = data.Split('|');
                var tempPos = new Dictionary<int, Vector2>();
                foreach(var item in items) {
                    string[] parts = item.Split(':');
                    if (parts.Length == 3 && int.TryParse(parts[0], out int id)) {
                         if (float.TryParse(parts[1], out float tx) && float.TryParse(parts[2], out float ty))
                             tempPos[id] = new Vector2(tx, ty);
                    }
                }
                
                // Defaults must be created first if list empty
                 if (Trackers.Count == 0) {
                     float cx = Screen.width / 2f; float cy = Screen.height - 150f;
                    Trackers.Add(new CooldownTracker { ID=101, Label="1", SlotIndex=0, WindowRect=new Rect(cx-180, cy, 60, 60) });
                    Trackers.Add(new CooldownTracker { ID=102, Label="Q", SlotIndex=1,  WindowRect=new Rect(cx-120, cy, 60, 60) });
                    Trackers.Add(new CooldownTracker { ID=103, Label="E", SlotIndex=4,  WindowRect=new Rect(cx-60, cy, 60, 60) });
                    Trackers.Add(new CooldownTracker { ID=104, Label="SPC", SlotIndex=2,WindowRect=new Rect(cx, cy, 60, 60) });
                    Trackers.Add(new CooldownTracker { ID=105, Label="R", SlotIndex=5,  WindowRect=new Rect(cx+60, cy, 60, 60) }); 
                    Trackers.Add(new CooldownTracker { ID=106, Label="C", SlotIndex=6,  WindowRect=new Rect(cx+120, cy, 60, 60) });
                    Trackers.Add(new CooldownTracker { ID=107, Label="ULT", SlotIndex=7,WindowRect=new Rect(cx+180, cy, 60, 60) });
                 }

                 foreach(var kvp in tempPos) {
                     var t = Trackers.Find(x => x.ID == kvp.Key);
                     if (t != null) t.WindowRect = new Rect(kvp.Value.x, kvp.Value.y, t.WindowRect.width, t.WindowRect.height);
                 }

            } catch {}
        }
        
        public void ResetPosition()
        {
             Trackers.Clear();
             Start(); 
        }

        private void Update()
        {
            if (!Plugin.IsEnabled) return;
            try {
                ScanSlotsAndCooldowns();
            } catch { }
        }

        private void ScanSlotsAndCooldowns()
        {
            InitializeWorld();
            if (_clientWorld == null || !_clientWorld.IsCreated) return;
            
            // 1. Server Time
            double currentServerTime = 0;
            var stQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ProjectM.ServerTime>());
            if (!stQuery.IsEmpty)
            {
                var stEntity = stQuery.GetSingletonEntity();
                currentServerTime = _entityManager.GetComponentData<ProjectM.ServerTime>(stEntity).Time;
            }
            else return;

            // 2. Player
            if (_localPlayerEntity == Entity.Null || !_entityManager.Exists(_localPlayerEntity))
            {
                var pdq = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalCharacter>());
                if (pdq.IsEmpty) return;
                _localPlayerEntity = pdq.GetSingletonEntity();
            }

            // 3. Map Slots
            if (_entityManager.HasComponent<AbilityGroupSlotBuffer>(_localPlayerEntity))
            {
                var buffer = _entityManager.GetBuffer<AbilityGroupSlotBuffer>(_localPlayerEntity);
                for (int i = 0; i < Math.Min(buffer.Length, 15); i++)
                {
                    Entity groupEntity = buffer[i].GroupSlotEntity._Entity;
                    string derivedName = "";
                    if (_entityManager.Exists(groupEntity) && _entityManager.HasComponent<AbilityGroupSlot>(groupEntity))
                    {
                        var slotData = _entityManager.GetComponentData<AbilityGroupSlot>(groupEntity);
                        Entity stateEnt = slotData.StateEntity._Entity;
                        if (_entityManager.Exists(stateEnt) && _entityManager.HasComponent<Stunlock.Core.PrefabGUID>(stateEnt))
                        {
                            var guid = _entityManager.GetComponentData<Stunlock.Core.PrefabGUID>(stateEnt);
                            if (AbilityDatabase.TryGetName(guid, out string n)) derivedName = n;
                            else derivedName = $"ID: {guid.GuidHash}";
                        }
                    }
                    if (i < _slotNames.Length) _slotNames[i] = derivedName ?? "";
                }
            }

            // 4. Update Each Tracker
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<AbilityCooldownState>(), ComponentType.ReadOnly<EntityOwner>());
            var entities = query.ToEntityArray(Allocator.Temp);
            
            List<CooldownData> activeCDs = new List<CooldownData>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var e in entities)
            {
                 if (!_entityManager.Exists(e)) continue;
                 var ownerData = _entityManager.GetComponentData<EntityOwner>(e);
                 bool valid = (ownerData.Owner.Index == _localPlayerEntity.Index || ownerData.Owner.Index == 0);
                 if (!valid) continue;

                 var cd = _entityManager.GetComponentData<AbilityCooldownState>(e);
                 if (cd.CooldownEndTime > 0)
                 {
                      double rem = cd.CooldownEndTime - currentServerTime;
                      float dur = 8f;
                      string name = "";
                      if (_entityManager.HasComponent<Stunlock.Core.PrefabGUID>(e)) {
                          var guid = _entityManager.GetComponentData<Stunlock.Core.PrefabGUID>(e);
                          if (AbilityDatabase.TryGetName(guid, out name)) dur = AbilityDatabase.GetDuration(guid);
                          else name = $"ID: {guid.GuidHash}";
                      }
                      
                      if (rem > -2.0 && rem < 600.0) {
                          activeCDs.Add(new CooldownData { Name = name, Remaining = (float)rem, MaxDuration = dur });
                          if (ShowDebug) sb.AppendLine($"GUID: {name} | Rem: {rem:F1}");
                      }
                 }
            }
            entities.Dispose();
            _debugLog = sb.ToString();

            // Match Trackers to Cooldowns
            foreach(var t in Trackers)
            {
                t.RemainingTime = -999f; 
                
                string targetName = "";
                if (t.SlotIndex < _slotNames.Length) targetName = _slotNames[t.SlotIndex];
                
                // Special Override for 'R' and 'C' if slots are non-standard?
                // Just trust the buffer for now.
                
                t.AbilityName = targetName; 

                if (!string.IsNullOrEmpty(targetName))
                {
                    foreach(var cd in activeCDs)
                    {
                        if (!string.IsNullOrEmpty(cd.Name) && (cd.Name == targetName || cd.Name.Contains(targetName)))
                        {
                            t.RemainingTime = cd.Remaining;
                            t.MaxDuration = cd.MaxDuration;
                            break;
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!Plugin.IsEnabled) return;
            if (!ShowOverlay) return;

            bool unlocked = Menu.UnlockUI;

            for (int i=0; i < Trackers.Count; i++)
            {
                var t = Trackers[i];
                bool show = unlocked || (t.RemainingTime > -2.0f && t.RemainingTime < 600.0f && t.RemainingTime != -999f);
                
                if (show)
                {
                    GUI.backgroundColor = unlocked ? new Color(0,0,0,0.5f) : Color.clear;
                    GUI.contentColor = Color.white; 
                    t.WindowRect = GUI.Window(t.ID, t.WindowRect, (GUI.WindowFunction)DrawSingleTracker, "");
                }
            }
            
            if (unlocked)
            {
                if (GUI.Button(new Rect(Screen.width/2 - 50, 10, 100, 30), "Toggle Debug")) 
                    ShowDebug = !ShowDebug;
            }

            if (ShowDebug)
            {
                GUI.backgroundColor = Color.black;
                GUI.Window(99999, new Rect(10, 10, 400, 800), (GUI.WindowFunction)DrawDebugWindow, "DEBUG DATA");
            }
        }

        private void DrawDebugWindow(int id)
        {
            GUI.DragWindow(new Rect(0,0, 10000, 20));
            GUILayout.BeginVertical();
            GUILayout.Label("<b>SLOTS (From AbilityGroupSlotBuffer)</b>");
            for(int i=0; i< _slotNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(_slotNames[i]))
                    GUILayout.Label($"[{i}] {_slotNames[i]}");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>ACTIVE COOLDOWNS</b>");
            GUILayout.Label(_debugLog);

            GUILayout.EndVertical();
        }

        private void DrawSingleTracker(int id)
        {
            var t = Trackers.Find(x => x.ID == id);
            if (t == null) return;

            if (Menu.UnlockUI)
            {
                GUI.DragWindow(new Rect(0,0, 10000, 20));
                GUI.Label(new Rect(0,0,t.WindowRect.width, 20), t.Label, CenteredStyle());
                GUI.Label(new Rect(0,25,t.WindowRect.width, 20), "DRAG", CenteredStyle());
                return;
            }

            float rem = t.RemainingTime;
            float max = t.MaxDuration;
            if (max < 0.1f) max = 1f;

            Color c = Color.gray;
            string text = "";

            if (rem > 0)
            {
                 float ratio = rem / max;
                 if (ratio > 0.5f) c = new Color(0.8f, 0.2f, 0.2f, 0.9f); 
                 else if (rem > 2.0f) c = new Color(0.9f, 0.8f, 0.2f, 0.9f);
                 else c = new Color(0.2f, 0.8f, 0.3f, 0.9f); 
                 text = rem.ToString("F1");
            }
            else if (rem > -2.0f) 
            {
                 c = new Color(0.2f, 1.0f, 0.4f, 1.0f);
                 text = "RDY";
            }
            
            var old = GUI.color;
            GUI.color = c;
            GUI.Box(new Rect(0,0, t.WindowRect.width, t.WindowRect.height), "");
            
            GUI.color = Color.white;
            GUI.Label(new Rect(0,0, t.WindowRect.width, t.WindowRect.height), text, CenteredStyle(16));
            GUI.Label(new Rect(0, t.WindowRect.height - 15, t.WindowRect.width, 15), t.Label, CenteredStyle(10));
            GUI.color = old;
        }

        private GUIStyle _style;
        private GUIStyle CenteredStyle(int size = 12)
        {
            if (_style == null) _style = new GUIStyle();
            _style.alignment = TextAnchor.MiddleCenter;
            _style.fontSize = size;
            _style.fontStyle = FontStyle.Bold;
            _style.normal.textColor = Color.white;
            return _style;
        }
        
        private void InitializeWorld()
        {
            if (_clientWorld == null)
            {
                foreach(var w in World.All)
                {
                    if (w.Name == "Client_0")
                    {
                        _clientWorld = w;
                        _entityManager = w.EntityManager;
                        break;
                    }
                }
            }
        }
    }
}
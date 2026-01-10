using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;

namespace MyScriptMod
{
    public class CooldownTracker
    {
        public int ID;
        public string Label;
        public int SlotIndex;
        public Rect WindowRect;
        public float RemainingTime;
        public float MaxDuration;
        public string AbilityName;
    }

    public class CooldownOverlay : MonoBehaviour
    {
        public static CooldownOverlay Instance;
        public bool ShowOverlay = true;
        private List<CooldownTracker> Trackers = new List<CooldownTracker>();
        private string configPath => System.IO.Path.Combine(Application.persistentDataPath, "MyScriptMod_Cooldowns_V2.txt");

        public bool ShowDebug = false;
        private string _debugLog = "";
        private World _clientWorld;
        private EntityManager _entityManager;
        private Entity _localPlayerEntity = Entity.Null;
        private bool _initialized = false;

        private void Awake()
        {
            Instance = this;
        }

        private void EnsureTrackersInitialized()
        {
            if (_initialized || Screen.width <= 0) return;

            LoadConfig();

            if (Trackers.Count == 0)
            {
                float cx = Screen.width / 2f;
                float cy = Screen.height - 150f;
                Trackers.Add(new CooldownTracker { ID = 101, Label = "1", SlotIndex = 0, WindowRect = new Rect(cx - 180, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID = 102, Label = "Q", SlotIndex = 1, WindowRect = new Rect(cx - 120, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID = 103, Label = "E", SlotIndex = 4, WindowRect = new Rect(cx - 60, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID = 104, Label = "SPC", SlotIndex = 2, WindowRect = new Rect(cx, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID = 105, Label = "R", SlotIndex = 5, WindowRect = new Rect(cx + 60, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID = 106, Label = "C", SlotIndex = 6, WindowRect = new Rect(cx + 120, cy, 60, 60) });
                Trackers.Add(new CooldownTracker { ID = 107, Label = "ULT", SlotIndex = 7, WindowRect = new Rect(cx + 180, cy, 60, 60) });
            }
            _initialized = true;
        }

        private void Update()
        {
            if (!Plugin.IsEnabled) return;
            EnsureTrackersInitialized();
            
            try {
                ScanSlotsAndCooldowns();
            } catch { }
        }

        private void ScanSlotsAndCooldowns()
        {
            InitializeWorld();
            if (_clientWorld == null || !_clientWorld.IsCreated) return;

            var stQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ServerTime>());
            if (stQuery.IsEmpty) return;
            
            var stEntity = stQuery.GetSingletonEntity();
            double currentServerTime = _entityManager.GetComponentData<ServerTime>(stEntity).Time;

            var pdq = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalCharacter>());
            if (pdq.IsEmpty) return;
            _localPlayerEntity = pdq.GetSingletonEntity();

            if (!_entityManager.HasComponent<AbilityGroupSlotBuffer>(_localPlayerEntity)) return;
            
            var buffer = _entityManager.GetBuffer<AbilityGroupSlotBuffer>(_localPlayerEntity);
            System.Text.StringBuilder debugSb = new System.Text.StringBuilder();

            for (int i = 0; i < Math.Min(buffer.Length, 15); i++)
            {
                Entity groupEntity = buffer[i].GroupSlotEntity._Entity;
                float foundRemaining = -999f;
                float foundDuration = 5.0f;
                string abilityName = "Unknown";

                if (_entityManager.Exists(groupEntity) && _entityManager.HasComponent<AbilityGroupSlot>(groupEntity))
                {
                    var slotData = _entityManager.GetComponentData<AbilityGroupSlot>(groupEntity);
                    Entity stateEnt = slotData.StateEntity._Entity;

                    if (_entityManager.Exists(stateEnt))
                    {
                        // Get Name
                        if (_entityManager.HasComponent<PrefabGUID>(stateEnt))
                        {
                            var guid = _entityManager.GetComponentData<PrefabGUID>(stateEnt);
                            abilityName = $"ID: {guid.GuidHash}";
                        }

                        // Get Cooldown
                        if (_entityManager.HasComponent<AbilityCooldownState>(stateEnt))
                        {
                            var cd = _entityManager.GetComponentData<AbilityCooldownState>(stateEnt);
                            foundRemaining = (float)(cd.CooldownEndTime - currentServerTime);
                        }
                    }
                }

                foreach (var t in Trackers)
                {
                    if (t.SlotIndex == i)
                    {
                        t.RemainingTime = foundRemaining;
                        t.MaxDuration = foundDuration;
                        t.AbilityName = abilityName;
                    }
                }
            }
            if (ShowDebug) _debugLog = debugSb.ToString();
        }

        private void OnGUI()
        {
            if (!Plugin.IsEnabled || !ShowOverlay || !_initialized) return;

            for (int i = 0; i < Trackers.Count; i++)
            {
                var t = Trackers[i];
                bool show = Menu.UnlockUI || (t.RemainingTime > -1.5f && t.RemainingTime < 600.0f && t.RemainingTime != -999f);
                if (show)
                {
                    GUI.backgroundColor = Menu.UnlockUI ? new Color(0, 0, 0, 0.5f) : Color.clear;
                    t.WindowRect = GUI.Window(t.ID, t.WindowRect, (GUI.WindowFunction)DrawSingleTracker, "");
                }
            }
        }

        private void DrawSingleTracker(int id)
        {
            var t = Trackers.Find(x => x.ID == id);
            if (t == null) return;

            if (Menu.UnlockUI)
            {
                GUI.DragWindow(new Rect(0, 0, 10000, 20));
                GUI.Label(new Rect(0, 0, t.WindowRect.width, 20), new GUIContent(t.Label), GetStyle(12));
                GUI.Label(new Rect(0, 25, t.WindowRect.width, 20), new GUIContent("DRAG"), GetStyle(10));
                return;
            }

            string text = t.RemainingTime > 0 ? t.RemainingTime.ToString("F1") : "RDY";
            Color c = t.RemainingTime > 0 ? new Color(1f, 0.4f, 0.4f, 1f) : new Color(0.4f, 1f, 0.4f, 1f);

            var oldColor = GUI.color;
            GUI.color = c;
            GUI.Box(new Rect(0, 0, t.WindowRect.width, t.WindowRect.height), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 0, t.WindowRect.width, t.WindowRect.height), new GUIContent(text), GetStyle(16));
            GUI.color = oldColor;
        }

        private GUIStyle _style;
        private GUIStyle GetStyle(int size)
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
            if (_clientWorld != null && _clientWorld.IsCreated) return;
            foreach (var w in World.All)
            {
                if (w.Name == "Client_0")
                {
                    _clientWorld = w;
                    _entityManager = w.EntityManager;
                    break;
                }
            }
        }

        private void LoadConfig() 
        { 
            if (!System.IO.File.Exists(configPath)) return;
            try {
                string data = System.IO.File.ReadAllText(configPath);
                string[] items = data.Split('|');
                foreach(var item in items) {
                    string[] parts = item.Split(':');
                    if (parts.Length == 3 && int.TryParse(parts[0], out int id)) {
                        var t = Trackers.Find(x => x.ID == id);
                        if (t != null && float.TryParse(parts[1], out float tx) && float.TryParse(parts[2], out float ty))
                            t.WindowRect = new Rect(tx, ty, t.WindowRect.width, t.WindowRect.height);
                    }
                }
            } catch {}
        }
        
        private void SaveConfig() 
        {
            List<string> lines = new List<string>();
            foreach(var t in Trackers) lines.Add($"{t.ID}:{t.WindowRect.x}:{t.WindowRect.y}");
            try { System.IO.File.WriteAllText(configPath, string.Join("|", lines)); } catch {}
        }

        private void OnDestroy() { SaveConfig(); }
        public void ResetPosition() { Trackers.Clear(); _initialized = false; EnsureTrackersInitialized(); }
    }
}
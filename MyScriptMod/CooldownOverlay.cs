using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
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
        public float RemainingTime = -999f;
        public float MaxDuration = 1f;
        public string AbilityName = "";
    }

    public class CooldownOverlay : MonoBehaviour
    {
        public static CooldownOverlay Instance;
        public bool ShowOverlay = true;
        private List<CooldownTracker> Trackers = new List<CooldownTracker>();
        private string configPath => System.IO.Path.Combine(Application.persistentDataPath, "MyScriptMod_Cooldowns_V2.txt");

        // Settings
        public bool ForceDebug = false; 
        private string _debugLog_Slots = "";
        private string _debugLog_Active = "";

        private World _clientWorld;
        private EntityManager _entityManager;
        private Entity _localPlayerEntity = Entity.Null;
        private bool _initialized = false;
        
        private void Awake() { Instance = this; }

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
            // PRESS F3 TO TOGGLE DEBUG MANUALLY
            if (Input.GetKeyDown(KeyCode.F3)) ForceDebug = !ForceDebug;

            if (!Plugin.IsEnabled) return;
            EnsureTrackersInitialized();
            try { ScanSlotsAndCooldowns(); } catch { }
        }

        private void ScanSlotsAndCooldowns()
        {
            InitializeWorld();
            if (_clientWorld == null) return;

            // 1. Get Server Time
            var stQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<ServerTime>());
            if (stQuery.IsEmpty) return;
            double currentServerTime = _entityManager.GetComponentData<ServerTime>(stQuery.GetSingletonEntity()).Time;

            // 2. Find Player
            var userQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>(), ComponentType.ReadOnly<LocalUser>());
            if (userQuery.IsEmpty) return;
            Entity userEntity = userQuery.GetSingletonEntity();
            User userData = _entityManager.GetComponentData<User>(userEntity);
            _localPlayerEntity = userData.LocalCharacter._Entity;

            // --- STEP 3: MAP SLOTS TO GUIDS ---
            Dictionary<int, List<PrefabGUID>> slotMap = new Dictionary<int, List<PrefabGUID>>();

            if (_entityManager.HasComponent<AbilityGroupSlotBuffer>(_localPlayerEntity))
            {
                var buffer = _entityManager.GetBuffer<AbilityGroupSlotBuffer>(_localPlayerEntity);
                for (int i = 0; i < Math.Min(buffer.Length, 15); i++)
                {
                    Entity groupEntity = buffer[i].GroupSlotEntity._Entity;
                    if (_entityManager.Exists(groupEntity) && _entityManager.HasComponent<AbilityGroupSlot>(groupEntity))
                    {
                        var slotData = _entityManager.GetComponentData<AbilityGroupSlot>(groupEntity);
                        Entity stateEnt = slotData.StateEntity._Entity;

                        if (_entityManager.Exists(stateEnt) && _entityManager.HasBuffer<AbilityGroupStartAbilitiesBuffer>(stateEnt))
                        {
                            var abilities = _entityManager.GetBuffer<AbilityGroupStartAbilitiesBuffer>(stateEnt);
                            List<PrefabGUID> guidsInThisSlot = new List<PrefabGUID>();
                            
                            foreach (var ability in abilities)
                            {
                                // TRY THIS: .PrefabGUID
                                guidsInThisSlot.Add(ability.PrefabGUID);
                            }
                            slotMap[i] = guidsInThisSlot;
                        }
                    }
                }
            }

            // Reset trackers
            foreach (var t in Trackers) t.RemainingTime = -999f;

            // 4. Scan World for Cooldowns
            var cdQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<AbilityCooldownState>(), ComponentType.ReadOnly<EntityOwner>());
            NativeArray<Entity> entities = cdQuery.ToEntityArray(Allocator.Temp);

            foreach (var e in entities)
            {
                var owner = _entityManager.GetComponentData<EntityOwner>(e);
                if (owner.Owner.Index != _localPlayerEntity.Index && owner.Owner.Index != 0) continue;

                if (!_entityManager.HasComponent<PrefabGUID>(e)) continue;
                PrefabGUID activeCooldownGuid = _entityManager.GetComponentData<PrefabGUID>(e);

                var cd = _entityManager.GetComponentData<AbilityCooldownState>(e);
                float rem = (float)(cd.CooldownEndTime - currentServerTime);

                if (rem > -0.5f && rem < 600.0f)
                {
                    float dur = 8f; 

                    // --- MATCHING LOGIC ---
                    foreach(var kvp in slotMap)
                    {
                        int slotIndex = kvp.Key;
                        List<PrefabGUID> abilities = kvp.Value;

                        if (abilities.Contains(activeCooldownGuid))
                        {
                            foreach (var t in Trackers)
                            {
                                if (t.SlotIndex == slotIndex)
                                {
                                    if (rem > t.RemainingTime)
                                    {
                                        t.RemainingTime = rem;
                                        t.MaxDuration = dur;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            entities.Dispose();
        }

        private void OnGUI()
        {
            if (!Plugin.IsEnabled || !_initialized) return;

            foreach (var t in Trackers)
            {
                bool active = t.RemainingTime > 0.05f;
                bool dragging = Menu.UnlockUI;

                if (active || dragging)
                {
                    GUI.backgroundColor = dragging ? new Color(0,0,0,0.5f) : Color.clear;
                    t.WindowRect = GUI.Window(t.ID, t.WindowRect, (GUI.WindowFunction)DrawSingleTracker, "");
                }
            }

            if (ForceDebug)
            {
                GUI.backgroundColor = new Color(0, 0, 0, 0.9f);
                GUI.Window(99991, new Rect(20, 20, 300, 450), (GUI.WindowFunction)DrawDebugWindow, "F3 - DEBUG MONITOR");
            }
        }

        private void DrawDebugWindow(int id)
        {
            GUI.DragWindow(new Rect(0,0,10000,20));
            GUILayout.Label("<color=yellow>HOTBAR (Target Names):</color>");
            GUILayout.Label(string.IsNullOrEmpty(_debugLog_Slots) ? "Empty" : _debugLog_Slots);
            GUILayout.Space(10);
            GUILayout.Label("<color=cyan>ACTIVE (Found in World):</color>");
            GUILayout.Label(string.IsNullOrEmpty(_debugLog_Active) ? "None" : _debugLog_Active);
        }

        private void DrawSingleTracker(int id)
        {
            var t = Trackers.Find(x => x.ID == id);
            if (t == null) return;

            if (Menu.UnlockUI)
            {
                GUI.DragWindow(new Rect(0,0,10000,20));
                GUI.Label(new Rect(0,0, t.WindowRect.width, 20), t.Label, GetStyle(12));
                return;
            }

            string text = t.RemainingTime.ToString("F1");
            GUI.color = t.RemainingTime > 2.0f ? new Color(1, 0.3f, 0.3f) : new Color(0.3f, 1, 0.5f);
            GUI.Box(new Rect(0,0, t.WindowRect.width, t.WindowRect.height), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(0,0, t.WindowRect.width, t.WindowRect.height), text, GetStyle(18));
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
            if (_clientWorld != null) return;
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

        public void ResetPosition() { Trackers.Clear(); _initialized = false; EnsureTrackersInitialized(); }

        private void LoadConfig() 
        { 
            if (!System.IO.File.Exists(configPath)) return;
            try {
                string data = System.IO.File.ReadAllText(configPath);
                string[] items = data.Split('|');
                foreach(var item in items) {
                    string[] parts = item.Split(':');
                    if (parts.Length == 3 && int.TryParse(parts[0], out int id)) {
                        float tx = float.Parse(parts[1]);
                        float ty = float.Parse(parts[2]);
                        Trackers.Add(new CooldownTracker { 
                            ID = id, 
                            Label = GetLabelByID(id), 
                            SlotIndex = GetSlotByID(id),
                            WindowRect = new Rect(tx, ty, 60, 60) 
                        });
                    }
                }
            } catch {}
        }

        private string GetLabelByID(int id) => id switch { 101=>"1", 102=>"Q", 103=>"E", 104=>"SPC", 105=>"R", 106=>"C", 107=>"ULT", _=>"?" };
        private int GetSlotByID(int id) => id switch { 101=>0, 102=>1, 103=>4, 104=>2, 105=>5, 106=>6, 107=>7, _=>0 };

        private void SaveConfig() 
        {
            List<string> lines = new List<string>();
            foreach(var t in Trackers) lines.Add($"{t.ID}:{t.WindowRect.x}:{t.WindowRect.y}");
            try { System.IO.File.WriteAllText(configPath, string.Join("|", lines)); } catch {}
        }

        private void OnDestroy() { SaveConfig(); }
    }
}
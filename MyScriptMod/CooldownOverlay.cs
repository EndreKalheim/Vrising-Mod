using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;

namespace MyScriptMod
{
    public class CooldownOverlay : MonoBehaviour
    {
        public bool ShowOverlay = true;
        
        // Window 1: My Cooldowns
        public Rect MyRect = new Rect(50, Screen.height - 300, 200, 250);
        
        // Window 2: Enemy Cooldowns
        public Rect EnemyRect = new Rect(Screen.width - 250, 100, 230, 300);

        // Configuration
        public List<CooldownTracker> MyTrackers = new List<CooldownTracker>();
        public List<CooldownTracker> EnemyTrackers = new List<CooldownTracker>();

        // State
        private GameObject _currentTarget;
        private Camera _cam;

        private void Start()
        {
            // --- Setup MY Cooldowns (Triggers when I press my own keys) ---
            // You can adjust these keys to match your actual keybinds
            MyTrackers.Add(new CooldownTracker { Label = "My Dash", Duration = 8f, TriggerKey = KeyCode.Space });
            MyTrackers.Add(new CooldownTracker { Label = "My Counter (Q)", Duration = 8f, TriggerKey = KeyCode.Q });

            // --- Setup ENEMY Cooldowns (Manual triggers via Keypad) ---
            EnemyTrackers.Add(new CooldownTracker { Label = "Enemy Q", Duration = 8f, TriggerKey = KeyCode.Keypad7 });
            EnemyTrackers.Add(new CooldownTracker { Label = "Enemy E", Duration = 8f, TriggerKey = KeyCode.Keypad8 });
            EnemyTrackers.Add(new CooldownTracker { Label = "Enemy Space", Duration = 10f, TriggerKey = KeyCode.Keypad9 });
            EnemyTrackers.Add(new CooldownTracker { Label = "Enemy Ult", Duration = 120f, TriggerKey = KeyCode.KeypadPlus });
        }

        private void Update()
        {
            if (!ShowOverlay) return;
            if (_cam == null) _cam = Camera.main;

            // 1. Update My Cooldowns
            UpdateTrackers(MyTrackers);

            // 2. Find Enemy Under Mouse
            FindTargetUnderMouse();

            // 3. Update Enemy Cooldowns
            // If we press a trigger key, we reset the timer
            UpdateTrackers(EnemyTrackers);
        }

        private void FindTargetUnderMouse()
        {
            // Simple logic: Find closest "Player" to the mouse position
            if (_cam == null) return;

            float bestDist = 150f; // Max pixel distance from mouse
            GameObject bestTarget = null;
            Vector2 mousePos = Input.mousePosition;

            // Reuse the logic from your EspFeature if possible, or simple lookup
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                // Basic filter for Enemy Players
                if (!go.name.Contains("Vampire") && !go.name.Contains("Player")) continue;
                if (go.transform.position == _cam.transform.position) continue; // Ignore self (rough check)

                Vector3 screenPos = _cam.WorldToScreenPoint(go.transform.position);
                if (screenPos.z < 0) continue; // Behind camera

                float dist = Vector2.Distance(mousePos, new Vector2(screenPos.x, screenPos.y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = go;
                }
            }

            _currentTarget = bestTarget;
        }

        private void UpdateTrackers(List<CooldownTracker> list)
        {
            foreach (var tracker in list)
            {
                if (Input.GetKeyDown(tracker.TriggerKey))
                {
                    tracker.CurrentTime = tracker.Duration;
                }

                if (tracker.CurrentTime > 0)
                {
                    tracker.CurrentTime -= Time.deltaTime;
                    if (tracker.CurrentTime < 0) tracker.CurrentTime = 0;
                }
            }
        }

        private void OnGUI()
        {
            if (!ShowOverlay) return;

            GUI.backgroundColor = new Color(0, 0, 0, 0.7f);

            // Draw My Window
            MyRect = GUI.Window(10, MyRect, (GUI.WindowFunction)DrawMyWindow, "My Cooldowns");

            // Draw Enemy Window
            string title = _currentTarget != null ? $"Target: {_currentTarget.name}" : "No Target";
            EnemyRect = GUI.Window(11, EnemyRect, (GUI.WindowFunction)DrawEnemyWindow, title);
        }

        private void DrawMyWindow(int id) { DrawTrackerList(MyTrackers); GUI.DragWindow(); }
        private void DrawEnemyWindow(int id) { DrawTrackerList(EnemyTrackers); GUI.DragWindow(); }

        private void DrawTrackerList(List<CooldownTracker> list)
        {
            GUILayout.BeginVertical();
            foreach (var tracker in list)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(tracker.Label, GUILayout.Width(80));

                Rect r = GUILayoutUtility.GetRect(100, 20);
                GUI.Box(r, ""); 

                if (tracker.CurrentTime > 0)
                {
                    float pct = tracker.CurrentTime / tracker.Duration;
                    
                    var prevColor = GUI.color;
                    GUI.color = Color.Lerp(Color.green, Color.red, pct);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width * pct, r.height), Texture2D.whiteTexture);
                    GUI.color = prevColor;

                    TextAnchor originalAlignment = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(r, tracker.CurrentTime.ToString("F1"));
                    GUI.skin.label.alignment = originalAlignment;
                }
                else
                {
                    var prevContentColor = GUI.contentColor;
                    GUI.contentColor = Color.green;
                    TextAnchor originalAlignment = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(r, "READY");
                    GUI.skin.label.alignment = originalAlignment;
                    GUI.contentColor = prevContentColor;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndVertical();
        }
    }

    public class CooldownTracker
    {
        public string Label;
        public float Duration;
        public KeyCode TriggerKey;
        public float CurrentTime;
    }
}
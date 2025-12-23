using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;

namespace MyScriptMod
{
    public class CooldownOverlay : MonoBehaviour
    {
        public bool ShowOverlay = true;
        public Rect OverlayRect = new Rect(Screen.width - 220, 100, 200, 300);

        // Configuration for tracking ENEMY cooldowns manually
        // We trigger these with keys when we see an enemy use a skill
        public List<CooldownTracker> Trackers = new List<CooldownTracker>();

        private void Start()
        {
            // Default Trackers covering standard V Rising slots
            Trackers.Add(new CooldownTracker { Label = "Enemy Q", Duration = 8f, TriggerKey = KeyCode.Keypad7 });
            Trackers.Add(new CooldownTracker { Label = "Enemy E", Duration = 8f, TriggerKey = KeyCode.Keypad8 });
            Trackers.Add(new CooldownTracker { Label = "Enemy Space", Duration = 10f, TriggerKey = KeyCode.Keypad9 });
            Trackers.Add(new CooldownTracker { Label = "Enemy Ult (R)", Duration = 120f, TriggerKey = KeyCode.KeypadPlus });
        }

        private void Update()
        {
            if (!ShowOverlay) return;

            foreach (var tracker in Trackers)
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

            GUI.backgroundColor = new Color(0, 0, 0, 0.6f);
            OverlayRect = GUI.Window(1, OverlayRect, (GUI.WindowFunction)DrawWindow, "Cooldown Tracker");
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            GUILayout.BeginVertical();

            foreach (var tracker in Trackers)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(tracker.Label, GUILayout.Width(80));

                Rect r = GUILayoutUtility.GetRect(100, 20);
                GUI.Box(r, ""); // Background of the bar

                if (tracker.CurrentTime > 0)
                {
                    float pct = tracker.CurrentTime / tracker.Duration;
                    
                    // Draw the progress bar
                    var prevColor = GUI.color;
                    GUI.color = Color.Lerp(Color.green, Color.red, pct);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width * pct, r.height), Texture2D.whiteTexture);
                    GUI.color = prevColor;

                    // FIX: Instead of passing a GUIStyle object (which triggers the IntPtr error),
                    // we modify the global skin alignment temporarily.
                    TextAnchor originalAlignment = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    
                    GUI.Label(r, tracker.CurrentTime.ToString("F1"));
                    
                    GUI.skin.label.alignment = originalAlignment;
                }
                else
                {
                    // Draw "READY" in green
                    var prevContentColor = GUI.contentColor;
                    GUI.contentColor = Color.green;
                    
                    TextAnchor originalAlignment = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    
                    GUI.Label(r, "READY");
                    
                    GUI.skin.label.alignment = originalAlignment;
                    GUI.contentColor = prevContentColor;
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5);
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

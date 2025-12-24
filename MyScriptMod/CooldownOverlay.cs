using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyScriptMod
{
    public class CooldownOverlay : MonoBehaviour
    {
        public bool ShowOverlay = true;
        public List<CooldownTracker> Trackers = new List<CooldownTracker>();

        private void Start()
        {
            // My Abilities (Standard Keys)
            Trackers.Add(new CooldownTracker(100, "Q", KeyCode.Q, 8f, new Rect(Screen.width/2 - 60, Screen.height - 100, 80, 80)));
            Trackers.Add(new CooldownTracker(101, "E", KeyCode.E, 8f, new Rect(Screen.width/2 + 30, Screen.height - 100, 80, 80)));
            Trackers.Add(new CooldownTracker(102, "Spc", KeyCode.Space, 8f, new Rect(Screen.width/2 - 15, Screen.height - 180, 80, 80)));
            
            // Mouse Abilities (Extra)
            Trackers.Add(new CooldownTracker(103, "M4", KeyCode.Mouse3, 8f, new Rect(Screen.width/2 - 140, Screen.height - 100, 80, 80)));
            Trackers.Add(new CooldownTracker(104, "M5", KeyCode.Mouse4, 8f, new Rect(Screen.width/2 + 110, Screen.height - 100, 80, 80)));
        }

        private void Update()
        {
            foreach (var t in Trackers)
            {
                if (Input.GetKeyDown(t.TriggerKey)) t.CurrentTime = t.Duration;
                if (t.CurrentTime > 0) t.CurrentTime -= Time.deltaTime;
            }
        }

        private void OnGUI()
        {
            if (!ShowOverlay) return;

            foreach (var t in Trackers)
            {
                // Logic for showing: only if cooldown active OR menu says UnlockUI is on
                if (t.CurrentTime > 0 || Menu.UnlockUI)
                {
                    GUI.backgroundColor = Menu.UnlockUI ? new Color(1, 0, 0, 0.5f) : Color.clear;
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
                // Drag Handle (Header)
                GUI.color = Color.cyan;
                GUI.DrawTexture(new Rect(0, 0, 80, 25), Texture2D.whiteTexture);
                GUI.color = Color.black; 
                GUI.Label(new Rect(0, 0, 80, 25), "DRAG");
                GUI.color = Color.white;

                // Drag Logic (Must be AFTER drawing if we want to see it, but DragWindow usually consumes events so it might be tricky.
                // Actually GUI.DragWindow should be called last for the window, but inside the callback it's usually first. 
                // We keep it first logic-wise, but visual-wise the header represents it).
                GUI.DragWindow(new Rect(0, 0, 10000, 25)); 

                // Value Display
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(0, 30, 80, 20), t.Duration.ToString("F1") + "s");
                
                // Buttons (0.1s adjustment)
                if (GUI.Button(new Rect(5, 55, 30, 20), "-")) t.Duration -= 0.1f;
                if (GUI.Button(new Rect(45, 55, 30, 20), "+")) t.Duration += 0.1f;
                return;
            }

            // Visual for Cooldown
            GUI.Box(new Rect(0, 0, 50, 50), "");
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontSize = 20;
            
            if (t.CurrentTime > 0)
            {
                 GUI.color = Color.red;
                 GUI.Label(new Rect(0, 0, 50, 50), t.CurrentTime.ToString("F0"));
            }
            else
            {
                 GUI.color = Color.green;
                 GUI.Label(new Rect(0, 0, 50, 50), "RDY");
            }

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 12;
            GUI.Label(new Rect(0, 35, 50, 15), t.Label);
        }
    }

    public class CooldownTracker
    {
        public int ID;
        public string Label;
        public KeyCode TriggerKey;
        public float Duration;
        public float CurrentTime;
        public Rect WindowRect;

        public CooldownTracker(int id, string label, KeyCode key, float dur, Rect r)
        {
            ID = id; Label = label; TriggerKey = key; Duration = dur; WindowRect = r;
        }
    }
}
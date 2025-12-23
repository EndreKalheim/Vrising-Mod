using System.Collections.Generic;
using UnityEngine;

namespace MyScriptMod
{
    public class EspFeature : MonoBehaviour
    {
        public bool IsEspActive = false;
        public bool ShowTracers = true;
        public bool ShowBoxes = false;
        public float ScanRadius = 80f;
        public float UpdateInterval = 1.0f; // Faster update for PvP
        
        // Updated Filter Keywords for V Rising
        public List<string> FilterKeywords = new List<string>() 
        { 
            "VampireMale", "VampireFemale", "HYB_Vampire", "HYB_Creature", "CHAR_", "Player" 
        };

        private List<Transform> _cachedTargets = new List<Transform>();
        private float _lastScanTime;
        private Camera _mainCamera;

        private void Update()
        {
            if (!IsEspActive) return;
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (Time.time - _lastScanTime >= UpdateInterval)
            {
                ScanForTargets();
                _lastScanTime = Time.time;
            }
        }

        private void ScanForTargets()
        {
            _cachedTargets.Clear();
            if (_mainCamera == null) return;

            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            Vector3 myPos = _mainCamera.transform.position;

            foreach (var obj in allObjects)
            {
                if (obj == null) continue;

                // 1. DISTANCE CHECK
                float dist = Vector3.Distance(myPos, obj.transform.position);
                
                // IGNORE SELF: If it's within 1.5m, it's likely your own character or spotlight
                if (dist < 2.0f) continue; 
                if (dist > ScanRadius) continue;

                // 2. NAME FILTER
                bool isMatch = false;
                foreach (var k in FilterKeywords)
                {
                    if (obj.name.Contains(k)) { isMatch = true; break; }
                }

                if (isMatch) _cachedTargets.Add(obj.transform);
            }
        }

        private void OnGUI()
        {
            if (!IsEspActive || _mainCamera == null) return;

            foreach (var target in _cachedTargets)
            {
                if (target == null) continue;

                Vector3 screenPos = _mainCamera.WorldToScreenPoint(target.position);
                if (screenPos.z > 0) // Target is in front of camera
                {
                    float guiX = screenPos.x;
                    float guiY = Screen.height - screenPos.y;

                    // Draw Line
                    if (ShowTracers)
                    {
                        DrawSimpleLine(new Vector2(Screen.width / 2, Screen.height), new Vector2(guiX, guiY), Color.red);
                    }

                    // Draw Text with shadow (to make it readable)
                    string info = $"{target.name.Replace("(Clone)", "")} [{(int)screenPos.z}m]";
                    GUI.color = Color.black;
                    GUI.Label(new Rect(guiX + 1, guiY - 21, 150, 20), info); // Shadow
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(guiX, guiY - 20, 150, 20), info);
                }
            }
        }

        private void DrawSimpleLine(Vector2 start, Vector2 end, Color color)
        {
            GUI.color = color;
            var matrix = GUI.matrix;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(start, end);
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y, length, 2f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }
    }
}
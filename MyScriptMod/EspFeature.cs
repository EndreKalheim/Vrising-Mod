using System.Collections.Generic;
using UnityEngine;

namespace MyScriptMod
{
    public class EspFeature : MonoBehaviour
    {
        public bool IsEspActive = false;
        public bool ShowTracers = true;
        public bool ShowBoxes = true;
        
        public float ScanRadius = 150f;
        public float IgnoreRadius = 4.0f; // Radius around camera to ignore (Local Player)
        public float UpdateInterval = 0.5f; 
        
        // Exact names based on your request + standard players
        public List<string> FilterKeywords = new List<string>() 
        { 
            "VampireMale", 
            "VampireFemale", 
            "HYB_Vampire_S",      // Transformation
            "HYB_CreatureSp",     // Transformation 
            "HYB_CreatureHorse",  // Horse
            "Player"              // Fallback
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

            // Find all objects
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            Vector3 camPos = _mainCamera.transform.position;

            foreach (var obj in allObjects)
            {
                if (obj == null) continue;

                string objName = obj.name;

                // 1. FILTERING (Optimization: Check Name First)
                
                // IGNORE: Spotlights (prevents double text)
                if (objName.Contains("Spotlight") || objName.Contains("Point Light")) continue;

                // MATCH: Check against keyword list
                bool isMatch = false;
                foreach (var k in FilterKeywords)
                {
                    if (objName.Contains(k)) 
                    { 
                        isMatch = true; 
                        break; 
                    }
                }
                if (!isMatch) continue;

                // 2. DISTANCE CHECKS
                float dist = Vector3.Distance(camPos, obj.transform.position);
                
                // IGNORE SELF: If it is too close to the camera logic, it's you.
                // Adjust "IgnoreRadius" in menu if you still see yourself.
                if (dist < IgnoreRadius) continue; 
                
                // Max Range Check
                if (dist > ScanRadius) continue;

                _cachedTargets.Add(obj.transform);
            }
        }

        private void OnGUI()
        {
            if (!IsEspActive || _mainCamera == null) return;

            foreach (var target in _cachedTargets)
            {
                if (target == null) continue;

                Vector3 worldPos = target.position;
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0) // In front of camera
                {
                    float guiX = screenPos.x;
                    float guiY = Screen.height - screenPos.y; // Invert Y for GUI

                    // Calculate Distance
                    float dist = Vector3.Distance(_mainCamera.transform.position, worldPos);

                    // --- DRAW TRACERS (FROM CENTER) ---
                    if (ShowTracers)
                    {
                        // Screen.height / 2 is the center Y
                        DrawSimpleLine(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(guiX, guiY), Color.red);
                    }

                    // --- DRAW BOXES (2D APPROXIMATION) ---
                    if (ShowBoxes)
                    {
                        // Estimate Head Position (approx 2 units up)
                        Vector3 headPos = worldPos + Vector3.up * 2.2f; 
                        Vector3 screenHead = _mainCamera.WorldToScreenPoint(headPos);
                        float headY = Screen.height - screenHead.y;

                        float boxHeight = guiY - headY;
                        float boxWidth = boxHeight / 2f;

                        DrawBoxOutline(new Rect(guiX - (boxWidth/2), headY, boxWidth, boxHeight), Color.green, 1);
                    }

                    // --- DRAW TEXT ---
                    string cleanName = target.name
                        .Replace("CHAR_", "")
                        .Replace("HYB_", "")
                        .Replace("(Clone)", "")
                        .Replace("Vampire", "");
                        
                    string info = $"{cleanName}\n[{Mathf.RoundToInt(dist)}m]";
                    
                    GUI.color = Color.black;
                    GUI.Label(new Rect(guiX + 1, guiY - 39, 150, 40), info); // Shadow
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(guiX, guiY - 40, 150, 40), info);
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
            GUI.DrawTexture(new Rect(start.x, start.y, length, 1f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }

        private void DrawBoxOutline(Rect rect, Color color, float width)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y, width, rect.height), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), Texture2D.whiteTexture); // Right
        }
    }
}
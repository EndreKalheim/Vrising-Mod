using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;

namespace MyScriptMod
{
    public class EspFeature : MonoBehaviour
    {
        public enum TracerOriginMode { Bottom, Center }

        public bool IsEspActive = false;
        public bool ShowTracers = true;
        public bool ShowBoxes = false;
        public float ScanRadius = 50f;
        public float UpdateInterval = 3.0f; // Increased to prevent stutter (Process is heavy)
        public TracerOriginMode TracerOrigin = TracerOriginMode.Center;
        
        public bool UseFilters = true;
        public List<string> FilterKeywords = new List<string>() { "Player", "CHAR_", "Vampire", "Hero" };
        
        // New: Filter by finding a component by name (string)
        // This helps find "PlayerCharacter" or "Vampire" specifically
        public bool UseComponentFilter = false;
        public List<string> RequiredComponents = new List<string>();

        private List<Transform> _cachedTargets = new List<Transform>();
        private float _lastScanTime;
        private Camera _mainCamera;

        // Simple line material for drawing
        private static Material _lineMaterial;

        private void Start()
        {
            CreateLineMaterial();
        }

        private void Update()
        {
            if (!IsEspActive) return;

            if (Time.time - _lastScanTime >= UpdateInterval)
            {
                ScanForTargets();
                _lastScanTime = Time.time;
            }

            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void ScanForTargets()
        {
            _cachedTargets.Clear();
            
            // FIX: Find the actual Local Player, not (0,0,0)
            // In V Rising, finding the local player can be tricky, but we can 
            // look for the object that has the Main Camera attached or a Player component.
            GameObject localPlayer = GameObject.Find("LocalPlayer"); // Placeholder - V Rising usually names it something else
            Vector3 myPos = (localPlayer != null) ? localPlayer.transform.position : (_mainCamera != null ? _mainCamera.transform.position : Vector3.zero);

            // OPTIMIZATION: Instead of ALL transforms, let's look for specific things.
            // If you find the internal name for players (e.g., "PlayerCharacter"), put it in RequiredComponents.
            var allObjects = GameObject.FindObjectsOfType<GameObject>(); 

            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                
                float dist = Vector3.Distance(myPos, obj.transform.position);
                if (dist < ScanRadius && dist > 1.0f) 
                {
                    // Name Filter
                    if (UseFilters) {
                        bool match = false;
                        foreach (var k in FilterKeywords) if (obj.name.Contains(k)) match = true;
                        if (!match) continue;
                    }
                    
                    // Simple self-exclusion
                    if (dist < 1.5f) continue;

                    // Component Filter
                    if (UseComponentFilter) {
                        bool hasComp = false;
                        foreach (var c in RequiredComponents) if (obj.GetComponent(c) != null) hasComp = true;
                        if (!hasComp) continue;
                    }

                    _cachedTargets.Add(obj.transform);
                }
            }
        }
        private void OnGUI()
        {
            if (!IsEspActive || _mainCamera == null) return;

            foreach (var target in _cachedTargets)
            {
                if (target == null) continue;

                Vector3 targetPos = target.position;
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(targetPos);

                if (screenPos.z > 0)
                {
                    Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);

                    if (ShowTracers)
                    {
                        Vector2 startPos = (TracerOrigin == TracerOriginMode.Center) 
                            ? new Vector2(Screen.width / 2, Screen.height / 2)
                            : new Vector2(Screen.width / 2, Screen.height);
                        
                        DrawLine(startPos, guiPos, Color.red, 2f);
                    }

                    if (ShowBoxes)
                    {
                         float size = 1000f / screenPos.z; 
                         size = Mathf.Clamp(size, 10f, 100f);
                         Rect rect = new Rect(guiPos.x - size / 2, guiPos.y - size / 2, size, size);
                         DrawBox(rect, Color.red, 2f);
                    }
                    
                    GUI.Label(new Rect(guiPos.x, guiPos.y - 20, 100, 20), $"{target.name} {(int)screenPos.z}m");
                }
            }
        }

        private void CreateLineMaterial()
        {
            if (_lineMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _lineMaterial = new Material(shader);
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _lineMaterial.SetInt("_ZWrite", 0);
            }
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Texture2D tex = Texture2D.whiteTexture;
            var matrix = GUI.matrix;
            
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = (end - start).magnitude;
            
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.color = color;
            GUI.DrawTexture(new Rect(start.x, start.y, length, width), tex);
            
            GUI.matrix = matrix; 
        }

        private void DrawBox(Rect rect, Color color, float width)
        {
            Vector2 p1 = new Vector2(rect.x, rect.y);
            Vector2 p2 = new Vector2(rect.x + rect.width, rect.y);
            Vector2 p3 = new Vector2(rect.x + rect.width, rect.y + rect.height);
            Vector2 p4 = new Vector2(rect.x, rect.y + rect.height);

            DrawLine(p1, p2, color, width);
            DrawLine(p2, p3, color, width);
            DrawLine(p3, p4, color, width);
            DrawLine(p4, p1, color, width);
        }
    }
}

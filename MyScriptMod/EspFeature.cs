using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace MyScriptMod
{
    public class EspFeature : MonoBehaviour
    {
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
        public struct POINT { public int X; public int Y; }

        public bool IsEspActive = false;
        public bool EnableAimAssist = false;
        public bool ShowTracers = true;
        public bool ShowBoxes = true;
        
        public float ScanRadius = 300f; // Default higher
        public float IgnoreRadius = 2.0f;
        public float SelfCylinderRadius = 3.0f; 
        public float AimFov = 200f; 
        public float AimSmoothness = 5f; // Restored
        public float PredictionAmount = 5.0f; // Restored (just in case)
        public bool ShowDebug = false; // Toggle to see exclusion zone

        // Strictly the filters you asked for
        public List<string> FilterKeywords = new List<string>() { "Vampire", "CreatureHorse" };

        private List<Transform> _cachedTargets = new List<Transform>();
        private Camera _mainCamera;
        private float _lastScanTime;
        
        // Debug Data
        private Vector3? _debugSpotlightPos;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) IsEspActive = !IsEspActive;
            if (Input.GetKeyDown(KeyCode.F2)) EnableAimAssist = !EnableAimAssist;

            if (_mainCamera == null) _mainCamera = Camera.main;

            if (Time.time - _lastScanTime >= 0.5f)
            {
                ScanForTargets();
                _lastScanTime = Time.time;
            }

            if (EnableAimAssist && Input.GetKey(KeyCode.Mouse1))
            {
                DoTopDownAim();
            }
        }

        private void ScanForTargets()
        {
            _cachedTargets.Clear();
            var all = GameObject.FindObjectsOfType<GameObject>();
            
            // 1. Find Local Player via Spotlight
            // DEBUG: Log if we find it or not
            Vector3? localPlayerPos = null;
            
            // Try to find ANY spotlight for now, since names might vary
            // Or fallback to Camera position on ground plane
            
            var potentialSpotlight = GameObject.Find("PlayerSpotlight"); // Try exact name first
            if (potentialSpotlight != null) 
            {
                localPlayerPos = potentialSpotlight.transform.position;
            }
            else
            {
                // Backup search through all list (broader "Spotlight" search)
                foreach (var go in all)
                {
                    if (go.name.Contains("Spotlight")) 
                    {
                        localPlayerPos = go.transform.position;
                        break;
                    }
                }
                
                // FALLBACK: If still nothing, find the NEAREST "Vampire" or "Player" to the camera
                if (!localPlayerPos.HasValue)
                {
                    float closestDist = 9999f;
                    GameObject closestObj = null;
                    foreach (var go in all)
                    {
                        if (go.name.Contains("Vampire") || go.name.Contains("Player"))
                        {
                            float d = Vector3.Distance(go.transform.position, _mainCamera.transform.position);
                            if (d < closestDist && d < 10.0f) // Must be VERY close (assumed local player)
                            {
                                closestDist = d;
                                closestObj = go;
                            }
                        }
                    }
                    if (closestObj != null) localPlayerPos = closestObj.transform.position;
                }
            }

            // Fallback for Debug: If we can't find spotlight, use Camera position projected to ground
            if (!localPlayerPos.HasValue && _mainCamera != null)
            {
                // Raycast to ground? Or just assume ground is y=0 relative to something.
                // Simplified: Camera + Forward vector on ground
                 Ray r = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
                 if (Physics.Raycast(r, out RaycastHit hit, 100f))
                 {
                     _debugSpotlightPos = hit.point; // Visual debug ONLY
                 }
            }
            else
            {
                _debugSpotlightPos = localPlayerPos;
            }

            // 2. Filter & Collect Targets
            foreach (var go in all)
            {
                if (go == null) continue;
                
                string n = go.name;
                // Exclusions
                if (n.Contains("Hair") || n.Contains("Face")) continue;

                // Inclusions
                bool isMatch = false;
                foreach (var k in FilterKeywords) if (n.Contains(k)) { isMatch = true; break; }
                if (!isMatch) continue;

                Vector3 targetPos = go.transform.position;

                // Ignore Self (based on Spotlight) - CYLINDER CHECK
                if (localPlayerPos.HasValue)
                {
                    Vector2 spotXZ = new Vector2(localPlayerPos.Value.x, localPlayerPos.Value.z);
                    Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
                    if (Vector2.Distance(spotXZ, targetXZ) < SelfCylinderRadius) continue;
                }

                // Range Limit Restored (Fixed 400m limit to prevent off-map tracers)
                float dist = Vector3.Distance(_mainCamera.transform.position, targetPos);
                if (dist < IgnoreRadius || dist > 400f) continue;

                // De-duplication: overlapping text check
                bool valid = true;
                foreach (var existing in _cachedTargets)
                {
                    if (existing == null) continue;
                    if (Vector3.Distance(existing.position, targetPos) < 0.5f)
                    {
                        valid = false;
                        break;
                    } 
                }

                if (valid) _cachedTargets.Add(go.transform);
            }
        }

        private void DoTopDownAim()
        {
            Transform best = null;
            float closestToMouse = AimFov;
            GetCursorPos(out POINT currentP);
            Vector2 mouseScreen = new Vector2(currentP.X, Screen.height - currentP.Y);

            foreach (var t in _cachedTargets)
            {
                if (t == null) continue;
                Vector3 sPos = _mainCamera.WorldToScreenPoint(t.position);
                if (sPos.z < 0) continue;

                float dist = Vector2.Distance(mouseScreen, new Vector2(sPos.x, sPos.y));
                if (dist < closestToMouse) { closestToMouse = dist; best = t; }
            }

            if (best != null)
            {
                Vector3 sPos = _mainCamera.WorldToScreenPoint(best.position);
                float targetX = sPos.x;
                float targetY = Screen.height - sPos.y;

                float nextX = Mathf.Lerp(currentP.X, targetX, 1f / AimSmoothness);
                float nextY = Mathf.Lerp(currentP.Y, targetY, 1f / AimSmoothness);
                SetCursorPos((int)nextX, (int)nextY);
            }
        }

        private void OnGUI()
        {
            if (!IsEspActive || _mainCamera == null) return;

            foreach (var t in _cachedTargets)
            {
                if (t == null) continue;
                Vector3 worldPos = t.position;
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0) 
                {
                    float guiX = screenPos.x;
                    float guiY = Screen.height - screenPos.y;

                    // 1. TRACERS - Fixed to Screen Center
                    if (ShowTracers)
                    {
                        DrawLine(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(guiX, guiY), Color.red);
                    }

                    // 2. BOXES - Old Style 2D Approximation (Red)
                    if (ShowBoxes)
                    {
                        Vector3 headPos = worldPos + Vector3.up * 2.2f; 
                        Vector3 screenHead = _mainCamera.WorldToScreenPoint(headPos);
                        float headY = Screen.height - screenHead.y;

                        float boxHeight = guiY - headY;
                        float boxWidth = boxHeight / 1.5f; // Slightly wider for V-Rising models

                        DrawBoxOutline(new Rect(guiX - (boxWidth/2), headY, boxWidth, boxHeight), Color.red, 2f);
                    }

                    // Label
                    GUI.color = Color.white;
                    GUI.Label(new Rect(guiX, guiY + 5, 150, 20), t.name.Replace("(Clone)",""));
                }
            }

            // DEBUG DRAWING
            if (ShowDebug && _debugSpotlightPos.HasValue)
            {
                Vector3 center = _debugSpotlightPos.Value;
                // We draw a rough 2D circle on the screen to show where the exclusion cylinder is
                DrawWorldCircle(center, SelfCylinderRadius, Color.yellow);
            }
        }

        private void DrawWorldCircle(Vector3 center, float radius, Color color)
        {
            // Simple approximation: Draw 8 points around the center
            Vector3 prevPos = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= 16; i++)
            {
                float angle = i * (360f / 16f) * Mathf.Deg2Rad;
                Vector3 nextPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                // Convert to screen
                Vector3 screenA = _mainCamera.WorldToScreenPoint(prevPos);
                Vector3 screenB = _mainCamera.WorldToScreenPoint(nextPos);

                if (screenA.z > 0 && screenB.z > 0)
                {
                    DrawLine(new Vector2(screenA.x, Screen.height - screenA.y), 
                             new Vector2(screenB.x, Screen.height - screenB.y), color);
                }
                prevPos = nextPos;
            }
        }

        private void DrawBoxOutline(Rect rect, Color color, float thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            GUI.color = color;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(start, end);
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y, len, 1.5f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }
    }
}
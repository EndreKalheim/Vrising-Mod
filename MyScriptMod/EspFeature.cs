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
        public bool ShowOnlyAimTargets = false; // F3 Toggle logic
        
        public float ScanRadius = 400f; 
        public float IgnoreRadius = 2.0f;
        public float SelfCylinderRadius = 0.5f; // Hardcoded as requested
        public float AimFov = 200f; // Reduced for semi-close aiming
        public float AimSmoothness = 15f; // Higher usually means faster in "Lerp(..., dt * Speed)"
        
        // Strictly the filters you asked for
        public List<string> FilterKeywords = new List<string>() { "Vampire", "CreatureHorse" };
        
        // Lists
        public HashSet<string> IgnoreList = new HashSet<string>();
        public HashSet<string> FriendList = new HashSet<string>();

        private List<Transform> _cachedTargets = new List<Transform>();
        private Camera _mainCamera;
        private float _lastScanTime;
        
        private string configPath => System.IO.Path.Combine(Application.persistentDataPath, "MyScriptMod_Config.json");

        private void Start()
        {
            LoadConfig();
        }

        private void OnDestroy()
        {
            SaveConfig();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) IsEspActive = !IsEspActive;
            if (Input.GetKeyDown(KeyCode.F2)) EnableAimAssist = !EnableAimAssist;
            if (Input.GetKeyDown(KeyCode.F3)) ShowOnlyAimTargets = !ShowOnlyAimTargets;
            if (Input.GetKeyDown(KeyCode.F4)) ShowTracers = !ShowTracers;
            
            // F5: Ignore Entity under mouse
            if (Input.GetKeyDown(KeyCode.F5))
            {
                ToggleList(IgnoreList, "Ignore");
            }
            
            // F6: Friend Entity under mouse
            if (Input.GetKeyDown(KeyCode.F6))
            {
                ToggleList(FriendList, "Friend");
            }

            if (_mainCamera == null) _mainCamera = Camera.main;

            if (Time.time - _lastScanTime >= 0.5f)
            {
                ScanForTargets();
                _lastScanTime = Time.time;
            }

            if (EnableAimAssist)
            {
                DoTopDownAim();
            }
        }
        
        private void ToggleList(HashSet<string> list, string listName)
        {
            Transform t = GetClosestTargetToMouse(100f); // 100px radius tolerance
            if (t != null)
            {
                string cleanName = t.name.Replace("(Clone)", "").Trim();
                if (list.Contains(cleanName))
                {
                    list.Remove(cleanName);
                    // Debug.Log($"Removed {cleanName} from {listName}");
                }
                else
                {
                    // If adding to one list, remove from the other to avoid conflicts
                    if (listName == "Ignore") FriendList.Remove(cleanName);
                    else IgnoreList.Remove(cleanName);
                    
                    list.Add(cleanName);
                    // Debug.Log($"Added {cleanName} to {listName}");
                }
                SaveConfig();
            }
        }
        
        private Transform GetClosestTargetToMouse(float radius)
        {
             if (_mainCamera == null) return null;
             Vector2 mouse = Input.mousePosition;
             Vector2 mouseGui = new Vector2(mouse.x, Screen.height - mouse.y);
             
             Transform best = null;
             float closest = radius;
             
             foreach(var t in _cachedTargets)
             {
                 if (t==null) continue;
                 Vector3 sPos = _mainCamera.WorldToScreenPoint(t.position);
                 if (sPos.z < 0) continue;
                 Vector2 sGui = new Vector2(sPos.x, Screen.height - sPos.y);
                 float d = Vector2.Distance(mouseGui, sGui);
                 if (d < closest) { closest = d; best = t; }
             }
             return best;
        }

        public void TestMouseMove()
        {
            // Moves mouse to center of screen
            SetCursorPos(Screen.width / 2, Screen.height / 2);
        }

        private void ScanForTargets()
        {
            _cachedTargets.Clear();
            var all = GameObject.FindObjectsOfType<GameObject>();
            
            // 1. Find Local Player via Spotlight (or fallback)
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


            // 2. Filter & Collect Targets
            foreach (var go in all)
            {
                if (go == null) continue;
                
                string n = go.name;
                string cleanName = n.Replace("(Clone)","").Trim();

                // Exclusions
                if (n.Contains("Hair") || 
                    n.Contains("Face") || 
                    n.Contains("HeadGear") || 
                    n.Contains("Dummy") || // Logic to ignore Dummies
                    IgnoreList.Contains(cleanName)) continue;

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
            if (_mainCamera == null) return;
 
            Transform best = null;
            float closestToMouse = AimFov;
            
            // Use Unity Mouse Position (Bottom-Left) converted to Top-Left for logic consistency with GUI
            Vector2 mousePosUnity = Input.mousePosition;
            Vector2 mouseGui = new Vector2(mousePosUnity.x, Screen.height - mousePosUnity.y);
 
            foreach (var t in _cachedTargets)
            {
                if (t == null) continue;
                
                // SKip Horses / Non-Players for AimAssist
                // Allow only VampireMale or VampireFemale
                if (!t.name.Contains("VampireMale") && !t.name.Contains("VampireFemale")) continue;

                // Skip Friends & Ignored
                string clean = t.name.Replace("(Clone)", "").Trim();
                if (IgnoreList.Contains(clean) || FriendList.Contains(clean)) continue;

                Vector3 sPos = _mainCamera.WorldToScreenPoint(t.position);
                if (sPos.z < 0) continue;
 
                // 2D distance on screen (GUI coords)
                Vector2 sPosGui = new Vector2(sPos.x, Screen.height - sPos.y);
                float dist = Vector2.Distance(mouseGui, sPosGui);
                
                if (dist < closestToMouse) { closestToMouse = dist; best = t; }
            }
 
            if (best != null)
            {
                // Aim at the "Center" of the target (approx height adjustment)
                // Lowered to 0.5f as requested to aim "a bit lower"
                Vector3 targetWorld = best.position + new Vector3(0, 0.5f, 0); 
                Vector3 sPos = _mainCamera.WorldToScreenPoint(targetWorld);
                
                float targetX = sPos.x;
                float targetY = Screen.height - sPos.y; // Unity Screen to GUI/Win32 SetCursorPos Y (Top-Down)

                // The OS SetCursorPos expects Top-Down screen coordinates.
                // Unity Input.mousePosition is Window-relative Bottom-Up.
                // We need to map Unity GUI coordinates (Window Relative) to Screen Coordinates if we use SetCursorPos?
                // Actually SetCursorPos is GLOBAL. If we are windowed, this is tricky.
                
                // However, user said "true crosshair is above circle".
                // If we use Input.mousePosition for circle, the circle will match the game cursor.
                // But SetCursorPos moves the OS cursor.
                // If we are Fullscreen, Window 0,0 is Screen 0,0.
                
                // Let's assume user is Fullscreen or Borderless.
                // GetCursorPos(out POINT p) -> This is GLOBAL.
                
                // Let's rely on the previous lerp but utilizing Unity mouse pos for the Start Point?
                // No, SetCursorPos needs Global.
                GetCursorPos(out POINT currentP); // We still need this for the "Current" OS position to lerp FROM.
                
                // But we must calculate "Target" in Global space too?
                // If WorldToScreenPoint returns window-relative...
                
                // If the user's circle was offset, it means GetCursorPos (Global) != OnGUI (Window).
                // Changing Circle to use Input.mousePosition (Window) fixes the visual Circle.
                
                // Now for the actual movement.
                // If we want to move cursor to 'targetX/Y' (which are Window coords),
                // we need to offset them by Window Position if we are windowed.
                // But we don't have window position easily.
                
                // IF we validly assume Fullscreen:
                float currentX = currentP.X;
                float currentY = currentP.Y;
                
                float lerpVal = Time.deltaTime * AimSmoothness;
                lerpVal = Mathf.Clamp01(lerpVal);
 
                float nextX = Mathf.Lerp(currentX, targetX, lerpVal);
                float nextY = Mathf.Lerp(currentY, targetY, lerpVal);
                
                SetCursorPos((int)nextX, (int)nextY);
            }
        }

        private void OnGUI()
        {
            if (!IsEspActive || _mainCamera == null) return;

            foreach (var t in _cachedTargets)
            {
                if (t == null) continue;

                // F3 Mode: If enabled, ONLY show what Aim Assist would track
                if (ShowOnlyAimTargets)
                {
                    if (!t.name.Contains("VampireMale") && !t.name.Contains("VampireFemale")) continue;
                    // Filter headgear also here, though already filtered in Scan...
                    if (t.name.Contains("HeadGear")) continue; 
                }

                Vector3 worldPos = t.position;
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0) 
                {
                    float guiX = screenPos.x;
                    float guiY = Screen.height - screenPos.y;

                    // 1. TRACERS - Fixed to Screen Center
                    if (ShowTracers)
                    {
                        Color distColor = Color.red;
                        if (FriendList.Contains(t.name.Replace("(Clone)","").Trim())) distColor = Color.green;
                        DrawLine(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(guiX, guiY), distColor);
                    }

                    // 2. BOXES - Old Style 2D Approximation (Red)
                    if (ShowBoxes)
                    {
                        Vector3 headPos = worldPos + Vector3.up * 2.2f; 
                        Vector3 screenHead = _mainCamera.WorldToScreenPoint(headPos);
                        float headY = Screen.height - screenHead.y;

                        float boxHeight = guiY - headY;
                        float boxWidth = boxHeight / 1.5f; // Slightly wider for V-Rising models

                        Color boxColor = Color.red;
                        if (FriendList.Contains(t.name.Replace("(Clone)","").Trim())) boxColor = Color.green;

                        DrawBoxOutline(new Rect(guiX - (boxWidth/2), headY, boxWidth, boxHeight), boxColor, 2f);
                    }
 
                    // Label
                    string labelName = t.name.Replace("(Clone)","");
                    if (FriendList.Contains(labelName.Trim())) GUI.color = Color.green;
                    else GUI.color = Color.white;
                    
                    GUI.Label(new Rect(guiX, guiY + 5, 150, 20), labelName);
                }
            }

                if (EnableAimAssist)
        {
            // Use Unity Mouse Position for Drawing (Matches Window Coordinates)
            Vector2 mousePosUnity = Input.mousePosition;
            Vector2 mouseGui = new Vector2(mousePosUnity.x, Screen.height - mousePosUnity.y);
            
            DrawCircleGUI(mouseGui, AimFov, Color.white);
            
            // Debug line is implicit by movement now.
        }
        }
    
    // Helper Methods for loading/saving (Manual serialization to avoid IL2CPP JSON issues)
    public void SaveConfig()
    {
        string ignore = string.Join(";;", IgnoreList);
        string friend = string.Join(";;", FriendList);
        // Format: IgnoreList|FriendList|AimFov|AimSmoothness
        string data = $"{ignore}||{friend}||{AimFov}||{AimSmoothness}";
        
        System.IO.File.WriteAllText(configPath, data);
    }

    public void LoadConfig()
    {
        if (System.IO.File.Exists(configPath))
        {
            string data = System.IO.File.ReadAllText(configPath);
            string[] parts = data.Split(new string[]{"||"}, System.StringSplitOptions.None);
            
            if (parts.Length >= 2)
            {
                IgnoreList.Clear();
                foreach(var s in parts[0].Split(new string[]{";;"}, System.StringSplitOptions.RemoveEmptyEntries)) IgnoreList.Add(s);
                
                FriendList.Clear();
                foreach(var s in parts[1].Split(new string[]{";;"}, System.StringSplitOptions.RemoveEmptyEntries)) FriendList.Add(s);
            }
            if (parts.Length >= 4)
            {
                float.TryParse(parts[2], out AimFov);
                float.TryParse(parts[3], out AimSmoothness);
            }
        }
    }

    private void DrawCircleGUI(Vector2 center, float radius, Color color)
    {
         GUI.color = color;
         Vector2 prev = center + new Vector2(radius, 0);
         for(int i=1; i<=16; i++) {
             float a = i * (360f/16f) * Mathf.Deg2Rad;
             Vector2 next = center + new Vector2(Mathf.Cos(a)*radius, Mathf.Sin(a)*radius);
             DrawLine(prev, next, color);
             prev = next;
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
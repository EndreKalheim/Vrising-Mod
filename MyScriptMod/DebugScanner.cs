using System;
using UnityEngine;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using ProjectM;

namespace MyScriptMod
{
    public class DebugScanner : MonoBehaviour
    {
        private World _clientWorld;
        private EntityManager _entityManager;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ScanForMapIcons();
            }
        }

        private void ScanForMapIcons()
        {
            InitializeWorld();
            if (_clientWorld == null) return;
            
            Plugin.Logger.LogInfo("--- SCANNING FOR MAP ICONS ---");

            // Strategy: Iterate ALL entities, look for "Map" or "Icon" components.
            // We want to identify the Component Type that makes things show on the minimap.
            
            var allEnts = _entityManager.GetAllEntities(Allocator.Temp);
            int count = 0;
            
            foreach(var e in allEnts)
            {
                var types = _entityManager.GetComponentTypes(e);
                bool isMapRelated = false;
                string foundType = "";
                
                foreach(var t in types)
                {
                    string name = t.ToString();
                    if (name.IndexOf("MapIcon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Minimap", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isMapRelated = true;
                        foundType = name;
                        break;
                    }
                }
                
                if (isMapRelated)
                {
                    // Check if it has a position
                    bool hasPos = _entityManager.HasComponent<LocalToWorld>(e) || _entityManager.HasComponent<Translation>(e);
                    
                    Plugin.Logger.LogInfo($"[Ent {e.Index}] Has {foundType} | HasPos: {hasPos}");
                    
                    // Limit output
                    count++;
                    if (count > 50) break;
                }
                
                types.Dispose();
            }
            allEnts.Dispose();
            
            if (count == 0) Plugin.Logger.LogInfo("No Map/Icon components found.");
        }

        private void InitializeWorld()
        {
            if (_clientWorld == null)
            {
                foreach(var w in World.All)
                {
                    if (w.Name == "Client_0")
                    {
                        _clientWorld = w;
                        _entityManager = w.EntityManager;
                        break;
                    }
                }
            }
        }
    }
}
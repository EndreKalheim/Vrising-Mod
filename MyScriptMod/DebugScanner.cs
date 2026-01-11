using System;
using Unity.Entities;
using Unity.Collections;
using ProjectM;
using ProjectM.Network;
using UnityEngine;
using BepInEx;

namespace MyScriptMod
{
    public class DebugScanner : MonoBehaviour
    {
        private World _clientWorld;
        private EntityManager _entityManager;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) RunDeepScan();
        }

        private void RunDeepScan()
        {
            InitializeWorld();
            if (_clientWorld == null) return;

            Plugin.Logger.LogInfo("=== CHECKING ABILITY BUFFERS (F11) ===");

            var userQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>(), ComponentType.ReadOnly<LocalUser>());
            if (userQuery.IsEmpty) return;
            
            Entity userEntity = userQuery.GetSingletonEntity();
            User userData = _entityManager.GetComponentData<User>(userEntity);
            Entity playerCharacter = userData.LocalCharacter._Entity;

            if (_entityManager.HasComponent<AbilityGroupSlotBuffer>(playerCharacter))
            {
                var buffer = _entityManager.GetBuffer<AbilityGroupSlotBuffer>(playerCharacter);

                // Check slots 0 to 6
                for (int i = 0; i < Math.Min(buffer.Length, 7); i++)
                {
                    var slotData = buffer[i];
                    Entity groupEnt = slotData.GroupSlotEntity._Entity;

                    if (_entityManager.HasComponent<AbilityGroupSlot>(groupEnt))
                    {
                        var groupSlotComp = _entityManager.GetComponentData<AbilityGroupSlot>(groupEnt);
                        Entity stateEnt = groupSlotComp.StateEntity._Entity;

                        if (_entityManager.Exists(stateEnt) && _entityManager.HasBuffer<AbilityGroupStartAbilitiesBuffer>(stateEnt))
                        {
                            var abilityBuffer = _entityManager.GetBuffer<AbilityGroupStartAbilitiesBuffer>(stateEnt);
                            
                            Plugin.Logger.LogInfo($"--- SLOT {i} CONTAINS ---");
                            foreach(var abilityItem in abilityBuffer)
                            {
                                // TRY THIS: .PrefabGUID
                                var guid = abilityItem.PrefabGUID; 
                                
                                string name = "Unknown";
                                // Note: Ensure you have an AbilityDatabase or similar helper to read names, 
                                // otherwise just log the GUID hash.
                                Plugin.Logger.LogInfo($"   > Ability GUID: {guid.GuidHash}");
                            }
                        }
                    }
                }
            }
            Plugin.Logger.LogInfo("=== SCAN COMPLETE ===");
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
                    return;
                }
            }
        }
    }
}
using System.Collections.Generic;
using Stunlock.Core;

namespace MyScriptMod
{
    public static class AbilityDatabase
    {
        private struct AbilityData
        {
            public string Name;
            public float Duration;
        }

        private static Dictionary<int, AbilityData> _data = new Dictionary<int, AbilityData>();

        static AbilityDatabase()
        {
            // MOVEMENT (Standard is 8s)
            Add(1541401917, "Veil of Blood", 8.0f);
            Add(-131396040, "Veil of Frost", 8.0f);
            Add(1894476748, "Veil of Chaos", 8.0f); 
            Add(-1297441333, "Travel Ability", 8.0f); 

            // SPELLS
            Add(922224629, "Discharge", 8.0f);
            Add(1141366098, "Power Surge", 5.0f);
            Add(337805438, "Blood Rage", 10.0f);
            Add(-667967135, "Merciless Charge (Ult)", 60.0f);
            Add(1039072868, "Blood Rite", 9.0f);
            Add(863435029, "Blood Rite (Active)", 9.0f); // User found this ID
            Add(1215815401, "Shadowbolt", 3.0f);
            Add(-538471976, "Corpse Explosion", 8.0f);
            Add(1176579852, "Ward of the Damned", 9.0f);

            // WEAPONS (Approximate durations needed for Calib)
            Add(1976086857, "Whip E", 8.0f);
            Add(1057163055, "Whip Q", 8.0f);
            Add(2098101392, "Pistol E", 8.0f);
            Add(1356553255, "Pistol Q", 8.0f);
            Add(-1171039965, "Greatsword Q", 8.0f);
            Add(-2140721739, "Greatsword E", 8.0f);
            Add(-1731625958, "Slasher Q", 8.0f);
            Add(-2092408386, "Slasher E", 8.0f);
            Add(1671733210, "Axe Q", 8.0f);
            Add(624673924, "Axe E", 8.0f);
            Add(344610321, "Reaper Q", 8.0f);
            Add(-316825244, "Reaper E", 8.0f);
            Add(-438571790, "Spear Q", 8.0f);
            Add(1459814320, "Spear E", 8.0f);
            Add(-1655280244, "Sword Q", 8.0f);
            Add(414243770, "Sword E", 8.0f);
        }

        private static void Add(int hash, string name, float duration)
        {
            if (!_data.ContainsKey(hash)) 
                _data.Add(hash, new AbilityData { Name = name, Duration = duration });
            else 
                _data[hash] = new AbilityData { Name = name, Duration = duration };
        }

        public static bool TryGetName(PrefabGUID guid, out string name)
        {
            if (_data.TryGetValue(guid.GuidHash, out var d))
            {
                name = d.Name;
                return true;
            }
            name = $"ID: {guid.GuidHash}";
            return false;
        }

        public static float GetDuration(PrefabGUID guid)
        {
            if (_data.TryGetValue(guid.GuidHash, out var d))
            {
                return d.Duration;
            }
            return 8.0f; // Default guess
        }
        
        public static bool IsReliableCalibration(PrefabGUID guid, out float duration)
        {
            if (_data.TryGetValue(guid.GuidHash, out var d))
            {
                if (d.Name.Contains("Veil") || d.Name.Contains("Travel"))
                {
                    duration = d.Duration;
                    return true;
                }
            }
            duration = 0;
            return false;
        }
    }
}

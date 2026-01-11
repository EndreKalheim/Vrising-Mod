using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;
using BepInEx.Logging;
using System;
using System.Collections;

namespace MyScriptMod;

[BepInPlugin("com.myscripts.finalmacro", "Final Macro", "1.0.3")]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    public static Plugin Instance;
    public static ConfigEntry<KeyCode> TriggerKey;
    public static ConfigEntry<float> DelayAfterO9;
    public static ConfigEntry<float> WaitBeforeE;
    public static ConfigEntry<float> SpamDuration; 
    public static bool IsEnabled = true;

    public override void Load()
    {
        Instance = this;
        Logger = Log;
        
        TriggerKey = Config.Bind("General", "TriggerKey", KeyCode.L, "Key to start.");
        DelayAfterO9 = Config.Bind("Timings", "DelayAfterO9", 0.35f, "Delay after O+9.");
        WaitBeforeE = Config.Bind("Timings", "WaitBeforeE", 4.90f, "Time until E starts spamming.");
        SpamDuration = Config.Bind("Timings", "SpamDuration", 0.2f, "How long to spam E.");

        // --- COMPONENTS ---
        // I added DebugScanner here so F11 will work now!
        try { RegisterComponent<DebugScanner>(); } catch {} 
        RegisterComponent<CooldownOverlay>();
        RegisterComponent<Menu>();
        
        // Add optional components if they exist
        try { RegisterComponent<EspFeature>(); } catch {}
        try { RegisterComponent<ScriptManager>(); } catch {}
        try { RegisterComponent<ZoomHack>(); } catch {}

        Log.LogInfo("Mod Loaded Successfully.");
    }

    private T RegisterComponent<T>() where T : MonoBehaviour
    {
        return IL2CPPChainloader.AddUnityComponent<T>();
    }
}
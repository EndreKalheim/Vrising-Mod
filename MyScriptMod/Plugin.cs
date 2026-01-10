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

        // Add all components
        RegisterComponent<MacroController>();
        RegisterComponent<CooldownOverlay>();
        RegisterComponent<Menu>();
        
        // Add optional components if they exist in your project
        try { RegisterComponent<EspFeature>(); } catch {}
        try { RegisterComponent<ScriptManager>(); } catch {}
        try { RegisterComponent<ZoomHack>(); } catch {}

        Log.LogInfo("Mod Loaded Successfully.");
    }

    // Renamed to RegisterComponent to avoid the CS0108 Warning
    private T RegisterComponent<T>() where T : MonoBehaviour
    {
        return IL2CPPChainloader.AddUnityComponent<T>();
    }
}

// --- MACRO LOGIC ---
public class MacroController : MonoBehaviour
{
    private bool _isRunning = false;
    private bool _isMacroTyping = false; 

    private void Update()
    {
        if (Input.GetKeyDown(Plugin.TriggerKey.Value))
        {
            if (_isRunning) { StopSequence("Toggled Off"); return; }
            StartCoroutine(SequenceRoutine().WrapToIl2Cpp());
        }

        if (_isRunning && !_isMacroTyping)
        {
            bool shouldCancel = Input.GetMouseButtonDown(0) || 
                                Input.GetKeyDown(KeyCode.Q) || 
                                Input.GetKeyDown(KeyCode.E) || 
                                Input.GetKeyDown(KeyCode.Space);

            if (shouldCancel) StopSequence("Cancel Action Detected");
        }
    }

    private IEnumerator SequenceRoutine()
    {
        _isRunning = true;
        Plugin.Logger.LogInfo(">>> Macro Started");

        _isMacroTyping = true; 
        Win32.SendScanCode(0x18, true); // O Down
        yield return new WaitForSeconds(0.15f); 
        Win32.SendScanCode(0x0A, true); // 9 Down
        yield return new WaitForSeconds(0.1f); 
        Win32.SendScanCode(0x0A, false); // 9 Up
        yield return new WaitForSeconds(0.05f); 
        Win32.SendScanCode(0x18, false); // O Up
        _isMacroTyping = false; 
        
        yield return new WaitForSeconds(Plugin.DelayAfterO9.Value);

        if (!_isRunning) yield break;
        
        _isMacroTyping = true;
        Win32.SendMouseInput(Win32.MOUSEEVENTF_XDOWN, Win32.XBUTTON1);
        yield return new WaitForSeconds(0.06f); 
        Win32.SendMouseInput(Win32.MOUSEEVENTF_XUP, Win32.XBUTTON1);
        _isMacroTyping = false;

        float startTime = Time.time;
        while (Time.time - startTime < Plugin.WaitBeforeE.Value)
        {
            if (!_isRunning) yield break; 
            yield return null; 
        }

        if (!_isRunning) yield break;

        _isMacroTyping = true;
        float spamStart = Time.time;
        while (Time.time - spamStart < Plugin.SpamDuration.Value)
        {
            if (!_isRunning) break;
            Win32.SendScanCode(0x12, true); 
            yield return new WaitForSeconds(0.02f); 
            Win32.SendScanCode(0x12, false); 
            yield return new WaitForSeconds(0.02f);
        }
        
        _isMacroTyping = false;
        _isRunning = false;
        Plugin.Logger.LogInfo("Macro Finished");
    }

    private void StopSequence(string reason)
    {
        Plugin.Logger.LogInfo($"Stopped: {reason}");
        StopAllCoroutines();
        _isRunning = false;
        _isMacroTyping = false;
        Win32.SendScanCode(0x18, false); 
        Win32.SendScanCode(0x0A, false); 
        Win32.SendScanCode(0x12, false); 
    }
}
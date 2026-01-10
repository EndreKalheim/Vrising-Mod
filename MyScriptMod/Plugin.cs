using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;
using BepInEx.Logging;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using ProjectM;

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

        IL2CPPChainloader.AddUnityComponent<MacroController>();
        IL2CPPChainloader.AddUnityComponent<EspFeature>();
        IL2CPPChainloader.AddUnityComponent<ScriptManager>(); 
        IL2CPPChainloader.AddUnityComponent<Menu>();

        // Capture Instances
        try {
            var cd = IL2CPPChainloader.AddUnityComponent<CooldownOverlay>();
            if (cd != null) 
            {
                CooldownOverlay.Instance = cd;
                Log.LogInfo("CooldownOverlay Added & Instance Set.");
            }
            else Log.LogError("CooldownOverlay Add Failed (Returned Null)");
        } catch (Exception e) { Log.LogError($"CooldownOverlay Init Error: {e.Message}"); }

        try {
            var zoom = IL2CPPChainloader.AddUnityComponent<ZoomHack>();
            if (zoom != null) ZoomHack.Instance = zoom;
        } catch {}

        IL2CPPChainloader.AddUnityComponent<DebugScanner>();
        
        Log.LogInfo("Mod Loaded (Reverted to IL2CPPChainloader) - Safe Init");
    }
}

public class MacroController : MonoBehaviour
{
    private bool _isRunning = false;
    private bool _isMacroTyping = false; 

    private void Update()
    {
        if (Input.GetKeyDown(Plugin.TriggerKey.Value))
        {
            if (_isRunning) { StopSequence("Toggled Off"); return; }
            Plugin.Instance.Config.Reload();
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
        Plugin.Logger.LogInfo(">>> Started");

        // --- STEP 1: O + 9 ---
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

        // --- STEP 2: Mouse 4 ---
        if (!_isRunning) yield break;
        
        _isMacroTyping = true;
        Win32.SendMouseInput(Win32.MOUSEEVENTF_XDOWN, Win32.XBUTTON1);
        yield return new WaitForSeconds(0.06f); 
        Win32.SendMouseInput(Win32.MOUSEEVENTF_XUP, Win32.XBUTTON1);
        _isMacroTyping = false;
        
        Plugin.Logger.LogInfo("Mouse 4 Clicked");

        // --- STEP 3: High Precision Wait ---
        float startTime = Time.time;
        float waitDuration = Plugin.WaitBeforeE.Value;

        while (Time.time - startTime < waitDuration)
        {
            if (!_isRunning) yield break; 
            yield return null; 
        }

        // --- STEP 4: Buffered Input (Spam E) ---
        if (!_isRunning) yield break;

        Plugin.Logger.LogInfo("Buffering E...");
        _isMacroTyping = true;

        float spamStart = Time.time;
        float spamTime = Plugin.SpamDuration.Value;

        while (Time.time - spamStart < spamTime)
        {
            if (!_isRunning) break;

            Win32.SendScanCode(0x12, true); 
            yield return new WaitForSeconds(0.02f); 
            Win32.SendScanCode(0x12, false); 
            yield return new WaitForSeconds(0.02f);
        }
        
        _isMacroTyping = false;
        Plugin.Logger.LogInfo("E Sequence Finished");

        _isRunning = false;
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
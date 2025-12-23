using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MyScriptMod
{
    public static class Win32
    {
        // These are "P/Invokes" - they tell Windows to do things outside of the game
        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        public const uint KEYEVENTF_SCANCODE = 0x0008;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint XBUTTON1 = 0x0001;

        public static uint GetScanCode(KeyCode key)
        {
            // Unity KeyCodes roughly map to Virtual Keys for standard standard ASCII keys
            return MapVirtualKey((uint)key, 0);
        }

        public static void SendScanCode(ushort scanCode, bool isDown)
        {
            uint flags = KEYEVENTF_SCANCODE;
            if (!isDown) flags |= KEYEVENTF_KEYUP;
            keybd_event(0, (byte)scanCode, flags, UIntPtr.Zero);
        }

        public static void SendMouseInput(uint flags, uint data)
        {
            mouse_event(flags, 0, 0, data, UIntPtr.Zero);
        }
    }
}

namespace Loupedeck.MumiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Drawing;
    using System.Security.Cryptography;


    [Flags]
    public enum ModifierKeyEx
    {
        None = 0,
        Control = 1,
        LeftControl = Control,
        Alt = 2,
        LeftAlt = Alt,
        AltOrOption = Alt,
        Option = AltOrOption,
        Shift = 4,
        LeftShift = Shift,
        Command = 8,
        Windows = Command,
        LeftWindows = Command,
        ExtendedKeyboard = 64,
        ControlOrCommand = 128,
        RightControl = 256,
        RightAlt = 512,
        RightShift = 1024,
        RightWindows = 2048,
    }


    public enum KeyActionType
    {
        Normal,
        Up,
        Down
    }

    internal class NativeMethods
    {

        



        /// <summary>
        /// gets the handle of a process by name
        /// </summary>
        /// <param name="name">process name</param>
        /// <returns>IntPtr.zero if not found</returns>
        public static IntPtr GetProcessHandleByName(string name)
        {

            var processes = Process.GetProcessesByName(name);
            Process process = null;
            foreach (var p in processes)
            {
                if (p.ProcessName == name)
                {
                    process = p;
                    break;
                }
            }


            if (process != null)
                return process.MainWindowHandle;

            return IntPtr.Zero;

        }


   

        private static bool TargetWindow(IntPtr hWnd)
        {

            if (hWnd != IntPtr.Zero)
            {
                uint targetThreadID = GetWindowThreadProcessId(hWnd, IntPtr.Zero);
                uint currentThreadID = GetCurrentThreadId();

                if (targetThreadID != currentThreadID)
                {
                    try
                    {
                        if (!AttachThreadInput(currentThreadID, targetThreadID, true))
                            return false;
                        var parentWindow = GetAncestor(hWnd, GetAncestorFlags.GA_ROOT);
                        if (IsIconic(parentWindow))
                        {
                            if (!RestoreWindow(parentWindow))
                                return false;
                        }

                        if (!BringWindowToTop(parentWindow))
                            return false;
                        if (SetFocus(hWnd) == IntPtr.Zero)
                            return false;
                    }
                    finally
                    {
                        AttachThreadInput(currentThreadID, targetThreadID, false);
                    }
                }
                else
                {
                    SetFocus(hWnd);
                }
            }

            return true;

        }



        public static bool SendKeyboardInput(VirtualKeyCode key, ModifierKeyEx[] modifiers = null,
            int delay = 0, KeyActionType actionType = KeyActionType.Normal)
        {
           
            //if (!TargetWindow(hWnd))
            //    return false;

            var flagsKeyDw = IsExtendedKey(key) ? KeyboardInputFlags.ExtendedKey : KeyboardInputFlags.KeyDown;
            var flagsKeyUp = KeyboardInputFlags.KeyUp | (IsExtendedKey(key) ? KeyboardInputFlags.ExtendedKey : 0);

            var inputs = new List<INPUT>();
            var input = new INPUT(SendInputType.InputKeyboard);

            if (actionType == KeyActionType.Normal || actionType == KeyActionType.Down)
            {
                // Key Modifiers Down
                if (!(modifiers is null))
                {
                    foreach (var modifier in modifiers)
                    {
                        input.Union.Keyboard.Flags = KeyboardInputFlags.KeyDown;
                        input.Union.Keyboard.VirtKeys = (ushort)modifier;
                        inputs.Add(input);
                    }
                }


                // Key Down
                input.Union.Keyboard.Flags = flagsKeyDw | KeyboardInputFlags.Unicode;
                input.Union.Keyboard.VirtKeys = (ushort)key;
                inputs.Add(input);
            }





            if (actionType == KeyActionType.Normal || actionType == KeyActionType.Up)
            {

                // Key Up
                input.Union.Keyboard.Flags = flagsKeyUp | KeyboardInputFlags.Unicode;
                input.Union.Keyboard.VirtKeys = (ushort)key;
                inputs.Add(input);

                // Key Modifiers Up
                if (!(modifiers is null))
                {
                    foreach (var modifier in modifiers)
                    {
                        input.Union.Keyboard.Flags = KeyboardInputFlags.KeyUp;
                        input.Union.Keyboard.VirtKeys = (ushort)modifier;
                        inputs.Add(input);
                    }
                }
            }

            uint sent = SendInput((uint)inputs.Count(), inputs.ToArray(), Marshal.SizeOf<INPUT>());
            return sent > 0;
        }

        public static INPUT KeyboardInput(ushort key, KeyActionType actionType = KeyActionType.Normal)
        {
            var input = new INPUT(SendInputType.InputKeyboard);
            var flag = actionType == KeyActionType.Down ? KeyboardInputFlags.KeyDown : KeyboardInputFlags.KeyUp;
            input.Union.Keyboard.Flags = flag | KeyboardInputFlags.Unicode;
            input.Union.Keyboard.VirtKeys = (ushort)key;

            return input;
        }

        public static INPUT KeyboardInput(VirtualKeyCode key, KeyActionType actionType = KeyActionType.Normal)
        {
            return KeyboardInput((ushort)key, actionType);
        }

        public static INPUT KeyboardInput(ModifierKeyEx key, KeyActionType actionType = KeyActionType.Normal)
        {
            return KeyboardInput((ushort)key, actionType);
        }

        /// <summary>
        /// sends mouse wheel data with an optional modifier pressed
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="scroll"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static bool SendMouseWheelInput(IntPtr hWnd, int scroll, ModifierKeyEx[] modifiers = null)
        {
            if (!TargetWindow(hWnd))
                return false;
            var pos = GetMousePosition();
            var inputs = new List<INPUT>();

            if (modifiers != null)
            {
                // press modifier key down
                foreach (var modifier in modifiers)
                {
                    var kinput = KeyboardInput(modifier, KeyActionType.Down);
                    inputs.Add(kinput);
                }
            }

            var input = new INPUT(SendInputType.InputMouse);
            input.Union.Mouse.mouseData = (uint) scroll;
            input.Union.Mouse.dx = pos.x;
            input.Union.Mouse.dy = pos.y;
            inputs.Add(input);

            if (modifiers != null)
            {
                // press modifier key down
                foreach (var modifier in modifiers)
                {
                    var kinput = KeyboardInput(modifier, KeyActionType.Up);
                    inputs.Add(kinput);
                }
            }

            uint sent = SendInput((uint)inputs.Count(), inputs.ToArray(), Marshal.SizeOf<INPUT>());
            return sent > 0;

        }



        private static VirtualKeyCode[] extendedKeys =
        {
            VirtualKeyCode.ArrowUp,VirtualKeyCode.ArrowDown, VirtualKeyCode.ArrowLeft, VirtualKeyCode.ArrowRight, VirtualKeyCode.Home, VirtualKeyCode.End, VirtualKeyCode.MediaPrevTrack, VirtualKeyCode.MediaNextTrack, VirtualKeyCode.Insert, VirtualKeyCode.Delete
        };

        private static bool IsExtendedKey(VirtualKeyCode key) => extendedKeys.Contains(key);

        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-input
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public SendInputType InputType;
            public InputUnion Union;

            public INPUT(SendInputType type)
            {
                InputType = type;
                Union = new InputUnion();
            }
        }

        public enum SendInputType : uint
        {
            InputMouse = 0,
            InputKeyboard = 1,
            InputHardware = 2
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT Mouse;

            [FieldOffset(0)] public KEYBDINPUT Keyboard;

            [FieldOffset(0)] public HARDWAREINPUT Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public MouseEventdwFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort VirtKeys;
            public ushort wScan;
            public KeyboardInputFlags Flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [Flags]
        public enum MouseEventdwFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }

        [Flags]
        public enum KeyboardInputFlags : uint
        {
            KeyDown = 0x0,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Scancode = 0x0008,
            Unicode = 0x0004
        }

        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-windowplacement
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public WplFlags flags;
            public SW_Flags showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        public enum WplFlags : uint
        {
            WPF_ASYNCWINDOWPLACEMENT =
                0x0004, // If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.

            WPF_RESTORETOMAXIMIZED =
                0x0002, // The restored window will be maximized, regardless of whether it was maximized before it was minimized. This setting is only valid the next time the window is restored. It does not change the default restoration behavior.

            // This flag is only valid when the SW_SHOWMINIMIZED value is specified for the showCmd member.
            WPF_SETMINPOSITION =
                0x0001 // The coordinates of the minimized window may be specified. This flag must be specified if the coordinates are set in the ptMinPosition member.
        }

        [Flags]
        public enum SW_Flags : uint
        {
            SW_HIDE = 0X00,
            SW_SHOWNORMAL = 0x01,
            SW_MAXIMIZE = 0x03,
            SW_SHOWNOACTIVATE = 0x04,
            SW_SHOW = 0x05,
            SW_MINIMIZE = 0x06,
            SW_RESTORE = 0x09,
            SW_SHOWDEFAULT = 0x0A,
            SW_FORCEMINIMIZE = 0x0B
        }

        public enum GetAncestorFlags : uint
        {
            GA_PARENT = 1, // Retrieves the parent window.This does not include the owner, as it does with the GetParent function.
            GA_ROOT = 2, // Retrieves the root window by walking the chain of parent windows.

            GA_ROOTOWNER =
                3 // Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        }

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;

            public POINT() { }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public Point ToPoint() => new Point(this.x, this.y);
            public PointF ToPointF() => new PointF((float)this.x, (float)this.y);
            public POINT FromPoint(Point p) => new POINT(p.X, p.Y);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct W32POINT
        {
            public int x;
            public int y;


            public static implicit operator Point(W32POINT point)
            {
                return new Point(point.x, point.y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
            public Rectangle ToRectangleOffset(POINT p) => Rectangle.FromLTRB(p.x, p.y, Right + p.x, Bottom + p.y);

            public RECT FromRectangle(RectangleF rectangle) => FromRectangle(Rectangle.Round(rectangle));

            public RECT FromRectangle(Rectangle rectangle) => new RECT()
            {
                Left = rectangle.Left, Top = rectangle.Top, Bottom = rectangle.Bottom, Right = rectangle.Right
            };

            public RECT FromXYWH(int x, int y, int width, int height) => new RECT(x, y, x + width, y + height);
        }


        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowplacement
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, [In, Out] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr voidProcessId);

        // https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentthreadid
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint GetCurrentThreadId();

        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-attachthreadinput
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool AttachThreadInput([In] uint idAttach, [In] uint idAttachTo,
            [In, MarshalAs(UnmanagedType.Bool)] bool fAttach);

        [ResourceExposure(ResourceScope.None)]
        [DllImport("User32", ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetAncestor(IntPtr hWnd, GetAncestorFlags flags);

        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SetFocus(IntPtr hWnd);

        //https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-sendinput
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, [In, MarshalAs(UnmanagedType.LPArray)] INPUT[] pInputs,
            int cbSize);


        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out W32POINT lpPoint);


        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public static bool RestoreWindow(IntPtr hWnd)
        {
            var wpl = new WINDOWPLACEMENT() { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
            if (!GetWindowPlacement(hWnd, ref wpl))
                return false;

            wpl.flags = WplFlags.WPF_ASYNCWINDOWPLACEMENT;
            wpl.showCmd = SW_Flags.SW_RESTORE;
            return SetWindowPlacement(hWnd, ref wpl);
        }


        public static W32POINT GetMousePosition()
        {
            W32POINT lpPoint;
            GetCursorPos(out lpPoint);



            return lpPoint;
        }
    }

}
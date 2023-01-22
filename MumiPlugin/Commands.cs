namespace Loupedeck.MumiPlugin
{
    using System;

    

    //class RightAltWheelMouseUpCommand : PluginDynamicCommand
    //{

    //    public RightAltWheelMouseUpCommand(): base("RAlt Wheel Up", "RAlt MouseWheel Up", "Mouse")
    //    {
    //    }

    //    protected override void RunCommand(String actionParameter)
    //    {


    //        NativeMethods.SendKeyboardInput(VirtualKeyCode.KeyA, null, 0, KeyActionType.Down);

    //        ////var name = this.Plugin.ClientApplication.GetRunningProcessName();
    //        ////IntPtr hWnd = NativeMethods.GetProcessHandleByName(name);
    //        //IntPtr hWnd = NativeMethods.GetForegroundWindow();

    //        //if (hWnd != IntPtr.Zero)
    //        //{

    //        //    // NativeMethods.SendKeyboardInput(hWnd, VirtualKeyCode.KeyA);
    //        //    this.Plugin.ClientApplication.SendKeyboardShortcut(VirtualKeyCode.KeyA);

    //        //    //ModifierKeyEx[] modifiers = { ModifierKeyEx.RightAlt };
    //        //    //int scroll = 120;
    //        //    //NativeMethods.SendMouseWheelInput(hWnd, scroll, modifiers);
    //        //}
    //    }
    //}

//    class RightAltWheelMouseDownCommand : PluginDynamicCommand
//    {

//        public RightAltWheelMouseDownCommand() : base("RAlt Wheel Down", "RAlt MouseWheel Down", "Mouse")
//        {

//        }

//        protected override void RunCommand(String actionParameter)
//        {
//            //var name = this.Plugin.ClientApplication.GetRunningProcessName();
//            //IntPtr hWnd = NativeMethods.GetProcessHandleByName(name);

//            NativeMethods.SendKeyboardInput(VirtualKeyCode.KeyA,null,0,KeyActionType.Up);


////            IntPtr hWnd = NativeMethods.GetForegroundWindow();
////            if (hWnd != IntPtr.Zero)
////            {

                
////                this.Plugin.ClientApplication.SendKeyboardShortcut(VirtualKeyCode.AltRight,);
////# ModifierKeyEx[] modifiers = { ModifierKeyEx.RightAlt };
////                //int scroll = -120;
////                //NativeMethods.SendMouseWheelInput(hWnd, scroll, modifiers);
//            //}
//        }
//    }

    //class VJoyPulseButtonCommand : PluginDynamicCommand
    //{
    //    public VJoyPulseButtonCommand() // base("VJOY Button", "Pulses a VJOY button", "")
    //    {
    //        // add parameter
    //        for (int i = 0; i < 128; i++)
    //        {
    //            var device = i.ToString();
    //            var name = $"Button {i}";
    //            this.AddParameter(device, name, "Buttons");
    //        }
    //        //this.AddParameter("device", "Device ID", "VJoy");
    //        //this.AddParameter("button", "Button 0..127", "VJoy");
    //        //this.AddParameter("mode", "Mode", "VJoy");
    //        //this.AddParameter("data", "Device Button [Mode]", "Vjoy");
   

    //    }

    //    protected override void RunCommand(String actionParameter)
    //    {
    //        MumiLog.Info($"BUTTON PULSE: {this.Name}  {actionParameter}");

    //    }

        
    //}


}

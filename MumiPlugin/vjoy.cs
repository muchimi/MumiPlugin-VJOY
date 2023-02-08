using System.Collections.Generic;


using System;

namespace vJoy.Wrapper
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Timers;
    using System.Web.Hosting;

    public enum Hats
    {
        Hat = 1,
        HatExt1 = 2,
        HatExt2 = 3,
        HatExt3 = 4
    }

    public enum Axis
    {
        HID_USAGE_X = 48,
        HID_USAGE_Y = 49,
        HID_USAGE_Z = 50,
        HID_USAGE_RX = 51,
        HID_USAGE_RY = 52,
        HID_USAGE_RZ = 53,
        HID_USAGE_SL0 = 54,
        HID_USAGE_SL1 = 55,
        HID_USAGE_WHL = 56,
        HID_USAGE_POV = 57
    }





    /// <summary>
    /// VirtualJoystick: Helper class to set and read button states.  Helper keeps its own state and updates the vJoy object after state changes.
    /// If vJoy object's button states or axis are set directly through the "Joystick" property the internal state is not updated.
    /// </summary>
    public class VirtualJoystick : IDisposable
    {
        // Common vJoy Instance
        private static readonly vJoyInterfaceWrap.vJoy VJoyInstance = new vJoyInterfaceWrap.vJoy();

        /// <summary>
        /// Virtual Joystick is Aquired and connected
        /// </summary>
        public Boolean Aquired { get; private set; }

        /// <summary>
        /// Direct access to the vJoy object.
        /// </summary>
        public vJoyInterfaceWrap.vJoy Joystick => VJoyInstance;

        /// <summary>
        /// vJoy Joystick number
        /// </summary>
        public UInt32 JoystickId { get; }

        private vJoyInterfaceWrap.vJoy.JoystickState _state = new vJoyInterfaceWrap.vJoy.JoystickState();

        /// <summary>
        /// Constructor: vJoystick Id is the vJoy number to aquire
        /// </summary>
        /// <param name="vJoystickId"></param>
        public VirtualJoystick(UInt32 vJoystickId) => this.JoystickId = vJoystickId;

        /// <summary>
        /// returns list of deviceId currently defined on the client
        /// </summary>
        /// <returns></returns>
        public static List<UInt32> DeviceList()
        {
            List<UInt32> list = new List<UInt32>();
            UInt32 count;
            var joystick = VirtualJoystick.VJoyInstance;
            for (UInt32 i = 1; i <= 16; i++)
            {
                var status = joystick.GetVJDStatus(i);
                if (status != VjdStat.VJD_STAT_MISS && status != VjdStat.VJD_STAT_UNKN)
                {
                    list.Add(i);
                }
            }

            return list;
        }

        /// <summary>
        /// checks if a device is defined
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static bool DeviceExists(UInt32 deviceId)
        {
            var joystick = VirtualJoystick.VJoyInstance;
            var status = joystick.GetVJDStatus(deviceId);
            return status != VjdStat.VJD_STAT_MISS && status != VjdStat.VJD_STAT_UNKN;
        }



        /// <summary>
        /// Connect to the virtual joystick
        /// </summary>
        public void Aquire()
        {
            if (this.Aquired)
                return;

            try
            {

                this._state.bDevice = (Byte)this.JoystickId;
                this.Joystick.AcquireVJD(this.JoystickId);
                this.Joystick.ResetVJD(this.JoystickId);
                this.Aquired = true;
            }
            catch
            {
            }

        }

        /// <summary>
        /// Disconnect from the virtual joystick
        /// </summary>
        public void Release()
        {
            if (!this.Aquired)
                return;
            try
            {
                this.Joystick?.RelinquishVJD(this.JoystickId);
            }
            catch { }

            this.Aquired = false;
        }

        public void Dispose()
        {
            if (this.Aquired)
                this.Joystick?.RelinquishVJD(this.JoystickId);
        }

        /// <summary>
        /// Update the virtual joystick with the current state
        /// </summary>
        public void Update() => this.Joystick.UpdateVJD(this.JoystickId, ref this._state);

        /// <summary>
        /// Press/Release a virtual joystick button
        /// </summary>
        /// <param name="down">true = button pressed, false = button released</param>
        /// <param name="vButtonNumber">virtual button number</param>
        public void SetJoystickButton(Boolean down, UInt32 vButtonNumber)
        {
            // Offset by one
            var vButton = vButtonNumber - 1;

            // Set the position or don't
            var buttons = down ? (UInt32)0x1 << (Int32)vButton : 0;

            // Build a mask for that position
            var mask = (UInt32)0x1 << (Int32)vButton;

            // Clear just that position
            var holdValues = this._state.Buttons & ~mask;

            // Set the new value
            this._state.Buttons = holdValues | buttons;

            this.Joystick.SetBtn(down, this.JoystickId, vButtonNumber);

            // Update
            //this.Joystick.UpdateVJD(this.JoystickId, ref this._state);
        }

        /// <summary>
        /// Get the current state of a virtual button
        /// </summary>
        /// <param name="vButtonNumber">virtual button number</param>
        /// <returns>true = button pressed, false = button released</returns>
        public Boolean GetJoystickButton(UInt32 vButtonNumber)
        {
            
            // Offset by one
            var vButton = vButtonNumber - 1;

            // Build a mask for that position
            var mask = (UInt32)0x1 << (Int32)vButton;

            var result = this._state.Buttons & mask;
            return (this._state.Buttons & mask) == mask;
        }

        /// <summary>
        /// Sets the state of all the buttons (max 32 button device)
        /// </summary>
        /// <param name="buttons">binary state of all the buttons</param>
        /// <param name="mask">bitmask for applying changes</param>
        public void SetJoystickButtons(UInt32 buttons, UInt32 mask = 0xFFFFFFFF)
        {
            // Clear the buttons we are assigning
            var holdValues = this._state.Buttons & ~mask;
            this._state.Buttons = holdValues | buttons;
            this.Joystick.UpdateVJD(this.JoystickId, ref this._state);
        }

        /// <summary>
        /// Set the value for a virtual axis
        /// </summary>
        /// <param name="value">axis value</param>
        /// <param name="usage">axis to set</param>
        public void SetJoystickAxis(Int32 value, Axis usage)
        {
            switch (usage)
            {
                case Axis.HID_USAGE_X:
                    this._state.AxisX = value;
                    break;
                case Axis.HID_USAGE_Y:
                    this._state.AxisY = value;
                    break;
                case Axis.HID_USAGE_Z:
                    this._state.AxisZ = value;
                    break;
                case Axis.HID_USAGE_RX:
                    this._state.AxisXRot = value;
                    break;
                case Axis.HID_USAGE_RY:
                    this._state.AxisYRot = value;
                    break;
                case Axis.HID_USAGE_RZ:
                    this._state.AxisZRot = value;
                    break;
                case Axis.HID_USAGE_SL0:
                    this._state.Slider = value;
                    break;
                case Axis.HID_USAGE_SL1:
                    this._state.Dial = value;
                    break;
                case Axis.HID_USAGE_WHL:
                    this._state.Wheel = value;
                    break;
                case Axis.HID_USAGE_POV:
                    //State. = value;  //Not sure where this maps
                    break;
            }

            this.Joystick.UpdateVJD(this.JoystickId, ref this._state);
        }

        /// <summary>
        /// gets the joystick axis value
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public Int32 GetJoystickAxis(Axis usage)
        {
            this.Joystick.UpdateVJD(this.JoystickId, ref this._state);
            switch (usage)
            {
                case Axis.HID_USAGE_X:
                    return this._state.AxisX;

                case Axis.HID_USAGE_Y:
                    return this._state.AxisY;

                case Axis.HID_USAGE_Z:
                    return this._state.AxisZ;

                case Axis.HID_USAGE_RX:
                    return this._state.AxisXRot;

                case Axis.HID_USAGE_RY:
                    return this._state.AxisYRot;

                case Axis.HID_USAGE_RZ:
                    return this._state.AxisZRot;

                case Axis.HID_USAGE_SL0:
                    return this._state.Slider;

                case Axis.HID_USAGE_SL1:
                    return this._state.Dial;

                case Axis.HID_USAGE_WHL:
                    return this._state.Wheel;
            }

            return 0;
        }

        /// <summary>
        /// Set the value for a virtual hat
        /// </summary>
        /// <param name="value">hat value</param>
        /// <param name="hat">virtual hat</param>
        public void SetJoystickHat(Int32 value, Hats hat)
        {
            switch (hat)
            {
                case Hats.Hat:
                    this._state.bHats = (Byte)value;
                    break;
                case Hats.HatExt1:
                    this._state.bHatsEx1 = (Byte)value;
                    break;
                case Hats.HatExt2:
                    this._state.bHatsEx2 = (Byte)value;
                    break;
                case Hats.HatExt3:
                    this._state.bHatsEx3 = (Byte)value;
                    break;
            }

            this.Joystick.UpdateVJD(this.JoystickId, ref this._state);
        }

        /// <summary>
        ///  gets the value for a virtual hat
        /// </summary>
        /// <param name="hat"></param>
        /// <returns></returns>
        public Int32 GetJoystickHat(Hats hat)
        {
            this.Joystick.UpdateVJD(this.JoystickId, ref this._state);
            switch (hat)
            {
                case Hats.Hat:
                    return (Int32)this._state.bHats;

                case Hats.HatExt1:
                    return (Int32)this._state.bHatsEx1;
                    ;

                case Hats.HatExt2:
                    return (Int32)this._state.bHatsEx2;
                    ;

                case Hats.HatExt3:
                    return (Int32)this._state.bHatsEx3;
            }

            return -1;
        }
    }
}



namespace muchimi.vjoy
{
    using System.Diagnostics.Eventing.Reader;
    using System.Resources;
    using System.Timers;
    using System.Web.UI.WebControls;

    using vJoy.Wrapper;


    public enum VJoyAxis
    {
        AXIS_X,
        AXIS_Y,
        AXIS_Z,
        AXIS_RX,
        AXIS_RY,
        AXIS_RZ,
        AXIS_SL0,
        AXIS_SL1,
        AXIS_HAT0,
        AXIS_HAT1,
        AXIS_HAT2,
        AXIS_HAT3,
    }


    internal class PulseData
    {
        public ElapsedEventHandler Handler { get; set; }
        public Timer Timer => new Timer();

        public UInt32 DeviceId { get; set; }

        public UInt32 ButtonId { get; set; }


        public PulseData(UInt32 deviceId, UInt32 buttonId)
        {
            this.DeviceId = deviceId;
            this.ButtonId = buttonId;
        }

    }

    /// <summary>
    /// holds a list of button timers and their handlers, one per button
    /// </summary>
    internal class PulseTimers
    {
        private Dictionary<UInt32, PulseData> Map { get; set; }

        public UInt32 DeviceId { get; private set; }

        public PulseData GetButton(UInt32 button)
        {
            PulseData data;
            if (!this.Map.ContainsKey(button))
            {
                data = new PulseData(this.DeviceId, button);
                this.Map.Add(button, data);

            }
            else
            {
                data = this.Map[button];
            }

            return data;

        }

        public PulseTimers(UInt32 deviceId)
        {
            this.DeviceId = deviceId;
            this.Map = new Dictionary<UInt32, PulseData>();

        }

    }


    class PulseTrackerData
    {
        private Dictionary<UInt32, PulseTimers> Map { get; set; }



        /// <summary>
        /// holds the list of device timers for all devices
        /// </summary>


        private readonly PulseTimers _pulseTimers;

        private VjoyInstance VJoy { get; set; }

        public PulseTrackerData(VjoyInstance vjoy)
        {
            // map of device id to the map of button timers data
            this.Map = new Dictionary<UInt32, PulseTimers>();
            this.VJoy = vjoy;

        }


        private PulseData GetButtonData(UInt32 deviceId, UInt32 buttonId)
        {
            PulseTimers data;
            if (!this.Map.ContainsKey(deviceId))
            {
                data = new PulseTimers(deviceId);
                this.Map.Add(deviceId, data);
            }
            else
            {
                data = this.Map[deviceId];
            }

            return data.GetButton(buttonId);
        }


        public Timer GetButtonTimer(UInt32 deviceId, UInt32 buttonId)
        {
            return this.GetButtonData(deviceId, buttonId).Timer;
        }



        public void Pulse(UInt32 deviceId, UInt32 buttonId, UInt32 duration = 1000)
        {
            var data = this.GetButtonData(deviceId, buttonId);
            var timer = data.Timer;

            timer.Stop();

            if (data.Handler != null)
            {
                timer.Elapsed -= data.Handler;
            }

            if (data.Handler == null)
            {
                data.Handler = (sender, args) =>
                {
                    this.VJoy.Release(deviceId, buttonId);
                };
            }


            this.VJoy.Press(deviceId, buttonId);
            timer.Interval = duration;
            timer.Elapsed += data.Handler;
            timer.Start();

        }



    }



    public class VjoyInstance
    {
        private UInt32 _max_id = 0;
        private Dictionary<Int32, VirtualJoystick> _joyMap = new Dictionary<Int32, VirtualJoystick>();

        public UInt32 max_id => this._max_id;


        private PulseTrackerData PulseTracker { get; set; }


        public VjoyInstance()
        {
            this.PulseTracker = new PulseTrackerData(this);
        }


        public VirtualJoystick GetJoystick(UInt32 deviceId)
        {
            VirtualJoystick joy;
            Int32 id = (Int32)deviceId;
            if (!this._joyMap.TryGetValue(id, out joy))
            {
                joy = new VirtualJoystick(deviceId);
                this._joyMap[id] = joy;
            }


            return joy;
        }

        /// <summary>
        /// gets a list of configured VJOY device IDs on this client
        /// </summary>
        /// <returns></returns>
        public static List<UInt32> GetJoystickList()
        {
            return VirtualJoystick.DeviceList();
        }

        public static bool JoystickExists(UInt32 deviceId)
        {
            return VirtualJoystick.DeviceExists(deviceId);
        }

        /// <summary>
        /// releases all used items
        /// </summary>
        public void Release()
        {
            foreach (var joy in this._joyMap.Values)
            {
                if (joy.Aquired)
                    joy.Release();
            }

        }



        public void Press(UInt32 deviceId, UInt32 button)
        {
            var joy = this.GetJoystick(deviceId);
            if (!joy.Aquired)
                joy.Aquire();
            if (joy.Aquired)
            {
                joy.SetJoystickButton(true, button);
                joy.Release();

            }


        }

        public void Release(UInt32 deviceId, UInt32 button)
        {
            var joy = this.GetJoystick(deviceId);
            if (!joy.Aquired)
                joy.Aquire();
            if (joy.Aquired)
            {
                joy.SetJoystickButton(false, button);
                joy.Release();
            }

        }

        /// <summary>
        /// pulses a button for the specified duration
        /// </summary>
        /// <param name="deviceId">vjoy device number, 0 based</param>
        /// <param name="button">vjoy button number, 0 to 127</param>
        /// <param name="duration">duration in milliseconds</param>
        public void Pulse(UInt32 deviceId, UInt32 button, UInt32 duration = 1000)
        {
            this.PulseTracker.Pulse(deviceId, button, duration);
        }

        /// <summary>
        /// sets a button
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="button"></param>
        /// <param name="state"></param>
        public void SetButton(UInt32 deviceId, UInt32 button, bool state)
        {
            var joy = this.GetJoystick(deviceId);
            if (!joy.Aquired)
                joy.Aquire();
            if (joy.Aquired)
            {
                joy.SetJoystickButton(state, button);
                joy.Release();
            }

        }

        /// <summary>
        /// toggles a button
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="button"></param>
        public void ToggleButton(UInt32 deviceId, UInt32 button)
        {
            var joy = this.GetJoystick(deviceId);
            if (!joy.Aquired)
                joy.Aquire();
            if (joy.Aquired)
            {
                var state = joy.GetJoystickButton(button);
                joy.SetJoystickButton(!state, button);
                joy.Release();
            }

        }


        public enum HatPositionEnum
        {
            Center,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left,
            TopLeft,
        }


        //dill_hat_lookup = {
        //    -1: (0, 0),  center
        //    0: (0, 1), top    
        //    4500: (1, 1), top right
        //    9000: (1, 0), right
        //    13500: (1, -1), top left
        //    18000: (0, -1), bottom
        //    22500: (-1, -1), bottom left
        //    27000: (-1, 0), left
        //    31500: (-1, 1) bottom right
        //}

        public Int32 HatValue(HatPositionEnum pos)
        {
            switch (pos)
            {
                case HatPositionEnum.Center: // (0,0)
                    return -1;

                case HatPositionEnum.Top:
                    return 9000;

                case HatPositionEnum.TopRight:
                    return 4500;

                case HatPositionEnum.Right:
                    return 9000;
                case HatPositionEnum.BottomRight:
                    return 31500;
                case HatPositionEnum.Bottom:
                    return 1800;
                case HatPositionEnum.BottomLeft:
                    return 22500;
                case HatPositionEnum.Left:
                    return 27000;
                case HatPositionEnum.TopLeft:
                    return 13500;
                default:
                    return -1; // center by default
            }
        }

        /// <summary>
        /// converts a hat position to a vjoy hat value
        /// </summary>
        /// <param name="x">0 center, 1 = right, -1 = left</param>
        /// <param name="y">0 center, 1 = up, -1 = down</param>
        /// <returns></returns>
        public Int32 HatValue(Int32 x, Int32 y)
        {
            switch (x)
            {
                case 0:
                    switch (y)
                    {
                        case 0:
                            return -1; // 0,0
                        case 1:
                            return 0; // 0,1
                        case -1:
                            return 18000; // 0, -1
                    }

                    break;
                case -1:
                    switch (y)
                    {
                        case 0:
                            return 27000; // -1,0
                        case 1:
                            return 31500; // -1,1
                        case -1:
                            return 22500; // -1, -1
                    }
                    break;
                case 1:
                    switch (y)
                    {
                        case 0:
                            return 9000; // 1,0
                        case 1:
                            return 4500; // 1,1
                        case -1:
                            return 13500; // 1, -1
                    }
                    break;
            }

            return -1;

        }

        /// <summary>
        /// converts a hat axis value to x/y coordinates
        /// </summary>
        /// <param name="value"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void HatCoords(Int32 value, out Int32 x, out Int32 y)
        {
            x = 0;
            y = 0;
            switch (value)
            {
                //    -1: (0, 0),
                case -1:
                    x = 0;
                    y = 0;
                    break;
                //    0: (0, 1),
                case 0:
                    x = 0;
                    y = 1;
                    break;
                //    4500: (1, 1),
                case 4500:
                    x = 1;
                    y = 1;
                    break;
                //    9000: (1, 0),
                case 9000:
                    x = 1;
                    y = 0;
                    break;
                //    13500: (1, -1)
                case 13500:
                    x = 1;
                    y = -1;
                    break;
                //    18000: (0, -1),
                case 18000:
                    x = 0;
                    y = -1;
                    break;
                //    22500: (-1, -1),
                case 22500:
                    x = -1;
                    y = -1;
                    break;
                //    27000: (-1, 0),
                case 27000:
                    x = -1;
                    y = 0;
                    break;
                //    31500: (-1, 1)
                case 31500:
                    x = -1;
                    y = 1;
                    break;
                    //}
            }

        }

        //dill_hat_lookup = {
        //    -1: (0, 0),
        //    0: (0, 1),
        //    4500: (1, 1),
        //    9000: (1, 0),
        //    13500: (1, -1),
        //    18000: (0, -1),
        //    22500: (-1, -1),
        //    27000: (-1, 0),
        //    31500: (-1, 1)
        //}


        /// <summary>
        /// converts a float value like in Gremlin from -1 to 1 into a value VJOY understands
        /// </summary>
        /// <param name="value">an axis value between -1.0 and +1.0 with 0.0 being the center of the axis</param>
        /// <returns>a vjoy axis value</returns>
        public Int32 ToVjoyRange(Double value)
        {
            Int32 vv = (Int32)((value + 1.0) * 0x4000);
            return vv;
        }

        /// <summary>
        /// converts a vjoy axis value to a -1.0, 1.0 range
        /// </summary>
        /// <param name="value"></param>
        /// <returns>a value between the range of -1.0 and 1.0 with 0.0 being the center</returns>
        public Double RangeFromVjoy(Int32 value)
        {
            Double vv = (Double)(value) / (Double)(0x4000) - 1.0;
            return vv;

        }

        /// <summary>
        /// returns a wrapper axis from a plugin axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public Axis WrapperAxisFromAxis(VJoyAxis axis)
        {
            switch (axis)

            {
                case VJoyAxis.AXIS_X:
                    return Axis.HID_USAGE_X;

                case VJoyAxis.AXIS_Y:
                    return Axis.HID_USAGE_Y;

                case VJoyAxis.AXIS_Z:
                    return Axis.HID_USAGE_Z;

                case VJoyAxis.AXIS_RX:
                    return Axis.HID_USAGE_RX;

                case VJoyAxis.AXIS_RY:
                    return Axis.HID_USAGE_RX;

                case VJoyAxis.AXIS_RZ:
                    return Axis.HID_USAGE_RX;

                case VJoyAxis.AXIS_SL0:
                    return Axis.HID_USAGE_RX;

                case VJoyAxis.AXIS_SL1:
                    return Axis.HID_USAGE_RX;
            }

            return Axis.HID_USAGE_X;
        }

        /// <summary>
        /// returns a wrapper hat from a plugin axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public Hats WrapperHatFromAxis(VJoyAxis axis)
        {
            switch (axis)
            {
                case VJoyAxis.AXIS_HAT0:
                    return Hats.Hat;
                case VJoyAxis.AXIS_HAT1:
                    return Hats.HatExt1;
                case VJoyAxis.AXIS_HAT2:
                    return Hats.HatExt2;
                case VJoyAxis.AXIS_HAT3:
                    return Hats.HatExt3;
            }

            return Hats.Hat;

        }

        /// <summary>
        ///  gets the current axis value as a range -1.0 to 1.0
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public Boolean GetAxis(UInt32 deviceId, VJoyAxis axis, out Double value, out Int32 x, out Int32 y)
        {

            var joy = this.GetJoystick(deviceId);
            x = 0;
            y = 0;
            value = 0;
            try
            {

                if (!joy.Aquired)
                    joy.Aquire();

                if (joy.Aquired)
                {
                    if (this.IsAxisHat(axis))
                    {


                        // read hat value
                        Hats hat = this.WrapperHatFromAxis(axis);

                        // convert to x/y values
                        var hat_value = joy.GetJoystickHat(hat);
                        this.HatCoords(hat_value, out x, out y);
                        value = hat_value;
                        return true;
                    }

                    // read regular axis
                    var wrapper_axis = this.WrapperAxisFromAxis(axis);
                    var axis_value = joy.GetJoystickAxis(wrapper_axis);
                    value = this.RangeFromVjoy(axis_value);
                    return true;

                }

                return false;
            }
            finally
            {
                joy.Release();
            }
        }


        /// <summary>
        /// converts from an axis enum to a vjoy HID axis number
        /// </summary>
        /// <param name="deviceId">joystick id, 0 based</param>
        /// <param name="axis"></param>
        /// <param name="value"></param>
        /// <param name="x">x value</param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SetAxis(UInt32 deviceId, VJoyAxis axis, Double value, Int32 x = 0, Int32 y = 0)
        {

            var joy = this.GetJoystick(deviceId);
            if (!joy.Aquired)
                joy.Aquire();
            if (joy.Aquired)
            {

                Boolean isHat = this.IsAxisHat(axis);

                if (isHat)
                {
                    Hats hat = this.WrapperHatFromAxis(axis);
                    var hat_value = this.HatValue(x, y);
                    joy.SetJoystickHat(hat_value, hat);
                    //MacroDeckLogger.Info(Main.Instance,$"Set hat: {deviceId} {hat} {x} {y} {hat_value}");

                }
                else
                {
                    Axis vax = this.WrapperAxisFromAxis(axis);
                    Int32 axis_value = this.ToVjoyRange(value);
                    joy.SetJoystickAxis(axis_value, vax);
                    //MacroDeckLogger.Info(Main.Instance, $"Set axis: {deviceId} {axis} {value}");

                }
                joy.Release();
            }

        }


        public VJoyAxis AxisFromName(String name)
        {
            switch (name)
            {
                case "X":
                case "AXIS_X":
                    return VJoyAxis.AXIS_X;
                case "Y":
                case "AXIS_Y":
                    return VJoyAxis.AXIS_Y;
                case "Z":
                case "AXIS_Z":
                    return VJoyAxis.AXIS_Z;
                case "Rx":
                case "AXIS_RX":
                    return VJoyAxis.AXIS_RX;
                case "Ry":
                case "AXIS_RY":
                    return VJoyAxis.AXIS_RY;
                case "Rz":
                case "AXIS_RZ":
                    return VJoyAxis.AXIS_RZ;
                case "sl0":
                case "AXIS_SL0":
                    return VJoyAxis.AXIS_SL0;
                case "sl1":
                case "AXIS_SL1":
                    return VJoyAxis.AXIS_SL1;
                case "hat0":
                case "AXIS_HAT0":
                    return VJoyAxis.AXIS_HAT0;
                case "hat1":
                case "AXIS_HAT1":
                    return VJoyAxis.AXIS_HAT1;
                case "hat2":
                case "AXIS_HAT2":
                    return VJoyAxis.AXIS_HAT2;
                case "hat3":
                case "AXIS_HAT3":
                    return VJoyAxis.AXIS_HAT3;

            }

            return VJoyAxis.AXIS_X;

        }

        /// <summary>
        /// checks if the given axis is a hat axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public Boolean IsAxisHat(VJoyAxis axis)
        {
            switch (axis)
            {
                case VJoyAxis.AXIS_HAT0:
                case VJoyAxis.AXIS_HAT1:
                case VJoyAxis.AXIS_HAT2:
                case VJoyAxis.AXIS_HAT3:
                    return true;
            }
            return false;
        }



    }

}

namespace Loupedeck.MumiPlugin

{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;
    using System.Web.UI.WebControls;

    using muchimi.vjoy;


    /// <summary>
    /// holds a block of decoded data
    /// </summary>
    internal class DecodeData
    {
        public UInt32 deviceId;
        public UInt32 leftButtonId;
        public UInt32 rightButtonId;
        public UInt32 actionButtonId;
        public UInt32 fastLeftButtonId;
        public UInt32 fastRightButtonId;
        public UInt32 duration;
        public VJoyJob.JobMode mode;
        public VJoyJob.JobMode actionMode;
        public bool isEncoder;
        public bool valid { get; private set; }


        /// <summary>
        /// creates a rotary encoder decode block
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="leftButtonId"></param>
        /// <param name="rightButtonId"></param>
        /// <param name="actionButtonId"></param>
        /// <param name="fastLeftButtonId"></param>
        /// <param name="fastRightButtonId"></param>
        /// <param name="duration"></param>
        /// <param name="mode"></param>
        /// <param name="actionMode"></param>
        public DecodeData(UInt32 deviceId, UInt32 leftButtonId, UInt32 rightButtonId, UInt32 actionButtonId, UInt32 fastLeftButtonId, UInt32 fastRightButtonId, UInt32 duration = 250, VJoyJob.JobMode mode = VJoyJob.JobMode.Pulse, VJoyJob.JobMode actionMode = VJoyJob.JobMode.Pulse)
        {
            this.deviceId = deviceId;
            this.leftButtonId = leftButtonId;
            this.rightButtonId = rightButtonId;
            this.actionButtonId = actionButtonId;
            this.fastLeftButtonId = fastLeftButtonId;
            this.fastRightButtonId = fastRightButtonId;
            this.mode = mode;
            this.actionMode = actionMode;

            if (duration < 0)
            {
                duration = 250;
            }

            if (actionButtonId < 0)
            {
                actionButtonId = 0;
            }
            this.duration = duration;
            this.isEncoder = true;

            // basic validation
            this.valid = deviceId > 0 && deviceId <= 8 && leftButtonId > 0 && rightButtonId > 0  &&
                    leftButtonId <= 128 && rightButtonId <= 128 && actionButtonId <= 128 && fastLeftButtonId <= 128 &&
                    fastRightButtonId <= 128;
        }

        /// <summary>
        /// creates a button action decode
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="buttonId"></param>
        /// <param name="duration"></param>
        /// <param name="mode"></param>
        public DecodeData(UInt32 deviceId, UInt32 buttonId, UInt32 duration = 250, VJoyJob.JobMode mode = VJoyJob.JobMode.Pulse)
        {
            this.deviceId = deviceId;
            this.actionButtonId = buttonId;
            this.actionMode = mode;
            this.duration = duration;
            this.isEncoder = false;

            // basic validation
            this.valid = deviceId > 0 && deviceId <= 8 && buttonId > 0 && buttonId <= 128;

        }

        public override String ToString()
        {
            if (this.isEncoder)
            {
                return
                    $"Encoder: deviceID: {this.deviceId} left: {this.leftButtonId}/{this.fastLeftButtonId} right: {this.rightButtonId}/{this.fastRightButtonId} action: {this.actionButtonId} action mode: {this.actionMode} duration: {this.duration} valid: {this.valid}";
            }
            return $"Button: deviceID: {this.deviceId} action: {this.actionButtonId} action mode: {this.actionMode} duration {this.duration} valid: {this.valid}";
        }
        
    }

    /// <summary>
    /// implements a caching system to decode button and encoder command strings -
    /// a cache is used to eliminate parsing
    /// </summary>
    internal class Decoder
    {


        private readonly Dictionary<string, DecodeData> _decodeCache = new Dictionary<String, DecodeData>();


        public DecodeData Decode(string actionParameter)
        {
            var action = actionParameter.ToUpperInvariant();
            if (this._decodeCache.ContainsKey(action))
            {
                return this._decodeCache[action];
            }

            var data = actionParameter.Contains("L") ? this.DecodeRotation(action) : this.DecodeAction(action);
            this._decodeCache[action] = data;
            return data;

        }


        /// <summary>
        /// decodes a rotation encoder
        /// </summary>
        /// <param name="actionParameter"></param>
        /// <returns></returns>
        public DecodeData DecodeRotation(string actionParameter)
        {
            UInt32 deviceId = 0;
            UInt32 leftButtonId = 0;
            UInt32 rightButtonId = 0;
            UInt32 fastLeftButtonId = 0;
            UInt32 fastRightButtonId = 0;
            UInt32 actionButtonId = 0;
            UInt32 duration = 250;
            var data = actionParameter.ToUpperInvariant();
            var matches = Regex.Matches(data, @"([TF]?[A-Z]\s*\d+)");
            var mode = VJoyJob.JobMode.Pulse;
            var actionMode = VJoyJob.JobMode.Pulse;
            foreach (var match in matches)
            {
                var pairMatch = Regex.Match(match.ToString(), @"([A-Z]+)\s*(\d+)");
                var groups = pairMatch.Groups;
                // groups will be the complete item, letter code, number (expecting 4 DLRA and one optional duration D
                if (groups.Count < 3)
                {
                    continue;
                }

                var code = groups[1].Value;
                if (UInt32.TryParse(groups[2].Value, out var value))
                {
                    switch (code)
                    {
                        case "D":
                            deviceId = value;
                            break;
                        case "L":
                            leftButtonId = value;
                            break;
                        case "R":
                            rightButtonId = value;
                            break;
                        case "FL":
                            fastLeftButtonId = value;
                            break;
                        case "FR":
                            fastRightButtonId = value;
                            break;
                        case "A":
                            actionButtonId = value;
                            break;
                        case "TA":
                            actionButtonId = value;
                            actionMode = VJoyJob.JobMode.Toggle;
                            break;
                        case "P":
                            duration = value;
                            break;
                    }
                }
            }

            // if fast buttons not provided, use the slow button data for fast moves
            if (fastLeftButtonId == 0)
            {
                fastLeftButtonId = leftButtonId;
            }

            if (rightButtonId == 0)
            {
                fastRightButtonId = rightButtonId;
            }
            

            return new DecodeData(deviceId, leftButtonId, rightButtonId, actionButtonId, fastLeftButtonId,
                fastRightButtonId, duration, mode, actionMode);
        }


        /// <summary>
        /// decodes a standard button string
        ///
        /// D device_id  B button_id  P duration
        ///
        /// Duration is optional.
        /// On success, device ID and button ID will be non zero when returned
        /// </summary>
        /// <param name="actionParameter">input command string</param>
        public DecodeData DecodeAction(string actionParameter)
        {
            UInt32 deviceId = 0;
            UInt32 buttonId = 0;
            UInt32 duration = 250;
            var mode = VJoyJob.JobMode.Pulse;
            var data = actionParameter.ToUpperInvariant();
            var matches = Regex.Matches(data, @"([T]?[A-Z]\s*\d+)");
            foreach (var match in matches)
            {
                var pairMatch = Regex.Match(match.ToString(), @"([A-Z]+)\s*(\d+)");
                var groups = pairMatch.Groups;
                // groups will be the complete item, letter code, number
                if (groups.Count < 3)
                {
                    continue;
                }

                var code = groups[1].Value;
                if (UInt32.TryParse(groups[2].Value, out var value))
                {
                    switch (code)
                    {
                        case "D":
                            deviceId = value;
                            break;
                        case "B":
                            buttonId = value;
                            break;
                        case "PB":
                            buttonId = value;
                            mode = VJoyJob.JobMode.Press;
                            break;
                        case "RB":
                            buttonId = value;
                            mode = VJoyJob.JobMode.Release;
                            break;
                        case "TB":
                            buttonId = value;
                            mode = VJoyJob.JobMode.Toggle;
                            break;
                        case "P":
                            duration = value;
                            break;
                    }
                }
            }

            return new DecodeData(deviceId, buttonId, duration, mode);
        }
    }

    class MumiVjoyDynamicRotation : PluginDynamicAdjustment
    {

        private MumiPlugin Plugin => base.Plugin as MumiPlugin;
        private VjoyInstance Vjoy => this.Plugin.Vjoy;

        private BlockingCollectionQueue Queue => this.Plugin.Queue;


        public MumiVjoyDynamicRotation() : base(displayName: "Vjoy Rotation",
            description: "Attaches knob rotation to joystick axis or buttons", groupName: "Joystick", hasReset: false)
        {
            base.MakeProfileAction("text;Rotation Configuration D#L#[FL#][FR#]R#[T]A#P#");
        }


        /// <summary>
        /// decodes a rotary encoding string  in the format DLRAP FL FR  (device, left button, right button, action button (press), pulse duration (in ms), fast left, fast right
        /// </summary>
        /// <param name="actionParameter"></param>
        /// <param name="deviceId"></param>
        /// <param name="leftButtonId"></param>
        /// <param name="rightButtonId"></param>
        /// <param name="fastLeftButtonId"></param>
        /// <param name="fastRightButtonId"></param>
        /// <param name="actionButtonId"></param>
        /// <param name="actionToggle">true if the action parameter is a toggle flag</param>
        /// <param name="duration"></param>
      



        protected override void ApplyAdjustment(string actionParameter, int diff)
        {
            var data = this.Plugin.Decoder.Decode(actionParameter);
            if (!data.valid)
            {
                return;
            }

            // number of ticks for a "Fast" move - 5 is typical of a "fast" move
            int threshold = 5;

            var fast = Math.Abs(diff) >= threshold;
            var msg = $"ROTATE: direction: {diff} data: {data}";
                      MumiLog.Info(fast ? $"{msg} Fast" : $"{msg} Slow");

            if (diff < 0)
            {
                this.Plugin.Pulse(data.deviceId, fast ? data.fastLeftButtonId : data.leftButtonId,
                    data.duration);
            }
            else if (diff > 0)
            {
                this.Plugin.Pulse(data.deviceId, fast ? data.fastRightButtonId : data.rightButtonId,
                    data.duration);
            }
        }




        /// <summary>
        /// called when the encoder button is pressed
        /// </summary>
        /// <param name="actionParameter"></param>
        protected override void RunCommand(string actionParameter)
        {
            var data = this.Plugin.Decoder.Decode(actionParameter);
            if (!data.valid)
            {
                return;
            }

            MumiLog.Info($"ENCODER ACTION: {data}");

            if (data.actionButtonId > 0)
            {
                // queue up press action
                switch (data.actionMode)
                {
                    case VJoyJob.JobMode.Pulse:
                        this.Plugin.Pulse(data.deviceId, data.actionButtonId, data.duration);
                        break;
                    case VJoyJob.JobMode.Press:
                        this.Plugin.Press(data.deviceId, data.actionButtonId);
                        break;
                    case VJoyJob.JobMode.Release:
                        this.Plugin.Release(data.deviceId, data.actionButtonId);
                        break;
                    case VJoyJob.JobMode.Toggle:
                        this.Plugin.Toggle(data.deviceId, data.actionButtonId);
                        break;

                }
            }
        }



        class MumiButtonPress : PluginDynamicCommand
        {

            private MumiPlugin Plugin => base.Plugin as MumiPlugin;
            private VjoyInstance Vjoy => this.Plugin.Vjoy;

            public MumiButtonPress() : base()
            {
                this.DisplayName = "Button Press";
                this.Description = "Presses a button";
                base.MakeProfileAction("text;Command D#[T]B#[P#]");
            }

           
            


            protected override void RunCommand(string actionParameter)
            {
                var data = this.Plugin.Decoder.Decode(actionParameter);
                if (!data.valid)
                {
                    return;
                }

                MumiLog.Info($"ACTION: {data}");

                if (data.actionButtonId > 0)
                {
                    // queue up press action
                    switch (data.actionMode)
                    {
                        case VJoyJob.JobMode.Pulse:
                            this.Plugin.Pulse(data.deviceId, data.actionButtonId, data.duration);
                            break;
                        case VJoyJob.JobMode.Press:
                            this.Plugin.Press(data.deviceId, data.actionButtonId);
                            break;
                        case VJoyJob.JobMode.Release:
                            this.Plugin.Release(data.deviceId, data.actionButtonId);
                            break;
                        case VJoyJob.JobMode.Toggle:
                            this.Plugin.Toggle(data.deviceId, data.actionButtonId);
                            break;

                    }
                }

            }


        }
    }
}
namespace Loupedeck.MumiPlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Xml;

    using muchimi.vjoy;

    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Timers;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.WebControls.WebParts;
    using System.Xml.Linq;

    using log4net;

    using Timer = System.Threading.Timer;


    enum ActionTypeEnum
    {
        None = 0,
        Left,
        Right,
        Push,
    }

    public enum ControllerTypeEnum
    {
        None = 0,
        Encoder, // rotary knobs 1 to 6, 1-3 left, 4-6 right
        Button, // lower button 1 to 8 from left to right
        Touch, // touch button 1 to 12 top row 1-4, second row 5-8, third row 9-12
    }

    /// <summary>
    /// unique controller on the device
    /// </summary>
    public enum ControllerIdEnum
    {
        None = 0,
        Encoder1 = 101,
        Encoder2 = 102,
        Encoder3 = 103,
        Encoder4 = 104,
        Encoder5 = 105,
        Encoder6 = 106,
        Button1 = 201,
        Button2 = 202,
        Button3 = 203,
        Button4 = 204,
        Button5 = 205,
        Button6 = 206,
        Button7 = 207,
        Button8 = 208,
        Touch1 = 301,
        Touch2 = 302,
        Touch3 = 303,
        Touch4 = 304,
        Touch5 = 305,
        Touch6 = 306,
        Touch7 = 307,
        Touch8 = 308,
        Touch9 = 309,
        Touch10 = 310,
        Touch11 = 311,
        Touch12 = 312,
    }

    public class ControllerBase
    {
        /// <summary>
        /// controller ID on the device
        /// </summary>
        public ControllerIdEnum ControllerId { get; set; }
        public string Icon { get; set; }

        /// <summary>
        /// touch, button or encoder
        /// </summary>
        public ControllerTypeEnum ControllerType { get; set; }



        public bool Valid { get; internal set; }


        public ControllerBase()
        {
            this.ControllerType = ControllerTypeEnum.None;
            this.ControllerId = ControllerIdEnum.None;
            this.Valid = false;
        }


        /// <summary>
        /// get action param name from controller ID
        /// </summary>
        public String ActionParam
        {
            get
            {
                if (this.ControllerId == ControllerIdEnum.None)
                    return String.Empty;
                return this.ControllerId.ToString();
            }
        }
        /// <summary>
        /// get controller from action param
        /// </summary>
        /// <param name="actionParameter"></param>
        /// <returns></returns>
        public static ControllerIdEnum FromActionParam(String actionParameter)
        {
            if (Enum.TryParse<ControllerIdEnum>(actionParameter, out var value))
            {
                return value;
            }

            return ControllerIdEnum.None;

        }


        public int ReadAttInt(XElement e, string attribute, int defaultValue = 0)
        {
            var att = e.Attribute(attribute);
            if (att == null)
            {
                return defaultValue;
            }

            if (int.TryParse(att.Value, out var value))
            {
                return value;
            }

            return defaultValue;

        }

        public string ReadAttString(XElement e, string attribute, string defaultValue = null)
        {
            var att = e.Attribute(attribute);
            if (att == null)
            {
                return defaultValue;
            }

            return att.Value.ToLowerInvariant();
        }
    }

    /// <summary>
    /// holds a button action definition
    /// </summary>
    public class ButtonData : ControllerBase
    {
        public UInt32 DeviceId { get; set; }
        public UInt32 ButtonId { get; set; }
        public UInt32 Duration { get; set; }



        // reads a button element
        public bool Read(XElement e)
        {
            this.Valid = false;
            var att = e.Attribute("device");

            this.DeviceId = (UInt32)this.ReadAttInt(e, "device");
            this.ButtonId = (UInt32)this.ReadAttInt(e, "button");

            if (this.DeviceId == 0)
            {
                MumiLog.Error($"Missing or invalid device ID - value expected 1+ in {e}");
                return false;
            }

            if (this.DeviceId > 16)
            {
                MumiLog.Error($"Invalid button ID - range 1..16 expected - got {this.DeviceId} in {e}");
                return false;
            }

            if (this.ButtonId == 0)
            {
                MumiLog.Error($"Missing or invalid button ID - value expected 1+ in {e}");
                return false;
            }

            if (this.ButtonId > 128)
            {
                MumiLog.Error($"Invalid button ID - range 1..128 expected - got {this.ButtonId} in {e}");
                return false;
            }

            var duration = (UInt32)this.ReadAttInt(e, "duration");
            if (duration == 0)
            {
                duration = 1000;
            }

            this.Duration = duration;

            MumiLog.Info($"BUTTON: read ok: device: {this.DeviceId} button: {this.ButtonId} duration: {this.Duration}");

            this.Valid = true;
            return true;
        }

        public ButtonData()
        {
            this.DeviceId = 0;
            this.ButtonId = 0;
            this.Duration = 1000;
        }

        public ButtonData(XElement e) : this()
        {
            this.Read(e);
        }

    }


    /// <summary>
    /// holds an encoder action definition
    /// </summary>
    public class ActionData : ControllerBase
    {


        public ButtonData ButtonLeft { get; set; }
        public ButtonData ButtonRight { get; set; }

        public ButtonData ButtonPush { get; set; }






        /// <summary>
        ///  the button's image (if the button supports it)
        /// </summary>
        public String Icon { get; set; }

        public ActionData()
        {


        }

        public ActionData(XElement e) : this()
        {
            this.Read(e);
        }


        public bool Read(XElement e)
        {

            var id = base.ReadAttString(e, "id");
            if (id == null)
            {
                MumiLog.Error($"missing ID in {e}");
                this.Valid = false;
                return false;
            }

            this.Icon = base.ReadAttString(e, "icon");

            // encoder type
            var eType = e.Name.LocalName.ToLowerInvariant();
            switch (eType)
            {
                case "encoder":
                    this.ControllerType = ControllerTypeEnum.Encoder;
                    break;
                case "button":
                    this.ControllerType = ControllerTypeEnum.Button;
                    break;
                case "touch":
                    this.ControllerType = ControllerTypeEnum.Touch;
                    break;
                default:
                    MumiLog.Error($"ACTIONDATA: invalid controller type: {eType} in {e}");
                    this.Valid = false;
                    return false;

            }

            switch (id)
            {
                case "e1":
                case "r1":
                    this.ControllerId = ControllerIdEnum.Encoder1;
                    break;
                case "e2":
                case "r2":
                    this.ControllerId = ControllerIdEnum.Encoder2;
                    break;
                case "e3":
                case "r3":
                    this.ControllerId = ControllerIdEnum.Encoder3;
                    break;
                case "e4":
                case "r4":
                    this.ControllerId = ControllerIdEnum.Encoder4;
                    break;
                case "e5":
                case "r5":
                    this.ControllerId = ControllerIdEnum.Encoder5;
                    break;
                case "e6":
                case "r6":
                    this.ControllerId = ControllerIdEnum.Encoder6;
                    break;
                case "b1":
                    this.ControllerId = ControllerIdEnum.Button1;
                    break;
                case "b2":
                    this.ControllerId = ControllerIdEnum.Button2;
                    break;
                case "b3":
                    this.ControllerId = ControllerIdEnum.Button3;
                    break;
                case "b4":
                    this.ControllerId = ControllerIdEnum.Button4;
                    break;
                case "b5":
                    this.ControllerId = ControllerIdEnum.Button5;
                    break;
                case "b6":
                    this.ControllerId = ControllerIdEnum.Button6;
                    break;
                case "b7":
                    this.ControllerId = ControllerIdEnum.Button7;
                    break;
                case "b8":
                    this.ControllerId = ControllerIdEnum.Button8;
                    break;
                case "t1":
                    this.ControllerId = ControllerIdEnum.Touch1;
                    break;
                case "t2":
                    this.ControllerId = ControllerIdEnum.Touch2;
                    break;
                case "t3":
                    this.ControllerId = ControllerIdEnum.Touch3;
                    break;
                case "t4":
                    this.ControllerId = ControllerIdEnum.Touch4;
                    break;
                case "t5":
                    this.ControllerId = ControllerIdEnum.Touch5;
                    break;
                case "t6":
                    this.ControllerId = ControllerIdEnum.Touch6;
                    break;
                case "t7":
                    this.ControllerId = ControllerIdEnum.Touch7;
                    break;
                case "t8":
                    this.ControllerId = ControllerIdEnum.Touch8;
                    break;
                case "t9":
                    this.ControllerId = ControllerIdEnum.Touch9;
                    break;
                case "t10":
                    this.ControllerId = ControllerIdEnum.Touch10;
                    break;
                case "t11":
                    this.ControllerId = ControllerIdEnum.Touch11;
                    break;
                case "t12":
                    this.ControllerId = ControllerIdEnum.Touch12;
                    break;
                default:
                    // unknown
                    MumiLog.Error($"unknown ID in {e}");
                    this.Valid = false;
                    return false;

            }



            switch (this.ControllerType)
            {
                case ControllerTypeEnum.Encoder:
                    // encoder definition

                    // read button definitions
                    foreach (var ctrl in e.Elements())
                    {
                        var button = new ButtonData(ctrl);
                        var ctype = ctrl.Name.LocalName.ToLowerInvariant();
                        switch (ctype)
                        {
                            case "ccw":
                            case "left":
                                this.ButtonLeft = button;
                                break;
                            case "cw":
                            case "right":
                                this.ButtonRight = button;
                                break;
                            case "action":
                            case "push":
                                this.ButtonPush = button;
                                break;
                            default:
                                // unknown
                                MumiLog.Error($"unknown controller action type {ctype} in {e}");
                                this.Valid = false;
                                return false;
                        }
                    }

                    break;

                case ControllerTypeEnum.Button:
                case ControllerTypeEnum.Touch:
                    foreach (var ctrl in e.Elements())
                    {
                        var button = new ButtonData(ctrl);
                        var ctype = ctrl.Name.LocalName.ToLowerInvariant();
                        switch (ctype)
                        {
                            case "action":
                            case "push":
                                this.ButtonPush = button;
                                break;
                            default:
                                // unknown
                                MumiLog.Error($"unknown controller action type {ctype} in {e}");
                                this.Valid = false;
                                return false;
                        }


                        break;
                    }

                    break;

            }

            this.Valid = true;
            return true;

        }
    }

    public class PageData
    {
        public Dictionary<ControllerIdEnum, ActionData> Actions { get; private set; }
        public String Name { get; private set; }

        public PageData Parent { get; private set; }

        public PageData()
        {
            this.Actions = new Dictionary<ControllerIdEnum, ActionData>();
            this.Parent = null;
        }

        public PageData(XElement e) : this()
        {
            this.Read(e);
        }

        void Read(XElement e)
        {
            String pageName = e.GetAttributeValue("name", null);
            if (pageName == null)
                pageName = "Default";

            this.Name = pageName;

            foreach (var xDef in e.Elements())
            {

                var action = new ActionData(xDef);
                this.Actions[action.ControllerId] = action;

            }
        }

        /// <summary>
        /// gets the action data for a particular controller on the page
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public ActionData GetAction(ControllerIdEnum controller)
        {
            if (!this.Actions.ContainsKey(controller))
            {
                // add a default no-op action if not defined yet
                var action = new ActionData();
                this.Actions[controller] = action;
            }

            return this.Actions[controller];
        }

        String GetActionParam(ControllerIdEnum controller)
        {
            if (this.Actions.ContainsKey(controller))
            {
                return this.Actions[controller].ActionParam;
            }
            return string.Empty;
        }

        /// <summary>
        ///  gets 12 touch command names for the current page
        /// </summary>
        /// <returns></returns>
        public IEnumerable<String> GetTouchNames()
        {
            var actionParams = new List<String>
            {
                    this.GetActionParam(ControllerIdEnum.Touch1),
                    this.GetActionParam(ControllerIdEnum.Touch2),
                    this.GetActionParam(ControllerIdEnum.Touch3),
                    this.GetActionParam(ControllerIdEnum.Touch4),
                    this.GetActionParam(ControllerIdEnum.Touch5),
                    this.GetActionParam(ControllerIdEnum.Touch6),
                    this.GetActionParam(ControllerIdEnum.Touch7),
                    this.GetActionParam(ControllerIdEnum.Touch8),
                    this.GetActionParam(ControllerIdEnum.Touch9),
                    this.GetActionParam(ControllerIdEnum.Touch10),
                    this.GetActionParam(ControllerIdEnum.Touch11),
                    this.GetActionParam(ControllerIdEnum.Touch12)
                };



            return actionParams;
        }
        public IEnumerable<String> GetEncoderNames()
        {
            var actionParams = new List<String>
            {
                this.GetActionParam(ControllerIdEnum.Encoder1),
                this.GetActionParam(ControllerIdEnum.Encoder2),
                this.GetActionParam(ControllerIdEnum.Encoder3),
                this.GetActionParam(ControllerIdEnum.Encoder4),
                this.GetActionParam(ControllerIdEnum.Encoder5),
                this.GetActionParam(ControllerIdEnum.Encoder6),
            };
            return actionParams;
        }

        public IEnumerable<String> GetEncoderPressNames()
        {
            var actionParams = new List<String>
            {
                this.GetActionParam(ControllerIdEnum.Encoder1),
                this.GetActionParam(ControllerIdEnum.Encoder2),
                this.GetActionParam(ControllerIdEnum.Encoder3),
                this.GetActionParam(ControllerIdEnum.Encoder4),
                this.GetActionParam(ControllerIdEnum.Encoder5),
                this.GetActionParam(ControllerIdEnum.Encoder6),
            };
            return actionParams;
        }




    }

    public class ConfigData
    {
        public String Application { get; private set; }
        public Dictionary<String, PageData> Pages { get; private set; }
        public Boolean Loaded { get; private set; }

        public ConfigData(String source)
        {
            this.Pages = new Dictionary<String, PageData>();
            var loaded = this.LoadConfig(source);
            this.Loaded = loaded;
        }

        Boolean LoadConfig(String source = @"\config\mumi.xml")
        {



            if (File.Exists(source))
            {

                var xml = XDocument.Load(source);

                foreach (var config in xml.Descendants("config"))
                {
                    String application = config.GetAttributeValue("application", null);
                    if (application == null)
                        return false;

                    this.Application = application;

                    foreach (var xPage in config.Descendants("page"))
                    {
                        var page = new PageData(xPage);
                        this.Pages[page.Name] = page;

                    }
                }

                return true;

            }

            return false;


        }

        /// <summary>
        /// gets a particular page
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PageData GetPage(string name)
        {
            if (this.Loaded && this.Pages.ContainsKey(name))
                return this.Pages[name];
            return null;
        }

    }



    /// <summary>
    ///  class that tracks vjoy actions
    /// </summary>
    public class VJoyJob
    {

        public enum JobMode
        {
            Press,
            Release,
            Pulse,
            Toggle,
        }
        /// <summary>
        ///  vjoy device id, 1 based
        /// </summary>
        public UInt32 deviceId { get; set; }

        /// <summary>
        ///  id of the button to set, 1 based, max 128
        /// </summary>
        public UInt32 buttonId { get; set; }

        /// <summary>
        ///  duration in ms of a pulse
        /// </summary>
        public UInt32 duration { get; set; }

        public JobMode mode { get; internal set; }


        /// <summary>
        // for timed items
        /// </summary>
        private System.Threading.Timer timer;

        private BlockingCollectionQueue queue;

        public VJoyJob(UInt32 deviceId, UInt32 buttonid, JobMode mode, UInt32 duration = 250)
        {
            this.deviceId = deviceId;
            this.buttonId = buttonid;
            this.duration = duration;
            this.mode = JobMode.Pulse; // always a pulse job mode

        }

        /// <summary>
        ///  enqueues the job after specified delay
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="duration"></param>
        internal void StartTimer(BlockingCollectionQueue queue, UInt32 duration = 250)
        {
            if (this.timer == null)
            {
                this.timer = new Timer(this.Callback);
            }
            this.queue = queue;
            this.timer.Change(duration, Timeout.Infinite);
        }

        /// <summary>
        /// called when the timer lapses
        /// </summary>
        /// <param name="state"></param>
        private void Callback(object state)
        {
            // stop the timer
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);

            this.mode = JobMode.Release;
            // queue the button release
            this.queue.Enqueue(this);
        }

        /// <summary>
        /// stops the timer
        /// </summary>
        public void StopTimer()
        {
            if (this.timer != null)
            {
                this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            }

        }

        
    }

    public class TimerTask
    {
        public int WaitTime { get; set; }
        public VJoyJob Job { get; set; }
    }


    /// <summary>
    /// vjoy work queue to queue vjoy requests in thread safe manner
    /// </summary>
    internal class BlockingCollectionQueue
    {
        private BlockingCollection<VJoyJob> jobs = new BlockingCollection<VJoyJob>();

        private VjoyInstance Vjoy;


        public BlockingCollectionQueue(VjoyInstance vjoy)
        {
            var thread = new Thread(new ThreadStart(this.OnStart));
            thread.IsBackground = true;
            thread.Start();
            this.Vjoy = vjoy;
        }

      
        public void Enqueue(VJoyJob job)
        {
            this.jobs.Add(job);
        }

        private void OnStart()
        {
            // this blocks until an item is added to the queue - avoiding a while loop - also thread safe
            foreach (var job in this.jobs.GetConsumingEnumerable(CancellationToken.None))
            {
                switch (job.mode)
                {
                    case VJoyJob.JobMode.Press:
                        this.Vjoy.Press(job.deviceId, job.buttonId);
                        break;
                    case VJoyJob.JobMode.Release:
                        this.Vjoy.Release(job.deviceId, job.buttonId);
                        break;
                    case VJoyJob.JobMode.Pulse:
                        // create a new timed job to release the button after the duration is elapsed - the press is processed here
                        // the release is queued when the timer lapses

                        this.Vjoy.Press(job.deviceId, job.buttonId);
                        job.StartTimer(this, job.duration);
                        
                        break;
                    case VJoyJob.JobMode.Toggle:
                        this.Vjoy.ToggleButton(job.deviceId,job.buttonId);
                        break;
                    
                    
                }

            }
        }
    }



    public class MumiPlugin : Plugin
    {
        public VjoyInstance Vjoy { get; private set; }

        public ConfigData Config { get; private set; }

        // output to VJOY has no application dependency
        // public override Boolean HasNoApplication => true;

        internal Decoder Decoder { get; private set; }

        internal BlockingCollectionQueue Queue { get; private set; }


        public void Pulse(UInt32 deviceID, UInt32 buttonId, UInt32 duration = 250)
        {
            this.Queue.Enqueue(new VJoyJob(deviceID, buttonId,  VJoyJob.JobMode.Pulse, duration));
        }

        public void Press(UInt32 deviceID, UInt32 buttonId)
        {
            this.Queue.Enqueue(new VJoyJob(deviceID, buttonId, VJoyJob.JobMode.Press));
        }

        public void Release(UInt32 deviceID, UInt32 buttonId)
        {
            this.Queue.Enqueue(new VJoyJob(deviceID, buttonId, VJoyJob.JobMode.Release));
        }

        public void Toggle(UInt32 deviceID, UInt32 buttonId)
        {
            this.Queue.Enqueue(new VJoyJob(deviceID, buttonId, VJoyJob.JobMode.Toggle));
        }


        public override void Load()
        {




            this.LoadPluginIcons();
            this.Vjoy = new VjoyInstance();


            //this.ClientApplication.SendKeyboardShortcut(VirtualKeyCode.KeyA, ModifierKey.ExtendedKeyboard | ModifierKey.Alt);


            var pluginDataDirectory = this.GetPluginDataDirectory();
            if (IoHelpers.EnsureDirectoryExists(pluginDataDirectory))
            {

                MumiLog.Config(pluginDataDirectory);


                //var filePath = Path.Combine(pluginDataDirectory, "mumi.xml");
                //if (File.Exists(filePath))
                //    this.Config = new ConfigData(filePath);
                //MumiLog.Info($"PLUGIN LOAD: config file: {filePath}");
            }




            this.Queue = new BlockingCollectionQueue(this.Vjoy);


            this.Decoder = new Decoder();
        }

        public override void Unload()
        {
            MumiLog.Info($"PLUGIN UNLOAD");
            this.Vjoy.Release();
        }

        private void OnApplicationStarted(Object sender, EventArgs e)
        {
        }

        private void OnApplicationStopped(Object sender, EventArgs e)
        {
        }

        public override void RunCommand(String commandName, String parameter)
        {
        }

        public override void ApplyAdjustment(String adjustmentName, String parameter, Int32 diff)
        {
        }

        private void LoadPluginIcons()
        {
            // Icons for Loupedeck application UI
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.MumiPlugin.Icons.PluginIcon16x16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.MumiPlugin.Icons.PluginIcon32x32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.MumiPlugin.Icons.PluginIcon48x48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.MumiPlugin.Icons.PluginIcon256x256.png");
        }



    }

    //public class MumiButtonAreaFolder : PluginDynamicFolder
    //{

    //    MumiPlugin MumiPlugin => this.Plugin as MumiPlugin;
    //    ConfigData Config => this.MumiPlugin.Config;

    //    private String currentPage;

    //    public MumiButtonAreaFolder()
    //    {
    //        this.DisplayName = "Mumi";
    //        this.GroupName = "Mumi";
    //        this.Navigation = PluginDynamicFolderNavigation.None;
    //        this.currentPage = "Default";
    //    }

    //    public override Boolean Load()
    //    {
    //        return true;
    //    }

    //    public override Boolean Unload()
    //    {
    //        return true;
    //    }

    //    public override IEnumerable<String> GetButtonPressActionNames(DeviceType deviceType)
    //    {

    //        if (this.Config == null)
    //            // config is not loaded
    //            return null;

    //        // load the page
    //        PageData page = this.Config.GetPage(this.currentPage);
    //        if (page == null)
    //            return null;


    //        var actionCommands = new List<string>();
    //        foreach (var action in page.GetTouchNames())
    //        {
    //            actionCommands.Add(this.CreateCommandName(action));
    //        }

    //        var backControllerId = ControllerIdEnum.Touch12;
    //        if (backControllerId != ControllerIdEnum.None)
    //        {
    //            var index = ((int)backControllerId % 100);
    //            actionCommands[index - 1] = PluginDynamicFolder.NavigateUpActionName;
    //        }

    //        return actionCommands;
    //    }

    //    /// <summary>
    //    /// gets the display text or image for a command
    //    /// </summary>
    //    /// <param name="actionParameter"></param>
    //    /// <param name="imageSize"></param>
    //    /// <returns></returns>
    //    public override String GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
    //    {
    //        return actionParameter;
    //    }


    //    public override String GetAdjustmentDisplayName(string actionParameter, PluginImageSize imageSize)
    //    {
    //        return actionParameter;
    //    }



    //    public override IEnumerable<String> GetEncoderRotateActionNames(DeviceType deviceType)
    //    {
    //        if (this.Config == null)
    //            // config is not loaded
    //            return null;

    //        // load the page
    //        PageData page = this.Config.GetPage(this.currentPage);
    //        if (page == null)
    //            return null;

    //        var actionCommands = new List<string>();
    //        foreach (var action in page.GetEncoderNames())
    //        {
    //            actionCommands.Add(this.CreateCommandName(action));
    //        }

    //        return actionCommands;

    //    }


    //    public override String GetButtonDisplayName(PluginImageSize imageSize)
    //    {
    //        return "Mumi";
    //    }


    //    public override Boolean ProcessEncoderEvent(string actionParameter, DeviceEncoderEvent encoderEvent)
    //    {

    //        // process the action
    //        PageData page = this.Config.GetPage(this.currentPage);
    //        if (page == null)
    //            return false;

    //        var controller = ControllerBase.FromActionParam(actionParameter);
    //        var action = page.GetAction(controller);
    //        if (action == null)
    //            return false;


    //        var button = action.ButtonPush;
    //        this.MumiPlugin.Pulse(button.DeviceId, button.ButtonId, button.Duration);
            
    //        //this.MumiPlugin.Vjoy.Pulse(button.DeviceId, button.ButtonId, button.Duration);

    //        return true;
    //    }

    //    bool ProcessButtonEvent(string actionParameter)
    //    {
    //        // process the action
    //        PageData page = this.Config.GetPage(this.currentPage);
    //        if (page == null)
    //            return false;

    //        var controller = ControllerBase.FromActionParam(actionParameter);
    //        var action = page.GetAction(controller);
    //        if (action == null)
    //            return false;

    //        var button = action.ButtonPush;
    //        this.MumiPlugin.Pulse(button.DeviceId, button.ButtonId, button.Duration);

    //        //MumiLog.Info($"PULSE: {button.DeviceId} {button.ButtonId} {button.Duration}");
    //        //this.MumiPlugin.Vjoy.Pulse(button.DeviceId, button.ButtonId, button.Duration);

    //        return true;
    //    }

    //    public override Boolean ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
    //    {
    //        MumiLog.Info($"TOUCH: {actionParameter} type: {touchEvent.EventType}");
    //        if (touchEvent.EventType == DeviceTouchEventType.Tap)
    //        {
    //            return ProcessButtonEvent(actionParameter);

    //        }

    //        return false;
    //    }


    //    public override Boolean ProcessButtonEvent(string actionParameter, DeviceButtonEvent buttonEvent)
    //    {
    //        MumiLog.Info($"BUTTON: {actionParameter} pressed: {buttonEvent.IsPressed}  long press: {buttonEvent.IsLongPress}  duration: {buttonEvent.PressDuration}");
    //        if (buttonEvent.IsPressed)
    //        {
    //            return ProcessButtonEvent(actionParameter);
    //        }

    //        return false;
    //    }

    //}



}



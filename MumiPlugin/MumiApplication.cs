namespace Loupedeck.MumiPlugin
{
    using System;

    public class MumiApplication : ClientApplication
    {
        public MumiApplication()
        {

        }

        //protected override String GetProcessName() => "";

        //protected override String GetBundleName() => "";

        protected override Boolean IsProcessNameSupported(string processName)
        {
            return true;
        }
    }
}
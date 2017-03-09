using System.Collections.Generic;

namespace ChromeUpdater
{
    public class ChromeUpdate
    {
        public AppUpdateWithArch Stable;
        public AppUpdateWithArch Beta;
        public AppUpdateWithArch Dev;
        public AppUpdateWithArch Canary;

        public Dictionary<string, AppUpdateWithArch> ToDictionary()
        {
            var dc = new Dictionary<string, AppUpdateWithArch>();
            if (Stable != null)
                dc.Add("Stable", Stable);
            if (Beta != null)
                dc.Add("Beta", Beta);
            if (Dev != null)
                dc.Add("Dev", Dev);
            if (Canary != null)
                dc.Add("Canary", Canary);
            return dc;
        }
        public AppUpdate GetUpdate(string branch, bool isX64)
        {
            switch (branch)
            {
                case "Stable":
                    return isX64 ? Stable.x64 : Stable.x86;
                case "Beta":
                    return isX64 ? Beta.x64 : Beta.x86;
                case "Dev":
                    return isX64 ? Dev.x64 : Dev.x86;
                case "Canary":
                    return isX64 ? Canary.x64 : Canary.x86;
            }
            return null;
        }
        public AppUpdate GetUpdate(Branch branch, bool isX64)
        {
            switch (branch)
            {
                case Branch.Stable:
                    return isX64 ? Stable.x64 : Stable.x86;
                case Branch.Beta:
                    return isX64 ? Beta.x64 : Beta.x86;
                case Branch.Dev:
                    return isX64 ? Dev.x64 : Dev.x86;
                case Branch.Canary:
                    return isX64 ? Canary.x64 : Canary.x86;
            }
            return null;
        }
    }


    
    public class AppUpdateWithArch
    {
        public AppUpdate x64;
        public AppUpdate x86;
    }

    public class AppUpdate
    {
        public string[] url;
        public long size;
        public decimal time;
        public string version;
        public string name;
        public string cdn;
        public string sha1;
        public string sha256;
    }
}

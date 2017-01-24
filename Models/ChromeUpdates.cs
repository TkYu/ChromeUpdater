using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    [Serializable]
    public class ChromeUpdateRss
    {
        public CPUnit Stable;
        public CPUnit Beta;
        public CPUnit Dev;
        public CPUnit Canary;

        public Dictionary<string, CPUnit> GetDictionary()
        {
            var dc = new Dictionary<string, CPUnit>();
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

        public CUnit GetCU(string branch, bool isX64)
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
    }

    [Serializable]
    public class CPUnit
    {
        public CUnit x64;
        public CUnit x86;
    }

    [Serializable]
    public class CUnit
    {
        public string[] url;
        public long size;
        public decimal time;
        public string version;
        public string name;
        public string cdn;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeUpdater
{
    public class ChromeInfo
    {
        public ChromeInfo(string version,bool isx64,Branch? branch = null)
        {
            Version = version;
            IsX64 = isx64;
            Branch = branch;
        }
        public string Version { get; }
        public bool IsX64 { get; }
        public Branch? Branch { get; }

        public override string ToString()
        {
            return $"{Version}/{(IsX64 ? "64" : "32")}位/{Branch?.ToString() ?? "分支未知"}";
        }
    }
}

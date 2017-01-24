using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    [Serializable]
    public class GCUpdateRss
    {
        public float verison;
        public string version;
        public string description;
        public GCPUnit link;
    }
    [Serializable]
    public class GCPUnit
    {
        public GCUnit x86;
        public GCUnit x64;
    }
    [Serializable]
    public class GCUnit
    {
        public string url;
        public string sha1;
    }
}

namespace ChromeUpdater
{
    public class GreenChromeUpdate
    {
        public float verison { get; set; }//WTF???
        public string version { get; set; }
        public string description { get; set; }
        public GreenChromeWithArch link { get; set; }
    }

    public class GreenChromeWithArch
    {
        public GreenChrome x86 { get; set; }
        public GreenChrome x64 { get; set; }
    }

    public class GreenChrome
    {
        public string url { get; set; }
        public string sha1 { get; set; }

        public string GetFileName()
        {
            var split = url.Split('/');
            var last = split[split.Length - 1];
            return last.Split('?')[0];
        }
    }
}

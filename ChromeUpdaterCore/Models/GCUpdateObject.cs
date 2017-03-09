namespace ChromeUpdater
{
    public class GreenChromeUpdate
    {
        public float verison;//WTF???
        public string version;
        public string description;
        public GreenChromeWithArch link;
    }

    public class GreenChromeWithArch
    {
        public GreenChrome x86;
        public GreenChrome x64;
    }

    public class GreenChrome
    {
        public string url;
        public string sha1;

        public string GetFileName()
        {
            var split = url.Split('/');
            var last = split[split.Length - 1];
            return last.Split('?')[0];
        }
    }
}

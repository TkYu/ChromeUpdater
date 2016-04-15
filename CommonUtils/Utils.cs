using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Models;

namespace CommonUtils
{
    public static class Utils
    {
        #region Network
        public static async Task<bool> Ping(string url, int timeout = 1000)
        {
            using (var ping = new System.Net.NetworkInformation.Ping())
            {
                for (int times = 0; times < 3; times++)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(url, timeout);
                        if (reply?.Status == System.Net.NetworkInformation.IPStatus.Success)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        //TODO
                    }
                }
            }
            return false;
        }
        public static async Task<bool> CanUseGoogle(int timeout = 1000)
        {
            try
            {
                var hc = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) };
                var x = await hc.GetAsync("http://www.google.com/images/branding/product/ico/googleg_lodp.ico");
                return x.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region UpdateCheck
        internal static readonly Regex regxExePath = new Regex(@"\w:\\.*\.exe", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string GetClassesRootVal(string Path, string Name = null)
        {
            return Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(Path, false)?.GetValue(Name)?.ToString();
        }
        public static string GetExePath(string Path, string Name = null)
        {
            var val = GetClassesRootVal(Path,Name);
            return string.IsNullOrEmpty(val) ? null : regxExePath.IsMatch(val) ? regxExePath.Match(val).Value : null;
        }
        public static string GetFileVersion(string Path)
        {
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(Path).FileVersion;
        }
        public static string TryGetCurrChromeExePath()
        {
            var cd = Environment.CurrentDirectory + "\\chrome.exe";
            if (File.Exists(cd)) return cd;
            cd = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\chrome.exe";
            if (File.Exists(cd)) return cd;
            cd = GetExePath("http\\shell\\open\\command");
            if (!string.IsNullOrEmpty(cd) && cd.IndexOf("chrome.exe", StringComparison.OrdinalIgnoreCase) >= 0) return cd;
            cd = GetExePath("ChromeHTML\\shell\\open\\command");
            if (!string.IsNullOrEmpty(cd) && cd.IndexOf("chrome.exe", StringComparison.OrdinalIgnoreCase) >= 0) return cd;
            return null;
        }
        public static bool IsBiggerVersion(string v1, string v2)
        {
            var splv1 = v1.Split('.');
            var splv2 = v2.Split('.');
            var lt = Math.Min(splv1.Length, splv2.Length);
            for (var i = 0; i < lt; i++)
                if (int.Parse(splv2[i]) > int.Parse(splv1[i])) return true;
            return false;
        }


        public static async Task<CUnit> GetUpdateFromGoogle(string Branch, string Arch, int timeout = 8000)
        {
            CUnit cu = null;
            var hc = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) };
            const string url = "https://tools.google.com/service/update2";
            string appid, ap, ap64;
            switch (Branch)
            {
                case "Stable":
                    appid = "4DC8B4CA-1BDA-483E-B5FA-D3C12E15B62D";
                    ap = "-multi-chrome";
                    ap64 = "x64-stable-multi-chrome";
                    break;
                case "Beta":
                    appid = "4DC8B4CA-1BDA-483E-B5FA-D3C12E15B62D";
                    ap = "1.1-beta";
                    ap64 = "x64-beta-multi-chrome";
                    break;
                case "Dev":
                    appid = "4DC8B4CA-1BDA-483E-B5FA-D3C12E15B62D";
                    ap = "2.0-dev";
                    ap64 = "x64-dev-multi-chrome";
                    break;
                case "Canary":
                    appid = "4EA16AC7-FD5A-47C3-875B-DBF4A2008C20";
                    ap = "";
                    ap64 = "x64-canary";
                    break;
                default:
                    appid = "";
                    ap = "";
                    ap64 = "";
                    break;
            }
            HttpContent postData = new StringContent(Arch == "X64" ? @"<?xml version=""1.0"" encoding=""UTF-8""?><request protocol=""3.0"" version=""1.3.23.9"" shell_version=""1.3.21.103"" ismachine=""0"" sessionid=""{3597644B-2952-4F92-AE55-D315F45F80A5}"" installsource=""ondemandcheckforupdate"" requestid=""{CD7523AD-A40D-49F4-AEEF-8C114B804658}"" dedup=""cr""><os platform=""win"" version=""6.1"" sp=""Service Pack 1"" arch=""x64""/><app appid=""{" + appid + @"}"" version="""" nextversion="""" ap=""" + ap64 + @""" lang="""" brand=""GGLS"" client=""""><updatecheck/></app></request>" : @"<?xml version=""1.0"" encoding=""UTF-8""?><request protocol=""3.0"" version=""1.3.23.9"" shell_version=""1.3.21.103"" ismachine=""0"" sessionid=""{3597644B-2952-4F92-AE55-D315F45F80A5}"" installsource=""ondemandcheckforupdate"" requestid=""{CD7523AD-A40D-49F4-AEEF-8C114B804658}"" dedup=""cr""><os platform=""win"" version=""6.1"" sp=""Service Pack 1"" arch=""x86""/><app appid=""{" + appid + @"}"" version="""" nextversion="""" ap=""" + ap + @""" lang="""" brand=""GGLS"" client=""""><updatecheck/></app></request>");
            try
            {
                // ReSharper disable PossibleNullReferenceException
                var result = await hc.PostAsync(url, postData);
                result.EnsureSuccessStatusCode();
                var doc = new XmlDocument();
                doc.LoadXml(await result.Content.ReadAsStringAsync());
                var response = doc.ChildNodes[1];
                var app = response.ChildNodes[1];
                var updatecheck = app.ChildNodes[0];
                var urls = updatecheck.ChildNodes[0];
                var manifest = updatecheck.ChildNodes[1];
                var version = manifest.Attributes["version"].Value;
                var package = manifest.ChildNodes[0].ChildNodes[0];
                var size = package.Attributes["size"].Value;
                var name = package.Attributes["name"].Value;

                cu = new CUnit
                {
                    url = (from XmlNode u in urls.ChildNodes select u.Attributes["codebase"].Value + name).ToArray(),
                    size = long.Parse(size),
                    name = name,
                    version = version
                };
                // ReSharper restore PossibleNullReferenceException
            }
            catch
            {
                //TODO
            }
            return cu;
        }

        public static async Task<ChromeUpdateRss> GetUpdateFromShuax(int timeout = 8000)
        {
            var hc = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) };
            var str = await hc.GetStringAsync($"https://api.shuax.com/tools/getchrome/json?g={Guid.NewGuid().ToString("N")}");
            return SimpleJson.SimpleJson.DeserializeObject<ChromeUpdateRss>(str);
        }
        public static async Task<GCUpdateRss> GetGCVersion(int timeout = 5000)
        {
            var hc = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) };
            var str = await hc.GetStringAsync($"https://api.shuax.com/static/update/GreenChrome.json?g={Guid.NewGuid().ToString("N")}");
            return SimpleJson.SimpleJson.DeserializeObject<GCUpdateRss>(str);
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
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
        /// <summary>
        /// if val=C:\hello\hello.exe and exeName=bye.exe
        /// then i will check(C:\hello\bye.exe) is exists
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exeName"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static string GetSameLevelAndCheck(string path,string exeName, string Name = null)
        {
            var val = GetExePath(path, Name);
            if (string.IsNullOrEmpty(val)) return null;
            var sp = Path.GetDirectoryName(val) + "\\" + exeName;
            return File.Exists(sp) ? sp : null;
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

        #region Hash
        public static string MD5_Hash(string str_md5_in,bool remove = true)
        {
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bytes_md5_in = Encoding.Default.GetBytes(str_md5_in);
            byte[] bytes_md5_out = md5.ComputeHash(bytes_md5_in);
            string str_md5_out = BitConverter.ToString(bytes_md5_out);
            if(remove) str_md5_out = str_md5_out.Replace("-", "");
            return str_md5_out;
        }

        public static string SHA1_Hash(string str_sha1_in, bool remove = true)
        {
            var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            byte[] bytes_sha1_in = Encoding.Default.GetBytes(str_sha1_in);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            string str_sha1_out = BitConverter.ToString(bytes_sha1_out);
            if (remove) str_sha1_out = str_sha1_out.Replace("-", "");
            return str_sha1_out;
        }

        public static string SHA1_HashFile(string str_sha1_in, bool remove = true)
        {
            var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            byte[] bytes_sha1_in = File.ReadAllBytes(str_sha1_in);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            string str_sha1_out = BitConverter.ToString(bytes_sha1_out);
            if (remove) str_sha1_out = str_sha1_out.Replace("-", "");
            return str_sha1_out;
        }
        #endregion

        #region Other
        public static ushort GetImageArchitecture(string filepath)
        {
            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                //check the MZ signature to ensure it's a valid Portable Executable image
                if (reader.ReadUInt16() != 23117)
                    throw new BadImageFormatException("Not a valid Portable Executable image", filepath);

                // seek to, and read, e_lfanew then advance the stream to there (start of NT header)
                stream.Seek(0x3A, SeekOrigin.Current);
                stream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);

                // Ensure the NT header is valid by checking the "PE\0\0" signature
                if (reader.ReadUInt32() != 17744)
                    throw new BadImageFormatException("Not a valid Portable Executable image", filepath);

                // seek past the file header, then read the magic number from the optional header
                stream.Seek(20, SeekOrigin.Current);
                return reader.ReadUInt16();
            }
        }
        //public const ushort PE32 = 0x10b;
        internal const ushort PE32P = 0x20b;
        public static bool IsX64Image(string filepath)
        {

            return GetImageArchitecture(filepath) == PE32P;
        }

        public static bool IsAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            finally
            {
                user?.Dispose();
            }
            return isAdmin;
        }

        private const string c7z = "x \"{0}\" -o\"{1}\" -aoa -y";
        private const string crar = "x -y \"{0}\" \"{1}\"";

        public static void Extract(string file, string destinationPath, bool silence = true)
        {
            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
            var extractor = new System.Diagnostics.ProcessStartInfo(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\7za.exe",string.Format(c7z,file,destinationPath));
            bool isRAR = false;
            if (!File.Exists(extractor.FileName))
            {
                var z7 = GetSameLevelAndCheck("7-Zip.7z\\shell\\open\\command",silence? "7z.exe" : "7zG.exe");
                if (z7 != null) extractor.FileName = z7;
            }
            if (!File.Exists(extractor.FileName))
            {
                var rar = GetSameLevelAndCheck("WinRAR\\shell\\open\\command", "winrar.exe");
                if (rar != null)
                {
                    isRAR = true;
                    extractor.FileName = rar;
                    extractor.Arguments = string.Format(crar, file, destinationPath);
                }
            }
            if (!File.Exists(extractor.FileName)) using (var wc = new WebClient()) wc.DownloadFile($"http://static.pzhacm.org/exe{(Win32Api.Is64BitOperatingSystem ? "/x64" : "")}/7za.exe", extractor.FileName);
            if (!File.Exists(extractor.FileName)) throw new Exception("extractor disappeared!");
            if (isRAR)
            {
                if (silence)
                {
                    using (var desktop = Onyeyiri.Desktop.CreateDesktop("chromeextract"))
                    {
                        var p = desktop.CreateProcess(extractor.FileName + " " + extractor.Arguments);
                        p?.WaitForExit();
                        desktop.Close();
                    }
                }
                else
                {
                    var proc = System.Diagnostics.Process.Start(extractor);
                    if (proc != null)
                    {
                        proc.WaitForHandel();
                        proc.FuckCancel();
                        proc.WaitForExit();
                    }
                }
            }
            else
            {
                if (silence)
                {
                    extractor.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    extractor.CreateNoWindow = true;
                    extractor.UseShellExecute = false;
                    extractor.RedirectStandardError = true;
                    System.Diagnostics.Process.Start(extractor)?.WaitForExit();
                }
                else
                {
                    var proc = System.Diagnostics.Process.Start(extractor);
                    if (proc != null)
                    {
                        proc.WaitForHandel();
                        proc.FuckCancel();

                        var sb = new StringBuilder(20);
                        do
                        {
                            if (proc.HasExited)
                                break;
                            Win32Api.SendMessage(proc.MainWindowHandle, 0x000D, (IntPtr)sb.Capacity, sb);
                            System.Threading.Thread.Sleep(100);
                        } while (sb.Length <= 0 || !(sb[0] == '1' & sb[1] == '0' & sb[2] == '0'));
                        if (!proc.HasExited) proc.Kill();
                    }
                }
            }
        }

        public static async Task DownloadFileAsync(string url,string fileName, IProgress<double> progress, System.Threading.CancellationToken token)
        {
            if(File.Exists(fileName))File.Delete(fileName);
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"The request returned with HTTP status code {response.StatusCode}");
                }
                var total = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = total != -1 && progress != null;
                using (var sFile = new FileStream(fileName, FileMode.CreateNew))
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var totalRead = 0L;
                    var buffer = new byte[4096];
                    var isMoreToRead = true;
                    do
                    {
                        token.ThrowIfCancellationRequested();
                        var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            var data = new byte[read];
                            buffer.ToList().CopyTo(0, data, 0, read);
                            await sFile.WriteAsync(data, 0, data.Length, token);
                            totalRead += read;
                            if (canReportProgress)
                                progress.Report(totalRead * 1d / (total * 1d) * 100);
                        }
                    } while (isMoreToRead);
                }
            }
        }
        #endregion
    }
}

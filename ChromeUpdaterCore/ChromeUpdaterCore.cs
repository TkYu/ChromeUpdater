using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using ChromeUpdater.Services;

namespace ChromeUpdater
{
    public sealed class ChromeUpdaterCore : INotifyPropertyChanged
    {
        #region MVVM

        #region OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Private properties and ctor
        private readonly Progress<int> _downloadProgress = new Progress<int>();
        private ChromeUpdate chromeUpdate;
        public ChromeUpdaterCore()
        {
            _downloadProgress.ProgressChanged += (s, value) =>
            {
                DownloadPercent = value;
            };
            LoadConfig();
        }
        #endregion

        #region Properties
        
        private AppUpdate _updateInfo;
        public AppUpdate UpdateInfo
        {
            get { return _updateInfo; }
            set { _updateInfo = value; OnPropertyChanged(); }
        }

        private GreenChromeUpdate _GCUpdateInfo;
        public GreenChromeUpdate GCUpdateInfo
        {
            get { return _GCUpdateInfo; }
            set { _GCUpdateInfo = value; OnPropertyChanged(); }
        }

        

        private ChromeInfo _currentChromeInfo;
        public ChromeInfo CurrentChromeInfo
        {
            get { return _currentChromeInfo; }
            set { _currentChromeInfo = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged(); DownloadPercent = -1; }
        }

        private string _title = "ChromeUpdater";
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        private string _selectedPath;
        public string SelectedPath
        {
            get { return _selectedPath; }
            set
            {
                if (value != _selectedPath && Writeable && Directory.Exists(value))
                    ConfigIni.Write("LastPath", GetRelativePath(value), "Updater");
                _selectedPath = value;
                OnPropertyChanged();
                LoadChromeInfo();
            }
        }

        private Branch _branchSelected;
        public Branch BranchSelected
        {
            get { return _branchSelected; }
            set { _branchSelected = value; OnPropertyChanged(); SelectChanged(); }
        }

        private bool _isX64Selected;
        public bool IsX64Selected
        {
            get { return _isX64Selected; }
            set { _isX64Selected = value; OnPropertyChanged(); SelectChanged(); }
        }

        private bool _keepOldversion;
        public bool KeepOldversion
        {
            get { return _keepOldversion; }
            set
            {
                if (value != _keepOldversion && Writeable) ConfigIni.Write("KeepLastVersion", value ? "1" : "0", "Updater");
                _keepOldversion = value;
                OnPropertyChanged();
            }
        }

        private bool _keepInstaller;
        public bool KeepInstaller
        {
            get { return _keepInstaller; }
            set
            {
                if (value != _keepInstaller && Writeable) ConfigIni.Write("KeepInstaller", value ? "1" : "0", "Updater");
                _keepInstaller = value;
                OnPropertyChanged();
            }
        }

        private int _downloadPercent;
        public int DownloadPercent
        {
            get { return _downloadPercent; }
            set { _downloadPercent = value; OnPropertyChanged(); }
        }

        private IMessageService _messageService;
        public IMessageService MessageService => _messageService ?? (_messageService = ServiceManager.Instance.IsServiceExists<IMessageService>() ? ServiceManager.Instance.GetService<IMessageService>() : null);
        #endregion

        #region Methods
        private void LoadConfig()
        {
            if (File.Exists(ConfigIni.Path))
            {
                KeepOldversion = ConfigIni.Read("KeepLastVersion", "Updater") == "1";
                KeepInstaller = ConfigIni.Read("KeepInstaller", "Updater") == "1";
                var lastpath = ConfigIni.Read("LastPath", "Updater");
                if (!string.IsNullOrEmpty(lastpath))
                {
                    var bk = Path.GetFullPath(lastpath);
                    if (Directory.Exists(bk))
                        SelectedPath = bk;
                    return;
                }
            }
            if (TryGetCurrChromeExePath(out string path))
            {
                SelectedPath = Path.GetDirectoryName(path);
            }
        }

        private void SelectChanged()
        {
            if (chromeUpdate != null)
                UpdateInfo = chromeUpdate.GetUpdate(BranchSelected, IsX64Selected);
        }

        private void SaveBranch()
        {
            if (BranchSelected == CurrentChromeInfo?.Branch) return;
            if (!string.IsNullOrEmpty(SelectedPath) && HasWriteAccess(SelectedPath))
            {
                File.WriteAllText($"{SelectedPath}\\branch", BranchSelected.ToString());
            }
            else if (File.Exists($"{SelectedPath}\\GreenChrome.ini"))
            {
                var ini = new Ini($"{SelectedPath}\\GreenChrome.ini");
                ini.Write("检查版本", BranchSelected.ToString(), "检查更新");
            }
        }

        private void LoadChromeInfo()
        {
            if (SelectedPath == null)
            {
                Title = "ChromeUpdater";
                return;
            }
            if (Directory.Exists(SelectedPath))
            {
                var chromeExePath = Path.Combine(SelectedPath, "chrome.exe");
                if (File.Exists(chromeExePath))
                {
                    var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(chromeExePath);
                    var gcPath = Path.Combine(SelectedPath, "GreenChrome.ini");
                    Branch? bch = null;
                    var bfPath = Path.Combine(SelectedPath, "branch");
                    if (File.Exists(bfPath))
                    {
                        if (Enum.TryParse(File.ReadAllText(bfPath), out Branch b))
                            bch = b;
                    }
                    else if (File.Exists(gcPath))
                    {
                        var ini = new Ini(gcPath);
                        if (Enum.TryParse(ini.Read("检查版本", "检查更新"), out Branch b))
                            bch = b;
                    }
                    var ix = IsX64Image(chromeExePath);
                    CurrentChromeInfo = new ChromeInfo(version.FileVersion, ix, bch);
                    IsX64Selected = ix;
                }
                else
                {
                    CurrentChromeInfo = null;
                }
                Title = CurrentChromeInfo == null ? "ChromeUpdater - 当前目录没有找到chrome.exe" : $"ChromeUpdater - 当前版本:{CurrentChromeInfo}";
            }
            else
            {
                Title = "ChromeUpdater - 请选择一个有效的文件夹";
            }
        }

        private async Task ReportException(Exception ex)
        {
            if (MessageService != null)
            {
                if (await MessageService.ShowAsync($"遇到错误：{ex.Message}，请问是否要复制出错详情？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    Clipboard.SetText(ex.StackTrace);
            }
        }

        private Task MsgBox(string text, string title = null)
        {
            return MessageService == null ? Task.Delay(5) : MessageService.ShowAsync(text, title);
        }
        #endregion

        #region Commands
        private ICommand _cmdCheckUpdate;
        public ICommand CmdCheckUpdate => _cmdCheckUpdate ?? (_cmdCheckUpdate = new AsyncCommand(async () =>
        {
            if (IsBusy) return;
            IsBusy = true;
            chromeUpdate = await GetUpdateFromShuax();
            UpdateInfo = chromeUpdate?.GetUpdate(BranchSelected, IsX64Selected);
            IsBusy = false;
        }));

        private ICommand _cmdFolderBrowse;
        public ICommand CmdFolderBrowse => _cmdFolderBrowse ?? (_cmdFolderBrowse = new AsyncCommand(async () =>
        {
            if (IsBusy) return;
            var fbd = new WPFFolderBrowser.WPFFolderBrowserDialog();
            if (fbd.ShowDialog() ?? false)
                SelectedPath = fbd.FileName;
            await Task.Delay(1);
        }));

        private ICommand _cmdDownload;
        public ICommand CmdDownload => _cmdDownload ?? (_cmdDownload = new AsyncCommand(async () =>
        {
            if (IsBusy) return;
            if (UpdateInfo == null)
            {
                await MsgBox("请先查询！");
                return;
            }
            IsBusy = true;
            try
            {
                var file = Path.Combine(DownloadPath, UpdateInfo.name);
                if (!File.Exists(file))
                    await DownloadFile(UpdateInfo.url[0], UpdateInfo.name, UpdateInfo.sha1, _downloadProgress);
                if (File.Exists(file))
                    System.Diagnostics.Process.Start("Explorer.exe", $"/select,\"{file}\"");
            }
            catch (Exception ex)
            {
                await ReportException(ex);
            }
            finally 
            {
                IsBusy = false;
            }
        }));

        private ICommand _cmdDownloadAndExtract;
        public ICommand CmdDownloadAndExtract => _cmdDownloadAndExtract ?? (_cmdDownloadAndExtract = new AsyncCommand(async () =>
        {
            if (IsBusy) return;
            if (UpdateInfo == null)
            {
                await MsgBox("请先查询！");
                return;
            }
            IsBusy = true;
            try
            {
                var file = Path.Combine(DownloadPath, UpdateInfo.name);
                if (!File.Exists(file))
                    await DownloadFile(UpdateInfo.url[0], UpdateInfo.name, UpdateInfo.sha1, _downloadProgress);
                DownloadPercent = -1;
                bool canExtract = false;
                if (Directory.GetFiles(SelectedPath).Length > 0)
                {
                    if (CurrentChromeInfo != null)
                    {
                        canExtract = IsX64Selected != CurrentChromeInfo.IsX64 || BranchSelected != CurrentChromeInfo.Branch || IsBiggerVersion(CurrentChromeInfo.Version, UpdateInfo.version);
                    }
                    else
                    {
                        await MsgBox("请注意，您选择的文件夹不为空并且里面没有找到chrome，请重新选择一个文件夹！");
                        return;
                    }
                }
                else
                {
                    canExtract = true;
                }
                if (!canExtract)
                {
                    await MsgBox("更新包的版本和您本地的版本一致，不需要再次覆盖！", "提示");
                    return;
                }
                if (File.Exists(file))
                {
                    var c7z = Path.Combine(SelectedPath, "chrome.7z");
                    await Task.Run(() =>
                    {
                        ExtractFile(file, SelectedPath);
                        ExtractFile(c7z, SelectedPath);
                    });
                    if (File.Exists(c7z)) File.Delete(c7z);
                    await Task.Run(() =>
                    {
                        var chromeExePath = Path.Combine(SelectedPath, "chrome.exe");
                        var oldChrome = Path.Combine(SelectedPath, "old_chrome.exe");
                        if (KeepOldversion)
                        {
                            if (File.Exists(oldChrome))
                            {
                                //删除老版本的老版本
                                var oldversion = Path.Combine(SelectedPath, System.Diagnostics.FileVersionInfo.GetVersionInfo(oldChrome).FileVersion);
                                if (Directory.Exists(oldversion))
                                    Win32Api.IO.DeleteFile(oldversion);
                                File.Delete(oldversion);

                            }
                            if (File.Exists(chromeExePath)) File.Move(chromeExePath, oldChrome);
                        }
                        else
                        {
                            //不留活口
                            if (File.Exists(chromeExePath))
                            {
                                var cv = Path.Combine(SelectedPath, System.Diagnostics.FileVersionInfo.GetVersionInfo(chromeExePath).FileVersion);
                                if (Directory.Exists(cv))
                                    Win32Api.IO.DeleteFile(cv);
                            }
                        }
                        Win32Api.IO.MoveUp(Path.Combine(SelectedPath, "Chrome-bin"));
                        if (!KeepInstaller)
                        {
                            File.Delete(file);
                        }
                    });
                }
                SaveBranch();
                LoadChromeInfo();
                UpdateInfo = chromeUpdate?.GetUpdate(BranchSelected, IsX64Selected);
            }
            catch (Exception ex)
            {
                await ReportException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }));

        private ICommand _cmdCheckGCUpdate;
        public ICommand CmdCheckGCUpdate => _cmdCheckGCUpdate ?? (_cmdCheckGCUpdate = new AsyncCommand(async () =>
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                GCUpdateInfo = await GetGCVersion();
            }
            catch (Exception ex)
            {
                await ReportException(ex);
            }
            finally
            {
                IsBusy = false;
            }
            
        }));

        private ICommand _cmdDownloadGC;
        public ICommand CmdDownloadGC => _cmdDownloadGC ?? (_cmdDownloadGC = new AsyncCommand(async () =>
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (GCUpdateInfo != null)
                {
                    var arch = IsX64Image(Path.Combine(SelectedPath,"chrome.exe"));
                    var downloadUrl = arch ? GCUpdateInfo.link.x64.url : GCUpdateInfo.link.x86.url;
                    var sha1 = arch ? GCUpdateInfo.link.x64.sha1 : GCUpdateInfo.link.x86.sha1;
                    var gcName = arch ? GCUpdateInfo.link.x64.GetFileName() : GCUpdateInfo.link.x86.GetFileName();
                    await DownloadFile(downloadUrl, gcName, sha1, _downloadProgress);
                    var gcPath = Path.Combine(DownloadPath, gcName);
                    if (File.Exists(gcPath))
                    {
                        File.Move(gcPath, Path.Combine(SelectedPath, gcName));
                        File.WriteAllText(Path.Combine(SelectedPath, "GreenChrome.ini"), Win32Api.LoadRCString(Path.Combine(SelectedPath, gcName), "CONFIG", "INI"), Encoding.Unicode);
                    }
                }
            }
            catch (Exception ex)
            {
                await ReportException(ex);
            }
            finally 
            {
                IsBusy = false;
            }
        }));

        private ICommand _cmdCopyToClipboard;
        public ICommand CmdCopyToClipboard => _cmdCopyToClipboard ?? (_cmdCopyToClipboard = new AsyncCommand<string>(async str =>
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                {
                    if (UpdateInfo != null)
                        Clipboard.SetText(UpdateInfo.url[0]);
                }
                else
                {
                    Clipboard.SetText(str);
                }
                if (MessageService != null)
                    await MessageService.ShowAsync("复制成功", "提示");
            }
            catch (Exception ex)
            {
                await ReportException(ex);
            }
        }));
        #endregion

        #endregion

        #region Utils(static)

        public static string DownloadPath { get; }
        public static string CD { get; }
        public static Ini ConfigIni { get; }
        public static bool Writeable { get; }

        #region Static ctor
        static ChromeUpdaterCore()
        {
            var fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            CD = Path.GetDirectoryName(fileName);
            if (string.IsNullOrEmpty(CD)) throw new NullReferenceException("wrong path");
            DownloadPath = Path.Combine(CD, "installer");
            Writeable = HasWriteAccess(CD);
            ConfigIni = new Ini(Path.Combine(CD, $"{Path.GetFileNameWithoutExtension(fileName)}.ini"));
        }
        #endregion

        #region Update
        public static async Task<AppUpdate> GetUpdateFromGoogle(Branch branch, bool isX64, int timeout = 8000)
        {
            AppUpdate cu;
            var hc = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) };
            const string url = "https://tools.google.com/service/update2";
            string appid, ap, ap64;
            switch (branch)
            {
                case Branch.Stable:
                    //appid = "4DC8B4CA-1BDA-483E-B5FA-D3C12E15B62D";
                    appid = "8A69D345-D564-463C-AFF1-A69D9E530F96";
                    ap = "-multi-chrome";
                    ap64 = "x64-stable-multi-chrome";
                    break;
                case Branch.Beta:
                    //appid = "4DC8B4CA-1BDA-483E-B5FA-D3C12E15B62D";
                    appid = "8A69D345-D564-463C-AFF1-A69D9E530F96";
                    ap = "1.1-beta";
                    ap64 = "x64-beta-multi-chrome";
                    break;
                case Branch.Dev:
                    //appid = "4DC8B4CA-1BDA-483E-B5FA-D3C12E15B62D";
                    appid = "8A69D345-D564-463C-AFF1-A69D9E530F96";
                    ap = "2.0-dev";
                    ap64 = "x64-dev-multi-chrome";
                    break;
                case Branch.Canary:
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
            HttpContent postData = new StringContent(isX64 ? @"<?xml version=""1.0"" encoding=""UTF-8""?><request protocol=""3.0"" version=""1.3.23.9"" shell_version=""1.3.21.103"" ismachine=""0"" sessionid=""{3597644B-2952-4F92-AE55-D315F45F80A5}"" installsource=""ondemandcheckforupdate"" requestid=""{CD7523AD-A40D-49F4-AEEF-8C114B804658}"" dedup=""cr""><os platform=""win"" version=""6.1"" sp=""Service Pack 1"" arch=""x64""/><app appid=""{" + appid + @"}"" version="""" nextversion="""" ap=""" + ap64 + @""" lang="""" brand=""GGLS"" client=""""><updatecheck/></app></request>" : @"<?xml version=""1.0"" encoding=""UTF-8""?><request protocol=""3.0"" version=""1.3.23.9"" shell_version=""1.3.21.103"" ismachine=""0"" sessionid=""{3597644B-2952-4F92-AE55-D315F45F80A5}"" installsource=""ondemandcheckforupdate"" requestid=""{CD7523AD-A40D-49F4-AEEF-8C114B804658}"" dedup=""cr""><os platform=""win"" version=""6.1"" sp=""Service Pack 1"" arch=""x86""/><app appid=""{" + appid + @"}"" version="""" nextversion="""" ap=""" + ap + @""" lang="""" brand=""GGLS"" client=""""><updatecheck/></app></request>");
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
                //var action = manifest.ChildNodes[0].ChildNodes[0];
                var package = manifest.ChildNodes[1].ChildNodes[0];
                var size = package.Attributes["size"].Value;
                var name = package.Attributes["name"].Value;
                var hash_sha256 = package.Attributes["hash_sha256"].Value;
                var hash = package.Attributes["hash"].Value;
                cu = new AppUpdate
                {
                    url = (from XmlNode u in urls.ChildNodes select u.Attributes["codebase"].Value + name).ToArray(),
                    size = long.Parse(size),
                    name = name,
                    version = version,
                    sha256 = hash_sha256?.ToUpper()
                };
                if (!string.IsNullOrEmpty(hash))
                    cu.sha1 = BitConverter.ToString(Convert.FromBase64String(hash)).Replace("-", "");
                // ReSharper restore PossibleNullReferenceException
            }
            catch
            {
                throw new Exception("获取失败，可能是连接超时或APPID变更导致");
            }
            return cu;
        }

        public static async Task<ChromeUpdate> GetUpdateFromShuax(int timeout = 8000)
        {
            //try mirror first
            var hc = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            try
            {
                return SimpleJson.SimpleJson.DeserializeObject<ChromeUpdate>(await hc.GetStringAsync("http://api.pzhacm.org/iivb/cu.json"));
            }
            catch
            {
                //oops
            }
            hc.Timeout = TimeSpan.FromMilliseconds(timeout);
            var str = await hc.GetStringAsync($"https://api.shuax.com/tools/getchrome/json?g={Guid.NewGuid():N}");
            return SimpleJson.SimpleJson.DeserializeObject<ChromeUpdate>(str);
        }

        public static async Task<GreenChromeUpdate> GetGCVersion(int timeout = 5000)
        {
            var hc = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeout) };
            var str = await hc.GetStringAsync($"https://api.shuax.com/static/update/GreenChrome.json?g={Guid.NewGuid():N}");
            return SimpleJson.SimpleJson.DeserializeObject<GreenChromeUpdate>(str);
        }

        public static async Task DownloadFile(string url, string fileName, string sha1 = null, IProgress<int> progress = null)
        {
            if (!Directory.Exists(DownloadPath))
                Directory.CreateDirectory(DownloadPath);
            try
            {
                var temlFiles = new List<string>();
                temlFiles.AddRange(Directory.GetFiles(DownloadPath, "*.td.cfg"));
                temlFiles.AddRange(Directory.GetFiles(DownloadPath, "*.td"));
                temlFiles.ForEach(File.Delete);
            }
            catch
            {
                //TODO
            }
            var tempFileName = Guid.NewGuid().ToString("N") + ".td";
            var tempFileNamePath = DownloadPath + "\\" + tempFileName;
            if (File.Exists(CD + "\\xldl.dll") & Directory.Exists(CD + "\\download") & !Win32Api.Is64BitProcess)
            {//可以用迅雷
                if (!Xunlei.XL_Init())
                {
                    Xunlei.XL_UnInit();
                    throw new Exception("迅雷引擎初始化失败");
                }
                var param = new Xunlei.DownTaskParam { szTaskUrl = url, szFilename = tempFileName, szSavePath = DownloadPath };
                try
                {
                    var handel = Xunlei.XL_CreateTask(param);
                    Xunlei.XL_StartTask(handel);
                    while (true)
                    {
                        await Task.Delay(100);
                        var info = new Xunlei.DownTaskInfo();
                        Xunlei.XL_QueryTaskInfoEx(handel, info);
                        progress?.Report((int)Math.Min(100, info.fPercent * 100));
                        if (info.stat == Xunlei.DownTaskStatus.TscComplete || info.stat == Xunlei.DownTaskStatus.TscError)
                            break;
                    }
                }
                catch (Exception)
                {
                    Xunlei.XL_DelTempFile(param);
                    throw;
                }
                finally
                {
                    progress?.Report(0);
                    Xunlei.XL_UnInit();
                }
            }
            else
            {
                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadProgressChanged += (ss, ee) =>
                    {
                        progress?.Report(ee.ProgressPercentage);
                    };
                    try
                    {
                        await wc.DownloadFileTaskAsync(url, tempFileNamePath);
                    }
                    catch (Exception)
                    {
                        if (File.Exists(tempFileNamePath))
                            File.Delete(tempFileNamePath);
                        throw;
                    }
                    finally
                    {
                        progress?.Report(0);
                    }
                }
            }
            if (File.Exists(tempFileNamePath))
            {
                if (!string.IsNullOrEmpty(sha1) && SHA1_HashFile(tempFileNamePath) != sha1)
                {
                    File.Delete(tempFileNamePath);
                    throw new Exception("下载失败(SHA1校验不正确)!");
                }
                File.Move(tempFileNamePath, Path.Combine(DownloadPath, fileName));
            }
        }
        #endregion

        #region Hash
        public static string MD5_Hash(string str_md5_in, bool remove = true)
        {
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bytes_md5_in = Encoding.Default.GetBytes(str_md5_in);
            byte[] bytes_md5_out = md5.ComputeHash(bytes_md5_in);
            string str_md5_out = BitConverter.ToString(bytes_md5_out);
            if (remove) str_md5_out = str_md5_out.Replace("-", "");
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

        #region Permission

        public static bool IsAdministrator()
        {
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
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

        public static bool HasWriteAccess(string FilePath)
        {
            try
            {
                FileSystemSecurity security;
                if (File.Exists(FilePath))
                {
                    security = File.GetAccessControl(FilePath);
                }
                else
                {
                    var d = Path.GetDirectoryName(FilePath);
                    if (d == null) return false;
                    security = Directory.GetAccessControl(d);
                }
                var rules = security.GetAccessRules(true, true, typeof(NTAccount));
                var curr = WindowsIdentity.GetCurrent();
                var currentuser = new WindowsPrincipal(curr);
                bool result = false;
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (0 == (rule.FileSystemRights &
                              (FileSystemRights.WriteData | FileSystemRights.Write)))
                    {
                        continue;
                    }

                    if (rule.IdentityReference.Value.StartsWith("S-1-"))
                    {
                        var sid = new SecurityIdentifier(rule.IdentityReference.Value);
                        if (!currentuser.IsInRole(sid))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!currentuser.IsInRole(rule.IdentityReference.Value))
                        {
                            continue;
                        }
                    }

                    if (rule.AccessControlType == AccessControlType.Deny)
                        return false;
                    if (rule.AccessControlType == AccessControlType.Allow)
                        result = true;
                }
                return result;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Others
        internal static readonly Regex regxExePath = new Regex(@"\w:\\.*\.exe", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetExePath(string Path, string Name = null)
        {
            var val = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(Path, false)?.GetValue(Name)?.ToString();
            return string.IsNullOrEmpty(val) ? null : regxExePath.IsMatch(val) ? regxExePath.Match(val).Value : null;
        }

        public static bool TryGetCurrChromeExePath(out string filename)
        {
            //当前工作目录
            filename = Environment.CurrentDirectory + "\\chrome.exe";
            if (File.Exists(filename)) return true;
            //当前目录
            filename = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\chrome.exe";
            if (File.Exists(filename)) return true;
            //自动检测
            string progid;
            using (var userChoiceKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"))
                progid = userChoiceKey?.GetValue("Progid")?.ToString();
            filename = GetExePath($"{progid ?? "ChromeHTML"}\\shell\\open\\command");
            if (!string.IsNullOrEmpty(filename) && filename.IndexOf("\\chrome.exe", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            //查看http文件
            filename = GetExePath("http\\shell\\open\\command");
            if (!string.IsNullOrEmpty(filename) && filename.IndexOf("\\chrome.exe", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            filename = null;
            return false;
        }

        public static bool IsX64Image(string filepath)
        {
            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadUInt16() != 23117)
                    throw new BadImageFormatException("Not a valid Portable Executable image", filepath);
                stream.Seek(0x3A, SeekOrigin.Current);
                stream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);
                if (reader.ReadUInt32() != 17744)
                    throw new BadImageFormatException("Not a valid Portable Executable image", filepath);
                stream.Seek(20, SeekOrigin.Current);
                return reader.ReadUInt16() == 0x20b;
            }
        }

        public static bool IsBiggerVersion(string v1, string v2)
        {
            var splv1 = v1.Split('.');
            var splv2 = v2.Split('.');
            var lt = Math.Min(splv1.Length, splv2.Length);
            for (var i = 0; i < lt; i++)
            {
                if (int.Parse(splv2[i]) > int.Parse(splv1[i]))
                    return true;
                if (int.Parse(splv2[i]) < int.Parse(splv1[i]))
                    return false;
            }
            return false;
        }

        public static string GetRelativePath(string filespec, string folder = null)
        {
            if (string.IsNullOrEmpty(folder)) folder = CD;
             Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string GetSameLevelAndCheck(string path, string exeName, string Name = null)
        {
            var val = GetExePath(path, Name);
            if (string.IsNullOrEmpty(val)) return null;
            var sp = Path.GetDirectoryName(val) + "\\" + exeName;
            return File.Exists(sp) ? sp : null;
        }

        public static void ExtractFile(string file, string destinationPath, bool silence = true)
        {
            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
            var extractor = new System.Diagnostics.ProcessStartInfo(Path.Combine(CD, "7za.exe"), $"x \"{file}\" -o\"{destinationPath}\" -aoa -y");
            bool isRAR = false;
            if (!File.Exists(extractor.FileName))
            {
                var z7 = GetSameLevelAndCheck("7-Zip.7z\\shell\\open\\command", silence ? "7z.exe" : "7zG.exe");
                if (z7 != null) extractor.FileName = z7;
            }
            if (!File.Exists(extractor.FileName))
            {
                var rar = GetSameLevelAndCheck("WinRAR\\shell\\open\\command", "winrar.exe");
                if (rar != null)
                {
                    isRAR = true;
                    extractor.FileName = rar;
                    extractor.Arguments = $"x -y \"{file}\" \"{destinationPath}\"";
                }
            }
            if (!File.Exists(extractor.FileName)) using (var wc = new System.Net.WebClient()) wc.DownloadFile($"http://static.pzhacm.org/exe{(Win32Api.Is64BitOperatingSystem ? "/x64" : "")}/7za.exe", extractor.FileName);
            if (!File.Exists(extractor.FileName)) throw new Exception("extractor disappeared!");
            if (isRAR)
            {
                if (silence)
                {
#if DEBUG
                    var proc = System.Diagnostics.Process.Start(extractor);
                    if (proc != null)
                    {
                        WaitForHandel(proc);
                        FuckCancel(proc);
                        proc.WaitForExit();
                    }
#else
                    using (var desktop = Onyeyiri.Desktop.CreateDesktop("chromeextract"))
                    {
                        var p = desktop.CreateProcess(extractor.FileName + " " + extractor.Arguments);
                        p?.WaitForExit();
                        desktop.Close();
                    }
#endif
                }
                else
                {
                    var proc = System.Diagnostics.Process.Start(extractor);
                    if (proc != null)
                    {
                        WaitForHandel(proc);
                        FuckCancel(proc);
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
                        WaitForHandel(proc);
                        FuckCancel(proc);

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

        public static void WaitForHandel(System.Diagnostics.Process p)
        {
            while (p.MainWindowHandle == IntPtr.Zero)
                System.Threading.Thread.Sleep(100);
        }

        public static void FuckCancel(System.Diagnostics.Process p)
        {
            Win32Api.DeleteMenu(Win32Api.GetSystemMenu(p.Handle, false), Win32Api.SC_CLOSE, Win32Api.MF_BYCOMMAND);
            var hcancel = Win32Api.FindWindowEx(p.MainWindowHandle, 0, null, "取消");
            if (hcancel == IntPtr.Zero)
                hcancel = Win32Api.FindWindowEx(p.MainWindowHandle, 0, null, "Cancel");
            if (hcancel != IntPtr.Zero) Win32Api.EnableWindow(hcancel, false);
        }
        #endregion

        #endregion
    }
}

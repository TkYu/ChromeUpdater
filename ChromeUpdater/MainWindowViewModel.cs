using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommonUtils;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace ChromeUpdater
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindowViewModel()
        {
            Init();
        }

        #region Methods
        private ProgressDialogController progressDialogController;

        private async Task BusyBox(string Message = null, string Title = null)
        {
            IsReady = false;
            progressDialogController = await Me.ShowProgressAsync(Title ?? "稍等片刻...", Message ?? "老子正在执行一些后台任务。");
            progressDialogController.SetIndeterminate();
        }

        private async Task FreeBox()
        {
            if (progressDialogController == null) return;
            await progressDialogController.CloseAsync();
            IsReady = true;
        }

        private async Task GoodBye(string Message = null,double delay = 6.0)
        {
            if(progressDialogController == null) return;
            var i = 0.0;
            while (i < delay)
            {
                var val = (i / 100.0) * 20.0;
                progressDialogController.SetProgress(val);
                progressDialogController.SetMessage(Message?? "没有检查到网络" + "， " + (delay - i) + "秒后退出...");
                if (progressDialogController.IsCanceled)
                    break;
                i += 1.0;
                await Task.Delay(1000);
            }
            Environment.Exit(0);
        }

        public static Task ExtractChrome(string szfile, string dir, bool silence = true)
        {
            return Task.Run(() => { Utils.Extract(szfile, dir, silence); Utils.Extract(dir + "\\chrome.7z", dir); });
        }

        private async void Init()
        {
            while (Me == null || !Me.IsActive)
                await Task.Delay(100);
            await BusyBox("正在检查网络");
            if(!await Utils.Ping("www.baidu.com")) await GoodBye();
            CanUseGoogle = await Utils.CanUseGoogle();
            var tp = Utils.TryGetCurrChromeExePath();
            if (!string.IsNullOrEmpty(tp))
            {
                ChromePath = Path.GetDirectoryName(tp);
                var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(tp);
                IsX64 = Utils.IsX64Image(tp);
                CurrVersion = $"{version.FileVersion}/{(IsX64 ? "64" : "32")}位";
            }
            await FreeBox();
        }
        #endregion

        #region Properties
        private static MetroWindow Me => (MetroWindow) Application.Current.MainWindow;

        private bool _isReady = true;
        public bool IsReady
        {
            get { return _isReady; }
            set{ _isReady = value; OnPropertyChanged(); }
        }

        private string _chromePath;
        public string ChromePath
        {
            get { return _chromePath; }
            set{ _chromePath = value; OnPropertyChanged(); }
        }

        private string _currVersion = "NaN";
        public string CurrVersion
        {
            get { return _currVersion; }
            set { _currVersion = value; OnPropertyChanged(); }
        }

        private bool _canUseGoogle;
        public bool CanUseGoogle
        {
            get { return _canUseGoogle; }
            set { _canUseGoogle = value; OnPropertyChanged(); }
        }

        private bool _isX64;
        public bool IsX64
        {
            get { return _isX64; }
            set { _isX64 = value; OnPropertyChanged(); }
        }

        private Models.CUnit _currItem;
        public Models.CUnit CurrItem
        {
            get { return _currItem; }
            set { _currItem = value; OnPropertyChanged(); }
        }

        private List<string> _currUrls;
        public List<string> CurrUrls
        {
            get { return _currUrls; }
            set { _currUrls = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands
        private ICommand _cmdFolderBrowse;
        public ICommand CmdFolderBrowse => _cmdFolderBrowse ?? (_cmdFolderBrowse = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = x =>
            {
                var fbd = new WPFFolderBrowser.WPFFolderBrowserDialog();
                if (fbd.ShowDialog(Application.Current.MainWindow) ?? false)
                    ChromePath = fbd.FileName;
            }
        });

        private ICommand _copyToClipboard;
        public ICommand CopyToClipboard => _copyToClipboard ?? (_copyToClipboard = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = x =>
            {
                try
                {
                    var u = x?.ToString() ?? string.Join(Environment.NewLine, CurrUrls);
                    Clipboard.SetText(u);
                }
                catch (Exception)
                {
                    //TODO
                }
            }
        });

        private ICommand _cmdDoQuery;
        public ICommand CmdDoQuery => _cmdDoQuery ?? (_cmdDoQuery = new AsyncCommand<string>(async _ =>
        {
            await BusyBox("正在查询");
            if (CanUseGoogle)
            {
                CurrItem = await Utils.GetUpdateFromGoogle(_, IsX64 ? "X64" : "X86");
            }
            else
            {
                var d = await Utils.GetUpdateFromShuax();
                if (d != null) CurrItem = d.GetCU(_, IsX64);
            }
            if (CurrItem != null) CurrUrls = new List<string>(CurrItem.url) { $"文件：{CurrItem.name}，版本{CurrItem.version}，大小：{CurrItem.size}字节（{CurrItem.size / 1024 / 1024}M）" };
            await FreeBox();
        }));

        private ICommand _download;
        public ICommand Download => _download ?? (_download = new AsyncCommand(async _ =>
        {
            progressDialogController = await Me.ShowProgressAsync("提示", "正在初始化...", false, new MetroDialogSettings { ColorScheme = Me.MetroDialogOptions.ColorScheme, NegativeButtonText = "取消" });
            
            var progress = new Progress<double>();

            var cancellationToken = new System.Threading.CancellationTokenSource();

            progress.ProgressChanged += (s, value) =>
            {
                if (progressDialogController.IsCanceled)
                    cancellationToken.Cancel();
                progressDialogController.SetProgress(value/100);
                progressDialogController.SetMessage("正在下载： " + (int)value + "%");
            };

            progressDialogController.SetCancelable(true);

            var dd = Environment.CurrentDirectory + "\\download";
            if (!Directory.Exists(dd)) Directory.CreateDirectory(dd);

            var FileName = dd + "\\" + CurrItem.name;
            
            try
            {
                await Utils.DownloadFileAsync(CurrUrls[0], FileName, progress, cancellationToken.Token);
            }
            catch (Exception)
            {
                if (File.Exists(FileName))
                    File.Delete(FileName);
            }

            await progressDialogController.CloseAsync();

            if (File.Exists(FileName))
                System.Diagnostics.Process.Start("Explorer.exe", "/select,\"" + FileName + "\"");
        }));

        private ICommand _extract;
        public ICommand Extract => _extract ?? (_extract = new AsyncCommand(async _ =>
        {
            if (!Directory.Exists(ChromePath))
            {
                try
                {
                    Directory.CreateDirectory(ChromePath);
                }
                catch (Exception)
                {
                    await Me.ShowMessageAsync("累猴", "请填个正确的地址先");
                    return;
                }
            }
            
            var fileName = Environment.CurrentDirectory + "\\download\\" + CurrItem.name;

            if (File.Exists(fileName))
            {
                await BusyBox("正在解压");
                try
                {
                    await ExtractChrome(fileName, ChromePath);
                    if (File.Exists(ChromePath + "\\chrome.7z")) File.Delete(ChromePath + "\\chrome.7z");
                }
                catch (Exception ex)
                {
                    await Me.ShowMessageAsync("哦豁", "出了个错：" + ex.Message);
                    return;
                }
                try
                {
                    if (File.Exists(ChromePath + "\\old_chrome.exe")) File.Delete(ChromePath + "\\old_chrome.exe");
                    if (File.Exists(ChromePath + "\\chrome.exe")) File.Move(ChromePath + "\\chrome.exe", ChromePath + "\\old_chrome.exe");
                    Win32Api.IO.MoveUp(ChromePath + "\\Chrome-bin");
                }
                catch (Exception ex)
                {
                    await Me.ShowMessageAsync("哦豁", "出了个错：" + ex.Message);
                }
                await FreeBox();
            }
            else
            {
                await Me.ShowMessageAsync("哦豁", "请先下载安装包");
            }
        }));
        #endregion
    }
}

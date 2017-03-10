using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace ChromeUpdater.Console
{
    class Program
    {
        private static ChromeUpdaterCore vm;
        [STAThread]
        static void Main(string[] args)
        {
            vm = new ChromeUpdaterCore();
            Title = vm.Title;
            vm.PropertyChanged += Vm_PropertyChanged;
            WriteLine("欢迎使用");
            string tmpPath = vm.SelectedPath;
            if (tmpPath != null)
            {
                WriteLine($"自动检测到路径：{tmpPath}，如果正确的话请直接按下回车，或者输入一个您自己的路径");
                tmpPath = ReadLine();
                if(string.IsNullOrEmpty(tmpPath))
                    tmpPath = vm.SelectedPath;
            }
            else
            {
                WriteLine("没有自动检测到chrome，请输入一个有效的文件夹作为路径");
                tmpPath = ReadLine();
            }
            while (!Directory.Exists(tmpPath))
            {
                WriteLine("路径不存在，请重新输入！");
                tmpPath = ReadLine();
            }
            vm.SelectedPath = tmpPath;
            WriteLine("路径设置为：" + tmpPath);
            vm.CmdCheckUpdate.Execute(null);
            WriteLine("正在检查更新...");
            while (vm.IsBusy)
            {
                System.Threading.Thread.Sleep(100);
            }
            var canExtract = false;
            if (vm.CurrentChromeInfo!=null)
            {
                if (vm.IsX64Selected != vm.CurrentChromeInfo.IsX64)
                {
                    WriteLine($"请注意，您当前选择的架构为{(vm.IsX64Selected ? "x64" : "x86")}，但是您本地的Chrome架构为{(vm.CurrentChromeInfo.IsX64 ? "x64" : "x86")}！");
                    canExtract = true;
                }
                if (vm.BranchSelected != vm.CurrentChromeInfo.Branch)
                {
                    WriteLine($"请注意，您当前选择的分支为{vm.BranchSelected}，但是您本地的Chrome分支为{(vm.CurrentChromeInfo.Branch == null ? "未知" : vm.CurrentChromeInfo.Branch.ToString())}！");
                    canExtract = true;
                }
                if (ChromeUpdaterCore.IsBiggerVersion(vm.CurrentChromeInfo.Version, vm.UpdateInfo.version))
                {
                    WriteLine($"请注意，当前查询到的版本({vm.UpdateInfo.version}/{(vm.IsX64Selected ? "x64" : "x86")}/{vm.BranchSelected})的版本号大于您现有的Chrome版本号({vm.CurrentChromeInfo.Version})！");
                    canExtract = true;
                }
                else
                {
                    WriteLine($"您当前的chrome版本：{vm.CurrentChromeInfo} 是最新的！");
                }
            }
            else
            {
                if (Directory.GetFiles(vm.SelectedPath).Length > 0)
                {
                    WriteLine("请注意，您选择的文件夹不为空并且里面没有找到chrome，请重新选择一个文件夹！");
                }
                else
                {
                    canExtract = true;
                }
            }
            if (ChromeUpdaterCore.Writeable && canExtract)
            {
                Write("请按下y进行更新");
                var read = ReadKey();
                if (read.Key == ConsoleKey.Y)
                {
                    vm.CmdDownloadAndExtract.Execute(null);
                    while (vm.IsBusy)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    WriteLine("任务完成！按下任意键复制链接到剪贴板或是直接点关闭退出");
                    ReadKey();
                    System.Windows.Forms.Clipboard.SetText(vm.UpdateInfo.url[0]);
                }
            }
            else
            {
                WriteLine("当前为最新版本，按下任意键复制链接到剪贴板或是直接点关闭退出");
                ReadKey();
                System.Windows.Forms.Clipboard.SetText(vm.UpdateInfo.url[0]);
            }
        }

        private static void Vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Title":
                    Title = vm.Title;
                    break;
                case "UpdateInfo":
                    WriteLine(vm.UpdateInfo);
                    WriteLine(string.Join("\n", vm.UpdateInfo.url));
                    break;
                case "DownloadPercent":
                    Title = vm.DownloadPercent == -1 ? "请稍候..." : $"下载进度：{vm.DownloadPercent}%";
                    break;
                default:
                    Debug.WriteLine("Value of property {0} was changed! New value is {1}", e.PropertyName, typeof(ChromeUpdaterCore).GetProperty(e.PropertyName).GetValue(vm, null));
                    break;
            }
        }
    }
}

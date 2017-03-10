using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

namespace ChromeUpdater.ArthasUI.Converters
{
    [ValueConversion(typeof(AppUpdateToFlowDocumentConverter), typeof(FlowDocument))]
    public class AppUpdateToFlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return FlowDocumentExt.Default;
            var result = (AppUpdate)value;

            #region dirty hack
            var vm = (ChromeUpdaterCore)Application.Current.MainWindow.DataContext;
            var doc = new FlowDocument();
            if (!string.IsNullOrEmpty(vm.SelectedPath))
            {
                var canWrite = ChromeUpdaterCore.HasWriteAccess(vm.SelectedPath);
                if (!canWrite)
                {
                    doc.AddLine($"提示：程序无权对目录：{vm.SelectedPath} 进行操作，故无法进行一键更新！", FlowDocumentExt.Red);
                }
                var chromeExePath = System.IO.Path.Combine(vm.SelectedPath, "chrome.exe");
                var canExtract = false;
                if (System.IO.File.Exists(chromeExePath))
                {
                    if (vm.IsX64Selected != vm.CurrentChromeInfo.IsX64)
                    {
                        doc.AddLine($"请注意，您当前选择的架构为{(vm.IsX64Selected ? "x64" : "x86")}，但是您本地的Chrome架构为{(vm.CurrentChromeInfo.IsX64 ? "x64" : "x86")}！", FlowDocumentExt.Yellow);
                        canExtract = true;
                    }
                    if (vm.BranchSelected != vm.CurrentChromeInfo.Branch)
                    {
                        doc.AddLine($"请注意，您当前选择的分支为{vm.BranchSelected}，但是您本地的Chrome分支为{(vm.CurrentChromeInfo.Branch == null ? "未知" : vm.CurrentChromeInfo.Branch.ToString())}！", FlowDocumentExt.Yellow);
                        canExtract = true;
                    }
                    if (ChromeUpdaterCore.IsBiggerVersion(vm.CurrentChromeInfo.Version, result.version))
                    {
                        doc.AddLine($"请注意，当前查询到的版本({result.version}/{(vm.IsX64Selected ? "x64" : "x86")}/{vm.BranchSelected})的版本号大于您现有的Chrome版本号({vm.CurrentChromeInfo.Version})！", FlowDocumentExt.Green);
                        canExtract = true;
                    }
                    else
                    {
                        doc.AddLine($"您当前的chrome版本：{vm.CurrentChromeInfo} 是最新的！", FlowDocumentExt.Blue);
                    }
                }
                else
                {
                    if (System.IO.Directory.GetFiles(vm.SelectedPath).Length > 0)
                    {
                        doc.AddLine("请注意，您选择的文件夹不为空并且里面没有找到chrome，请重新选择一个文件夹！", FlowDocumentExt.Yellow);
                    }
                    else
                    {
                        canExtract = true;
                    }
                }
                doc.AddLine($"\n查询到的信息({vm.BranchSelected}/{(vm.IsX64Selected ? "x64" : "x86")})：\n", FlowDocumentExt.Blue);
                doc.Add(result.ToString(), FlowDocumentExt.Blue);
                if (canWrite && canExtract)
                {
                    doc.Add("  下载安装包并解压:", FlowDocumentExt.Blue);
                    doc.AddImage(Arthas.Utility.Media.ResObj.GetImageSource(System.Reflection.Assembly.GetExecutingAssembly(), "Resources.icon-download-e.png"), () =>
                    {
                        vm.CmdDownloadAndExtract.Execute(null);
                    });
                }
            }
            if (ChromeUpdaterCore.Writeable)
            {
                doc.Add(" 下载安装包:", FlowDocumentExt.Blue);
                doc.AddImage(Arthas.Utility.Media.ResObj.GetImageSource(System.Reflection.Assembly.GetExecutingAssembly(), "Resources.icon-download.png"), () =>
                {
                    vm.CmdDownload.Execute(null);
                });
            }
            doc.AddLine("");
            foreach (var s in result.url)
            {
                doc.AddLine(s, null, () =>
                {
                    vm.CmdCopyToClipboard.Execute(s);
                });
            }
            return doc;

            #endregion
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Arthas.Controls.Metro;

namespace ChromeUpdater.ArthasUI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var menu = sender as MetroMenuItem;
            if (menu == null) return;
            var header = (string)menu.Header;
            if (header.StartsWith("耍下"))
                System.Diagnostics.Process.Start("https://github.com/shuax");
            else if (header.StartsWith("ONEO"))
                System.Diagnostics.Process.Start("https://github.com/1217950746");
            else if (header.StartsWith("手动"))
                System.Diagnostics.Process.Start("https://api.shuax.com/tools/getchrome");
            else if (header.StartsWith("Chromium"))
                System.Diagnostics.Process.Start("http://commondatastorage.googleapis.com/chromium-browser-continuous/index.html");
            else
                System.Diagnostics.Process.Start("https://github.com/TkYu");
        }

        private void TxtPath_OnButtonClick(object sender, EventArgs e)
        {
            ((ChromeUpdaterCore)DataContext).CmdFolderBrowse.Execute(null);
        }
    }
}

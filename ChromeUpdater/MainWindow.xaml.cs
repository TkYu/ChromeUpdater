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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace ChromeUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly MainWindowViewModel viewModel;
        public MainWindow()
        {
            viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            InitializeComponent();
        }

        private void gotoGithub(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/TkYu/ChromeUpdater");
            }
            catch (Exception)
            {
                //TODO
            }
        }

        private async void ShowAbout(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("呵呵", "呵呵呵呵呵, ㄏㄏ 。\r\nBy TsungKang\r\n3BDE89C4");
        }
    }
}

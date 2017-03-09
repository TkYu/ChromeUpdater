using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace ChromeUpdater.MahAppsUI
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        async void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var errorMessage = $"出错啦: {e.Exception.Message}，请问是否要复制出错详情？";
            if (Current.MainWindow == null)
            {
                if (MessageBox.Show(errorMessage, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    Clipboard.SetText(e.Exception.StackTrace);
            }
            else
            {
                var wnd = (MetroWindow) Current.MainWindow;
                if (await wnd.ShowMessageAsync("Error",errorMessage,MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                    Clipboard.SetText(e.Exception.StackTrace);
            }
            e.Handled = true;
        }
    }
}

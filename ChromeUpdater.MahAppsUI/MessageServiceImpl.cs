using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ChromeUpdater.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace ChromeUpdater.MahAppsUI
{
    public class MessageService : IMessageService
    {
        private static string TXT_OK { get; }
        private static string TXT_CANCEL { get; }
        private static string TXT_YES { get; }
        private static string TXT_NO { get; }
        static MessageService()
        {
            var user32 = IntPtr.Zero;
            try
            {
                user32 = Win32Api.LoadLibrary(System.IO.Path.Combine(Environment.SystemDirectory, "User32.dll"));
                var sb = new StringBuilder(256);
                Win32Api.LoadString(user32, 800, sb, sb.Capacity);
                TXT_OK = sb.ToString();
                Win32Api.LoadString(user32, 801, sb, sb.Capacity);
                TXT_CANCEL = sb.ToString();
                Win32Api.LoadString(user32, 805, sb, sb.Capacity);
                TXT_YES = sb.ToString();
                Win32Api.LoadString(user32, 806, sb, sb.Capacity);
                TXT_NO = sb.ToString();
            }
            finally
            {
                if (user32 != IntPtr.Zero)
                    Win32Api.FreeLibrary(user32);
            }
        }

        private MetroWindow wnd;
        public MessageService(MetroWindow mw)
        {
            wnd = mw;
        }


        public async Task<MessageBoxResult> ShowAsync(string message, string title = null, MessageBoxButton? buttons = null, MessageBoxImage? image = null)
        {
            var style = MessageDialogStyle.Affirmative;
            var settings = new MetroDialogSettings
            {
                AnimateShow = true,
                AnimateHide = true
            };
            if (buttons != null)
            {
                if (buttons == MessageBoxButton.OKCancel)
                {
                    style = MessageDialogStyle.AffirmativeAndNegative;
                    settings.AffirmativeButtonText = TXT_OK;
                    settings.NegativeButtonText = TXT_CANCEL;
                }
                else if (buttons == MessageBoxButton.YesNo)
                {
                    style = MessageDialogStyle.AffirmativeAndNegative;
                    settings.AffirmativeButtonText = TXT_YES;
                    settings.NegativeButtonText = TXT_NO;
                }
            }
            var result = await wnd.ShowMessageAsync(title ?? "info", message, style, settings);
            if (buttons != null)
            {
                if (result == MessageDialogResult.Affirmative && buttons == MessageBoxButton.OKCancel)
                    return MessageBoxResult.OK;
                if (result == MessageDialogResult.Affirmative && buttons == MessageBoxButton.YesNo)
                    return MessageBoxResult.Yes;
                if (result == MessageDialogResult.Negative && buttons == MessageBoxButton.OKCancel)
                    return MessageBoxResult.Cancel;
                if (result == MessageDialogResult.Negative && buttons == MessageBoxButton.YesNo)
                    return MessageBoxResult.No;
            }
            return result == MessageDialogResult.Affirmative ? MessageBoxResult.OK : MessageBoxResult.None;
        }
    }
}

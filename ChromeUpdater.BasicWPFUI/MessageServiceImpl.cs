using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ChromeUpdater.Services;

namespace ChromeUpdater.BasicWPFUI
{
    public class MessageService : IMessageService
    {
        private readonly Window wnd;
        public MessageService(Window mw)
        {
            wnd = mw;
        }
        public Task<MessageBoxResult> ShowAsync(string message, string title = null, MessageBoxButton? buttons = null, MessageBoxImage? image = null)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();
            wnd.Dispatcher.BeginInvoke(new Action(() =>
            {
                var result = MessageBox.Show(wnd, message, title ?? "info", buttons ?? MessageBoxButton.OK, image ?? MessageBoxImage.Information);
                tcs.SetResult(result);
            }));
            return tcs.Task;
        }
    }
}

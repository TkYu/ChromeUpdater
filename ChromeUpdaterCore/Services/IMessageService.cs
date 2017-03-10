using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChromeUpdater.Services
{
    public interface IMessageService
    {
        Task<MessageBoxResult> ShowAsync(string message, string title = null, MessageBoxButton? buttons = null, MessageBoxImage? image = null);
    }
}

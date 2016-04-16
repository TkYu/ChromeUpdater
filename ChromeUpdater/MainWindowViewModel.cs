using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
            IsReady = true;
        }

        #region Methods

        #endregion

        #region Properties
        private bool _isReady = true;
        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                _isReady = value;
                OnPropertyChanged();
                IndeterminateStatus = value ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private Visibility _indeterminateStatus = Visibility.Collapsed;
        public Visibility IndeterminateStatus
        {
            get { return _indeterminateStatus; }
            set
            {
                _indeterminateStatus = value;
                OnPropertyChanged();
            }
        }

        private string _chromePath;
        public string ChromePath
        {
            get { return _chromePath; }
            set
            {
                _chromePath = value;
                OnPropertyChanged();
            }
        }

        private string _branch = "Stable";
        public string Branch
        {
            get { return _branch; }
            set { _branch = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands
        private ICommand cmdFolderBrowse;
        public ICommand CmdFolderBrowse => cmdFolderBrowse ?? (cmdFolderBrowse = new SimpleCommand
        {
            CanExecuteDelegate = x => true,
            ExecuteDelegate = x =>
            {
                var fbd = new WPFFolderBrowser.WPFFolderBrowserDialog();
                if (fbd.ShowDialog(Application.Current.MainWindow) ?? false)
                    ChromePath = fbd.FileName;
            }
        });
        private ICommand cmdDoQuery;
        public ICommand CmdDoQuery => cmdDoQuery ?? (cmdDoQuery = new AsyncCommand(async _ =>
        {
            IsReady = false;
            await Task.Delay(3000);
            IsReady = true;
        }));
        #endregion
    }
}

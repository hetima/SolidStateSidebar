using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SSS.Windows;
using System.Windows.Media;

namespace SSS.Module.WindowMonitor
{
    public class WindowItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _title = "";

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    NotifyPropertyChanged(nameof(Title));
                }
            }
        }

        private string _processName = "";

        public string ProcessName
        {
            get => _processName;
            set
            {
                if (_processName != value)
                {
                    _processName = value;
                    NotifyPropertyChanged(nameof(ProcessName));
                }
            }
        }

        private ImageSource? _processIcon;

        public ImageSource? ProcessIcon
        {
            get => _processIcon;
            set
            {
                if (_processIcon != value)
                {
                    _processIcon = value;
                    NotifyPropertyChanged(nameof(ProcessIcon));
                }
            }
        }
        

        private IntPtr _hwnd;

        public IntPtr Hwnd
        {
            get => _hwnd;
            set
            {
                if (_hwnd != value)
                {
                    _hwnd = value;
                    NotifyPropertyChanged(nameof(Hwnd));
                }
            }
        }

        private bool _isMinimized;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;
                    NotifyPropertyChanged(nameof(IsMinimized));
                }
            }
        }

        private Visibility _visibility = Visibility.Collapsed;

        public Visibility Visibility
        {
            get => _visibility;
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    NotifyPropertyChanged(nameof(Visibility));
                }
            }
        }

        private ICommand? _activateCommand;

        public ICommand? ActivateCommand
        {
            get => _activateCommand;
            set
            {
                if (_activateCommand != value)
                {
                    _activateCommand = value;
                    NotifyPropertyChanged(nameof(ActivateCommand));
                }
            }
        }

        public WindowItem()
        {
            ActivateCommand = new RelayCommand(Activate);
        }

        private void Activate()
        {
            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            WindowHelper.ActivateWindow(_hwnd);
        }
    }

    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }
}

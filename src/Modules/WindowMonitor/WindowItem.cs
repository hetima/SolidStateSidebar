using System;
using System.Windows;
using System.Windows.Input;
using SSS.Windows;
using System.Windows.Media;
using SSS.Core;

namespace SSS.Module.WindowMonitor
{
    public class WindowItem : ObservableObject
    {
        private string _title = "";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _processName = "";

        public string ProcessName
        {
            get => _processName;
            set => SetProperty(ref _processName, value);
        }

        private ImageSource? _processIcon;

        public ImageSource? ProcessIcon
        {
            get => _processIcon;
            set => SetProperty(ref _processIcon, value);
        }


        private IntPtr _hwnd;

        public IntPtr Hwnd
        {
            get => _hwnd;
            set => SetProperty(ref _hwnd, value);
        }

        private bool _isMinimized;

        public bool IsMinimized
        {
            get => _isMinimized;
            set => SetProperty(ref _isMinimized, value);
        }

        private Visibility _visibility = Visibility.Collapsed;

        public Visibility Visibility
        {
            get => _visibility;
            set => SetProperty(ref _visibility, value);
        }

        private bool _isSwitchHighlighted;

        public bool IsSwitchHighlighted
        {
            get => _isSwitchHighlighted;
            set => SetProperty(ref _isSwitchHighlighted, value);
        }

        private ICommand? _activateCommand;

        public ICommand? ActivateCommand
        {
            get => _activateCommand;
            set => SetProperty(ref _activateCommand, value);
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

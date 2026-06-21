using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SSS.Features.EditShortcutKey
{
    public partial class EditShortcutKeyView : Window
    {
        public EditShortcutKeyViewModel ViewModel { get; }

        public EditShortcutKeyView(EditShortcutKeyViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = vm;

            vm.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += (_, _) => ShortcutInputTextBox.Focus();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.RequestClose) && ViewModel.RequestClose)
            {
                Close();
            }
        }

        private void ShortcutInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ViewModel.OnKeyDown(e);
        }
    }
}

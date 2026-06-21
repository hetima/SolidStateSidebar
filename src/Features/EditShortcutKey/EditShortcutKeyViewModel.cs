using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SSS.Core;
using SSS.Windows;

namespace SSS.Features.EditShortcutKey
{
    /// <summary>
    /// ショートカットキー編集ダイアログの ViewModel
    /// </summary>
    public class EditShortcutKeyViewModel : ObservableObject
    {
        private readonly IEnumerable<ShortcutKey?> _otherShortcuts;
        private readonly Action<ShortcutKey?> _onSaved;
        private readonly Action? _onConflictRemoved;
        private ShortcutKey _currentShortcut;
        private string _displayString = string.Empty;
        private string _warningMessage = string.Empty;
        private bool _shortcutConflicted;
        private bool _requestClose;

        /// <summary>
        /// アクション名（ダイアログタイトル表示用）
        /// </summary>
        public string ActionName { get; }

        /// <summary>
        /// 現在の入力を表示する文字列
        /// </summary>
        public string DisplayString
        {
            get => _displayString;
            private set => SetProperty(ref _displayString, value);
        }

        /// <summary>
        /// 警告メッセージ（重複・エラー時）
        /// </summary>
        public string WarningMessage
        {
            get => _warningMessage;
            private set => SetProperty(ref _warningMessage, value);
        }

        /// <summary>
        /// 他のホットキーと重複しているかどうか
        /// </summary>
        public bool ShortcutConflicted
        {
            get => _shortcutConflicted;
            private set => SetProperty(ref _shortcutConflicted, value);
        }

        /// <summary>
        /// true になったら Window を Close させる
        /// </summary>
        public bool RequestClose
        {
            get => _requestClose;
            private set => SetProperty(ref _requestClose, value);
        }

        public ICommand ClearCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RemoveConflictingShortcutCommand { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="actionName">アクション名（ダイアログタイトル）</param>
        /// <param name="currentShortcut">現在設定されているショートカット（null の場合は空として扱う）</param>
        /// <param name="otherShortcuts">重複チェック用の他のショートカット一覧</param>
        /// <param name="onSaved">保存時コールバック</param>
        /// <param name="onConflictRemoved">競合削除時コールバック</param>
        public EditShortcutKeyViewModel(
            string actionName,
            ShortcutKey? currentShortcut,
            IEnumerable<ShortcutKey?> otherShortcuts,
            Action<ShortcutKey?> onSaved,
            Action? onConflictRemoved = null)
        {
            ActionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
            _currentShortcut = currentShortcut ?? new ShortcutKey(Key.None);
            _otherShortcuts = otherShortcuts ?? Enumerable.Empty<ShortcutKey?>();
            _onSaved = onSaved ?? throw new ArgumentNullException(nameof(onSaved));
            _onConflictRemoved = onConflictRemoved;

            _displayString = _currentShortcut.IsEmpty ? Strings.EditShortcutKeyNone : _currentShortcut.ToDisplayString();

            ClearCommand = new RelayCommand(OnClear, CanClear);
            SaveCommand = new RelayCommand(OnSave, CanSave);
            CloseCommand = new RelayCommand(OnClose);
            RemoveConflictingShortcutCommand = new RelayCommand(OnRemoveConflictingShortcut, CanRemoveConflictingShortcut);
        }

        /// <summary>
        /// キーダウンイベントを処理する
        /// </summary>
        public void OnKeyDown(KeyEventArgs e)
        {
            // Esc キーはキャンセル
            if (e.Key == Key.Escape)
            {
                OnClose();
                e.Handled = true;
                return;
            }

            // Delete / Backspace はクリア
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                OnClear();
                e.Handled = true;
                return;
            }

            // 修飾キーのみは無視
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt  || e.Key == Key.RightAlt  ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LWin || e.Key == Key.RWin)
            {
                return;
            }

            _currentShortcut = new ShortcutKey(e);
            UpdateDisplay();
            CheckForConflicts();
            RaiseCommandsCanExecuteChanged();
            e.Handled = true;
        }

        private void UpdateDisplay()
        {
            DisplayString = _currentShortcut.IsEmpty ? Strings.EditShortcutKeyNone : _currentShortcut.ToDisplayString();
        }

        private void CheckForConflicts()
        {
            WarningMessage = string.Empty;
            ShortcutConflicted = false;

            if (_currentShortcut.IsEmpty) return;

            // 他のショートカットキーとの重複チェック
            var duplicate = _otherShortcuts.FirstOrDefault(k => k != null && k.Equals(_currentShortcut));
            if (duplicate != null)
            {
                WarningMessage = Strings.EditShortcutKeyDuplicate;
                ShortcutConflicted = true;
            }
        }

        private bool CanClear() => !_currentShortcut.IsEmpty;

        private void OnClear()
        {
            _currentShortcut = new ShortcutKey(Key.None);
            UpdateDisplay();
            WarningMessage = string.Empty;
            ShortcutConflicted = false;
            RaiseCommandsCanExecuteChanged();
        }

        private bool CanSave() => _currentShortcut.IsEmpty || string.IsNullOrEmpty(WarningMessage);

        private void OnSave()
        {
            _onSaved(_currentShortcut.IsEmpty ? new ShortcutKey(Key.None) : _currentShortcut);
            RequestClose = true;
        }

        private void OnClose()
        {
            RequestClose = true;
        }

        private bool CanRemoveConflictingShortcut() => ShortcutConflicted;

        private void OnRemoveConflictingShortcut()
        {
            _onConflictRemoved?.Invoke();
            CheckForConflicts();
            RaiseCommandsCanExecuteChanged();
        }

        private void RaiseCommandsCanExecuteChanged()
        {
            ((RelayCommand)ClearCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RemoveConflictingShortcutCommand).RaiseCanExecuteChanged();
        }
    }
}

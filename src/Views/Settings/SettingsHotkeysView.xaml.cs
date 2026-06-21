using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SSS.Features.EditShortcutKey;
using SSS.Models;
using SSS.Windows;

namespace SSS
{
    public partial class SettingsHotkeysView : UserControl
    {
        public SettingsHotkeysView()
        {
            InitializeComponent();
        }

        private void EditKey_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SettingsModel model) return;

            var tag = ((Button)sender).Tag?.ToString();
            if (!Enum.TryParse<Hotkey.KeyAction>(tag, out var action)) return;

            var currentKey = model.GetHotkeyByAction(action)?.Key;
            var otherKeys = model.GetAllHotkeys()
                                 .Where(h => h?.Action != action)
                                 .Select(h => h?.Key);
            var actionName = GetActionName(action);

            var vm = new EditShortcutKeyViewModel(
                actionName,
                currentKey,
                otherKeys,
                saved => model.SetHotkeyByAction(action, saved),
                () =>
                {
                    // 競合する他のホットキーを削除する
                    var conflict = model.GetAllHotkeys()
                                       .FirstOrDefault(h => h?.Action != action && h?.Key != null && h.Key.Equals(currentKey));
                    if (conflict != null)
                        model.SetHotkeyByAction(conflict.Action, null);
                });

            var dialog = new EditShortcutKeyView(vm) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
        }

        private static string GetActionName(Hotkey.KeyAction action)
        {
            return action switch
            {
                Hotkey.KeyAction.Toggle => Strings.SettingsHotkeyToggle,
                Hotkey.KeyAction.Show => Strings.SettingsHotkeyShow,
                Hotkey.KeyAction.Hide => Strings.SettingsHotkeyHide,
                Hotkey.KeyAction.Reload => Strings.SettingsHotkeyReload,
                Hotkey.KeyAction.Close => Strings.SettingsHotkeyClose,
                Hotkey.KeyAction.CycleEdge => Strings.SettingsHotkeyCycleEdge,
                Hotkey.KeyAction.CycleScreen => Strings.SettingsHotkeyCycleScreen,
                Hotkey.KeyAction.ReserveSpace => Strings.SettingsHotkeyReserveSpace,
                Hotkey.KeyAction.WindowCycle => Strings.SettingsHotkeyWindowCycle,
                _ => action.ToString()
            };
        }
    }
}

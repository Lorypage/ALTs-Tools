using RefreshToAccess2.Localization;
using RefreshToAccess2.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace RefreshToAccess2.Views.Inject
{
    public partial class MinecraftProcSelectorView : Window
    {
        // Parallel list that keeps the actual Process objects in sync with
        // the display strings shown in the combo box.
        private readonly List<Process> _processes = new();

        public MinecraftProcSelectorView()
        {
            InitializeComponent();
            LoadProcesses();
        }

        // ── Process loading ────────────────────────────────────────────

        private void LoadProcesses()
        {
            _processes.Clear();
            var items = new List<string>();

            try
            {
                foreach (Process p in Helper.GetJavaProcesses())
                {
                    // Only surface processes that have a visible window title –
                    // headless java.exe instances (e.g. servers) are skipped.
                    if (string.IsNullOrWhiteSpace(p.MainWindowTitle))
                        continue;

                    _processes.Add(p);
                    items.Add($"[{p.Id}]  {p.MainWindowTitle}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Loc.T("ProcSel.Msg.EnumFailed", ex.Message),
                    Loc.T("Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ProcComboBox.ItemsSource   = items;
            ProcComboBox.SelectedIndex = items.Count > 0 ? 0 : -1;
        }

        // ── Button handlers ────────────────────────────────────────────

        private void OnRefresh(object sender, RoutedEventArgs e)
            => LoadProcesses();

        private async void OnConfirm(object sender, RoutedEventArgs e)
        {
            int idx = ProcComboBox.SelectedIndex;
            if (idx < 0 || idx >= _processes.Count)
            {
                MessageBox.Show(
                    Loc.T("ProcSel.Msg.NothingSelected"),
                    Loc.T("ProcSel.Msg.NothingSelectedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Process target = _processes[idx];

            // If the DLL has already reported back for this PID we can go
            // straight to token selection; otherwise inject first.
            if (!TokenInjectionService.PidPortMap.ContainsKey(target.Id))
            {
                bool ok = TokenInjectionService.InjectDll(
                    target.Id, Helper.tmpFileName);

                if (!ok)
                {
                    MessageBox.Show(
                        Loc.T("ProcSel.Msg.InjectFailed"),
                        Loc.T("ProcSel.Msg.InjectFailedTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Give the DLL a moment to initialise and register its port.
                await System.Threading.Tasks.Task.Delay(800);

                if (!TokenInjectionService.PidPortMap.ContainsKey(target.Id))
                {
                    MessageBox.Show(
                        Loc.T("ProcSel.Msg.NotReady"),
                        Loc.T("ProcSel.Msg.NotReadyTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            // Open the token-selection dialog and hide this window.
            var tokenSelector = new InjectionTokenSelectorView(target.Id)
            {
                Owner = this
            };
            Hide();
            tokenSelector.ShowDialog();
            Close();
        }
    }
}

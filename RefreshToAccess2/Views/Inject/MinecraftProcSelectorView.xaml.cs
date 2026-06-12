using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Helpers;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views.Inject
{
    /// <summary>
    /// In-app Minecraft process selector, hosted inside the root DialogHost.
    /// </summary>
    public partial class MinecraftProcSelectorView : UserControl
    {
        private const string InnerHost = "ProcSelectorDialog";

        // Parallel list that keeps the actual Process objects in sync with
        // the display strings shown in the combo box.
        private readonly List<Process> _processes = new();

        // Optional access token supplied up front on the injector page. When
        // set it is forwarded to the token selector so the user can inject it
        // directly; null means the normal stored-token flow.
        private readonly string? _prefilledToken;

        public MinecraftProcSelectorView() : this(null) { }

        public MinecraftProcSelectorView(string? prefilledToken)
        {
            InitializeComponent();

            _prefilledToken = prefilledToken;

            Loaded += (_, _) => AppMessageBox.PushHost(InnerHost);
            Unloaded += (_, _) => AppMessageBox.PopHost(InnerHost);

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

            ProcComboBox.ItemsSource = items;
            ProcComboBox.SelectedIndex = items.Count > 0 ? 0 : -1;
        }

        // ── Button handlers ────────────────────────────────────────────

        private void OnRefresh(object sender, RoutedEventArgs e)
            => LoadProcesses();

        private void OnCancel(object sender, RoutedEventArgs e)
            => DialogHost.CloseDialogCommand.Execute(false, this);

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

            // Open the token-selection dialog nested on this dialog's own host,
            // then close ourselves once the user is done with it.
            var tokenSelector = new InjectionTokenSelectorView(target.Id, _prefilledToken);
            await DialogHost.Show(tokenSelector, InnerHost);

            DialogHost.CloseDialogCommand.Execute(true, this);
        }
    }
}

using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Crypto;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views.Dialogs
{
    /// <summary>
    /// In-app custom client-id editor, shown inside the root DialogHost.
    /// </summary>
    public partial class CustomClientIdDialog : UserControl
    {
        private const string InnerHost = "CustomClientDialog";
        private readonly TokenConverterViewModel _vm;

        /// <summary>True when the user confirmed a valid client identification.</summary>
        public bool Confirmed { get; private set; }

        public CustomClientIdDialog(TokenConverterViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            // Nested message boxes raised from this dialog target our inner host.
            Loaded += (_, _) => Helpers.AppMessageBox.PushHost(InnerHost);
            Unloaded += (_, _) => Helpers.AppMessageBox.PopHost(InnerHost);

            // Pre-fill from registry if the user has saved a custom ID before.
            string savedId = DataPacker.UnpackData(false, RegistryService.Read("customClientId"), 8964);
            string savedScope = DataPacker.UnpackData(false, RegistryService.Read("customClientScope"), 8964);

            ClientIdBox.Text = savedId;
            ScopeBox.Text = savedScope;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            string id = ClientIdBox.Text.Trim();
            string scope = ScopeBox.Text.Trim();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(scope))
            {
                MessageBox.Show(
                    Loc.T("CustomClient.Msg.Incomplete"),
                    Loc.T("CustomClient.Msg.IncompleteTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _vm.CustomClient = new ClientIdentification(id, scope);

            // Persist so the dialog is pre-filled next time.
            RegistryService.Write("customClientId", DataPacker.PackData(false, (id), 8964));
            RegistryService.Write("customClientScope", DataPacker.PackData(false, (scope), 8964));

            Confirmed = true;
            DialogHost.CloseDialogCommand.Execute(true, this);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            // Revert combo-box back to the previous valid selection so the VM
            // does not end up stuck on the custom index with an empty client.
            _vm.SelectedClientIndex = 0;
            Confirmed = false;
            DialogHost.CloseDialogCommand.Execute(false, this);
        }
    }
}

using RefreshToAccess2.Crypto;
using RefreshToAccess2.Localization;
using RefreshToAccess2.Models;
using RefreshToAccess2.Services;
using RefreshToAccess2.ViewModels;
using System.Windows;

namespace RefreshToAccess2.Views.Dialogs
{
    public partial class CustomClientIdDialog : Window
    {
        private readonly TokenConverterViewModel _vm;

        public CustomClientIdDialog(TokenConverterViewModel vm)
        {
            InitializeComponent();
            _vm = vm;

            // Pre-fill from registry if the user has saved a custom ID before.
            string savedId = DataPacker.UnpackData(false, RegistryService.Read("customClientId"), 8964);
            string savedScope = DataPacker.UnpackData(false, RegistryService.Read("customClientScope"), 8964);

            // Unpack stubs – replace with DataPacker calls if encrypted.
            ClientIdBox.Text = savedId;
            ScopeBox.Text    = savedScope;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            string id    = ClientIdBox.Text.Trim();
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
            RegistryService.Write("customClientId",    DataPacker.PackData(false,(id),8964));
            RegistryService.Write("customClientScope", DataPacker.PackData(false, (scope), 8964));

            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            // Revert combo-box back to the previous valid selection
            // so the VM does not end up stuck on index 10 with an empty client.
            _vm.SelectedClientIndex = 0;
            DialogResult = false;
        }

    }
}

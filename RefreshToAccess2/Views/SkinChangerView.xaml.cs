using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views
{
    public partial class SkinChangerView : System.Windows.Controls.UserControl
    {
        public SkinChangerView()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.SkinChangerViewModel vm)
                await vm.EnsureLoadedAsync();
        }

        private void ResetCameraButton_OnClick(object sender, RoutedEventArgs e)
        {
            SkinPreview.ResetCamera(true);
        }
    }
}

using MaterialDesignThemes.Wpf;
using RefreshToAccess2.Localization;
using System.Windows;
using System.Windows.Controls;

namespace RefreshToAccess2.Views.Dialogs
{
    /// <summary>
    /// In-app message dialog hosted inside the root <see cref="DialogHost"/>.
    /// Mirrors the look and behaviour of a <see cref="System.Windows.MessageBox"/>
    /// but renders within the application window instead of a separate OS window.
    /// </summary>
    public partial class MessageDialog : UserControl
    {
        /// <summary>The result chosen by the user; read after the dialog closes.</summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public MessageDialog(
            string message,
            string title,
            MessageBoxButton buttons,
            MessageBoxImage icon)
        {
            InitializeComponent();

            MessageText.Text = message ?? string.Empty;
            TitleText.Text = string.IsNullOrEmpty(title) ? DefaultTitle(icon) : title;

            ApplyIcon(icon);
            BuildButtons(buttons);
        }

        private static string DefaultTitle(MessageBoxImage icon) => icon switch
        {
            MessageBoxImage.Error => Loc.T("Common.Error"),
            MessageBoxImage.Warning => Loc.T("Common.Warning"),
            MessageBoxImage.Question => Loc.T("Common.Confirm.Title"),
            _ => Loc.T("Common.Information"),
        };

        private void ApplyIcon(MessageBoxImage icon)
        {
            // MessageBoxImage shares enum values (e.g. Information == Asterisk),
            // so map the distinct visible cases only.
            (PackIconKind kind, string brushKey) = icon switch
            {
                MessageBoxImage.Error =>
                    (PackIconKind.AlertCircle, "MaterialDesign.Brush.ValidationError"),
                MessageBoxImage.Warning =>
                    (PackIconKind.Alert, "MaterialDesign.Brush.ValidationError"),
                MessageBoxImage.Question =>
                    (PackIconKind.HelpCircle, "MaterialDesign.Brush.Primary"),
                MessageBoxImage.Information =>
                    (PackIconKind.InformationOutline, "MaterialDesign.Brush.Primary"),
                _ => (PackIconKind.InformationOutline, "MaterialDesign.Brush.Primary"),
            };

            HeaderIcon.Kind = kind;
            if (TryFindResource(brushKey) is System.Windows.Media.Brush brush)
                HeaderIcon.Foreground = brush;
        }

        private void BuildButtons(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton(Loc.T("Common.OK"), MessageBoxResult.OK, primary: true);
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton(Loc.T("Common.Cancel"), MessageBoxResult.Cancel, primary: false);
                    AddButton(Loc.T("Common.OK"), MessageBoxResult.OK, primary: true);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton(Loc.T("Common.No"), MessageBoxResult.No, primary: false);
                    AddButton(Loc.T("Common.Yes"), MessageBoxResult.Yes, primary: true);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton(Loc.T("Common.Cancel"), MessageBoxResult.Cancel, primary: false);
                    AddButton(Loc.T("Common.No"), MessageBoxResult.No, primary: false);
                    AddButton(Loc.T("Common.Yes"), MessageBoxResult.Yes, primary: true);
                    break;
            }
        }

        private void AddButton(string content, MessageBoxResult result, bool primary)
        {
            var button = new Button
            {
                Content = content,
                MinWidth = 88,
                Height = 38,
                Margin = new Thickness(8, 0, 0, 0),
                Style = primary
                    ? (Style)FindResource("MaterialDesignRaisedButton")
                    : (Style)FindResource("MaterialDesignOutlinedButton"),
            };

            button.Click += (_, _) =>
            {
                Result = result;
                DialogHost.CloseDialogCommand.Execute(result, button);
            };

            ButtonPanel.Children.Add(button);
        }
    }
}

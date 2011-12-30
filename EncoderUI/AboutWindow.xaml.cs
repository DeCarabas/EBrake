namespace EBrake
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void OnLinkClicked(object sender, RoutedEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }  

        void OnWindowTopMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
                DragMove();
        }
    }
}

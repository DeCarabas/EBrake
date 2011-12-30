namespace EncoderUI.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public partial class WindowButtons : UserControl
    {
        public WindowButtons()
        {
            InitializeComponent();
        }

        public event ClosingWindowEventHandler ClosingWindow;

        void CloseClick(object sender, RoutedEventArgs e)
        {
            var closingWindowEventHandlerArgs = new ClosingWindowEventArgs();
            OnClosingWindow(closingWindowEventHandlerArgs);

            if (closingWindowEventHandlerArgs.Cancelled) return;

            var parentWindow = GetParentWindow();
            if (parentWindow != null) { parentWindow.Close(); }
        }

        Window GetParentWindow()
        {
            var parent = VisualTreeHelper.GetParent(this);

            while (parent != null)
            {
                var parentWindow = parent as Window;
                if (parentWindow != null) { return parentWindow; }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        void MaximiseClick(object sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow != null)
            {
                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    parentWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    parentWindow.WindowState = WindowState.Maximized;
                }
            }
        }

        void MinimiseClick(object sender, RoutedEventArgs e)
        {
            var parentWindow = GetParentWindow();
            if (parentWindow != null)
            {
                parentWindow.WindowState = WindowState.Minimized;
            }
        }

        void OnClosingWindow(ClosingWindowEventArgs args)
        {
            var handler = ClosingWindow;
            if (handler != null) handler(this, args);
        }
    }
}

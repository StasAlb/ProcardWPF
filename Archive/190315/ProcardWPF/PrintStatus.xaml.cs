using System.Windows;
using System.Windows.Controls;

namespace ProcardWPF
{
    public enum StatusPrintMessage
    {
        Repeat,
        Skip,
        Cancel
    }
    /// <summary>
    /// Interaction logic for PrintStatus.xaml
    /// </summary>
    public partial class PrintStatus : Window
    {
        public delegate void PrintStatusMessage(StatusPrintMessage message);
        public event PrintStatusMessage statusMessage;
        public PrintStatus()
        {
            InitializeComponent();
        }
        public void SetMessage(string str)
        {
            lMessage.SetValue(TextBlock.TextProperty, str);
        }
        public void SetButtonsEnable(bool repeatEnable, bool skipEnable)
        {
            bRepeat.IsEnabled = repeatEnable;
            bSkip.IsEnabled = skipEnable;
        }
        private void bRepeat_Click(object sender, RoutedEventArgs e)
        {
            statusMessage?.Invoke(StatusPrintMessage.Repeat);
        }
        private void bSkip_Click(object sender, RoutedEventArgs e)
        {
            statusMessage?.Invoke(StatusPrintMessage.Skip);
        }
        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            statusMessage?.Invoke(StatusPrintMessage.Cancel);
        }
    }
}

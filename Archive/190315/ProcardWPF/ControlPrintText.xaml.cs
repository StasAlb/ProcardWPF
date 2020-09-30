using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProcardWPF
{
    /// <summary>
    /// Interaction logic for ControlPrintText.xaml
    /// </summary>
    public partial class ControlPrintText : UserControl
    {
        public ControlPrintText(int oId, PrintValuesTypes pvt)
        {
            InitializeComponent();
            objectId = oId;
            printValuesType = pvt;
            switch (pvt)
            {
                case PrintValuesTypes.Text:
                    tbText.Visibility = Visibility.Visible;
                    break;
            }
        }
        public void SetTitle(string title)
        {
            tbTitle.Text = title;
        }
        public void SetText(string text)
        {
            tbText.Text = text;
        }
        public string Text
        {
            get
            {
                return tbText.Text;
            }
        }
        public void Lock(bool locked)
        {
            tbText.IsEnabled = locked;
        }
        private int objectId;
        public int ObjectId
        {
            get
            {
                return objectId;
            }
        }
        private PrintValuesTypes printValuesType;
        public PrintValuesTypes PrintValuesType
        {
            get
            {
                return printValuesType;
            }
        }
        public event PrintTextChanged printTextChanged = null;
        private void tbText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (printTextChanged != null)
                printTextChanged(objectId, tbText.Text, misc);
        }
        private object misc;
        public object Misc
        {
            get { return misc; }
            set { misc = value; }
        }
    }
}

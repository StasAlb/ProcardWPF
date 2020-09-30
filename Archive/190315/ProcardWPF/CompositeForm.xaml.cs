using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Collections;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProcardWPF
{
    /// <summary>
    /// Interaction logic for CompositeForm.xaml
    /// </summary>
    public partial class CompositeForm : Window
    {
        public CompositeForm()
        {
            InitializeComponent();
            cbFunction.Items.Add(new Para((int)CompositeFunc.None, (string)this.FindResource("NotDefined")));
            cbFunction.Items.Add(new Para((int)CompositeFunc.AddChar, (string)this.FindResource("Composite_FAddChar")));
            cbFunction.Items.Add(new Para((int)CompositeFunc.SubStringPos, (string)this.FindResource("Composite_FSubstring")));
        }
        public void LoadFields(Card c, DesignObject current, int magtrack)
        {
            ListBoxItem lbi = null;
            //lbi = new ListBoxItem();
            //lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_FixText"));
            //lbi.Tag = new Composite();
            //lbPossible.Items.Insert(0, lbi);
            if (c == null)
                return;
            if (c.DbIn != null)
            {
                lbi = new ListBoxItem();
                lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_DBFields"));
                lbi.Tag = new Composite();
                lbPossible.Items.Add(lbi);
                foreach (string col in c.DbIn.Columns)
                {
                    lbi = new ListBoxItem();
                    lbi.SetValue(ListBoxItem.ContentProperty, col);
                    lbi.Tag = new Composite(CompositeType.DB, col);
                    lbPossible.Items.Add(lbi);
                }
            }
            bool was = false, wasfeedback = false;
            for(int i=0; i < c.objects.Count; i++)
            {
                if (c.objects[i].ID == current.ID)
                    continue;
                if (c.objects[i].OType == ObjectType.EmbossLine || c.objects[i].OType == ObjectType.ImageField)
                    continue;
                if ((c.objects[i].OType == ObjectType.MagStripe && ((MagStripe) c.objects[i]).Feedback)
                    || (c.objects[i].OType == ObjectType.SmartField && ((SmartField) c.objects[i]).Feedback))
                    wasfeedback = true;
                if (!was)
                {
                    lbi = new ListBoxItem();
                    lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_DesignFields"));
                    lbi.Tag = new Composite();
                    lbPossible.Items.Add(lbi);
                    was = true;
                }
                lbi = new ListBoxItem();
                lbi.SetValue(ListBoxItem.ContentProperty, c.objects[i].Name);
                lbi.Tag = new Composite(CompositeType.Design, c.objects[i].Name);
                lbPossible.Items.Add(lbi);
            }
            if (wasfeedback)
            {
                lbi = new ListBoxItem();
                lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_Feedback"));
                lbi.Tag = new Composite();
                lbPossible.Items.Add(lbi);
                for (int i = 0; i < c.objects.Count; i++)
                {
                    if (c.objects[i].OType == ObjectType.MagStripe && ((MagStripe) c.objects[i]).Feedback)
                    {
                        for (int t = 0; t < 3; t++)
                        {
                            lbi = new ListBoxItem();
                            lbi.SetValue(ListBoxItem.ContentProperty,
                                $"{(string)this.FindResource("MagStripeFeedback")} {t + 1}");
                            lbi.Tag = new Composite(CompositeType.Feedback, $"{t + 1}");
                            lbPossible.Items.Add(lbi);
                        }
                    }
                    if (c.objects[i].OType == ObjectType.SmartField && ((SmartField)c.objects[i]).Feedback)
                    {
                        lbi = new ListBoxItem();
                        lbi.SetValue(ListBoxItem.ContentProperty,
                            $"{(string)this.FindResource("SmartField_Feedback")}");
                        lbi.Tag = new Composite(CompositeType.Feedback, "4");
                        lbPossible.Items.Add(lbi);
                    }
                }
            }
            lbi = new ListBoxItem();
            lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_Misc"));
            lbi.Tag = new Composite();
            lbPossible.Items.Add(lbi);

            lbi = new ListBoxItem();
            lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_DateTime"));
            lbi.Tag = new Composite(CompositeType.DateTime, lbi.Content.ToString());
            lbPossible.Items.Add(lbi);
            lbi = new ListBoxItem();
            lbi.SetValue(ListBoxItem.ContentProperty, (string)this.FindResource("Composite_Login"));
            lbi.Tag = new Composite(CompositeType.Login, lbi.Content.ToString());
            lbPossible.Items.Add(lbi);
            //=============================
            if (current == null || (current.OType != ObjectType.MagStripe && current.InData == null) || (current.OType == ObjectType.MagStripe && ((MagStripe)current).InDataM[magtrack-1] == null))
                return;
            CompositeArray ca = null;
            try
            {
                string indata = (magtrack > 0) ? ((MagStripe)current).InDataM[magtrack-1] : current.InData;
                XmlSerializer ser = new XmlSerializer(typeof(CompositeArray));
                StringReader sr = new StringReader(indata);
                ca = (CompositeArray)ser.Deserialize(sr);
                sr.Close();
            }
            catch (Exception ex)
            {
                Params.WriteLogString(ex.ToString());
                ca = null;
            }
            if (ca == null)
                return;
            for(int i=0;i<ca.Count;i++)
            {
                lbi = new ListBoxItem();
                lbi.SetValue(ListBoxItem.ContentProperty, ca[i].ToString());
                lbi.Tag = ca[i];
                lbCurrent.Items.Add(lbi);
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (lbPossible.ActualWidth > 0)
            {
                tbFixedText.MinWidth = lbPossible.ActualWidth-20;
                tbFixedText.MaxWidth = lbPossible.ActualWidth-20;
                tbFixedText.Width = lbPossible.ActualWidth - 20;
            }
        }

        private void bAdd_Click(object sender, RoutedEventArgs e)
        {
            Control c = (Control)lbPossible.SelectedItem;
            if (c == null)
                return;
            if (c.GetType().ToString() == "System.Windows.Controls.TextBox")
            {
                ListBoxItem lbi_n = new ListBoxItem();
                lbi_n.SetValue(ListBoxItem.ContentProperty, tbFixedText.Text);
                lbi_n.Tag = new Composite(CompositeType.Fixed, tbFixedText.Text);
                lbCurrent.Items.Add(lbi_n);
                return;
            }
            foreach(ListBoxItem lbi in lbPossible.SelectedItems)
            {
                if (((Composite)lbi.Tag).Type == CompositeType.None)
                    continue;
                ListBoxItem lbi_n = new ListBoxItem();
                lbi_n.SetValue(ListBoxItem.ContentProperty, lbi.Content.ToString());
                lbi_n.Tag = lbi.Tag;
                lbCurrent.Items.Add(lbi_n);
            }
        }

        private void bRemove_Click(object sender, RoutedEventArgs e)
        {
            for(int i=0;i<lbCurrent.Items.Count;i++)
            {
                if (((ListBoxItem)lbCurrent.Items[i]).IsSelected)
                {
                    lbCurrent.Items.RemoveAt(i);
                }
            }
        }

        private void bRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            lbCurrent.Items.Clear();
        }

        private void cbFunction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            spPar1.Visibility = Visibility.Hidden;
            spPar2.Visibility = Visibility.Hidden;
            if (lbCurrent.SelectedItem == null)
                return;
            Composite c = (Composite)((ListBoxItem)lbCurrent.SelectedValue).Tag;
            if (c == null)
                return;
            switch ((CompositeFunc)((Para)cbFunction.SelectedValue).ID)
            {
                case CompositeFunc.None:
                    c.Function = CompositeFunc.None;
                    c.Parameters.Clear();
                    break;
                case CompositeFunc.AddChar:
                    lPar1.SetResourceReference(Label.ContentProperty, "Composite_FAddChar1");
                    lPar2.SetResourceReference(Label.ContentProperty, "Composite_FAddChar2");
                    spPar1.Visibility = Visibility.Visible;
                    spPar2.Visibility = Visibility.Visible;
                    while (c.Parameters.Count < 2)
                        c.Parameters.Add("");
                    tbPar1.Text = (string)c.Parameters[0];
                    tbPar2.Text = (string)c.Parameters[1];
                    c.Function = CompositeFunc.AddChar;
                    break;
                case CompositeFunc.SubStringPos:
                    lPar1.SetResourceReference(Label.ContentProperty, "Composite_FSubstring1");
                    lPar2.SetResourceReference(Label.ContentProperty, "Composite_FSubstring2");
                    spPar1.Visibility = Visibility.Visible;
                    spPar2.Visibility = Visibility.Visible;
                    while (c.Parameters.Count < 2)
                        c.Parameters.Add("");
                    tbPar1.Text = (string)c.Parameters[0];
                    tbPar2.Text = (string)c.Parameters[1];
                    c.Function = CompositeFunc.SubStringPos;
                    break;
            }
            ((ListBoxItem)lbCurrent.SelectedItem).SetCurrentValue(ListBoxItem.ContentProperty, c.ToString());
        }

        private void lbCurrent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //cbFunction.SelectedIndex = 0;
            if (lbCurrent.SelectedItem == null)
                return;

            Composite c = (Composite)((ListBoxItem)lbCurrent.SelectedValue).Tag;
            if (c == null || c.Function == CompositeFunc.None)
            {
                cbFunction.SelectedIndex = 0;
                return;
            }
            for (int i=0;i<cbFunction.Items.Count;i++)
                if (((Para)cbFunction.Items[i]).ID == (int)c.Function)
                {
                    cbFunction.SelectedIndex = i;
                    break;
                }
            if (c.Parameters.Count > 0)
                tbPar1.Text = (string)c.Parameters[0];
            if (c.Parameters.Count > 1)
                tbPar2.Text = (string)c.Parameters[1];
        }

        private void tbPar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lbCurrent.SelectedItem == null)
                return;
            Composite c = (Composite)((ListBoxItem)lbCurrent.SelectedValue).Tag;
            if (c == null)
                return;
            int index = Convert.ToInt32(((TextBox)sender).Tag);
            if (c.Parameters.Count <= index)
                return;
            c.Parameters[index] = ((TextBox)sender).Text;
            ((ListBoxItem)lbCurrent.SelectedItem).SetCurrentValue(ListBoxItem.ContentProperty, c.ToString());
        }
        private void tbFixedText_GotFocus(object sender, RoutedEventArgs e)
        {
            lbPossible.SelectedIndex = 0;
        }
        private void bFieldDown_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = (ListBoxItem)lbCurrent.SelectedItem;
            if (lbi == null)
                return;
            int index = lbCurrent.SelectedIndex;
            if (index + 1 >= lbCurrent.Items.Count)
                return;
            lbCurrent.Items.RemoveAt(index);
            lbCurrent.Items.Insert(index + 1, lbi);
            lbCurrent.SelectedIndex = index + 1;
            lbCurrent.Focus();
        }

        private void bFieldUp_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = (ListBoxItem)lbCurrent.SelectedItem;
            if (lbi == null)
                return;
            int index = lbCurrent.SelectedIndex;
            if (index - 1 < 0)
                return;
            lbCurrent.Items.RemoveAt(index);
            lbCurrent.Items.Insert(index-1, lbi);
            lbCurrent.SelectedIndex = index - 1;
            lbCurrent.Focus();
        }
        public string GetCurrent()
        {
            string res = "";
            CompositeArray ca = new CompositeArray();
            for (int i = 0; i < lbCurrent.Items.Count; i++)
                ca.collection.Add((Composite)((ListBoxItem)lbCurrent.Items[i]).Tag);
            XmlSerializer ser = new XmlSerializer(typeof(CompositeArray));
            using (var ms = new MemoryStream())
            {
                using (var xw = XmlWriter.Create(ms,
                    new XmlWriterSettings()
                    { Encoding = new UTF8Encoding(false), Indent = false, NewLineOnAttributes = false, OmitXmlDeclaration=true, NamespaceHandling = NamespaceHandling.OmitDuplicates }))
                {
                    ser.Serialize(xw, ca);
                    res = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return res;
        }
        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

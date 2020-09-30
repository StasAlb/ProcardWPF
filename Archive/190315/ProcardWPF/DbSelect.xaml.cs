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
using System.Windows.Shapes;
using System.Data;
using System.ComponentModel;
using Microsoft.Win32;

namespace ProcardWPF
{
    /// <summary>
    /// Interaction logic for DbSelect.xaml
    /// </summary>
    public partial class DbSelect : Window
    {
        Database db = null;
        public DbSelect()
        {
            InitializeComponent();
            dgView.MaxColumnWidth = 150;
        }

        private void bOK_Click(object sender, RoutedEventArgs e)
        {
            FillFilter();
            DialogResult = true;
            Close();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void SetDatabase(Database db)
        {
            if (db == null || db.DbType == DBTypes.None)
            {
                foreach(ListBoxItem lbi in lbType.Items)
                    if (Convert.ToInt32(lbi.Tag) == (int)DBTypes.None)
                    {
                        lbi.IsSelected = true;
                        break;
                    }
                return;
            }
            foreach (ListBoxItem lbi in lbType.Items)
                if (Convert.ToInt32(lbi.Tag) == (int)db.DbType)
                {
                    lbi.IsSelected = true;
                    break;
                }
            switch (db.DbType)
            {
                case (DBTypes.ODBC):
                    for(int i=0;i<cbODBC.Items.Count;i++)
                        if (cbODBC.Items[i].ToString().Equals(db.Path, StringComparison.InvariantCultureIgnoreCase))
                        {
                            cbODBC.SelectedIndex = i;
                            break;
                        }
                    for (int i = 0; i < cbTable.Items.Count; i++)
                        if (cbTable.Items[i].ToString().Equals(db.Table, StringComparison.InvariantCultureIgnoreCase))
                        {
                            cbTable.SelectedIndex = i;
                            break;
                        }
                    break;
                case DBTypes.OleText:
                    tbOleText_Dir.Text = db.Path;
                    RefreshDb();
                    for (int i = 0; i < cbTable.Items.Count; i++)
                        if (cbTable.Items[i].ToString().Equals(db.Table, StringComparison.InvariantCultureIgnoreCase))
                        {
                            cbTable.SelectedIndex = i;
                            break;
                        }
                    break;
            }
            int index1 = db.Filter.IndexOf("where", StringComparison.InvariantCultureIgnoreCase);
            int index2 = db.Filter.IndexOf("order by", StringComparison.InvariantCultureIgnoreCase);
            if (index1 >= 0)
                tbFilter.Text = db.Filter.Substring(index1 + 5, (index2 > index1) ? index2 - index1 - 5 : db.Filter.Length - index1 - 5);
            if (index2 >= 0)
                tbOrderBy.Text = db.Filter.Substring(index2 + 8, db.Filter.Length - index2 - 8);
        }
        public Database GetDatabase()
        {
            return db;
        }
        private void tcTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tiView.IsSelected)
            {
                FillFilter();
                dgView.ItemsSource = db.GetData(10).DefaultView;
            }
        }

        

        private void lbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int tp = Convert.ToInt32(((ListBoxItem)lbType.SelectedItem).Tag);
            gbDSN.Visibility = Visibility.Hidden;
            gbDir.Visibility = Visibility.Hidden;
            if (tp == (int)DBTypes.None)
            {
                tiFilter.IsEnabled = false;
                tiSearch.IsEnabled = false;
                tiSecurity.IsEnabled = false;
                tiView.IsEnabled = false;
                db = null;
                cbODBC.Items.Clear();
                gbDSN.Visibility = Visibility.Visible;
                gbDSN.IsEnabled = false;
                gbDir.IsEnabled = false;
                tbLogin.Text = "";
                pbPassword.Password = "";
                gbSecurity.IsEnabled = false;
                cbTable.Items.Clear();
                gbTable.IsEnabled = false;
            }
            if (tp == (int)DBTypes.ODBC)
            {
                gbDSN.IsEnabled = true;
                gbSecurity.IsEnabled = true;
                gbTable.IsEnabled = true;
                gbDSN.Visibility = Visibility.Visible;
                // считываем все ODBC источники
                RefreshDSN();
            }

            if (tp == (int) DBTypes.OleText)
            {
                gbDir.IsEnabled = true;
                gbTable.IsEnabled = true;
                gbDir.Visibility = Visibility.Visible;
            }
        }
        private IEnumerable<string> EnumDsn(RegistryKey root)
        {
            RegistryKey key = root.OpenSubKey(@"Software\ODBC\ODBC.INI\ODBC Data Sources");
            if (key != null)
            {
                foreach(string name in key.GetValueNames())
                {
                    string val = key.GetValue(name, "").ToString();
                    yield return name;
                }
            }
        }

        private void cbODBC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDb();
        }

        private void bRefreshTable_Click(object sender, RoutedEventArgs e)
        {
            RefreshDb();
        }
        private void RefreshDb()
        {
            if (lbType.SelectedItem == null)
                return;
            cbTable.Items.Clear();
            switch ((DBTypes)int.Parse(((ListBoxItem) lbType.SelectedItem).Tag.ToString()))
            {
                case DBTypes.ODBC:
                    db = new Database(DBTypes.ODBC, cbODBC.SelectedItem?.ToString(), tbLogin.Text, pbPassword.Password);
                    break;
                case DBTypes.OleText:
                    db = new Database(DBTypes.OleText, tbOleText_Dir.Text, tbLogin.Text, pbPassword.Password);
                    break;
            }
            db?.SetConnection();
            List<String> strs = db?.GetTables();
            if (strs == null)
                return;
            strs.Sort();
            foreach (string str in strs)
                cbTable.Items.Add(str);
        }

        private void bRefreshDSN_Click(object sender, RoutedEventArgs e)
        {
            RefreshDSN();
        }
        private void RefreshDSN()
        {
            cbODBC.Items.Clear();
            List<string> dsns = new List<string>();
            dsns.AddRange(EnumDsn(Registry.LocalMachine));
            dsns.AddRange(EnumDsn(Registry.CurrentUser));
            dsns.Sort();
            dsns.Insert(0, (string)this.FindResource("NotDefined"));
            foreach (string str in dsns)
                cbODBC.Items.Add(str);
        }
        private void cbTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (db == null || cbTable.SelectedValue == null)
                return;
            db.Table = cbTable.SelectedValue.ToString();
            db.Columns = db.GetFieldsList();
            cbFilterField.Items.Clear();
            for (int i=0;i<db.Columns.Count;i++)
                cbFilterField.Items.Add((string)db.Columns[i]);
            tiFilter.IsEnabled = true;
            //tiSearch.IsEnabled = true;
            //tiSecurity.IsEnabled = true;
            tiView.IsEnabled = true;
            
            //dgView.ItemsSource = db.GetData(10).DefaultView;
        }

        private void bAddFilter_Click(object sender, RoutedEventArgs e)
        {
            if (cbFilterField.SelectedItem == null || cbFilterRelation.SelectedItem == null)
                return;
            string str = "";
            switch (Convert.ToInt32(((ComboBoxItem)cbFilterRelation.SelectedItem).Tag))
            {
                case (1): // равно
                    str = String.Format("[{0}] = '{1}'", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
                case (2): // не равно
                    str = String.Format("[{0}] <> '{1}'", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
                case (3): // больше
                    str = String.Format("[{0}] > {1}", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
                case (4): // больше или равно
                    str = String.Format("[{0}] >= {1}", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
                case (5): // меньше
                    str = String.Format("[{0}] < {1}", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
                case (6): // меньше или равно
                    str = String.Format("[{0}] <= {1}", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
                case (7): // нулевое
                    str = String.Format("[{0}] is null", cbFilterField.SelectedItem);
                    break;
                case (8): // не нулевое 
                    str = String.Format("[{0}] is not null", cbFilterField.SelectedItem);
                    break;
                case (9): // начинается с 
                    str = String.Format("[{0}] like '{1}%'", cbFilterField.SelectedItem, tbFilterValue.Text);
                    break;
            }
            if (tbFilter.Text.Length == 0)
                tbFilter.Text = str;
            else
                tbFilter.Text += String.Format(" {0} {1}", (rbFilterOr.IsChecked == true) ? "OR" : "AND", str);
        }

        private void bCheckFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FillFilter();
                db.GetData(1);
                pCheckFilter.Data = (Geometry)TypeDescriptor.GetConverter(typeof(Geometry)).ConvertFrom("M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, this.FindResource("Error").ToString());
            }
        }
        private void FillFilter()
        {
            db.Filter = tbFilter.Text.Trim();
            if (db.Filter.Length > 0 && !db.Filter.ToLower().StartsWith("where"))
                db.Filter = String.Format(" where {0}", db.Filter);
            if (tbOrderBy.Text.Trim().Length > 0 && !tbOrderBy.Text.ToLower().StartsWith("order by"))
                db.Filter += String.Format(" order by {0}", tbOrderBy.Text);
        }

        private void tbFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            pCheckFilter.Data = (Geometry)TypeDescriptor.GetConverter(typeof(Geometry)).ConvertFrom("M6,5.75L10.25,10H7V16H13.5L15.5,18H7A2,2 0 0,1 5,16V10H1.75L6,5.75M18,18.25L13.75,14H17V8H10.5L8.5,6H17A2,2 0 0,1 19,8V14H22.25L18,18.25Z");
        }

        private void tbOrderBy_TextChanged(object sender, TextChangedEventArgs e)
        {
            pCheckFilter.Data = (Geometry)TypeDescriptor.GetConverter(typeof(Geometry)).ConvertFrom("M6,5.75L10.25,10H7V16H13.5L15.5,18H7A2,2 0 0,1 5,16V10H1.75L6,5.75M18,18.25L13.75,14H17V8H10.5L8.5,6H17A2,2 0 0,1 19,8V14H22.25L18,18.25Z");
        }

        private void bOleTextSelectDir_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult res = dialog.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    tbOleText_Dir.Text = dialog.SelectedPath;
                    tbOleText_Dir.Focus();
                    tbOleText_Dir.Select(tbOleText_Dir.Text.Length, 0);
                    RefreshDb();
                }
            }
        }
    }
}

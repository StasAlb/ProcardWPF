using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Devices;
using Xceed.Wpf.Toolkit;


using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using GroupBox = System.Windows.Controls.GroupBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RadioButton = System.Windows.Controls.RadioButton;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace ProcardWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class fMain : Window
    {
        public fMain()
        {
            stateManager = new ProcardWindowStateManager(SettingManager.ProcardSettings.WindowState, this);
            xmlSettings = new XmlDocument();
            try
            {
                xmlSettings.Load(String.Format("{0}{1}.xml", System.AppDomain.CurrentDomain.BaseDirectory, System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName.Replace(".exe", "").Replace(".vshost", "")));
            }
            catch
            {
            }
            xnmSettings = new XmlNamespaceManager(xmlSettings.NameTable);
            
            InitializeComponent();
            currentRegim = Regim.NoTask;
            SetLanguage(Params.Language);
            ShowPanel();
            Params.Log = 1;
        }
        #region MyVariables
        XmlDocument xmlSettings = null;
        XmlNamespaceManager xnmSettings = null;
        ProcardWindowStateManager stateManager;
        bool isBorderDevice = false;
        bool isBorderSmart = false;
        System.Threading.Thread threadPrint = null;
        Queue steps = new Queue();

        private Regim currentRegim;
        private ObjectType currentTool;
        private DesignObject currentObject;
        private DataTable dataIn;
        private int currentRecord;
        private Card card = null;
        private int prevX, prevY;
        PrintStatus printStatus = null;

        List<Para> listDataIn = new List<Para>();

        private string filename = "";
                
        #endregion
        public void Command_FileNew(object sender, ExecutedRoutedEventArgs e)
        {
            CreateNewCard();
        }
        private void CreateNewCard()
        {
            filename = "";
            this.Title = $"{this.FindResource("WindowTitle").ToString()}";
            card = new Card();
            //card.objects.Add(new CardObject(this.FindResource("Fields_Card").ToString()));
            card.Name = this.FindResource("Card_Name_default").ToString();
            currentRegim = Regim.Design;
            currentTool = ObjectType.None;
            currentObject = null;
            RefreshList();
            RefreshProperties();
            ShowPanel();
        }
        public void Command_FileNewCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_FileOpen(object sender, ExecutedRoutedEventArgs e)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Card));
            ser.UnknownNode += new XmlNodeEventHandler(Ser_UnknownNode);
            ser.UnknownAttribute += new XmlAttributeEventHandler(Ser_UnknownAttribute);
            try
            {
//#if DEBUG
                //filename = "design.xml";
//#else
                OpenFileDialog oFile = new OpenFileDialog();
                oFile.Filter = $"{(string) this.FindResource("DesignFiles")}|*.xml|{(string)this.FindResource("AllFiles")}|*.*";
                    //"Design files (*.xml)|*.xml|All files (*.*)|*.*";
                oFile.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "Designs");
                if (oFile.ShowDialog() == true)
                {
                    filename = oFile.FileName;
                }
                else
                    return;
//#endif
                XmlReader xr = XmlReader.Create(filename, new XmlReaderSettings { IgnoreWhitespace = false });
                card = (Card)ser.Deserialize(xr);
                xr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, (string)this.FindResource("Error"));
                return;
            }
            //восстанавливаем Parent у полей эмбоссирования и присваиваем digits
            //для смартов, если надо заполняем список ридеров
            //для изображений подтягиваем картинки-константы
            for(int i=0;i<card.objects.Count;i++)
            {
                if (card.objects[i].OType == ObjectType.EmbossText)
                {
                    DesignObject el = null;
                    if (card.FindObject(ref el, ((EmbossText)card.objects[i]).ParentID))
                    {
                        ((EmbossLine)el).X = el.X; // нужно, чтобы создалось нужное число digits
                        ((EmbossText)card.objects[i]).Parent = (EmbossLine)el;
                        ((EmbossLine)el).SetDigits(((EmbossText)card.objects[i]).Position, ((EmbossText)card.objects[i]).Shablon.Length, true);
                    }
                }
                if (card.objects[i].OType == ObjectType.SmartField && ((SmartField)card.objects[i]).SModule != null)
                {
                    if (((SmartField)card.objects[i]).SModule.SType == SmartModule.SmartType.OstcardStandard)
                        FillReaders();
                }
                if (card.objects[i].OType == ObjectType.ImageField && card.objects[i].InType == InTypes.File)
                    card.objects[i].SetText(card.objects[i].InData);
            }
            this.Title = $"{this.FindResource("WindowTitle").ToString()} - {filename}";
            //расчитываем MaxID
            card.SetMaxID();
            if (card.DbIn != null)
                card.DbIn.SetConnection();
            currentRegim = Regim.Design;
            currentTool = ObjectType.None;
            currentObject = null;
            RefreshList();
            RefreshProperties();
            //обновляем контексты
            lbFields_SelectionChanged(this, null);
            cbDevice_SelectionChanged(this, null);
            ShowPanel();
        }
        private void Ser_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Params.WriteLogString("XmlOpen, Unknown node: {0} = {1}", e.Name, e.Text);
        }
        private void Ser_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            Params.WriteLogString("XmlOpen, Unknown attribute: {0} = {1}", e.Attr.Name, e.Attr.Value);
        }
        public void Command_FileOpenCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_FileSave(object sender, ExecutedRoutedEventArgs e)
        {
//#if DEBUG
  //          filename = "design.xml";
//#else
            if (filename.Length == 0)
            {
                SaveFileDialog sFile = new SaveFileDialog();
                sFile.Filter = "Design files (*.xml)|*.xml|All files (*.*)|*.*";
                sFile.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "Designs");
                if (sFile.ShowDialog() == true)
                {
                    filename = sFile.FileName;
                }
                else
                    return;
            }
 //#endif
            this.Title = $"{this.FindResource("WindowTitle").ToString()} - {filename}";
            XmlSerializer ser = new XmlSerializer(typeof(Card));
            ser.UnknownNode += new XmlNodeEventHandler(Ser_UnknownNode);
            ser.UnknownAttribute += new XmlAttributeEventHandler(Ser_UnknownAttribute);
            TextWriter tw = new StreamWriter(filename);
            ser.Serialize(tw, card);
            tw.Close();
        }
        public void Command_FileSaveCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(card == null || card.Saved);
        }
        public void Command_Pointer(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            currentTool = ObjectType.None;
        }
        public void Command_PointerCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_Russian(object sender, ExecutedRoutedEventArgs e)
        {
            SetLanguage(Lang.Russian);
        }
        public void Command_RussianCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Params.Language != Lang.Russian);
        }
        public void Command_English(object sender, ExecutedRoutedEventArgs e)
        {
            SetLanguage(Lang.English);
        }
        public void Command_EnglishCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Params.Language != Lang.English);
        }
        public void Command_EmbossLine(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            int cnt = card.GetFieldCount(ObjectType.EmbossLine);
            if (!card.Landscape)
            {
                MessageBox.Show((string)this.FindResource("Message_EmbossOrientation"), (string)this.FindResource("Error"));
                return;
            }
            currentTool = ObjectType.None;
            if (cnt >= 11)
            {
                MessageBox.Show((string)this.FindResource("Message_EmbossLineExceed"), (string)this.FindResource("Error"));
                return;
            }
            EmbossLine el = new EmbossLine();
            el.Name = card.GetName(ObjectType.EmbossLine);
            el.ID = card.GetNextID();
            double koef = (Params.UseMetric) ? 25.4 : 1;
            if (cnt == 0)
            {
                el.Font = EmbossFont.Farrington;
                el.X = 0.401 * koef;
                el.Y = 0.843 * koef;
                el.FirstLine = true;
            }
            else
            {
                el.Font = EmbossFont.Gothic;
                el.X = 0.301 * koef;
                el.FirstLine = false;
                el.Y = (1 + (cnt-4) * 0.2) * koef;
                if (cnt == 1)
                    el.Y = 0.492 * koef;
                if (cnt == 2)
                    el.Y = 0.334 * koef;
                if (cnt == 3)
                    el.Y = 0.177 * koef;
            }
            el.RedrawCard += DrawCard;
            card.objects.Add(el);
            currentObject = el;
            RefreshList();
            DrawCard(currentObject.Side);
        }
        public void Command_EmbossLineCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void SetLanguage(Lang newLanguage)
        {
            Params.Language = newLanguage;
            ResourceDictionary dict = Application.Current.Resources;
            string resname = "Interface.ru.xaml";
            switch (Params.Language)
            {
                case Lang.Russian:
                    resname = "Interface.ru.xaml";
                    break;
                case Lang.English:
                    resname = "Interface.en.xaml";
                    break;
                default:
                    resname = "Interface.ru.xaml";
                    break;
            }
            try
            {
                dict.BeginInit();
                int i = 0;
                for (i = 0; i < dict.MergedDictionaries.Count; i++)
                {
                    if (((System.Windows.ResourceDictionary)dict.MergedDictionaries[i]).Source.LocalPath.EndsWith(resname))
                        break;
                }
                if (i < dict.MergedDictionaries.Count)
                {
                    ResourceDictionary res = dict.MergedDictionaries[i];
                    dict.MergedDictionaries.Remove(dict.MergedDictionaries[i]);
                    dict.MergedDictionaries.Add(res);
                }
            }
            finally
            {
                dict.EndInit();
            }
            #region изменяем содержимое combobox
            cbTextAlign.Items.Clear();
            List<Para> list = new List<Para>();
            list.Add(new Para((int)TextAlignment.Left, (string)this.FindResource("TextAlignLeft")));
            list.Add(new Para((int)TextAlignment.Center, (string)this.FindResource("TextAlignCenter")));
            list.Add(new Para((int)TextAlignment.Right, (string)this.FindResource("TextAlignRight")));
            cbTextAlign.ItemsSource = list;
            cbTextRotate.Items.Clear();
            list = new List<Para>();
            list.Add(new Para((int)Rotate.None, (string)this.FindResource("TextRotateNo")));
            list.Add(new Para((int)Rotate.R90, (string)this.FindResource("TextRotate90")));
            list.Add(new Para((int)Rotate.R180, (string)this.FindResource("TextRotate180")));
            list.Add(new Para((int)Rotate.R270, (string)this.FindResource("TextRotate270")));
            cbTextRotate.ItemsSource = list;
            cbImageStyle.Items.Clear();
            list = new List<Para>();
            list.Add(new Para((int)ImageStyle.FitToField, (string)this.FindResource("Image_StyleStretch")));
            list.Add(new Para((int)ImageStyle.AutoSize, (string)this.FindResource("Image_StyleAuto")));
            list.Add(new Para((int)ImageStyle.Background, (string)this.FindResource("Image_StyleBackground")));
            list.Add(new Para((int)ImageStyle.Mask, (string)this.FindResource("Image_StyleMask")));
            cbImageStyle.ItemsSource = list;
            #endregion
        }
        public void Command_MagStripe(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            currentTool = ObjectType.None;
            for (int i = 0; i < card.objects.Count; i++)
                if (card.objects[i].OType == ObjectType.MagStripe)
                    return;
            MagStripe ms = new MagStripe();
            ms.Name = card.GetName(ObjectType.MagStripe);
            ms.ID = card.GetNextID();
            card.objects.Add(ms);
            currentObject = ms;
            RefreshList();
            DrawCard(currentObject.Side);
        }
        public void Command_MagStripeCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_EmbossField(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            currentTool = ObjectType.EmbossText;
        }
        public void Command_EmbossFieldCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_EmbossField2(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            int cnt = card.GetFieldCount(ObjectType.EmbossText2);
            if (!card.Landscape)
            {
                MessageBox.Show((string)this.FindResource("Message_EmbossOrientation"), (string)this.FindResource("Error"));
                return;
            }
            currentTool = ObjectType.None;
            EmbossText2 et2 = new EmbossText2();
            et2.Name = card.GetName(ObjectType.EmbossText2);
            et2.ID = card.GetNextID();
            et2.Side = SideType.Front;
            et2.Shablon = "X";
            double koef = (Params.UseMetric) ? 25.4 : 1;
            if (cnt == 0)
            {
                et2.Font = EmbossFont.Farrington;
                et2.Shablon = "**** **** **** ****";
                et2.X = 0.401 * koef;
                et2.Y = 0.843 * koef;
            }
            else
            {
                et2.Font = EmbossFont.Gothic;
                et2.X = 0.301 * koef;
                et2.Y = (1 + (cnt - 4) * 0.2) * koef;
                if (cnt == 1)
                {
                    et2.X = 1.601 * koef;
                    et2.Y = 0.492 * koef;
                    et2.Shablon = "XXXXX";
                }
                if (cnt == 2)
                {
                    et2.Y = 0.334 * koef;
                    et2.Shablon = "XXXXXXXXXXXXXXXXXXXXXXXX";
                }
                if (cnt == 3)
                {
                    et2.Y = 0.177 * koef;
                    et2.Shablon = "XXXXXXXXXXXXXXXXXXXXXXXX";
                }
                if (cnt == 4)
                {
                    et2.X = 1.5 * koef;
                    et2.Y = 1 * koef;
                    et2.Shablon = "XXXX XXX";
                    et2.Font = EmbossFont.MCIndent;
                    et2.Side = SideType.Back;
                }
            }
            et2.RedrawCard += DrawCard;
            card.objects.Add(et2);
            currentObject = et2;
            RefreshList();
            DrawCard(currentObject.Side);
        }
        public void Command_EmbossField2CanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_TextField(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            bTextField.IsChecked = true;
            currentTool = ObjectType.TextField;
        }
        public void Command_TextFieldCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_ImageField(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            bImageField.IsChecked = true;
            currentTool = ObjectType.ImageField;
        }
        public void Command_ImageFieldCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_Topcoat(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            bTopcoatField.IsChecked = true;
            currentTool = ObjectType.TopCoat;
        }
        public void Command_TopcoatCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_Barcode(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            bBarcodeField.IsChecked = true;
            currentTool = ObjectType.Barcode;
        }
        public void Command_BarcodeCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_SmartField(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
            currentTool = ObjectType.None;
            int cnt = 0;
            for (int i = 0; i < card.objects.Count; i++)
                if (card.objects[i].OType == ObjectType.SmartField)
                    cnt++;
            if (cnt > 4)
                return;
            SmartField sf = new SmartField();
            sf.Name = card.GetName(ObjectType.SmartField);
            sf.ID = card.GetNextID();
            card.objects.Add(sf);
            currentObject = sf;
            RefreshList();
            DrawCard(currentObject.Side);
        }
        public void Command_SmartFieldCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void Command_ReportField(object sender, ExecutedRoutedEventArgs e)
        {
            ResetToolBar();
            ClearDesignContext();
        }
        public void Command_ReportFieldCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        public void ResetToolBar()
        {
            currentTool = ObjectType.None;
            //bPointer.IsChecked = true;
            //bEmbossLine.IsChecked = false;
            bEmbossField.IsChecked = false;
            bImageField.IsChecked = false;
            bTextField.IsChecked = false;
            bTopcoatField.IsChecked = false;
            bBarcodeField.IsChecked = false;

        }
        
        public void ShowPanel()
        {
            gDesign.Visibility = Visibility.Hidden;
            gLogo.Visibility = Visibility.Hidden;
            gPrint.Visibility = Visibility.Hidden;
            switch (currentRegim)
            {
                case(Regim.NoTask):
                    gLogo.Visibility = Visibility.Visible;
                    break;
                case(Regim.Design):
                    gDesign.Visibility = Visibility.Visible;
                    DrawCard();
                    break;
                case (Regim.Print):
                    gPrint.Visibility = Visibility.Visible;
                    LoadRecordSet();
                    rbSingleCard.IsChecked = true;
                    tbPrintCopies.Text = "1";
                    if (dataIn == null)
                    {
                        rbToEnd.IsEnabled = false;
                        rbInRange.IsEnabled = false;
                    }
                    else
                    {
                        rbToEnd.IsEnabled = true;
                        rbInRange.IsEnabled = true;
                        tbRangeStart.Text = (currentRecord+1).ToString();
                        tbRangeEnd.Text = dataIn.Rows.Count.ToString();
                    }
                    SetFieldsText();
                    CreatePrintValue();
                    DrawCard();
                    break;
                default:
                    gLogo.Visibility = Visibility.Visible;
                    break;
            }
        }
        private void DrawCard()
        {
            this.Dispatcher.Invoke(delegate ()
            {
                DrawCard(SideType.Front);
                DrawCard(SideType.Back);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
        private void DrawCard(SideType side)
        {
            if (currentRegim == Regim.Design)
            {
                if (side == SideType.Front)
                    dpDesignFront.ClearVisuals();
                else
                    dpDesignBack.ClearVisuals();
            }
            if (currentRegim == Regim.Print)
            {
                if (side == SideType.Front)
                    dpPrintFront.ClearVisuals();
                else
                    dpPrintBack.ClearVisuals();
            }
            if (card == null)
                return;
            card.SetMas(96);
            card.RecalcTopLeft((int)dpDesignFront.ActualWidth, (int)dpDesignFront.ActualHeight, (int)dpDesignBack.ActualHeight);
            DrawingVisual dw = new DrawingVisual();
            DrawingContext dc = dw.RenderOpen();
            card.Draw(dc, side, Regim.Design, 0);
            if (currentRegim == Regim.Design)
            {
                if (side == SideType.Front)
                    dpDesignFront.AddVisual(dw);
                else
                    dpDesignBack.AddVisual(dw);
            }
            if (currentRegim == Regim.Print)
            {
                if (side == SideType.Front)
                    dpPrintFront.AddVisual(dw);
                else
                    dpPrintBack.AddVisual(dw);
            }
            dc.Close();
            for (int i = 0; i < card.objects.Count; i++)
            {
                if (card.objects[i].Side == side)
                {
                    bool sel = (currentObject == null) ? false : card.objects[i].ID == currentObject.ID;
                    if (currentRegim == Regim.Print)
                        sel = false;
                    dw = new DrawingVisual();
                    card.objects[i].Draw(dw, currentRegim, sel, 0);
                    if (currentRegim == Regim.Design)
                    {
                        if (side == SideType.Front)
                            dpDesignFront.AddVisual(dw);
                        else
                            dpDesignBack.AddVisual(dw);
                    }
                    if (currentRegim == Regim.Print)
                    {
                        if (side == SideType.Front)
                            dpPrintFront.AddVisual(dw);
                        else
                            dpPrintBack.AddVisual(dw);
                        for (int t=0;t<spPrintValues.Children.Count;t++)
                        {
                            ControlPrintText cpt = (ControlPrintText)spPrintValues.Children[t];
                            if (cpt.ObjectId != card.objects[i].ID)
                                continue;
                            if (card.objects[i].OType == ObjectType.MagStripe)
                            {
                                if (cpt.PrintValuesType == PrintValuesTypes.Text)
                                    cpt.SetText(((MagStripe)card.objects[i]).GetText((int)cpt.Misc));
                                continue;
                            }
                            if (cpt.PrintValuesType == PrintValuesTypes.Text)
                                cpt.SetText(card.objects[i].GetText());
                            break;
                        }
                    }
                }            
            }
        }
        private void LoadRecordSet()
        {
            if (dataIn != null)
                dataIn.Clear();
            currentRecord = 0;
            if (card.DbIn == null)
                return;
            dataIn = card.DbIn.GetData();
        }
        /// <summary>
        /// создание контролов справа в режиме печати
        /// </summary>
        private void CreatePrintValue()
        {
            spPrintValues.Children.Clear();
            for(int i=0;i<card.objects.Count;i++)
            {
                if (card.objects[i].OType == ObjectType.EmbossText || card.objects[i].OType == ObjectType.EmbossText2
                    || card.objects[i].OType == ObjectType.TextField || card.objects[i].OType == ObjectType.Barcode)
                {
                    ControlPrintText cpt = new ControlPrintText(card.objects[i].ID, PrintValuesTypes.Text);
                    cpt.SetTitle(card.objects[i].Name);
                    cpt.Lock(card.objects[i].InType == InTypes.Keyboard);
                    cpt.printTextChanged += Cpt_printTextChanged;
                    spPrintValues.Children.Add(cpt);
                }
                if (card.objects[i].OType == ObjectType.MagStripe)
                {
                    for(int t=0;t<3;t++)
                    {
                        if (((MagStripe)card.objects[i]).InTypeM[t] != InTypes.None)
                        {
                            ControlPrintText cpt = new ControlPrintText(card.objects[i].ID, PrintValuesTypes.Text);
                            cpt.SetTitle(String.Format("{0} {1}", this.FindResource("MagStripe"), t+1));
                            cpt.Lock(((MagStripe)card.objects[i]).InTypeM[t] == InTypes.Keyboard);
                            cpt.Misc = t;
                            cpt.printTextChanged += Cpt_printTextChanged;
                            spPrintValues.Children.Add(cpt);
                        }
                    }
                }
            }
        }

        private void Cpt_printTextChanged(int objectId, string newText, object misc)
        {
            DesignObject desO = null;
            if (card.FindObject(ref desO, objectId))
            {
                switch (desO.OType)
                {
                    case ObjectType.MagStripe:
                        ((MagStripe)desO).SetText(newText, (int)misc);
                        break;
                    default:
                        desO.SetText(newText);
                        break;
                }
            }
            DrawCard(desO.Side);
        }
        private bool SetFieldsText()
        {
            bool wasLast = false;
            //обнуляем значения полей
            for (int i = 0; i < card.objects.Count; i++)
            {
                if (card.objects[i].OType == ObjectType.MagStripe)
                {
                    ((MagStripe)card.objects[i]).SetText("", 0);
                    ((MagStripe)card.objects[i]).SetText("", 1);
                    ((MagStripe)card.objects[i]).SetText("", 2);
                }
                else
                    card.objects[i].SetText("");
            }
            if (dataIn != null && currentRecord >= dataIn.Rows.Count)
            {
                wasLast = true;
                currentRecord = dataIn.Rows.Count - 1;
//                return false;
            }
            //if (currentRecord < 0)
                //currentRecord = 0;
            if (dataIn != null)
                sbiObjectName.Dispatcher.Invoke(new Action(delegate () { sbiObjectName.Content = String.Format("{0}: {1}/{2}", this.FindResource("CurrentCard"), currentRecord + 1, dataIn.Rows.Count); }), System.Windows.Threading.DispatcherPriority.Background);
            else
                sbiObjectName.Dispatcher.Invoke(new Action(delegate () { sbiObjectName.Content = String.Format("{0}", this.FindResource("CurrentCard")); }), System.Windows.Threading.DispatcherPriority.Background);
            //кроме композитных
            for (int i=0;i<card.objects.Count;i++)
            { 
                if (card.objects[i].OType == ObjectType.MagStripe)
                {
                    for (int t = 0; t < 3; t++)
                    {
                        if (((MagStripe)card.objects[i]).InTypeM[t] == InTypes.None)
                            ((MagStripe)card.objects[i]).SetText("", t);
                        if (((MagStripe)card.objects[i]).InTypeM[t] == InTypes.Db && currentRecord >= 0)
                            ((MagStripe)card.objects[i]).SetText(dataIn.Rows[currentRecord][((MagStripe)card.objects[i]).InDataM[t]].ToString(), t);
                        if (((MagStripe)card.objects[i]).InTypeM[t] == InTypes.Keyboard)
                            ((MagStripe)card.objects[i]).SetText("", t);
                    }
                    continue;
                }

                if (card.objects[i].InType == InTypes.None)
                {
                    card.objects[i].SetText("");
                    if (card.objects[i].OType == ObjectType.EmbossText)
                        card.objects[i].SetText(((EmbossText)card.objects[i]).Shablon);
                    if (card.objects[i].OType == ObjectType.EmbossText2)
                        card.objects[i].SetText(((EmbossText2)card.objects[i]).Shablon);
                    if (card.objects[i].OType == ObjectType.TextField)
                        card.objects[i].SetText(((TextField)card.objects[i]).Shablon);
                    if (card.objects[i].OType == ObjectType.Barcode)
                        card.objects[i].SetText(((Barcode)card.objects[i]).Shablon);
                }
                if (card.objects[i].InType == InTypes.Db && currentRecord >= 0)
                    card.objects[i].SetText(dataIn.Rows[currentRecord][card.objects[i].InData].ToString());
                if (card.objects[i].InType == InTypes.Keyboard)
                    card.objects[i].SetText("");
                if (card.objects[i].OType == ObjectType.ImageField && card.objects[i].InType == InTypes.File)
                    card.objects[i].SetText(card.objects[i].InData);
                //для некомпозитных эмбосс поля обрабатываем в шаблоне спецсимвол
                if (DesignObject.IsEmbossField(card.objects[i].OType) && card.objects[i].InType != InTypes.Composite)
                {
                    string newtext = ((IEmbossField)card.objects[i]).Shablon;
                    int posInNew = newtext.IndexOf('*');
                    string oldtext = card.objects[i].GetText();
                    int posInOld = 0;
                    while (posInNew >= 0)
                    {
                        if (posInOld < oldtext.Length)
                            newtext = newtext.Remove(posInNew, 1).Insert(posInNew, oldtext[posInOld++].ToString());
                        else
                            newtext = newtext.Remove(posInNew, 1);
                        posInNew = newtext.IndexOf('*');
                    }
                    if (posInOld > 0) //если мы хоть раз зашли в обработку по спецсимволу
                        card.objects[i].SetText(newtext);
                }
            }
            //теперь композитные
            for(int i=0;i<card.objects.Count;i++)
            {
                if (card.objects[i].OType == ObjectType.MagStripe)
                {
                    for(int t=0;t<3;t++)
                    {
                        if (((MagStripe)card.objects[i]).InTypeM[t] == InTypes.Composite)
                        {
                            ((MagStripe)card.objects[i]).SetText(MakeComposite(((MagStripe)card.objects[i]).InDataM[t]),t);
                        }
                    }
                    continue;
                }
                if (card.objects[i].InType == InTypes.Composite)
                    card.objects[i].SetText(MakeComposite(card.objects[i].InData));
            }
            //для полей эмбоссирования контролируем длину
            //for (int i=0;i<card.objects.Count;i++)
            //{
            //    if (card.objects[i].OType == ObjectType.EmbossText || card.objects[i].OType == ObjectType.EmbossText2)
                    
            //}
            return !wasLast;
        }
        private string MakeComposite(string composite)
        {
            CompositeArray ca = null;
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(CompositeArray));
                StringReader sr = new StringReader(composite);
                ca = (CompositeArray)ser.Deserialize(sr);
                sr.Close();
            }
            catch
            {
                ca = null;
            }
            if (ca == null)
                return "";
            string res = "";
            for (int i = 0; i < ca.Count; i++)
            {
                string one = "";
                if (ca[i].Type == CompositeType.Design)
                {
                    DesignObject dsO = null;
                    if (card.FindObject(ref dsO, ca[i].Data))
                        one = dsO.GetText();
                }
                if (ca[i].Type == CompositeType.DB && dataIn != null && currentRecord >= 0)
                    one = dataIn.Rows[currentRecord][ca[i].Data].ToString();
                if (ca[i].Type == CompositeType.Fixed)
                {
                    one = ca[i].Data;
                    if (one == "[sp]")
                        one = " ";
                }
                if (ca[i].Type == CompositeType.Feedback)
                {
                    if (ca[i].Data == "1")
                        one = card.device.GetMagstripe()[0];
                    if (ca[i].Data == "2")
                        one = card.device.GetMagstripe()[1];
                    if (ca[i].Data == "3")
                        one = card.device.GetMagstripe()[2];

                }
                //отработка функции
                try
                {
                    if (ca[i].Function == CompositeFunc.AddChar && ca[i].Parameters.Count > 0)
                    {
                        int len = Convert.ToInt32(ca[i].Parameters[0]);
                        string ch = (ca[i].Parameters.Count > 1) ? (string)ca[i].Parameters[1] : " ";
                        ch = (ch.Length > 0) ? ch : " ";
                        while (one.Length < len)
                            one += ch;
                        one = one.Substring(0, len);
                    }
                    if (ca[i].Function == CompositeFunc.SubString && ca[i].Parameters.Count > 0)
                    {
                        int start = 1, len = 0;
                        try
                        {
                            start = Convert.ToInt32(ca[i].Parameters[0]);
                        }
                        catch
                        {
                            start = 1;
                        }
                        try
                        {
                            len = Convert.ToInt32(ca[i].Parameters[1]);
                        }
                        catch
                        {
                            len = 0;
                        }
                        one = len > 0 ? one.Substring(start - 1, len) : one.Substring(start - 1);
                    }
                    if (ca[i].Function == CompositeFunc.Split && ca[i].Parameters.Count == 2)
                    {
                        if (Convert.ToString(ca[i].Parameters[0]).Length == 1)
                            one = one.Split(Convert.ToChar(ca[i].Parameters[0]))[Convert.ToInt32(ca[i].Parameters[1])];
                        else
                            one = one.Split(' ')[Convert.ToInt32(ca[i].Parameters[1])];
                    }
                    if (ca[i].Function == CompositeFunc.Ean13)
                    {
                        one = one.Replace(" ","");
                        if (one.Length == 12)
                        {
                            int sum = 0;
                            try
                            {
                                for (int j = 0; j < 12; j++)
                                    sum += Convert.ToInt32(one[j] - 48) * ((j % 2 == 0) ? 1 : 3);
                                int t = 0;
                                while ((sum + t) % 10 != 0)
                                    t++;
                                one += t.ToString();
                            }
                            catch { }
                        }
                    }

                    if (ca[i].Function == CompositeFunc.Sphinx)
                    {
                        one = one.Replace(" ", "");
                        if (one.Length == 16)
                        {
                            try
                            {
                                one = String.Format("00{0:X4}{1:X8}", Convert.ToInt64(one.Substring(0, 4)), Convert.ToInt64(one.Substring(4, 10)));
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message);
                            }
                        }
                        break;
                    }
                }
                catch { }
                res += one;
            }
            return res;
        }
        private void pDesignFront_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            DrawCard();
        }
        private void pDesignBack_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            DrawCard();
        }
        private void RefreshList()
        {
            //lbFields.ItemsSource = card.objects.collection; // Это если делать через binding, решил для ListBox делать по старому потому что начинаются проблемы с первой строкой Карта
            //lbFields.DataContext = card.objects;
            lbFields.Items.Clear();
            if (card == null)
                return;
            ListBoxItem lbi = new ListBoxItem();
            lbi.Tag = 0;
            lbi.SetResourceReference(ListBoxItem.ContentProperty, "Fields_Card");
            lbFields.Items.Add(lbi);
            for (int i = 0; i < card.objects.Count; i++)
            {
                lbi = new ListBoxItem();
                lbi.Tag = card.objects[i].ID;
                lbi.SetValue(ListBoxItem.ContentProperty, card.objects[i].Name);
                lbFields.Items.Add(lbi);
            }
        }

        private void RefreshList(int field_id)
        {
            RefreshList();
            foreach (ListBoxItem lbi in lbFields.Items)
            {
                if ((int) lbi.Tag == field_id)
                {
                    lbi.IsSelected = true;
                    break;
                }
            }
        }
        private void RefreshProperties()
        {
            if (card == null)
                return;
            gbEmbossLine.Visibility = Visibility.Hidden;
            gbEmbossText.Visibility = Visibility.Hidden;
            gbEmbossText2.Visibility = Visibility.Hidden;
            gbCard.Visibility = Visibility.Hidden;
            gbMagStripe.Visibility = Visibility.Hidden;
            gbSmart.Visibility = Visibility.Hidden;
            gbText.Visibility = Visibility.Hidden;
            gbImage.Visibility = Visibility.Hidden;
            gbBarcode.Visibility = Visibility.Hidden;
            if (currentObject == null)
            {
                gFieldCommon.IsEnabled = false;
                gbCard.Visibility = Visibility.Visible;
                tbDbIn.Text = (card.DbIn == null) ? "" : card.DbIn.ToString();
                tbDbOut.Text = (card.DbOut == null) ? "" : card.DbOut.ToString();
                if (card.device != null)
                {
                    for(int i=0;i<cbDevice.Items.Count;i++)
                        if (((Para)cbDevice.Items[i]).ID == (int)card.device.DeviceType)
                        {
                            cbDevice.SelectedIndex = i;
                            break;
                        }
                    if (DeviceClass.IsXPS(card.device.DeviceType))
                        tbCEHopper.Text = $"{((XPSPrinter) card.device).HopperID}";
                }
            }
            else
            {
                gFieldCommon.IsEnabled = true;
                switch (currentObject.OType)
                {
                    case ObjectType.EmbossLine:
                        for (int i=0;i<cbEmbossLine_Font.Items.Count;i++)
                        {
                            if (Convert.ToInt32(((ComboBoxItem)cbEmbossLine_Font.Items[i]).Tag) == Convert.ToInt32(((EmbossLine)currentObject).Font))
                            {
                                cbEmbossLine_Font.SelectedIndex = i;
                                break;
                            }
                        }
                        gbEmbossLine.Visibility = Visibility.Visible;
                        break;
                    case ObjectType.EmbossText:
                        gbEmbossText.Visibility = Visibility.Visible;
                        lEmbossTextLength.SetValue(Label.ContentProperty, String.Format("{0} {1}", this.FindResource("EmbossText_Length"), ((EmbossText)currentObject).Shablon.Length));
                        tbEmbossTextPosition.Text = ((EmbossText)currentObject).Position.ToString();
                        tbEmbossTextShablon.Text = ((EmbossText)currentObject).Shablon;
                        for(int i=0;i<cbEmbossText_Align.Items.Count;i++)
                            if (Convert.ToInt32(((ComboBoxItem)cbEmbossText_Align.Items[i]).Tag) == (int)((EmbossText)currentObject).Align)
                            {
                                cbEmbossText_Align.SelectedIndex = i;
                                break;
                            }
                        fieldDataIn(cbEmbossText_In, (InTypes)currentObject.InType, currentObject.InData);
                        break;
                    case ObjectType.EmbossText2:
                        gbEmbossText2.Visibility = Visibility.Visible;  
                        gbEmbossText2.DataContext = (EmbossText2)currentObject;
                        fieldDataIn(cbEmbossText2_In, (InTypes)currentObject.InType, currentObject.InData);
                        break;
                    case ObjectType.MagStripe:
                        gbMagStripe.Visibility = Visibility.Visible;
                        fieldDataIn(cbMagStripe1, (InTypes)((MagStripe)currentObject).InTypeM[0], ((MagStripe)currentObject).InDataM[0]);
                        fieldDataIn(cbMagStripe2, (InTypes)((MagStripe)currentObject).InTypeM[1], ((MagStripe)currentObject).InDataM[1]);
                        fieldDataIn(cbMagStripe3, (InTypes)((MagStripe)currentObject).InTypeM[2], ((MagStripe)currentObject).InDataM[2]);
                        cbReadMagstripe.IsChecked = ((MagStripe)currentObject).Feedback;
                        break;
                    case ObjectType.SmartField:
                        gbSmart.Visibility = Visibility.Visible;
                        fieldDataIn(cbSmartIn, (InTypes)currentObject.InType, currentObject.InData);
                        lSmartTitle.DataContext = (SmartField)currentObject;
                        gbSmart.Height = 100;
                        if (((SmartField)currentObject).SModule == null)
                            break;
                        gbSmartConfig.DataContext = ((SmartField)currentObject).SModule;
                        cbSmartType.DataContext = (SmartField)currentObject;
                        //if (((SmartField)currentObject).SModule.SType == SmartModule.SmartType.OstcardStandard)
                        //{
                            gbSmartOstcardStandart.DataContext = (SmartModule.OstcardStandard)((SmartField)currentObject).SModule;
                        //}
                        break;
                    case ObjectType.TextField:
                        gbText.Visibility = Visibility.Visible;
                        gbText.DataContext = (TextField)currentObject;
                        fieldDataIn(cbTextField_In, (InTypes)currentObject.InType, currentObject.InData);
                        break;
                    case ObjectType.ImageField:
                        gbImage.Visibility = Visibility.Visible;
                        gbImage.DataContext = (ImageField)currentObject;
                        fieldDataIn(cbImageIn, (InTypes)currentObject.InType, currentObject.InData, true);
                        break;
                    case ObjectType.Barcode:
                        gbBarcode.Visibility = Visibility.Visible;
                        gbBarcode.DataContext = (Barcode)currentObject;
                        fieldDataIn(cbBarcodeIn, (InTypes)currentObject.InType, currentObject.InData);
                        break;
                    default:
                        gbCard.Visibility = Visibility.Visible;
                        break;
                }
            }
        }
        private int cbSmartModuleTypeIndex(int SmartModuleType)
        {
            for(int i=0;i<cbSmartType.Items.Count;i++)
            {
                if (Convert.ToInt32(((ComboBoxItem)cbSmartType.Items[i]).Tag) == SmartModuleType)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// заполнение комбобокса для входных данных и выбор текущего
        /// </summary>
        /// <param name="sender"></param>
        private void fieldDataIn(ComboBox sender, InTypes cInType, string sInData)
        {
            fieldDataIn(sender, cInType, sInData, false);
        }
        private void fieldDataIn(ComboBox sender, InTypes cInType, string sInData, bool image)
        {
            sender.Items.Clear();
            sender.Items.Add(new Para((int)InTypes.None, (string)this.FindResource("NotDefined")));
            if (!image)
                sender.Items.Add(new Para((int)InTypes.Keyboard, (string)this.FindResource("Field_Keyboard")));
            if (!image)
                sender.Items.Add(new Para((int)InTypes.Auto, (string)this.FindResource("Field_Auto")));
            if (image)
                sender.Items.Add(new Para((int) InTypes.File, (string) this.FindResource("Field_File")));
            sender.Items.Add(new Para((int)InTypes.Composite, (string)this.FindResource("Field_Composite")));
            if (card.DbIn != null)
                for (int i=0; i<card.DbIn.Columns.Count; i++)// each (string col in card.DbIn.Columns)
                    sender.Items.Add(new Para((int)InTypes.Db, card.DbIn.Columns[i], card.DbIn.Table + "-->" + card.DbIn.Columns[i]));
            for(int i=0; i<sender.Items.Count; i++)
            {
                if (((Para)sender.Items[i]).ID == (int)cInType)
                {
                    if (cInType != InTypes.Db)
                    {
                        sender.SelectedIndex = i;
                        return;
                    }
                    if ((string)((Para)sender.Items[i]).Value == sInData)
                    {
                        sender.SelectedIndex = i;
                        return;
                    }
                }
            }
            sender.SelectedIndex = 0;
        }
        private void fieldDataInChanged(ComboBox sender)
        {
            if (currentObject == null || sender.SelectedItem == null)
                return;
            InTypes temp;
            //для композитных полей новое не присваиваем
            switch (currentObject.OType)
            {
                case (ObjectType.MagStripe):
                    int index = Convert.ToInt32(sender.Tag) - 1;
                    temp = (InTypes)((MagStripe)currentObject).InTypeM[index];
                    ((MagStripe)currentObject).InTypeM[index] = (InTypes)((Para)sender.SelectedItem).ID;
                    if (((MagStripe)currentObject).InTypeM[index] == temp && temp == InTypes.Composite)
                        break;
                    ((MagStripe)currentObject).InDataM[index] = (string)((Para)sender.SelectedItem).Value;
                    break;
                default:
                    temp = (InTypes)currentObject.InType;
                    currentObject.InType = (InTypes)((Para)sender.SelectedItem).ID;
                    if (currentObject.InType == temp && temp == InTypes.Composite)
                        break;
                    if (currentObject.InType == temp && temp == InTypes.File)
                        break;
                    currentObject.InData = (string)((Para)sender.SelectedItem).Value;
                    break;
            }

            //if (currentObject.OType == ObjectType.ImageField && ((Para)sender.SelectedItem).ID == (int)InTypes.File)
            //{
            //    OpenFileDialog ofd = new OpenFileDialog();
            //    ofd.Filter = $"{(string) this.FindResource("Image_ImageFileExt")}|*.bmp;*.gif;*.jpg;*.jpeg;*.png|{(string) this.FindResource("AllFiles")}|*.*";
            //    if (ofd.ShowDialog() == true)
            //    {
            //        currentObject.InData = ofd.FileName;
            //        currentObject.SetText(ofd.FileName);
            //    }
            //}
            DrawCard();
        }
        private void lbFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (lbFields.SelectedItem == null)
            //  return;
            //currentObject = (DesignObject)lbFields.SelectedItem;
            if (lbFields.SelectedItem == null)
                return;
            card.FindObject(ref currentObject, (int)((ListBoxItem)lbFields.SelectedItem).Tag);
            if (currentObject != null)
            {
                gbFieldCommon.DataContext = currentObject;
            }
            else
            {
                gbFieldCommon.DataContext = card;
            }
            RefreshProperties();
            DrawCard(); // чтобы обновить рамку вокруг выбранного объекта 
        }
        private void dpDesignFront_MouseMove(object sender, MouseEventArgs e)
        {
            dpDesign_MouseMove(sender, e, SideType.Front);
        }
        private void dpDesignBack_MouseMove(object sender, MouseEventArgs e)
        {
            dpDesign_MouseMove(sender, e, SideType.Back);
        }
        private void dpDesign_MouseMove(object sender, MouseEventArgs e, SideType side)
        {
            int X = (int)e.GetPosition((DrawPanel)sender).X;
            int Y = (int)e.GetPosition((DrawPanel)sender).Y;
            string dm = (Params.UseMetric) ? "mm" : String.Format("{0}", "\x22");
            if (Card.ScreenXToClient(X) >= 0.0 && Card.ScreenYToClient(Y, side) >= 0.0 && Card.ScreenXToClient(X) <= Card.Width && Card.ScreenYToClient(Y, side) <= Card.Height)
                sbiPos.Content = String.Format("X: {0:F2}{2} Y: {1:F2}{2}", Card.ScreenXToClient(X), Card.ScreenYToClient(Y, side), dm);
            DesignObject dsO = null;
            if (e.LeftButton == MouseButtonState.Released)
                ((DrawPanel)sender).Cursor = Cursors.Arrow;
            if (e.LeftButton == MouseButtonState.Released && card.IsMouseOver(ref dsO, X, Y, side))
            {
#region изменение курсора
                switch (dsO.Misc)
                {
                    case 0:
                        ((DrawPanel)sender).Cursor = Cursors.Arrow;
                        break;
                    case 1:
                    case 5:
                        ((DrawPanel)sender).Cursor = Cursors.SizeWE;
                        break;
                    case 3:
                    case 7:
                        ((DrawPanel)sender).Cursor = Cursors.SizeNS;
                        break;
                    case 2:
                    case 6:
                        ((DrawPanel)sender).Cursor = Cursors.SizeNWSE;
                        break;
                    case 4:
                    case 8:
                        ((DrawPanel)sender).Cursor = Cursors.SizeNESW;
                        break;
                    case 9:
                        ((DrawPanel)sender).Cursor = Cursors.SizeWE;
                        break;
                    case 10:
                        ((DrawPanel)sender).Cursor = Cursors.SizeNS;
                        break;
                }
#endregion
            }
            sbiObjectName.Content = (dsO == null) ? "" : dsO.Name;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
#region добавление поля изображения, текстового, штриха, без покрытия
                if ((currentTool == ObjectType.ImageField || currentTool == ObjectType.TextField ||
                        currentTool == ObjectType.Barcode || currentTool == ObjectType.TopCoat) && prevX != X && prevY != Y)
                {
                    dsO = null;
                    switch (currentTool)
                    {
                        case ObjectType.ImageField:
                            bImageField.IsChecked = false;
                            dsO = new ImageField();
                            break;
                        case ObjectType.TextField:
                            bTextField.IsChecked = false;
                            dsO = new TextField();
                            break;
                        case ObjectType.Barcode:
                            bBarcodeField.IsChecked = false;
                            dsO = new Barcode();
                            break;
                        case ObjectType.TopCoat:
                            bTopcoatField.IsChecked = false;
                            dsO = new TopCoat();
                            break;
                    }
                    if (dsO != null)
                    {
                        dsO.Side = side;
                        dsO.X = Card.ScreenXToClient(prevX);
                        dsO.Y = Card.ScreenYToClient(prevY, side);
                        dsO.ID = card.GetNextID();
                        dsO.Name = card.GetName(currentTool);
                        if (X >= prevX && Y >= prevY)
                            dsO.Misc = 6;
                        if (X <= prevX && Y >= prevY)
                            dsO.Misc = 8;
                        if (X >= prevX && Y <= prevY)
                            dsO.Misc = 4;
                        if (X <= prevX && Y <= prevY)
                            dsO.Misc = 2;
                        if (dsO.OType == ObjectType.TextField)
                            ((TextField)dsO).Shablon = dsO.Name;
                        dsO.RedrawCard += DrawCard;
                        card.objects.Add(dsO);
                        RefreshList();
                        currentObject = dsO;
                        currentTool = ObjectType.None;
                    }
                }
#endregion
                if (currentObject == null)
                    return;
#region Движение и изменение размеров
                switch (currentObject.OType)
                {
#region Линия эмбоссирования
                    case ObjectType.EmbossLine:
                        if ((int)currentObject.Misc == 10 && Y != prevY)
                        {
                            currentObject.Y = Card.ScreenYToClient(Y, side);
                            prevY = Y;
                        }
                        if ((int)currentObject.Misc == 9 && X != prevX)
                        {
                            currentObject.X = Card.ScreenXToClient(X);
                            if (!((EmbossLine)currentObject).FirstLine)
                            {
                                card.SetEmbossLineX(currentObject.X);
                                if (!card.CheckEmbossLineX(currentObject.X))
                                    card.SetEmbossLineX(Card.ScreenXToClient(prevX));
                                else
                                    prevX = X;
                            }
                            else
                                prevX = X;
                        }
                        break;
#endregion
#region Поле эмбоссирования
                    case ObjectType.EmbossText:
                        // digits мы обнуляем по MouseDown и выставляем по MouseUp
                        EmbossText et = (EmbossText)currentObject;
                        double ds = Card.ScreenToClient(X - prevX);
                        int ps = 0;
                        if (ds > 0)
                            ps = (int)Math.Floor(ds / Card.FontDis(et.Parent.Font));
                        else
                            ps = (int)Math.Ceiling(ds / Card.FontDis(et.Parent.Font));
                        sbiDesign.Content = String.Format("ds={0:0.000},ps={1:0.000},X={2:000},prevX={3:000}", ds, ps, X, prevX);
                        //движение
                        if (currentObject.Misc == 0)
                        {
                            if (et.Parent.Registered)
                                ps = et.Position + ps;
                            else
                                ps = et.Position - ps; 
                            if (ps < 0)
                                ps = 0;
                            if (et.Position != ps && et.Parent.CheckDigit(ps, et.Shablon.Length))
                            {
                                et.Position = ps;
                                prevX = X;
                            }
                        }
                        //справа
                        if (currentObject.Misc == 5)
                        {
                            if (!et.Parent.Registered)
                                ps = -ps;
                            if (ps > 0 && et.Parent.CheckDigit(et.Position, et.Shablon.Length + ps))
                            {
                                et.Shablon = String.Format("{0}#", et.Shablon);
                                prevX = X;
                            }
                            if (ps < 0 && et.Shablon.Length + ps > 0)
                            {
                                et.Shablon = et.Shablon.Remove(et.Shablon.Length - 1);
                                prevX = X;
                            }
                        }
                        //слева
                        if (currentObject.Misc == 1)
                        {
                            if (!et.Parent.Registered)
                                ps = -ps;
                            if (ps > 0 && et.Shablon.Length - ps > 0)
                            {
                                et.Position += ps;
                                et.Shablon = et.Shablon.Remove(et.Shablon.Length - 1);
                                prevX = X;
                            }
                            if (ps < 0 && et.Parent.CheckDigit(et.Position + ps, et.Shablon.Length - ps))
                            {
                                et.Position += ps;
                                et.Shablon = String.Format("{0}#", et.Shablon);
                                prevX = X;
                            }
                        }
                        currentObject = null; // нужно, чтобы при выставлении шаблона и позиции не зажигались digits
                        tbEmbossTextShablon.Text = et.Shablon;
                        tbEmbossTextPosition.Text = et.Position.ToString();
                        currentObject = et;
                        lEmbossTextLength.SetValue(Label.ContentProperty, String.Format("{0} {1}", this.FindResource("EmbossText_Length"), ((EmbossText)currentObject).Shablon.Length));
                        break;
#endregion
#region Поле эмбоссирования 2 
                    case (ObjectType.EmbossText2):
                        EmbossText2 et2 = (EmbossText2)currentObject;
                        double ds2 = Card.ScreenToClient(X - prevX);
                        int ps2 = 0;
                        if (ds2 > 0)
                            ps2 = (int)Math.Floor(ds2 / Card.FontDis(et2.Font));
                        else
                            ps2 = (int)Math.Ceiling(ds2 / Card.FontDis(et2.Font));

                        if ((int)currentObject.Misc == 0 && (prevX != X || prevY != Y))
                        {
                            double dx = currentObject.X - Card.ScreenToClient(prevX - X);
                            double dy = currentObject.Y - Card.ScreenToClient(Y - prevY);
                            if (dx >= 0  && dx + currentObject.Width <= Card.Width)
                                currentObject.X = dx;
                            if (dy >= 0 && dy + currentObject.Height <= Card.Height)
                                currentObject.Y = dy;
                            prevX = X; prevY = Y;
                        }
                        //справа
                        if (currentObject.Misc == 5)
                        {
                            if (ps2 > 0 && (et2.Width + Card.FontDis(et2.Font) * ps2) < Card.Width)
                            {
                                et2.Shablon = String.Format("{0}X", et2.Shablon);
                                prevX = X;
                            }
                            if (ps2 < 0 && et2.Shablon.Length + ps2 > 0)
                            {
                                et2.Shablon = et2.Shablon.Remove(et2.Shablon.Length - 1);
                                prevX = X;
                            }
                        }
                        //слева
                        if (currentObject.Misc == 1)
                        {
                            if (ps2 > 0 && et2.Shablon.Length - ps2 > 0)
                            {
                                et2.X = Card.ScreenXToClient(X);
                                et2.Shablon = et2.Shablon.Remove(et2.Shablon.Length - 1);
                                prevX = X;
                            }
                            if (ps2 < 0)
                            {
                                et2.X = Card.ScreenXToClient(X);
                                et2.Shablon = String.Format("{0}X", et2.Shablon);
                                prevX = X;
                            }
                        }


                        //if (currentObject.Misc == 1)
                        //{
                        //    if (ps > 0 && et.Shablon.Length - ps > 0)
                        //    {
                        //        et.Position += ps;
                        //        et.Shablon = et.Shablon.Remove(et.Shablon.Length - 1);
                        //        prevX = X;
                        //    }
                        //    if (ps < 0 && et.Parent.CheckDigit(et.Position + ps, et.Shablon.Length - ps))
                        //    {
                        //        et.Position += ps;
                        //        et.Shablon = String.Format("{0}#", et.Shablon);
                        //        prevX = X;
                        //    }
                        //}



                        //if ((int)currentObject.Misc == 1)
                        //{
                        //    if (ps2 < 0)
                        //    {
                        //        for (int t = 0; t < -ps2; t++)
                        //        {
                        //            double tm = ((EmbossText2)currentObject).X;
                        //            ((EmbossText2)currentObject).X -= Params.FontDis(((EmbossText2)currentObject).Font);
                        //            if (tm != ((EmbossText2)currentObject).X)
                        //                ((EmbossText2)currentObject).Shablon += "X";
                        //        }
                        //        RefreshProperties();
                        //        Redraw(currentObject.Side);
                        //        prevX = e.X;
                        //    }
                        //    if (ps2 > 0 && ((EmbossText2)currentObject).Shablon.Length - ps2 > 0)
                        //    {
                        //        ((EmbossText2)currentObject).Shablon = ((EmbossText2)currentObject).Shablon.Remove(0, ps2);
                        //        ((EmbossText2)currentObject).X += Params.FontDis(((EmbossText2)currentObject).Font);
                        //        RefreshProperties();
                        //        Redraw(currentObject.Side);
                        //        prevX = e.X;
                        //    }
                        //}

                        currentObject = et2;
                        //lEmbossTextLength.SetValue(Label.ContentProperty, String.Format("{0} {1}", this.FindResource("EmbossText_Length"), ((EmbossText)currentObject).Shablon.Length));
                        
                        break;
#endregion
#region тестовое, картинки, без покрытия, штрихкода
                    case (ObjectType.TextField):
                    case (ObjectType.ImageField):
                    case (ObjectType.Barcode):
                    case (ObjectType.TopCoat):
                        // слева
                        if ((int)currentObject.Misc == 1 && prevX != X)
                        {
                            int nx = (X < Card.Left()) ? Card.Left() : X;
                            currentObject.Width -= Card.ScreenToClient(nx - prevX);
                            if (currentObject.Width <= 0)
                            {
                                currentObject.Misc = 5; // переход направо
                                currentObject.Width *= -1;
                            }
                            else
                                currentObject.X = Card.ScreenXToClient(nx);
                            prevX = nx;
                        }
                        // справа
                        if ((int)currentObject.Misc == 5 && prevX != X)
                        {
                            currentObject.Width = Card.ScreenXToClient(X) - currentObject.X;
                            if (currentObject.Width <= 0)
                            {
                                currentObject.Misc = 1; // переход налево
                                currentObject.Width *= -1;
                            }
                            prevX = X;
                        }
                        // снизу
                        if ((int)currentObject.Misc == 7 && prevY != Y)
                        {
                            int ny = (Y > Card.Bottom(side)) ? Card.Bottom(side) : Y;
                            currentObject.Height += Card.ScreenToClient(ny - prevY);
                            if (currentObject.Height < 0)
                            {
                                currentObject.Misc = 3;	// переход наверх
                                currentObject.Height *= -1;
                            }
                            else
                                currentObject.Y = Card.ScreenYToClient(ny, side);
                            prevY = ny;
                        }
                        // сверху
                        if ((int)currentObject.Misc == 3 && prevY != Y)
                        {
                            currentObject.Height = Card.ScreenYToClient(Y, side) - currentObject.Y;
                            if (currentObject.Height <= 0)
                            {
                                currentObject.Misc = 7; // переход вниз
                                currentObject.Height *= -1;
                            }
                            prevY = Y;
                        }
                        // юго-восток - смесь снизу и справа
                        if ((int)currentObject.Misc == 6 && (prevX != X || prevY != Y))
                        {
                            currentObject.Width = Card.ScreenXToClient(X) - currentObject.X;
                            if (currentObject.Width <= 0)
                            {
                                currentObject.Misc = 8;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNESW;
                                currentObject.Width *= -1;
                            }
                            int ny = (Y > Card.Bottom(side)) ? Card.Bottom(side) : Y;
                            currentObject.Height += Card.ScreenToClient(ny - prevY);
                            if (currentObject.Height <= 0)
                            {
                                currentObject.Misc = 4;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNESW;
                                currentObject.Height *= -1;
                            }
                            else
                                currentObject.Y = Card.ScreenYToClient(ny, side);
                            prevX = X; prevY = ny;
                        }
                        // юго-запад - смесь снизу и слева
                        if ((int)currentObject.Misc == 8 && (prevX != X || prevY != Y))
                        {
                            int nx = (X < Card.Left()) ? Card.Left() : X;
                            currentObject.Width -= Card.ScreenToClient(nx - prevX);
                            if (currentObject.Width <= 0)
                            {
                                currentObject.Misc = 6;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNWSE;
                                currentObject.Width *= -1;
                            }
                            else
                                currentObject.X = Card.ScreenXToClient(nx);
                            int ny = (Y > Card.Bottom(side)) ? Card.Bottom(side) : Y;
                            currentObject.Height += Card.ScreenToClient(ny - prevY);
                            if (currentObject.Height <= 0)
                            {
                                currentObject.Misc = 2;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNWSE;
                                currentObject.Height *= -1;
                            }
                            else
                                currentObject.Y = Card.ScreenYToClient(ny, side);
                            prevX = nx; prevY = ny;
                        }
                        //северо-восток - смесь сверху с справа
                        if ((int)currentObject.Misc == 4 && (prevX != X || prevY != Y))
                        {
                            currentObject.Width = Card.ScreenXToClient(X) - currentObject.X;
                            if (currentObject.Width <= 0)
                            {
                                currentObject.Misc = 2;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNWSE;
                                currentObject.Width *= -1;
                            }
                            currentObject.Height = Card.ScreenYToClient(Y, side) - currentObject.Y;
                            if (currentObject.Height <= 0)
                            {
                                currentObject.Misc = 6;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNWSE;
                                currentObject.Height *= -1;
                            }
                            prevX = X; prevY = Y;
                        }
                        // северо-запад - смесь сверху и слева
                        if ((int)currentObject.Misc == 2 && (prevX != X || prevY != Y))
                        {
                            int nx = (X < Card.Left()) ? Card.Left() : X;
                            currentObject.Width -= Card.ScreenToClient(nx - prevX);
                            if (currentObject.Width <= 0)
                            {
                                currentObject.Misc = 4;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNESW;
                                currentObject.Width *= -1;
                            }
                            else
                                currentObject.X = Card.ScreenXToClient(nx);
                            currentObject.Height = Card.ScreenYToClient(Y, side) - currentObject.Y;
                            if (currentObject.Height <= 0)
                            {
                                currentObject.Misc = 8;
                                ((DrawPanel)sender).Cursor = Cursors.SizeNESW;
                                currentObject.Height *= -1;
                            }
                            prevX = nx; prevY = Y;
                        }
                        // все текстовое поле
                        if ((int)currentObject.Misc == 0 && (prevX != X || prevY != Y))
                        {
                            double dx = currentObject.X - Card.ScreenToClient(prevX - X);
                            double dy = currentObject.Y - Card.ScreenToClient(Y - prevY);
                            if (dx >= 0 && dx + currentObject.Width <= Card.Width)
                                currentObject.X = dx;
                            if (dy >= 0 && dy + currentObject.Height <= Card.Height)
                                currentObject.Y = dy;
                            prevX = X; prevY = Y;
                        }
                        break;
#endregion
                }
#endregion
            }
            //вместо refreshproperties обновляю только координаты (остальное не нужно)
            //RefreshProperties();
            if (currentObject != null)
            {
//                tbFieldX.Text = currentObject.X.ToString("F3"); tbFieldY.Text = currentObject.Y.ToString("F3");
            }
            else
            {
//                tbFieldX.Text = "0.0"; tbFieldY.Text = "0.0";
            }
            DrawCard(side);
        }
        private void dpDesignFront_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dpDesign_MouseDown(sender, e, SideType.Front);
        }
        private void dpDesignBack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dpDesign_MouseDown(sender, e, SideType.Back);
        }
        private void dpDesign_MouseDown(object sender, MouseButtonEventArgs e, SideType side)
        {
            int X = (int)e.GetPosition((DrawPanel)sender).X;
            int Y = (int)e.GetPosition((DrawPanel)sender).Y;
            prevX = X; prevY = Y;

            if (card.IsMouseOver(ref currentObject, X, Y, side))
            {
                if (currentObject.OType == ObjectType.EmbossLine && currentTool == ObjectType.EmbossText)
                {
                    EmbossText et = new EmbossText();
                    et.Parent = (EmbossLine)currentObject;
                    et.Shablon = "#";
                    et.ID = card.GetNextID();
                    et.Position = ((EmbossLine)currentObject).GetFreeDigit();
                    et.Side = ((EmbossLine)currentObject).Side;
                    et.Name = card.GetName(ObjectType.EmbossText);
                    if (et.Position >= 0) //если есть куда вставлять
                    {
                        ((EmbossLine)currentObject).SetDigits(et.Position, et.Shablon.Length, true);
                        card.objects.Add(et);
                    }
                    RefreshList();
                    ResetToolBar();
                }
                card.SelectedID = currentObject.ID;
                for (int i = 0; i < lbFields.Items.Count; i++)
                {
                    if ((int)((ListBoxItem)lbFields.Items[i]).Tag == currentObject.ID)
                    {
                        ((ListBoxItem)lbFields.Items[i]).IsSelected = true;
                        break;
                    }
                }
                // если мы нажали на поле эмбоссирования, то для возможности движения обнуляем digits в его позициях
                if (currentObject.OType == ObjectType.EmbossText)
                    ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, false);
            }
            else
                ((ListBoxItem)lbFields.Items[0]).IsSelected = true;
            //RefreshProperties();
            DrawCard(side);
        }
        private void dpDesignFront_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dpDesign_MouseUp(sender, e, SideType.Front);
        }
        private void dpDesignBack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dpDesign_MouseUp(sender, e, SideType.Back);
        }
        private void dpDesign_MouseUp(object sender, MouseButtonEventArgs e, SideType side)
        {
            prevX = 0; prevY = 0;
            if (currentObject != null)
            {
                for (int i = 0; i < lbFields.Items.Count; i++)
                {
                    if ((int)((ListBoxItem)lbFields.Items[i]).Tag == currentObject.ID)
                    {
                        ((ListBoxItem)lbFields.Items[i]).IsSelected = true;
                        break;
                    }
                }
                if (currentObject.OType == ObjectType.EmbossText)
                    ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, true);
            }
            else
                ((ListBoxItem)lbFields.Items[0]).IsSelected = true;
        }

        private void bDBIn_Click(object sender, RoutedEventArgs e)
        {
            DbSelect winDB = new DbSelect();
            winDB.SetDatabase(card.DbIn);
            winDB.Owner = this;
            var res = winDB.ShowDialog();
            if (res != null && res.Value)
            {
                card.DbIn = winDB.GetDatabase();
                tbDbIn.Text = card.DbIn.ToString();
                currentRecord = 0;
            }
        }
        private void tbEmbossTextShablon_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.EmbossText)
                return;
            ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, false);
            if (((EmbossText)currentObject).Parent.CheckDigit(((EmbossText)currentObject).Position, tbEmbossTextShablon.Text.Length))
                ((EmbossText)currentObject).Shablon = tbEmbossTextShablon.Text;
            ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, true);
            DrawCard(currentObject.Side);
        }

        private void bEmbossTextIn_Click(object sender, RoutedEventArgs e)
        {
            bFieldIn_Click(sender);
        }
        private void bFieldIn_Click(object sender)
        {
            if (currentObject == null)
                return;
            int magtrack = 0;
            if (currentObject.OType == ObjectType.MagStripe)
                magtrack = Convert.ToInt32(((Control)sender).Tag);
            bool isComposite = (currentObject.InType == InTypes.Composite);
            if (currentObject.OType == ObjectType.MagStripe)
                isComposite = (((MagStripe)currentObject).InTypeM[magtrack - 1] == InTypes.Composite);
            if (isComposite)
            {
                CompositeForm cf = new CompositeForm();
                cf.Owner = this;
                cf.LoadFields(card, currentObject, magtrack);
                var res = cf.ShowDialog();
                if (res != null && res.Value)
                {
                    if (magtrack > 0)
                        ((MagStripe)currentObject).InDataM[magtrack - 1] = cf.GetCurrent();
                    else
                        currentObject.InData = cf.GetCurrent();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string str = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "Designs")))
            {
                
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "Designs"));
            }
            cbDevice.Items.Add(new Para((int)DeviceType.None, (string)this.FindResource("NotDefined")));
            cbDevice.Items.Add(new Para((int)DeviceType.Simulator, (string)this.FindResource("Simulator")));
            if (Params.CheckClue_Adv())
            {
                //if (Params.CheckDevice(DeviceType.DC450))
                //    cbDevice.Items.Add(new Para((int)DeviceType.DC450, "Datacard 450"));
                //if (Params.CheckDevice(DeviceType.DC150))
                //    cbDevice.Items.Add(new Para((int)DeviceType.DC150, "Datacard 150"));
                if (Params.CheckDevice(DeviceType.CD))
                    cbDevice.Items.Add(new Para((int)DeviceType.CD, (string)this.FindResource("CDPrinter")));
                if (Params.CheckDevice(DeviceType.CE))
                    cbDevice.Items.Add(new Para((int)DeviceType.CE, (string)this.FindResource("CEEmbosser")));
                if (Params.CheckDevice(DeviceType.SR))
                    cbDevice.Items.Add(new Para((int)DeviceType.SR, (string)this.FindResource("SRPrinter")));
            }
            else
            {
                MessageBox.Show((string)this.FindResource("Message_NoHasp"));
            }
            borderDevice.Height = 0;
            borderPrinter.Height = 0;
            borderSmart.Height = 0;
        }

        private void bDeviceProperies_Click(object sender, RoutedEventArgs e)
        {
            Para dev = (Para)cbDevice.SelectedValue;
            if (dev == null || card.device == null)
                return;

            DoubleAnimation da = new DoubleAnimation();
            if (!isBorderDevice)
            {
                if (DeviceClass.IsEmbosser(card.device.DeviceType) && !DeviceClass.IsXPS((card.device.DeviceType)))
                {
                    cbComPort.Items.Clear();
                    string[] ports = SerialPort.GetPortNames();
                    foreach (string str in ports)
                        cbComPort.Items.Add(str);
                    cbComPort.Text = ((Devices.Embosser)card.device).PortName;
                    cbComBaudrate.Text = ((Devices.Embosser)card.device).BaudRate.ToString();
                    cbComParity.Text = ((Devices.Embosser)card.device).PortParity.ToString();
                    cbComBit.Text = ((Devices.Embosser)card.device).DataBits.ToString();
                    if (card.device.DeviceType == DeviceType.DC450)
                    {
                        cb450Speed.IsEnabled = true;
                        cb450Speed.SelectedIndex = (int)((Devices.DC450)card.device).Speed;
                        cb450Indent.IsEnabled = true;
                        cb450Indent.SelectedIndex = (int)((Devices.DC450)card.device).DIndent;
                        tb450DopOffset.IsEnabled = true;
                        tb450DopOffset.Text = ((Devices.DC450)card.device).DopOffset.ToString("0.000");
                    }
                    else
                    {
                        cb450Speed.IsEnabled = false;
                        cb450Speed.SelectedIndex = 0;
                        cb450Indent.IsEnabled = false;
                        cb450Indent.SelectedIndex = 0;
                    }
                    switch(((Devices.Embosser)card.device).PortStopBit)
                    {
                        case (StopBits.One):
                        default:
                            cbComStopbits.Text = "1"; ;
                            break;
                        case (StopBits.OnePointFive):
                            cbComStopbits.Text = "1.5";
                            break;
                        case (StopBits.Two):
                            cbComStopbits.Text = "2";
                            break;
                    }
                    da.To = 270;
                    da.Duration = TimeSpan.FromSeconds(1);
                    borderDevice.BeginAnimation(Border.HeightProperty, da);
                    da.To = 340;
                    da.Duration = TimeSpan.FromSeconds(1);
                    gbCard.BeginAnimation(GroupBox.HeightProperty, da);
                    isBorderDevice = true;
                }
                if (Devices.DeviceClass.IsPrinter(card.device.DeviceType))
                {
                    cbPrinterDrivers.Items.Clear();
                    System.Drawing.Printing.PrinterSettings.StringCollection printerNames = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
                    foreach (string printerName in printerNames)
                        cbPrinterDrivers.Items.Add(printerName);
                    cbPrinterDrivers.Text = ((Devices.PrinterClass)card.device).printerName;
                    da.To = 180;
                    da.Duration = TimeSpan.FromSeconds(1);
                    borderPrinter.BeginAnimation(Border.HeightProperty, da);
                    da.To = 250;
                    da.Duration = TimeSpan.FromSeconds(1);
                    gbCard.BeginAnimation(GroupBox.HeightProperty, da);
                    isBorderDevice = true;
                }
            }
            else
            {
                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(1);
                if (Devices.DeviceClass.IsEmbosser(card.device.DeviceType))
                    borderDevice.BeginAnimation(Border.HeightProperty, da);
                if (Devices.DeviceClass.IsXPS(card.device.DeviceType))
                    borderPrinter.BeginAnimation(Border.HeightProperty, da);
                da.To = 100;
                da.Duration = TimeSpan.FromSeconds(1);
                gbCard.BeginAnimation(GroupBox.HeightProperty, da);
                isBorderDevice = false;
            }
        }

        private void cbDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Para dev = (Para)cbDevice.SelectedValue;
            if (dev == null)
            {
                card.device = null;
                return;
            }
            if (card.device != null && DeviceClass.IsPrinter(card.device.DeviceType))
                borderPrinter.DataContext = (PrinterClass)card.device;
            if (card.device != null && dev.ID == (int)card.device.DeviceType)
                return;
            switch (dev.ID)
            {
                case (int)DeviceType.DC450:
                    card.device = new DC450();
                    break;
                case (int)DeviceType.DC150:
                    card.device = new DC150();
                    break;
                case (int)DeviceType.Simulator:
                    card.device = new Simulator();
                    break;
                case (int)DeviceType.CD:
                    card.device = new XPSPrinter();
                    card.device.DeviceType = DeviceType.CD;
                    borderPrinter.DataContext = (PrinterClass)card.device;
                    break;
                case (int)DeviceType.CE:
                    card.device = new XPSPrinter();
                    card.device.DeviceType = DeviceType.CE;
                    borderPrinter.DataContext = (PrinterClass)card.device;
                    break;
                case (int)DeviceType.SR:
                    card.device = new SRPrinter();
                    borderPrinter.DataContext = (PrinterClass)card.device;
                    break;
                default:
                    card.device = null;
                    break;
            }
        }

        private void cbComBaudrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Devices.DeviceClass.IsEmbosser(card.device.DeviceType))
                return;
            if (cbComBaudrate.SelectedIndex < 0)
                return;
            ((Devices.Embosser)card.device).BaudRate = Int32.Parse(((ComboBoxItem)cbComBaudrate.SelectedItem).Content.ToString());
        }

        private void cbComBit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Devices.DeviceClass.IsEmbosser(card.device.DeviceType))
                return;
            if (cbComBit.SelectedIndex < 0)
                return;
            ((Devices.Embosser)card.device).DataBits = Int32.Parse(((ComboBoxItem)cbComBit.SelectedItem).Content.ToString());
        }

        private void cbComParity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Devices.DeviceClass.IsEmbosser(card.device.DeviceType))
                return;
            if (cbComParity.SelectedIndex < 0)
                return;
            switch (cbComParity.SelectedIndex)
            {
                case 0:
                    ((Devices.Embosser)card.device).PortParity = Parity.Even;
                    break;
                case 1:
                    ((Devices.Embosser)card.device).PortParity = Parity.Odd;
                    break;
                case 2:
                default:
                    ((Devices.Embosser)card.device).PortParity = Parity.None;
                    break;
                case 3:
                    ((Devices.Embosser)card.device).PortParity = Parity.Mark;
                    break;
                case 4:
                    ((Devices.Embosser)card.device).PortParity = Parity.Space;
                    break;
            }
            
        }

        private void cbComStopbits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Devices.DeviceClass.IsEmbosser(card.device.DeviceType))
                return;
            if (cbComStopbits.SelectedIndex < 0)
                return;
            switch (cbComStopbits.SelectedIndex)
            {
                case 0:
                default:
                    ((Devices.Embosser)card.device).PortStopBit = StopBits.One;
                    break;
                case 1:
                    ((Devices.Embosser)card.device).PortStopBit = StopBits.OnePointFive;
                    break;
                case 2:
                    ((Devices.Embosser)card.device).PortStopBit = StopBits.Two;
                    break;
            }

        }

        private void cbComPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Devices.DeviceClass.IsEmbosser(card.device.DeviceType))
                return;
            if (cbComPort.SelectedIndex < 0)
                return;
            ((Devices.Embosser)card.device).PortName = cbComPort.SelectedValue.ToString();
        }

        private void dpDesignFront_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawCard();
        }

        private void tbEmbossTextPosition_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.EmbossText)
                return;
            try {
                int i = Convert.ToInt32(tbEmbossTextPosition.Text);
                ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, false);
                if (((EmbossText)currentObject).Parent.CheckDigit(i, ((EmbossText)currentObject).Shablon.Length))
                    ((EmbossText)currentObject).Position = i;
                ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, true);
            }
            catch
            {
                ((EmbossText)currentObject).Position = 0;
            }
            DrawCard(currentObject.Side);
        }

        private void cbEmbossText_Align_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.EmbossText)
                return;
            ((EmbossText)currentObject).Align = Convert.ToInt32(((ComboBoxItem)cbEmbossText_Align.SelectedItem).Tag) == 1 ? EmbossAlign.Left : EmbossAlign.Right;
        }

        //private void cbEmbossText_In_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    fieldDataInChanged((ComboBox)sender);
        //}
        //private void cbMagStripe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    fieldDataInChanged((ComboBox)sender);
        //}

        //private void cbSmartIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    fieldDataInChanged((ComboBox)sender);
        //}

        private void bSmartConfig_Click(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField)
                return;            
            if (!isBorderSmart)
            {
                MoveSmartConfig();
            }
            else
            {
                DoubleAnimation da = new DoubleAnimation();
                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(1);
                borderSmart.BeginAnimation(Border.HeightProperty, da);
                da.To = 100;
                da.Duration = TimeSpan.FromSeconds(1);
                gbSmart.BeginAnimation(GroupBox.HeightProperty, da);
                isBorderSmart = false;
            }
        }
        private void MoveSmartConfig()
        {
            DoubleAnimation da = new DoubleAnimation();
            int newHeight = 225;
            if (((SmartField)currentObject).SModule == null)
                newHeight = 60;
            else
            {
                switch (((SmartField)currentObject).SModule.SType)
                {
                    case SmartModule.SmartType.OstcardStandard:
                        newHeight = 315;
                        //if (card.device != null && Devices.DeviceClass.IsXPS(card.device.DeviceType))
                        //{
                        //    card.device.StartJob();
                        //    spOneWire.Visibility = (((Devices.iXPS)card.device).IsOneWire()) ? Visibility.Visible : Visibility.Hidden;
                        //    spDoubleWire.Visibility = (((Devices.iXPS)card.device).IsOneWire()) ? Visibility.Hidden : Visibility.Visible;
                        //    card.device.StopJob();
                        //}
                        //else
                        //{
                        //    spOneWire.Visibility = Visibility.Hidden;
                        //    spDoubleWire.Visibility = Visibility.Visible;
                        //}
                        break;
                    case SmartModule.SmartType.None:
                    default:
                        newHeight = 60;
                        break;
                }
            }
            da.To = newHeight;
            da.Duration = TimeSpan.FromSeconds(1);
            borderSmart.BeginAnimation(Border.HeightProperty, da);
            da.To = newHeight + 75;
            da.Duration = TimeSpan.FromSeconds(1);
            gbSmart.BeginAnimation(GroupBox.HeightProperty, da);
            isBorderSmart = true;
        }
        private void cbSmartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField)// && (int)((SmartField)currentObject).SModule.SType == tp)
                return;
            if (cbSmartType.SelectedItem == null)
                return;
            int tp = Convert.ToInt32(((ComboBoxItem)cbSmartType.SelectedItem).Tag);
            if (((SmartField)currentObject).SModule != null && (int)((SmartField)currentObject).SModule.SType == tp)
                return;
            switch (tp)
            {
                case (int)SmartModule.SmartType.OstcardStandard:
                    ((SmartField)currentObject).SModule = new SmartModule.OstcardStandard();
                    //lSmartTitle.SetValue(Label.ContentProperty, String.Format("Ostcard: {0} {1}", this.FindResource("Smart_Script"), ((SmartModule.OstcardStandard)(((SmartField)currentObject).SModule)).Script));
                    FillReaders();
                    gbSmartConfig.DataContext = ((SmartField)currentObject).SModule;
                    cbSmartType.DataContext = (SmartField)currentObject;
                    gbSmartOstcardStandart.DataContext = ((SmartField)currentObject).SModule;
                    break;
                case (int)SmartModule.SmartType.None:
                default:
                    ((SmartField)currentObject).SModule = null;
                    gbSmartConfig.DataContext = null;
                    cbSmartType.DataContext = null;
                    gbSmartOstcardStandart.DataContext = null;
                    break;
            }
            ((SmartField)currentObject).RaisePropertyChanged("SmartTitle");
            MoveSmartConfig();
        }
        private void FillReaders()
        {
            string[] readers = null;
            try
            {
                readers = HugeLib.SCard.SmartClass.ListReaders();
            }
            catch
            {
                readers = null;
            }
            cbReaders.Items.Clear();
            if (readers != null)
                foreach (string str in readers)
                    cbReaders.Items.Add(str);
        }
        private void tbSmartTimeout_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if (currentObject == null || currentObject.OType != ObjectType.SmartField || ((SmartField)currentObject).SModule == null)
            //    return;
            //int last = ((SmartField)currentObject).SModule.Timeout;
            //try {
            //    ((SmartField)currentObject).SModule.Timeout = Convert.ToInt32(tbSmartTimeout.Text);
            //}
            //catch {
            //    ((SmartField)currentObject).SModule.Timeout = last;
            //}
        }

        private void bSmartPath_Click(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField || ((SmartField)currentObject).SModule == null)
                return;
            OpenFileDialog oFile = new OpenFileDialog();
            oFile.Filter = $"{(string)this.FindResource("DllFiles")}|*.dll|{(string)this.FindResource("AllFiles")}|*.*";
            if (oFile.ShowDialog() == true)
            {
                ((SmartField)currentObject).SModule.Path = oFile.FileName;
                //tbDllPath.Text = oFile.FileName;
            }
        }

        private void tbHSIp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField || ((SmartField)currentObject).SModule == null)
                return;
            if (((SmartField)currentObject).SModule.SType != SmartModule.SmartType.OstcardStandard)
                return;
            ((SmartModule.OstcardStandard)((SmartField)currentObject).SModule).HSIP = tbHSIp.Text;
        }

        private void tbHSPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField || ((SmartField)currentObject).SModule == null)
                return;
            if (((SmartField)currentObject).SModule.SType != SmartModule.SmartType.OstcardStandard)
                return;
            try {
                ((SmartModule.OstcardStandard)((SmartField)currentObject).SModule).HSPort = Convert.ToInt32(tbHSPort.Text);
            }
            catch {
            }
        }

        private void tbScript_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField || ((SmartField)currentObject).SModule == null)
                return;
            if (((SmartField)currentObject).SModule.SType != SmartModule.SmartType.OstcardStandard)
                return;
            ((SmartField)currentObject).RaisePropertyChanged("SmartTitle");
            //((SmartModule.OstcardStandard)((SmartField)currentObject).SModule).Script = tbScript.Text;
            //lSmartTitle.SetValue(Label.ContentProperty, String.Format("Ostcard: {0} {1}", this.FindResource("Smart_Script"), ((SmartModule.OstcardStandard)(((SmartField)currentObject).SModule)).Script));
        }

        private void cbReaders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbReaders.SelectedValue == null)
                return;
            if (currentObject == null || currentObject.OType != ObjectType.SmartField || ((SmartField)currentObject).SModule == null)
                return;
            if (((SmartField)currentObject).SModule.SType != SmartModule.SmartType.OstcardStandard)
                return;
            ((SmartModule.OstcardStandard)((SmartField)currentObject).SModule).ReaderName = cbReaders.SelectedValue.ToString();
        }
        private void Command_SendToPrint(object sender, ExecutedRoutedEventArgs e)
        {
            if (card.device == null)
            {
                MessageBox.Show((string)this.FindResource("DeviceNotSupported"));
                return;
            }
            if (rbInRange.IsChecked == true)
            {
                currentRecord = Convert.ToInt32(tbRangeStart.Text) - 1;
                SetFieldsText();
                DrawCard();
            }
            if (card.device.HasMessage())
                card.device.eventPassMessage -= Device_eventPassMessage;
            card.device.eventPassMessage += Device_eventPassMessage;
            if (Devices.DeviceClass.IsXPS(card.device.DeviceType))
            {
                //if (((Devices.XPSPrinter)card.device).HasDrawForPrint())
                    //((Devices.XPSPrinter)card.device).drawForPrint -= FMain_drawForPrint;
                //((Devices.XPSPrinter)card.device).drawForPrint += FMain_drawForPrint;
            }
            Params.WriteLogString("=======================================");
            Params.WriteLogString("Дизайн: {0}", filename);
            // пробежимся по полям, проставим тип оборудования
            for (int i = 0; i < card.objects.Count; i++)
                card.objects[i].SetDevice(card.device.DeviceType);

            printStatus = new PrintStatus();
            printStatus.Closed += PrintStatus_Closed;
            printStatus.statusMessage += PrintStatus_statusMessage;
            printStatus.SetMessage((string)this.FindResource("TransferInProgress"));
            printStatus.SetButtonsEnable(false, false);
            printStatus.Owner = this;

            //card.device.StartJob();
            threadPrint = new System.Threading.Thread(new System.Threading.ThreadStart(BeginIssuing));
            threadPrint.SetApartmentState(ApartmentState.STA);
            threadPrint.Start();

            printStatus.ShowDialog();
        }

        private void PrintStatus_statusMessage(StatusPrintMessage message)
        {
            switch (message)
            {
                case StatusPrintMessage.Cancel:
                    printStatus.Dispatcher.Invoke(new Action(delegate () {
                        printStatus.Hide();
                        printStatus.Close();
                        printStatus = null;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    break;
                case StatusPrintMessage.Repeat:
                    threadPrint = new System.Threading.Thread(new System.Threading.ThreadStart(BeginIssuing));
                    threadPrint.Start();
                    //printStatus.Dispatcher.Invoke(new Action(SendToDevice), System.Windows.Threading.DispatcherPriority.Background);
                    //SendToDevice();
                    break;
                case StatusPrintMessage.Skip:
                    if (dataIn != null)
                        currentRecord++;
                    bool stop = !SetFieldsText();
                    if (GetRBChecked(rbSingleCard))
                        stop = true;
                    if (GetRBChecked(rbInRange))
                    {
                        if (dataIn != null)
                        {
                            if (currentRecord >= Convert.ToInt32(GetTextBox(tbRangeEnd)))
                                stop = true;
                        }
                    }
                    DrawCard();
                    threadPrint = new System.Threading.Thread(new System.Threading.ThreadStart(BeginIssuing));
                    threadPrint.Start();
                    //SendToDevice();
                    break;
            }
        }

        private void FMain_drawForPrint1(System.Drawing.Graphics graphics, SideType side)
        {
            if (card.device.DeviceType == DeviceType.CE && side == SideType.Front)
            {
                for (int i=0; i<card.objects.Count; i++)
                {
                    #region EmbossText2
                    if (card.objects[i].OType == ObjectType.EmbossText2)
                    {
                        EmbossText2 et = (EmbossText2)card.objects[i];
                        double x = (Params.UseMetric) ? et.X / 25.4 : et.X;
                        double y = (Params.UseMetric) ? et.Y / 25.4 : et.Y;
                        string txt = et.GetText().Trim().ToUpper();
                        if (et.Shablon.Length < txt.Length)
                            txt = txt.Substring(0, et.Shablon.Length);
                        bool reverse = false;
                        string sReverse = "";
                        /* раньше мы переворачивали задние индентные шрифты и FC4 и FC8
                         * На прошивке D3.17.4-6 FC4 переворачивать не надо, а FC8 надо
                         * теперь так, плюс сделал настройку в Procard.xml для любого шрифта можно принудительно сказать переворачивать или нет
                         */
                        switch (et.Font)
                        {
                            case EmbossFont.MCIndent:
                                reverse = false;
                                sReverse = HugeLib.XmlClass.GetXmlAttribute(xmlSettings, "Embosser/Font", "Name", String.Format("FC{0}", (int)et.Font), "Reverse", xnmSettings);
                                if (sReverse.ToLower() == "true")
                                    reverse = true;
                                if (sReverse.ToLower() == "false")
                                    reverse = false;
                                break;
                            case EmbossFont.MCIndentInvert:
                                reverse = true;
                                sReverse = HugeLib.XmlClass.GetXmlAttribute(xmlSettings, "Embosser/Font", "Name", String.Format("FC{0}", (int)et.Font), "Reverse", xnmSettings);
                                if (sReverse.ToLower() == "true")
                                    reverse = true;
                                if (sReverse.ToLower() == "false")
                                    reverse = false;
                                break;
                        }

                        //if (et.Font == EmbossFont.MCIndent || et.Font == EmbossFont.MCIndentInvert)
                        if (reverse)
                        {
                            txt = ReverseString(txt);
                            double wd = (txt.Length - 1) * Card.FontDis(et.Font) + Card.FontWidth(et.Font);
                            x = Card.Width - et.X - wd;
                            x = (Params.UseMetric) ? x / 25.4 : x;
                            y = Card.Height - et.Y;
                            y = (Params.UseMetric) ? y / 25.4 : y;
                        }
                        if (txt.Length > 0)
                        {
                            using (System.Drawing.Font font = new System.Drawing.Font("Arial", 8))
                            {
                                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                                {
                                    graphics.DrawString(string.Format("~EM%{0};{1:0000};{2:0000};{3}", (int)et.Font, Convert.ToInt32(x * 1000), Convert.ToInt32(y * 1000), txt), font, brush, 50, 50);
                                }
                            }
                        }
                    }
                    #endregion
                    if (card.objects[i].OType == ObjectType.TextField)
                    {
                        //card.objects[i].Draw()
                    }
                }
            }
        }
        private string ReverseString(string str)
        {
            string res = "";
            for (int i = 0; i < str.Length; i++)
                res = String.Format("{0}{1}", res, str[str.Length - i - 1]);
            return res;
        }
        private void PrintStatus_Closed(object sender, EventArgs e)
        {
            if (threadPrint == null)
                return;
            threadPrint.Abort();
            threadPrint = null;
            card.device.eventPassMessage -= Device_eventPassMessage;
        }
        private bool GetRBChecked(RadioButton rb)
        {
            return this.Dispatcher.Invoke(delegate () { return rb.IsChecked == true; }, System.Windows.Threading.DispatcherPriority.Background);
        }
        private string GetTextBox(TextBox tb)
        {
            return this.Dispatcher.Invoke(delegate () { return tb.Text; }, System.Windows.Threading.DispatcherPriority.Background);
        }
        private void Device_eventPassMessage(Devices.MessageType messageType, string message)
        {
            if (this == null)
                return;
            if (messageType == Devices.MessageType.Debug)
            {
                this.Dispatcher.Invoke(new Action(delegate () {
                    Params.WriteLogString(message);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            if (printStatus == null)
                return;
            if (messageType == Devices.MessageType.ProductionMessage)
            {
                string str = message;
                try
                {
                    str = (string)this.FindResource(message);
                }
                catch
                {
                }
                printStatus.Dispatcher.Invoke(new Action(delegate () {
                    printStatus.SetMessage(str);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            if (messageType == Devices.MessageType.CardOK)
            {
                if (dataIn != null)
                    currentRecord++;
                bool stop = !SetFieldsText();
                if (GetRBChecked(rbSingleCard))
                    stop = true;
                if (GetRBChecked(rbInRange))
                {
                    if (dataIn != null)
                    {
                        if (currentRecord >= Convert.ToInt32(GetTextBox(tbRangeEnd)))
                            stop = true;
                    }
                }
                DrawCard();
                if (!stop)
                    SendToDevice();
                else
                    printStatus.Dispatcher.Invoke(new Action(delegate () {
                        printStatus.Hide();
                        printStatus.Close();
                        printStatus = null;
                    }), System.Windows.Threading.DispatcherPriority.Background);
            }
            if (messageType == Devices.MessageType.CardError)
            {
                steps.Clear();
                card.device.RemoveCard(Devices.ResultCard.RejectCard);
                //card.device.EndCard();
                card.device.StopJob();
                string str = message;
                try
                {
                    this.Dispatcher.Invoke(new Action(delegate()
                    {
                        str = (string)this.FindResource(message);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    
                }
                catch
                {
                    str = message;
                }

                try
                {
                    printStatus?.Dispatcher.Invoke(new Action(delegate()
                    {
                        Params.WriteLogString(message);
                        printStatus?.SetMessage(str);
                        printStatus?.SetButtonsEnable(true, true);
                        try
                        {
                            threadPrint.Abort();
                        }
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            if (messageType == Devices.MessageType.CompleteStep)
            {
                if (steps.Count == 0)
                    return;
                Step st = (Step)steps.Dequeue();
                Params.WriteLogString("Завершен шаг {0}", st);
                // если чтение магнитки
                if (st == Step.ReadMag)
                {
                    // сюда надо добавить поиск в случае поиска по магнитки
                    // ....
                    // затем перезаполняем поля и пересчитываем композитные
                    SetFieldsText();
                }

                if (card.device.DeviceType == DeviceType.DC450 && ((Devices.DC450)card.device).Speed != Devices.SpeedType.Standard && st == Step.End)
                    Device_eventPassMessage(Devices.MessageType.CardOK, "");
                else
                    if (steps.Count > 0)
                        ExecuteStep((Step)steps.Peek());

                //if (steps.Count > 0)                
                //    this.Dispatcher.Invoke(new Action(delegate () {
                //        Step st = (Step)steps.Dequeue();
                //        Params.WriteLogString("Завершен шаг {0}", st);
                //        if (card.device.DeviceType == DeviceType.DC450 && ((Devices.DC450)card.device).Speed != Devices.SpeedType.Standard && st == Step.End)
                //            Device_eventPassMessage(Devices.MessageType.CardOK, "");
                //        else
                //            if (steps.Count > 0)
                //                ExecuteStep((Step)steps.Peek());

                //    }), System.Windows.Threading.DispatcherPriority.Background);
                ////else
                ////{
                ////    this.Dispatcher.Invoke(new Action(delegate () {
                ////        Params.WriteLogString("Queue is empty. Card complete");
                ////    }), System.Windows.Threading.DispatcherPriority.Background);
                ////    Device_eventPassMessage(Devices.MessageType.CardOK, "");
                ////}
            }
        }
        private void ExecuteStep(Step step)
        {
            this.Dispatcher.Invoke(new Action(delegate () { Params.WriteLogString("Старт шага {0}", step.ToString()); }), System.Windows.Threading.DispatcherPriority.Background);
            switch (step)
            {
                case Step.Start:
#region start
                    card.device.SetNowPrinting(false);
                    card.device.StartCard();
#endregion
                    break;
                case Step.Perso:
#region perso
                    System.Timers.Timer smartTimer = new System.Timers.Timer();
                    smartTimer.AutoReset = false;
                    smartTimer.Elapsed += SmartTimer_Elapsed;                    
                    bool allok = true;
                    for (int i = 0; i < card.objects.Count; i++)
                    {
                        if (card.objects[i].OType == ObjectType.SmartField)
                        {
                            smartTimer.Interval = ((SmartField)card.objects[i]).SModule.Timeout * 1000.0;
                            smartTimer.Start();
                            Device_eventPassMessage(Devices.MessageType.Debug, String.Format("Start perso: {0}", ((SmartField)card.objects[i]).SModule.Desc));
                            Device_eventPassMessage(Devices.MessageType.ProductionMessage, "StatusPerso");
                            if (((SmartField)card.objects[i]).SModule.SType == SmartModule.SmartType.OstcardStandard)
                            {
                                if (((SmartModule.OstcardStandard)((SmartField)card.objects[i]).SModule).OneWire)
                                {
                                    Device_eventPassMessage(Devices.MessageType.Debug, "One wire mode");
                                    ((SmartModule.OstcardStandard)((SmartField)card.objects[i]).SModule).ReaderName = ((Devices.XPSPrinter)card.device).printerName;
                                }
                                else
                                {
                                    //обновляем индекс ридера в списке ридеров
                                    string[] readers = null;
                                    try
                                    {
                                        readers = HugeLib.SCard.SmartClass.ListReaders();
                                    }
                                    catch
                                    {
                                        readers = null;
                                    }
                                    if (readers != null)
                                    {
                                        for (int t = 0; t < readers.Length; t++)
                                        {
                                            if (((SmartModule.OstcardStandard)(((SmartField)card.objects[i]).SModule)).ReaderName == readers[t])
                                                ((SmartModule.OstcardStandard)(((SmartField)card.objects[i]).SModule)).ReaderIndex = t;
                                        }
                                    }
                                }


                            }                            
                            ((SmartField)card.objects[i]).SModule.DefineFunction();
                            int res = -1;
                            if (((SmartField)card.objects[i]).Feedback)
                            {
                                res = ((SmartField)card.objects[i]).SModule.GetData(card.objects[i].GetText());
                                Device_eventPassMessage(Devices.MessageType.Debug, String.Format("GetData: {0} bytes, feedback: {1}", res, ((SmartField)card.objects[i]).SModule.GetFeedback()));
                                if (res > 0)
                                    res = 1;
                            }
                            else
                            {
                                res = ((SmartField)card.objects[i]).SModule.WriteCard(card.objects[i].GetText());
                                Device_eventPassMessage(Devices.MessageType.Debug, String.Format("Perso: {0:X}", res));
                            }
                            smartTimer.Stop();

                            if (res != 1)
                            {
                                allok = false;
                                break;
                            }
                        }
                    }
                    if (allok)
                        Device_eventPassMessage(Devices.MessageType.CompleteStep, "");
                    else
                        Device_eventPassMessage(Devices.MessageType.CardError, "StatusPersoError");
#endregion
                    break;
                case Step.Print:
#region print
                    if (Devices.DeviceClass.IsEmbosser(card.device.DeviceType) && !DeviceClass.IsXPS(card.device.DeviceType))
                    {
                        ArrayList al = card.CompileMessage(currentRecord);
                        for (int i = 0; i < al.Count; i++)
                            ((Devices.Embosser)card.device).AddToQueue((string)al[i]);
                    }
                    if (Devices.DeviceClass.IsPrinter(card.device.DeviceType))
                    {
                        for (int i = 0; i < card.objects.Count; i++)
                        {
                            if (card.objects[i].OType == ObjectType.MagStripe)
                            {
                                ((PrinterClass)card.device).SetMagstripe(((MagStripe)card.objects[i]).TextM);
                                break;
                            }
                        }
                    }
                    
                    
                    card.SetTopLeftForPrint();
                    DrawingVisual dw = new DrawingVisual();

                    //DrawingContext dc = dw.RenderOpen();
                    //                    RenderTargetBitmap bitmap = new RenderTargetBitmap(
                    //                      Card.ClientToScreen(Card.Width), Card.ClientToScreen(Card.Height), 300, 300, PixelFormats.Default);
                    using (DrawingContext dc = dw.RenderOpen())
                    {
                        for (int i = 0; i < card.objects.Count; i++)
                            card.objects[i].Draw(dc, Regim.ToPrinter, false, 0);
                        dc.Close();
                    }
                    //dc.Close();

                    if (DeviceClass.IsPrinter(card.device.DeviceType))
                        ((Devices.PrinterClass)card.device).SetImages(dw, null);
                    card.device.PrintCard();
#endregion
                    break;
                case Step.FeedSmart:
                    card.device.FeedCard(FeedType.SmartFront);
                    break;
                case Step.FeedMag:
                    card.device.FeedCard(FeedType.Magstripe);
                    break;
                case Step.Resume:
                    card.device.ResumeCard();
                    break;
                case Step.ReadMag:
                    card.device.ReadMagstripe();
                    break;
                case Step.GetMagData:
                    for (int i = 0; i < card.objects.Count;i++)
                    {
                        if (card.objects[i].OType == ObjectType.MagStripe)
                        {
                            string[] strs = card.device.GetMagstripe();
                            Device_eventPassMessage(Devices.MessageType.Debug, String.Format("Read Magstripe: Track1 = {0}, Track2 = {1}, Track3 = {2}", strs[0], strs[1], strs[2]));
                            //((MagStripe)card.objects[i]).Tracks[0] = strs[0];
                            //((MagStripe)card.objects[i]).Tracks[1] = strs[1];
                            //((MagStripe)card.objects[i]).Tracks[2] = strs[2];
                            break;
                        }
                    }
                    for (int i = 0; i < card.objects.Count; i++)
                    {
                        if (card.objects[i].OType == ObjectType.MagStripe)
                        {
                            break;
                        }
                    }
                    Device_eventPassMessage(Devices.MessageType.CompleteStep, "");
                    break;
                case Step.End:
                    card.device.EndCard();
                    break;
            }
        }
        private void SmartTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
#warning smart timer elapsed not implemented
        }
        private void Command_SendToPrintCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void Command_LastRecord(object sender, ExecutedRoutedEventArgs e)
        {
            if (dataIn == null)
                return;
            currentRecord = dataIn.Rows.Count - 1;
            SetFieldsText();
            DrawCard();
        }

        private void Command_LastRecordCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (dataIn != null && currentRecord < dataIn.Rows.Count - 1);
        }

        private void Command_PrevRecord(object sender, ExecutedRoutedEventArgs e)
        {
            if (dataIn == null)
                return;
            currentRecord--;
            if (currentRecord < 0)
                currentRecord = 0;
            SetFieldsText();
            DrawCard();
        }

        private void Command_PrevRecordCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (dataIn != null && currentRecord > 0);
        }

        private void Command_NextRecord(object sender, ExecutedRoutedEventArgs e)
        {
            if (dataIn == null)
                return;
            currentRecord++;
            if (currentRecord >= dataIn.Rows.Count)
                currentRecord = dataIn.Rows.Count - 1;
            SetFieldsText();
            DrawCard();
        }
        private void Command_NextRecordCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (dataIn != null && currentRecord < dataIn.Rows.Count - 1);
        }
        private void Command_FirstRecord(object sender, ExecutedRoutedEventArgs e)
        {
            if (dataIn == null)
                return;
            currentRecord = 0;
            SetFieldsText();
            DrawCard();
        }
        private void Command_FirstRecordCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (dataIn != null && currentRecord > 0);
        }
        private void Command_DesignMode(object sender, ExecutedRoutedEventArgs e)
        {
            if (card == null)
            {
                CreateNewCard();
                return;
            }
            currentRegim = Regim.Design;
            currentObject = null;
            currentTool = ObjectType.None;
            RefreshList();
            RefreshProperties();
            ShowPanel();
        }
        private void Command_DesignModeCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void Command_PrintMode(object sender, ExecutedRoutedEventArgs e)
        {
            if (card == null)
                return;
            currentRegim = Regim.Print;
            ShowPanel();
        }
        private void Command_PrintModeCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (card != null);
        }

        private void dpPrintFront_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void dpPrintFront_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void dpPrintBack_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void bDBOut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cbEmbossLine_Font_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.EmbossLine)
                return;
            switch (Convert.ToInt32(((ComboBoxItem)cbEmbossLine_Font.SelectedItem).Tag))
            {
                case 1:
                    ((EmbossLine)currentObject).Font = EmbossFont.Gothic;
                    ((EmbossLine)currentObject).Registered = true;
                    currentObject.Side = SideType.Front;
                    break;
                case 2:
                    ((EmbossLine)currentObject).Font = EmbossFont.Farrington;
                    ((EmbossLine)currentObject).Registered = true;
                    currentObject.Side = SideType.Front;
                    break;
                case 3:
                    ((EmbossLine)currentObject).Font = EmbossFont.OCR10;
                    ((EmbossLine)currentObject).Registered = true;
                    currentObject.Side = SideType.Front;
                    break;
                case 4:
                    ((EmbossLine)currentObject).Font = EmbossFont.MCIndent;
                    ((EmbossLine)currentObject).Registered = false;
                    currentObject.Side = SideType.Back;
                    break;
                case 5:
                    ((EmbossLine)currentObject).Font = EmbossFont.OCR7;
                    ((EmbossLine)currentObject).Registered = true;
                    currentObject.Side = SideType.Front;
                    break;
                case 6:
                    ((EmbossLine)currentObject).Font = EmbossFont.Braille;
                    ((EmbossLine)currentObject).Registered = true;
                    currentObject.Side = SideType.Front;
                    break;
            }
            currentObject.X = currentObject.X; //чтобы перерасчитать digits
            for (int i = 0; i < card.objects.Count; i++)
            {
                if (card.objects[i].OType == ObjectType.EmbossText && ((EmbossText)card.objects[i]).Parent.ID == currentObject.ID)
                {
                    card.objects[i].Side = currentObject.Side;
                    EmbossText et = ((EmbossText)card.objects[i]);
                    EmbossLine el = ((EmbossLine)currentObject);
                    if (et.Position > el.GetDigitsCount())
                    {
                        et.Shablon = ""; et.Position = el.GetDigitsCount() - 1;
                    }
                    if (et.Position + et.Shablon.Length > el.GetDigitsCount())
                        et.Shablon = et.Shablon.Substring(0, el.GetDigitsCount() - et.Position);
                }
            }
            if (card != null && card.device != null && card.device.DeviceType == DeviceType.DC450)
                ((EmbossLine)currentObject).SetRegistered(((Devices.DC450)card.device).DIndent);
            //DrawCard(currentObject.Side);
        }

        private void cbSmartFeedback_Checked(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField)
                return;
            ((SmartField)currentObject).Feedback = (cbSmartFeedback.IsChecked == true);
        }

        private void bSmartIn_Click(object sender, RoutedEventArgs e)
        {
            bFieldIn_Click(sender);
        }

        private void cbReadMagstripe_Checked(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.MagStripe)
                return;
            ((MagStripe)currentObject).Feedback = (cbReadMagstripe.IsChecked == true);
        }

        private void cb450Speed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (card == null || card.device == null || card.device.DeviceType != DeviceType.DC450)
                return;
            if (cb450Speed.SelectedIndex == 0)
                ((Devices.DC450)card.device).Speed = Devices.SpeedType.Standard;
            if (cb450Speed.SelectedIndex == 1)
                ((Devices.DC450)card.device).Speed = Devices.SpeedType.Mag;
            if (cb450Speed.SelectedIndex == 2)
                ((Devices.DC450)card.device).Speed = Devices.SpeedType.Smart;

        }

        private void lbFields_KeyUp(object sender, KeyEventArgs e)
        {
            //удаление полей
            if (e.Key != Key.Delete)
                return;
            if (currentObject == null)
                return;
            if (currentObject.OType == ObjectType.EmbossLine) //удаляем все поля эмбоссирования на ней
            {
                for(int i = 0; i < card.objects.Count; i++)
                {
                    if (card.objects[i].OType == ObjectType.EmbossText && ((EmbossText)card.objects[i]).ParentID == currentObject.ID)
                        card.objects.RemoveAt(i--); //по i остаемся на месте
                }
                //проверяем первая ли линия и меняем, если да
                if (((EmbossLine)currentObject).FirstLine)
                {
                    for (int i = 0; i < card.objects.Count; i++)
                    {
                        if (card.objects[i].OType == ObjectType.EmbossLine && card.objects[i].ID != currentObject.ID)
                        {
                            ((EmbossLine)currentObject).FirstLine = true;
                            break;
                        }
                    }
                }
            }
            if (currentObject.OType == ObjectType.EmbossText) // очищаем позиции
                ((EmbossText)currentObject).Parent.SetDigits(((EmbossText)currentObject).Position, ((EmbossText)currentObject).Shablon.Length, false);
            card.objects.Remove(currentObject.ID);
            currentObject = null;
            //bPointer.IsChecked = true;
            bEmbossField.IsChecked = false;
            DrawCard();
            RefreshList();
        }

        private void bMagstripeIn_Click(object sender, RoutedEventArgs e)
        {
            bFieldIn_Click(sender);
        }

        private void cb450Indent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (card == null || card.device == null || card.device.DeviceType != DeviceType.DC450)
                return;
            ((Devices.DC450)card.device).DIndent = Devices.DoubleIndent.None;
            if (cb450Indent.SelectedIndex == 1)
                ((Devices.DC450)card.device).DIndent = Devices.DoubleIndent.Front;
            if (cb450Indent.SelectedIndex == 2)
                ((Devices.DC450)card.device).DIndent = Devices.DoubleIndent.Back;
            // переопределяем регистрацию индента
            for(int i = 0; i < card.objects.Count; i++)
            {
                if (card.objects[i].OType != ObjectType.EmbossLine || ((EmbossLine)card.objects[i]).IsEmbossFont)
                    continue;
                ((EmbossLine)card.objects[i]).SetRegistered(((Devices.DC450)card.device).DIndent);
            }
            DrawCard();
        }
        private void tb450DopOffset_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (card == null || card.device == null || card.device.DeviceType != DeviceType.DC450)
                return;
            try
            {
                ((Devices.DC450)card.device).DopOffset = Convert.ToDouble(tb450DopOffset.Text);
            }
            catch
            {
                ((Devices.DC450)card.device).DopOffset = 0.0;
            }
        }

        private void bSmartReaderRefresh_Click(object sender, RoutedEventArgs e)
        {
            FillReaders();
        }
        
        private void Command_SmartDefaultSave(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField)
                return;
            XmlSerializer ser = new XmlSerializer(typeof(SmartModule.SmartModule));
            ser.UnknownNode += new XmlNodeEventHandler(Ser_UnknownNode);
            ser.UnknownAttribute += new XmlAttributeEventHandler(Ser_UnknownAttribute);
            string smartfilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "smartdefault.xml");
            TextWriter tw = new StreamWriter(smartfilename);
            ser.Serialize(tw, ((SmartField)currentObject).SModule);
            tw.Close();
        }

        private void Command_SmartDefaultSaveCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Command_SmartDefaultLoad(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.SmartField)
                return;
            XmlSerializer ser = new XmlSerializer(typeof(SmartModule.SmartModule));
            ser.UnknownNode += new XmlNodeEventHandler(Ser_UnknownNode);
            ser.UnknownAttribute += new XmlAttributeEventHandler(Ser_UnknownAttribute);
            string smartfilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "smartdefault.xml");
            TextReader tr = new StreamReader(smartfilename);
            ((SmartField)currentObject).SModule = (SmartModule.SmartModule)ser.Deserialize(tr);
            tr.Close();
            if (((SmartField)currentObject).SModule == null)
                return;
            lSmartTitle.DataContext = (SmartField)currentObject;
            gbSmart.Height = 100;
            //gbSmartConfig.DataContext = ((SmartField)currentObject).SModule;
            cbSmartType.DataContext = (SmartField)currentObject;
            switch (((SmartField)currentObject).SModule.SType)
            {
                case SmartModule.SmartType.OstcardStandard:
                    FillReaders();
                    gbSmartConfig.DataContext = (SmartModule.OstcardStandard)((SmartField)currentObject).SModule;
                    cbSmartType.DataContext = (SmartField)currentObject;
                    gbSmartOstcardStandart.DataContext = (SmartModule.OstcardStandard)((SmartField)currentObject).SModule;
                    break;
            }
            //gbSmartConfig.DataContext = (SmartField)currentObject;
            //tbSmartTimeout.Text = ((SmartField)currentObject).SModule.Timeout.ToString();
            //tbDllPath.Text = ((SmartField)currentObject).SModule.Path;
            //cbSmartType.SelectedIndex = cbSmartModuleTypeIndex((int)((SmartField)currentObject).SModule.SType);
            //cbSmartFeedback.IsChecked = ((SmartField)currentObject).Feedback;
            //if (((SmartField)currentObject).SModule.SType == SmartModule.SmartType.OstcardStandard)
            //    gbSmartOstcardStandart.DataContext = (SmartModule.OstcardStandard)((SmartField)currentObject).SModule;
        }

        private void Command_SmartDefaultLoadCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void cbPrinterDrivers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Devices.DeviceClass.IsPrinter(card.device.DeviceType))
                return;
            if (cbPrinterDrivers.SelectedIndex < 0)
                return;
            ((PrinterClass)card.device).printerName = cbPrinterDrivers.SelectedValue.ToString();
        }

        private void BeginIssuing()
        {
            card.device.StartJob();
            SendToDevice();
        }
        private void SendToDevice()
        {
            steps.Clear();
            steps.Enqueue(Step.Start);
            if (card.HasMagstripeRead())
            {
                steps.Enqueue(Step.FeedMag);
                steps.Enqueue(Step.ReadMag);
                steps.Enqueue(Step.GetMagData);
            }
            if (card.HasSmart())
            {
                steps.Enqueue(Step.FeedSmart);
                steps.Enqueue(Step.Perso);
                steps.Enqueue(Step.Resume);
            }
            steps.Enqueue(Step.Print);
            steps.Enqueue(Step.End);
            ExecuteStep((Step)steps.Peek());
        }

        private void Command_LoadDbFile(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog oFile = new OpenFileDialog();
            oFile.Filter = $"{(string)this.FindResource("AllFiles")}|*.*";
            string dataDir = HugeLib.XmlClass.GetDataXml(xmlSettings, "DataFolder", xnmSettings);
            if (!String.IsNullOrEmpty(dataDir))
                oFile.InitialDirectory = dataDir;
            if (oFile.ShowDialog() == true)
            {
                try
                {
                    File.Copy(oFile.FileName, Path.Combine(card.DbIn.Directory(), card.DbIn.Table), true);
                    LoadRecordSet();
                    SetFieldsText();
                    DrawCard();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, (string)this.FindResource("Error"));
                }
            }
        }

        private void Command_LoadDbFileCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            if (card == null || card.DbIn == null)
                return;
            e.CanExecute = card.DbIn.IsText();
        }

        private void Command_FileSaveAsCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (card != null);
        }

        private void Command_FileSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
#if DEBUG
            filename = "design.xml";
#else
            SaveFileDialog sFile = new SaveFileDialog();
            sFile.Filter = "Design files (*.xml)|*.xml|All files (*.*)|*.*";
            sFile.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Ostcard", "Procard 2.0", "Designs");
            if (sFile.ShowDialog() == true)
                filename = sFile.FileName;
            else
                return;
#endif
            this.Title = $"{this.FindResource("WindowTitle").ToString()} - {filename}";
            XmlSerializer ser = new XmlSerializer(typeof(Card));
            ser.UnknownNode += new XmlNodeEventHandler(Ser_UnknownNode);
            ser.UnknownAttribute += new XmlAttributeEventHandler(Ser_UnknownAttribute);
            TextWriter tw = new StreamWriter(filename);
            ser.Serialize(tw, card);
            tw.Close();
        }

        private void tbTextFieldShablon_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.TextField)
                return;
            DrawCard(currentObject.Side);
        }

        private void tbEmbossText2Shablon_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.EmbossText2)
                return;
            DrawCard(currentObject.Side);
        }

        private void ClearDesignContext()
        {
            gbEmbossText2.DataContext = null;
            lSmartTitle.DataContext = null;
            gbSmartConfig.DataContext = null;
            gbSmartOstcardStandart.DataContext = null;
            gbFieldCommon.DataContext = null;
        }

        private void bTextFieldFont_Click(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.TextField)
                return;
            FontDialog fd = new FontDialog();
            fd.Font = ((TextField)currentObject).Font.GetFont();
            DialogResult dr = fd.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                ((TextField)currentObject).Font.FontName = fd.Font.Name;
                ((TextField)currentObject).Font.FontSize = fd.Font.Size;
                ((TextField)currentObject).Font.FontStyle = (fd.Font.Italic) ? FontStyles.Italic : FontStyles.Normal;
                ((TextField)currentObject).Font.SetParameters(fd.Font.Underline, fd.Font.Strikeout);
                ((TextField)currentObject).RaisePropertyChanged("FontString");
                DrawCard(currentObject.Side);
            }
        }
        //private void bTextFieldColor_Click(object sender, RoutedEventArgs e)
        //{
        //    if (currentObject == null || currentObject.OType != ObjectType.TextField)
        //        return;
        //}
        private void ColorToolbar_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            DrawCard(currentObject.Side);
        }
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentObject == null)
                return;
            DrawCard(currentObject.Side);
        }

        private void comboBoxDataIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fieldDataInChanged((ComboBox)sender);
        }

        private void bBarcodeFont_Click(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.Barcode)
                return;
            FontDialog fd = new FontDialog();
            fd.Font = ((Barcode)currentObject).Font.GetFont();
            DialogResult dr = fd.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                ((Barcode)currentObject).Font.FontName = fd.Font.Name;
                ((Barcode)currentObject).Font.FontSize = fd.Font.Size;
                ((Barcode)currentObject).Font.FontStyle = (fd.Font.Italic) ? FontStyles.Italic : FontStyles.Normal;
                ((Barcode)currentObject).Font.SetParameters(fd.Font.Underline, fd.Font.Strikeout);
                ((Barcode)currentObject).RaisePropertyChanged("FontString");
                DrawCard(currentObject.Side);
            }
        }
        private void bBarcodeHeaderFont_Click(object sender, RoutedEventArgs e)
        {
            if (currentObject == null || currentObject.OType != ObjectType.Barcode)
                return;
            FontDialog fd = new FontDialog();
            fd.Font = ((Barcode)currentObject).HeaderFont.GetFont();
            DialogResult dr = fd.ShowDialog();
            if (dr != System.Windows.Forms.DialogResult.Cancel)
            {
                ((Barcode)currentObject).HeaderFont.FontName = fd.Font.Name;
                ((Barcode)currentObject).HeaderFont.FontSize = fd.Font.Size;
                ((Barcode)currentObject).HeaderFont.FontStyle = (fd.Font.Italic) ? FontStyles.Italic : FontStyles.Normal;
                ((Barcode)currentObject).HeaderFont.SetParameters(fd.Font.Underline, fd.Font.Strikeout);
                ((Barcode)currentObject).RaisePropertyChanged("HeaderFontString");
                DrawCard(currentObject.Side);
            }
        }

        private void tbCEHopper_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (card.device == null || card.device.DeviceType != DeviceType.CE)
                return;
            try
            {
                ((XPSPrinter)card.device).HopperID = Convert.ToInt32(tbCEHopper.Text);
            }
            catch 
            {
            }
        }

        private void textBoxChanged(object sender, TextChangedEventArgs e)
        {
            DrawCard(currentObject.Side);
        }

        private void bImageIn_Click(object sender, RoutedEventArgs e)
        {
            if (currentObject.OType != ObjectType.ImageField)
                return;
            if (currentObject.InType == InTypes.File)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (((ImageField)currentObject).Style == ImageStyle.Mask)
                    ofd.Filter = $"{(string)this.FindResource("Image_MaskExt")}|*.xaml|{(string)this.FindResource("AllFiles")}|*.*";
                else
                    ofd.Filter = $"{(string)this.FindResource("Image_ImageFileExt")}|*.bmp;*.gif;*.jpg;*.jpeg;*.png|{(string)this.FindResource("AllFiles")}|*.*";
                if (ofd.ShowDialog() == true)
                {
                    currentObject.InData = ofd.FileName;
                    currentObject.SetText(ofd.FileName);
                }
            }
            bool isComposite = (currentObject.InType == InTypes.Composite);
            if (isComposite)
            {
                CompositeForm cf = new CompositeForm();
                cf.Owner = this;
                cf.LoadFields(card, currentObject, 0);
                var res = cf.ShowDialog();
                if (res != null && res.Value)
                {
                    currentObject.InData = cf.GetCurrent();
                }
            }
            DrawCard();
        }

        private void Command_FieldUp(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentObject == null || card == null) 
                return;
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            if (index == 0)
                return;
            card.objects.RemoveAt(index);
            card.objects.InsertAt(index-1, currentObject);
            RefreshList(currentObject.ID);
            DrawCard();
        }

        private void Command_FieldDown(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
                return;
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            if (index >= card.objects.Count-1)
                return;
            card.objects.RemoveAt(index);
            card.objects.InsertAt(index + 1, currentObject);
            RefreshList(currentObject.ID);
            DrawCard();
        }

        private void Command_FieldFirst(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
                return;
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            if (index == 0)
                return;
            card.objects.RemoveAt(index);
            card.objects.InsertAt(0, currentObject);
            RefreshList(currentObject.ID);
            DrawCard();

        }

        private void Command_FieldEnd(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
                return;
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            if (index >= card.objects.Count-1)
                return;
            card.objects.RemoveAt(index);
            card.objects.Add(currentObject);
            RefreshList(currentObject.ID);
            DrawCard();
        }

        private void Command_FieldUpCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
            {
                e.CanExecute = false;
                return;
            }
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            e.CanExecute = (index > 0);
        }

        private void Command_FieldDownCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
            {
                e.CanExecute = false;
                return;
            }
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            e.CanExecute = (index < card.objects.Count-1);

        }

        private void Command_FieldFirstCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
            {
                e.CanExecute = false;
                return;
            }
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            e.CanExecute = (index > 1);
        }

        private void Command_FieldEndCanBeExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (currentObject == null || card == null)
            {
                e.CanExecute = false;
                return;
            }
            int index = 0;
            for (int i = 0; i < card.objects.Count; i++, index++)
                if (card.objects[i].ID == currentObject.ID)
                    break;
            e.CanExecute = (index < card.objects.Count - 2);


        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DrawCard(currentObject.Side);
        }
    }
    public static class CustomCommands
    {
        public static readonly RoutedUICommand FileNew = new RoutedUICommand("FileNew","FileNew", typeof(CustomCommands));
        public static readonly RoutedUICommand FileOpen = new RoutedUICommand("FileOpen", "FileOpen", typeof(CustomCommands));
        public static readonly RoutedUICommand FileSave = new RoutedUICommand("FileSave", "FileSave", typeof(CustomCommands));
        public static readonly RoutedUICommand FileSaveAs = new RoutedUICommand("FileSaveAs", "FileSaveAs", typeof(CustomCommands));
        public static readonly RoutedUICommand LanguageRussian = new RoutedUICommand("Russian", "Russian", typeof(CustomCommands));
        public static readonly RoutedUICommand LanguageEnglish = new RoutedUICommand("English", "English", typeof(CustomCommands));
        public static readonly RoutedUICommand DesignMode = new RoutedUICommand("DesignMode", "DesignMode", typeof(CustomCommands));
        public static readonly RoutedUICommand PrintMode = new RoutedUICommand("PrintMode", "PrintMode", typeof(CustomCommands));
        public static readonly RoutedUICommand Pointer = new RoutedUICommand("Pointer", "Pointer", typeof(CustomCommands));
        public static readonly RoutedUICommand EmbossLine = new RoutedUICommand("EmbossLine", "EmbossLine", typeof(CustomCommands));
        public static readonly RoutedUICommand EmbossField = new RoutedUICommand("EmbossField", "EmbossField", typeof(CustomCommands));
        public static readonly RoutedUICommand EmbossField2 = new RoutedUICommand("EmbossField2", "EmbossField2", typeof(CustomCommands));
        public static readonly RoutedUICommand MagStripe = new RoutedUICommand("MagStripe", "MagStripe", typeof(CustomCommands));
        public static readonly RoutedUICommand TextField = new RoutedUICommand("TextField", "TextField", typeof(CustomCommands));
        public static readonly RoutedUICommand ImageField = new RoutedUICommand("ImageField", "ImageField", typeof(CustomCommands));
        public static readonly RoutedUICommand BarCode = new RoutedUICommand("BarCode", "BarCode", typeof(CustomCommands));
        public static readonly RoutedUICommand Topcoat = new RoutedUICommand("Topcoat", "Topcoat", typeof(CustomCommands));
        public static readonly RoutedUICommand SmartField = new RoutedUICommand("SmartField", "SmartField", typeof(CustomCommands));
        public static readonly RoutedUICommand ReportField = new RoutedUICommand("ReportField", "ReportField", typeof(CustomCommands));
        public static readonly RoutedUICommand FieldUp = new RoutedUICommand("FieldUp", "FieldUp", typeof(CustomCommands));
        public static readonly RoutedUICommand FieldDown = new RoutedUICommand("FieldDown", "FieldDown", typeof(CustomCommands));
        public static readonly RoutedUICommand FieldFirst = new RoutedUICommand("FieldFirst", "FieldFirst", typeof(CustomCommands));
        public static readonly RoutedUICommand FieldEnd = new RoutedUICommand("FieldEnd", "FieldEnd", typeof(CustomCommands));
        public static readonly RoutedUICommand SendToPrint = new RoutedUICommand("SendToPrint", "SendToPrint", typeof(CustomCommands));
        public static readonly RoutedUICommand FirstRecord = new RoutedUICommand("FirstRecord", "FirstRecord", typeof(CustomCommands));
        public static readonly RoutedUICommand PrevRecord = new RoutedUICommand("PrevRecord", "PrevRecord", typeof(CustomCommands));
        public static readonly RoutedUICommand NextRecord = new RoutedUICommand("NextRecord", "NextRecord", typeof(CustomCommands));
        public static readonly RoutedUICommand LastRecord = new RoutedUICommand("LastRecord", "LastRecord", typeof(CustomCommands));
        public static readonly RoutedUICommand LoadDbFile = new RoutedUICommand("LoadDbFile", "LoadDbFile", typeof(CustomCommands));
        public static readonly RoutedUICommand SmartDefaultLoad = new RoutedUICommand("SmartDefaultLoad", "SmartDefaultLoad", typeof(CustomCommands));
        public static readonly RoutedUICommand SmartDefaultSave = new RoutedUICommand("SmartDefaultSave", "SmartDefaultSave", typeof(CustomCommands));
    }
}
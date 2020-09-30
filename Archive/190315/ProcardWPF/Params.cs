using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
//using System.Collections.Generic;
using System.Linq;
//using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Resources;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using AKSHASP;
//using System.Windows.Forms;

namespace ProcardWPF
{
    public class ProcardWindowState
    {
        private double left;
        private double top;
        private double height;
        private double width;

        WindowState state;

        public ProcardWindowState()
        {
            state = WindowState.Normal;
        }

        public double Left
        {
            get
            {
                return left;
            }
            set
            {
                left = value;
            }
        }
        public double Top
        {
            get
            {
                return top;
            }
            set
            {
                top = value;
            }
        }
        public double Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }
        public double Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }
        public WindowState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }
    }
    public class ProcardWindowStateManager
    {
        ProcardWindowState windowState;
        Window ownerWindow;

        public ProcardWindowState WindowState
        {
            get
            {
                return windowState;
            }
            set
            {
                windowState = value;
            }
        }
        public Window OwnerWindow
        {
            get
            {
                return ownerWindow;
            }
            set
            {
                ownerWindow = value;
            }
        }

        public ProcardWindowStateManager(ProcardWindowState procardWinState, Window ownerWin)
        {
            if (procardWinState == null)
                throw new ArgumentNullException("ProcardWindowState");
            if (ownerWin == null)
                throw new ArgumentNullException("ProcardOwnerWindow");
            windowState = procardWinState;
            ownerWindow = ownerWin;
            ownerWindow.LocationChanged += new EventHandler(ownerWindow_LocationChanged);
            ownerWindow.SizeChanged += new SizeChangedEventHandler(ownerWindow_SizeChanged);

            SetInitialWindowState();
        }
        private void SetInitialWindowState()
        {
            if (windowState.Width == 0.0 && windowState.Height == 0.0)
                return;
            // check window sizes
            if (windowState.Width < ownerWindow.MinWidth)
                windowState.Width = ownerWindow.MinWidth;
            if (windowState.Width > ownerWindow.MaxWidth)
                windowState.Width = ownerWindow.MaxWidth;
            if (windowState.Height < ownerWindow.MinHeight)
                windowState.Height = ownerWindow.MinHeight;
            if (windowState.Height > ownerWindow.MaxHeight)
                windowState.Height = ownerWindow.MaxHeight;
            if (windowState.Left < 0.0)
                windowState.Left = 0.0;
            if (windowState.Left > SystemParameters.WorkArea.Width - 50.0)
                windowState.Left = SystemParameters.WorkArea.Width - 50.0;
            if (windowState.Top < 0.0)
                windowState.Top = 0.0;
            if (windowState.Top > SystemParameters.WorkArea.Height - 50.0)
                windowState.Top = SystemParameters.WorkArea.Height - 50.0;
            // set window size
            ownerWindow.Left = windowState.Left;
            ownerWindow.Top = windowState.Top;
            ownerWindow.Width = windowState.Width;
            ownerWindow.Height = windowState.Height;
            if (windowState.State == System.Windows.WindowState.Normal || windowState.State == System.Windows.WindowState.Maximized)
                ownerWindow.WindowState = windowState.State;
        }

        void ownerWindow_LocationChanged(object sender, EventArgs e)
        {
            if (ownerWindow.WindowState == System.Windows.WindowState.Normal)
            {
                windowState.Left = ownerWindow.Left;
                windowState.Top = ownerWindow.Top;
            }
        }
        void ownerWindow_SizeChanged(object sender, EventArgs e)
        {
            if (ownerWindow.WindowState == System.Windows.WindowState.Normal)
            {
                windowState.Width = ownerWindow.Width;
                windowState.Height = ownerWindow.Height;
            }
            windowState.State = ownerWindow.WindowState;
        }
    }
    public class Settings
    {
        ProcardWindowState windowState;
        public ProcardWindowState WindowState
        {
            get
            {
                return windowState;
            }
            set
            {
                windowState = value;
            }
        }
        public Lang Language;

        public string DesignDirectory;
        public string[] EmbCyrillic;
        public Settings()
        {
            windowState = new ProcardWindowState();
            Language = Lang.Russian;
            EmbCyrillic = new string[2];
            EmbCyrillic[0] = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
            EmbCyrillic[1] = "AaBbcEEdefuKgMHOhPCTijXlmnopqvrst";

        }
    }
    static class SettingManager
    {
        static Settings settings = new Settings();
        public static Settings ProcardSettings
        {
            get
            {
                return settings;
            }
        }
        const string applicationDirectory = "Ostcard\\Procard 2.0";
        
        const string settingsFileName = "Settings.xml";
        static SettingManager()
        {
            EnsureDirectoryExists();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        static void EnsureDirectoryExists()
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    applicationDirectory));

                if (!info.Exists)
                {
                    info.Create();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
        public static void OnStartup()
        {
            LoadSettings();
        }
        public static void OnExit()
        {
            SaveSettings();
        }
        static string SettingsFileName
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), applicationDirectory, settingsFileName);
            }
        }
        static void LoadSettings()
        {
            Settings tmp;
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(Settings));
                using (Stream stream = new FileStream(SettingsFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    tmp = (Settings)xml.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return;
            }
            settings = tmp;
            Params.Language = settings.Language;
            if (settings.EmbCyrillic != null && settings.EmbCyrillic.Length > 1)
            {
                Params.EmbCyrillic[0] = settings.EmbCyrillic[0];
                Params.EmbCyrillic[1] = settings.EmbCyrillic[1];
            }
        }
        static void SaveSettings()
        {
            try
            {
                settings.Language = Params.Language;
                XmlSerializer xml = new XmlSerializer(typeof(Settings));
                using (Stream stream = new FileStream(SettingsFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    xml.Serialize(stream, settings);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }
    }
    static class Params
    {
        public static int Log;
        public static bool UseMetric;
        public static Lang Language;
        private static object lockLog = new object();
        public static string[] EmbCyrillic = new string[2];
        public static string Version;
        public static string License;
        public static VisualBrush HatchBrush()
        {
            VisualBrush vb = new VisualBrush();
            vb.TileMode = TileMode.Tile;
            vb.Viewport = new Rect(0, 0, 6, 6);
            vb.ViewportUnits = BrushMappingMode.Absolute;
            vb.Viewbox = new Rect(0, 0, 6, 6);
            vb.ViewboxUnits = BrushMappingMode.Absolute;
            System.Windows.Shapes.Line l = new System.Windows.Shapes.Line();
            l.Stroke = Brushes.Black;
            l.X1 = -3; l.Y1 = -3;
            l.X2 = 9; l.Y2 = 9;
            vb.Visual = l;
            return vb;
        }
        public static void WriteLogString(string str, params object[] args)
        {
            if (Log <= 0)
                return;
            str = String.Format(str, args).Replace(String.Format($"{0x0d}{0x0a}"), "[CR][LF]");
            if (str.Length > 0)
                str = $"{DateTime.Now:HH:mm:ss.hhh}\t{str}\n";
            lock (lockLog)
            {
                Stream file = null;
                try
                {
                    String appStartPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    file = File.Open(System.Windows.Forms.Application.StartupPath + String.Format("\\Procard_{0:yyMMdd}.log", DateTime.Now.Date), FileMode.OpenOrCreate, FileAccess.Write);
                    file.Seek(0, SeekOrigin.End);
                    byte[] bytes = new byte[str.Length];
                    bytes = System.Text.Encoding.Default.GetBytes(str);
                    file.Write(bytes, 0, bytes.Length);
                }
                catch
                {
                }
                finally
                {
                    if (file != null)
                    {
                        file.Close();
                        file = null;
                    }
                }
            }
        }
        public static bool CheckHasp()
        {
            Version = "";
            License = "";
            string vendorCode = "DkjDDrb92dBzOk7UHEDk8ZSTR1n3npW1BrI9FPSekIcKnjD1HTYmKR0Buqe1Jq67QJQAcyo0JYbUedJpa9UP3TUxUIEh5j3vm1ofhnT1u2l25QViB5oeQ2ncK67W+AyFNbngPjcEeu80uIlUyDfQvtjBMSjP/N4Zz1G6LrXyFGCte7gkteoZJrrThrbvJY+JkjYsKlCkcB8gVcuftGx0HZNqEEF4cBaLIOjpm5w4jfqZPBQZo6OCexEogUhrsw9NQsZW1A8e/jbgbb/5MzPnX/At7S8mpgWijFGFAtviopwgqdi+VrqpPT7OEi1988/c2NSmpiYQpJ9OGveeJs05UUnwhsZONG50b0vhEwyasdOkC6JGhJKNF68C5z8AMTRbIxlTCLtEfMSBJccJc3Ci31kfKLl+fenHFhAYETrz2L/S435RlXtAGk5/v2Qt3rcHkT6Eu0XAFH5sBLMR1r7UgzX+Le3k27tbV11N7lAva2FYAwk0ZRrBsmY+B+ARoleciwzSTTBcOgEa2Pb2oilM2sXsDAzKQf6MxxRyj74pyOKf5wt7FWyS1fZLTrqNFKytA6+XF/RFGBatvpD28WFjjX64rXCDgNM2XUa9HTeiIf6Q2jM+uDZ+Pvj8UDEhcUl/gBYPX9vC+WimetVtcSLoohvdoZOq3oO4ZiVG3VRKNq2fcrh6VjHzzTk288IG1E03asCPzddEvL6LAe7xGLuTOrRc8cE1";            
            HaspApplication hasp1 = new HaspApplication();
            HaspFeature feature = (HaspFeature)hasp1.ProgNumDefaultFeature;
            feature.SetOptions((int)HaspFeatureOptions.haspOptionNotRemote, (int)HaspFeatureOptions.haspOptionDefault);
            feature.SetOptions((int)HaspFeatureOptions.haspOptionClassic, (int)HaspFeatureOptions.haspOptionDefault);
            Hasp hasp2 = (Hasp)hasp1.Hasp(feature);
            byte[] vc = System.Text.Encoding.GetEncoding(1251).GetBytes(vendorCode);
            vc[3] = (byte)'Y';
            int res_log = (int)hasp2.Login(System.Text.Encoding.GetEncoding(1251).GetString(vc));
            hasp2.Logout();
            return (res_log == (int)HaspStatusCodes.haspStatusOk);
        }
        public static bool CheckClue_Adv()
        {
            Version = "";
            License = "";
            string vendorCode = "DkjDDrb92dBzOk7UHEDk8ZSTR1n3npW1BrI9FPSekIcKnjD1HTYmKR0Buqe1Jq67QJQAcyo0JYbUedJpa9UP3TUxUIEh5j3vm1ofhnT1u2l25QViB5oeQ2ncK67W+AyFNbngPjcEeu80uIlUyDfQvtjBMSjP/N4Zz1G6LrXyFGCte7gkteoZJrrThrbvJY+JkjYsKlCkcB8gVcuftGx0HZNqEEF4cBaLIOjpm5w4jfqZPBQZo6OCexEogUhrsw9NQsZW1A8e/jbgbb/5MzPnX/At7S8mpgWijFGFAtviopwgqdi+VrqpPT7OEi1988/c2NSmpiYQpJ9OGveeJs05UUnwhsZONG50b0vhEwyasdOkC6JGhJKNF68C5z8AMTRbIxlTCLtEfMSBJccJc3Ci31kfKLl+fenHFhAYETrz2L/S435RlXtAGk5/v2Qt3rcHkT6Eu0XAFH5sBLMR1r7UgzX+Le3k27tbV11N7lAva2FYAwk0ZRrBsmY+B+ARoleciwzSTTBcOgEa2Pb2oilM2sXsDAzKQf6MxxRyj74pyOKf5wt7FWyS1fZLTrqNFKytA6+XF/RFGBatvpD28WFjjX64rXCDgNM2XUa9HTeiIf6Q2jM+uDZ+Pvj8UDEhcUl/gBYPX9vC+WimetVtcSLoohvdoZOq3oO4ZiVG3VRKNq2fcrh6VjHzzTk288IG1E03asCPzddEvL6LAe7xGLuTOrRc8cE1";
            HaspApplication hasp1 = new HaspApplication();
            HaspFeature feature = (HaspFeature)hasp1.ProgNumDefaultFeature;
            feature.SetOptions((int)HaspFeatureOptions.haspOptionNotRemote, (int)HaspFeatureOptions.haspOptionDefault);
            feature.SetOptions((int)HaspFeatureOptions.haspOptionClassic, (int)HaspFeatureOptions.haspOptionDefault);
            Hasp hasp2 = (Hasp)hasp1.Hasp(feature);
            byte[] vc = System.Text.Encoding.GetEncoding(1251).GetBytes(vendorCode);
            vc[3] = (byte)'Y';
            int res_log = (int)hasp2.Login(System.Text.Encoding.GetEncoding(1251).GetString(vc));
            if (hasp2.IsLoggedIn())
            {
                HaspFile hf = ((HaspFile)hasp2.GetFile((int)AKSHASP.HaspFileTypes.haspFileLicense));
                hf.FilePos = 0;
                byte[] bts = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    HaspData hd = (HaspData)hf.ReadByte();
                    bts[i] = hd.Byte;
                    hf.FilePos = i + 1;
                }
                HaspData data = (HaspData)hasp2.GetSessionInfo(hasp2.KeyInfo);
                string str = data.String.ToLower();
                int index1 = str.IndexOf("<haspid>");
                int index2 = str.IndexOf("</haspid>");
                int nom = 0;
                if (index1 >= 0 && index2 > index1)
                {
                    try
                    {
                        nom = Convert.ToInt32(str.Substring(index1 + 8, index2 - index1 - 8));
                    }
                    catch
                    {
                        hasp2.Logout();
                        return false;
                    }
                }
                else
                {
                    hasp2.Logout();
                    return false;
                }
                str = HugeLib.Crypto.MyCrypto.TripleDES_DecryptData(bts, nom.ToString("00000000"), System.Security.Cryptography.CipherMode.CBC);
                try
                {
                    nom = Convert.ToInt32(HugeLib.Utils.AHex2String(str));
                }
                catch
                {
                    hasp2.Logout();
                    return false;
                }
                hf = (HaspFile)hasp2.GetFile((int)AKSHASP.HaspFileTypes.haspFileMain);
                hf.FilePos = 0;
                bts = new byte[40];
                for (int i = 0; i < 40; i++)
                {
                    HaspData hd = (HaspData)hf.ReadByte();
                    bts[i] = hd.Byte;
                    hf.FilePos = i + 1;
                }
                str = HugeLib.Crypto.MyCrypto.TripleDES_DecryptData(bts, nom.ToString("00000000"), System.Security.Cryptography.CipherMode.CBC);
                str = HugeLib.Utils.AHex2String(str);
                if (!str.StartsWith("SA"))
                {
                    hasp2.Logout();
                    return false;
                }
                Version = str.Substring(2, 8).Trim();
                License = str.Substring(10, 30).Trim();
            }
            hasp2.Logout();
            return (res_log == (int)HaspStatusCodes.haspStatusOk);
        }
        private static bool CheckLic(string l, int i)
        {
            if (i < l.Length)
                return (l[i] == '1');
            return false;
        }
        public static bool CheckDevice(DeviceType deviceT)
        {
            //			if (deviceT == DeviceT.WebCam)
            //				return true;
            if (License.Trim().Length > 0) // лицензия из ключа
            {
                if (deviceT == DeviceType.DC150 && CheckLic(License, 0))
                    return true;
                //if (deviceT == DeviceType.Emboss280 && checkLic(license, 1))
                //    return true;
                if (deviceT == DeviceType.DC450 && CheckLic(License, 3))
                    return true;
                //if (deviceT == DeviceType.SP && checkLic(license, 6))
                //    return true;
                //if (deviceT == DeviceType.Olympus && checkLic(license, 11))
                //    return true;
                //if (deviceT == DeviceType.SmartModule && checkLic(license, 10))
                //    return true;
                //if (deviceT == DeviceType.UltraGraph && checkLic(license, 2))
                //    return true;
                //if (deviceT == DeviceType.Magna && checkLic(license, 7))
                //    return true;
                //if (deviceT == DeviceType.Emboss9000 && checkLic(license, 5))
                //    return true;
                //if (deviceT == DeviceType.IC_Select && checkLic(license, 8))
                //    return true;
                //if (deviceT == DeviceT.RP90 && checkLic(license, 9))
                //    return true;
                //if (deviceT == DeviceT.Emboss450_DS && checkLic(license, 4))
                //    return true;
                //if (deviceT == DeviceT.Canon && checkLic(license, 12))
                //    return true;
                //if (deviceT == DeviceT.SE48 && checkLic(license, 13))
                //    return true;
                //if (deviceT == DeviceT.AsyncSmart && checkLic(license, 14))
                //    return true;
                //if (deviceT == DeviceT.FP && checkLic(license, 15))
                //    return true;
                //if (deviceT == DeviceT.CP && checkLic(license, 16))
                //    return true;
                if (deviceT == DeviceType.CD && CheckLic(License, 17))
                    return true;
                //if (deviceT == DeviceT.SR && checkLic(license, 18))
                //    return true;
                //if (deviceT == DeviceT.WebCam && checkLic(license, 19))
                //    return true;
                if (deviceT == DeviceType.CE && CheckLic(License, 20))
                    return true;
            }
            return false;
        }
    }
    //public class LocalizedDescriptionAttribute : DescriptionAttribute
    //{
    //    static string Localize(string key)
    //    {

    //        return this.FindResource(key).ToString();
    //    }

    //    public LocalizedDescriptionAttribute(string key)
    //        : base(Localize(key))
    //    {
    //    }
    //}
    public class EnumToItemsSource : MarkupExtension
    {
        private readonly Type _type;

        public EnumToItemsSource(Type type)
        {
            _type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {            
            object obj = _type.GetMembers().SelectMany(
                member => member.GetCustomAttributes(typeof(DisplayAttribute), true).Cast<DisplayAttribute>()).Select(x => x.Name).ToList();
            return obj;
            return _type.GetMembers().SelectMany(
                member => member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>()).Select(x => x.Description).ToList();

        }
    }
}

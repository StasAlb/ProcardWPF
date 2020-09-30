using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Neodynamic.WPF;
using ProcardWPF.Properties;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FlowDirection = System.Windows.FlowDirection;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Stretch = Neodynamic.WPF.Stretch;

namespace ProcardWPF
{
	public enum Regim
	{
		NoTask,
		Design,
		Print,
        ToPrinter,
		Debug
	}
    public enum Lang
    {
        Russian,
        English
    }
    public enum EmbossFont : int
    {
        Gothic = 1,
        Farrington = 2,
        OCR10 = 3,
        MCIndent = 4,
        OCR7 = 5,
        MCIndentFront = 6,
        OCRB = 7,
        MCIndentInvert = 8,
        IndentHelveticaWhite = 9,
        IndentHelveticaBlack = 10
    }
    public enum InTypes : int
    {
        #warning сделать поддержку разных типов, выбрать какие нужны/не нужны
        None = 1,
        Keyboard = 2,
        Auto = 3,
        Db = 4,
        Composite = 5,
        File = 6 //пока для ImageField загрузка из файла 
        //DateTime = 6,
        //Image = 7,
        //Olympus = 8,
        //Design = 9,
        //Misc = 10,
        //Canon = 11,
        //Array = 12,
        //ImageField = 13,
        //Track1 = 14,
        //Track2 = 15,
        //Track3 = 16,
        //Feedback = 17,
        //Topaz = 18,
        //WebCam = 19
    }
	public enum DBTypes : int
	{
		None = 0,
		OleText = 1,
		OleDbf = 2,
		OleExcel = 3,
		OleAccess = 4,
		SQL = 5,
		OleOracle = 6,
		OracleDP = 7,
		MyText = 8,
		ODBC = 9
	}
    public enum CompositeType : int
    {
        None = 0,
        Design = 1,
        Fixed = 2,
        DB = 3,
        Feedback = 4,
        DateTime = 5,
        Login = 6
    }
    public enum CompositeFunc : int
    {
        None = 0,
        AddChar = 1,
        SubStringPos = 2,
        SubStringStr = 3
        //Split = 3,
        //SubStringChar = 4,
        //EAN13Control = 5     
    }

    public enum ObjectType : int
    {
        None = 0,
        EmbossLine,
        EmbossText,
        MagStripe,
        SmartField,
        ImageField,
        TextField,
        TopCoat,
        Barcode,
        ReportField,
        EmbossText2,
        Card
    }
    public enum EmbossAlign : int
    {
        Left = 1,
        Right = 3
    }
    [Flags]
    //пока не получилось сделать комбобоксы из enum с учетом локализации
    public enum Rotate : int
    {
        [Display(Name = "TextRotateNo")]
        [Description("TextRotateNo")]
        None = 0,
        [Display(Name = "TextRotate90")]
        [Description("TextRotate90")]
        R90 = 1,
        [Display(Name = "TextRotate180")]
        [Description("TextRotate180")]
        R180 = 2,
        [Display(Name = "TextRotate270")]
        [Description("TextRotate270")]
        R270 = 3
    }
    public enum PrintValuesTypes : int
    {
        Text,
        Array,
        Image
    }
    public delegate void PrintTextChanged(int oid, string newText, object misc);
    public interface IEmbossField
    {
        string Shablon { get; set; }
    }
	public abstract class DesignObject : INotifyPropertyChanged
	{
		protected int id;
		public int ID
		{
			get
			{
				return id;
			}
            set
            {
                if (id != value)
                    saved = false;
                id = value;
            }
		}
		protected bool saved;
		public bool Saved
		{
			get
			{
				return saved;
			}
		}
        protected ObjectType oType;
        public ObjectType OType
        {
            get
            {
                return oType;
            }
        }
        public static bool IsEmbossField(ObjectType o)
        {
            if (o == ObjectType.EmbossText || o == ObjectType.EmbossText2)
                return true;
            return false;
        }

        protected string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (value.Trim().Length == 0)
                    throw new Exception("Поле дизайна: неправильное имя");
                if (value.Trim() != name)
                    saved = false;
                name = value.Trim();
                RaisePropertyChanged("Name");
            }
        }
        protected SideType side;
        public SideType Side
        {
            get
            {
                return side;
            }
            set
            {
                if (side != value)
                    saved = false;
                side = value;
            }
        }
        protected double x;
        public virtual double X
        {
            get
            {
                return x;
            }
            set
            {
                if (x != value)
                    saved = false;
                x = value;
                #warning добавить возможность некоторого выхода за границу карты для текста, картинки, штрихкода
                if (x + width > Card.Width)
                    x = Card.Width - width;
                if (x < 0)
                    x = 0;
                RaisePropertyChanged("X");
            }
        }

        protected double y;
        public virtual double Y
        {
            get
            {
                return y;
            }
            set
            {
                if (y != value)
                    saved = false;
                y = value;
                #warning добавить возможность некоторого выхода за границу карты для текста, картинки, штрихкода
                if (y + height > Card.Height)
                    y = Card.Height - height;
                if (y < 0)
                    y = 0;
                RaisePropertyChanged("Y");
            }
        }

	    private double mas;
        [XmlIgnore]
	    public bool DrawFlag;

	    public void SetMas(double newMas)
	    {
	        mas = newMas;
	    }
        protected double width;
        public virtual double Width
        {
            get
            {
                return width;
            }
            set
            {
                if (width != value)
                    saved = false;
                width = value;
                #warning добавить возможность некоторого выхода за границу карты для текста, картинки, штрихкода
                if (x + width > Card.Width)
                    width = Card.Width - x;
                RaisePropertyChanged("Width");
            }
        }
        protected double height;
        public virtual double Height
        {
            get
            {
                return height;
            }
            set
            {
                if (height != value)
                    saved = false;
                height = value;
                #warning добавить возможность некоторого выхода за границу карты для текста, картинки, штрихкода
                if (y + height > Card.Height)
                    height = Card.Height - y;
                if (height <= 0)
                    saved = false;
                RaisePropertyChanged("Height");
            }
        }
        /// <summary>
        /// экранный вверх
        /// </summary>
        public virtual int Top
        {
            get
            {
                return Card.ClientYToScreen(y + height, side);
            }
        }
        /// <summary>
        /// экранный низ
        /// </summary>
        public virtual int Bottom
        {
            get
            {
                return Card.ClientYToScreen(y, side);
            }
        }
        /// <summary>
        /// экранное лево
        /// </summary>
        public virtual int Left
        {
            get
            {
                return Card.ClientXToScreen(x);
            }
        }
        /// <summary>
        /// экранное право
        /// </summary>
        public virtual int Right
        {
            get
            {
                return Card.ClientXToScreen(x + width);
            }
        }
        protected int misc;
        [XmlIgnore]
        public int Misc
        {
            get
            {
                return misc;
            }
            set
            {
                misc = value;
            }
        }
        protected object inType;
        public InTypes? InType //? добавлен, чтобы разрешал вернуть null и для магнитки не был определен
        {
            get
            {
                //для магнитки есть своя inData = inTypeM
                if (oType != ObjectType.MagStripe)
                    return (InTypes)inType;
                return null;
            }
            set
            {
                inType = value;
            }
        }
        protected object inData;
        public string InData
        {
            get
            {
                //для магнитки есть своя inData = inDataM
                if (oType != ObjectType.MagStripe)
                    return (string)inData;
                return null;
            }
            set
            {
                inData = value;
            }
        }
	    protected string text;
        [XmlIgnore]
	    public string Text
	    {
	        get
	        {
	            return text;
	        }
	        set
	        {
	            text = value;
	        }
	    }
        public DesignObject()
		{
			Init();
		}
		public void Init()
		{
			id = -1;
            misc = 0;
			saved = false;
            inType = InTypes.None;
		    text = "";
		}
        public abstract void Draw(DrawingContext dc, Regim regim, bool selected, int step);

	    public virtual void Draw(DrawingVisual dv, Regim regim, bool selected, int step)
	    {
	        DrawingContext dc = dv.RenderOpen();
            Draw(dc, regim, selected, step);
	        dc.Close();
	    }
        public virtual bool IsOver(int screenX, int screenY)
        {
            //перегружено для линий эмбоссирования
            misc = -1;
            if (NearTop(screenX, screenY) && NearRight(screenX, screenY))
            {
                misc = 4;
                return true;
            }
            if (NearTop(screenX, screenY) && NearLeft(screenX, screenY))
            {
                misc = 2;
                return true;
            }
            if (NearBottom(screenX, screenY) && NearRight(screenX, screenY))
            {
                misc = 6;
                return true;
            }
            if (NearBottom(screenX, screenY) && NearLeft(screenX, screenY))
            {
                misc = 8;
                return true;
            }
            if (NearTop(screenX, screenY))
            {
                misc = 3;
                return true;
            }
            if (NearBottom(screenX, screenY))
            {
                misc = 7;
                return true;
            }
            if (NearLeft(screenX, screenY))
            {
                misc = 1;
                return true;
            }
            if (NearRight(screenX, screenY))
            {
                misc = 5;
                return true;
            }
            if (screenX > Left && screenX < Right && screenY < Bottom && screenY > Top)
            {
                misc = 0;
                return true;
            }
            return false;
        }
        protected bool NearTop(int screenX, int screenY)
        {
            if (screenX > Left - 3 && screenX < Right + 3 && screenY > Top - 3 && screenY < Top + 3)
                return true;
            return false;
        }
        protected bool NearBottom(int screenX, int screenY)
        {
            if (screenX > Left - 3 && screenX < Right + 3 && screenY > Bottom - 3 && screenY < Bottom + 3)
                return true;
            return false;
        }
        protected bool NearRight(int screenX, int screenY)
        {
            if (screenX > Right - 3 && screenX < Right + 3 && screenY < Bottom + 3 && screenY > Top - 3)
                return true;
            return false;
        }
        protected bool NearLeft(int screenX, int screenY)
        {
            if (screenX > Left - 3 && screenX < Left + 3 && screenY < Bottom + 3 && screenY > Top - 3)
                return true;
            return false;
        }
	    public void SetText(string newText)
	    {
	        text = newText;
	    }

	    public string GetText()
	    {
	        return text;
	    }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
	    public delegate void redrawCard();
        public event redrawCard RedrawCard;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            RedrawCard?.Invoke();
        }
	    #endregion
	}
    public class EmbossLine : DesignObject
    {
        public override double X
        {
            get
            {
                return base.X;
            }

            set
            {
                int cnt = 1 + Convert.ToInt32(Math.Floor((Card.Width - value - Card.FontWidth(font) / 2.0) / Card.FontDis(font)));
                if (cnt < digits.Count && digits[digits.Count - 1])
                    return;
                if (x != value)
                    saved = false;
                base.X = value;
                if (base.X < Card.Radius)
                    base.X = Card.Radius;
                if (base.X > Card.Width - Card.Radius)
                    base.X = Card.Width - Card.Radius;
                cnt = 1 + Convert.ToInt32(Math.Floor((Card.Width - x - Card.FontWidth(font) / 2.0) / Card.FontDis(font)));
                if (cnt < 0)
                    cnt = 0;
                while (cnt > digits.Count)
                    digits.Add(false);
                while (cnt < digits.Count)
                    digits.RemoveAt(digits.Count - 1);

            }
        }
        public override double Y
        {
            get
            {
                return base.Y;
            }

            set
            {
                if (y != value)
                    saved = false;
                base.Y = value;
                if (base.Y < Card.Radius)
                    base.Y = Card.Radius;
                if (base.Y > Card.Height - Card.Radius)
                    base.Y = Card.Height - Card.Radius;
            }
        }
        public override double Width
        {
            get
            {
                return 0.0;
            }
            set
            {
                return;
            }
        }
        public override double Height
        {
            get
            {
                return 0.0;
            }

            set
            {
                return;
            }
        }

        private bool registered;
        public bool Registered
        {
            get
            {
                return registered;
            }
            set
            {
                registered = value;
            }
        }      
        private List<bool> digits;
        public EmbossLine()
        {
            side = SideType.Front;
            oType = ObjectType.EmbossLine;
            font = EmbossFont.Farrington;
            InType = InTypes.None;
            firstLine = false;
            registered = true;
            digits = new List<bool>();
        }
        private EmbossFont font;
        public EmbossFont Font
        {
            get
            {
                return font;
            }
            set
            {
                if (font != value)
                    saved = false;
                font = value;
            }
        }

        public bool IsEmbossFont
        {
            get
            {
                return (font == EmbossFont.Farrington || font == EmbossFont.Gothic);
            }
        }
        private bool firstLine;
        public bool FirstLine
        {
            set
            {
                firstLine = value;
            }
            get
            {
                return firstLine;
            }
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            if (regim != Regim.Design)
                return;
            int cTop = (registered) ? Card.ClientYToScreen(Card.Height, this.side) : Card.ClientYToScreen(Y + Card.Radius, this.side);
            int cBottom = (registered) ? Card.ClientYToScreen(0, this.Side) : Card.ClientYToScreen(Y - Card.Radius, this.side);
            int cRight = (registered) ? Card.ClientXToScreen(Card.Width) : Card.ClientXToScreen(0);
            int cLeft = (registered) ? Card.ClientXToScreen(this.X) : Card.ClientXToScreen(Card.Width - this.X);

            dc.DrawLine((firstLine) ? new Pen(Brushes.Red, 1) : new Pen(Brushes.Black, 1), new System.Windows.Point(cLeft, cTop), new System.Windows.Point(cLeft, cBottom));//height);
            dc.DrawLine((firstLine) ? new Pen(Brushes.Red, 1) : new Pen(Brushes.Black, 1), new System.Windows.Point(cLeft, Card.ClientYToScreen(this.Y, this.Side)), new System.Windows.Point(cRight, Card.ClientYToScreen(this.Y, this.Side)));

            #region digits отладочный
            /*
            if (regim != Regim.Design)
                return;
            double lf = (registered) ? X : Card.Width - X;
            double tp = Y + 0.2;
            lf = lf - Card.FontWidth(Font) / 2.0;
            tp += Card.FontHeight(Font) / 2.0;
            Typeface font = null;
            int fontSize = 0;
            switch (Font)
            {
                case (EmbossFont.Farrington):
                case (EmbossFont.OCR7):
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 13;
                    break;
                case (EmbossFont.MCIndentInvert):
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Oblique, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 10;
                    break;
                default:
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 10;
                    break;
            }
            for (int i = 0; i < GetDigitsCount(); i++)
            {
                dc.DrawRectangle(Brushes.White, new Pen(Brushes.Gray, 1), new Rect(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(Card.FontWidth(Font)), Card.ClientToScreen(Card.FontHeight(Font))));
                if (CheckDigit(i, 1))
                    dc.DrawText(new FormattedText("0", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                else
                    dc.DrawText(new FormattedText("1", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                lf += Card.FontDis(Font);
            }
            */
            #endregion
        }
        public override bool IsOver(int screenX, int screenY)
        {
            misc = -1;
            double cTop = Card.ClientYToScreen(Card.Height, this.side);
            double cBottom = Card.ClientYToScreen(0, this.Side);
            double cRight = (registered) ? Card.ClientXToScreen(Card.Width) : Card.ClientXToScreen(Card.Width - this.X);
            double cLeft = (registered) ? Left : Card.ClientXToScreen(0);
            if (screenX > ((registered) ? cLeft : cRight) - 3 && screenX < ((registered) ? cLeft : cRight) + 3 && screenY < cBottom && screenY > cTop)
            {
                misc = 9;
                return true;
            }
            if (screenX > cLeft - 3 && screenX < cRight + 3 && screenY > Bottom - 3 && screenY < Bottom + 3)
            {
                misc = 10;
                return true;
            }
            return false;
        }
        public int GetFreeDigit()
        {
            for (int i = 0; i < digits.Count; i++)
                if (!digits[i])
                    return i;
            return -1;
        }
        public bool CheckDigit(int start, int len)
        {
            if (start < 0)
                return false;
            if (start + len > digits.Count)
                return false;
            for (int i = start; i < start + len; i++)
            {
                if (digits[i])
                    return false;
            }
            return true;
        }
        public void SetDigits(int startPos, int Len, bool value)
        {
            for (int i = startPos; i < startPos + Len && i < digits.Count; i++)
                digits[i] = value;
        }
        public int GetDigitsCount()
        {
            return digits.Count;
        }
        public void SetRegistered(Devices.DoubleIndent di)
        {
            registered = true;
            if (di == Devices.DoubleIndent.None)
                return;
            if (!IsEmbossFont)
                registered = ((int)side == (int)di);
        }
    }
    public class Barcode : DesignObject
    {
        private string shablon;
        public string Shablon
        {
            get
            {
                return shablon;
            }
            set
            {
                if (shablon != value)
                    saved = false;
                shablon = value;
                RaisePropertyChanged("Shablon");
            }
        }
        private Neodynamic.WPF.Symbology symbology;
        private Color barColor;
        public Color BarColor
        {
            get
            {
                return barColor;
            }
            set
            {
                if (value != barColor)
                    saved = false;
                barColor = value;
                RaisePropertyChanged("BarColor");
            }
        }
        private Color textColor;
        public Color TextColor
        {
            get
            {
                return textColor;
            }
            set
            {
                if (value != textColor)
                    saved = false;
                textColor = value;
                RaisePropertyChanged("TextColor");
            }
        }
        private Color backColor;
        public Color BackColor
        {
            get
            {
                return backColor;
            }
            set
            {
                if (value != backColor)
                    saved = false;
                backColor = value;
                RaisePropertyChanged("BackColor");
            }
        }
        private MyFont font;
        public MyFont Font
        {
            get { return font; }
            set
            {
                if (font != value)
                    saved = false;
                font = value;
                RaisePropertyChanged("FontString");
            }
        }

        private double barWidth;
        public double BarWidth
        {
            get { return barWidth; }
            set
            {
                if (value != barWidth)
                    saved = false;
                barWidth = value;
                RaisePropertyChanged("BarWidth");
            }
        }
        private double barHeight;
        public double BarHeight
        {
            get { return barHeight; }
            set
            {
                if (value != barHeight)
                    saved = false;
                barHeight = value;
                RaisePropertyChanged("BarHeight");
            }
        }
        private double barRatio;
        public double BarRatio
        {
            get { return barRatio; }
            set
            {
                if (value != barRatio)
                    saved = false;
                barRatio = value;
                RaisePropertyChanged("BarRatio");
            }
        }

        private bool addControl;

        public bool AddControl
        {
            get { return addControl; }
            set
            {
                if (addControl != value)
                    saved = false;
                addControl = value;
                RaisePropertyChanged("AddControl");
            }
        }

        private bool addStartStop;

        public bool AddStartStop
        {
            get { return addStartStop; }
            set
            {
                if (addStartStop != value)
                    saved = false;
                addStartStop = value;
                RaisePropertyChanged("AddStartStop");
            }
        }

        private bool showText;

        public bool ShowText
        {
            get { return showText; }
            set
            {
                if (showText != value)
                    saved = false;
                showText = value;
                RaisePropertyChanged("ShowText");
            }
        }

        private bool fitToField;

        public bool FitToField
        {
            get { return fitToField; }
            set
            {
                if (fitToField != value)
                    saved = false;
                fitToField = value;
                RaisePropertyChanged("FitToField");
            }
        }

        private string header;

        public string Header
        {
            get { return header; }
            set
            {
                if (header != value)
                    saved = false;
                header = value;
                RaisePropertyChanged("Header");
            }
        }

        private MyFont headerFont;

        public MyFont HeaderFont
        {
            get { return headerFont; }
            set
            {
                if (headerFont != value)
                    saved = false;
                headerFont = value;
                RaisePropertyChanged("HeaderFontString");
            }
        }

        private Color headerColor;

        public Color HeaderColor
        {
            get { return headerColor; }
            set
            {
                if (headerColor != value)
                    saved = false;
                headerColor = value;
                RaisePropertyChanged("HeaderColor");
            }
        }

        public string FontString
        {
            get { return $"{font.FontName}, {font.FontSize}pt"; }
        }

        public string HeaderFontString
        {
            get { return $"{headerFont.FontName}, {headerFont.FontSize}pt"; }
        }

        public Neodynamic.WPF.Symbology Symbology
        {
            get { return symbology; }
            set
            {
                if (value != symbology)
                    saved = false;
                symbology = value;
                RaisePropertyChanged("Symbology");
            }
        }
        public Barcode()
        {
            side = SideType.Front;
            oType = ObjectType.Barcode;
            shablon = "123456789";
            symbology = Symbology.Code128;
            barColor = Colors.Black;
            backColor = Colors.White;
            textColor = Colors.Black;
            barHeight = 1;
            barWidth = 0.01;
            barRatio = 1;
            font = new MyFont();
            addStartStop = true;
            addControl = true;
            showText = false;
            fitToField = false;
            header = "";
            headerFont = new MyFont();
            headerColor = Colors.Black;
        }

        Neodynamic.WPF.BarcodeProfessional barCode = new Neodynamic.WPF.BarcodeProfessional();
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            // пусть сперва чуть увеличатся
            if (width < 0 || height < 0)
                return;

            int wd = Card.ClientToScreen(Width), ht = Card.ClientToScreen(Height);
            if (regim == Regim.Design)
            {
                wd = (wd < 0) ? 0 : wd;
                ht = (ht < 0) ? 0 : ht;
                if (selected)
                {
                    dc.DrawRectangle(Params.HatchBrush(), null,
                        new Rect(Card.ClientXToScreen(X) - 5, Card.ClientYToScreen(Y + Height, Side) - 5,
                            wd + 10, ht + 10));
                    dc.DrawRectangle(Brushes.White, null,
                        new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side),
                            wd, ht));
                }
            }
            Neodynamic.WPF.BarcodeProfessional.LicenseKey = "QS84LKU5GT2G2PX3HMPNEXHT8ERZ7U2WCUBDBARYMJ7KKPSLJCWQ";
            Neodynamic.WPF.BarcodeProfessional.LicenseOwner = "ОСТПАК Т-Ultimate Edition-Developer License";
            barCode.ErrorBehavior = ErrorBehavior.BlankImage;
            barCode.BarcodeUnit = BarcodeUnit.Inch;
            barCode.Width = width;
            barCode.Height = height;
            barCode.Symbology = symbology;
            barCode.BarColor = barColor;
            barCode.Background = new SolidColorBrush(backColor);
            barCode.TextForeground = new SolidColorBrush(textColor);
            barCode.FontFamily = new FontFamily(font.FontName);
            barCode.Foreground = new SolidColorBrush(textColor);
            barCode.TextFontStyle = font.FontStyle;
            barCode.FontSize = font.FontSize;
            barCode.BarWidth = barWidth;
            barCode.BarHeight = barHeight;
            barCode.BarRatio = barRatio;
            barCode.Code = (regim == Regim.Design) ? shablon : text;
            barCode.AddChecksum = addControl;
            barCode.DisplayStartStopChar = addStartStop;
            barCode.DisplayCode = showText;
            barCode.Text = header;
            barCode.TextFontFamily = new FontFamily(headerFont.FontName);
            barCode.TextFontStyle = headerFont.FontStyle;
            barCode.TextFontSize = headerFont.FontSize;
            barCode.TextForeground = new SolidColorBrush(headerColor);
            if (fitToField)
            {
                double hhh = 0;
                if (!String.IsNullOrEmpty(barCode.Text))
                    hhh += headerFont.MeasureText(header).Height;
                if (barCode.DisplayCode)
                    hhh += font.MeasureText(barCode.Code).Height;
                hhh = Height - Card.ScreenToClient((int)hhh);
                barCode.FitBarcodeToSize = new Size(Width > 0 ? Width : 0, hhh > 0 ? hhh : 0);
            }
            else
                barCode.FitBarcodeToSize = Size.Empty;
            barCode.QuietZone = new Thickness(0, 0, 0, 0);
            barCode.MinWidth = width;
            barCode.MaxWidth = width;
            barCode.MinHeight = height;
            barCode.MaxHeight = height;
            dc.PushTransform(
                    new TranslateTransform(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, side)));
                dc.DrawDrawing(barCode.GetBarcodeDrawing());
        }
    }
    public class TextField : DesignObject
    {
        private string shablon;
        public string Shablon
        {
            get
            {
                return shablon;
            }
            set
            {
                if (shablon != value)
                    saved = false;
                shablon = value;
                RaisePropertyChanged("Shablon");
            }
        }

        private MyFont font;
        public MyFont Font
        {
            get { return font; }
            set
            {
                if (font != value)
                    saved = false;
                font = value;
                RaisePropertyChanged("FontString");
            }
        }
        public string FontString
        {
            get { return $"{font.FontName}, {font.FontSize}pt"; }
        }        
        private Brush fontBrush
        {
            get
            {
                return new SolidColorBrush(textColor);
            }
        }
        private Brush backgroundBrush
        {
            get
            {
                return new SolidColorBrush(backColor);
            }
        }
        private Color textColor;
        public Color TextColor
        {
            get
            {
                return textColor;
            }
            set
            {
                if (value != textColor)
                    saved = false;
                textColor = value;
                RaisePropertyChanged("TextColor");
            }
        }
        private Color backColor;
        public Color BackColor
        {
            get
            {
                return backColor;
            }
            set
            {
                if (value != backColor)
                    saved = false;
                backColor = value;
                RaisePropertyChanged("BackColor");
            }
        }
        private bool isTransparent;
        public bool IsTransparent
        {
            get
            {
                return isTransparent;
            }
            set
            {
                if (value != isTransparent)
                    saved = false;
                isTransparent = value;
                RaisePropertyChanged("IsTransparent");
            }
        }

        // не смог пока сделать заполнение комбобоксов из enum с учетом локализации и поддержкой binding
        // поэтому пока что заполняю их вручную при установке языка и binding через доп. интовское поле
        private TextAlignment hAlignment;        
        public TextAlignment HAlignment
        {
            get { return hAlignment; }
            set
            {
                if (hAlignment != value)
                    saved = false;
                hAlignment = value;
                RaisePropertyChanged("HAlignmentBinding");
            }
        }
        [XmlIgnore]
        public int HAlignmentBinding
        {
            get
            {
                return (int)hAlignment;
            }
            set
            {
                if ((int)hAlignment != value)
                    saved = false;
                switch(value)
                {
                    case (int)TextAlignment.Left:
                        hAlignment = TextAlignment.Left;
                        break;
                    case (int)TextAlignment.Right:
                        hAlignment = TextAlignment.Right;
                        break;
                    case (int)TextAlignment.Center:
                        hAlignment = TextAlignment.Center;
                        break;
                }
                RaisePropertyChanged("HAlignmentBinding");
            }
        }
        private Rotate textRotate;
        public Rotate TextRotate
        {
            get { return textRotate; }
            set
            {
                if (textRotate != value)
                    saved = false;
                textRotate = value;
                RaisePropertyChanged("TextRotateBinding");
            }
        }

        public int TextRotateBinding
        {
            get
            {
                return (int) textRotate;
            }
            set
            {
                if ((int) textRotate != value)
                    saved = false;
                switch (value)
                {
                    case (int)Rotate.None:
                        textRotate = Rotate.None;
                        break;
                    case (int)Rotate.R90:
                        textRotate = Rotate.R90;
                        break;
                    case (int)Rotate.R180:
                        textRotate = Rotate.R180;
                        break;
                    case (int)Rotate.R270:
                        textRotate = Rotate.R270;
                        break;
                }
                RaisePropertyChanged("TextRotateBinding");

            }
        }
        public TextField()
        {
            side = SideType.Front;
            oType = ObjectType.TextField;
            font = new MyFont();
            textColor = Colors.Black;
            backColor = Colors.White;
            hAlignment = TextAlignment.Left;
            textRotate = Rotate.None;
            isTransparent = true;
            text = "";
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            //поскольку для Text перегружена Draw(DrawingVisua...), то эта функция не нужна
            //а след. Draw перегружена для поворота
        }
        public override void Draw(DrawingVisual dv, Regim regim, bool selected, int step)
        {
            string textToDraw = (regim == Regim.Design) ? shablon : text;
            DrawingContext dc = dv.RenderOpen();
            FormattedText ft = new FormattedText(textToDraw, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font.GetTypeface(), font.FontSize * 96.0 / 72.0, fontBrush);
            ft.TextAlignment = (TextAlignment)hAlignment;
            if (font.IsStrikeout || font.IsUnderline)
            {
                TextDecorationCollection tdc = new TextDecorationCollection(1);
                if (font.IsUnderline)
                    tdc.Add(TextDecorations.Underline);
                if (font.IsStrikeout)
                    tdc.Add(TextDecorations.Strikethrough);
                ft.SetTextDecorations(tdc);
            }
            if (width > 0)
                ft.MaxTextWidth = Card.ClientToScreen(Width);
            if (Card.ClientToScreen(Height) > 0)
                ft.MaxTextHeight = Card.ClientToScreen(Height);
            int wd = Card.ClientToScreen(Width), ht = Card.ClientToScreen(Height);
            wd = (wd < 0) ? 0 : wd;
            ht = (ht < 0) ? 0 : ht;
            if (regim == Regim.Design)
            {
                if (selected)
                {
                    dc.DrawRectangle(Params.HatchBrush(), null,
                        new Rect(Card.ClientXToScreen(X) - 5, Card.ClientYToScreen(Y + Height, Side) - 5,
                            wd + 10,  ht + 10));
                    dc.DrawRectangle(Brushes.White, null,
                        new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side),
                            wd, ht));
                }
            }
            if (!isTransparent)
                dc.DrawRectangle(backgroundBrush, null,
                    new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side),
                        wd, ht));
            Point location = new Point();
            switch (textRotate)
            {
                case Rotate.None:
                    location = new Point(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, side));
                    break;
                case Rotate.R90:
                    if (width > 0)
                        ft.MaxTextHeight = Card.ClientToScreen(Width);
                    if (Card.ClientToScreen(Height) > 0)
                        ft.MaxTextWidth = Card.ClientToScreen(Height);
                    location = new Point(Card.ClientXToScreen(X + Width), Card.ClientYToScreen(Y + Height, side));
                    dc.PushTransform(new RotateTransform(90, Card.ClientXToScreen(X + Width), Card.ClientYToScreen(Y + Height, side)));
                    break;
                case Rotate.R180:
                    location = new Point(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, side));
                    dc.PushTransform(new RotateTransform(180, Card.ClientXToScreen(X + Width / 2), Card.ClientYToScreen(Y + Height / 2, side)));
                    break;
                case Rotate.R270:
                    if (width > 0)
                        ft.MaxTextHeight = Card.ClientToScreen(Width);
                    if (Card.ClientToScreen(Height) > 0)
                        ft.MaxTextWidth = Card.ClientToScreen(Height);
                    location = new Point(Card.ClientXToScreen(X), Card.ClientYToScreen(Y, side));
                    dc.PushTransform(new RotateTransform(-90, Card.ClientXToScreen(X), Card.ClientYToScreen(Y, side)));
                    break;
            }
            dc.DrawText(ft, location);
            if (textRotate != Rotate.None)
                dc.Pop();
            dc.Close();
        }

    }
    public class ImageField : DesignObject
    {
        public ImageField()
        {
            side = SideType.Front;
            oType = ObjectType.ImageField;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            int wd = Card.ClientToScreen(Width), ht = Card.ClientToScreen(Height);
            wd = (wd < 0) ? 0 : wd;
            ht = (ht < 0) ? 0 : ht;
            if (regim == Regim.Design)
            {
                if (selected)
                {
                    dc.DrawRectangle(Params.HatchBrush(), null,
                        new Rect(Card.ClientXToScreen(X) - 5, Card.ClientYToScreen(Y + Height, Side) - 5,
                            wd + 10, ht + 10));
                    dc.DrawRectangle(Brushes.White, null,
                        new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side),
                            wd, ht));
                }
            }
            if (text.Length > 0)
            {
                Rect drawRect = new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side), wd, ht);
                try
                {
                    BitmapImage bi = new BitmapImage(new Uri(text));
                    dc.DrawImage(bi, drawRect);
                }
                catch
                {
                }
            }
        }
    }
    public class TopCoat : DesignObject
    {
        public TopCoat()
        {
            side = SideType.Front;
            oType = ObjectType.TopCoat;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            if (regim == Regim.Design)
            {
                int wd = Card.ClientToScreen(Width), ht = Card.ClientToScreen(Height);
                wd = (wd < 0) ? 0 : wd;
                ht = (ht < 0) ? 0 : ht;
                if (selected)
                {
                    dc.DrawRectangle(Params.HatchBrush(), null,
                        new Rect(Card.ClientXToScreen(X) - 5, Card.ClientYToScreen(Y + Height, Side) - 5,
                            wd + 10, ht + 10));
                    dc.DrawRectangle(Brushes.White, null,
                        new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side),
                            wd, ht));
                }
            }
        }
    }
    public class MagStripe : DesignObject
    {
        public InTypes[] InTypeM
        {
            get
            {
                return ((InTypes[])inType);
            }
            set
            {
                inType = value;
            }
        }
        public string[] InDataM
        {
            get
            {
                return ((string[])inData);
            }
            set
            {
                inData = value;
            }
        }
        private string[] textM;
        [XmlIgnore]
        public string[] TextM
        {
            get
            {
                return textM;
            }
        }
        private bool feedback;
        public bool Feedback
        {
            get
            {
                return feedback;
            }
            set
            {
                feedback = value;
            }
        }
        //[NonSerialized]
        //public string[] Tracks;
        public MagStripe()
        {
            side = SideType.Back;
            oType = ObjectType.MagStripe;
            x = 0; width = Card.Width;
            y = (Params.UseMetric) ? 1.45 * 25.4 : 1.45;
            height = (Params.UseMetric) ? 0.51 * 25.4 : 0.51;
            inType = new InTypes[3] { InTypes.None, InTypes.None, InTypes.None };
            inData = new string[3] { "", "", "" };
            textM = new string[3] { "", "", "" };
            //Tracks = new string[] { "", "", "" };
            feedback = false;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            if (regim == Regim.Design)
                dc.DrawRectangle(Brushes.Black, null, new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side), Card.ClientToScreen(Width), Card.ClientToScreen(Height)));
        }
        public void SetText(string newText, int index)
        {
            textM[index] = newText;
        }
        public string GetText(int index)
        {
            return textM[index];
        }
    }
    public class EmbossText : DesignObject, IEmbossField
    {
        public override double X
        {
            get
            {
                return parent.X + Card.FontDis(parent.Font) * position;
            }
            set
            {
                base.X = value;
            }
        }
        public override double Y
        {
            get
            {
                return parent.Y;
            }

            set
            {
                base.Y = value;
            }
        }
        public override double Height
        {
            get
            {
                return Card.FontHeight(parent.Font);
            }
            set { }
        }
        public override double Width
        {
            get
            {
                return Card.FontDis(parent.Font)*shablon.Length;
            }
            set { }
        }
        private int position;
        public int Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value != position)
                    saved = false;
                position = value;
                if (parent != null)
                    x = parent.X + Card.FontDis(parent.Font) * position;
            }
        }
        private string shablon;
        public string Shablon
        {
            get
            {
                return shablon;
            }
            set
            {
                if (value != shablon)
                    saved = false;
                shablon = value;
            }
        }
        private EmbossLine parent;
        [XmlIgnore]
        public EmbossLine Parent
        {
            set
            {
                ParentID = ((EmbossLine)value).ID;
                parent = value;
            }
            get
            {
                return parent;
            }
        }
        private int parentID;
        public int ParentID
        {
            get
            {
                return parentID;
            }
            set
            {
                parentID = value;
            }
        }
        private EmbossAlign align;
        public EmbossAlign Align
        {
            get
            {
                return align;
            }
            set
            {
                align = value;
            }
        }
        private string text;
        public string Text
        {
            get
            {
                return text;
            }
        }
        public bool IsEmbossFont
        {
            get
            {
                return Parent.IsEmbossFont;
            }
        }
        public EmbossText()
        {
            oType = ObjectType.EmbossText;
            align = EmbossAlign.Left;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            double lf = (parent.Registered) ? X : Card.Width - X;
            double tp = Y;
            lf = (parent.Registered) ? lf - Card.FontWidth(parent.Font) / 2.0 : lf - Card.FontWidth(parent.Font) / 2.0 - Card.FontDis(parent.Font) * (shablon.Length - 1);
            tp += Card.FontHeight(parent.Font) / 2.0;
            Typeface font = null;
            int fontSize = 0;
            switch (parent.Font)
            {
                case (EmbossFont.Farrington):
                case (EmbossFont.OCR7):
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 13;
                    break;
                case (EmbossFont.MCIndentInvert):
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Oblique, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 10;
                    break;
                default:
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 10;
                    break;
            }
            if (selected)
                dc.DrawRectangle(Params.HatchBrush(), null, new Rect(Card.ClientXToScreen(lf) - 3, Card.ClientYToScreen(tp, Side) - 3, Card.ClientToScreen(Width) + 4, Card.ClientToScreen(Height) + 6));
            for (int i = 0; i < shablon.Length; i++)
            {
                if (regim == Regim.Design)
                { 
                    dc.DrawRectangle(Brushes.White, new Pen(Brushes.Gray, 1), new Rect(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(Card.FontWidth(parent.Font)), Card.ClientToScreen(Card.FontHeight(parent.Font))));
                    dc.DrawText(new FormattedText(shablon[i].ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                }
                if (regim == Regim.Print && i < text.Length)
                {
                    dc.DrawText(new FormattedText(text[i].ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                }
                
                lf += Card.FontDis(parent.Font);
            }
        }
        public override bool IsOver(int screenX, int screenY)
        {
            misc = -1;
            if (NearLeft(screenX, screenY))
            {
                misc = (Parent.Registered) ? 1 : 5; //для незарегестрированных меняем лево и право
                return true;
            }
            if (NearRight(screenX, screenY))
            {
                misc = (Parent.Registered) ? 5 : 1; //для незарегестрированных меняем лево и право
                return true;
            }
            if (screenX > Left && screenX < Right && screenY < Bottom && screenY > Top)
            {
                misc = 0;
                return true;
            }
            return false;
        }
        public override int Left
        {
            get
            {
                return (parent.Registered) ? Card.ClientXToScreen(X - Card.FontWidth(parent.Font) / 2.0) : Card.ClientXToScreen(Card.Width - X - Card.FontDis(parent.Font) * (shablon.Length - 0.5));
            }
        }
        public override int Right
        {
            get
            {
                return (parent.Registered) ? Card.ClientXToScreen(X + Card.FontDis(parent.Font) * (shablon.Length - 0.5)) : Card.ClientXToScreen(Card.Width - X + Card.FontWidth(parent.Font) / 2.0);
            }
        }
        public override int Top
        {
            get
            {
                return Card.ClientYToScreen(Y + Card.FontHeight(parent.Font) / 2.0, side);
            }
        }
        public override int Bottom
        {
            get
            {
                return Card.ClientYToScreen(Y - Card.FontHeight(parent.Font) / 2.0, side);
            }
        }
    }
    public class EmbossText2 : DesignObject, IEmbossField
    {
        public override double Height
        {
            get
            {
                return Card.FontHeight(font);
            }
            set { }
        }
        public override double Width
        {
            get
            {
                return Card.FontDis(font) * shablon.Length;
            }
            set { }
        }

        public string ShablonLength => $"{Application.Current.FindResource("EmbossText_Length")} {shablon.Length}";
        private EmbossFont font;
        public EmbossFont Font
        {
            get
            {
                return font;
            }
            set
            {
                if (value != font)
                    saved = false;
                font = value;
                side = Card.FontSide(font);
                RaisePropertyChanged("Font");
            }
        }
        private string shablon;
        public string Shablon
        {
            get
            {
                return shablon;
            }
            set
            {
                if (value != shablon)
                    saved = false;
                shablon = value;
                RaisePropertyChanged("Width");
                RaisePropertyChanged("Shablon");
                RaisePropertyChanged("ShablonLength");
            }
        }
        private EmbossAlign align;
        public EmbossAlign Align
        {
            get
            {
                return align;
            }
            set
            {
                align = value;
            }
        }
        public EmbossText2()
        {
            oType = ObjectType.EmbossText2;
            font = EmbossFont.Farrington;
            align = EmbossAlign.Left;
        }
        public EmbossText2(int embossLineID)
        {
            oType = ObjectType.EmbossText2;
            font = EmbossFont.Farrington;
            align = EmbossAlign.Left;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            double lf = X;
            double tp = Y;
            lf = lf - Card.FontWidth(font) / 2.0;
            tp += Card.FontHeight(font) / 2.0;
            Typeface fontText = null;
            int fontSize = 0;
            switch (font)
            {
                case (EmbossFont.Farrington):
                case (EmbossFont.OCR7):
                    fontText = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 13;
                    break;
                case (EmbossFont.MCIndentInvert):
                    fontText = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Oblique, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 10;
                    break;
                default:
                    fontText = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Heavy, FontStretches.Normal);
                    fontSize = 10;
                    break;
            }
            if (selected)
                dc.DrawRectangle(Params.HatchBrush(), null, new Rect(Card.ClientXToScreen(lf) - 3, Card.ClientYToScreen(tp, Side) - 3, Card.ClientToScreen(Width) + 4, Card.ClientToScreen(Height) + 6));
            for (int i = 0; i < shablon.Length; i++)
            {
                if (regim == Regim.Design)
                {
                    dc.DrawRectangle(Brushes.White, new Pen(Brushes.Gray, 1), new Rect(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(Card.FontWidth(font)), Card.ClientToScreen(Card.FontHeight(font))));
                    dc.DrawText(new FormattedText(shablon[i].ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontText, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                }
                if (regim == Regim.Print && i < text.Length)
                {
                    dc.DrawText(new FormattedText(text[i].ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontText, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                }
                lf += Card.FontDis(font);
            }
            if (regim == Regim.ToPrinter)
            {
                bool reverse = false;
                string sReverse = "";
                /* раньше мы переворачивали задние индентные шрифты и FC4 и FC8
                 * На прошивке D3.17.4-6 FC4 переворачивать не надо, а FC8 надо
                 * теперь так, плюс сделал настройку в Procard.xml для любого шрифта можно принудительно сказать переворачивать или нет
                 */
                if (font == EmbossFont.MCIndent || font == EmbossFont.MCIndentInvert)
                    reverse = true;
                //sReverse = HugeLib.XmlClass.GetXmlAttribute(xmlSettings, "Embosser/Font", "Name", String.Format("FC{0}", (int)et.Font), "Reverse", xnmSettings);

                dc.DrawText(new FormattedText($"~EM%{(int)font};{x * 1000:0000};{y * 1000:0000};{text}", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontText, fontSize, Brushes.Black), new Point(10, 10));
            }
//            dc.DrawText(string.Format("~EM%{0};{1:0000};{2:0000};{3}", (int)font, Convert.ToInt32(x * 1000), Convert.ToInt32(y * 1000), text), font, Brushes.Black, 50, 50);
        }
        public override bool IsOver(int screenX, int screenY)
        {
            misc = -1;
            if (NearLeft(screenX, screenY))
            {
                misc = 1;
                return true;
            }
            if (NearRight(screenX, screenY))
            {
                misc = 5;
                return true;
            }
            if (screenX > Left && screenX < Right && screenY < Bottom && screenY > Top)
            {
                misc = 0;
                return true;
            }
            return false;
        }
        public override int Left
        {
            get
            {
                return Card.ClientXToScreen(X - Card.FontWidth(font) / 2.0);
            }
        }
        public override int Right
        {
            get
            {
                return Card.ClientXToScreen(X + Card.FontDis(font) * (shablon.Length - 0.5));
            }
        }

        public override int Top
        {
            get
            {
                return Card.ClientYToScreen(Y + Card.FontHeight(font) / 2.0, side);
            }
        }
        public override int Bottom
        {
            get
            {
                return Card.ClientYToScreen(Y - Card.FontHeight(font) / 2.0, side);
            }
        }
    }
    public class SmartField : DesignObject
    {
        private bool feedback;
        public bool Feedback
        {
            get
            {
                return feedback;
            }
            set
            {
                feedback = value;
            }
        }
        [XmlElement(Type = typeof(SmartModule.OstcardStandard))]//, XmlElement(Type = typeof(SmartModule.SmartModule))]
        public SmartModule.SmartModule SModule;
        public int SmartType
        {
            get
            {
                if (SModule == null)
                    return (int)SmartModule.SmartType.None;
                return (int)SModule.SType;
            }
            set { }
        }
        public string SmartTypeIdString
        {
            get
            {
                if (SModule == null)
                    return String.Format("{0}", (int)SmartModule.SmartType.None);
                return String.Format("{0}", (int)SModule.SType);
            }
            set
            {
                int tp = 0;
                try
                {
                    tp = Convert.ToInt32(value);
                }
                catch
                {
                    tp = 0;
                }
                if (SModule == null || tp != (int)SModule.SType)
                {
                    switch (tp)
                    {
                        case ((int)SmartModule.SmartType.OstcardStandard):
                            SModule = new SmartModule.OstcardStandard();
                            break;
                        case ((int)SmartModule.SmartType.None):
                        default:
                            SModule = null;
                            break;
                    }
                }
            }
        }
        public string SmartTitle
        {
            get
            {
                if (SModule == null)
                    return (string)Application.Current.FindResource("NotDefined");
                return  SModule.Desc;
            }
            set { }
        }
        public SmartField()
        {
            side = SideType.Front;
            oType = ObjectType.SmartField;
            x = (Params.UseMetric) ? 9 : 9 / 25.4;
            y = (Params.UseMetric) ? 24 : 24 / 25.4;
            width = (Params.UseMetric) ? 14 : 14 / 25.4;
            height = (Params.UseMetric) ? 11.5 : 11.5 / 25.4;
            feedback = false;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            if (regim == Regim.Design)
            {
                float dx = (Params.UseMetric) ? (float) 4 : (float) 4.0 / (float) 25.4;
                float dy = (Params.UseMetric) ? (float) 3 : (float) 3.0 / (float) 25.4;
                PathGeometry pg = new PathGeometry();
                pg.FillRule = FillRule.Nonzero;
                PathFigure pf = new PathFigure();
                pf.IsClosed = false;
                pf.StartPoint = new Point(Card.ClientXToScreen(X + dx), Card.ClientYToScreen(Y, Side));
                pg.Figures.Add(pf);
                pf.Segments.Add(
                    new LineSegment(new Point(Card.ClientXToScreen(X + Width - dx), Card.ClientYToScreen(Y, Side)),
                        true));
                pf.Segments.Add(new ArcSegment(
                    new Point(Card.ClientXToScreen(X + Width), Card.ClientYToScreen(Y + dy, Side)),
                    new Size(Card.ClientToScreen(dx), Card.ClientToScreen(dy)), 0, false,
                    SweepDirection.Counterclockwise, true));
                pf.Segments.Add(new LineSegment(
                    new Point(Card.ClientXToScreen(X + Width), Card.ClientYToScreen(Y + Height - dy, Side)), true));
                pf.Segments.Add(new ArcSegment(
                    new Point(Card.ClientXToScreen(X + Width - dx), Card.ClientYToScreen(Y + Height, Side)),
                    new Size(Card.ClientToScreen(dx), Card.ClientToScreen(dy)), 0, false,
                    SweepDirection.Counterclockwise, true));
                pf.Segments.Add(new LineSegment(
                    new Point(Card.ClientXToScreen(X + dx), Card.ClientYToScreen(Y + Height, Side)), true));
                pf.Segments.Add(new ArcSegment(
                    new Point(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height - dy, Side)),
                    new Size(Card.ClientToScreen(dx), Card.ClientToScreen(dy)), 0, false,
                    SweepDirection.Counterclockwise, true));
                pf.Segments.Add(new LineSegment(new Point(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + dy, Side)),
                    true));
                pf.Segments.Add(new ArcSegment(new Point(Card.ClientXToScreen(X + dx), Card.ClientYToScreen(Y, Side)),
                    new Size(Card.ClientToScreen(dx), Card.ClientToScreen(dy)), 0, false,
                    SweepDirection.Counterclockwise, true));
                dc.DrawGeometry(Brushes.Yellow, new Pen(Brushes.Black, 1), pg);
            }
        }
    }
    public class ReportField : DesignObject
    {
        public ReportField()
        {
        }
        public override void Draw(DrawingContext gr, Regim regim, bool selected, int step)
        {
        }
    }

    public class Card
    {
        public ObjectArray objects = null;
        //public DBConnection dbConn;
        public static double Height
        {
            get
            {
                return (useMetric) ? 53.98f : 2.125f;
            }
            set
            {
                return;
            }
        }
        public static double Width
        {
            get
            {
                return (useMetric) ? 85.72f : 3.375f;
            }
            set
            {
                return;
            }
        }
        public static float Radius
        {
            get
            {
                return (useMetric) ? 3.18f : 0.125f;
            }
        }
        private static float radius
        {
            get
            {
                return Card.Radius;
            }
        }
        private static int left, top_f, top_r;
        private static float mas;
        private bool block;
        public bool Block
        {
            get
            {
                return block;
            }
            set
            {
                if (block != value)
                    saved = false;
                block = value;
            }
        }
        private bool saved;
        public bool Saved
        {
            get
            {
                bool res = saved;
                for (int i = 0; i < objects.Count; i++)
                    if (!objects[i].Saved)
                        res = false;
                return res;
            }
        }
        private static bool useMetric;
        public bool UseMetric
        {
            get
            {
                return useMetric;
            }
            set
            {
                if (useMetric != value)
                    saved = false;
                useMetric = value;
            }
        }
        public void SetMas(float new_mas)
        {
            mas = (useMetric) ? new_mas / 25.4f : new_mas;
        }
        private bool landscape;
        /// <summary>
        /// 
        /// </summary>
        public bool Landscape
        {
            get
            {
                return landscape;
            }
            set
            {
                if (landscape != value)
                    saved = false;
                landscape = value;
            }
        }
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                    saved = false;
                name = value;
            }
        }
        private Database dbIn;
        public Database DbIn
        {
            get
            {
                return dbIn;
            }
            set
            {
                dbIn = value;
            }
        }
        private Database dbOut;
        public Database DbOut
        {
            get
            {
                return dbOut;
            }
            set
            {
                dbOut = value;
            }
        }
        [XmlElement(Type = typeof(Devices.DC450)), XmlElement(Type = typeof(Devices.DC150)), XmlElement(Type = typeof(Devices.Simulator)), 
            XmlElement(Type = typeof(Devices.XPSPrinter))]
        public Devices.DeviceClass device = null;
        [XmlIgnore]
        public int SelectedID;
        private int maxID;
        public Card()
        {
            block = false;
            objects = new ObjectArray();
            landscape = true;
            saved = false;
            maxID = 0;
            dbIn = null;
            dbOut = null;
        }
        public int GetNextID()
        {
            return ++maxID;
        }
        public void SetMaxID()
        {
            maxID = 0;
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].ID > maxID)
                    maxID = objects[i].ID;
        }
        public ArrayList CompileMessage(int nom)
        {
            if (device == null)
                return null;
            ArrayList al = new ArrayList();
            string dcl = String.Format("{0:000000}#DCL#08", nom+1);
            string dcc = String.Format("{0:000000}#DCC#", nom+1); 
            int t = 0;
            bool wasDcl = false;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].OType == ObjectType.EmbossLine)
                {
                    double temp_x = objects[i].X;
                    if (device.DeviceType == DeviceType.DC450 && !((EmbossLine)objects[i]).IsEmbossFont)
                    {
                        if (((Devices.DC450)device).DIndent == Devices.DoubleIndent.Back && objects[i].Side == SideType.Front)
                            temp_x = Card.Width - temp_x;
                        if (((Devices.DC450)device).DIndent == Devices.DoubleIndent.Front && objects[i].Side == SideType.Back)
                            temp_x = Card.Width - temp_x;
                    }
                    if (Params.UseMetric)
                        temp_x = temp_x / 25.4;
                    dcl = String.Format("{0}{1:0000}", dcl.Trim(), Convert.ToInt32(temp_x * 1000.0));
                    wasDcl = true;
                    t++;
                    if (t == 2) // два отступа по горизонтали
                        break;
                }
            }
            t = 1;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].OType == ObjectType.EmbossLine)
                {
                    double temp_y = objects[i].Y;
                    if (Params.UseMetric)
                        temp_y = temp_y / 25.4;
                    dcl = String.Format("{0} {1:X}#{2}#{3:0000}", dcl, t++, Card.FontName(((EmbossLine)objects[i]).Font), temp_y * 1000.0);
                    string txt = "".PadLeft(((EmbossLine)objects[i]).GetDigitsCount());
                    //добавляем строку для dcc
                    for(int j = 0; j < objects.Count; j++)
                    {
                        if (objects[j].OType == ObjectType.EmbossText && ((EmbossText)objects[j]).ParentID == objects[i].ID)
                        {
                            string str = objects[j].GetText().ToUpper();
                            if (str.Length > ((EmbossText)objects[j]).Shablon.Length)
                                str = str.Substring(0, ((EmbossText)objects[j]).Shablon.Length);
                            for (int k = 0; k < str.Length; k++)
                            {
                                int ind;
                                if ((ind = Params.EmbCyrillic[0].IndexOf(str[k])) >= 0)
                                    str = str.Replace(str[k], Params.EmbCyrillic[1][ind]);
                            }
                            if (((EmbossText)objects[j]).Align == EmbossAlign.Right)
                                str = str.PadLeft(((EmbossText)objects[j]).Shablon.Length);
                            if (device.DeviceType == DeviceType.DC450 && !((EmbossLine)objects[i]).IsEmbossFont)
                            {
                                if (((Devices.DC450)device).DIndent == Devices.DoubleIndent.Back && objects[i].Side == SideType.Front)
                                    str = ReverseString(str);
                                if (((Devices.DC450)device).DIndent == Devices.DoubleIndent.Front && objects[i].Side == SideType.Back)
                                    str = ReverseString(str);
                            }
                            
                            txt = txt.Remove(((EmbossText)objects[j]).Position, str.Length);
                            txt = txt.Insert(((EmbossText)objects[j]).Position, str);
                        }
                    }
                    dcc = String.Format("{0}{1}{2}", dcc, txt.TrimEnd(), (char)0x22);
                }
            }
            if (device.DeviceType == DeviceType.DC450 && ((Devices.DC450)device).DopOffset > 0)
            {
                if (Params.UseMetric)
                    dcl = String.Format("{0}:P{1:0000}", dcl, Convert.ToInt32(((Devices.DC450)device).DopOffset / 25.4 * 100));
                else
                    dcl = String.Format("{0}:P{1:0000}", dcl, ((Devices.DC450)device).DopOffset);
            }
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].OType == ObjectType.MagStripe)
                {
                    string[] starts = new string[] { "%", ";", "_;" };
                    string trks = "";
                    for (int k = 0; k < 3; k++)
                    {
                        string trk = ((MagStripe)objects[i]).GetText(k).Trim();
                        if (trk.Length > 0)
                        {
                            if (!trk.StartsWith(starts[k]))
                                trk = String.Format("{0}{1}", starts[k], trk);
                            if (!trk.EndsWith("?"))
                                trk = String.Format("{0}{1}", trk , "?");
                            trks += trk;
                        }
                    }
                    if (trks.Length > 0)
                        dcc = String.Format("{0}#ENC#{1}", dcc, trks);
                }
            }
            dcl = String.Format("{0}#END#@@@@@@", dcl);
            dcc = String.Format("{0}#END#@@@@@@", dcc);
            if (wasDcl)
                al.Add(dcl);
            al.Add(dcc);
            return al;
        }
        private string ReverseString(string str)
        {
            string res = "";
            for (int i = 0; i < str.Length; i++)
                res = String.Format("{0}{1}", res, str[str.Length - i - 1]);
            return res;
        }
        public bool HasSmart()
        {
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].OType == ObjectType.SmartField)
                    return true;
            return false;
        }
        public bool HasMagstripeRead()
        {
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].OType == ObjectType.MagStripe)
                    return ((MagStripe)objects[i]).Feedback;
            return false;
        }

        public string GetName(ObjectType ot)
        {
            string res = "";
            switch (ot)
            {
                case (ObjectType.Barcode):
                    res = "Field_BarCode_Name";
                    break;
                case (ObjectType.EmbossLine):
                    res = "Field_EmbossLine_Name";
                    break;
                case (ObjectType.EmbossText):
                    res = "Field_EmbossField_Name";
                    break;
                case (ObjectType.EmbossText2):
                    res = "Field_EmbossField2_Name";
                    break;
                case (ObjectType.ImageField):
                    res = "Field_ImageField_Name";
                    break;
                case (ObjectType.MagStripe):
                    res = "Field_MagStripe_Name";
                    break;
                case (ObjectType.ReportField):
                    res = "Field_ReportField_Name";
                    break;
                case (ObjectType.SmartField):
                    res = "Field_SmartField_Name";
                    break;
                case (ObjectType.TextField):
                    res = "Field_TextField_Name";
                    break;
                case (ObjectType.TopCoat):
                    res = "Field_TopCoat_Name";
                    break;
                default:
                    res = "";
                    break;
            }
            res = (string)System.Windows.Application.Current.FindResource(res);
            int cnt = 0;
            bool flag = true;
            do
            {
                cnt++;
                flag = false;
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].Name == res + " " + cnt.ToString())
                    {
                        flag = true;
                        break;
                    }
                }
            }
            while (flag);
            return res + " " + cnt.ToString();
        }
        public void Draw(DrawingContext dc, SideType side, Regim regim, int step)
        {
            if (regim == Regim.Design)
            {
                int x1 = ClientXToScreen(0);
                int width = ClientToScreen(Width);
                int y1 = ClientYToScreen(Height, side);
                int height = ClientToScreen(Height);
                //это вместо закрашивания
                dc.DrawRectangle(Brushes.White, new Pen(Brushes.White, 1), new Rect(x1 - 20, y1 - 20, width + 40, height + 40));
                dc.DrawRoundedRectangle(Brushes.White, new Pen(Brushes.Black, 1), new Rect(x1, y1, width, height), ClientToScreen(radius), ClientToScreen(radius));
            }
        }
        public static int ClientXToScreen(double clientX)
        {
            return Convert.ToInt32(clientX * mas + left);
        }
        public static int ClientYToScreen(double clientY, SideType st)
        {
            int dt = (st == SideType.Front) ? top_f : top_r;
            return Convert.ToInt32(Height * mas - clientY * mas + dt);
        }
        public static int ClientToScreen(double client)
        {
            return Convert.ToInt32(client * mas);
        }
        public static double ScreenXToClient(int screenX)
        {
            return (screenX - left) / mas;
        }
        public static double ScreenYToClient(int screenY, SideType st)
        {
            int dt = (st == SideType.Front) ? top_f : top_r;
            return Height - (screenY - dt) / mas;
        }
        public static double ScreenToClient(int screen)
        {
            return screen / mas;
        }
        public static int Left()
        {
            return ClientXToScreen(0);
        }
        public static int Right()
        {
            return ClientXToScreen(Card.Width);
        }
        public static int Bottom(SideType st)
        {
            return ClientYToScreen(0, st);
        }
        public static int Top(SideType st)
        {
            return ClientYToScreen(Card.Height, st);
        }
        /// <summary>
        /// ширина одного символа
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public static double FontWidth(EmbossFont font)
        {
            double koeff = (Params.UseMetric) ? 1 : 25.4;
            switch (font)
            {
                case (EmbossFont.Farrington):
                    return 2.7 / koeff;
                case (EmbossFont.Gothic):
                    return 2 / koeff;
                case (EmbossFont.OCR10):
                    return 2 / koeff;
                case (EmbossFont.OCR7):
                    return 2.7 / koeff;
                case (EmbossFont.MCIndent):
                    return 1.6 / koeff;
                case (EmbossFont.MCIndentInvert):
                    return 1.6 / koeff;
                default:
                    return 2 / koeff;
            }
        }
        public static double FontHeight(EmbossFont font)
        {
            double koeff = (Params.UseMetric) ? 1 : 25.4;
            switch (font)
            {
                case (EmbossFont.Farrington):
                    return 4 / koeff;
                case (EmbossFont.Gothic):
                    return 3 / koeff;
                case (EmbossFont.OCR10):
                    return 3 / koeff;
                case (EmbossFont.OCR7):
                    return 4 / koeff;
                case (EmbossFont.MCIndent):
                    return 2.9 / koeff;
                default:
                    return 3 / koeff;
            }
        }
        /// <summary>
        /// ширина символа с интервалом
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public static double FontDis(EmbossFont font)
        {
            double koeff = (Params.UseMetric) ? 1 : 25.4;
            switch (font)
            {
                case (EmbossFont.Farrington):
                    return 3.6285 / koeff;
                case (EmbossFont.Gothic):
                    return 2.54 / koeff;
                case (EmbossFont.OCR10):
                    return 2.54 / koeff;
                case (EmbossFont.OCR7):
                    return 3.6285 / koeff;
                case (EmbossFont.MCIndent):
                    return 1.7 / koeff;
                case (EmbossFont.MCIndentInvert):
                    return 1.7 / koeff;
                default:
                    return 2.54 / koeff;
            }

        }
        public static string FontName(EmbossFont font)
        {
            switch (font)
            {
                case EmbossFont.Farrington:
                    return "FC2";
                case EmbossFont.Gothic:
                    return "FC1";
                case EmbossFont.OCR10:
                    return "FC3";
                case EmbossFont.MCIndent:
                    return "FC4";
                case EmbossFont.OCR7:
                    return "FC5";
                case EmbossFont.MCIndentFront:
                    return "FC6";
                case EmbossFont.OCRB:
                    return "FC7";
                case EmbossFont.MCIndentInvert:
                    return "FC8";
            }
            return "";
        }
        public static SideType FontSide(EmbossFont font)
        {
            switch (font)
            {
                case EmbossFont.Farrington:
                case EmbossFont.Gothic:
                case EmbossFont.OCR10:
                case EmbossFont.OCR7:
                case EmbossFont.MCIndentFront:
                case EmbossFont.OCRB:
                    return SideType.Front;
                case EmbossFont.MCIndent:
                case EmbossFont.MCIndentInvert:
                    return SideType.Back;
                default:
                    return SideType.Front;
            }
        }
        public bool PointInCard(int mX, int mY, SideType st)
        {
            return (mX >= ClientXToScreen(0) && mX <= ClientXToScreen(Width) && mY <= ClientYToScreen(0, st) && mY >= ClientYToScreen(Height, st));
        }
        public bool FindObject(ref DesignObject dsObject, int oID)
        {
            dsObject = null;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].ID == oID)
                {
                    dsObject = objects[i];
                    return true;
                }
            }
            return false;
        }
        public bool FindObject(ref DesignObject dsObject, string objectName)
        {
            dsObject = null;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Name.Equals(objectName))
                {
                    dsObject = objects[i];
                    return true;
                }
            }
            return false;
        }
        public bool IsMouseOver(ref DesignObject dsObject, int mX, int mY, SideType st)
        {
            dsObject = null;
            DesignObject dsO = null;
            // линии эмбоссирования ищем во вторую очередь, чтобы не пересекалось с полями эмбоссирования
            for (int i = 0; i < objects.Count; i++)
            {
                dsO = objects[i];
                if (dsO.OType == ObjectType.EmbossLine)
                    continue;
                if (dsO.Side != st)
                    continue;
                if (dsO.IsOver(mX, mY))
                {
                    dsObject = dsO;
                    return true;
                }
            }
            for (int i = 0; i < objects.Count; i++)
            {
                dsO = objects[i];
                if (dsO.OType != ObjectType.EmbossLine)
                    continue;
                if (dsO.Side != st)
                    continue;
                if (dsO.IsOver(mX, mY))
                {
                    dsObject = dsO;
                    return true;
                }
            }

            return false;
        }
        public void RecalcTopLeft(int panelWidth, int panelFrontHeight, int panelRearHeight)
        {
            top_f = Convert.ToInt32((panelFrontHeight - ClientToScreen(Height)) / 2.0);
            top_r = Convert.ToInt32((panelRearHeight - ClientToScreen(Height)) / 2.0);
            left = Convert.ToInt32((panelWidth - ClientToScreen(Width)) / 2.0);
            if (top_f < 5)
                top_f = 5;
            if (top_r < 5)
                top_r = 5;
            if (left < 5)
                left = 5;
        }

        public void SetTopLeftForPrint()
        {
            top_f = 0; top_r = 0; left = 0;
        }
        public int GetFieldCount(ObjectType ot)
        {
            int cnt = 0;
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].OType == ot)
                    cnt++;
            return cnt;
        }
        public void SetEmbossLineX(double x)
        {
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].OType == ObjectType.EmbossLine && !((EmbossLine)objects[i]).FirstLine)
                    objects[i].X = x;
        }
        /// <summary>
        /// проверяем на равенство х у всех вторых линий. чтобы откатить, если на какой-то уткнулись в край
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool CheckEmbossLineX(double x)
        {
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].OType == ObjectType.EmbossLine && !((EmbossLine)objects[i]).FirstLine)
                    if (objects[i].X != x)
                        return false;
            return true;
        }
    }
	public class Para
	{
		private int id;
		public int ID
		{
			get
			{
				return id;
			}
			set
			{
				id = value;
			}
		}
		private object val;
		public object Value
		{
			get
			{
				return val;
			}
			set
			{
				val = value;
			}
		}
        private string toString;
		public Para(int i, object obj)
		{
			id = i; val = obj; toString = "";
		}
        public Para(int i, object obj, string str)
        {
            id = i; val = obj; toString = str;
        }
		public override string ToString()
		{
			return (toString.Length > 0) ? toString : val.ToString();
		}
	}
    /*
	public class DBConnection
	{
		private int typeID;
		public int TypeID
		{
			get
			{
				return typeID;
			}
			set
			{
				typeID = value;
			}
		}
		private string path;
		public string Path
		{
			get
			{
				return path;
			}
			set
			{
				path = value;
			}
		}
		private string table;
		public string Table
		{
			get
			{
				return table;
			}
			set
			{
				table = value;
			}
		}
		private string filter;
		public string Filter
		{
			get
			{
				return filter;
			}
			set
			{
				filter = value;
			}
		}
		private string restrict;
		public string Restrict
		{
			get
			{
				return restrict;
			}
			set
			{
				restrict = value;
			}
		}
		private string login;
		private string password;
		private bool winAuthenticate;
		public string Security
		{
			get
			{
				string str = (winAuthenticate) ? "1" : "0";
				str = login.PadLeft(20, '@') + password + str;
				return str;
			}
			set
			{
				string str = value;
				if (str.Length > 20)
				{
					login = str.Substring(0, 20).TrimEnd('@');
					str = str.Remove(0, 20);
				}
				if (str.Length > 1)
				{
					password = str.Substring(0, str.Length - 1);
					winAuthenticate = (str[str.Length - 1] == '1') ? true : false;
				}
			}
		}
		[XmlArrayItem(typeof(string))]
		public ArrayList Columns;
		private string colTypes;
		public string ColTypes
		{
			get
			{
				return colTypes;
			}
			set
			{
				colTypes = value;
			}
		}
		public DBConnection()
		{
			typeID = 0;
			path = "";
			table = "";
			login = "";
			password = "";
			restrict = "*+*";
			colTypes = "";
			Columns = new ArrayList();
			winAuthenticate = false;
		}
	}*/
    [Serializable]
    [XmlRootAttribute("ObjectArray", Namespace = "", IsNullable = false)]
    public class ObjectArray : IEnumerator
    {
        [XmlArrayItem(Type = typeof(DesignObject)), XmlArrayItem(Type = typeof(EmbossLine)),
            XmlArrayItem(Type = typeof(EmbossText)), XmlArrayItem(Type = typeof(MagStripe)),
            XmlArrayItem(Type = typeof(SmartField)), XmlArrayItem(Type = typeof(ImageField)),
            XmlArrayItem(Type = typeof(TextField)), XmlArrayItem(Type = typeof(TopCoat)),
            XmlArrayItem(Type = typeof(Barcode)), XmlArrayItem(Type = typeof(ReportField)),
            XmlArrayItem(Type = typeof(EmbossText2))]
        public ArrayList collection;
        private int index;
        public bool MoveNext()
        {
            index++;
            if (index >= collection.Count)
                return false;
            else
                return true;
        }
        public void Reset()
        {
            index = -1;
        }
        public void ClearArray()
        {
            collection.Clear();
            Reset();
        }
        public object Current
        {
            get
            {
                return collection[index];
            }
        }
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }
        public ObjectArray()
        {
            collection = new ArrayList();
            index = -1;
        }
        public DesignObject this[int index]
        {
            get
            {
                if (index > -1 && index < collection.Count)
                    return (DesignObject)collection[index];
                else
                    return null;
            }
            set
            {
                if (index > -1 && index < collection.Count)
                    collection[index] = value;
                else
                    if (index == collection.Count)
                        collection.Add(value);
            }
        }

        public void Add(DesignObject dsO) => collection.Add(dsO);
        public void InsertAt(int newIndex, DesignObject dsO) => collection.Insert(newIndex, dsO);

        public int Count => collection.Count;

        public override string ToString()
        {
            return "";
        }
        public int GetIndex()
        {
            return index;
        }
        public void SetIndex(int val) => index = val;
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < collection.Count)
                collection.RemoveAt(index);
        }
        public void Remove(int objectId)
        {
            for (int i = 0; i < collection.Count; i++)
                if (((DesignObject) collection[i]).ID == objectId)
                {
                    collection.RemoveAt(i);
                    return;
                }
        }
    }
    [Serializable]
    public class Composite
    {
        private CompositeType type;
        public CompositeType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }
        private string data;
        public string Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }
        private CompositeFunc function;
        public CompositeFunc Function
        {
            get
            {
                return function;
            }
            set
            {
                function = value;
            }
        }
        private ArrayList parameters;
        [XmlArrayItem(Type=typeof(string))]
        public ArrayList Parameters
        {
            get
            {
                return parameters;
            }
            set
            {
                parameters = value;
            }
        }
        public Composite()
        {
            type = CompositeType.None;
            data = "";
            function = CompositeFunc.None;
            parameters = new ArrayList();
        }
        public Composite(CompositeType tp, string dt)
        {
            type = tp; data = dt;
            function = CompositeFunc.None;
            parameters = new ArrayList();
        }
        public override string ToString()
        {
            string res = "", fname = "";
            string dname = data;
            if (type == CompositeType.Feedback)
            {
                try
                {
                    // магнитка
                    if (Convert.ToInt32(data) > 0 && Convert.ToInt32(data) < 4)
                        dname = $"{(string)System.Windows.Application.Current.FindResource("MagStripeFeedback")} {Convert.ToInt32(data)}";
                    // чип
                    if (Convert.ToInt32(data) == 4)
                        dname = $"{(string)System.Windows.Application.Current.FindResource("SmartField_Feedback")}";
                }
                catch { }
            }
            switch (function)
            {
                case (CompositeFunc.AddChar):
                    fname = (string)System.Windows.Application.Current.FindResource("Composite_FAddCharShort");
                    res = String.Format("{0} - {1}[{2},'{3}']", dname, fname, (parameters.Count > 0) ? parameters[0] : "", (parameters.Count > 1) ? parameters[1] : "");
                    break;
                case (CompositeFunc.SubStringPos):
                    fname = (string)Application.Current.FindResource("Composite_FSubstringShort");
                    res = String.Format("{0} - {1}[{2},{3}]", dname, fname, (parameters.Count > 0) ? parameters[0] : "", (parameters.Count > 1) ? parameters[1] : "");
                    break;
                case (CompositeFunc.None):
                default:
                    res = dname;
                    break;
            }
            return res;
        }
    }
    [Serializable]
    [XmlRootAttribute("CompositeArray", Namespace="", IsNullable = false)]
    public class CompositeArray : IEnumerator
    {
        private int index;
        [XmlArrayItem(Type= typeof(Composite))]
        public ArrayList collection;
        public CompositeArray()
        {
            collection = new ArrayList();
            index = -1;
        }
        public bool MoveNext()
        {
            index++;
            if (index >= collection.Count)
                return false;
            else
                return true;
        }
        public void Reset()
        {
            index = -1;
        }
        public void ClearArray()
        {
            collection.Clear();
            Reset();
        }
        public object Current
        {
            get
            {
                return collection[index];
            }
        }
        public int Count
        {
            get
            {
                return collection.Count;
            }
        }
        public Composite this[int index]
        {
            get
            {
                if (index > -1 && index < collection.Count)
                    return (Composite)collection[index];
                else
                    return null;
            }
            set
            {
                if (index > -1 && index < collection.Count)
                    collection[index] = value;
                else
                    if (index == collection.Count)
                    collection.Add(value);
            }
        }

    }
}

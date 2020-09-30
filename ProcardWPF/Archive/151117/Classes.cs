using System;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

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
        MCIndentInvert = 8
    }
    public enum InTypes : int
    {
        #warning сделать поддержку разных типов, выбрать какие нужны/не нужны
        None = 1,
        Keyboard = 2,
        Auto = 3,
        Db = 4,
        Composite = 5,
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
        BarCode,
        ReportField,
        EmbossText2
    }
    public enum SideType : int
    {
        Front = 0,
        Back = 1
    }
	public abstract class DesignObject
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
            }
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
            }
        }
        public virtual int Top
        {
            get
            {
                return Card.ClientYToScreen(y + height, side);
            }
        }
        public virtual int Bottom
        {
            get
            {
                return Card.ClientYToScreen(y, side);
            }
        }
        public virtual int Left
        {
            get
            {
                return Card.ClientXToScreen(x);
            }
        }
        public virtual int Right
        {
            get
            {
                return Card.ClientXToScreen(x + width);
            }
        }
        protected int misc;
        private string text;
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                if (value != text)
                    saved = false;
                text = value;
            }
        }
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
		public DesignObject()
		{
			Init();
		}
		public void Init()
		{
			id = -1;
            misc = 0;
			saved = false;
		}
        public abstract void Draw(DrawingContext dc, Regim regim, bool selected, int step);
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
            #warning добавить отдельное рисование для незарегистрированых линий индента для dc450 (определять по parent)
            int cTop = Card.ClientYToScreen(Card.Height, this.side);
            int cBottom = Card.ClientYToScreen(0, this.Side);
            int cRight = Card.ClientXToScreen(Card.Width);
            if (registered)
            {
                dc.DrawLine((firstLine) ? new Pen(Brushes.Red, 1) : new Pen(Brushes.Black, 1), new System.Windows.Point(Card.ClientXToScreen(this.X), cTop), new System.Windows.Point(Card.ClientXToScreen(this.X), cBottom));//height);
                dc.DrawLine((firstLine) ? new Pen(Brushes.Red, 1) : new Pen(Brushes.Black, 1), new System.Windows.Point(Card.ClientXToScreen(this.X), Card.ClientYToScreen(this.Y, this.Side)), new System.Windows.Point(Card.ClientXToScreen(Card.Width), Card.ClientYToScreen(this.Y, this.Side)));
            }
            else
            {
                cTop = (Y + Card.Radius > Card.Height) ? Card.ClientYToScreen(Card.Height, this.side) : Card.ClientYToScreen(Y + Card.Radius, this.side);
                cBottom = (Y - Card.Radius < 0) ? Card.ClientYToScreen(0, this.side) : Card.ClientYToScreen(Y - Card.Radius, this.side);
                dc.DrawLine((firstLine) ? new Pen(Brushes.Red, 1) : new Pen(Brushes.Black, 1), new System.Windows.Point(Card.ClientXToScreen(Card.Width-this.X), cTop), new System.Windows.Point(Card.ClientXToScreen(Card.Width-this.X), cBottom));//height);
                dc.DrawLine((firstLine) ? new Pen(Brushes.Red, 1) : new Pen(Brushes.Black, 1), new System.Windows.Point(Card.ClientXToScreen(Card.Width-this.X), Card.ClientYToScreen(this.Y, this.Side)), new System.Windows.Point(Card.ClientXToScreen(0), Card.ClientYToScreen(this.Y, this.Side)));
            }

            /*            double lf = X, tp = Y;
                        lf -= Card.FontWidth(font) / 2.0;
                        tp += Card.FontHeight(font) / 2.0;

                        while (true)
                        {
                            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Gray, 1), new Rect(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(Card.FontWidth(font)), Card.ClientToScreen(Card.FontHeight(font))));
                            lf += Card.FontDis(font);
                            if (lf + Card.FontWidth(font) > Card.Width)
                                break;
                        }
                        lf = Card.ClientXToScreen(Card.Width);
                        foreach (bool b in digits)
                        {
                            dc.DrawText(new FormattedText((b) ? "1" : "0", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Microsoft Times Roman"), 15, Brushes.Black), new Point(lf += 10, Card.ClientYToScreen(tp, side)));
                        }*/

        }
        public override bool IsOver(int screenX, int screenY)
        {
            misc = -1;
            double cTop = Card.ClientYToScreen(Card.Height, this.side);
            double cBottom = Card.ClientYToScreen(0, this.Side);
            double cRight = Card.ClientXToScreen(Card.Width);
            if (screenX > Left - 3 && screenX < Left + 3 && screenY < cBottom && screenY > cTop)
            {
                misc = 9;
                return true;
            }
            if (screenX > Left - 3 && screenX < cRight && screenY > Bottom - 3 && screenY < Bottom + 3)
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
    }
    public class BarCode : DesignObject
    {
        public BarCode()
        {
            side = SideType.Front;
            oType = ObjectType.BarCode;
        }
        //Neodynamic.WinControls.BarcodeProfessional.BarcodeProfessional barCode = new Neodynamic.WinControls.BarcodeProfessional.BarcodeProfessional();
        Neodynamic.WPF.BarcodeProfessional barCode = new Neodynamic.WPF.BarcodeProfessional();
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            //dc.DrawRectangle(Brushes.White, new Pen(Brushes.Black, (selected) ? 3 : 1), new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side), Card.ClientToScreen(Width), Card.ClientToScreen(Height)));
            barCode.BarcodeUnit = Neodynamic.WPF.BarcodeUnit.Inch;
            barCode.Symbology = Neodynamic.WPF.Symbology.Code39;
            barCode.Extended = true;
            barCode.AddChecksum = false;
            barCode.Code = "1234567890123456";
            barCode.BarWidth = 1.0 / 96.0;
            barCode.BarHeight = 1;// 0.75;
            barCode.QuietZone = new Thickness(0, 0.1, 0, 0.1);
            barCode.FontSize = 12;
            barCode.FitBarcodeToSize = new Size(1, 1);
            //dc.PushTransform(new TranslateTransform(100, 100));
            dc.DrawDrawing(barCode.GetBarcodeDrawing());
        }
    }
    public class TextField : DesignObject
    {
        public TextField()
        {
            side = SideType.Front;
            oType = ObjectType.TextField;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            if (regim == Regim.Design)
            {
                double brd_ti = (Params.UseMetric) ? 0.7 : 0.7 / 25.4;
                double brd_wi = (Params.UseMetric) ? 1.5 : 1.5 / 25.4;
                dc.DrawRectangle(Brushes.White, new Pen(Brushes.Black, (selected) ? 3 : 1), new Rect(Card.ClientXToScreen(X), Card.ClientYToScreen(Y + Height, Side), Card.ClientToScreen(Width), Card.ClientToScreen(Height)));
            }
        }
    }
    public class ImageField : DesignObject
    {
        public ImageField()
        {
            side = SideType.Front;
            oType = ObjectType.ImageField;
        }
        public override void Draw(DrawingContext gr, Regim regim, bool selected, int step)
        {
            //gr.DrawRectangle(Pens.Black, Card.ClientXToScreen(x), Card.ClientYToScreen(y+height, Side), Card.ClientToScreen(width), Card.ClientToScreen(height));
        }
    }
    public class TopCoat : DesignObject
    {
        public TopCoat()
        {
            side = SideType.Front;
            oType = ObjectType.TopCoat;
        }
        public override void Draw(DrawingContext gr, Regim regim, bool selected, int step)
        {
            //gr.DrawRectangle(Pens.Black, Card.ClientXToScreen(x), Card.ClientYToScreen(y+height, Side), Card.ClientToScreen(width), Card.ClientToScreen(height));
        }
    }
    public class MagStripe : DesignObject
    {
        public MagStripe()
        {
            side = SideType.Back;
            oType = ObjectType.MagStripe;
            x = 0; width = Card.Width;
            y = (Params.UseMetric) ? 1.45 * 25.4 : 1.45; 
            height = (Params.UseMetric) ? 0.51 * 25.4 : 0.51;
        }
        public override void Draw(DrawingContext gr, Regim regim, bool selected, int step)
        {
            //gr.FillRectangle(Brushes.Black, Card.ClientXToScreen(x), Card.ClientYToScreen(y + height, Side), Card.ClientToScreen(width), Card.ClientToScreen(height));
        }
    }
    public class EmbossText : DesignObject
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
                    x = parent.X + Card.FontDis(parent.Font)*position;
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
        public EmbossLine Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value; ;
            }
        }
        public EmbossText()
        {
            oType = ObjectType.EmbossText;
        }
        public EmbossText(EmbossLine el)
        {
            parent = el;
            oType = ObjectType.EmbossText;
            x = el.X; y = el.Y;
        }
        public override void Draw(DrawingContext dc, Regim regim, bool selected, int step)
        {
            double lf = X, tp = Y;
            lf -= Card.FontWidth(parent.Font) / 2.0;
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
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Oblique, FontWeights.Normal, FontStretches.Normal);
                    fontSize = 10;
                    break;
                default:
                    font = new Typeface(new FontFamily("Microsoft Sans Serif"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                    fontSize = 10;
                    break;
            }

            for (int i = 0; i < shablon.Length; i++)
            {
                if (regim == Regim.Design)
                { 
                    dc.DrawRectangle(Brushes.White, new Pen(Brushes.Gray, 1), new Rect(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(Card.FontWidth(parent.Font)), Card.ClientToScreen(Card.FontHeight(parent.Font))));
                    dc.DrawText(new FormattedText(shablon[i].ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black), new Point(Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side)));
                }
                lf += Card.FontDis(parent.Font);
            }
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
                return Card.ClientXToScreen(X - Card.FontWidth(parent.Font) / 2.0);
            }
        }
        public override int Right
        {
            get
            {
                return Card.ClientXToScreen(X + Card.FontDis(parent.Font) * (shablon.Length - 0.5));
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
    public class EmbossText2 : DesignObject
    {
        private EmbossFont font;
        public EmbossFont Font
        {
            set
            {
                if (value != font)
                    saved = false;
                font = value;
            }
        }
        private int charNum;
        public int CharNum
        {
            get
            {
                return charNum;
            }
            set
            {
                if (value != charNum)
                    saved = false;
                charNum = value;
            }
        }

        //private int FontSize
        //{
        //    get
        //    {
        //        switch (font)
        //        {
        //            case (EmbossFont.Farrington):
        //            case (EmbossFont.OCR7):
        //                return 11;
        //            default:
        //                return 10;
        //        }
        //    }
        //}
        public EmbossText2()
        {
            oType = ObjectType.EmbossText2;
        }
        public EmbossText2(int embossLineID)
        {
            oType = ObjectType.EmbossText2;
        }
        public override void Draw(DrawingContext gr, Regim regim, bool selected, int step)
        {
            //string txt = Text;
            //double tp = 1, lf = x;
            //lf -= FontWidth / 2.0;
            //tp = y + FontHeight / 2.0;
            ////lf += FontDis * position;
            //Font fn = new Font("Microsoft Sans Serif", FontSize, System.Drawing.FontStyle.Bold);

            //if (regim == Regim.Design && selected)
            //{
            //    double brd_t = (Params.UseMetric) ? 0.7 : 0.7 / 25.4;
            //    double brd_w = (Params.UseMetric) ? 1.5 : 1.5 / 25.4;
            //    //Brush hatchBrush = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.DarkUpwardDiagonal, Color.Black, Color.White);
            //    //gr.FillRectangle(hatchBrush, Card.ClientXToScreen(lf - brd_t), Card.ClientYToScreen(tp + brd_t, side), Card.ClientToScreen(FontDis * charNum - FontDis + FontWidth + brd_w), Card.ClientToScreen(FontHeight + brd_w));
            //    //gr.FillRectangle(Brushes.White, Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(FontDis * charNum - FontDis + FontWidth), Card.ClientToScreen(FontHeight));
            //    //((IDisposable)hatchBrush).Dispose();
            //}

            //for (int i = 0; i < charNum; i++)
            //{
            //    if (regim == Regim.Design)
            //        gr.DrawRectangle(Pens.Black, Card.ClientXToScreen(lf), Card.ClientYToScreen(tp, side), Card.ClientToScreen(FontWidth), Card.ClientToScreen(FontHeight));
            //    if (i < txt.Length)
            //        gr.DrawString(txt.Substring(i, 1), fn, System.Drawing.Brushes.Black, Card.ClientXToScreen(lf) - 2, Card.ClientYToScreen(tp, side) - 1);
            //    lf += FontDis;
            //}
        }
        public override bool IsOver(int screenX, int screenY)
        {
            //int ox = Card.ClientXToScreen(x);
            //int oy = Card.ClientYToScreen(y, side);
            //double st = position * FontDis;
            //ox -= Card.ClientToScreen(FontWidth / 2.0);
            //if (st < 0)
            //    st = 0;
            //int ist = Card.ClientToScreen(st);
            //misc = -1;
            //int temp_l = ox + ist;
            //int temp_r = ox + ist + Card.ClientToScreen(charNum * FontDis);
            //if (screenX > temp_l - 2 && screenX < temp_r + 2 && screenY > oy - Card.ClientToScreen(FontHeight / 2.0) - 2 && screenY < oy + Card.ClientToScreen(FontHeight / 2.0) + 2)
            //{
            //    misc = 0;
            //    if (screenX > temp_l - 2 && screenX < temp_l + 2)
            //        misc = 5;
            //    if (screenX > temp_r - 2 && screenX < temp_r + 2)
            //        misc = 7;
            //    return true;
            //}
            return false;
        }
    }
    public class SmartField : DesignObject
    {
        public SmartField()
        {
        }
        public override void Draw(DrawingContext gr, Regim regim, bool selected, int step)
        {
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
        public static float Height
        {
            get
            {
                return (useMetric) ? 53.98f : 2.125f;
            }
        }
        public static float Width
        {
            get
            {
                return (useMetric) ? 85.72f : 3.375f;
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
		}
        public int GetNextID()
        {
            maxID++;
            return maxID;
        }
        public string GetName(ObjectType ot)
        {
            string res = "";
            switch (ot)
            {
                case(ObjectType.BarCode):
                    res = "Field_BarCode_Name";
                    break;
                case(ObjectType.EmbossLine):
                    res = "Field_EmbossLine_Name";
                    break;
                case(ObjectType.EmbossText):
                    res = "Field_EmbossField_Name";
                    break;
                case(ObjectType.EmbossText2):
                    res = "Field_EmbossField2_Name";
                    break;
                case(ObjectType.ImageField):
                    res = "Field_ImageField_Name";
                    break;
                case(ObjectType.MagStripe):
                    res = "Field_MagStripe_Name";
                    break;
                case(ObjectType.ReportField):
                    res = "Field_ReportField_Name";
                    break;
                case(ObjectType.SmartField):
                    res = "Field_SmartField_Name";
                    break;
                case(ObjectType.TextField):
                    res = "Field_TextField_Name";
                    break;
                case(ObjectType.TopCoat):
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
                dc.DrawRectangle(Brushes.White, new Pen(Brushes.White, 1), new Rect(x1-20, y1-20, width+40, height+40));
                dc.DrawRoundedRectangle(Brushes.White, new Pen(Brushes.Black, 1), new Rect(x1, y1, width, height), ClientToScreen(radius), ClientToScreen(radius));
            }
        }
        public static int ClientXToScreen(double clientX)
        {
            return Convert.ToInt32(clientX*mas+left);
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
                    return 3.7 / koeff;
                case (EmbossFont.Gothic):
                    return 2.54 / koeff;
                case (EmbossFont.OCR10):
                    return 2.54 / koeff;
                case (EmbossFont.OCR7):
                    return 3.7 / koeff;
                case (EmbossFont.MCIndent):
                    return 1.7 / koeff;
                case (EmbossFont.MCIndentInvert):
                    return 1.7 / koeff;
                default:
                    return 2.54 / koeff;
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
        public int GetEmbossLineCount()
        {
            int cnt = 0;
            for (int i = 0; i < objects.Count; i++)
                if (objects[i].OType == ObjectType.EmbossLine)
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
		public Para(int i, object obj)
		{
			id = i; val = obj;
		}
		public override string ToString()
		{
			return val.ToString();
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
            XmlArrayItem(Type = typeof(BarCode)), XmlArrayItem(Type = typeof(ReportField))]
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
        public void Add(DesignObject dsO)
        {
            collection.Add(dsO);
        }
        public void InsertAt(int new_index, DesignObject dsO)
        {
            collection.Insert(new_index, dsO);
        }
        public int Count
        {
            get
            {
                return collection.Count;
            }
        }
        public override string ToString()
        {
            return "";
        }
        public int GetIndex()
        {
            return index;
        }
        public void SetIndex(int val)
        {
            index = val;
        }
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < collection.Count)
                collection.RemoveAt(index);
        }
    }
    
}

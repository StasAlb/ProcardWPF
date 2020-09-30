using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;

namespace ProcardWPF
{
    public class MyFont
    {
        private string fontName;

        public string FontName
        {
            get { return fontName; }
            set { fontName = value; }
        }

        private float fontSize;

        public float FontSize
        {
            get { return fontSize; }
            set { fontSize = value; }
        }

        private FontStyle fontStyle;

        public FontStyle FontStyle
        {
            get { return fontStyle; }
            set { fontStyle = value; }
        }

        private FontWeight fontWeight;

        public FontWeight FontWeight
        {
            get { return fontWeight; }
            set { fontWeight = value; }
        }
        private FontStretch fontStretch;
        public FontStretch FontStretch
        {
            get { return fontStretch; }
            set { fontStretch = value; }
        }
        // 0 - underline, 1 - strikeout
        private string service;

        public string Service
        {
            get { return service; }
            set { service = value; }
        }
        public MyFont()
        {
            fontName = "Microsoft Sans Serif";
            fontSize = 14;
            fontStyle = FontStyles.Normal;
            fontWeight = FontWeights.Normal;
            service = "";
        }
        public Typeface GetTypeface()
        {
            Typeface tf = new Typeface(new FontFamily(fontName), fontStyle, fontWeight, fontStretch);
            return tf;
        }
        public bool IsUnderline
        {
            get
            {
                return (service.Length > 0 && service[0] == '1');
            }
        }

        public bool IsStrikeout
        {
            get
            {
                return (service.Length > 1 && service[1] == '1');
            }
        }

        public void SetParameters(bool isUnderline, bool isStrikeout)
        {
            service = "";
            service += (isUnderline) ? "1" : "0";
            service += (isStrikeout) ? "1" : "0";
        }
        public Font GetFont()
        {
            System.Drawing.FontStyle fs = (fontStyle == FontStyles.Italic) ? System.Drawing.FontStyle.Italic : System.Drawing.FontStyle.Regular;
            if (fontWeight == FontWeights.Bold)
                fs = fs | System.Drawing.FontStyle.Bold;
            if (service.Length > 0 && service[0] == '1')
                fs = fs | System.Drawing.FontStyle.Underline;
            if (service.Length > 1 && service[1] == '1')
                fs = fs | System.Drawing.FontStyle.Strikeout;
            Font f = new Font(new System.Drawing.FontFamily(fontName), fontSize, fs);
            return f;
        }
        public System.Windows.Size MeasureText(string text)
        {
            Typeface typeface = GetTypeface();
            GlyphTypeface glyphTypeface;

            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                return MeasureTextSize(text);
            }

            double totalWidth = 0;
            double height = 0;

            for (int n = 0; n < text.Length; n++)
            {
                ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

                double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize * 96.0 / 72.0;

                double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize * 96.0 / 72.0;

                if (glyphHeight > height)
                {
                    height = glyphHeight;
                }

                totalWidth += width;
            }
            return new System.Windows.Size(totalWidth, height);
        }
        private System.Windows.Size MeasureTextSize(string text)
        {
            FormattedText ft = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                GetTypeface(),
                fontSize * 96.0 / 72.0,
                System.Windows.Media.Brushes.Black);
            return new System.Windows.Size(ft.Width, ft.Height);
        }
    }
}
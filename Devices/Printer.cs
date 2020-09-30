using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Printing;
using System.Printing;
using System.Windows.Media;


namespace Devices
{
    public abstract class PrinterClass : DeviceClass
    {
        protected bool onlyPrint;

        public bool OnlyPrint
        {
            get { return onlyPrint; }
            set
            {
                onlyPrint = value;
                RaisePropertyChanged("OnlyPrint");
            }
        }
        public string printerName = "";
        protected bool _firstPage = true;
        protected bool TwoSide = false;
        protected DrawingVisual dvFrontImage, dvBackImage;
        public void SetImages(DrawingVisual front, DrawingVisual back)
        {
            dvFrontImage = front;
            dvBackImage = back;
        }

        public delegate void DrawForPrint(System.Drawing.Graphics graphics, ProcardWPF.SideType side);
        //public event DrawForPrint drawForPrint;
        public void SetMagstripe(string[] tracks)
        {
            if (tracks == null)
                return;
            for (int i = 0; i < 3; i++)
            {
                magstripe[i] = "";
                if (tracks.Length > i)
                    magstripe[i] = tracks[i];
            }
        }
        //protected void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        //{
        //    if (_firstPage)
        //    {
        //        drawForPrint?.Invoke(e.Graphics, ProcardWPF.SideType.Front);
        //        _firstPage = false;
        //        if (TwoSide)
        //            e.HasMorePages = true;
        //    }
        //    else
        //    {
        //        drawForPrint?.Invoke(e.Graphics, ProcardWPF.SideType.Back);
        //    }
        //}
        //public bool HasDrawForPrint()
        //{
        //    return drawForPrint != null;
        //}
    }
}

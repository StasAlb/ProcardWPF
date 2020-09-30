using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Printing;
using System.Drawing;
using System.Drawing.Printing;
using HugeLib;
using ProcardWPF;

namespace Devices
{
    public class SRPrinter : PrinterClass 
    {
        private bool _firstPage = true;
        int pSlot = 0, pID = 0;
        bool TwoSide = false;
        #region PrinterMethods
        [DllImport("pcp21ct.dll")]
        internal static extern int CXCMD_ScanPrinter(ref int piSlot, ref int piID);
        [DllImport("pcp21ct.dll")]
        internal static extern bool CXCMD_CheckIfConnected(ref int piSlot, ref int piID);
        [DllImport("pcp21ct.dll")]
        internal static extern int CXCMD_LoadCard(int piSlot, int piID, int iDest, int iFlip, int iFilmInit, int iImmed);
        [DllImport("pcp21ct.dll")]
        internal static extern int CXCMD_MoveCard(int piSlot, int piID, int iDest, int iFlip, int iFilmInit, int iImmed);
        [DllImport("pcp21ct.dll")]
        internal static extern int CXCMD_ICControl(int piSlot, int piID, int iICType, int iAction);
        [DllImport("pcp21ct.dll")]
        internal static extern int CXCMD_TestUnitReady(int piSlot, int piId);
        #endregion
        public event DrawForPrint drawForPrint;
        public SRPrinter()
        {
            printerName = "SR-CP U1";
            deviceType = ProcardWPF.DeviceType.SR;
        }
        public override bool EndCard()
        {
            LogClass.WriteToLog("EndCard begin");
            int res = CXCMD_MoveCard(pSlot, pID, 4, 0, 0, 0);
            LogClass.WriteToLog("MoveCard, out: res = {2}", pSlot, pID, ErrorDesc(res));
            return (res == 0);
        }
        public override bool FeedCard(FeedType feedType)
        {
            int res = 0, loadType = 0, controlType = 0;
            int cnt = 0;
            if (feedType == FeedType.SmartFront)
            {
                loadType = 1;
                controlType = 0;
            }
            if (feedType == FeedType.SmartContactless)
            {
                loadType = 2;
                controlType = 1;
            }
            do
            {
                res = CXCMD_LoadCard(pSlot, pID, loadType, 0, 0, 0);
                LogClass.WriteToLog("LoadCard: res = {2}", pSlot, pID, ErrorDesc(res));
                cnt++;
                System.Threading.Thread.Sleep(500);
            }
            while (res != 0 && cnt < 3);
            res = CXCMD_ICControl(pSlot, pID, controlType, 0);
            LogClass.WriteToLog("ICControl, lock: res = {0}", ErrorDesc(res));
            return (res == 0);
        }
        public bool MoveCard(FeedType feedType)
        {
            int res = 0, iDest = 0, iFlip = 0;
            switch (feedType)
            {
                case FeedType.Print:
                    iDest = 0; iFlip = 0;
                    break;
                case FeedType.PrintAfterTurn:
                    iDest = 0; iFlip = 1;
                    break;
                default:
                    return true;
            }
            res = CXCMD_MoveCard(pSlot, pID, iDest, iFlip, 0, 1);
            LogClass.WriteToLog("MoveCard: FeedType = {0}, res = {1}", feedType, ErrorDesc(res));
            return true;
        }
        public bool ReleaseAfterFeed(FeedType feedType)
        {
            int res = 0;
            if (feedType == FeedType.SmartFront)
                res = CXCMD_ICControl(pSlot, pID, 0, 1);
            if (feedType == FeedType.SmartContactless)
                res = CXCMD_ICControl(pSlot, pID, 1, 1);
            LogClass.WriteToLog("ICControl, release: res = {0}", ErrorDesc(res));
            return (res == 0);
        }
        public bool PrintCard(int pageCount)
        {
            return PrintCard();
        }
        public override bool PrintCard()
        {
            //if (_firstPage)
            //{
            //    Bitmap b = new Bitmap(1036, 664, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //    System.Drawing.Graphics gr = Graphics.FromImage(b);
            //}

            PrintDocument printDocument = new PrintDocument();
            printDocument.PrinterSettings.PrinterName = printerName;
            printDocument.PrintController = new StandardPrintController();
            printDocument.BeginPrint += PrintDocument_BeginPrint;
            printDocument.QueryPageSettings += PrintDocument_QueryPageSettings;
            printDocument.PrintPage += PrintDocument_PrintPage;
            printDocument.Print();
            return true;
        }
        private void PrintDocument_BeginPrint(object sender, PrintEventArgs e)
        {
            PrintQueue printQueue = new PrintQueue(new LocalPrintServer(), printerName);
            PrintTicket printTicket = new PrintTicket();
            printTicket.Duplexing = Duplexing.OneSided;//.TwoSidedLongEdge;
            printTicket.PageOrientation = PageOrientation.Landscape;
            printTicket.PageResolution = new PageResolution(300, 300, PageQualitativeResolution.Draft);

            ValidationResult validationResult = printQueue.MergeAndValidatePrintTicket(printQueue.UserPrintTicket, printTicket);

            //string xmlString = PrintTicketXml.Prefix;
            //xmlString += PrintTicketXml.FlipFrontNone;
            //xmlString += PrintTicketXml.Suffix;

            //XmlDocument xmlDocument = new XmlDocument();
            //xmlDocument.LoadXml(xmlString);
            //MemoryStream memoryStream = new MemoryStream();
            //xmlDocument.Save(memoryStream);
            //memoryStream.Position = 0;

            //printTicket = new PrintTicket(memoryStream);
            validationResult = printQueue.MergeAndValidatePrintTicket(validationResult.ValidatedPrintTicket, printTicket);
            printQueue.UserPrintTicket = validationResult.ValidatedPrintTicket;
            printQueue.Commit();
        }
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_firstPage)
            {
                if (drawForPrint != null)
                    drawForPrint(e.Graphics, ProcardWPF.SideType.Front);
                _firstPage = false;
                e.HasMorePages = TwoSide;
            }
            else
            {
                if (drawForPrint != null)
                    drawForPrint(e.Graphics, ProcardWPF.SideType.Back);
                e.HasMorePages = false;
            }
        }
        private void PrintDocument_QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
        }

        public override bool ReadMagstripe()
        {
            return true;
        }
        public override bool RemoveCard(ResultCard resultCard)
        {
            return true;
        }
        public override bool ResumeCard()
        {
            return true;
        }
        public override bool StartCard()
        {
            int res = CXCMD_TestUnitReady(pSlot, pID);
            LogClass.WriteToLog("TestUnitReady: res = {0}", ErrorDesc(res));
            return (res == 0);
        }
        public override bool StartJob()
        {
            int res = CXCMD_ScanPrinter(ref pSlot, ref pID);
            LogClass.WriteToLog("ScanPrinter: pSlot = {0}, pID = {1}, res = {2}", pSlot, pID, ErrorDesc(res));
            if (res != 0)
                return false;
            bool isConn = CXCMD_CheckIfConnected(ref pSlot, ref pID);
            LogClass.WriteToLog("CheckIfConnected: res = {0}", isConn);
            return isConn;
        }
        public override bool StopJob()
        {
            return true;
        }
        public override string[] GetMagstripe()
        {
            return null;
        }
        public override void SetParams(params object[] pars)
        {
            if (pars == null)
                return;
            if (pars.Length > 0)
                TwoSide = Convert.ToBoolean(pars[0]);
        }
        private string ErrorDesc(int error)
        {
            error = (error < 0) ? error * -1 : error;
            switch (error)
            {
                case 0x0:
                    return "0";
                case 0x01052A00:
                    return String.Format("{0:x} The command is issued out of order", error);
                case 0x01062800:
                    return String.Format("{0:x} The printer was inialized by pressing the RESET button", error);
                case 0x01062900:
                    return String.Format("{0:x} The printer was inialized by turning on the printer power", error);                
                default:
                    return String.Format("{0:x} unknown error", error);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing.Printing;
using System.Globalization;
using System.Printing;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using dxp01sdk;
using ProcardWPF;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace Devices
{
    public class XPSPrinter : PrinterClass, iXPS
    {
        [XmlIgnore]
        public BidiSplWrap bidi = null;
        public int HopperID;

        private int printJobID = 0;

        protected bool noTopper;

        public bool NoTopper
        {
            get { return noTopper; }
            set
            {
                noTopper = value;
                RaisePropertyChanged("NoTopper");
            }
        }

        protected bool graphicTopper;

        public bool GraphicTopper
        {
            get { return graphicTopper; }
            set
            {
                graphicTopper = value;
                RaisePropertyChanged("GraphicTopper");
            }
        }

        public XPSPrinter()
        {
            printerName = "";
            HopperID = 1;
            graphicTopper = false;
        }
        public override bool StartJob()
        {
            try
            {
                SendMessage(MessageType.ProductionMessage, "StatusJobStart");
                if (OnlyPrint)
                    return true;
                if (bidi == null)
                    bidi = new BidiSplWrap();
                bidi.BindDevice(printerName);
            }
            catch (Exception e)
            {
                SendMessage(MessageType.CardError, e.Message);
            }
            return true;
        }
        public override bool StopJob()
        {
            try
            {
                SendMessage(MessageType.ProductionMessage, "StatusJobEnd");
                if (OnlyPrint)
                    return true;
                bidi.UnbindDevice();
            }
            catch (Exception e)
            {
                //SendMessage(MessageType.CardError, e.Message);
            }
            return true;            
        }
        public override bool StartCard()
        {
            SendMessage(MessageType.ProductionMessage, "StatusCardStart");
            magstripe[0] = ""; magstripe[1] = ""; magstripe[2] = "";
            if (OnlyPrint)
            {
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            try
            {
                String hopper = (HopperID >= 2 && HopperID <= 6) ? $"{HopperID}" : String.Empty;
                _firstPage = true;
                printJobID = Util.StartJob(bidi, false, hopper);
                SendMessage(MessageType.CompleteStep, "");
            }
            catch (Exception e)
            {
                SendMessage(MessageType.CardError, e.Message);
            }
            return true;
        } 
        public string GetPrinterOption2()
        {
            return bidi.GetPrinterData(strings.PRINTER_OPTIONS2);
        }
        public bool IsOneWire()
        {
            PrinterOptionsValues options = Util.ParsePrinterOptionsXML(GetPrinterOption2());
            return options._optionSmartcard.ToLower() == "single wire";
        }

        public override bool ReadMagstripe()
        {
            string printerStatusXML = bidi.GetPrinterData(strings.MAGSTRIPE_READ);
            PrinterStatusValues printerStatusValues = Util.ParsePrinterStatusXML(printerStatusXML);
            
            magstripe[0] = ""; magstripe[1] = ""; magstripe[2] = "";
            if (0 == printerStatusValues._errorCode)
            {
                string track1 = "";
                string track2 = "";
                string track3 = "";

                Util.ParseMagstripeStrings(printerStatusXML, ref track1, ref track2, ref track3, false);

                if (track1.Length != 0)
                {
                    byte[] binaryData = System.Convert.FromBase64String(track1);
                    magstripe[0] = System.Text.Encoding.UTF8.GetString(binaryData);
                }
                if (track2.Length != 0)
                {
                    byte[] binaryData = System.Convert.FromBase64String(track2);
                    magstripe[1] = System.Text.Encoding.UTF8.GetString(binaryData);
                }
                if (track3.Length != 0)
                {
                    byte[] binaryData = System.Convert.FromBase64String(track3);
                    magstripe[2] = System.Text.Encoding.UTF8.GetString(binaryData);
                }
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            SendMessage(MessageType.CardError, printerStatusValues._errorString);
            return false;
        }
        private void EncodeMagstripe()
        {
            // Hardcoded XML to encode all 3 tracks in IAT mode.
            // track 1 = "TRACK1", track 2 = "1122", track 3 = "321"
            string trackDataXML = String.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<magstripe>"
                + "<track number=\"1\"><base64Data>{0}</base64Data></track>"
                + "<track number=\"2\"><base64Data>{1}</base64Data></track>"
                + "<track number=\"3\"><base64Data>{2}</base64Data></track>"
                + "</magstripe>", Convert.ToBase64String(HugeLib.Utils.String2Bin(magstripe[0])),
                Convert.ToBase64String(HugeLib.Utils.String2Bin(magstripe[1])),
                Convert.ToBase64String(HugeLib.Utils.String2Bin(magstripe[2])));

            // replace schema string MAGSTRIPE_ENCODE to MAGSTRIPE_ENCODE_FRONT for front side encode
            string printerStatusXML = bidi.SetPrinterData(strings.MAGSTRIPE_ENCODE, trackDataXML);
            PrinterStatusValues printerStatusValues = Util.ParsePrinterStatusXML(printerStatusXML);

            if (0 != printerStatusValues._errorCode)
            {
                throw new BidiException(printerStatusValues._errorString, printerStatusValues._printerJobID, printerStatusValues._errorCode);
            }
        }

        public override string[] GetMagstripe()
        {
            return magstripe;
        }

        public override bool FeedCard(ProcardWPF.FeedType feedType)
        {
            if (OnlyPrint)
            {
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            SendMessage(MessageType.ProductionMessage, "StatusFeed");
            // в принтере для чтения магнитки отдельной команды для ее загона туда нет
            if (feedType == FeedType.Magstripe)
            {
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            //var parkCommand = parkBack ? strings.SMARTCARD_PARK_BACK : strings.SMARTCARD_PARK;
            string printerStatusXML = bidi.SetPrinterData(strings.SMARTCARD_PARK);
            PrinterStatusValues printerStatusValues = Util.ParsePrinterStatusXML(printerStatusXML);
            if (0 != printerStatusValues._errorCode)
            {
                throw new BidiException(
                    "SmartcardPark() fail" + printerStatusValues._errorString,
                    printerStatusValues._printerJobID,
                    printerStatusValues._errorCode);
            }
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        public override bool ResumeCard()
        {
            if (OnlyPrint)
            {
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            string xmlFormat = strings.PRINTER_ACTION_XML;
            string input = string.Format(xmlFormat, (int)Actions.Resume, printJobID, 0);
            bidi.SetPrinterData(strings.PRINTER_ACTION, input);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }

        public override bool PrintCard()
        {
            SendMessage(MessageType.ProductionMessage, "PrintingInProgress");
            try
            {
                if (!String.IsNullOrEmpty(magstripe[0]) || !String.IsNullOrEmpty(magstripe[1]) ||
                    !String.IsNullOrEmpty(magstripe[2]))
                {
                    if (!OnlyPrint)
                        EncodeMagstripe();
                }

                PrintQueue printQueue = new PrintQueue(new LocalPrintServer(), printerName);
                PrintTicket printTicket = new PrintTicket();
                printTicket.Duplexing = Duplexing.TwoSidedLongEdge;
                printTicket.PageOrientation = PageOrientation.Landscape;
                printTicket.PageResolution = new PageResolution(300, 300, PageQualitativeResolution.Draft);

                ValidationResult validationResult = printQueue.MergeAndValidatePrintTicket(printQueue.UserPrintTicket, printTicket);

                string xmlString = PrintTicketXml.Prefix;
                xmlString += (NoTopper) ? PrintTicketXml.ToppingOff : PrintTicketXml.ToppingOn;
                xmlString += PrintTicketXml.FlipFrontNone;
                xmlString += PrintTicketXml.Suffix;

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xmlString);
                MemoryStream memoryStream = new MemoryStream();
                xmlDocument.Save(memoryStream);
                memoryStream.Position = 0;

                printTicket = new PrintTicket(memoryStream);
                validationResult = printQueue.MergeAndValidatePrintTicket(validationResult.ValidatedPrintTicket, printTicket);
                printQueue.UserPrintTicket = validationResult.ValidatedPrintTicket;
                printQueue.Commit();

                System.Windows.Controls.PrintDialog printDialog =
                    new System.Windows.Controls.PrintDialog
                    {
                        PrintQueue = printQueue
                        //PrintQueue = new PrintQueue(new PrintServer(), printerName)
                    };
                
                if (dvFrontImage != null)
                    printDialog.PrintVisual(dvFrontImage, "Print");
                if (dvBackImage != null)
                    printDialog.PrintVisual(dvBackImage, "Print");

                //PrintDocument printDocument = new PrintDocument();
                //printDocument.PrinterSettings.PrinterName = printerName;
                //printDocument.PrintController = new StandardPrintController();
                //printDocument.BeginPrint += PrintDocument_BeginPrint;
                //printDocument.QueryPageSettings += PrintDocument_QueryPageSettings;
                //printDocument.PrintPage += PrintDocument_PrintPage;

                //printDocument.Print();
                if (OnlyPrint)
                {
                    SendMessage(MessageType.CompleteStep, "");
                    return true;
                }
                Util.WaitForWindowsJobID(bidi, printerName);
                bidi.SetPrinterData(strings.ENDJOB);
                //string printerStatusXML = bidi.SetPrinterData(strings.ENDJOB);

                //PrinterStatusValues printerStatusValues = Util.ParsePrinterStatusXML(printerStatusXML);
                //if (0 != printerStatusValues._errorCode)
                //{
                //    throw new BidiException(printerStatusValues._errorString, printerStatusValues._printerJobID, printerStatusValues._errorCode);
                //}
                
                Util.PollForJobCompletion(bidi, printJobID);
                SendMessage(MessageType.CompleteStep, "");
            }
            catch (Exception ex)
            {
                SendMessage(MessageType.CardError, ex.Message);
                return false;
            }
            return true;
        }
        public override bool EndCard()
        {
            if (OnlyPrint)
            {
                SendMessage(MessageType.CardOK, "StatusComplete");
                return true;
            }
            bidi.SetPrinterData(strings.ENDJOB);
            SendMessage(MessageType.ProductionMessage, "StatusCardEnd");
            SendMessage(MessageType.ProductionMessage, "StatusComplete");
            SendMessage(MessageType.CompleteStep, "");
            SendMessage(MessageType.CardOK, "StatusComplete");
            return true;
        } 
        public override bool RemoveCard(ResultCard resultCard)
        {
            try
            {
                Util.CancelJob(bidi, printJobID, 0);
            }
            catch { }

            return true;
        }
        private void PrintDocument_QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
        }
        private void PrintDocument_BeginPrint(object sender, PrintEventArgs e)
        {
            PrintQueue printQueue = new PrintQueue(new LocalPrintServer(), printerName);
            PrintTicket printTicket = new PrintTicket();
            printTicket.Duplexing = Duplexing.TwoSidedLongEdge;
            printTicket.PageOrientation = PageOrientation.Landscape;
            printTicket.PageResolution = new PageResolution(300, 300, PageQualitativeResolution.Draft);

            ValidationResult validationResult = printQueue.MergeAndValidatePrintTicket(printQueue.UserPrintTicket, printTicket);

            string xmlString = PrintTicketXml.Prefix;
            xmlString += PrintTicketXml.FlipFrontNone;
            xmlString += PrintTicketXml.Suffix;

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlString);
            MemoryStream memoryStream = new MemoryStream();
            xmlDocument.Save(memoryStream);
            memoryStream.Position = 0;

            printTicket = new PrintTicket(memoryStream);
            validationResult = printQueue.MergeAndValidatePrintTicket(validationResult.ValidatedPrintTicket, printTicket);
            printQueue.UserPrintTicket = validationResult.ValidatedPrintTicket;
            printQueue.Commit();
        }
        public override void SetParams(params object[] pars)
        {
            if (pars == null)
                return;
            if (pars.Length > 0)
                TwoSide = Convert.ToBoolean(pars[0]);
        }
    }
}

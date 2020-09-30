using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.IO.Ports;

namespace Devices
{
    public enum MessageType
    {
        Debug,
        ProductionMessage,
        CardOK,
        CardError,
        NoAnswer,
        CompleteStep
    }
    public enum ResultCard
    {
        GoodCard,
        RejectCard
    }
    public interface iXPS
    {
        bool IsOneWire();
    }
    public abstract class DeviceClass : INotifyPropertyChanged
    {
        protected string errorMsg = "";
        protected ProcardWPF.DeviceType deviceType;
        protected bool nowPrinting;
        public delegate void PassMessage(MessageType messageType, string message);
        
        public ProcardWPF.DeviceType DeviceType
        {
            get
            {
                return deviceType;
            }
            set
            {
                deviceType = value;
            }
        }
        public static bool IsEmbosser(ProcardWPF.DeviceType dt)
        {
            switch(dt)
            {
                case ProcardWPF.DeviceType.DC450:
                case ProcardWPF.DeviceType.DC150:
                case ProcardWPF.DeviceType.CE:
                    return true;
            }
            return false;
        }
        public static bool IsXPS(ProcardWPF.DeviceType dt)
        {
            switch(dt)
            {
                case ProcardWPF.DeviceType.CD:
                case ProcardWPF.DeviceType.CE:
                    return true;
            }
            return false;
        }
        public static bool IsPrinter(ProcardWPF.DeviceType dt)
        {
            switch (dt)
            {
                case ProcardWPF.DeviceType.CD:
                case ProcardWPF.DeviceType.CE:
                case ProcardWPF.DeviceType.SR:
                    return true;
            }
            return false;
        }


        protected string[] magstripe = new string[3];
        public DeviceClass()
        { } 
        public abstract bool StartJob();
        public abstract bool StopJob();

        public virtual int FindHopper(int[] hoppers)
        {
            return 1;
        }

        public string GetLastError()
        {
            return errorMsg;
        }
        public void SetNowPrinting(bool val)
        {
            nowPrinting = val;
        }
        public abstract bool StartCard();
        public abstract bool EndCard();
        public abstract bool FeedCard(ProcardWPF.FeedType feedType);
        public abstract bool ReadMagstripe();
        public abstract string[] GetMagstripe();
        public abstract bool ResumeCard();
        public abstract bool PrintCard();
        public abstract bool RemoveCard(ResultCard resultCard);

        public abstract void SetParams(params object[] pars);

        public event PassMessage eventPassMessage;
        public void SendMessage(MessageType messageType, string msg)
        {
            if (eventPassMessage != null)
                eventPassMessage(messageType, msg);
        }
        public bool HasMessage()
        {
            return eventPassMessage != null;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class Simulator : DeviceClass
    {
        public Simulator()
        {
            deviceType = ProcardWPF.DeviceType.Simulator;
        }
        public override bool StartJob()
        {
            SendMessage(MessageType.ProductionMessage, "StatusJobStart");
            System.Threading.Thread.Sleep(1000);
            return true;
        }
        public override bool StopJob()
        {
            SendMessage(MessageType.ProductionMessage, "StatusJobEnd");
            System.Threading.Thread.Sleep(1000);
            return true;
        }
        public override bool StartCard()
        {
            SendMessage(MessageType.ProductionMessage, "StatusCardStart");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        public override bool EndCard()
        {
            SendMessage(MessageType.ProductionMessage, "StatusCardEnd");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CardOK, "");
            return true;
        }
        public override bool FeedCard(ProcardWPF.FeedType feedType)
        {
            SendMessage(MessageType.ProductionMessage, "StatusFeed");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        public override bool ReadMagstripe()
        {
            SendMessage(MessageType.ProductionMessage, "MagstripeRead");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        public override string[] GetMagstripe()
        {
            return new string[] { "", "", ""};
        } 
        public override bool ResumeCard()
        {
            SendMessage(MessageType.ProductionMessage, "ResumePrinting");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        public override bool PrintCard()
        {
            SendMessage(MessageType.ProductionMessage, "PrintingInProgress");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        public override bool RemoveCard(ResultCard resultCard)
        {
            SendMessage(MessageType.ProductionMessage, "RemoveCard");
            System.Threading.Thread.Sleep(2000);
            SendMessage(MessageType.CompleteStep, "");
            return true;
        }
        /// <summary>
        /// Разнообразные нужные параметры
        /// </summary>
        /// <param name="pars">1 - true/false - двусторонная или односторонняя печать для принтеров</param>
        public override void SetParams(params object[] pars)
        {
        }
    }
}

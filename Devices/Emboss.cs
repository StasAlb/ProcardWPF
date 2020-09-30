using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.IO.Ports;
using ProcardWPF;

namespace Devices
{
    public enum DoubleIndent : int
    {
        None = 0,
        Front = 1,
        Back = 2
    }
    public enum SpeedType : int
    {
        Standard = 0,
        Mag,
        Smart
    }
    public abstract class Embosser : DeviceClass
    {
        protected abstract byte ACK
        {
            get;
        }
        protected abstract byte NAK
        {
            get;
        }
        protected byte Complete
        {
            get
            {
                return 0x43;
            }
        }
        public byte ENQ
        {
            get { return 0x05;}
        }
        private string portName;
        public string PortName
        {
            get
            {
                return portName;
            }
            set
            {
                portName = value;
            }
        }
        private int baudRate;
        public int BaudRate
        {
            get
            {
                return baudRate;
            }
            set
            {
                baudRate = value;
            }
        }
        private Parity portParity;
        public Parity PortParity
        {
            get
            {
                return portParity;
            }
            set
            {
                portParity = value;
            }
        }
        private int dataBits;
        public int DataBits
        {
            get
            {
                return dataBits;
            }
            set
            {
                dataBits = value;
            }
        }
        private StopBits portStopBits;
        public StopBits PortStopBit
        {
            get
            {
                return portStopBits;
            }
            set
            {
                portStopBits = value;
            }
        }

        private SerialPort sp = null;
        int timeAnswer = 2000, timeNoResponse = 30000, timeLongMessage = 1000;
        System.Timers.Timer timerAnswer = null; // ответ на одно сообщение
        System.Timers.Timer timerNoResponse = null; // если вообще нет ответа
        System.Timers.Timer timerLongMessage = null; // таймер на длинные сообщения (дорожки)

        private bool WeWaitForLong = false;
        protected Queue queue = new Queue();
        private string tracks = "";

        public Embosser()
        {
            portName = "";
            baudRate = 9600;
            portParity = Parity.None;
            dataBits = 8;
            portStopBits = StopBits.One;

            timerAnswer = new System.Timers.Timer(timeAnswer);
            timerNoResponse = new System.Timers.Timer(timeNoResponse);
            timerLongMessage = new System.Timers.Timer(timeLongMessage);
            timerAnswer.AutoReset = false;
            timerNoResponse.AutoReset = false;
            timerLongMessage.AutoReset = false;
            timerAnswer.Elapsed += TimerAnswer_Elapsed;
            timerNoResponse.Elapsed += TimerNoResponse_Elapsed;
            timerLongMessage.Elapsed += TimerLongMessage_Elapsed;
        }

        private void TimerLongMessage_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            tracks = HugeLib.Utils.AHex2String(tracks);
            int index1 = tracks.IndexOf("%");
            int index2 = tracks.IndexOf("?", (index1 >= 0) ? index1 : 0);
            if (index1 >= 0 && index2 >= 0)
                magstripe[0] = tracks.Substring(index1 + 1, index2 - index1 - 1);
            index1 = tracks.IndexOf(";");
            index2 = tracks.IndexOf("?", (index1 >= 0) ? index1 : 0);
            if (index1 >= 0 && index2 >= 0)
                magstripe[1] = tracks.Substring(index1 + 1, index2 - index1 - 1);
            index1 = tracks.IndexOf("_;");
            index2 = tracks.IndexOf("?", (index1 >= 0) ? index1 : 0);
            if (index1 >= 0 && index2 >= 0)
                magstripe[2] = tracks.Substring(index1 + 2, index2 - index1 - 2);
            if (tracks.IndexOf(";") == tracks.IndexOf("_;") + 1) // проверка что при поиске второй мы не нашли третью
                magstripe[1] = "";
            SendMessage(MessageType.Debug, String.Format("Message: {0}", tracks));
            SendMessage(MessageType.CompleteStep, "");
        }

        private void TimerNoResponse_Elapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            SendMessage(MessageType.CardError, "StatusNoAnswer");
            timerAnswer.Dispose();
        }
        public void AddToQueue(string str)
        {
            WeWaitForLong = false;
            if (deviceType == DeviceType.DC450 && ((DC450)this).Speed == SpeedType.Smart)
                queue.Enqueue(HugeLib.Utils.Bin2AHex(ENQ));
            queue.Enqueue(HugeLib.Utils.String2AHex(str));
//            if (deviceType != DeviceType.DC450 || ((DC450)(this)).Speed != SpeedType.Mag)
//                queue.Enqueue(HugeLib.Utils.Bin2AHex(ENQ));
        }
        private void TimerAnswer_Elapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            SendToDevice();
        }
        public override bool StartJob()
        {
            if (sp != null)
                sp.Close();
            sp = new SerialPort(portName);
            sp.BaudRate = baudRate;
            sp.Parity = portParity;
            sp.DataBits = dataBits;
            sp.StopBits = PortStopBit;
            try
            {
                sp.Open();
                sp.DataReceived += Sp_DataReceived;
            }
            catch (Exception ex)
            {
                SendMessage(MessageType.Debug, ex.Message);
                errorMsg = ex.Message;
                return false;
            }
            return true;
        }
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int cnt = sp.BytesToRead;
            if (cnt > 0)
            {
                timerAnswer.Stop();
                timerNoResponse.Stop();
                byte[] msg = new byte[cnt];
                sp.Read(msg, 0, cnt);
                string message = HugeLib.Utils.Bin2AHex(msg);
                //SendMessage(MessageType.Debug, "<-- " + message);
                if (WeWaitForLong)
                {
                    timerLongMessage.Stop();
                    tracks += message;
                    timerLongMessage.Start();
                }
                else
                {
                    //SendMessage(MessageType.Debug, "Message: " + HugeLib.Utils.Bin2AHex(msg));
                    for (int i = 0; i < cnt - 1; i++)
                    {
                        if (msg[i] == NAK || msg[i] == ACK)
                            continue;
                        EvaluateAnswer(msg[i]);
                    }
                    EvaluateAnswer(msg[cnt - 1]);
                }
            }
            else
            {
                SendMessage(MessageType.Debug, "event rise but no message");
            }
        }
        private void EvaluateAnswer(byte answ)
        {
            if (answ == ACK)
            {
                if (queue.Count > 0)
                    queue.Dequeue();
                SendMessage(MessageType.Debug, "<-- ACK");
                SendMessage(MessageType.ProductionMessage, "StatusAck");
                if (queue.Count == 0)
                    SendMessage(MessageType.CompleteStep, "");
                else
                    SendToDevice();
                return;
            }
            if (answ == NAK)
            {
                SendMessage(MessageType.Debug, "<-- NAK");
                SendMessage(MessageType.ProductionMessage, "StatusNak");
                Thread.Sleep(1000);
                SendToDevice();
                return;
            }
            if (answ == Complete)
            {
                if (deviceType == DeviceType.DC450 && ((DC450)this).Speed == SpeedType.Smart) //если ускорение по чипу, то CardComplete приравниваем к ACK
                {
                    if (queue.Count > 0)
                        queue.Dequeue();
                    SendMessage(MessageType.Debug, "<-- ACK");
                    SendMessage(MessageType.ProductionMessage, "StatusAck");
                    if (queue.Count == 0)
                        SendMessage(MessageType.CompleteStep, "");
                    else
                        SendToDevice();
                    return;
                }
                if (queue.Count > 0)
                    queue.Dequeue();
                SendMessage(MessageType.Debug, "<-- Card Complete");
                SendMessage(MessageType.ProductionMessage, "StatusComplete");
                SendMessage(MessageType.CompleteStep, "");
                SendMessage(MessageType.CardOK, "StatusComplete");
                return;
            }
            switch(answ)
            {
                case 0x42:
                case 0x62:
                case 0x44:
                case 0x45:
                case 0x47:
                case 0x48:
                case 0x4a:
                case 0x4b:
                case 0x4d:
                case 0x4e:
                case 0x4f:
                case 0x50:
                case 0x51:
                case 0x52:
                case 0x54:
                case 0x55:
                case 0x56:
                case 0x57:
                case 0x5a:
                    SendMessage(MessageType.Debug, String.Format("<-- {0:X2}", answ));
                    SendMessage(MessageType.ProductionMessage, String.Format("Status_{0}", (char)answ));
                    break;
                default:
                    SendMessage(MessageType.Debug, String.Format("<-- Unknown {0:X2}", answ));
                    break;
            }
        }
        public override bool StopJob()
        {
            if (sp == null)
                return true;
            sp.Close();
            sp.Dispose();
            return true;
        }
        protected void SendToDevice()
        {
            if (queue.Count == 0 && deviceType == DeviceType.DC450 && ((DC450)this).Speed == SpeedType.Smart && nowPrinting)
            {
                SendMessage(MessageType.CompleteStep, "");
                return;
            }
            if (queue.Count == 0)
                return;
            string str = (string)queue.Peek();
            //SendMessage(MessageType.Debug, "--> " + str);
            byte[] msg = HugeLib.Utils.AHex2Bin(str);
            str = HugeLib.Utils.Bin2String(msg);
            if (msg.Length == 1)
            {
                if (msg[0] == ENQ)
                    str = "ENQ";
                if (msg[0] == NAK)
                    str = "NAK";
                if (msg[0] == ACK)
                    str = "ACK";
                if (msg[0] == Complete)
                    str = "Card complete";
            }
            SendMessage(MessageType.Debug, String.Format("--> {0}", str));
            sp.Write(msg, 0, msg.Length);
            if (msg.Length > 1)
            {
                Thread.Sleep(500);
                queue.Dequeue();
                SendToDevice();
            }
            else
            {
                timerNoResponse.Start();
                timerAnswer.Start();
            }
        }
        public override bool StartCard()
        {
            queue.Clear();
            if (!sp.IsOpen)
            {
                errorMsg = String.Format("{0} not opened", sp.PortName);
                return false;
            }
            if (deviceType == DeviceType.DC450 && ((DC450)(this)).Speed == SpeedType.Smart) //для ускорения по чипу не ждем окончания остального, чтобы начать персонализацию
            {
                Thread.Sleep(1000); //ждем секунду, чтобы успел сработать захват
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            queue.Enqueue(HugeLib.Utils.Bin2AHex(ENQ));
            if (deviceType == DeviceType.DC450 && ((DC450)(this)).Speed != SpeedType.Standard) // для ускорения по магнитке и по чипу не ждем отклика от эмбоссера в начале (он может быть занят)
                queue.Dequeue();
            SendToDevice();
            return true;
        }
        public override bool EndCard()
        {
            if (deviceType == DeviceType.DC450 && ((DC450)(this)).Speed != SpeedType.Standard) //для ускорения по магнитке и по чипу не ждем окончания остального
            {
                SendMessage(MessageType.CompleteStep, "");
                return true;
            }
            queue.Clear();
            queue.Enqueue(HugeLib.Utils.Bin2AHex(ENQ));
            SendToDevice();
            return true;
        }
        public override bool FeedCard(ProcardWPF.FeedType feedType)
        {
            return true;
        }
        public override bool ReadMagstripe()
        {
            WeWaitForLong = true;
            magstripe[0] = ""; magstripe[1] = ""; magstripe[2] = "";
            tracks = "";
            queue.Clear();
            queue.Enqueue(HugeLib.Utils.String2AHex(String.Format("000000#DCC##TRK##END#@@@@@@{0}{1}", (char)0x0d, (char)0x0a)));
            SendToDevice();
            return true;
        }
        public override string[] GetMagstripe()
        {
            return magstripe;
        }
        public override bool ResumeCard()
        {
            return true;
        }
        public override bool PrintCard()
        {
            SendToDevice();
            return true;
        }
        public override bool RemoveCard(ResultCard resultCard)
        {
            return true;
        }
        public override void SetParams(params object[] pars)
        {
        }
    }
    public class DC450 : Embosser
    {
        private DoubleIndent dIndent;
        public DoubleIndent DIndent
        {
            get
            {
                return dIndent;
            }
            set
            {
                dIndent = value;
            }
        }
        private double dopOffset;
        public double DopOffset
        {
            get
            {
                return dopOffset;
            }
            set
            {
                dopOffset = value;
            }
        }
        private SpeedType speed;
        public SpeedType Speed
        {
            get
            {
                return speed;
            }
            set
            {
                speed = value;
            }
        } 
        protected override byte ACK
        {
            get
            {
                return (speed == SpeedType.Mag) ? (byte)0x11 : (byte)0x41;
            }
        }
        protected override byte NAK
        {
            get
            {
                return 0x13;
            }
        }
        public DC450()
        {
            deviceType = ProcardWPF.DeviceType.DC450;
            dIndent = DoubleIndent.None;
            dopOffset = 0.0;
            speed = SpeedType.Standard;
        }
        public override bool FeedCard(FeedType feedType)
        {
            if (feedType == FeedType.SmartFront)
                SendMessage(MessageType.CompleteStep, "");
            if (feedType == FeedType.Magstripe)
            {
                queue.Clear();
                queue.Enqueue(HugeLib.Utils.String2AHex(String.Format("000000#DCC##FED##END#@@@@@@{0}{1}", (char)0x0d, (char)0x0a)));
                queue.Enqueue(HugeLib.Utils.Bin2AHex(ENQ));
                SendToDevice();
            }
            return true;
        }
    }
    public class DC150 : Embosser
    {
        protected override byte ACK
        {
            get
            {
                return (byte)0x41;
            }
        }
        protected override byte NAK
        {
            get
            {
                return (byte)0x15;
            }
        }
        public DC150()
        {
            deviceType = DeviceType.DC150;
        }
    }
}
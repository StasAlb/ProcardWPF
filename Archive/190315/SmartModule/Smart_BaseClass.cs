using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;

namespace SmartModule
{
    public enum SmartType
    {
        None = 0,
        OstcardStandard = 8,
        OstcardAdvance = 9
    }
    [Serializable()]
    [XmlInclude(typeof(OstcardStandard))]
    public abstract class SmartModule : INotifyPropertyChanged
    {
        protected T CreateDynamicDllInvoke<T>(string functionName, string library)
        {
            #region linkproof
            //http://stackoverflow.com/questions/1660761/parameterising-dllimport-for-use-in-a-c-sharp-application
            #endregion
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicDllInvoke"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicDllModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicDllInvokeType", TypeAttributes.Public | TypeAttributes.UnicodeClass);
            MethodInfo delegateMI = typeof(T).GetMethod("Invoke");
            
            Type[] delegateParams = (from param in delegateMI.GetParameters() select param.ParameterType).ToArray();
            MethodBuilder methodBuilder = typeBuilder.DefinePInvokeMethod(functionName, library, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                CallingConventions.Standard, delegateMI.ReturnType, delegateParams, CallingConvention.Winapi, System.Runtime.InteropServices.CharSet.Ansi);
            methodBuilder.SetImplementationFlags(methodBuilder.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);
            Type dynamicType = typeBuilder.CreateType();
            MethodInfo methodInfo = dynamicType.GetMethod(functionName);
            return (T)(object)Delegate.CreateDelegate(typeof(T), methodInfo, true);
        }

        protected SmartType sType;
        public SmartType SType
        {
            get
            {
                return sType;
            }
            set
            {
                sType = value;
            }
        }
        private int timeout;
        public int Timeout
        {
            get
            {
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }
        protected string path;
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                RaisePropertyChanged("Path");
            }
        }

        protected string feedback;
        [XmlIgnore]
        public abstract string Desc
        {
            get; set;
        }
            
        public SmartModule()
        {
            timeout = 30;
            feedback = "";
        }
        public abstract bool DefineFunction();
        public abstract int WriteCard(string data);
        public abstract int GetData(string data);
        public string GetFeedback()
        {
            return feedback;
        }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
    public class OstcardStandard : SmartModule
    {
        //public delegate int WriteCardInvoke(string script, byte[] data, int dataLength, int readerIndex, string hsIP, int hsPort);
        public delegate int WriteCardInvoke(string script, StringBuilder data, int dataLength, int readerIndex, string hsIP, int hsPort);
        public delegate int WriteCardInvokeOneWire(string script, StringBuilder data, int dataLength, string readerName, Int32 tp, string hsIP, int hsPort);

        private string hsIP;
        public string HSIP
        {
            get
            {
                return hsIP;
            }
            set
            {
                hsIP = value;
                RaisePropertyChanged("HSIP");
            }
        }
        private int hsPort;
        public int HSPort
        {
            get
            {
                return hsPort;
            }
            set
            {
                hsPort = value;
            }
        }
        private string script;
        public string Script
        {
            get
            {
                return script;
            }
            set
            {
                script = value;
            }
        }
        private string readerName;
        public string ReaderName
        {
            get
            {
                return readerName;
            }
            set
            {
                readerName = value;
            }
        }
        private int readerIndex;
        public int ReaderIndex
        {
            get
            {
                return readerIndex;
            }
            set
            {
                readerIndex = value;
            }
        }
        private bool oneWire;
        public bool OneWire
        {
            get
            {
                return oneWire;
            }
            set
            {
                oneWire = value;
                RaisePropertyChanged("OneWire");
                RaisePropertyChanged("NotOneWire");
            }
        }
        private string oneWireProtocol;
        public string OneWireProtocol
        { get
            {
                return oneWireProtocol;
            }
            set
            {
                oneWireProtocol = value;
                RaisePropertyChanged("OneWireProtocol");
            }
        }
        private string oneWireType;

        public string OneWireType
        {
            get
            {
                return oneWireType;
            }
            set
            {
                oneWireType = value;
                RaisePropertyChanged("OneWireType");
            }
        }
        private int oneWireParameter
        {
            get
            {
                if (oneWireProtocol == "T0")
                {
                    if (oneWireType == "Contact")
                        return 201;
                    if (oneWireType == "Contactless")
                        return 202;
                }
                if (oneWireProtocol == "T1")
                {
                    if (oneWireType == "Contact")
                        return 211;
                    if (oneWireType == "Contactless")
                        return 212;
                }
                return 201;
            }
        }
        [XmlIgnore]
        public bool NotOneWire
        {
            get
            {
                return !oneWire;
            }
            set
            {
                oneWire = !value;
                RaisePropertyChanged("OneWire");
                RaisePropertyChanged("NotOneWire");

            }
        }
        private WriteCardInvoke scppPerso = null;
        private WriteCardInvoke scppGetData = null;

        private WriteCardInvokeOneWire scppPersoOneWire = null;
        private WriteCardInvokeOneWire scppGetDataOneWire = null;
        [XmlIgnore]
        public override string Desc
        {
            get
            {
                return String.Format("Ostcard: {0}", (String.IsNullOrEmpty(script) ? "no script" : script));
            }
            set { }
        }
        public OstcardStandard() : base()
        {
            sType = SmartType.OstcardStandard;
            hsIP = "127.0.0.1";
            hsPort = 1600;
            script = "";
            readerName = "";
            readerIndex = 0;
            oneWire = false;
        }
        public override bool DefineFunction()
        {
            if (oneWire)
            {
                scppPersoOneWire = CreateDynamicDllInvoke<WriteCardInvokeOneWire>("SCPP_Personalize3", path);
                scppGetDataOneWire = CreateDynamicDllInvoke<WriteCardInvokeOneWire>("GetData3", path);
            }
            else
            {
                scppPerso = CreateDynamicDllInvoke<WriteCardInvoke>("SCPP_Personalize2", path);
                scppGetData = CreateDynamicDllInvoke<WriteCardInvoke>("GetData", path);
            }
            return true;
        }
        public override int WriteCard(string data)
        {
            if (oneWire && scppPersoOneWire == null)
                return 0xFF03;
            if (!oneWire && scppPerso == null)
                return 0xFF01;

            StringBuilder sb = new StringBuilder(data);

            if (oneWire)
            {
                int t = scppPersoOneWire(script, sb, data.Length, readerName, oneWireParameter, hsIP, hsPort);
                return t;
            }
            else
                return scppPerso(script, sb, data.Length, readerIndex, hsIP, hsPort);
        }
        public override int GetData(string data)
        {
            feedback = "";
            if (!oneWire && scppGetData == null)
                return 0xFF02;
            if (oneWire && scppGetDataOneWire == null)
                return 0xFF04;
            StringBuilder sb = new StringBuilder(1024);
            sb.Append(data);
            int res = 0;
            if (oneWire)
                res = scppGetDataOneWire(script, sb, 1024, readerName, oneWireParameter, hsIP, hsPort);
            else
                res = scppGetData(script, sb, 1024, readerIndex, hsIP, hsPort);
            if (res > 0 && res < 1024)
            feedback = sb.ToString().Substring(0, res);
            return res;
        }
    }
}
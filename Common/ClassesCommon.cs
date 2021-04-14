using System;
using System.ComponentModel;
using System.Reflection;
namespace ProcardWPF
{ 
    public enum DeviceType : byte
    {
        None = 0,
        Simulator = 1,
        DC450 = 2,
        CD = 3,
        DC150 = 4,
        SR = 6,
        CE = 7
    }
    // 1,2 - сравниваем для зарегистрированного индента для 450-го
    public enum SideType : int
    {
        Front = 1,
        Back = 2,
        FrontColor = 3,
        FrontMono = 4,
        BackColor = 5,
        BackMono = 6
    }
    public enum Step
    {
        None,
        Start,
        Perso,
        ReadMag,
        GetMagData,
        Print,
        End,
        FeedSmart,
        FeedMag,
        Resume
    }
    public enum FeedType
    {
        SmartFront,
        SmartBack,
        Magstripe,
        SmartContactless,
        Print,
        PrintAfterTurn,
        NotDefine = -1
    }
    public enum ReaderModel
    {
        None,
        SCMmicro,
        DUALi,
        Identive,
        Acs
    }
}

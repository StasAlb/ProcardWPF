//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Linq;
//using System.Threading.Tasks;
using System.Windows;

namespace ProcardWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SettingManager.OnStartup();
            base.OnStartup(e);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            SettingManager.OnExit();
            base.OnExit(e);
        }
    }
}

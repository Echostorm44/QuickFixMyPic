using QuickFixMyPic.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuickFixMyPic;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public Mutex instanceMutex;

    public App()
    {
        SingleInstanceCheck();
    }

    public void SingleInstanceCheck()
    {
        bool isOnlyInstance = false;
        instanceMutex = new Mutex(true, @"ImageConverter", out isOnlyInstance);
        if(!isOnlyInstance)
        {
            Thread.Sleep(2000);// Give the named pipe a sec to startup on main instance

            var args = Environment.GetCommandLineArgs();
            //System.IO.File.WriteAllText(@"g:\Temp\ImageConverterTesting\" + Guid.NewGuid().ToString(), args.Length.ToString());
            string putThemBackTogether = string.Join(' ', args);
            // We need to do this because the GetCommandLineArgs was breaking up by spaces && when you had files || folders with spaces they'd come in broken
            var runningDllFullPathToRemove = System.Reflection.Assembly.GetEntryAssembly().Location;
            // The DLL for the app is one of the 2 parameters that gets sent, we need to rip that out so we only have the file passed in.            
            var onlyTheParam = putThemBackTogether.Replace(runningDllFullPathToRemove, "");

            var manager = new NamedPipeManager("ImageConverter");
            manager.Write(onlyTheParam);

            Environment.Exit(0);
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using Naldthal;
using EasyHook;

namespace NaldthalInjector
{
    static class Program
    {
        private static BridgeInterface _bridge;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var process = Process.GetProcessesByName("ffxiv_dx11")[0];

            var dll = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Naldthal.dll");

            _bridge = new BridgeInterface();

            string channelName = null;
            RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, _bridge);

            RemoteHooking.Inject(
                process.Id,          // ID of process to inject into
                InjectionOptions.DoNotRequireStrongName,
                dll,   // 32-bit library to inject (if target is 32-bit)
                dll,   // 64-bit library to inject (if target is 64-bit)
                channelName,
                Path.GetFullPath("data.json")
            );

            Console.ReadLine();
        }
    }
}

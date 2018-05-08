using System;
using System.Diagnostics;
using System.IO;
using Naldthal;
using EasyHook;

namespace NaldthalInjector
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var process = Process.GetProcessesByName("ffxiv_dx11")[0];

            string channelName = null;
            var bridge = new BridgeInterface();
            RemoteHooking.IpcCreateServer(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, bridge);

            RemoteHooking.Inject(
                process.Id,
                InjectionOptions.NoService | InjectionOptions.DoNotRequireStrongName,
                null,
                Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "payload.dll"),
                channelName,
                Path.GetFullPath("data.json")
            );

            Console.WriteLine("Press enter key to unload.");
            Console.ReadLine();
        }
    }
}

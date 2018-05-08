using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using EasyHook;
using Naldthal;

namespace NaldthalInjector
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Press enter key to unload.");
            Console.WriteLine();

            var process = Process.GetProcessesByName("ffxiv_dx11")[0];

            string channelName = null;
            var bridge = new Bridge();
            RemoteHooking.IpcCreateServer(ref channelName, WellKnownObjectMode.Singleton, bridge);

            RemoteHooking.Inject(
                process.Id,
                InjectionOptions.NoService | InjectionOptions.DoNotRequireStrongName,
                null,
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "payload.dll"),
                channelName,
                Path.GetFullPath("data.json")
            );

            Console.ReadLine();
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Windows.Forms;
using EasyHook;
using Naldthal;
using NaldthalInjector.Properties;

namespace NaldthalInjector
{
    internal static class Program
    {
        private static readonly Bridge Bridge = new Bridge();

        [STAThread]
        private static void Main(string[] args)
        {
#if DEBUG
            Console.OutputEncoding = Encoding.UTF8;
#endif

            var process = Process.GetProcessesByName("ffxiv_dx11")[0];

            string channelName = null;
            RemoteHooking.IpcCreateServer(ref channelName, WellKnownObjectMode.Singleton, Bridge);

            RemoteHooking.Inject(
                process.Id,
                InjectionOptions.NoService | InjectionOptions.DoNotRequireStrongName,
                null,
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "payload.dll"),
                channelName,
                Path.GetFullPath("data.json")
            );

#if DEBUG
            Console.ReadLine();
#else
            Application.Run(new SystemTrayApplicationContext());
#endif
        }

#if !DEBUG
        private class SystemTrayApplicationContext : ApplicationContext
        {
            private readonly NotifyIcon _trayIcon;

            public SystemTrayApplicationContext()
            {
                _trayIcon = new NotifyIcon
                {
                    Icon = Resources.AppIcon,
                    ContextMenu = new ContextMenu(new[]
                    {
                        new MenuItem("로그", (sender, e) =>
                        {
                            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
                            File.WriteAllLines(path, Bridge.Logs);
                            Process.Start(path);
                        }),
                        new MenuItem("종료", (sender, e) =>
                        {
                            _trayIcon.Visible = false;
                            Application.Exit();
                        })
                    }),
                    Visible = true
                };
            }
        }
#endif
    }
}
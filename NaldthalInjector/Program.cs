using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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

            try
            {
                var processes = Process.GetProcessesByName("ffxiv_dx11");
                if (processes.Length == 0)
                {
                    throw new Exception("ffxiv_dx11.exe 프로세스를 찾을 수 없습니다.");
                }

#if !DEBUG
                new WebClient().DownloadFile("https://raw.githubusercontent.com/devunt/Naldthal/master/Naldthal/data.json", "data.json");
#endif

                string channelName = null;
                RemoteHooking.IpcCreateServer(ref channelName, WellKnownObjectMode.Singleton, Bridge);

                RemoteHooking.Inject(
                    processes[0].Id,
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
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
                Console.ReadLine();
#else
                MessageBox.Show("실행에 오류가 발생했습니다.\n\n" + ex, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
            }
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

                _trayIcon.ShowBalloonTip(5000, "Naldthal", "실행됨", ToolTipIcon.Info);
            }
        }
#endif
    }
}
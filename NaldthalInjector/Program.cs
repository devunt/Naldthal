using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EasyHook;
using Naldthal;
using NaldthalInjector.Properties;

namespace NaldthalInjector
{
    internal static class Program
    {
        private const string Version = "v20181104.2";
        private static readonly Bridge Bridge = new Bridge();
        private static Mutex _mutex;

        [STAThread]
        private static void Main(string[] args)
        {
#if DEBUG
            Console.OutputEncoding = Encoding.UTF8;
#endif

            Bridge.WriteLine($"Running Naldthal {Version}");

            try
            {
                _mutex = new Mutex(true, "Naldthal", out var acquired);
                if (!acquired)
                {
                    throw new Exception("이미 실행중입니다.");
                }

                var process = GetProcess();
                var datapath = Path.GetFullPath("data.json");

#if !DEBUG
                // new WebClient().DownloadFile("https://raw.githubusercontent.com/devunt/Naldthal/master/Naldthal/data.json", datapath);
#endif

                string channelName = null;
                RemoteHooking.IpcCreateServer(ref channelName, WellKnownObjectMode.Singleton, Bridge);

                RemoteHooking.Inject(
                    process.Id,
                    InjectionOptions.NoService | InjectionOptions.DoNotRequireStrongName,
                    null,
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "payload.dll"),
                    channelName,
                    datapath
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

        private static Process GetProcess()
        {
            var processes = Process.GetProcessesByName("ffxiv_dx11");
            if (processes.Length == 0)
            {
                throw new Exception("ffxiv_dx11.exe 프로세스를 찾을 수 없습니다.");
            }

            return processes[0];
        }

#if !DEBUG
        private class SystemTrayApplicationContext : ApplicationContext
        {
            private readonly NotifyIcon _trayIcon;

            public SystemTrayApplicationContext()
            {
                var path = Path.Combine(Path.GetTempPath(), "Naldthal.log");
                _trayIcon = new NotifyIcon
                {
                    Text = $"날달 {Version}",
                    Icon = Resources.AppIcon,
                    ContextMenu = new ContextMenu(new[]
                    {
                        new MenuItem("로그", (sender, e) =>
                        {
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

                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            GetProcess();
                        }
                        catch
                        {
                            _trayIcon.Visible = false;
                            Application.Exit();
                        }

                        await Task.Delay(1000);
                    }
                });

                _trayIcon.ShowBalloonTip(1000, "Naldthal", "실행중입니다...", ToolTipIcon.Info);
            }
        }
#endif
    }
}
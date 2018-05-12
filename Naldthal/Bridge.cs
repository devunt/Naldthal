using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Naldthal
{
    public class Bridge : MarshalByRefObject
    {
        public List<string> Logs { get; } = new List<string>();

        public void WriteLine(object format, params object[] args)
        {
            if (format is IntPtr ptr)
            {
                format = ptr.ToString("X");
            }

            if (format is IEnumerable<byte> bytes)
            {
                format = BitConverter.ToString(bytes.ToArray()).Replace('-', ' ');
            }

            var datetime = DateTime.Now.ToString("HH:mm:ss");
            var formatted = $"[{datetime}] {string.Format(format.ToString(), args)}";

#if DEBUG
            Console.WriteLine(formatted);
#else
            Logs.Add(formatted);
#endif
        }

        public void Error(Exception ex)
        {
#if DEBUG
            WriteLine(ex);
#else
            MessageBox.Show("실행중 오류가 발생했습니다.\n\n" + ex, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
#endif
        }

        public void Ping()
        {
        }
    }
}
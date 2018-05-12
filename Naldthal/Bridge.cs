using System;
using System.Collections.Generic;
using System.Linq;

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

        public void Ping()
        {
        }
    }
}
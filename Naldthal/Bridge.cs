using System;
using System.Collections.Generic;
using System.Linq;

namespace Naldthal
{
    public class Bridge : MarshalByRefObject
    {
        public void WriteLine()
        {
            Console.WriteLine();
        }

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

            Console.WriteLine(format.ToString(), args);
        }

        public void Ping()
        {
        }
    }
}
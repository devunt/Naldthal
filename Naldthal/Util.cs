using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

namespace Naldthal
{
    internal static class Util
    {
        public static Process CurrentProcess { get; }

        static Util()
        {
            CurrentProcess = Process.GetCurrentProcess();
        }

        public static byte[] ReadMemory(IntPtr offset, int size)
        {
            if (offset == IntPtr.Zero)
            {
                throw new Exception("ReadMemory: offset is zero.");
            }

            var b = new byte[size];
            var _ = IntPtr.Zero;

            NativeMethods.ReadProcessMemory(CurrentProcess.Handle, offset, b, size, ref _);

            return b;
        }

        public static byte[] ReadMemory(IntPtr offset, int addOffset, int size)
        {
            return ReadMemory(IntPtr.Add(offset, addOffset), size);
        }

        public static void WriteMemory(IntPtr offset, byte[] buffer, int size)
        {
            if (offset == IntPtr.Zero)
            {
                throw new Exception("WriteMemory: offset is zero.");
            }

            NativeMethods.WriteProcessMemory(CurrentProcess.Handle, offset, buffer, (uint)size, out var _);
        }

        public static void WriteMemory(IntPtr offset, int addOffset, byte[] buffer, int size)
        {
            WriteMemory(IntPtr.Add(offset, addOffset), buffer, size);
        }

        public static byte[] ReadCStringAsBytes(IntPtr offset)
        {
            using (var ms = new MemoryStream(1024))
            {
                for (var off = 0; off <= 1024; off += 8)
                {
                    var bytes = ReadMemory(offset, off, 8);
                    var z = Array.IndexOf<byte>(bytes, 0);

                    if (z == -1)
                    {
                        ms.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        ms.Write(bytes, 0, z);
                        return ms.ToArray();
                    }
                }
            }

            return new byte[0];
        }

        public static string ReadCString(IntPtr offset)
        {
            return Encoding.UTF8.GetString(ReadCStringAsBytes(offset));
        }

        public static string ReadString(IntPtr offset, int size)
        {
            var bytes = ReadMemory(offset, size);
            var z = Array.IndexOf(bytes, 0);
            return Encoding.UTF8.GetString(bytes, 0, z == -1 ? bytes.Length : z);
        }

        public static string ReadString(IntPtr offset, int addOffset, int size)
        {
            return ReadString(IntPtr.Add(offset, addOffset), size);
        }

        public static void WriteString(IntPtr offset, string text, int size)
        {
            var buffer = new byte[size];
            var encoded = Encoding.UTF8.GetBytes(text);
            var encodedSize = Math.Min(encoded.Length, size - 1);

            Array.Copy(encoded, buffer, encodedSize);
            buffer[encodedSize] = 0;

            WriteMemory(offset, buffer, size);
        }

        public static void WriteString(IntPtr offset, int addOffset, string text, int size)
        {
            WriteString(IntPtr.Add(offset, addOffset), text, size);
        }

        public static void Log(object format, params object[] args)
        {
            if (format is IntPtr ptr)
            {
                format = ptr.ToString("X");
            }

            if (format is IEnumerable<byte> bytes)
            {
                format = BitConverter.ToString(bytes.ToArray()).Replace('-', ' ');
            }

            Debug.WriteLine("[NT] "  + string.Format(format.ToString(), args));
        }

        public static void WriteColorizedString(MemoryStream ms, string text, Color color)
        {
            var encoded = Encoding.UTF8.GetBytes(text);

            ms.Write(new byte[] { 0x02, 0x13, 0x06, 0xfe, 0xff, color.R, color.G, color.B, 0x03 }, 0, 9);
            ms.Write(encoded, 0, encoded.Length);
            ms.Write(new byte[] { 0x02, 0x13, 0x02, 0xec, 0x03 }, 0, 5);
        }
    }

    public class Color
    {
        public static readonly Color Header = new Color(0xF3, 0xF3, 0xF3);
        public static readonly Color Normal = new Color(0xEE, 0xEE, 0xEE);
        public static readonly Color Cost   = new Color(0xAA, 0xFF, 0xAA);
        public static readonly Color Place  = new Color(0xC3, 0xDE, 0xE7);
        public static readonly Color Misc   = new Color(0xCC, 0xCC, 0xCC);

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}

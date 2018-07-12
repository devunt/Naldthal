using System;
using System.Linq;

namespace Naldthal
{
    internal class Pattern
    {
        private const int ChunkSize = 4096;

        // 140809DF0
        public static Pattern GetItemTooltipDescriptionMethod { get; } = new Pattern
        {
            Bytes = new byte[]
            {
                0x48, 0x33, 0xc4,                               // xor rax, rsp
                0x48, 0x89, 0x84, 0x24, 0xa0, 0x00, 0x00, 0x00, // mov qword ptr ss:[rsp+A0],rax
                0x48, 0x8b, 0xf1,                               // mov rsi, rcx
                0x4d, 0x8b, 0xf0,                               // mov r14, r8
                0x48, 0x8d, 0x4c, 0x24, 0x30,                   // lea rcx, qword ptr ss:[rsp+30]
            },
            OffsetType = PatternOffsetType.RelativeToBegin,
            Offset = -22
        };

        private byte[] Bytes { get; set; }
        private PatternOffsetType OffsetType { get; set; }
        private int Offset { get; set; }

        public static IntPtr Search(Pattern pattern)
        {
            var baseAddress = Util.CurrentProcess.MainModule.BaseAddress;
            var size = Util.CurrentProcess.MainModule.ModuleMemorySize;
            var endAddress = IntPtr.Add(baseAddress, size).ToInt64();

            var patternSize = pattern.Bytes.Length;

            var matchedOffset = IntPtr.Zero;
            var offset = baseAddress;
            while (offset.ToInt64() < endAddress)
            {
                var memory = Util.ReadMemory(offset, ChunkSize);
                for (var i = 0; i < memory.Length - patternSize; i++)
                {
                    var matchcount = pattern.Bytes.TakeWhile((t, j) => t == 0xff || memory[i + j] == t).Count();

                    if (matchcount == patternSize)
                    {
                        matchedOffset = IntPtr.Add(offset, i);
                        break;
                    }
                }

                if (matchedOffset != IntPtr.Zero)
                {
                    break;
                }

                offset = IntPtr.Add(offset, ChunkSize);
            }

            if (matchedOffset == IntPtr.Zero)
            {
                throw new Exception("Pattern matching failed.");
            }

            if (pattern.OffsetType == PatternOffsetType.RelativeToEnd)
            {
                matchedOffset = IntPtr.Add(matchedOffset, patternSize);
            }

            matchedOffset = IntPtr.Add(matchedOffset, pattern.Offset);

            return matchedOffset;
        }

        private enum PatternOffsetType
        {
            RelativeToBegin,
            RelativeToEnd
        }
    }
}
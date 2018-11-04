using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;
using Newtonsoft.Json;

namespace Naldthal
{
    internal static class Hook
    {
        private static Bridge _bridge;
        private static Data _data;

        private static LocalHook _getItemTooltipDescriptionHook;
        private delegate IntPtr GetItemTooltipDescriptionDelegate(IntPtr self, IntPtr descriptionText, IntPtr additionalText);
        private static GetItemTooltipDescriptionDelegate _getItemTooltipDescriptionOrigMethod;

        private static readonly Dictionary<int, IntPtr> CachedBufferAddrs = new Dictionary<int, IntPtr>();

        public static void Initialize(Bridge bridge, string dataJsonPath)
        {
            _bridge = bridge;
            _data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(dataJsonPath));

            WriteLine("Data loaded.");
        }

        public static void Install()
        {
            WriteLine("Pattern searching...");

            var address = Pattern.Search(Pattern.GetItemTooltipDescriptionMethod);

            WriteLine($"Pattern found at 0x{address.ToInt64():X}.");

            _getItemTooltipDescriptionHook = LocalHook.Create(address, new GetItemTooltipDescriptionDelegate(GetItemTooltipDescription), null);
            _getItemTooltipDescriptionHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            _getItemTooltipDescriptionOrigMethod = Marshal.GetDelegateForFunctionPointer<GetItemTooltipDescriptionDelegate>(_getItemTooltipDescriptionHook.HookBypassAddress);

            WriteLine("Hook installed.");

            WriteLine("");
            WriteLine("Successfully initialized.");
            WriteLine("");
        }

        public static void Join()
        {
            while (true)
            {
                _bridge.Ping();

                Thread.Sleep(1000);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        public static void Release()
        {
            _getItemTooltipDescriptionHook.Dispose();
            LocalHook.Release();

            foreach (var buffer in CachedBufferAddrs.Values)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static IntPtr GetItemTooltipDescription(IntPtr self, IntPtr descriptionText, IntPtr additionalText)
        {
            if ((NativeMethods.GetAsyncKeyState(0x10) & 0x8000) > 0) // VK_SHIFT
            {
                try
                {
                    var itemId = Marshal.ReadInt32(self, 300);
                    bool isHq = false, isCollectible = false;

                    if (itemId > 1000000)
                    {
                        itemId -= 1000000;
                        isHq = true;
                    }
                    else if (itemId > 500000)
                    {
                        itemId -= 500000;
                        isCollectible = true;
                    }

                    if (CachedBufferAddrs.TryGetValue(itemId, out var cachedBufferAddr))
                    {
                        return _getItemTooltipDescriptionOrigMethod(self, descriptionText, cachedBufferAddr);
                    }

                    using (var ms = new MemoryStream())
                    {
                        if (_data.Shops.TryGetValue(itemId, out var shops))
                        {
                            var i = 0;
                            foreach (var shop in shops)
                            {
                                var costItem = _data.Metadata.Items[shop.CostId];
                                var lastCode = (costItem.Name[costItem.Name.Length - 1] - 0xAC00) % 28;
                                var josa = lastCode == 0 || lastCode == 8 ? "로" : "으로";

                                ms.WriteColoredString("> 다음 NPC 에서 ", Color.Normal);
                                ms.WriteColoredString($"{shop.CostAmount} {costItem.Name}", Color.Cost);
                                ms.WriteColoredString($"{josa} 교환", Color.Normal);
                                ms.WriteByte(0xA);

                                foreach (var sellerId in shop.SellerIds)
                                {
                                    var npc = _data.Metadata.NPCs[sellerId];
                                    var name = npc.Title == "" || npc.Title.StartsWith("상인") ? npc.Name : npc.Title;
                                    foreach (var location in npc.Locations)
                                    {
                                        ms.WriteColoredString("  - ", Color.Misc);
                                        ms.WriteColoredString(_data.Metadata.Placenames[location.PlaceId], Color.Place);
                                        ms.WriteColoredString($" - {name} ({location.X}, {location.Y})", Color.Misc);
                                        ms.WriteByte(0xA);
                                        i++;
                                    }

                                    if (i >= 10)
                                    {
                                        i = 0;
                                        ms.WriteColoredString("  - ...이하 생략됨", Color.Misc);
                                        ms.WriteByte(0xA);
                                        break;
                                    }
                                }
                            }
                        }

                        if (_data.GCShops.TryGetValue(itemId, out var gcshop))
                        {
                            ms.WriteColoredString("> 총사령부에서 ", Color.Normal);
                            ms.WriteColoredString($"{gcshop.CostAmount} 군표", Color.Cost);
                            ms.WriteColoredString("로 교환", Color.Normal);
                            ms.WriteByte(0xA);
                        }

                        if (_data.FCShops.TryGetValue(itemId, out var fcshop))
                        {
                            ms.WriteColoredString("> 총사령부에서 ", Color.Normal);
                            ms.WriteColoredString($"{fcshop.CostAmount} 부대 명성", Color.Cost);
                            ms.WriteColoredString("으로 교환", Color.Normal);
                            ms.WriteByte(0xA);
                        }

                        if (_data.Craftings.TryGetValue(itemId, out var craftings))
                        {
                            if (craftings.Length == 1)
                            {
                                var classjobName = _data.Metadata.ClassJobCats[craftings[0].ClassJobCat];
                                ms.WriteColoredString("> ", Color.Normal);
                                ms.WriteColoredString($"{craftings[0].Level}레벨 {classjobName}", Color.Cost);
                                ms.WriteColoredString("로 제작", Color.Normal);
                                ms.WriteByte(0xA);
                            }
                            else
                            {
                                ms.WriteColoredString("> 다음 제작자 클래스로 제작", Color.Normal);
                                ms.WriteByte(0xA);

                                foreach (var crafting in craftings)
                                {
                                    var classjobName = _data.Metadata.ClassJobCats[crafting.ClassJobCat];
                                    ms.WriteColoredString($"  - {crafting.Level}레벨 {classjobName}", Color.Cost);
                                    ms.WriteByte(0xA);
                                }
                            }
                        }

                        if (_data.Gatherings.TryGetValue(itemId, out var gatherings))
                        {
                            if (gatherings.Length == 1)
                            {
                                var classjobName = _data.Metadata.ClassJobCats[gatherings[0].ClassJobCat];
                                ms.WriteColoredString("> ", Color.Normal);
                                ms.WriteColoredString($"{gatherings[0].Level}레벨 {classjobName}", Color.Cost);
                                ms.WriteColoredString("로 채집", Color.Normal);
                                ms.WriteByte(0xA);
                            }
                            else
                            {
                                ms.WriteColoredString("> 다음 채집가 클래스로 채집", Color.Normal);
                                ms.WriteByte(0xA);

                                foreach (var gathering in gatherings)
                                {
                                    var classjobName = _data.Metadata.ClassJobCats[gathering.ClassJobCat];
                                    ms.WriteColoredString($"  - {gathering.Level}레벨 {classjobName}", Color.Cost);
                                    ms.WriteByte(0xA);
                                }
                            }
                        }

                        if (_data.RetainerTasks.TryGetValue(itemId, out var retainerTasks))
                        {
                            if (retainerTasks.Length == 1)
                            {
                                var classjobName = _data.Metadata.ClassJobCats[retainerTasks[0].ClassJobCat];
                                classjobName = classjobName == "투사 마법사" ? "전투" : classjobName;
                                ms.WriteColoredString("> ", Color.Normal);
                                ms.WriteColoredString($"{classjobName} 집사 {retainerTasks[0].Level}레벨", Color.Cost);
                                ms.WriteColoredString("로 조달", Color.Normal);
                                ms.WriteByte(0xA);
                            }
                            else
                            {
                                ms.WriteColoredString("> 다음 클래스 집사로 조달", Color.Normal);
                                ms.WriteByte(0xA);

                                foreach (var retainerTask in retainerTasks)
                                {
                                    var classjobName = _data.Metadata.ClassJobCats[retainerTask.ClassJobCat];
                                    classjobName = classjobName == "투사 마법사" ? "전투" : classjobName;
                                    ms.WriteColoredString($"  - {classjobName} 집사 {retainerTask.Level}레벨", Color.Cost);
                                    ms.WriteByte(0xA);
                                }
                            }
                        }

                        if (_data.InstanceContentIds.TryGetValue(itemId, out var icIds))
                        {
                            ms.WriteColoredString("> 다음 임무에서 입수", Color.Normal);
                            ms.WriteByte(0xA);
    
                            for (var i = 0; i < icIds.Length; i += 2)
                            {
                                var icNames = string.Join(", ", icIds.Skip(i).Take(2).Select(icId => _data.Metadata.InstanceContents[icId].Name));
                                ms.WriteColoredString($"  - {icNames}", Color.Place);
                                ms.WriteByte(0xA);
                            }
                        }

#if DEBUG
                        ms.WriteColoredString($"> ID: {itemId}", Color.Debug);
                        ms.WriteByte(0xA);
                        ms.WriteColoredString($"> HQ: {isHq}, Cltb: {isCollectible}", Color.Debug);
                        ms.WriteByte(0xA);
#endif

                        if (ms.Length > 0)
                        {
                            using (var ms2 = new MemoryStream())
                            {
                                if (additionalText != IntPtr.Zero)
                                {
                                    var bytes = Util.ReadCStringAsBytes(additionalText);
                                    ms2.Write(bytes, 0, bytes.Length);
                                    ms2.WriteByte(0xA);
                                }

                                ms2.WriteByte(0xA);
                                ms2.WriteColoredString("[입수 방법]", Color.Header);
                                ms2.WriteByte(0xA);
                                ms.WriteTo(ms2);

                                var buffer = ms2.ToArray();
                                var size = Math.Min(buffer.Length, 2047);

                                var bufferAddr = Marshal.AllocHGlobal(size + 1);
                                CachedBufferAddrs[itemId] = bufferAddr;
                                Util.WriteMemory(bufferAddr, buffer, size);
                                Marshal.WriteByte(bufferAddr, size, 0);

                                return _getItemTooltipDescriptionOrigMethod(self, descriptionText, bufferAddr);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLine(ex);
                }
            }

            return _getItemTooltipDescriptionOrigMethod(self, descriptionText, additionalText);
        }

        private static void WriteLine(object format, params object[] args)
        {
            _bridge.WriteLine(format, args);
        }
    }
}
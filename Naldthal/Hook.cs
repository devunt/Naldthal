using System;
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

        private static IntPtr _sharedBuffer;

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

            _sharedBuffer = Marshal.AllocHGlobal(2048);
            WriteLine($"Shared buffer allocated at 0x{_sharedBuffer.ToInt64():X}.");

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

                Thread.Sleep(500);
            }

            // ReSharper disable once FunctionNeverReturns
        }

        public static void Release()
        {
            _getItemTooltipDescriptionHook.Dispose();
            LocalHook.Release();

            Marshal.FreeHGlobal(_sharedBuffer);
        }

        private static IntPtr GetItemTooltipDescription(IntPtr self, IntPtr descriptionText, IntPtr additionalText)
        {
            if ((NativeMethods.GetAsyncKeyState(0x10) & 0x8000) > 0) // VK_SHIFT
            {
                try
                {
                    var itemId = Marshal.ReadInt32(self, 292);

                    if (itemId > 1000000)
                    {
                        itemId -= 1000000;
                    }
                    else if (itemId > 500000)
                    {
                        itemId -= 500000;
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
                                    var name = npc.Title == "" ? npc.Name : npc.Title;
                                    foreach (var location in npc.Locations)
                                    {
                                        ms.WriteColoredString("  - ", Color.Misc);
                                        ms.WriteColoredString(_data.Metadata.Placenames[location.PlaceId],
                                            Color.Place);
                                        ms.WriteColoredString($" - {name} ({location.X}, {location.Y})",
                                            Color.Misc);
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
                                var classjobName = _data.Metadata.CrafterTypeNames[craftings[0].CrafterType];
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
                                    var classjobName = _data.Metadata.CrafterTypeNames[crafting.CrafterType];
                                    ms.WriteColoredString($"  - {crafting.Level}레벨 {classjobName}", Color.Cost);
                                    ms.WriteByte(0xA);
                                }
                            }
                        }

                        if (_data.Gatherings.TryGetValue(itemId, out var gatherings))
                        {
                            if (gatherings.Length == 1)
                            {
                                var classjobName = _data.Metadata.GathererTypeNames[gatherings[0].GathererType];
                                ms.WriteColoredString("> ", Color.Normal);
                                ms.WriteColoredString($"{classjobName}", Color.Cost);
                                ms.WriteColoredString("로 채집", Color.Normal);
                                ms.WriteByte(0xA);
                            }
                            else
                            {
                                ms.WriteColoredString("> 다음 채집가 클래스로 채집", Color.Normal);
                                ms.WriteByte(0xA);

                                foreach (var gathering in gatherings)
                                {
                                    var classjobName = _data.Metadata.GathererTypeNames[gathering.GathererType];
                                    ms.WriteColoredString($"  - {classjobName}", Color.Cost);
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
                                ms2.WriteByte(0x0);

                                var buffer = ms2.ToArray();
                                Util.WriteMemory(_sharedBuffer, buffer, Math.Min(buffer.Length, 2040));
                            }

                            additionalText = _sharedBuffer;
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
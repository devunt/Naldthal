using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using EasyHook;
using Newtonsoft.Json;

namespace Naldthal
{
    internal static class Hook
    {
        private static BridgeInterface _bridge;
        private static Data _data;

        private delegate IntPtr AllocItemTooltipDescStrDelegate(IntPtr a1, IntPtr item, IntPtr price);
        private static LocalHook _allocItemTooltipDescStrHook;
        private static AllocItemTooltipDescStrDelegate _allocItemTooltipDescStrOrigMethod;

        private static IntPtr _sharedBuffer;

        public static void Initialize(BridgeInterface bridge, string dataJsonPath)
        {
            _bridge = bridge;
            _data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(dataJsonPath));

            _bridge.WriteLine("Data loaded.");
        }

        public static void Install()
        {
            _bridge.WriteLine("Pattern matching...");

            var address = Pattern.Search(Pattern.SetTooltipDescriptionMethod);

            _bridge.WriteLine($"Pattern matched at 0x{address.ToInt64():X}");

            _allocItemTooltipDescStrHook = LocalHook.Create(address, new AllocItemTooltipDescStrDelegate(AllocItemTooltipDescStrHook), new object());
            _allocItemTooltipDescStrHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            _allocItemTooltipDescStrOrigMethod = (AllocItemTooltipDescStrDelegate)Marshal.GetDelegateForFunctionPointer(_allocItemTooltipDescStrHook.HookBypassAddress, typeof(AllocItemTooltipDescStrDelegate));

            _bridge.WriteLine("Hook installed.");

            _sharedBuffer = Marshal.AllocHGlobal(2048);
            _bridge.WriteLine($"Shared buffer allocated at 0x{_sharedBuffer.ToInt64():X}");
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
            _allocItemTooltipDescStrHook.Dispose();
            LocalHook.Release();

            Marshal.FreeHGlobal(_sharedBuffer);
        }

        private static IntPtr AllocItemTooltipDescStrHook(IntPtr self, IntPtr descriptionText, IntPtr additionalText)
        {
            try
            {
                var itemId = Marshal.ReadInt32(self, 292);

                if (itemId > 1000000)
                {
                    itemId -= 1000000;
                }

                using (var ms = new MemoryStream())
                {
                    if (_data.Shops.TryGetValue(itemId, out var shops))
                    {
                        var i = 0;
                        foreach (var shop in shops)
                        {
                            var costItem  = _data.Metadata.Items[shop.CostId];
                            var lastCode = (costItem.Name[costItem.Name.Length - 1] - 0xAC00) % 28;
                            var josa = lastCode == 0 || lastCode == 8 ? "로" : "으로";

                            Util.WriteColorizedString(ms, "> 다음 NPC 에서 ", Color.Normal);
                            Util.WriteColorizedString(ms, $"{shop.CostAmount} {costItem.Name}", Color.Cost);
                            Util.WriteColorizedString(ms, $"{josa} 교환", Color.Normal);
                            ms.WriteByte(0xA);

                            foreach (var sellerId in shop.SellerIds)
                            {
                                var npc = _data.Metadata.NPCs[sellerId];
                                var name = npc.Title == "" ? npc.Name : npc.Title;
                                foreach (var location in npc.Locations)
                                {
                                    Util.WriteColorizedString(ms, "  - ", Color.Misc);
                                    Util.WriteColorizedString(ms, _data.Metadata.Placenames[location.PlaceId], Color.Place);
                                    Util.WriteColorizedString(ms, $" - {name} ({location.X}, {location.Y})", Color.Misc);
                                    ms.WriteByte(0xA);
                                    i++;
                                }

                                if (i >= 10)
                                {
                                    i = 0;
                                    Util.WriteColorizedString(ms, "  - ...이하 생략됨", Color.Misc);
                                    ms.WriteByte(0xA);
                                    break;
                                }
                            }
                        }
                    }

                    if (_data.GCShops.TryGetValue(itemId, out var gcshop))
                    {
                        Util.WriteColorizedString(ms, "> 총사령부에서 ", Color.Normal);
                        Util.WriteColorizedString(ms, $"{gcshop.CostAmount} 군표", Color.Cost);
                        Util.WriteColorizedString(ms, "로 교환", Color.Normal);
                        ms.WriteByte(0xA);
                    }

                    if (_data.FCShops.TryGetValue(itemId, out var fcshop))
                    {
                        Util.WriteColorizedString(ms, "> 총사령부에서 ", Color.Normal);
                        Util.WriteColorizedString(ms, $"{fcshop.CostAmount} 부대 명성", Color.Cost);
                        Util.WriteColorizedString(ms, "으로 교환", Color.Normal);
                        ms.WriteByte(0xA);
                    }

                    if (_data.Craftings.TryGetValue(itemId, out var craftings))
                    {
                        if (craftings.Length == 1)
                        {
                            var classjobName = _data.Metadata.CrafterTypeNames[craftings[0].CrafterType];
                            Util.WriteColorizedString(ms, "> ", Color.Normal);
                            Util.WriteColorizedString(ms, $"{craftings[0].Level}레벨 {classjobName}", Color.Cost);
                            Util.WriteColorizedString(ms, "로 제작", Color.Normal);
                            ms.WriteByte(0xA);
                        }
                        else
                        {
                            Util.WriteColorizedString(ms, "> 다음 제작자 클래스로 제작", Color.Normal);
                            ms.WriteByte(0xA);

                            foreach (var crafting in craftings)
                            {
                                var classjobName = _data.Metadata.CrafterTypeNames[crafting.CrafterType];
                                Util.WriteColorizedString(ms, $"  - {crafting.Level}레벨 {classjobName}", Color.Cost);
                                ms.WriteByte(0xA);
                            }
                        }
                    }

                    if (_data.Gatherings.TryGetValue(itemId, out var gatherings))
                    {
                        if (gatherings.Length == 1)
                        {
                            var classjobName = _data.Metadata.GathererTypeNames[gatherings[0].GathererType];
                            Util.WriteColorizedString(ms, "> ", Color.Normal);
                            Util.WriteColorizedString(ms, $"{classjobName}", Color.Cost);
                            Util.WriteColorizedString(ms, "로 채집", Color.Normal);
                            ms.WriteByte(0xA);
                        }
                        else
                        {
                            Util.WriteColorizedString(ms, "> 다음 채집가 클래스로 채집", Color.Normal);
                            ms.WriteByte(0xA);

                            foreach (var gathering in gatherings)
                            {
                                var classjobName = _data.Metadata.GathererTypeNames[gathering.GathererType];
                                Util.WriteColorizedString(ms, $"  - {classjobName}", Color.Cost);
                                ms.WriteByte(0xA);
                            }
                        }
                    }

                    /*
                    if (_data.InstanceContentIds.TryGetValue(itemId, out var icIds))
                    {
                        Util.WriteColorizedString(ms, "> 다음 임무에서 입수", Color.Normal);
                        ms.WriteByte(0xA);

                        for (var i = 0; i < icIds.Length; i += 2)
                        {
                            var icNames = string.Join(", ", icIds.Skip(i).Take(2).Select(icId => _data.Metadata.InstanceContents[icId].Name));
                            Util.WriteColorizedString(ms, $"  - {icNames}", Color.Place);
                            ms.WriteByte(0xA);
                        }
                    }
                    */

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
                            Util.WriteColorizedString(ms2, "[입수 방법]", Color.Header);
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
                _bridge.WriteLine(ex);
            }

            return _allocItemTooltipDescStrOrigMethod(self, descriptionText, additionalText);
        }
    }
}

﻿using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TPie.Helpers
{
    internal class ItemsHelper
    {
        private delegate void UseItem(IntPtr agent, uint itemId, uint unk1, uint unk2, short unk3);

        #region Singleton
        private ItemsHelper()
        {
            _useItemPtr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 89 74 24 ??");

            ExcelSheet<Item>? itemsSheet = Plugin.DataManager.GetExcelSheet<Item>();
            List<Item> validItems = itemsSheet?.Where(item => item.ItemAction.Row > 0).ToList() ?? new List<Item>();
            _usableItems = validItems.ToDictionary(item => item.RowId);

            ExcelSheet<EventItem>? eventItemsSheet = Plugin.DataManager.GetExcelSheet<EventItem>();
            List<EventItem> validEventItems = eventItemsSheet?.Where(item => item.Action.Row > 0).ToList() ?? new List<EventItem>();
            _usableEventItems = validEventItems.ToDictionary(item => item.RowId);
        }

        public static void Initialize() { Instance = new ItemsHelper(); }

        public static ItemsHelper Instance { get; private set; } = null!;

        ~ItemsHelper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Instance = null!;
        }
        #endregion

        private IntPtr _useItemPtr = IntPtr.Zero;
        private Dictionary<uint, Item> _usableItems;
        private Dictionary<uint, EventItem> _usableEventItems;

        private Dictionary<string, UsableItem> UsableItems = new Dictionary<string, UsableItem>();

        public unsafe void CalculateUsableItems()
        {
            InventoryManager* manager = InventoryManager.Instance();
            InventoryType[] inventoryTypes = new InventoryType[]
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
                InventoryType.KeyItems
            };

            UsableItems.Clear();

            try
            {
                foreach (InventoryType inventoryType in inventoryTypes)
                {
                    InventoryContainer* container = manager->GetInventoryContainer(inventoryType);
                    if (container == null) continue;

                    for (int i = 0; i < container->Size; i++)
                    {
                        try
                        {
                            InventoryItem* item = container->GetInventorySlot(i);
                            if (item == null) continue;

                            if (item->Quantity == 0) continue;

                            bool hq = (item->Flags & InventoryItem.ItemFlags.HighQuality) != 0;
                            string hqString = hq ? "_1" : "_0";
                            string key = $"{item->ItemId}{hqString}";

                            if (UsableItems.TryGetValue(key, out UsableItem? usableItem) && usableItem != null)
                            {
                                usableItem.Count += item->Quantity;
                            }
                            else
                            {
                                if (_usableItems.TryGetValue(item->ItemId, out Item? itemData) && itemData != null)
                                {
                                    UsableItems.Add(key, new UsableItem(itemData, hq, item->Quantity));
                                }
                                else if (_usableEventItems.TryGetValue(item->ItemId, out EventItem? eventItemData) && eventItemData != null)
                                {
                                    UsableItems.Add(key, new UsableItem(eventItemData, hq, item->Quantity));
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public UsableItem? GetUsableItem(uint itemId, bool hq)
        {
            string hqString = hq ? "_1" : "_0";
            string key = $"{itemId}{hqString}";

            if (UsableItems.TryGetValue(key, out UsableItem? value))
            {
                return value;
            }

            return null;
        }

        public List<UsableItem> GetUsableItems()
        {
            return UsableItems.Values.ToList();
        }

        public unsafe void Use(uint itemId)
        {
            if (_useItemPtr == IntPtr.Zero) return;

            AgentModule* agentModule = (AgentModule*)Plugin.GameGui.GetUIModule();
            IntPtr agent = (IntPtr)agentModule->GetAgentByInternalId((AgentId)10);

            UseItem usetItemDelegate = Marshal.GetDelegateForFunctionPointer<UseItem>(_useItemPtr);
            usetItemDelegate(agent, itemId, 999, 0, 0);
        }
    }

    public class UsableItem
    {
        public readonly string Name;
        public readonly uint ID;
        public readonly bool IsHQ;
        public readonly uint IconID;
        public uint Count;
        public readonly bool IsKey;

        public UsableItem(Item item, bool hq, uint count)
        {
            Name = item.Name;
            ID = item.RowId;
            IsHQ = hq;
            IconID = item.Icon;
            Count = count;
            IsKey = false;
        }

        public UsableItem(EventItem item, bool hq, uint count)
        {
            Name = item.Name;
            ID = item.RowId;
            IsHQ = hq;
            IconID = item.Icon;
            Count = count;
            IsKey = true;
        }

        public override string ToString()
        {
            return $"UsableItem: {ID}, {Name}, {IsHQ}, {IconID}, {Count}";
        }
    }
}

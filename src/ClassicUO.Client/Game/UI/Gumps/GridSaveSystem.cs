// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class GridSaveSystem
    {
        private const long TimeCutoffSeconds = 60L * 60 * 24 * 60;

        private static GridSaveSystem _instance;
        private readonly string _gridSavePath;
        private XDocument _saveDocument;
        private XElement _rootElement;
        private bool _enabled;

        public static GridSaveSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GridSaveSystem();
                }

                return _instance;
            }
        }

        private GridSaveSystem()
        {
            _gridSavePath = Path.Combine(ProfileManager.ProfilePath ?? ".", "GridContainers.xml");

            if (!SaveFileCheck())
            {
                _enabled = false;
                return;
            }

            try
            {
                _saveDocument = XDocument.Load(_gridSavePath);
            }
            catch
            {
                _saveDocument = new XDocument();
            }

            _rootElement = _saveDocument.Element("grid_gumps");
            if (_rootElement == null)
            {
                _saveDocument.Add(new XElement("grid_gumps"));
                _rootElement = _saveDocument.Root;
            }

            _enabled = true;
        }

        public bool SaveContainer(
            uint serial,
            IReadOnlyDictionary<int, GridSlotControl> gridSlots,
            int width,
            int height,
            int lastX,
            int lastY,
            bool? useOriginalContainer,
            bool autoSort)
        {
            if (!_enabled)
            {
                return false;
            }

            if (useOriginalContainer == null)
            {
                useOriginalContainer = false;
            }

            XElement thisContainer = _rootElement.Element("container_" + serial);
            if (thisContainer == null)
            {
                thisContainer = new XElement("container_" + serial);
                _rootElement.Add(thisContainer);
            }
            else
            {
                thisContainer.RemoveNodes();
            }

            thisContainer.SetAttributeValue("last_opened", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            thisContainer.SetAttributeValue("width", width.ToString());
            thisContainer.SetAttributeValue("height", height.ToString());
            thisContainer.SetAttributeValue("lastX", lastX.ToString());
            thisContainer.SetAttributeValue("lastY", lastY.ToString());
            thisContainer.SetAttributeValue("useOriginalContainer", useOriginalContainer.ToString());
            thisContainer.SetAttributeValue("autoSort", autoSort.ToString());

            foreach (var slot in gridSlots)
            {
                Item item = slot.Value.SlotItem;
                if (item == null)
                {
                    continue;
                }

                XElement itemSlot = new XElement("item");
                itemSlot.SetAttributeValue("serial", item.Serial.ToString());
                itemSlot.SetAttributeValue("locked", slot.Value.ItemGridLocked.ToString());
                itemSlot.SetAttributeValue("slot", slot.Key.ToString());
                thisContainer.Add(itemSlot);
            }

            RemoveOldContainers();

            try
            {
                _saveDocument.Save(_gridSavePath);
            }
            catch (Exception ex)
            {
                Log.Error($"GridSaveSystem SaveContainer: {ex.Message}");
                return false;
            }

            return true;
        }

        public List<GridItemSlotSaveData> GetItemSlots(uint container)
        {
            var items = new List<GridItemSlotSaveData>();

            XElement thisContainer = _rootElement.Element("container_" + container);
            if (thisContainer == null)
            {
                return items;
            }

            foreach (XElement itemSlot in thisContainer.Elements("item"))
            {
                XAttribute slotAttr = itemSlot.Attribute("slot");
                XAttribute serialAttr = itemSlot.Attribute("serial");
                XAttribute isLockedAttr = itemSlot.Attribute("locked");

                if (slotAttr == null || serialAttr == null)
                {
                    continue;
                }

                if (!int.TryParse(slotAttr.Value, out int slotV) || !uint.TryParse(serialAttr.Value, out uint serialV))
                {
                    continue;
                }

                bool isLocked = isLockedAttr != null && bool.TryParse(isLockedAttr.Value, out bool locked) && locked;
                items.Add(new GridItemSlotSaveData(slotV, serialV, isLocked));
            }

            return items;
        }

        public Point GetLastSize(uint container, int defaultWidth, int defaultHeight)
        {
            Point lastSize = new Point(defaultWidth, defaultHeight);

            XElement thisContainer = _rootElement.Element("container_" + container);
            if (thisContainer == null)
            {
                return lastSize;
            }

            XAttribute widthAttr = thisContainer.Attribute("width");
            XAttribute heightAttr = thisContainer.Attribute("height");
            if (widthAttr != null && heightAttr != null)
            {
                int.TryParse(widthAttr.Value, out lastSize.X);
                int.TryParse(heightAttr.Value, out lastSize.Y);
            }

            return lastSize;
        }

        public Point GetLastPosition(uint container, int defaultX, int defaultY)
        {
            Point lastPos = new Point(defaultX, defaultY);

            XElement thisContainer = _rootElement.Element("container_" + container);
            if (thisContainer == null)
            {
                return lastPos;
            }

            XAttribute lastXAttr = thisContainer.Attribute("lastX");
            XAttribute lastYAttr = thisContainer.Attribute("lastY");
            if (lastXAttr != null && lastYAttr != null)
            {
                int.TryParse(lastXAttr.Value, out lastPos.X);
                int.TryParse(lastYAttr.Value, out lastPos.Y);
            }

            return lastPos;
        }

        public bool UseOriginalContainerGump(uint container)
        {
            XElement thisContainer = _rootElement.Element("container_" + container);
            if (thisContainer == null)
            {
                return false;
            }

            XAttribute useOriginalAttr = thisContainer.Attribute("useOriginalContainer");
            if (useOriginalAttr == null)
            {
                return false;
            }

            return bool.TryParse(useOriginalAttr.Value, out bool useOriginal) && useOriginal;
        }

        public bool AutoSortContainer(uint container)
        {
            XElement thisContainer = _rootElement.Element("container_" + container);
            if (thisContainer == null)
            {
                return false;
            }

            XAttribute attr = thisContainer.Attribute("autoSort");
            if (attr == null)
            {
                return false;
            }

            return bool.TryParse(attr.Value, out bool autoSort) && autoSort;
        }

        private void RemoveOldContainers()
        {
            long cutOffTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - TimeCutoffSeconds;
            var removeMe = new List<XElement>();

            foreach (XElement container in _rootElement.Elements())
            {
                XAttribute lastOpened = container.Attribute("last_opened");
                if (lastOpened == null)
                {
                    continue;
                }

                if (!long.TryParse(lastOpened.Value, out long lo))
                {
                    continue;
                }

                if (lo < cutOffTime)
                {
                    removeMe.Add(container);
                }
            }

            foreach (XElement container in removeMe)
            {
                container.Remove();
            }
        }

        private bool SaveFileCheck()
        {
            try
            {
                if (!File.Exists(_gridSavePath))
                {
                    using (File.Create(_gridSavePath))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GridSaveSystem could not create file: {_gridSavePath}, {ex.Message}");
                return false;
            }

            return true;
        }

        public static void Clear()
        {
            _instance = null;
        }
    }

    internal sealed class GridItemSlotSaveData
    {
        public int Slot { get; }
        public uint Serial { get; }
        public bool IsLocked { get; }

        public GridItemSlotSaveData(int slot, uint serial, bool isLocked)
        {
            Slot = slot;
            Serial = serial;
            IsLocked = isLocked;
        }
    }
}

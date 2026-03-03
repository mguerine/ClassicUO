// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class GridSlotManager
    {
        private const int XSpacing = 1;
        private const int YSpacing = 1;
        private const int DefaultSlotCount = 125;
        private const int BaseGridItemSize = 44;

        private readonly World _world;
        private readonly uint _containerSerial;
        private readonly GridContainerGump _gridGump;
        private readonly Control _area;
        private readonly Dictionary<int, GridSlotControl> _gridSlots = new Dictionary<int, GridSlotControl>();
        private readonly Dictionary<int, uint> _itemPositions = new Dictionary<int, uint>();
        private readonly List<uint> _itemLocks = new List<uint>();
        private readonly int _slotSize;
        private List<Item> _containerContents = new List<Item>();
        private Item _container;

        public GridSlotManager(World world, uint containerSerial, GridContainerGump gridGump, Control scrollArea)
        {
            _world = world;
            _containerSerial = containerSerial;
            _gridGump = gridGump;
            _area = scrollArea;

            _container = world.Items.Get(containerSerial);
            if (_container == null)
            {
                return;
            }

            foreach (GridItemSlotSaveData item in GridSaveSystem.Instance.GetItemSlots(containerSerial))
            {
                _itemPositions[item.Slot] = item.Serial;
                if (item.IsLocked)
                {
                    _itemLocks.Add(item.Serial);
                }
            }

            UpdateItems();
            int amount = DefaultSlotCount;
            if (_containerContents.Count > amount)
            {
                amount = _containerContents.Count;
            }

            int slotSize = BaseGridItemSize;
            Profile profile = ProfileManager.CurrentProfile;
            if (profile != null)
            {
                int scale = profile.GridContainersScale;
                if (scale < 50) scale = 50;
                if (scale > 200) scale = 200;
                slotSize = (int)(BaseGridItemSize * scale / 100f);
                if (slotSize < 24) slotSize = 24;
                if (slotSize > 88) slotSize = 88;
            }
            _slotSize = slotSize;

            for (int i = 0; i < amount; i++)
            {
                var slot = new GridSlotControl(_gridGump, _container, i, _slotSize);
                _gridSlots[i] = slot;
                _area.Add(slot);
            }
        }

        public IReadOnlyDictionary<int, GridSlotControl> GridSlots => _gridSlots;
        public List<Item> ContainerContents => _containerContents;
        public IReadOnlyDictionary<int, uint> ItemPositions => _itemPositions;

        public void AddLockedItemSlot(uint serial, int specificSlot)
        {
            int? removeKey = null;
            foreach (var kv in _itemPositions)
            {
                if (kv.Value == serial)
                {
                    removeKey = kv.Key;
                    break;
                }
            }

            if (removeKey.HasValue)
            {
                _itemPositions.Remove(removeKey.Value);
            }

            if (_itemPositions.ContainsKey(specificSlot))
            {
                _itemPositions.Remove(specificSlot);
            }

            _itemPositions[specificSlot] = serial;
        }

        public GridSlotControl FindItem(uint serial)
        {
            foreach (var slot in _gridSlots)
            {
                if (slot.Value.SlotItem != null && slot.Value.SlotItem.Serial == serial)
                {
                    return slot.Value;
                }
            }

            return null;
        }

        public void RebuildContainer(List<Item> filteredItems, string searchText = "", bool overrideSort = false)
        {
            foreach (var slot in _gridSlots)
            {
                slot.Value.SetGridItem(null);
            }

            foreach (var spot in _itemPositions)
            {
                Item i = _world.Items.Get(spot.Value);
                if (i == null)
                {
                    continue;
                }

                if (!filteredItems.Contains(i) || (overrideSort && !_itemLocks.Contains(spot.Value)))
                {
                    continue;
                }

                if (spot.Key >= _gridSlots.Count)
                {
                    continue;
                }

                _gridSlots[spot.Key].SetGridItem(i);
                if (_itemLocks.Contains(spot.Value))
                {
                    _gridSlots[spot.Key].ItemGridLocked = true;
                }

                filteredItems.Remove(i);
            }

            foreach (Item i in filteredItems.ToList())
            {
                foreach (var slot in _gridSlots)
                {
                    if (slot.Value.SlotItem != null)
                    {
                        continue;
                    }

                    slot.Value.SetGridItem(i);
                    AddLockedItemSlot(i.Serial, slot.Key);
                    filteredItems.Remove(i);
                    break;
                }
            }

            SetGridPositions();
        }

        public void SetLockedSlot(int slot, bool locked)
        {
            if (!_gridSlots.TryGetValue(slot, out GridSlotControl ctrl) || ctrl.SlotItem == null)
            {
                return;
            }

            ctrl.ItemGridLocked = locked;
            if (!locked)
            {
                _itemLocks.Remove(ctrl.SlotItem.Serial);
            }
            else
            {
                _itemLocks.Add(ctrl.SlotItem.Serial);
            }
        }

        public void SetGridPositions()
        {
            int x = XSpacing;
            int y = 0;

            foreach (var slot in _gridSlots.OrderBy(s => s.Key))
            {
                if (!slot.Value.IsVisible)
                {
                    continue;
                }

                if (x + _slotSize >= _area.Width - 14)
                {
                    x = XSpacing;
                    y += _slotSize + YSpacing;
                }

                slot.Value.X = x;
                slot.Value.Y = y;
                slot.Value.Resize();
                x += _slotSize + XSpacing;
            }
        }

        public List<Item> SearchResults(string search)
        {
            UpdateItems();
            if (string.IsNullOrWhiteSpace(search))
            {
                return _containerContents;
            }

            string term = search.Trim().ToLowerInvariant();
            var filtered = new List<Item>();
            foreach (Item i in _containerContents)
            {
                if (SearchItemName(term, i))
                {
                    filtered.Add(i);
                }
            }

            return filtered;
        }

        private static bool SearchItemName(string searchTermLower, Item item)
        {
            if (item?.Name != null && item.Name.ToLowerInvariant().Contains(searchTermLower))
            {
                return true;
            }

            if (item?.ItemData.Name != null && item.ItemData.Name.ToLowerInvariant().Contains(searchTermLower))
            {
                return true;
            }

            return false;
        }

        public void UpdateItems()
        {
            _container = _world.Items.Get(_containerSerial);
            _containerContents = GetItemsInContainer(_container);
        }

        public static List<Item> GetItemsInContainer(Item container)
        {
            var contents = new List<Item>();
            if (container == null)
            {
                return contents;
            }

            for (LinkedObject i = container.Items; i != null; i = i.Next)
            {
                Item item = (Item)i;
                var layer = (Layer)item.ItemData.Layer;

                if (container.IsCorpse && item.Layer > 0 && !Constants.BAD_CONTAINER_LAYERS[(int)layer])
                {
                    continue;
                }

                if (item.ItemData.IsWearable && (layer == Layer.Face || layer == Layer.Beard || layer == Layer.Hair))
                {
                    continue;
                }

                contents.Add(item);
            }

            return contents.OrderBy(x => x.Graphic).ThenBy(x => x.Hue).ToList();
        }
    }

    internal sealed class GridSlotControl : Control
    {
        private readonly GridContainerGump _gump;
        private readonly Item _container;
        private readonly HitBox _hit;
        private readonly int _slotIndex;
        private readonly int _size;
        private Item _slotItem;

        public GridSlotControl(GridContainerGump gump, Item container, int slotIndex, int size)
        {
            _gump = gump;
            _container = container;
            _slotIndex = slotIndex;
            _size = size;

            CanMove = false;
            Width = size;
            Height = size;

            var bg = new AlphaBlendControl(0.25f) { Width = size, Height = size };
            Add(bg);

            _hit = new HitBox(0, 0, size, size, null, 0f);
            Add(_hit);

            _hit.MouseUp += Hit_MouseUp;
        }

        public Item SlotItem
        {
            get => _slotItem;
        }

        public bool ItemGridLocked { get; set; }

        public void SetGridItem(Item item)
        {
            _slotItem = item;
            if (item != null)
            {
                LocalSerial = item.Serial;
                if (_gump.World.ClientFeatures.TooltipsEnabled)
                {
                    _hit.SetTooltip(item);
                }
            }
            else
            {
                LocalSerial = 0;
                _hit.ClearTooltip();
            }
        }

        public void Resize()
        {
            Width = Height = _size;
            _hit.Width = _hit.Height = _size;
            if (Children.Count > 0 && Children[0] is AlphaBlendControl bg)
            {
                bg.Width = bg.Height = _size;
            }
        }

        private void Hit_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left)
            {
                return;
            }

            if (Client.Game.UO.GameCursor.ItemHold.Enabled)
            {
                if (_slotItem != null && _slotItem.ItemData.IsContainer)
                {
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _slotItem.Serial);
                }
                else if (_slotItem != null && _slotItem.ItemData.IsStackable && _slotItem.Graphic == Client.Game.UO.GameCursor.ItemHold.Graphic)
                {
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, _slotItem.X, _slotItem.Y, 0, _slotItem.Serial);
                }
                else
                {
                    _gump.GridSlotManager.AddLockedItemSlot(Client.Game.UO.GameCursor.ItemHold.Serial, _slotIndex);
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _gump.LocalSerial);
                }

                Mouse.CancelDoubleClick = true;
            }
            else if (Keyboard.Ctrl)
            {
                _gump.GridSlotManager.SetLockedSlot(_slotIndex, !ItemGridLocked);
                Mouse.CancelDoubleClick = true;
            }
            else if (_slotItem != null)
            {
                GameActions.GrabItem(_gump.World, _slotItem, _slotItem.Amount);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);
            Profile profile = ProfileManager.CurrentProfile;

            if (_slotItem == null)
            {
                if (_hit.MouseIsOver)
                {
                    Vector3 hv = ShaderHueTranslator.GetHueVector(0);
                    hv.Z = 0.3f;
                    batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(x, y, Width, Height), hv);
                }
            }
            else
            {
            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_slotItem.DisplayedGraphic);
            var rect = Client.Game.UO.Arts.GetRealArtBounds(_slotItem.DisplayedGraphic);
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(_slotItem.Hue, _slotItem.ItemData.IsPartialHue, 1f);

            float itemScale = 1f;
            if (profile != null && profile.GridContainerScaleItems && profile.GridContainersScale > 0)
            {
                itemScale = profile.GridContainersScale / 100f;
            }

            int w = (int)(rect.Width * itemScale);
            int h = (int)(rect.Height * itemScale);
            int px = 0;
            int py = 0;
            if (w > _hit.Width || h > _hit.Height)
            {
                float s = System.Math.Min((float)_hit.Width / w, (float)_hit.Height / h);
                w = (int)(w * s);
                h = (int)(h * s);
            }

            px = (_hit.Width - w) / 2;
            py = (_hit.Height - h) / 2;

            if (artInfo.Texture != null)
            {
                batcher.Draw(
                    artInfo.Texture,
                    new Rectangle(x + px, y + py, w, h),
                    new Rectangle(artInfo.UV.X + rect.X, artInfo.UV.Y + rect.Y, rect.Width, rect.Height),
                    hueVector
                );
            }

            if (ItemGridLocked)
            {
                Vector3 borderHue = ShaderHueTranslator.GetHueVector(0x02, false, 0.8f);
                batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.White), x, y, Width, Height, borderHue);
            }

            if (_hit.MouseIsOver)
            {
                Vector3 hv = ShaderHueTranslator.GetHueVector(0x34, false, 0.5f);
                batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(x, y, Width, Height), hv);
            }
            }

            if (profile != null && profile.GridSlotLineStyle != 0)
            {
                float lineAlpha = System.Math.Max(profile.GridBorderAlpha / 100f, 0.4f);
                Vector3 lineHue = ShaderHueTranslator.GetHueVector(profile.GridBorderHue, false, lineAlpha);
                var tex = SolidColorTextureCache.GetTexture(Color.White);
                if (profile.GridSlotLineStyle == 1)
                {
                    batcher.Draw(tex, new Rectangle(x + Width - 1, y, 1, Height), lineHue);
                    batcher.Draw(tex, new Rectangle(x, y + Height - 1, Width, 1), lineHue);
                }
                else if (profile.GridSlotLineStyle == 2)
                {
                    batcher.DrawRectangle(tex, x, y, Width, Height, lineHue);
                }
            }

            return true;
        }
    }
}

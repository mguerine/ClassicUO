// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridContainerGump : Gump
    {
        private const int TopHeight = 24;
        private const int DefaultWidth = 320;
        private const int DefaultHeight = 300;
        private const int MinWidth = 200;
        private const int MinHeight = 180;
        private const int MaxWidth = 900;
        private const int MaxHeight = 700;

        private static int _lastX = 100;
        private static int _lastY = 100;

        private readonly AlphaBlendControl _background;
        private readonly Label _titleLabel;
        private readonly NiceButton _classicViewButton;
        private readonly GridScrollArea _scrollArea;
        private readonly GridSlotManager _gridSlotManager;
        private readonly GridBorderStyleControl _borderStyleControl;
        private readonly GridStyleBackgroundControl _styleBackgroundControl;
        private readonly Button _resizeButton;
        private bool _resizeClicked;
        private Point _resizeSavedSize;

        public GridContainerGump(World world, uint serial, ushort containerGraphic)
            : base(world, serial, 0)
        {
            Item cont = world.Items.Get(serial);
            if (cont == null)
            {
                Dispose();
                return;
            }

            ContainerGraphic = containerGraphic;

            Profile profile = ProfileManager.CurrentProfile;
            int scalePct = profile != null ? profile.GridContainersScale : 100;
            scalePct = scalePct < 50 ? 50 : (scalePct > 200 ? 200 : scalePct);
            float scaleMult = scalePct / 100f;

            Point savedPos = GridSaveSystem.Instance.GetLastPosition(serial, _lastX, _lastY);
            Point savedSize = GridSaveSystem.Instance.GetLastSize(serial, DefaultWidth, DefaultHeight);
            int w = (int)(savedSize.X * scaleMult);
            int h = (int)(savedSize.Y * scaleMult);
            if (w < MinWidth) w = MinWidth;
            if (h < MinHeight) h = MinHeight;
            if (w > MaxWidth) w = MaxWidth;
            if (h > MaxHeight) h = MaxHeight;
            X = savedPos.X;
            Y = savedPos.Y;
            _lastX = X;
            _lastY = Y;
            Width = w;
            Height = h;

            if (GridSaveSystem.Instance.UseOriginalContainerGump(serial))
            {
                UIManager.GetGump<GridContainerGump>(serial)?.Dispose();
                UIManager.Add(new ContainerGump(world, serial, containerGraphic, false, true)
                {
                    X = X,
                    Y = Y,
                    InvalidateContents = true
                });
                Dispose();
                return;
            }

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = true;

            float bgAlpha = profile != null ? (ProfileManager.CurrentProfile.GridContainerOpacity / 100f) : 0.95f;
            ushort bgHue = 0;
            if (profile != null && profile.Grid_UseContainerHue && cont != null)
                bgHue = cont.Hue;
            else if (profile != null)
                bgHue = profile.AltGridContainerBackgroundHue;
            _background = new AlphaBlendControl(bgAlpha)
            {
                X = 0,
                Y = TopHeight,
                Width = Width,
                Height = Height - TopHeight,
                Hue = bgHue
            };
            Add(_background);

            _styleBackgroundControl = new GridStyleBackgroundControl(0, 0, Width, Height);
            Add(_styleBackgroundControl);

            _titleLabel = new Label(GetContainerName(cont), true, 0x0481)
            {
                X = 4,
                Y = 2,
                Width = 200
            };

            _classicViewButton = new NiceButton(0, 0, 80, 22, ButtonAction.Activate, "Classic")
            {
                ButtonParameter = 0,
                IsSelectable = false
            };
            _classicViewButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    OpenClassicAndDispose();
                }
            };

            _scrollArea = new GridScrollArea(0, TopHeight, Width, Height - TopHeight)
            {
                ScissorRectangle = new Rectangle(0, 0, Width - 14, Height - TopHeight)
            };
            Add(_scrollArea);

            _borderStyleControl = new GridBorderStyleControl(0, 0, Width, Height);
            Add(_borderStyleControl);

            _resizeSavedSize = new Point(Width, Height);
            _resizeButton = new Button(0, 0x837, 0x838, 0x838);
            _resizeButton.MouseDown += (s, e) => { _resizeClicked = true; };
            _resizeButton.MouseUp += (s, e) =>
            {
                _resizeSavedSize = new Point(Width, Height);
                _resizeClicked = false;
                if (_gridSlotManager != null && _gridSlotManager.GridSlots != null && _gridSlotManager.GridSlots.Count > 0)
                {
                    GridSaveSystem.Instance.SaveContainer(LocalSerial, _gridSlotManager.GridSlots, Width, Height, X, Y, null, false);
                }
            };
            Add(_resizeButton);

            Add(_titleLabel);
            Add(_classicViewButton);

            _gridSlotManager = new GridSlotManager(world, serial, this, _scrollArea);
            UpdateGridContents();
        }

        public ushort ContainerGraphic { get; }
        public GridSlotManager GridSlotManager => _gridSlotManager;

        public override GumpType GumpType => GumpType.GridContainer;

        private static int GetBorderWidth()
        {
            Profile p = ProfileManager.CurrentProfile;
            if (p == null || p.Grid_BorderStyle < 1 || p.Grid_BorderStyle > 8)
                return 0;
            switch (p.Grid_BorderStyle)
            {
                case 1: return 26;
                case 2: return 12;
                case 3: return 10;
                case 4: return 7;
                case 5: return 10;
                case 6: return 4;
                case 7: return 17;
                case 8: return 16;
                default: return 0;
            }
        }

        private void OpenClassicAndDispose()
        {
            if (_gridSlotManager != null && _gridSlotManager.GridSlots != null && _gridSlotManager.GridSlots.Count > 0)
            {
                GridSaveSystem.Instance.SaveContainer(
                    LocalSerial,
                    _gridSlotManager.GridSlots,
                    Width,
                    Height,
                    X,
                    Y,
                    true,
                    false
                );
            }

            UIManager.GetGump<GridContainerGump>(LocalSerial)?.Dispose();
            UIManager.Add(new ContainerGump(World, LocalSerial, ContainerGraphic, false, true)
            {
                X = X,
                Y = Y,
                InvalidateContents = true
            });
            Dispose();
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                OpenClassicAndDispose();
            }
        }

        protected override void UpdateContents()
        {
            _gridSlotManager?.UpdateItems();
            UpdateGridContents();
        }

        private void UpdateGridContents()
        {
            Item cont = World.Items.Get(LocalSerial);
            if (cont == null)
            {
                return;
            }

            List<Item> contents = GridSlotManager.GetItemsInContainer(cont);
            _gridSlotManager.RebuildContainer(contents);
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Item cont = World.Items.Get(LocalSerial);
            if (cont == null)
            {
                return;
            }

            int count = _gridSlotManager?.ContainerContents?.Count ?? 0;
            _titleLabel.Text = GetContainerName(cont) + $" ({count})";
        }

        private static string GetContainerName(Item item)
        {
            return item?.Name?.Length > 0 == true ? item.Name : "Container";
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && Client.Game.UO.GameCursor.ItemHold.Enabled
                && x >= 0 && y >= TopHeight && x < Width && y < Height)
            {
                GameActions.DropItem(
                    Client.Game.UO.GameCursor.ItemHold.Serial,
                    0xFFFF,
                    0xFFFF,
                    0,
                    LocalSerial
                );
            }

            base.OnMouseUp(x, y, button);
        }

        public override void Update()
        {
            base.Update();

            Item item = World.Items.Get(LocalSerial);
            if (item == null || item.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (UIManager.MouseOverControl != null
                && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this)
                && ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.HighlightContainerWhenSelected)
            {
                SelectedObject.SelectedContainer = item;
            }

            if (_resizeClicked && !Mouse.LButtonPressed)
            {
                _resizeSavedSize = new Point(Width, Height);
                _resizeClicked = false;
                if (_gridSlotManager != null && _gridSlotManager.GridSlots != null && _gridSlotManager.GridSlots.Count > 0)
                {
                    GridSaveSystem.Instance.SaveContainer(LocalSerial, _gridSlotManager.GridSlots, Width, Height, X, Y, null, false);
                }
            }

            Point resizeSize = _resizeSavedSize;
            if (_resizeClicked && Mouse.LDragOffset != Point.Zero)
            {
                int w = _resizeSavedSize.X + Mouse.LDragOffset.X;
                int h = _resizeSavedSize.Y + Mouse.LDragOffset.Y;
                if (w < MinWidth) w = MinWidth;
                if (w > MaxWidth) w = MaxWidth;
                if (h < MinHeight) h = MinHeight;
                if (h > MaxHeight) h = MaxHeight;
                resizeSize = new Point(w, h);
            }
            bool sizeChanged = Width != resizeSize.X || Height != resizeSize.Y;
            if (sizeChanged)
            {
                Width = resizeSize.X;
                Height = resizeSize.Y;
                if (!_resizeClicked)
                    _resizeSavedSize = resizeSize;
            }

            int borderWidth = GetBorderWidth();
            int innerW = Width - 2 * borderWidth;
            int innerH = Height - TopHeight - 2 * borderWidth;
            if (innerW < 0) innerW = Width;
            if (innerH < 0) innerH = Height - TopHeight;

            _background.IsVisible = borderWidth == 0;
            _background.X = borderWidth;
            _background.Y = TopHeight + borderWidth;
            _background.Width = innerW;
            _background.Height = innerH;

            _scrollArea.X = borderWidth;
            _scrollArea.Y = TopHeight + borderWidth;
            _scrollArea.Width = innerW;
            _scrollArea.Height = innerH;
            _scrollArea.ScissorRectangle = new Rectangle(0, 0, Math.Max(0, innerW - 14), Math.Max(0, innerH));

            if (sizeChanged)
            {
                _gridSlotManager?.SetGridPositions();
            }

            _titleLabel.X = borderWidth + 4;
            _titleLabel.Y = 2;
            _classicViewButton.X = Width - borderWidth - _classicViewButton.Width - 4;
            _classicViewButton.Y = 2;

            _borderStyleControl.Width = Width;
            _borderStyleControl.Height = Height;
            _styleBackgroundControl.Width = Width;
            _styleBackgroundControl.Height = Height;
            _resizeButton.X = Width - _resizeButton.Width + 2;
            _resizeButton.Y = Height - _resizeButton.Height + 2;

            Profile p = ProfileManager.CurrentProfile;
            bool useStyleBorder = p != null && !p.Grid_HideBorder && p.Grid_BorderStyle >= 1 && p.Grid_BorderStyle <= 8;
            _borderStyleControl.IsVisible = useStyleBorder;
            _styleBackgroundControl.IsVisible = useStyleBorder;
            if (p != null)
            {
                _background.Alpha = p.GridContainerOpacity / 100f;
                _background.Hue = p.Grid_UseContainerHue && item != null ? item.Hue : p.AltGridContainerBackgroundHue;
            }
            UpdateTitle();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            bool result = base.Draw(batcher, x, y);
            Profile p = ProfileManager.CurrentProfile;
            if (p != null && !p.Grid_HideBorder && p.GridBorderAlpha > 0 && p.Grid_BorderStyle <= 0)
            {
                float borderAlpha = p.GridBorderAlpha / 100f;
                Vector3 borderHue = ShaderHueTranslator.GetHueVector(p.GridBorderHue, false, borderAlpha);
                batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.White), x, y, Width, Height, borderHue);
            }
            return result;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("graphic", ContainerGraphic.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
            Dispose();
        }

        public override void Dispose()
        {
            _lastX = X;
            _lastY = Y;

            if (_gridSlotManager != null && _gridSlotManager.GridSlots != null && _gridSlotManager.GridSlots.Count > 0)
            {
                GridSaveSystem.Instance.SaveContainer(
                    LocalSerial,
                    _gridSlotManager.GridSlots,
                    Width,
                    Height,
                    X,
                    Y,
                    null,
                    false
                );
            }

            Item cont = World.Items.Get(LocalSerial);
            if (cont != null && cont == SelectedObject.CorpseObject)
            {
                SelectedObject.CorpseObject = null;
            }

            base.Dispose();
        }

        private sealed class GridStyleBackgroundControl : Control
        {
            public GridStyleBackgroundControl(int x, int y, int w, int h)
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;
                CanMove = false;
                AcceptMouseInput = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Profile p = ProfileManager.CurrentProfile;
                if (p == null || p.Grid_BorderStyle < 1 || p.Grid_BorderStyle > 8)
                    return true;

                int graphic = 0, borderSize = 0;
                switch (p.Grid_BorderStyle)
                {
                    case 1: graphic = 3500; borderSize = 26; break;
                    case 2: graphic = 5054; borderSize = 12; break;
                    case 3: graphic = 5120; borderSize = 10; break;
                    case 4: graphic = 9200; borderSize = 7; break;
                    case 5: graphic = 9270; borderSize = 10; break;
                    case 6: graphic = 9300; borderSize = 4; break;
                    case 7: graphic = 9260; borderSize = 17; break;
                    case 8:
                        graphic = Client.Game.UO.Gumps.GetGump(40303).Texture != null ? 40303 : 83;
                        borderSize = 16;
                        break;
                }

                int innerX = x + borderSize;
                int innerY = y + borderSize;
                int innerW = Width - 2 * borderSize;
                int innerH = Height - 2 * borderSize;
                if (innerW <= 0 || innerH <= 0) return true;

                ref readonly var g = ref Client.Game.UO.Gumps.GetGump((ushort)(graphic + 4));
                if (g.Texture == null) return true;

                float alpha = p.GridBorderAlpha / 100f;
                if (alpha <= 0) alpha = 1f;
                Vector3 hueVec = ShaderHueTranslator.GetHueVector(p.GridBorderHue, false, alpha);
                batcher.DrawTiled(g.Texture, new Rectangle(innerX, innerY, innerW, innerH), g.UV, hueVec);
                return true;
            }
        }

        private sealed class GridBorderStyleControl : Control
        {
            private static void GetStyleGraphics(int style, out int graphic, out int borderSize)
            {
                graphic = 0;
                borderSize = 4;
                switch (style)
                {
                    case 1: graphic = 3500; borderSize = 26; break;
                    case 2: graphic = 5054; borderSize = 12; break;
                    case 3: graphic = 5120; borderSize = 10; break;
                    case 4: graphic = 9200; borderSize = 7; break;
                    case 5: graphic = 9270; borderSize = 10; break;
                    case 6: graphic = 9300; borderSize = 4; break;
                    case 7: graphic = 9260; borderSize = 17; break;
                    case 8:
                        if (Client.Game.UO.Gumps.GetGump(40303).Texture != null)
                            graphic = 40303;
                        else
                            graphic = 83;
                        borderSize = 16;
                        break;
                }
            }

            public GridBorderStyleControl(int x, int y, int w, int h)
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;
                CanMove = false;
                AcceptMouseInput = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Profile p = ProfileManager.CurrentProfile;
                if (p == null || p.Grid_BorderStyle < 1 || p.Grid_BorderStyle > 8)
                    return true;

                GetStyleGraphics(p.Grid_BorderStyle, out int graphic, out int borderSize);
                float alpha = (p.GridBorderAlpha / 100f);
                if (alpha <= 0) return true;
                Vector3 hueVec = ShaderHueTranslator.GetHueVector(p.GridBorderHue, false, alpha);

                int w = Width;
                int h = Height;
                if (borderSize <= 0 || w < 2 * borderSize || h < 2 * borderSize)
                    return true;

                void DrawGump(ushort gid, int dx, int dy, int dW, int dH)
                {
                    ref readonly var g = ref Client.Game.UO.Gumps.GetGump(gid);
                    if (g.Texture == null) return;
                    batcher.Draw(g.Texture, new Rectangle(x + dx, y + dy, dW, dH), g.UV, hueVec);
                }

                void DrawTiledGump(ushort gid, int dx, int dy, int dW, int dH)
                {
                    ref readonly var g = ref Client.Game.UO.Gumps.GetGump(gid);
                    if (g.Texture == null) return;
                    batcher.DrawTiled(g.Texture, new Rectangle(x + dx, y + dy, dW, dH), g.UV, hueVec);
                }

                DrawGump((ushort)graphic, 0, 0, borderSize, borderSize);
                DrawGump((ushort)(graphic + 2), w - borderSize, 0, borderSize, borderSize);
                DrawGump((ushort)(graphic + 6), 0, h - borderSize, borderSize, borderSize);
                DrawGump((ushort)(graphic + 8), w - borderSize, h - borderSize, borderSize, borderSize);

                DrawTiledGump((ushort)(graphic + 1), borderSize, 0, w - 2 * borderSize, borderSize);
                DrawTiledGump((ushort)(graphic + 7), borderSize, h - borderSize, w - 2 * borderSize, borderSize);
                DrawTiledGump((ushort)(graphic + 3), 0, borderSize, borderSize, h - 2 * borderSize);
                DrawTiledGump((ushort)(graphic + 5), w - borderSize, borderSize, borderSize, h - 2 * borderSize);

                return true;
            }
        }
    }
}

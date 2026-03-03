// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class GridScrollArea : Control
    {
        private readonly ScrollBarBase _scrollBar;
        private int _lastWidth;
        private int _lastHeight;

        public GridScrollArea(int x, int y, int w, int h, int scrollMaxHeight = -1)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _lastWidth = w;
            _lastHeight = h;

            _scrollBar = new ScrollBar(Width - 14, 0, Height);
            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = scrollMaxHeight >= 0 ? scrollMaxHeight : Height;
            Add(_scrollBar);

            ScrollMaxHeight = scrollMaxHeight;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            CanMove = true;
            ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
        }

        public int ScrollMaxHeight { get; set; } = -1;
        public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
        public int ScrollValue => _scrollBar.Value;
        public int ScrollMinValue => _scrollBar.MinValue;
        public int ScrollMaxValue => _scrollBar.MaxValue;
        public Rectangle ScissorRectangle { get; set; }

        public override void Update()
        {
            base.Update();
            CalculateScrollBarMaxValue();

            if (Width != _lastWidth || Height != _lastHeight)
            {
                _scrollBar.X = Width - 14;
                _scrollBar.Height = Height;
                _lastWidth = Width;
                _lastHeight = Height;
            }

            if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
            {
                _scrollBar.IsVisible = true;
            }
            else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
            {
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }
        }

        public void Scroll(bool isUp)
        {
            if (isUp)
            {
                _scrollBar.Value -= _scrollBar.ScrollStep;
            }
            else
            {
                _scrollBar.Value += _scrollBar.ScrollStep;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

            int clipW = Width - 14;
            int clipH = Height;
            if (clipW <= 0 || clipH <= 0)
            {
                return true;
            }

            if (batcher.ClipBegin(x, y, clipW, clipH))
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Control child = Children[i];
                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    int finalY = y + child.Y - _scrollBar.Value;
                    child.Draw(batcher, x + child.X, finalY);
                }

                batcher.ClipEnd();
            }

            return true;
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                    break;
                case MouseEventType.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;
                    break;
            }
        }

        public override void Clear()
        {
            for (int i = Children.Count - 1; i >= 1; i--)
            {
                Children[i].Dispose();
            }
        }

        private void CalculateScrollBarMaxValue()
        {
            _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
            bool wasMaxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

            int contentBottom = 0;

            for (int i = 1; i < Children.Count; i++)
            {
                Control c = Children[i];
                if (c.IsVisible && !c.IsDisposed && c.Bounds.Bottom > contentBottom)
                {
                    contentBottom = c.Bounds.Bottom;
                }
            }

            int maxScroll = Math.Max(0, contentBottom - Height);

            if (maxScroll > 0)
            {
                _scrollBar.MaxValue = maxScroll;
                if (_scrollBar.Value > _scrollBar.MaxValue)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
                if (wasMaxValue)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
            }
            else
            {
                _scrollBar.Value = 0;
                _scrollBar.MaxValue = 0;
            }

            _scrollBar.UpdateOffset(0, Offset.Y);

            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].UpdateOffset(0, -_scrollBar.Value);
            }
        }
    }
}

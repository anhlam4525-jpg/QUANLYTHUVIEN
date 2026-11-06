using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LibraryManagement.UI.Controls
{
    public enum ButtonVariant
    {
        Primary,
        Secondary,
        Danger,
        DangerSoft,
        Success,
        Ghost,
        White
    }

    /// <summary>Nút bo góc, có icon, đổi màu khi hover/nhấn – vẽ hoàn toàn bằng GDI+.</summary>
    [DesignerCategory("Code")]
    public class ModernButton : Button
    {
        private bool _hover;
        private bool _pressed;
        private ButtonVariant _variant = ButtonVariant.Primary;
        private string? _glyph;

        public ButtonVariant Variant
        {
            get => _variant;
            set { _variant = value; Invalidate(); }
        }

        /// <summary>Ký tự icon trong font Segoe MDL2 Assets (xem Theme.Icons).</summary>
        public string? Glyph
        {
            get => _glyph;
            set { _glyph = value; Invalidate(); }
        }

        public float Radius { get; set; } = Theme.Sf(8);

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            UseVisualStyleBackColor = false;
            Font = Theme.BodyBold;
            Cursor = Cursors.Hand;
            Height = Theme.S(38);
            Margin = new Padding(Theme.S(8), 0, 0, 0);
        }

        public ModernButton(string text, ButtonVariant variant, string? glyph = null) : this()
        {
            Text = text;
            Variant = variant;
            Glyph = glyph;
            AutoFit();
        }

        /// <summary>Tự tính chiều rộng theo nội dung.</summary>
        public void AutoFit(int minWidth = 0)
        {
            int textW = string.IsNullOrEmpty(Text) ? 0 : TextRenderer.MeasureText(Text, Font).Width;
            int iconW = string.IsNullOrEmpty(Glyph) ? 0 : Theme.S(16) + (textW > 0 ? Theme.S(8) : 0);
            Width = Math.Max(minWidth, textW + iconW + Theme.S(32));
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        private (Color Fill, Color Text, Color Border) GetColors()
        {
            Color fill, text, border;
            switch (_variant)
            {
                case ButtonVariant.Primary:
                    fill = _pressed ? Theme.PrimaryPressed : _hover ? Theme.PrimaryHover : Theme.Primary;
                    text = Color.White; border = fill; break;
                case ButtonVariant.Danger:
                    fill = _pressed ? Theme.DangerText : _hover ? Color.FromArgb(220, 38, 38) : Theme.Danger;
                    text = Color.White; border = fill; break;
                case ButtonVariant.Success:
                    fill = _pressed ? Theme.SuccessText : _hover ? Color.FromArgb(5, 150, 105) : Theme.Success;
                    text = Color.White; border = fill; break;
                case ButtonVariant.DangerSoft:
                    fill = _pressed ? Color.FromArgb(254, 202, 202) : _hover ? Theme.DangerSoft : Color.FromArgb(254, 242, 242);
                    text = Theme.DangerText; border = Color.FromArgb(254, 202, 202); break;
                case ButtonVariant.Ghost:
                    fill = _pressed ? Theme.Border : _hover ? Theme.Background : Color.Transparent;
                    text = Theme.TextMuted; border = Color.Transparent; break;
                case ButtonVariant.White:
                    fill = _pressed ? Theme.PrimarySoft : _hover ? Color.FromArgb(245, 247, 255) : Color.White;
                    text = Theme.Primary; border = Color.White; break;
                default: // Secondary
                    fill = _pressed ? Theme.Border : _hover ? Theme.SurfaceAlt : Color.White;
                    text = Theme.Text; border = Theme.BorderStrong; break;
            }

            if (!Enabled)
            {
                fill = fill.A == 0 ? fill : Theme.Blend(fill, Color.White, 0.55f);
                text = Theme.Blend(text, Color.White, 0.5f);
                border = border.A == 0 ? border : Theme.Blend(border, Color.White, 0.5f);
            }
            return (fill, text, border);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var (fill, textColor, border) = GetColors();

            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            if (fill.A > 0) Theme.FillRound(g, fill, rect, Radius);
            if (border.A > 0 && border != fill) Theme.DrawRound(g, border, rect, Radius);

            // Vòng focus khi dùng bàn phím (Tab)
            if (Focused && ShowFocusCues)
            {
                var focusRect = new RectangleF(2f, 2f, Width - 5f, Height - 5f);
                Theme.DrawRound(g, Theme.Blend(textColor, fill.A == 0 ? Color.White : fill, 0.5f), focusRect, Radius - 1);
            }

            int textW = string.IsNullOrEmpty(Text) ? 0 : TextRenderer.MeasureText(Text, Font).Width;
            int iconW = string.IsNullOrEmpty(_glyph) ? 0 : Theme.S(16);
            int gap = iconW > 0 && textW > 0 ? Theme.S(8) : 0;
            int total = iconW + gap + textW;
            int x = Math.Max(Theme.S(6), (Width - total) / 2);

            if (iconW > 0)
            {
                Theme.DrawIcon(g, _glyph!, 10.5f, textColor, new Rectangle(x, 0, iconW, Height));
                x += iconW + gap;
            }
            if (textW > 0)
            {
                TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x, Height), textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix |
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }
    }
}

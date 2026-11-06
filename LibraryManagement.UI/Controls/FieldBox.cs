using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LibraryManagement.UI.Controls
{
    /// <summary>
    /// Khung nhập liệu bo góc kiểu web: bọc TextBox / ComboBox / DateTimePicker / NumericUpDown,
    /// viền đổi sang màu chủ đạo khi focus, có icon bên trái. Với ComboBox/DateTimePicker, viền 3D gốc
    /// được che đi bằng một panel cắt (clip) bên trong.
    /// </summary>
    [DesignerCategory("Code")]
    public class FieldBox : Panel
    {
        private readonly Control _editor;
        private readonly Panel? _clip;
        private readonly int _inset;
        private bool _focused;
        private string? _glyph;

        public Control Editor => _editor;

        public string? Glyph
        {
            get => _glyph;
            set { _glyph = value; PerformLayout(); Invalidate(); }
        }

        public FieldBox(Control editor, string? glyph = null, bool clipEditorBorder = false, int inset = 2)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Height = Theme.S(40);
            Margin = new Padding(0, 0, Theme.S(12), 0);
            Cursor = Cursors.IBeam;

            _editor = editor;
            _glyph = glyph;
            _inset = inset;

            editor.Font = Theme.Body;
            editor.BackColor = Color.White;
            editor.ForeColor = Theme.Text;

            if (clipEditorBorder)
            {
                _clip = new Panel { BackColor = Color.White };
                _clip.Controls.Add(editor);
                Controls.Add(_clip);
            }
            else
            {
                Controls.Add(editor);
            }

            editor.Enter += (s, e) => { _focused = true; Invalidate(); };
            editor.Leave += (s, e) => { _focused = false; Invalidate(); };
            editor.EnabledChanged += (s, e) => Invalidate();
            Click += (s, e) => editor.Focus();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (_editor == null) return;

            int left = string.IsNullOrEmpty(_glyph) ? Theme.S(12) : Theme.S(38);
            int right = Theme.S(8);
            int width = Math.Max(Theme.S(20), Width - left - right);

            if (_clip != null)
            {
                int editorHeight = _editor.Height;
                int clipHeight = Math.Max(Theme.S(10), editorHeight - _inset * 2);
                _clip.SetBounds(left, (Height - clipHeight) / 2, width, clipHeight);
                _editor.SetBounds(-_inset, -_inset, width + _inset * 2, editorHeight);
            }
            else
            {
                _editor.SetBounds(left, (Height - _editor.Height) / 2, width, _editor.Height);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            Color parent = Theme.ParentColor(this);
            g.Clear(parent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool enabled = _editor.Enabled;
            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            float radius = Theme.Sf(8);

            if (_focused && enabled)
            {
                Theme.FillRound(g, Theme.PrimarySoft, rect, radius);
                var inner = new RectangleF(1.5f, 1.5f, Width - 3.5f, Height - 3.5f);
                Theme.FillRound(g, Color.White, inner, radius - 1);
                Theme.DrawRound(g, Theme.Primary, inner, radius - 1, 1.4f);
            }
            else
            {
                Theme.FillRound(g, enabled ? Color.White : Theme.SurfaceAlt, rect, radius);
                Theme.DrawRound(g, Theme.BorderStrong, rect, radius);
            }

            if (!string.IsNullOrEmpty(_glyph))
            {
                Theme.DrawIcon(g, _glyph, 10.5f, _focused ? Theme.Primary : Theme.TextSubtle,
                    new Rectangle(Theme.S(10), 0, Theme.S(22), Height));
            }
        }

        protected override void OnPaint(PaintEventArgs e) { /* đã vẽ trong OnPaintBackground */ }
    }

    /// <summary>Hàm tạo nhanh các ô nhập liệu đã được định dạng sẵn.</summary>
    public static class Fields
    {
        public static FieldBox Text(out TextBox textBox, string placeholder, string? glyph = null, bool password = false)
        {
            textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                PlaceholderText = placeholder,
                UseSystemPasswordChar = password
            };
            return new FieldBox(textBox, glyph);
        }

        public static FieldBox Combo(out ComboBox comboBox, bool editable = false, string? glyph = null)
        {
            var cb = new ComboBox
            {
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = Theme.S(26),
                IntegralHeight = false,
                MaxDropDownItems = 12
            };
            if (editable)
            {
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            cb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;

                bool editArea = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected && !editArea;

                using (var brush = new SolidBrush(selected ? Theme.PrimarySoft : Color.White))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                object? item = cb.Items[e.Index];
                string text = cb.GetItemText(item ?? string.Empty) ?? string.Empty;

                var r = new Rectangle(
                    e.Bounds.X + (editArea ? Theme.S(2) : Theme.S(10)),
                    e.Bounds.Y,
                    e.Bounds.Width - Theme.S(12),
                    e.Bounds.Height);

                Theme.DrawText(e.Graphics, text, cb.Font, r, selected ? Theme.PrimarySoftText : Theme.Text);
            };
            comboBox = cb;
            return new FieldBox(cb, glyph, clipEditorBorder: true, inset: 2);
        }

        public static FieldBox Date(out DateTimePicker picker, string? glyph = Theme.Icons.Calendar)
        {
            picker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy"
            };
            return new FieldBox(picker, glyph, clipEditorBorder: true, inset: 2);
        }

        public static FieldBox Number(out NumericUpDown number, int min, int max, string? glyph = null)
        {
            number = new NumericUpDown
            {
                BorderStyle = BorderStyle.None,
                Minimum = min,
                Maximum = max,
                TextAlign = HorizontalAlignment.Left,
                ThousandsSeparator = false
            };
            return new FieldBox(number, glyph);
        }

        /// <summary>Nhãn đặt phía trên ô nhập.</summary>
        public static Label Caption(string text, bool required = false)
        {
            return new Label
            {
                Text = required ? text + " *" : text,
                Font = Theme.SmallBold,
                ForeColor = Theme.Text,
                AutoSize = true,
                Margin = new Padding(0, Theme.S(12), 0, Theme.S(6)),
                BackColor = Color.Transparent
            };
        }

        /// <summary>Dòng chú thích nhỏ dưới ô nhập.</summary>
        public static Label Hint(string text = "")
        {
            return new Label
            {
                Text = text,
                Font = Theme.Small,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Margin = new Padding(Theme.S(2), Theme.S(6), 0, 0),
                BackColor = Color.Transparent
            };
        }
    }
}

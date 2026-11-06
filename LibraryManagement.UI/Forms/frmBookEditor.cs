using System.ComponentModel;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DTO;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Forms
{
    /// <summary>Hộp thoại Thêm / Cập nhật sách.</summary>
    [DesignerCategory("Code")]
    public class frmBookEditor : DialogForm
    {
        private readonly AppServices _services;
        private readonly BookDTO? _existing;
        private readonly TextBox _txtTitle;
        private readonly TextBox _txtAuthor;
        private readonly ComboBox _cboCategory;
        private readonly NumericUpDown _nudYear;
        private readonly NumericUpDown _nudQuantity;
        private readonly Label _lblError;

        public string ResultMessage { get; private set; } = string.Empty;
        public int ResultId { get; private set; }

        public frmBookEditor(AppServices services, BookDTO? existing, IEnumerable<string> categories)
        {
            _services = services;
            _existing = existing;
            bool isEdit = existing != null;

            SetHeader(isEdit ? "Cập nhật thông tin sách" : "Thêm sách mới",
                isEdit ? $"Mã sách: {existing!.BookId}" : "Nhập thông tin đầu sách cần bổ sung vào thư viện",
                isEdit ? Theme.Icons.Edit : Theme.Icons.Add, Theme.Primary, Theme.PrimarySoft);
            ClientSize = new Size(Theme.S(580), Theme.S(560));

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Color.White
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var fTitle = Fields.Text(out _txtTitle, "VD: Lập trình C# cơ bản", Theme.Icons.Book);
            var fAuthor = Fields.Text(out _txtAuthor, "VD: Nguyễn Văn Hưng", Theme.Icons.Contact);
            var fCategory = Fields.Combo(out _cboCategory, editable: true, glyph: Theme.Icons.Tag);
            var fYear = Fields.Number(out _nudYear, BookValidator.MinPublishYear, DateTime.Now.Year, Theme.Icons.Calendar);
            var fQty = Fields.Number(out _nudQuantity, 0, BookValidator.MaxQuantity, Theme.Icons.List);

            _txtTitle.MaxLength = 200;
            _txtAuthor.MaxLength = 100;
            _cboCategory.MaxLength = 50;
            foreach (string c in categories.Where(c => c != "Tất cả").Distinct())
            {
                _cboCategory.Items.Add(c);
            }

            AddRow(table, Fields.Caption("Tên sách", true), null);
            AddRow(table, fTitle, null);
            AddRow(table, Fields.Caption("Tác giả", true), null);
            AddRow(table, fAuthor, null);
            AddRow(table, Fields.Caption("Thể loại", true), Fields.Caption("Năm xuất bản", true));
            AddRow(table, fCategory, fYear);
            AddRow(table, Fields.Caption("Tổng số lượng (cuốn)", true), Fields.Caption("Đang cho mượn"));

            var lblBorrowed = new Label
            {
                Text = isEdit ? $"{existing!.BorrowedQuantity} cuốn  ·  còn {existing.AvailableQuantity} cuốn" : "0 cuốn (sách mới)",
                Font = Theme.BodyBold,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Height = Theme.S(40),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            AddRow(table, fQty, lblBorrowed);

            _lblError = new Label
            {
                Font = Theme.SmallBold,
                ForeColor = Theme.DangerText,
                AutoSize = true,
                MaximumSize = new Size(Theme.S(520), 0),
                Margin = new Padding(0, Theme.S(14), 0, 0),
                BackColor = Color.White
            };
            AddRow(table, _lblError, null);

            BodyPanel.Controls.Add(table);

            if (isEdit)
            {
                _txtTitle.Text = existing!.Title;
                _txtAuthor.Text = existing.Author;
                _cboCategory.Text = existing.Category;
                _nudYear.Value = Math.Clamp(existing.PublishYear, BookValidator.MinPublishYear, DateTime.Now.Year);
                _nudQuantity.Value = Math.Clamp(existing.TotalQuantity, 0, BookValidator.MaxQuantity);
                _nudQuantity.Minimum = existing.BorrowedQuantity; // không cho giảm dưới số đang mượn
            }
            else
            {
                _nudYear.Value = DateTime.Now.Year;
                _nudQuantity.Value = 1;
                if (_cboCategory.Items.Count > 0) _cboCategory.SelectedIndex = 0;
            }

            var btnSave = AddFooterButton(isEdit ? "Lưu thay đổi" : "Thêm sách", ButtonVariant.Primary, Theme.Icons.Check);
            var btnCancel = AddFooterButton("Hủy", ButtonVariant.Secondary);
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton = btnSave;

            foreach (var tb in new Control[] { _txtTitle, _txtAuthor, _cboCategory })
            {
                tb.TextChanged += (s, e) => _lblError.Text = string.Empty;
            }
            Shown += (s, e) => _txtTitle.Focus();
        }

        private static void AddRow(TableLayoutPanel table, Control left, Control? right)
        {
            int row = table.RowCount;
            table.RowCount = row + 1;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            if (left is FieldBox) left.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            table.Controls.Add(left, 0, row);

            if (right == null)
            {
                table.SetColumnSpan(left, 2);
                if (left is FieldBox) left.Margin = new Padding(0);
            }
            else
            {
                if (right is FieldBox) right.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                right.Margin = new Padding(Theme.S(6), right.Margin.Top, 0, right.Margin.Bottom);
                left.Margin = new Padding(0, left.Margin.Top, Theme.S(6), left.Margin.Bottom);
                table.Controls.Add(right, 1, row);
            }
        }

        private void Save()
        {
            var book = new BookDTO
            {
                BookId = _existing?.BookId ?? 0,
                Title = _txtTitle.Text.Trim(),
                Author = _txtAuthor.Text.Trim(),
                Category = _cboCategory.Text.Trim(),
                PublishYear = (int)_nudYear.Value,
                TotalQuantity = (int)_nudQuantity.Value,
                BorrowedQuantity = _existing?.BorrowedQuantity ?? 0
            };

            string? error = _services.Books.Validate(book);
            if (error != null)
            {
                _lblError.Text = "⚠ " + error;
                return;
            }

            var result = _existing == null ? _services.Books.AddBook(book) : _services.Books.UpdateBook(book);
            if (!result.Success)
            {
                _lblError.Text = "⚠ " + result.Message;
                return;
            }

            ResultMessage = result.Message;
            ResultId = result.Id;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

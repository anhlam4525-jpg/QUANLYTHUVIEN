using System.ComponentModel;
using LibraryManagement.DTO;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Forms
{
    /// <summary>Hộp thoại Thêm / Cập nhật độc giả.</summary>
    [DesignerCategory("Code")]
    public class frmReaderEditor : DialogForm
    {
        private readonly AppServices _services;
        private readonly ReaderDTO? _existing;
        private readonly TextBox _txtCode;
        private readonly TextBox _txtName;
        private readonly TextBox _txtClass;
        private readonly TextBox _txtEmail;
        private readonly TextBox _txtPhone;
        private readonly Label _lblError;

        public string ResultMessage { get; private set; } = string.Empty;
        public int ResultId { get; private set; }

        public frmReaderEditor(AppServices services, ReaderDTO? existing, string suggestedCode,
                               string? presetName = null, string? presetClass = null)
        {
            _services = services;
            _existing = existing;
            bool isEdit = existing != null;

            SetHeader(isEdit ? "Cập nhật độc giả" : "Thêm độc giả mới",
                isEdit ? $"Mã độc giả: {existing!.ReaderCode}" : "Đăng ký thẻ thư viện cho sinh viên",
                isEdit ? Theme.Icons.Edit : Theme.Icons.Contact, Theme.Primary, Theme.PrimarySoft);
            ClientSize = new Size(Theme.S(580), Theme.S(540));

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

            var fCode = Fields.Text(out _txtCode, "VD: DG011", Theme.Icons.Tag);
            var fClass = Fields.Text(out _txtClass, "VD: CNTT01", Theme.Icons.People);
            var fName = Fields.Text(out _txtName, "VD: Nguyễn Văn An", Theme.Icons.Contact);
            var fEmail = Fields.Text(out _txtEmail, "VD: an@sv.edu.vn", Theme.Icons.Mail);
            var fPhone = Fields.Text(out _txtPhone, "VD: 0901234567 (không bắt buộc)", Theme.Icons.Phone);

            _txtCode.MaxLength = 20;
            _txtCode.CharacterCasing = CharacterCasing.Upper;
            _txtClass.MaxLength = 20;
            _txtClass.CharacterCasing = CharacterCasing.Upper;
            _txtName.MaxLength = 100;
            _txtEmail.MaxLength = 100;
            _txtPhone.MaxLength = 11;
            _txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
            };

            AddRow(table, Fields.Caption("Mã độc giả", true), Fields.Caption("Lớp", true));
            AddRow(table, fCode, fClass);
            AddRow(table, Fields.Caption("Họ và tên", true), null);
            AddRow(table, fName, null);
            AddRow(table, Fields.Caption("Email", true), null);
            AddRow(table, fEmail, null);
            AddRow(table, Fields.Caption("Số điện thoại"), null);
            AddRow(table, fPhone, null);

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
                _txtCode.Text = existing!.ReaderCode;
                _txtCode.Enabled = false; // mã độc giả là khóa nghiệp vụ, không cho sửa
                _txtName.Text = existing.FullName;
                _txtClass.Text = existing.Class;
                _txtEmail.Text = existing.Email;
                _txtPhone.Text = existing.PhoneNumber ?? string.Empty;
            }
            else
            {
                _txtCode.Text = suggestedCode;
                _txtName.Text = presetName ?? string.Empty;
                _txtClass.Text = presetClass ?? string.Empty;
            }

            var btnSave = AddFooterButton(isEdit ? "Lưu thay đổi" : "Thêm độc giả", ButtonVariant.Primary, Theme.Icons.Check);
            var btnCancel = AddFooterButton("Hủy", ButtonVariant.Secondary);
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton = btnSave;

            foreach (var tb in new[] { _txtCode, _txtName, _txtClass, _txtEmail, _txtPhone })
            {
                tb.TextChanged += (s, e) => _lblError.Text = string.Empty;
            }
            Shown += (s, e) => (string.IsNullOrEmpty(presetName) ? _txtName : _txtEmail).Focus();
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
            var reader = new ReaderDTO
            {
                ReaderId = _existing?.ReaderId ?? 0,
                ReaderCode = _txtCode.Text.Trim(),
                FullName = _txtName.Text.Trim(),
                Class = _txtClass.Text.Trim(),
                Email = _txtEmail.Text.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(_txtPhone.Text) ? null : _txtPhone.Text.Trim()
            };

            var result = _existing == null ? _services.Readers.AddReader(reader) : _services.Readers.UpdateReader(reader);
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
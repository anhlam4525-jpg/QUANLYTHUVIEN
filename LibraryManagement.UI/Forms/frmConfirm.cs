using System.ComponentModel;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Forms
{
    /// <summary>Hộp thoại xác nhận / thông báo hiện đại thay cho MessageBox.</summary>
    [DesignerCategory("Code")]
    public class frmConfirm : DialogForm
    {
        private frmConfirm(string title, string message, string confirmText, string? cancelText,
                           ToastKind kind)
        {
            var (glyph, accent, soft, variant) = kind switch
            {
                ToastKind.Error => (Theme.Icons.Delete, Theme.Danger, Theme.DangerSoft, ButtonVariant.Danger),
                ToastKind.Warning => (Theme.Icons.Warning, Theme.Warning, Theme.WarningSoft, ButtonVariant.Primary),
                ToastKind.Success => (Theme.Icons.Check, Theme.Success, Theme.SuccessSoft, ButtonVariant.Success),
                _ => (Theme.Icons.Info, Theme.Primary, Theme.PrimarySoft, ButtonVariant.Primary)
            };

            SetHeader(title, "Vui lòng xác nhận thao tác", glyph, accent, soft);

            int width = Theme.S(460);
            var lbl = new Label
            {
                Text = message,
                Font = Theme.Body,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                MaximumSize = new Size(width - Theme.S(56), 0),
                Location = new Point(Theme.S(28), Theme.S(4)),
                BackColor = Color.White
            };
            BodyPanel.Controls.Add(lbl);

            var ok = AddFooterButton(confirmText, variant);
            ok.DialogResult = DialogResult.OK;
            AcceptButton = ok;

            if (cancelText != null)
            {
                var cancel = AddFooterButton(cancelText, ButtonVariant.Secondary);
                cancel.DialogResult = DialogResult.Cancel;
                CancelButton = cancel;
            }

            int textHeight = TextRenderer.MeasureText(message, Theme.Body, new Size(width - Theme.S(56), 0),
                TextFormatFlags.WordBreak).Height;
            int bodyHeight = textHeight + Theme.S(36);
            ClientSize = new Size(width, Theme.S(76) + bodyHeight + Theme.S(72) + 2);
        }

        /// <summary>Hỏi xác nhận. Trả về true nếu người dùng đồng ý.</summary>
        public static bool Ask(IWin32Window? owner, string title, string message,
                               string confirmText = "Đồng ý", bool danger = false)
        {
            using var dlg = new frmConfirm(title, message, confirmText, "Hủy",
                danger ? ToastKind.Error : ToastKind.Info);
            return dlg.ShowDialog(owner) == DialogResult.OK;
        }

        /// <summary>Thông báo 1 nút (lỗi nghiêm trọng, hướng dẫn...).</summary>
        public static void Alert(IWin32Window? owner, string title, string message, ToastKind kind = ToastKind.Info)
        {
            using var dlg = new frmConfirm(title, message, "Đã hiểu", null, kind);
            dlg.ShowDialog(owner);
        }

        /// <summary>Hỏi Thử lại / Thoát (dùng khi không kết nối được CSDL).</summary>
        public static bool AskRetry(IWin32Window? owner, string title, string message)
        {
            using var dlg = new frmConfirm(title, message, "Thử lại", "Thoát", ToastKind.Warning);
            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowInTaskbar = true;
            return dlg.ShowDialog(owner) == DialogResult.OK;
        }
    }
}

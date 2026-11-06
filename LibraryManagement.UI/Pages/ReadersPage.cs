using System.Text.RegularExpressions;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DTO;
using LibraryManagement.UI.Controls;
using LibraryManagement.UI.Forms;

namespace LibraryManagement.UI.Pages
{
    /// <summary>Quản lý độc giả: hiển thị, thêm, sửa, xóa, tìm kiếm.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class ReadersPage : PageBase
    {
        private readonly DataGridView _grid;
        private readonly Label _empty;
        private readonly TextBox _txtSearch;
        private readonly Card _gridCard;
        private readonly InfoPanel _info;
        private readonly ModernButton _btnEdit;
        private readonly ModernButton _btnDelete;
        private readonly ModernButton _btnDetailEdit;
        private readonly ModernButton _btnDetailBorrow;
        private readonly System.Windows.Forms.Timer _debounce;
        private List<BorrowRecordDTO> _activeRecords = new();

        public ReadersPage(AppServices services) : base(services, "LibraryManagement.UI.ReadersPage")
        {
            Title = "Quản lý độc giả";
            Subtitle = "Danh sách sinh viên có thẻ thư viện – thêm, sửa, xóa, tìm kiếm";

            var root = Grid(1, 2);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(88)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ================= TOOLBAR =================
            var toolbar = new Card
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, Theme.S(16)),
                Padding = new Padding(Theme.S(16), Theme.S(16), Theme.S(16), Theme.S(16))
            };
            var (bar, left, right) = ToolbarRow(Color.White);
            var fSearch = Fields.Text(out _txtSearch, "Tìm theo mã, họ tên, lớp hoặc email…", Theme.Icons.Search);
            fSearch.Width = Theme.S(400);
            left.Controls.Add(fSearch);

            var btnAdd = new ModernButton("Thêm độc giả", ButtonVariant.Primary, Theme.Icons.Add);
            _btnDelete = new ModernButton("Xóa", ButtonVariant.DangerSoft, Theme.Icons.Delete);
            _btnEdit = new ModernButton("Cập nhật", ButtonVariant.Secondary, Theme.Icons.Edit);
            var btnRefresh = new ModernButton("Làm mới", ButtonVariant.Secondary, Theme.Icons.Refresh);
            right.Controls.AddRange(new Control[] { btnAdd, _btnDelete, _btnEdit, btnRefresh });
            toolbar.AddFill(bar);
            root.Controls.Add(toolbar, 0, 0);

            // ================= NỘI DUNG =================
            var body = Grid(2, 1);
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(340)));

            _gridCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.S(16), 0) };
            _gridCard.Padding = new Padding(Theme.S(16), Theme.S(16), Theme.S(16), Theme.S(12));
            _gridCard.SetHeader("Danh sách độc giả", "Đang tải…");
            _grid = GridStyle.Create();
            _grid.RowTemplate.Height = Theme.S(52);
            BuildColumns();
            _gridCard.AddFill(_grid);
            _empty = AttachEmptyState(_grid, "Không tìm thấy độc giả phù hợp");

            var detailCard = new Card { Dock = DockStyle.Fill };
            detailCard.SetHeader("Hồ sơ độc giả");
            _info = new InfoPanel { UseInitials = true, ProgressColor = Theme.Primary, EmptyText = "Chọn một độc giả để xem hồ sơ và tình trạng mượn sách" };

            var detailButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = Theme.S(48),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(0, Theme.S(8), 0, 0)
            };
            detailButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            detailButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            detailButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _btnDetailEdit = new ModernButton("Sửa", ButtonVariant.Secondary, Theme.Icons.Edit)
            { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.S(6), 0) };
            _btnDetailBorrow = new ModernButton("Tạo phiếu mượn", ButtonVariant.Primary, Theme.Icons.Switch)
            { Dock = DockStyle.Fill, Margin = new Padding(Theme.S(6), 0, 0, 0) };
            detailButtons.Controls.Add(_btnDetailEdit, 0, 0);
            detailButtons.Controls.Add(_btnDetailBorrow, 1, 0);
            detailCard.Controls.Add(detailButtons);
            detailCard.AddFill(_info);

            body.Controls.Add(_gridCard, 0, 0);
            body.Controls.Add(detailCard, 1, 0);
            root.Controls.Add(body, 0, 1);
            Controls.Add(root);

            // ================= SỰ KIỆN =================
            _debounce = Debounce(() => LoadData());
            _txtSearch.TextChanged += (s, e) => { _debounce.Stop(); _debounce.Start(); };
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { _debounce.Stop(); LoadData(); e.SuppressKeyPress = true; }
            };

            btnAdd.Click += (s, e) => AddReader();
            _btnEdit.Click += (s, e) => EditSelected();
            _btnDetailEdit.Click += (s, e) => EditSelected();
            _btnDelete.Click += (s, e) => DeleteSelected();
            _btnDetailBorrow.Click += (s, e) =>
            {
                var r = GridStyle.Selected<ReaderDTO>(_grid);
                if (r != null) Navigate?.Invoke("borrow", new BorrowRequest(r.ReaderId, null));
            };
            btnRefresh.Click += (s, e) =>
            {
                _txtSearch.Clear();
                _debounce.Stop();
                LoadData();
                ShowToast("Đã làm mới danh sách độc giả.", ToastKind.Info);
            };

            _grid.SelectionChanged += (s, e) => UpdateDetail();
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
            _grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
                if (e.KeyCode == Keys.Enter) { EditSelected(); e.Handled = true; }
            };
            _grid.CellPainting += Grid_CellPainting;
        }

        private void BuildColumns()
        {
            GridStyle.AddColumn(_grid, nameof(ReaderDTO.ReaderCode), "MÃ ĐỘC GIẢ", 110).DefaultCellStyle.Font = Theme.BodyBold;
            GridStyle.AddColumn(_grid, nameof(ReaderDTO.FullName), "HỌ VÀ TÊN", 220, fill: true);
            GridStyle.AddColumn(_grid, nameof(ReaderDTO.Class), "LỚP", 100);
            GridStyle.AddColumn(_grid, nameof(ReaderDTO.Email), "EMAIL", 190);
            GridStyle.AddColumn(_grid, nameof(ReaderDTO.PhoneNumber), "SỐ ĐIỆN THOẠI", 130);
            GridStyle.AddColumn(_grid, nameof(ReaderDTO.CreatedAt), "NGÀY ĐĂNG KÝ", 120, format: "dd/MM/yyyy");
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not ReaderDTO r) return;
            string column = _grid.Columns[e.ColumnIndex].Name;

            if (column == nameof(ReaderDTO.FullName))
            {
                int borrowing = _activeRecords.Count(x => x.ReaderId == r.ReaderId);
                GridStyle.PaintAvatarCell(e, r.FullName, borrowing > 0 ? $"Đang mượn {borrowing} cuốn" : "Không có sách đang mượn");
            }
            else if (column == nameof(ReaderDTO.Class))
            {
                GridStyle.PaintBadge(e, r.Class, Theme.PrimarySoft, Theme.PrimarySoftText);
            }
        }

        public override void OnNavigatedTo(object? parameter)
        {
            LoadData();
        }

        private void LoadData(int? selectReaderId = null)
        {
            try
            {
                int? keepId = selectReaderId ?? GridStyle.Selected<ReaderDTO>(_grid)?.ReaderId;
                _activeRecords = Services.Borrows.SearchRecords(string.Empty, BorrowRecordDTO.StatusBorrowing);
                var list = Services.Readers.SearchReaders(_txtSearch.Text);

                _grid.DataSource = list;
                _empty.Visible = list.Count == 0;
                int borrowingReaders = list.Count(r => _activeRecords.Any(a => a.ReaderId == r.ReaderId));
                _gridCard.SetSubtitle($"{list.Count} độc giả  ·  {borrowingReaders} người đang mượn sách");

                if (keepId.HasValue) GridStyle.SelectWhere<ReaderDTO>(_grid, r => r.ReaderId == keepId.Value);
                UpdateDetail();
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải danh sách độc giả");
            }
        }

        private void UpdateDetail()
        {
            var r = GridStyle.Selected<ReaderDTO>(_grid);
            bool has = r != null;
            _btnEdit.Enabled = has;
            _btnDelete.Enabled = has;
            _btnDetailEdit.Enabled = has;
            _btnDetailBorrow.Enabled = has;

            if (r == null)
            {
                _info.Clear();
                return;
            }

            var mine = _activeRecords.Where(x => x.ReaderId == r.ReaderId).ToList();
            int overdue = mine.Count(x => x.IsOverdue);

            _info.HeaderTitle = r.FullName;
            _info.HeaderSubtitle = $"{r.ReaderCode}  ·  LỚP {r.Class}";
            _info.HeaderColor = Theme.ColorFor(r.FullName);
            _info.Progress = (double)mine.Count / BorrowValidator.MaxActiveBorrowsPerReader;
            _info.ProgressCaption = $"Hạn mức mượn: {mine.Count}/{BorrowValidator.MaxActiveBorrowsPerReader} cuốn";
            _info.SetRows(new (string, string, Color?)[]
            {
                ("Email", r.Email, null),
                ("Số điện thoại", string.IsNullOrWhiteSpace(r.PhoneNumber) ? "—" : r.PhoneNumber, null),
                ("Ngày đăng ký", r.CreatedAt.ToString("dd/MM/yyyy"), null),
                ("Đang mượn", $"{mine.Count} cuốn", mine.Count > 0 ? (Color?)Theme.PrimarySoftText : null),
                ("Quá hạn", overdue > 0 ? $"{overdue} phiếu" : "Không", overdue > 0 ? (Color?)Theme.DangerText : Theme.SuccessText)
            });
        }

        /// <summary>Gợi ý mã độc giả tiếp theo: DG + (số lớn nhất + 1).</summary>
        private string SuggestNextCode()
        {
            try
            {
                int max = Services.Readers.GetAllReaders()
                    .Select(r => Regex.Match(r.ReaderCode, @"^DG(\d+)$", RegexOptions.IgnoreCase))
                    .Where(m => m.Success)
                    .Select(m => int.TryParse(m.Groups[1].Value, out int n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                return "DG" + (max + 1).ToString("D3");
            }
            catch
            {
                return string.Empty;
            }
        }

        private void AddReader()
        {
            LogAction("Mở form thêm độc giả");
            using var dlg = new frmReaderEditor(Services, null, SuggestNextCode());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            ShowToast(dlg.ResultMessage);
            LoadData(dlg.ResultId);
            RaiseDataChanged();
        }

        private void EditSelected()
        {
            var r = GridStyle.Selected<ReaderDTO>(_grid);
            if (r == null)
            {
                ShowToast("Vui lòng chọn độc giả cần cập nhật.", ToastKind.Warning);
                return;
            }
            LogAction("Mở form sửa độc giả", r.ReaderCode);
            using var dlg = new frmReaderEditor(Services, r, r.ReaderCode);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            ShowToast(dlg.ResultMessage);
            LoadData(r.ReaderId);
            RaiseDataChanged();
        }

        private void DeleteSelected()
        {
            var r = GridStyle.Selected<ReaderDTO>(_grid);
            if (r == null)
            {
                ShowToast("Vui lòng chọn độc giả cần xóa.", ToastKind.Warning);
                return;
            }

            if (!frmConfirm.Ask(FindForm(), "Xóa độc giả",
                    $"Bạn có chắc muốn xóa độc giả {r.FullName} ({r.ReaderCode})?\nLịch sử phiếu đã trả của độc giả cũng sẽ bị xóa.",
                    "Xóa độc giả", danger: true))
            {
                return;
            }

            var result = Services.Readers.DeleteReader(r.ReaderId);
            ShowToast(result.Message, result.Success ? ToastKind.Success : ToastKind.Warning);
            if (result.Success)
            {
                LoadData();
                RaiseDataChanged();
            }
        }
    }
}

using LibraryManagement.DTO;
using LibraryManagement.UI.Controls;
using LibraryManagement.UI.Forms;

namespace LibraryManagement.UI.Pages
{
    /// <summary>Quản lý sách: hiển thị, thêm, sửa, xóa, tìm kiếm theo tên / tác giả / thể loại.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class BooksPage : PageBase
    {
        private readonly DataGridView _grid;
        private readonly Label _empty;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cboCategory;
        private readonly Card _gridCard;
        private readonly InfoPanel _info;
        private readonly ModernButton _btnEdit;
        private readonly ModernButton _btnDelete;
        private readonly ModernButton _btnDetailEdit;
        private readonly ModernButton _btnDetailBorrow;
        private readonly System.Windows.Forms.Timer _debounce;
        private bool _suppressFilter;

        public BooksPage(AppServices services) : base(services, "LibraryManagement.UI.BooksPage")
        {
            Title = "Quản lý sách";
            Subtitle = "Thêm, sửa, xóa và tìm kiếm đầu sách theo tên, tác giả hoặc thể loại";

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

            var fSearch = Fields.Text(out _txtSearch, "Tìm theo tên sách, tác giả, thể loại…", Theme.Icons.Search);
            fSearch.Width = Theme.S(360);
            var fCategory = Fields.Combo(out _cboCategory, glyph: Theme.Icons.Tag);
            fCategory.Width = Theme.S(200);
            left.Controls.Add(fSearch);
            left.Controls.Add(fCategory);

            var btnAdd = new ModernButton("Thêm sách", ButtonVariant.Primary, Theme.Icons.Add);
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
            _gridCard.SetHeader("Danh sách đầu sách", "Đang tải…");
            _grid = GridStyle.Create();
            BuildColumns();
            _gridCard.AddFill(_grid);
            _empty = AttachEmptyState(_grid, "Không tìm thấy sách phù hợp với bộ lọc");

            var detailCard = new Card { Dock = DockStyle.Fill };
            detailCard.SetHeader("Thông tin sách");
            _info = new InfoPanel { Glyph = Theme.Icons.Book, EmptyText = "Chọn một cuốn sách trong danh sách để xem chi tiết" };

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
            _btnDetailBorrow = new ModernButton("Cho mượn", ButtonVariant.Primary, Theme.Icons.Switch)
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
            _cboCategory.SelectedIndexChanged += (s, e) => { if (!_suppressFilter) LoadData(); };

            btnAdd.Click += (s, e) => AddBook();
            _btnEdit.Click += (s, e) => EditSelected();
            _btnDetailEdit.Click += (s, e) => EditSelected();
            _btnDelete.Click += (s, e) => DeleteSelected();
            _btnDetailBorrow.Click += (s, e) =>
            {
                var b = GridStyle.Selected<BookDTO>(_grid);
                if (b != null) Navigate?.Invoke("borrow", new BorrowRequest(null, b.BookId));
            };
            btnRefresh.Click += (s, e) =>
            {
                _suppressFilter = true;
                _txtSearch.Clear();
                _debounce.Stop();
                _suppressFilter = false;
                ReloadCategories(resetToAll: true);
                LoadData();
                ShowToast("Đã làm mới danh sách sách.", ToastKind.Info);
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
            GridStyle.AddColumn(_grid, nameof(BookDTO.BookId), "MÃ SÁCH", 90, align: DataGridViewContentAlignment.MiddleLeft);
            var title = GridStyle.AddColumn(_grid, nameof(BookDTO.Title), "TÊN SÁCH", 200, fill: true);
            title.DefaultCellStyle.Font = Theme.BodyBold;
            GridStyle.AddColumn(_grid, nameof(BookDTO.Author), "TÁC GIẢ", 160);
            GridStyle.AddColumn(_grid, nameof(BookDTO.Category), "THỂ LOẠI", 120);
            GridStyle.AddColumn(_grid, nameof(BookDTO.PublishYear), "NĂM XB", 80, align: DataGridViewContentAlignment.MiddleCenter);
            GridStyle.AddColumn(_grid, nameof(BookDTO.TotalQuantity), "TỔNG", 70, align: DataGridViewContentAlignment.MiddleCenter);
            GridStyle.AddColumn(_grid, nameof(BookDTO.BorrowedQuantity), "ĐANG MƯỢN", 100, align: DataGridViewContentAlignment.MiddleCenter);
            GridStyle.AddColumn(_grid, nameof(BookDTO.AvailableQuantity), "CÒN LẠI", 100, align: DataGridViewContentAlignment.MiddleCenter);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BookDTO book) return;
            string column = _grid.Columns[e.ColumnIndex].Name;

            if (column == nameof(BookDTO.Category))
            {
                Color c = Theme.ColorFor(book.Category);
                GridStyle.PaintBadge(e, book.Category, Theme.Blend(c, Color.White, 0.86f), Theme.Blend(c, Color.Black, 0.2f));
            }
            else if (column == nameof(BookDTO.AvailableQuantity))
            {
                if (book.AvailableQuantity > 0)
                    GridStyle.PaintBadge(e, $"{book.AvailableQuantity} cuốn", Theme.SuccessSoft, Theme.SuccessText, center: true);
                else
                    GridStyle.PaintBadge(e, "Hết sách", Theme.DangerSoft, Theme.DangerText, center: true);
            }
        }

        public override void OnNavigatedTo(object? parameter)
        {
            ReloadCategories(resetToAll: false);
            LoadData();
            if (parameter is string s && s == "add")
            {
                BeginInvoke(new Action(AddBook));
            }
        }

        private void ReloadCategories(bool resetToAll)
        {
            _suppressFilter = true;
            _cboCategory.BeginUpdate();
            try
            {
                string current = resetToAll ? "Tất cả" : (_cboCategory.SelectedItem as string ?? "Tất cả");
                _cboCategory.Items.Clear();
                _cboCategory.Items.Add("Tất cả");
                foreach (string c in Services.Books.GetCategories()) _cboCategory.Items.Add(c);
                int idx = _cboCategory.Items.IndexOf(current);
                _cboCategory.SelectedIndex = idx >= 0 ? idx : 0;
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải danh sách thể loại");
            }
            finally
            {
                _cboCategory.EndUpdate();
                _suppressFilter = false;
            }
        }

        private void LoadData(int? selectBookId = null)
        {
            try
            {
                int? keepId = selectBookId ?? GridStyle.Selected<BookDTO>(_grid)?.BookId;
                string category = _cboCategory.SelectedItem as string ?? "Tất cả";
                var list = Services.Books.SearchBooks(_txtSearch.Text, category);

                _grid.DataSource = list;
                _empty.Visible = list.Count == 0;
                _gridCard.SetSubtitle($"{list.Count} đầu sách  ·  {list.Sum(b => b.TotalQuantity)} cuốn  ·  còn {list.Sum(b => b.AvailableQuantity)} cuốn có thể mượn");

                if (keepId.HasValue) GridStyle.SelectWhere<BookDTO>(_grid, b => b.BookId == keepId.Value);
                UpdateDetail();
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải danh sách sách");
            }
        }

        private void UpdateDetail()
        {
            var b = GridStyle.Selected<BookDTO>(_grid);
            bool has = b != null;
            _btnEdit.Enabled = has;
            _btnDelete.Enabled = has;
            _btnDetailEdit.Enabled = has;
            _btnDetailBorrow.Enabled = has && b!.AvailableQuantity > 0;

            if (b == null)
            {
                _info.Clear();
                return;
            }

            _info.HeaderTitle = b.Title;
            _info.HeaderSubtitle = $"{b.Category.ToUpperInvariant()}  ·  MÃ {b.BookId}";
            _info.HeaderColor = Theme.ColorFor(b.Category);
            _info.Progress = b.TotalQuantity == 0 ? 0 : (double)b.AvailableQuantity / b.TotalQuantity;
            _info.ProgressCaption = b.TotalQuantity == 0
                ? "Chưa có cuốn nào trong kho"
                : $"Tình trạng kho: còn {b.AvailableQuantity}/{b.TotalQuantity} cuốn ({(double)b.AvailableQuantity / b.TotalQuantity:P0})";
            _info.SetRows(new (string, string, Color?)[]
            {
                ("Tác giả", b.Author, null),
                ("Thể loại", b.Category, null),
                ("Năm xuất bản", b.PublishYear.ToString(), null),
                ("Tổng số lượng", $"{b.TotalQuantity} cuốn", null),
                ("Đang cho mượn", $"{b.BorrowedQuantity} cuốn", b.BorrowedQuantity > 0 ? (Color?)Theme.WarningText : null),
                ("Còn có thể mượn", $"{b.AvailableQuantity} cuốn", b.AvailableQuantity > 0 ? (Color?)Theme.SuccessText : Theme.DangerText)
            });
        }

        private List<string> CurrentCategories() =>
            _cboCategory.Items.Cast<object>().Select(o => o.ToString() ?? string.Empty)
                .Where(c => c.Length > 0 && c != "Tất cả").ToList();

        private void AddBook()
        {
            LogAction("Mở form thêm sách");
            using var dlg = new frmBookEditor(Services, null, CurrentCategories());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            ShowToast(dlg.ResultMessage);
            ReloadCategories(resetToAll: false);
            LoadData(dlg.ResultId);
            RaiseDataChanged();
        }

        private void EditSelected()
        {
            var b = GridStyle.Selected<BookDTO>(_grid);
            if (b == null)
            {
                ShowToast("Vui lòng chọn sách cần cập nhật.", ToastKind.Warning);
                return;
            }

            LogAction("Mở form sửa sách", b.BookId);
            using var dlg = new frmBookEditor(Services, b, CurrentCategories());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            ShowToast(dlg.ResultMessage);
            ReloadCategories(resetToAll: false);
            LoadData(b.BookId);
            RaiseDataChanged();
        }

        private void DeleteSelected()
        {
            var b = GridStyle.Selected<BookDTO>(_grid);
            if (b == null)
            {
                ShowToast("Vui lòng chọn sách cần xóa.", ToastKind.Warning);
                return;
            }

            if (!frmConfirm.Ask(FindForm(), "Xóa sách",
                    $"Bạn có chắc muốn xóa sách \"{b.Title}\" (mã {b.BookId})?\nLịch sử phiếu đã trả của sách cũng sẽ bị xóa. Thao tác này không thể hoàn tác.",
                    "Xóa sách", danger: true))
            {
                return;
            }

            var result = Services.Books.DeleteBook(b.BookId);
            ShowToast(result.Message, result.Success ? ToastKind.Success : ToastKind.Warning);
            if (result.Success)
            {
                ReloadCategories(resetToAll: false);
                LoadData();
                RaiseDataChanged();
            }
        }
    }
}

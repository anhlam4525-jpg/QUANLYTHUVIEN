using System.Text.RegularExpressions;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DTO;
using LibraryManagement.UI.Controls;
using LibraryManagement.UI.Forms;

namespace LibraryManagement.UI.Pages
{
    /// <summary>Tham số khi chuyển sang trang Mượn/Trả từ trang khác (chọn sẵn độc giả hoặc sách).</summary>
    public record BorrowRequest(int? ReaderId, int? BookId);

    /// <summary>Mượn – trả sách: tạo phiếu mượn (lưu hạn trả), ghi nhận ngày trả, không cho mượn khi hết sách.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class BorrowPage : PageBase
    {
        private static readonly string[] Tabs = { "Tất cả", "Đang mượn", "Quá hạn", "Đã trả" };

        private readonly ComboBox _cboReader;
        private readonly ComboBox _cboBook;
        private readonly DateTimePicker _dtpDue;
        private readonly TextBox _txtNotes;
        private readonly Label _lblReaderHint;
        private readonly Label _lblBookHint;
        private readonly Label _lblDueHint;
        private readonly ModernButton _btnCreate;

        private readonly Card _listCard;
        private readonly SegmentedTabs _tabs;
        private readonly TextBox _txtSearch;
        private readonly DataGridView _grid;
        private readonly Label _empty;
        private readonly ModernButton _btnReturn;
        private readonly System.Windows.Forms.Timer _debounce;

        private List<BorrowRecordDTO> _allRecords = new();

        public BorrowPage(AppServices services) : base(services, "LibraryManagement.UI.BorrowPage")
        {
            Title = "Mượn / Trả sách";
            Subtitle = "Tạo phiếu mượn, theo dõi hạn trả và ghi nhận trả sách";

            var root = Grid(2, 1);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(390)));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ================= FORM TẠO PHIẾU =================
            var formCard = new Card
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, Theme.S(16), 0),
                Padding = new Padding(Theme.S(22), Theme.S(18), Theme.S(22), Theme.S(18)),
                AutoScroll = true
            };
            formCard.SetHeader("Tạo phiếu mượn", "Chọn độc giả, sách và hạn trả");

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = Color.White
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var fReader = Fields.Combo(out _cboReader, editable: true, glyph: Theme.Icons.Contact);
            _lblReaderHint = Fields.Hint();
            var fBook = Fields.Combo(out _cboBook, editable: true, glyph: Theme.Icons.Book);
            _lblBookHint = Fields.Hint();
            var fDue = Fields.Date(out _dtpDue);
            _lblDueHint = Fields.Hint();

            var chips = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = Color.White,
                Margin = new Padding(0, Theme.S(8), 0, 0)
            };
            foreach (int days in new[] { 7, 14, 30 })
            {
                var chip = new ModernButton($"+{days} ngày", ButtonVariant.Secondary)
                {
                    Height = Theme.S(30),
                    Font = Theme.SmallBold,
                    Margin = new Padding(0, 0, Theme.S(8), 0)
                };
                chip.AutoFit();
                int d = days;
                chip.Click += (s, e) => _dtpDue.Value = DateTime.Today.AddDays(d);
                chips.Controls.Add(chip);
            }

            var fNotes = Fields.Text(out _txtNotes, "VD: Mượn làm tiểu luận (không bắt buộc)", Theme.Icons.Edit);
            _txtNotes.MaxLength = 255;

            _btnCreate = new ModernButton("Tạo phiếu mượn", ButtonVariant.Primary, Theme.Icons.Accept)
            {
                Height = Theme.S(46),
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, Theme.S(22), 0, 0),
                Radius = Theme.Sf(10)
            };

            foreach (var c in new Control[]
                     {
                         Fields.Caption("Độc giả", true), fReader, _lblReaderHint,
                         Fields.Caption("Sách cần mượn", true), fBook, _lblBookHint,
                         Fields.Caption("Hạn trả", true), fDue, chips, _lblDueHint,
                         Fields.Caption("Ghi chú"), fNotes,
                         _btnCreate
                     })
            {
                if (c is FieldBox)
                {
                    c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    c.Margin = new Padding(0);
                }
                if (c is Label lbl && lbl.Font == Theme.Small)
                {
                    lbl.MaximumSize = new Size(Theme.S(330), 0);
                }
                stack.RowCount++;
                stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                stack.Controls.Add(c, 0, stack.RowCount - 1);
            }
            formCard.Controls.Add(stack);
            stack.BringToFront();

            // ================= DANH SÁCH PHIẾU =================
            _listCard = new Card { Dock = DockStyle.Fill, Padding = new Padding(Theme.S(16), Theme.S(16), Theme.S(16), Theme.S(12)) };
            _tabs = new SegmentedTabs { Items = Tabs };
            var listHeader = _listCard.SetHeader("Danh sách phiếu mượn", "Đang tải…", _tabs);

            var (bar, left, right) = ToolbarRow(Color.White);
            bar.Dock = DockStyle.Top;
            bar.Height = Theme.S(52);
            bar.Padding = new Padding(0, 0, 0, Theme.S(10));
            var fSearch = Fields.Text(out _txtSearch, "Tìm theo mã/tên độc giả hoặc tên sách…", Theme.Icons.Search);
            fSearch.Width = Theme.S(340);
            left.Controls.Add(fSearch);
            _btnReturn = new ModernButton("Ghi nhận trả sách", ButtonVariant.Success, Theme.Icons.Return);
            right.Controls.Add(_btnReturn);

            _grid = GridStyle.Create();
            _grid.RowTemplate.Height = Theme.S(52);
            BuildColumns();

            _listCard.Controls.Add(bar);
            _listCard.AddFill(_grid);
            listHeader.SendToBack();
            _empty = AttachEmptyState(_grid, "Không có phiếu mượn nào");

            root.Controls.Add(formCard, 0, 0);
            root.Controls.Add(_listCard, 1, 0);
            Controls.Add(root);

            // ================= SỰ KIỆN =================
            _cboReader.SelectedIndexChanged += (s, e) => UpdateReaderHint();
            _cboReader.TextChanged += (s, e) => UpdateReaderHint();
            _cboBook.SelectedIndexChanged += (s, e) => UpdateBookHint();
            _dtpDue.ValueChanged += (s, e) => UpdateDueHint();
            _btnCreate.Click += (s, e) => CreateTicket();

            _tabs.SelectedIndexChanged += (s, e) => ApplyFilter();
            _debounce = Debounce(LoadRecords);
            _txtSearch.TextChanged += (s, e) => { _debounce.Stop(); _debounce.Start(); };
            _btnReturn.Click += (s, e) => ReturnSelected();
            _grid.SelectionChanged += (s, e) => UpdateReturnButton();
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ReturnSelected(); };
            _grid.CellPainting += Grid_CellPainting;
            _grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == nameof(BorrowRecordDTO.ReturnDate) && (e.Value == null || e.Value == DBNull.Value))
                {
                    e.Value = "—";
                    e.FormattingApplied = true;
                }
            };
        }

        private void BuildColumns()
        {
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.RecordId), "SỐ PHIẾU", 90, format: "'#'0");
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.ReaderName), "ĐỘC GIẢ", 200, fill: true);
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.BookTitle), "SÁCH", 200, fill: true);
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.BorrowDate), "NGÀY MƯỢN", 110, format: "dd/MM/yyyy");
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.DueDate), "HẠN TRẢ", 110, format: "dd/MM/yyyy");
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.ReturnDate), "NGÀY TRẢ", 110, format: "dd/MM/yyyy");
            GridStyle.AddColumn(_grid, nameof(BorrowRecordDTO.DisplayStatus), "TRẠNG THÁI", 150);
        }

        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BorrowRecordDTO r) return;
            string column = _grid.Columns[e.ColumnIndex].Name;

            if (column == nameof(BorrowRecordDTO.ReaderName))
            {
                GridStyle.PaintAvatarCell(e, r.ReaderName, r.ReaderCode);
            }
            else if (column == nameof(BorrowRecordDTO.DisplayStatus))
            {
                if (r.IsOverdue)
                    GridStyle.PaintBadge(e, $"Quá hạn {r.OverdueDays} ngày", Theme.DangerSoft, Theme.DangerText);
                else if (r.Status == BorrowRecordDTO.StatusReturned)
                    GridStyle.PaintBadge(e, "Đã trả", Theme.SuccessSoft, Theme.SuccessText);
                else
                    GridStyle.PaintBadge(e, "Đang mượn", Theme.InfoSoft, Theme.InfoText);
            }
        }

        public override void OnNavigatedTo(object? parameter)
        {
            LoadCombos();
            LoadRecords();
            if (_dtpDue.Value.Date < DateTime.Today) _dtpDue.Value = Services.Borrows.SuggestDueDate();
            UpdateDueHint();

            if (parameter is BorrowRequest req)
            {
                if (req.ReaderId.HasValue)
                    SelectItem<ReaderDTO>(_cboReader, r => r.ReaderId == req.ReaderId.Value);
                if (req.BookId.HasValue)
                    SelectItem<BookDTO>(_cboBook, b => b.BookId == req.BookId.Value);
                BeginInvoke(new Action(() => (req.ReaderId.HasValue ? _cboBook : _cboReader).Focus()));
            }
        }

        // ---------------------------------------------------------------
        // FORM TẠO PHIẾU
        // ---------------------------------------------------------------
        private void LoadCombos()
        {
            try
            {
                int? readerId = (_cboReader.SelectedItem as ReaderDTO)?.ReaderId;
                int? bookId = (_cboBook.SelectedItem as BookDTO)?.BookId;

                _cboReader.BeginUpdate();
                _cboReader.Items.Clear();
                foreach (var r in Services.Readers.GetAllReaders()) _cboReader.Items.Add(r);
                _cboReader.EndUpdate();

                _cboBook.BeginUpdate();
                _cboBook.Items.Clear();
                foreach (var b in Services.Books.GetAllBooks().OrderBy(b => b.Title)) _cboBook.Items.Add(b);
                _cboBook.EndUpdate();

                if (readerId.HasValue) SelectItem<ReaderDTO>(_cboReader, r => r.ReaderId == readerId.Value);
                else _cboReader.SelectedIndex = -1;
                if (bookId.HasValue) SelectItem<BookDTO>(_cboBook, b => b.BookId == bookId.Value);
                else _cboBook.SelectedIndex = -1;

                UpdateReaderHint();
                UpdateBookHint();
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải danh sách độc giả và sách");
            }
        }

        private static void SelectItem<T>(ComboBox combo, Func<T, bool> predicate) where T : class
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is T item && predicate(item))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        /// <summary>Lấy đối tượng đang chọn; nếu người dùng gõ tay thì tìm theo chuỗi hiển thị.</summary>
        private static T? Resolve<T>(ComboBox combo) where T : class
        {
            if (combo.SelectedItem is T selected) return selected;
            string text = combo.Text.Trim();
            if (text.Length == 0) return null;
            return combo.Items.OfType<T>().FirstOrDefault(i =>
                string.Equals(combo.GetItemText(i), text, StringComparison.CurrentCultureIgnoreCase));
        }

        private void UpdateReaderHint()
        {
            var r = Resolve<ReaderDTO>(_cboReader);
            if (r == null)
            {
                if (_cboReader.Text.Trim().Length > 0)
                    SetHint(_lblReaderHint, "+ Người mới – bấm \"Tạo phiếu mượn\" để đăng ký nhanh độc giả này", Theme.PrimarySoftText);
                else
                    SetHint(_lblReaderHint, "Gõ mã hoặc tên để tìm nhanh, hoặc gõ tên người mới", Theme.TextMuted);
                return;
            }
            var mine = _allRecords.Where(x => x.ReaderId == r.ReaderId && x.Status == BorrowRecordDTO.StatusBorrowing).ToList();
            int overdue = mine.Count(x => x.IsOverdue);
            if (overdue > 0)
                SetHint(_lblReaderHint, $"⚠ Đang có {overdue} phiếu quá hạn – cần trả trước khi mượn tiếp", Theme.DangerText);
            else if (mine.Count >= BorrowValidator.MaxActiveBorrowsPerReader)
                SetHint(_lblReaderHint, $"⚠ Đã mượn tối đa {BorrowValidator.MaxActiveBorrowsPerReader} cuốn", Theme.DangerText);
            else
                SetHint(_lblReaderHint, $"✓ Đang mượn {mine.Count}/{BorrowValidator.MaxActiveBorrowsPerReader} cuốn · Lớp {r.Class}", Theme.SuccessText);
        }

        private void UpdateBookHint()
        {
            var b = Resolve<BookDTO>(_cboBook);
            if (b == null)
            {
                SetHint(_lblBookHint, "Gõ tên sách để tìm nhanh", Theme.TextMuted);
                return;
            }
            if (b.AvailableQuantity > 0)
                SetHint(_lblBookHint, $"✓ Còn {b.AvailableQuantity}/{b.TotalQuantity} cuốn có thể mượn · {b.Author}", Theme.SuccessText);
            else
                SetHint(_lblBookHint, "⚠ Sách đã hết – không thể cho mượn", Theme.DangerText);
        }

        private void UpdateDueHint()
        {
            int days = (_dtpDue.Value.Date - DateTime.Today).Days;
            string text = $"Ngày mượn: {DateTime.Today:dd/MM/yyyy}  ·  Thời hạn: {days} ngày (tối đa {BorrowValidator.MaxLoanDays})";
            SetHint(_lblDueHint, text, days < 0 || days > BorrowValidator.MaxLoanDays ? Theme.DangerText : Theme.TextMuted);
        }

        private static void SetHint(Label label, string text, Color color)
        {
            label.Text = text;
            label.ForeColor = color;
        }

        private void CreateTicket()
        {
            var reader = Resolve<ReaderDTO>(_cboReader);
            var book = Resolve<BookDTO>(_cboBook);
            if (reader == null)
            {
                string typed = _cboReader.Text.Trim();
                if (typed.Length == 0)
                {
                    ShowToast("Vui lòng chọn hoặc nhập tên độc giả.", ToastKind.Warning);
                    _cboReader.Focus();
                    return;
                }
                // Người mượn chưa có trong CSDL → đăng ký nhanh rồi lập phiếu luôn
                reader = QuickRegisterReader(typed);
                if (reader == null) return;
            }
            if (book == null)
            {
                ShowToast("Vui lòng chọn sách hợp lệ trong danh sách.", ToastKind.Warning);
                _cboBook.Focus();
                return;
            }

            _btnCreate.Enabled = false;
            try
            {
                LogAction("Tạo phiếu mượn", $"{reader.ReaderCode}/{book.BookId}");
                var result = Services.Borrows.BorrowBook(reader.ReaderId, book.BookId, _dtpDue.Value.Date, _txtNotes.Text);
                if (!result.Success)
                {
                    ShowToast(result.Message, ToastKind.Warning);
                    return;
                }

                ShowToast(result.Message);
                _txtNotes.Clear();
                _cboBook.SelectedIndex = -1;
                _cboBook.Text = string.Empty;
                LoadCombos();
                LoadRecords(result.Id);
                RaiseDataChanged();
            }
            finally
            {
                _btnCreate.Enabled = true;
            }
        }

        /// <summary>Đăng ký nhanh người mượn từ bên ngoài (chưa có trong danh sách độc giả).</summary>
        private ReaderDTO? QuickRegisterReader(string name)
        {
            if (!frmConfirm.Ask(FindForm(), "Độc giả mới",
                    $"\"{name}\" chưa có trong danh sách độc giả.\nĐăng ký nhanh người này để lập phiếu mượn?",
                    "Đăng ký ngay"))
            {
                return null;
            }

            using var dlg = new frmReaderEditor(Services, null, SuggestNextCode(), name, "KHACH");
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return null;

            LogAction("Đăng ký nhanh độc giả tại màn hình mượn", dlg.ResultId);
            ShowToast(dlg.ResultMessage);
            RaiseDataChanged();

            LoadCombos();
            SelectItem<ReaderDTO>(_cboReader, r => r.ReaderId == dlg.ResultId);
            return _cboReader.SelectedItem as ReaderDTO;
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

        // ---------------------------------------------------------------
        // DANH SÁCH PHIẾU
        // ---------------------------------------------------------------
        private void LoadRecords() => LoadRecords(null);

        private void LoadRecords(int? selectRecordId)
        {
            try
            {
                _allRecords = Services.Borrows.SearchRecords(_txtSearch.Text, "Tất cả");
                _tabs.SetCounts(new[]
                {
                    _allRecords.Count,
                    _allRecords.Count(r => r.Status == BorrowRecordDTO.StatusBorrowing),
                    _allRecords.Count(r => r.IsOverdue),
                    _allRecords.Count(r => r.Status == BorrowRecordDTO.StatusReturned)
                });
                ApplyFilter(selectRecordId);
                UpdateReaderHint();
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải danh sách phiếu mượn");
            }
        }

        private void ApplyFilter() => ApplyFilter(null);

        private void ApplyFilter(int? selectRecordId)
        {
            int? keepId = selectRecordId ?? GridStyle.Selected<BorrowRecordDTO>(_grid)?.RecordId;
            IEnumerable<BorrowRecordDTO> view = _tabs.SelectedItem switch
            {
                "Đang mượn" => _allRecords.Where(r => r.Status == BorrowRecordDTO.StatusBorrowing),
                "Quá hạn" => _allRecords.Where(r => r.IsOverdue),
                "Đã trả" => _allRecords.Where(r => r.Status == BorrowRecordDTO.StatusReturned),
                _ => _allRecords
            };
            var list = view.ToList();
            _grid.DataSource = list;
            _empty.Visible = list.Count == 0;
            _listCard.SetSubtitle($"{list.Count} phiếu  ·  {_allRecords.Count(r => r.Status == BorrowRecordDTO.StatusBorrowing)} phiếu chưa trả  ·  {_allRecords.Count(r => r.IsOverdue)} quá hạn");
            if (keepId.HasValue) GridStyle.SelectWhere<BorrowRecordDTO>(_grid, r => r.RecordId == keepId.Value);
            UpdateReturnButton();
        }

        private void UpdateReturnButton()
        {
            var r = GridStyle.Selected<BorrowRecordDTO>(_grid);
            _btnReturn.Enabled = r != null && r.Status == BorrowRecordDTO.StatusBorrowing;
        }

        private void ReturnSelected()
        {
            var r = GridStyle.Selected<BorrowRecordDTO>(_grid);
            if (r == null || r.Status != BorrowRecordDTO.StatusBorrowing)
            {
                ShowToast("Vui lòng chọn một phiếu đang mượn để ghi nhận trả sách.", ToastKind.Warning);
                return;
            }

            string late = r.IsOverdue ? $"\nPhiếu đã quá hạn {r.OverdueDays} ngày." : string.Empty;
            if (!frmConfirm.Ask(FindForm(), "Ghi nhận trả sách",
                    $"Xác nhận {r.ReaderName} ({r.ReaderCode}) trả sách \"{r.BookTitle}\" – phiếu #{r.RecordId}?{late}",
                    "Xác nhận trả"))
            {
                return;
            }

            var result = Services.Borrows.ReturnBook(r.RecordId);
            ShowToast(result.Message, result.Success ? ToastKind.Success : ToastKind.Warning);
            if (result.Success)
            {
                LoadCombos();
                LoadRecords(r.RecordId);
                RaiseDataChanged();
            }
        }
    }
}
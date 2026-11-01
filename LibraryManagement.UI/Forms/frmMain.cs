using System.Data;
using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Services;
using LibraryManagement.DTO;
using LibraryManagement.UI.Logging;

namespace LibraryManagement.UI.Forms
{
    public partial class frmMain : Form
    {
        private readonly ILogger _logger;
        private readonly BookService _bookService;
        private readonly ReaderService _readerService;
        private readonly BorrowService _borrowService;

        // UI Controls - Containers
        private Panel pnlSidebar = null!;
        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblDbStatus = null!;
        private ToolStripStatusLabel lblSummaryStatus = null!;

        // Content Panels
        private Panel pnlBooksView = null!;
        private Panel pnlReadersView = null!;
        private Panel pnlBorrowView = null!;
        private Panel pnlDashboardView = null!;

        // Navigation Buttons
        private Button btnNavHome = null!;
        private Button btnNavBooks = null!;
        private Button btnNavReaders = null!;
        private Button btnNavBorrow = null!;
        private Button btnNavExit = null!;

        // Books Controls
        private DataGridView dgvBooks = null!;
        private TextBox txtSearchBook = null!;
        private ComboBox cboCategoryFilter = null!;
        private TextBox txtBookId = null!;
        private TextBox txtBookTitle = null!;
        private TextBox txtBookAuthor = null!;
        private ComboBox cboBookCategory = null!;
        private TextBox txtPublishYear = null!;
        private TextBox txtTotalQuantity = null!;
        private Label lblAvailableCount = null!;

        // Readers Controls
        private DataGridView dgvReaders = null!;
        private TextBox txtSearchReader = null!;
        private TextBox txtReaderId = null!;
        private TextBox txtReaderCode = null!;
        private TextBox txtReaderFullName = null!;
        private TextBox txtReaderClass = null!;
        private TextBox txtReaderEmail = null!;
        private TextBox txtReaderPhone = null!;

        // Borrow Controls
        private ComboBox cboBorrowReader = null!;
        private ComboBox cboBorrowBook = null!;
        private DateTimePicker dtpBorrowDate = null!;
        private DateTimePicker dtpDueDate = null!;
        private Button btnCreateBorrow = null!;
        private Label lblBookStockWarning = null!;
        private DataGridView dgvBorrowRecords = null!;
        private Button btnReturnBook = null!;

        // Dashboard Stats Labels
        private Label lblStatTitles = null!;
        private Label lblStatCopies = null!;
        private Label lblStatAvailable = null!;
        private Label lblStatBorrowing = null!;
        private Label lblStatReaders = null!;

        public frmMain()
        {
            InitializeComponent();
            _logger = AppLogging.CreateLogger("LibraryManagement.UI");
            _bookService = new BookService();
            _readerService = new ReaderService();
            _borrowService = new BorrowService();

            BuildCustomInterface();
            _logger.LogInformation("Ứng dụng Quản Lý Thư Viện khởi động thành công.");
        }

        private void BuildCustomInterface()
        {
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.MinimumSize = new Size(1150, 720);

            // 1. Header Panel
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42) // Dark Navy
            };

            var lblHeaderTitle = new Label
            {
                Text = "📚 HỆ THỐNG QUẢN LÝ THƯ VIỆN",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 16),
                AutoSize = true
            };
            var lblSubtitle = new Label
            {
                Text = "Library Management System | C# .NET 8 WinForms & SQL Server",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(380, 22),
                AutoSize = true
            };
            var lblUserInfo = new Label
            {
                Text = $"👤 Thủ thư: Admin | {DateTime.Now:dd/MM/yyyy}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(226, 232, 240),
                Dock = DockStyle.Right,
                Width = 260,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 20, 0)
            };
            pnlHeader.Controls.AddRange(new Control[] { lblHeaderTitle, lblSubtitle, lblUserInfo });

            // 2. Sidebar Navigation
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            btnNavHome = CreateNavButton("📊 Thống kê Dashboard", 20);
            btnNavBooks = CreateNavButton("📖 Quản lý sách", 80);
            btnNavReaders = CreateNavButton("👥 Quản lý độc giả", 140);
            btnNavBorrow = CreateNavButton("🔄 Mượn – Trả sách", 200);
            btnNavExit = CreateNavButton("🚪 Đăng xuất / Thoát", 260);

            btnNavHome.Click += (s, e) => ShowView(pnlDashboardView, btnNavHome);
            btnNavBooks.Click += (s, e) => ShowView(pnlBooksView, btnNavBooks);
            btnNavReaders.Click += (s, e) => ShowView(pnlReadersView, btnNavReaders);
            btnNavBorrow.Click += (s, e) => ShowView(pnlBorrowView, btnNavBorrow);
            btnNavExit.Click += (s, e) => { if (MessageBox.Show("Bạn có muốn thoát khỏi hệ thống?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Application.Exit(); };

            pnlSidebar.Controls.AddRange(new Control[] { btnNavHome, btnNavBooks, btnNavReaders, btnNavBorrow, btnNavExit });

            // 3. Status Bar
            statusStrip = new StatusStrip();
            lblDbStatus = new ToolStripStatusLabel("Cơ sở dữ liệu: LibraryDB (LocalDB/SQL Server) | Trạng thái: Kết nối")
            {
                ForeColor = Color.DarkSlateGray
            };
            lblSummaryStatus = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Sẵn sàng"
            };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblDbStatus, lblSummaryStatus });

            // 4. Content Panel hosting views
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Build individual views
            BuildBooksView();
            BuildReadersView();
            BuildBorrowView();
            BuildDashboardView();

            // Add views to Content
            pnlContent.Controls.AddRange(new Control[] { pnlDashboardView, pnlBooksView, pnlReadersView, pnlBorrowView });

            // Add main layout controls
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(statusStrip);

            // Default view: Books Management
            ShowView(pnlBooksView, btnNavBooks);
        }

        private Button CreateNavButton(string text, int top)
        {
            return new Button
            {
                Text = text,
                Location = new Point(12, top),
                Size = new Size(196, 48),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Cursor = Cursors.Hand
            };
        }

        private void ShowView(Panel targetView, Button activeBtn)
        {
            pnlBooksView.Visible = false;
            pnlReadersView.Visible = false;
            pnlBorrowView.Visible = false;
            pnlDashboardView.Visible = false;

            targetView.Visible = true;
            targetView.BringToFront();

            foreach (Control c in pnlSidebar.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = Color.FromArgb(30, 41, 59);
                    b.ForeColor = Color.FromArgb(203, 213, 225);
                }
            }
            activeBtn.BackColor = Color.FromArgb(37, 99, 235); // Blue highlight
            activeBtn.ForeColor = Color.White;

            if (targetView == pnlBooksView) LoadBooksData();
            if (targetView == pnlReadersView) LoadReadersData();
            if (targetView == pnlBorrowView) LoadBorrowData();
            if (targetView == pnlDashboardView) LoadDashboardData();
        }

        #region 1. Phân hệ Quản Lý Sách
        private void BuildBooksView()
        {
            pnlBooksView = new Panel { Dock = DockStyle.Fill, Visible = false };

            // Top action & filter bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.White, Padding = new Padding(15) };

            var lblSearch = new Label { Text = "Tìm kiếm:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtSearchBook = new TextBox { Location = new Point(85, 12), Width = 230, PlaceholderText = "Nhập tên sách, tác giả..." };

            var lblCat = new Label { Text = "Thể loại:", Location = new Point(330, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            cboCategoryFilter = new ComboBox { Location = new Point(395, 12), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };

            var btnSearch = new Button { Text = "🔍 Tìm kiếm", Location = new Point(545, 10), Width = 110, Height = 32, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnRefresh = new Button { Text = "🔄 Làm mới", Location = new Point(665, 10), Width = 100, Height = 32, BackColor = Color.FromArgb(100, 116, 139), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            var btnAdd = new Button { Text = "+ Thêm sách mới", Location = new Point(15, 52), Width = 150, Height = 32, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var btnUpdate = new Button { Text = "💾 Cập nhật", Location = new Point(175, 52), Width = 110, Height = 32, BackColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var btnDelete = new Button { Text = "❌ Xóa sách", Location = new Point(295, 52), Width = 100, Height = 32, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            btnSearch.Click += (s, e) => LoadBooksData();
            btnRefresh.Click += (s, e) => { txtSearchBook.Clear(); cboCategoryFilter.SelectedItem = "Tất cả"; LoadBooksData(); ClearBookInputs(); };
            btnAdd.Click += BtnAddBook_Click;
            btnUpdate.Click += BtnUpdateBook_Click;
            btnDelete.Click += BtnDeleteBook_Click;

            topBar.Controls.AddRange(new Control[] { lblSearch, txtSearchBook, lblCat, cboCategoryFilter, btnSearch, btnRefresh, btnAdd, btnUpdate, btnDelete });

            // Right Detail Panel
            var pnlDetail = new GroupBox { Text = "Thông tin chi tiết sách", Dock = DockStyle.Right, Width = 310, BackColor = Color.White, Padding = new Padding(15) };
            int rowY = 30;
            txtBookId = AddDetailField(pnlDetail, "Mã sách:", ref rowY, isReadOnly: true);
            txtBookTitle = AddDetailField(pnlDetail, "Tên sách (*):", ref rowY);
            txtBookAuthor = AddDetailField(pnlDetail, "Tác giả (*):", ref rowY);

            var lblCbo = new Label { Text = "Thể loại (*):", Location = new Point(15, rowY), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            cboBookCategory = new ComboBox { Location = new Point(15, rowY + 22), Width = 265, DropDownStyle = ComboBoxStyle.DropDown };
            cboBookCategory.Items.AddRange(new object[] { "CNTT", "Kỹ năng", "Toán tin", "Ngoại ngữ", "Kinh tế", "DevOps", "AI" });
            cboBookCategory.SelectedIndex = 0;
            pnlDetail.Controls.AddRange(new Control[] { lblCbo, cboBookCategory });
            rowY += 60;

            txtPublishYear = AddDetailField(pnlDetail, "Năm xuất bản (*):", ref rowY);
            txtTotalQuantity = AddDetailField(pnlDetail, "Tổng số lượng (*):", ref rowY);

            lblAvailableCount = new Label
            {
                Text = "Tồn khả dụng: 0 cuốn",
                Location = new Point(15, rowY + 5),
                AutoSize = true,
                ForeColor = Color.FromArgb(16, 185, 129),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            pnlDetail.Controls.Add(lblAvailableCount);

            // Central DataGridView
            dgvBooks = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };
            dgvBooks.SelectionChanged += DgvBooks_SelectionChanged;

            pnlBooksView.Controls.Add(dgvBooks);
            pnlBooksView.Controls.Add(pnlDetail);
            pnlBooksView.Controls.Add(topBar);
        }

        private TextBox AddDetailField(Control parent, string labelText, ref int top, bool isReadOnly = false)
        {
            var lbl = new Label { Text = labelText, Location = new Point(15, top), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var txt = new TextBox { Location = new Point(15, top + 22), Width = 265, ReadOnly = isReadOnly };
            if (isReadOnly) txt.BackColor = Color.FromArgb(241, 245, 249);
            parent.Controls.AddRange(new Control[] { lbl, txt });
            top += 58;
            return txt;
        }

        private void LoadBooksData()
        {
            try
            {
                string kw = txtSearchBook.Text.Trim();
                string cat = cboCategoryFilter.SelectedItem?.ToString() ?? "Tất cả";
                var list = _bookService.SearchBooks(kw, cat);
                dgvBooks.DataSource = list;

                // Format columns
                if (dgvBooks.Columns["BookId"] != null) dgvBooks.Columns["BookId"].HeaderText = "Mã";
                if (dgvBooks.Columns["Title"] != null) dgvBooks.Columns["Title"].HeaderText = "Tên sách";
                if (dgvBooks.Columns["Author"] != null) dgvBooks.Columns["Author"].HeaderText = "Tác giả";
                if (dgvBooks.Columns["Category"] != null) dgvBooks.Columns["Category"].HeaderText = "Thể loại";
                if (dgvBooks.Columns["PublishYear"] != null) dgvBooks.Columns["PublishYear"].HeaderText = "Năm XB";
                if (dgvBooks.Columns["TotalQuantity"] != null) dgvBooks.Columns["TotalQuantity"].HeaderText = "Tổng SL";
                if (dgvBooks.Columns["BorrowedQuantity"] != null) dgvBooks.Columns["BorrowedQuantity"].HeaderText = "Đang mượn";
                if (dgvBooks.Columns["AvailableQuantity"] != null) dgvBooks.Columns["AvailableQuantity"].HeaderText = "Khả dụng";
                if (dgvBooks.Columns["CreatedAt"] != null) dgvBooks.Columns["CreatedAt"].Visible = false;

                // Categories
                if (cboCategoryFilter.Items.Count == 0)
                {
                    cboCategoryFilter.Items.Clear();
                    foreach (var c in _bookService.GetCategories()) cboCategoryFilter.Items.Add(c);
                    cboCategoryFilter.SelectedIndex = 0;
                }

                lblSummaryStatus.Text = $"Tổng số sách hiển thị: {list.Count} đầu sách";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nạp danh sách sách: {Message}", ex.Message);
                MessageBox.Show($"Không thể kết nối SQL Server hoặc chưa tạo bảng Books!\nChi tiết: {ex.Message}", "Lỗi Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvBooks_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvBooks.CurrentRow?.DataBoundItem is BookDTO b)
            {
                txtBookId.Text = b.BookId.ToString();
                txtBookTitle.Text = b.Title;
                txtBookAuthor.Text = b.Author;
                cboBookCategory.Text = b.Category;
                txtPublishYear.Text = b.PublishYear.ToString();
                txtTotalQuantity.Text = b.TotalQuantity.ToString();
                lblAvailableCount.Text = $"Tồn khả dụng: {b.AvailableQuantity}/{b.TotalQuantity} cuốn";
            }
        }

        private void ClearBookInputs()
        {
            txtBookId.Clear();
            txtBookTitle.Clear();
            txtBookAuthor.Clear();
            txtPublishYear.Clear();
            txtTotalQuantity.Clear();
            lblAvailableCount.Text = "Tồn khả dụng: 0 cuốn";
        }

        private void BtnAddBook_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtPublishYear.Text.Trim(), out int year) || !int.TryParse(txtTotalQuantity.Text.Trim(), out int qty))
            {
                MessageBox.Show("Năm xuất bản và số lượng phải là số nguyên hợp lệ!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var book = new BookDTO
            {
                Title = txtBookTitle.Text.Trim(),
                Author = txtBookAuthor.Text.Trim(),
                Category = cboBookCategory.Text.Trim(),
                PublishYear = year,
                TotalQuantity = qty
            };

            var (success, msg, newId) = _bookService.AddBook(book);
            if (success)
            {
                _logger.LogInformation(new EventId(1001, "BookInserted"), "Thêm sách thành công: ID={BookId}, Tựa={Title}", newId, book.Title);
                MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBooksData();
                ClearBookInputs();
            }
            else
            {
                _logger.LogWarning("Thêm sách không thành công: {Message}", msg);
                MessageBox.Show(msg, "Lỗi kiểm tra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnUpdateBook_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtBookId.Text, out int bookId))
            {
                MessageBox.Show("Vui lòng chọn một cuốn sách từ danh sách để sửa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPublishYear.Text.Trim(), out int year) || !int.TryParse(txtTotalQuantity.Text.Trim(), out int qty))
            {
                MessageBox.Show("Năm xuất bản và số lượng phải là số nguyên hợp lệ!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var book = new BookDTO
            {
                BookId = bookId,
                Title = txtBookTitle.Text.Trim(),
                Author = txtBookAuthor.Text.Trim(),
                Category = cboBookCategory.Text.Trim(),
                PublishYear = year,
                TotalQuantity = qty
            };

            var (success, msg) = _bookService.UpdateBook(book);
            if (success)
            {
                _logger.LogInformation(new EventId(1002, "BookUpdated"), "Cập nhật sách thành công: ID={BookId}", bookId);
                MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBooksData();
            }
            else
            {
                _logger.LogWarning("Cập nhật sách thất bại: {Message}", msg);
                MessageBox.Show(msg, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDeleteBook_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtBookId.Text, out int bookId))
            {
                MessageBox.Show("Vui lòng chọn một cuốn sách từ danh sách để xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa cuốn sách '{txtBookTitle.Text}'?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var (success, msg) = _bookService.DeleteBook(bookId);
                if (success)
                {
                    _logger.LogInformation(new EventId(1003, "BookDeleted"), "Xóa sách thành công: ID={BookId}", bookId);
                    MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadBooksData();
                    ClearBookInputs();
                }
                else
                {
                    _logger.LogError("Lỗi xóa sách ID={BookId}: {Message}", bookId, msg);
                    MessageBox.Show(msg, "Lỗi xóa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region 2. Phân hệ Quản Lý Độc Giả
        private void BuildReadersView()
        {
            pnlReadersView = new Panel { Dock = DockStyle.Fill, Visible = false };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 95, BackColor = Color.White, Padding = new Padding(15) };
            var lblSearch = new Label { Text = "Tìm kiếm độc giả:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtSearchReader = new TextBox { Location = new Point(145, 12), Width = 280, PlaceholderText = "Nhập mã độc giả, họ tên, lớp..." };

            var btnSearch = new Button { Text = "🔍 Tìm kiếm", Location = new Point(440, 10), Width = 110, Height = 32, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnRefresh = new Button { Text = "🔄 Làm mới", Location = new Point(560, 10), Width = 100, Height = 32, BackColor = Color.FromArgb(100, 116, 139), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            var btnAdd = new Button { Text = "+ Thêm độc giả", Location = new Point(15, 52), Width = 140, Height = 32, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var btnUpdate = new Button { Text = "💾 Cập nhật", Location = new Point(165, 52), Width = 110, Height = 32, BackColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var btnDelete = new Button { Text = "❌ Xóa độc giả", Location = new Point(285, 52), Width = 120, Height = 32, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            btnSearch.Click += (s, e) => LoadReadersData();
            btnRefresh.Click += (s, e) => { txtSearchReader.Clear(); LoadReadersData(); ClearReaderInputs(); };
            btnAdd.Click += BtnAddReader_Click;
            btnUpdate.Click += BtnUpdateReader_Click;
            btnDelete.Click += BtnDeleteReader_Click;

            topBar.Controls.AddRange(new Control[] { lblSearch, txtSearchReader, btnSearch, btnRefresh, btnAdd, btnUpdate, btnDelete });

            var pnlDetail = new GroupBox { Text = "Thông tin độc giả", Dock = DockStyle.Right, Width = 310, BackColor = Color.White, Padding = new Padding(15) };
            int rowY = 30;
            txtReaderId = AddDetailField(pnlDetail, "ID Hệ thống:", ref rowY, isReadOnly: true);
            txtReaderCode = AddDetailField(pnlDetail, "Mã độc giả (VD: DG001):", ref rowY);
            txtReaderFullName = AddDetailField(pnlDetail, "Họ và tên (*):", ref rowY);
            txtReaderClass = AddDetailField(pnlDetail, "Lớp (*):", ref rowY);
            txtReaderEmail = AddDetailField(pnlDetail, "Email sinh viên (*):", ref rowY);
            txtReaderPhone = AddDetailField(pnlDetail, "Số điện thoại:", ref rowY);

            dgvReaders = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };
            dgvReaders.SelectionChanged += DgvReaders_SelectionChanged;

            pnlReadersView.Controls.Add(dgvReaders);
            pnlReadersView.Controls.Add(pnlDetail);
            pnlReadersView.Controls.Add(topBar);
        }

        private void LoadReadersData()
        {
            try
            {
                var list = _readerService.SearchReaders(txtSearchReader.Text.Trim());
                dgvReaders.DataSource = list;
                if (dgvReaders.Columns["ReaderId"] != null) dgvReaders.Columns["ReaderId"].HeaderText = "ID";
                if (dgvReaders.Columns["ReaderCode"] != null) dgvReaders.Columns["ReaderCode"].HeaderText = "Mã độc giả";
                if (dgvReaders.Columns["FullName"] != null) dgvReaders.Columns["FullName"].HeaderText = "Họ và tên";
                if (dgvReaders.Columns["Class"] != null) dgvReaders.Columns["Class"].HeaderText = "Lớp";
                if (dgvReaders.Columns["Email"] != null) dgvReaders.Columns["Email"].HeaderText = "Email";
                if (dgvReaders.Columns["PhoneNumber"] != null) dgvReaders.Columns["PhoneNumber"].HeaderText = "Điện thoại";
                if (dgvReaders.Columns["CreatedAt"] != null) dgvReaders.Columns["CreatedAt"].Visible = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nạp độc giả: {Message}", ex.Message);
            }
        }

        private void DgvReaders_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvReaders.CurrentRow?.DataBoundItem is ReaderDTO r)
            {
                txtReaderId.Text = r.ReaderId.ToString();
                txtReaderCode.Text = r.ReaderCode;
                txtReaderFullName.Text = r.FullName;
                txtReaderClass.Text = r.Class;
                txtReaderEmail.Text = r.Email;
                txtReaderPhone.Text = r.PhoneNumber ?? string.Empty;
            }
        }

        private void ClearReaderInputs()
        {
            txtReaderId.Clear();
            txtReaderCode.Clear();
            txtReaderFullName.Clear();
            txtReaderClass.Clear();
            txtReaderEmail.Clear();
            txtReaderPhone.Clear();
        }

        private void BtnAddReader_Click(object? sender, EventArgs e)
        {
            var r = new ReaderDTO
            {
                ReaderCode = txtReaderCode.Text.Trim(),
                FullName = txtReaderFullName.Text.Trim(),
                Class = txtReaderClass.Text.Trim(),
                Email = txtReaderEmail.Text.Trim(),
                PhoneNumber = txtReaderPhone.Text.Trim()
            };

            var (success, msg, newId) = _readerService.AddReader(r);
            if (success)
            {
                _logger.LogInformation("Thêm độc giả thành công: Code={Code}", r.ReaderCode);
                MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadReadersData();
                ClearReaderInputs();
            }
            else
            {
                MessageBox.Show(msg, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnUpdateReader_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtReaderId.Text, out int id))
            {
                MessageBox.Show("Vui lòng chọn độc giả cần cập nhật!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var r = new ReaderDTO
            {
                ReaderId = id,
                FullName = txtReaderFullName.Text.Trim(),
                Class = txtReaderClass.Text.Trim(),
                Email = txtReaderEmail.Text.Trim(),
                PhoneNumber = txtReaderPhone.Text.Trim()
            };

            var (success, msg) = _readerService.UpdateReader(r);
            if (success)
            {
                MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadReadersData();
            }
            else
            {
                MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDeleteReader_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtReaderId.Text, out int id))
            {
                MessageBox.Show("Vui lòng chọn độc giả cần xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa độc giả '{txtReaderFullName.Text}'?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var (success, msg) = _readerService.DeleteReader(id);
                if (success)
                {
                    MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadReadersData();
                    ClearReaderInputs();
                }
                else
                {
                    MessageBox.Show(msg, "Lỗi xóa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region 3. Phân hệ Mượn – Trả Sách
        private void BuildBorrowView()
        {
            pnlBorrowView = new Panel { Dock = DockStyle.Fill, Visible = false };

            // Top GroupBox for creating Borrow tickets
            var gbCreateBorrow = new GroupBox
            {
                Text = "Tạo phiếu mượn sách mới",
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            var lblReader = new Label { Text = "Chọn độc giả:", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            cboBorrowReader = new ComboBox { Location = new Point(20, 52), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblBook = new Label { Text = "Chọn sách cần mượn:", Location = new Point(360, 30), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            cboBorrowBook = new ComboBox { Location = new Point(360, 52), Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
            cboBorrowBook.SelectedIndexChanged += CboBorrowBook_SelectedIndexChanged;

            lblBookStockWarning = new Label
            {
                Text = "",
                Location = new Point(360, 85),
                AutoSize = true,
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var lblBorrowDate = new Label { Text = "Ngày mượn:", Location = new Point(20, 95), AutoSize = true };
            dtpBorrowDate = new DateTimePicker { Location = new Point(105, 92), Width = 140, Format = DateTimePickerFormat.Short, Enabled = false, Value = DateTime.Today };

            var lblDueDate = new Label { Text = "Hạn trả:", Location = new Point(265, 95), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            dtpDueDate = new DateTimePicker { Location = new Point(330, 92), Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(14) };

            btnCreateBorrow = new Button
            {
                Text = "✓ Xác nhận mượn sách",
                Location = new Point(740, 48),
                Width = 200,
                Height = 42,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreateBorrow.Click += BtnCreateBorrow_Click;

            gbCreateBorrow.Controls.AddRange(new Control[] { lblReader, cboBorrowReader, lblBook, cboBorrowBook, lblBookStockWarning, lblBorrowDate, dtpBorrowDate, lblDueDate, dtpDueDate, btnCreateBorrow });

            // Middle Action Bar for Returns
            var pnlReturnBar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(15, 10, 15, 10) };
            var lblRecordsTitle = new Label { Text = "DANH SÁCH PHIẾU MƯỢN TRẢ HIỆN TẠI", Location = new Point(15, 16), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            btnReturnBook = new Button
            {
                Text = "↩ Ghi nhận trả sách đã chọn",
                Dock = DockStyle.Right,
                Width = 240,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReturnBook.Click += BtnReturnBook_Click;
            pnlReturnBar.Controls.AddRange(new Control[] { lblRecordsTitle, btnReturnBook });

            // DataGridView for Borrow records
            dgvBorrowRecords = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToAddRows = false
            };

            pnlBorrowView.Controls.Add(dgvBorrowRecords);
            pnlBorrowView.Controls.Add(pnlReturnBar);
            pnlBorrowView.Controls.Add(gbCreateBorrow);
        }

        private void LoadBorrowData()
        {
            try
            {
                // Load combo readers
                var readers = _readerService.GetAllReaders();
                cboBorrowReader.DataSource = readers;

                // Load combo books
                var books = _bookService.GetAllBooks();
                cboBorrowBook.DataSource = books;

                // Load records table
                var records = _borrowService.GetAllRecords();
                dgvBorrowRecords.DataSource = records;

                if (dgvBorrowRecords.Columns["RecordId"] != null) dgvBorrowRecords.Columns["RecordId"].HeaderText = "Mã phiếu";
                if (dgvBorrowRecords.Columns["ReaderCode"] != null) dgvBorrowRecords.Columns["ReaderCode"].HeaderText = "Mã ĐG";
                if (dgvBorrowRecords.Columns["ReaderName"] != null) dgvBorrowRecords.Columns["ReaderName"].HeaderText = "Họ tên độc giả";
                if (dgvBorrowRecords.Columns["BookTitle"] != null) dgvBorrowRecords.Columns["BookTitle"].HeaderText = "Tên sách";
                if (dgvBorrowRecords.Columns["BorrowDate"] != null) dgvBorrowRecords.Columns["BorrowDate"].HeaderText = "Ngày mượn";
                if (dgvBorrowRecords.Columns["DueDate"] != null) dgvBorrowRecords.Columns["DueDate"].HeaderText = "Hạn trả";
                if (dgvBorrowRecords.Columns["ReturnDate"] != null) dgvBorrowRecords.Columns["ReturnDate"].HeaderText = "Ngày trả";
                if (dgvBorrowRecords.Columns["Status"] != null) dgvBorrowRecords.Columns["Status"].HeaderText = "Trạng thái";
                if (dgvBorrowRecords.Columns["ReaderId"] != null) dgvBorrowRecords.Columns["ReaderId"].Visible = false;
                if (dgvBorrowRecords.Columns["BookId"] != null) dgvBorrowRecords.Columns["BookId"].Visible = false;
                if (dgvBorrowRecords.Columns["Notes"] != null) dgvBorrowRecords.Columns["Notes"].Visible = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nạp dữ liệu mượn trả: {Message}", ex.Message);
            }
        }

        private void CboBorrowBook_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboBorrowBook.SelectedItem is BookDTO book)
            {
                if (book.AvailableQuantity <= 0)
                {
                    lblBookStockWarning.Text = "⚠ Sách này đã hết bản cuốn trong thư viện! Không thể mượn.";
                    btnCreateBorrow.Enabled = false;
                    btnCreateBorrow.BackColor = Color.Gray;
                }
                else
                {
                    lblBookStockWarning.Text = $"✓ Sách còn khả dụng: {book.AvailableQuantity} cuốn trong kho.";
                    lblBookStockWarning.ForeColor = Color.FromArgb(16, 185, 129);
                    btnCreateBorrow.Enabled = true;
                    btnCreateBorrow.BackColor = Color.FromArgb(16, 185, 129);
                }
            }
        }

        private void BtnCreateBorrow_Click(object? sender, EventArgs e)
        {
            if (cboBorrowReader.SelectedItem is not ReaderDTO reader)
            {
                MessageBox.Show("Vui lòng chọn độc giả!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboBorrowBook.SelectedItem is not BookDTO book)
            {
                MessageBox.Show("Vui lòng chọn sách cần mượn!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (success, msg) = _borrowService.BorrowBook(reader.ReaderId, book.BookId, dtpDueDate.Value);
            if (success)
            {
                _logger.LogInformation(new EventId(2001, "BorrowCreated"), "Tạo phiếu mượn thành công: Độc giả={ReaderCode}, Sách={BookId}", reader.ReaderCode, book.BookId);
                MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBorrowData();
            }
            else
            {
                _logger.LogWarning("Mượn sách không thành công: {Message}", msg);
                MessageBox.Show(msg, "Từ chối mượn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnReturnBook_Click(object? sender, EventArgs e)
        {
            if (dgvBorrowRecords.CurrentRow?.DataBoundItem is not BorrowRecordDTO record)
            {
                MessageBox.Show("Vui lòng chọn một phiếu mượn từ danh sách để trả sách!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (record.Status == "Đã trả")
            {
                MessageBox.Show("Phiếu mượn này đã hoàn tất việc trả sách trước đó!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Xác nhận độc giả '{record.ReaderName}' trả cuốn sách '{record.BookTitle}'?", "Xác nhận trả sách", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var (success, msg) = _borrowService.ReturnBook(record.RecordId, record.BookId);
                if (success)
                {
                    _logger.LogInformation(new EventId(2002, "BookReturned"), "Đã trả sách thành công: RecordId={RecordId}, BookId={BookId}", record.RecordId, record.BookId);
                    MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadBorrowData();
                }
                else
                {
                    _logger.LogError("Lỗi trả sách: {Message}", msg);
                    MessageBox.Show(msg, "Lỗi trả sách", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region 4. Phân hệ Thống kê Dashboard
        private void BuildDashboardView()
        {
            pnlDashboardView = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(30) };

            var lblDashTitle = new Label
            {
                Text = "TỔNG QUAN HOẠT ĐỘNG THƯ VIỆN",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 20),
                AutoSize = true
            };

            int cardW = 210;
            int cardH = 120;
            int gap = 20;
            int startX = 30;
            int startY = 70;

            lblStatTitles = CreateStatCard(pnlDashboardView, "Tổng đầu sách", "0", Color.FromArgb(59, 130, 246), startX, startY, cardW, cardH);
            lblStatCopies = CreateStatCard(pnlDashboardView, "Tổng số cuốn sách", "0", Color.FromArgb(16, 185, 129), startX + (cardW + gap), startY, cardW, cardH);
            lblStatAvailable = CreateStatCard(pnlDashboardView, "Số cuốn có thể mượn", "0", Color.FromArgb(20, 184, 166), startX + (cardW + gap) * 2, startY, cardW, cardH);
            lblStatBorrowing = CreateStatCard(pnlDashboardView, "Số cuốn đang mượn", "0", Color.FromArgb(245, 158, 11), startX + (cardW + gap) * 3, startY, cardW, cardH);
            lblStatReaders = CreateStatCard(pnlDashboardView, "Tổng số độc giả", "0", Color.FromArgb(139, 92, 246), startX, startY + cardH + gap, cardW, cardH);

            var btnReloadStats = new Button
            {
                Text = "🔄 Cập nhật số liệu tức thời",
                Location = new Point(startX + (cardW + gap), startY + cardH + 40),
                Size = new Size(240, 45),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReloadStats.Click += (s, e) => LoadDashboardData();

            pnlDashboardView.Controls.AddRange(new Control[] { lblDashTitle, btnReloadStats });
        }

        private Label CreateStatCard(Control parent, string title, string initialValue, Color accentColor, int x, int y, int w, int h)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = accentColor };
            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(12, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            var lblValue = new Label
            {
                Text = initialValue,
                Location = new Point(12, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            card.Controls.AddRange(new Control[] { topBar, lblTitle, lblValue });
            parent.Controls.Add(card);
            return lblValue;
        }

        private void LoadDashboardData()
        {
            try
            {
                var stats = _borrowService.GetDashboardStats();
                lblStatTitles.Text = stats.TotalBookTitles.ToString("N0");
                lblStatCopies.Text = stats.TotalCopies.ToString("N0");
                lblStatAvailable.Text = stats.AvailableCopies.ToString("N0");
                lblStatBorrowing.Text = stats.BorrowedCopies.ToString("N0");
                lblStatReaders.Text = stats.TotalReaders.ToString("N0");

                _logger.LogInformation("Thống kê thư viện: Đầu sách={Titles}, Khả dụng={Available}, Đang mượn={Borrowed}",
                    stats.TotalBookTitles, stats.AvailableCopies, stats.BorrowedCopies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nạp thống kê: {Message}", ex.Message);
            }
        }
        #endregion
    }
}

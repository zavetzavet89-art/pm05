using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class BookingsForm : Form
    {
        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cmbStatus;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;

        public BookingsForm()
        {
            InitializeComponent();
            Load += (_, __) => Reload();
        }

        private void InitializeComponent()
        {
            _txtSearch = new TextBox { Location = new Point(12, 12), Size = new Size(160, 23) };
            _txtSearch.TextChanged += (_, __) => Reload();

            _cmbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 12),
                Size = new Size(150, 23)
            };
            _cmbStatus.Items.AddRange(new object[] { "Все", BookingStatuses.AwaitingPayment, BookingStatuses.Paid, BookingStatuses.Cancelled });
            _cmbStatus.SelectedIndex = 0;
            _cmbStatus.SelectedIndexChanged += (_, __) => Reload();

            _btnAdd = new Button { Text = "Оформить", Location = new Point(340, 10), Size = new Size(90, 26) };
            _btnEdit = new Button { Text = "Изменить", Location = new Point(436, 10), Size = new Size(90, 26) };
            _btnDelete = new Button { Text = "Удалить", Location = new Point(532, 10), Size = new Size(90, 26) };

            _btnAdd.Click += (_, __) => OpenEditor(null);
            _btnEdit.Click += (_, __) => OpenSelected();
            _btnDelete.Click += (_, __) => DeleteSelected();

            _grid = new DataGridView
            {
                Location = new Point(12, 44),
                Size = new Size(710, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _grid.CellFormatting += Grid_CellFormatting;

            Controls.AddRange(new Control[] { _txtSearch, _cmbStatus, _btnAdd, _btnEdit, _btnDelete, _grid });

            Text = "Бронирования";
            ClientSize = new Size(740, 360);
            StartPosition = FormStartPosition.CenterParent;

            var canEdit = PermissionManager.CanEditData;
            _btnAdd.Enabled = canEdit;
            _btnEdit.Enabled = canEdit;
            _btnDelete.Enabled = canEdit;

            AppTheme.ApplyCrudForm(this, _grid);
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var statusCell = _grid.Rows[e.RowIndex].Cells["BookingStatus"];
            if (statusCell?.Value == null) return;

            var status = statusCell.Value.ToString();
            if (status == BookingStatuses.Paid)
                e.CellStyle.BackColor = Color.Honeydew;
            else if (status == BookingStatuses.AwaitingPayment)
                e.CellStyle.BackColor = Color.LemonChiffon;
            else if (status == BookingStatuses.Cancelled)
                e.CellStyle.BackColor = Color.Gainsboro;
        }

        private void Reload()
        {
            var search = _txtSearch.Text?.Trim() ?? "";
            var statusFilter = _cmbStatus.SelectedItem?.ToString() ?? "Все";

            using (var db = new ApplicationDbContext())
            {
                var query =
                    from b in db.Bookings
                    join c in db.Clients on b.ClientId equals c.Id
                    join t in db.Tours on b.TourId equals t.Id
                    select new
                    {
                        b.Id,
                        b.DateCreated,
                        Клиент = c.FullName,
                        Тур = t.TourName,
                        БазоваяЦена = t.BaseCost,
                        b.FinalAmount,
                        b.BookingStatus
                    };

                if (statusFilter != "Все")
                    query = query.Where(x => x.BookingStatus == statusFilter);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(x =>
                        x.Клиент.Contains(search) ||
                        x.Тур.Contains(search) ||
                        x.BookingStatus.Contains(search));
                }

                _grid.DataSource = query.OrderByDescending(x => x.DateCreated).ToList();
            }
        }

        private int? GetSelectedId()
        {
            if (_grid.CurrentRow == null) return null;
            return Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
        }

        private void OpenSelected()
        {
            var id = GetSelectedId();
            if (!id.HasValue)
            {
                MessageBox.Show("Выберите бронь.", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OpenEditor(id);
        }

        private void OpenEditor(int? id)
        {
            using (var form = new BookingEditForm(id))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    Reload();
            }
        }

        private void DeleteSelected()
        {
            var id = GetSelectedId();
            if (!id.HasValue) return;

            if (MessageBox.Show("Удалить бронирование?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            using (var db = new ApplicationDbContext())
            {
                var payments = db.Payments.Where(p => p.BookingId == id.Value).ToList();
                var booking = db.Bookings.Find(id.Value);
                if (booking == null) return;

                var clientId = booking.ClientId;
                db.Payments.RemoveRange(payments);
                db.Bookings.Remove(booking);
                db.SaveChanges();

                LoyaltyManager.RecalculateClientLoyalty(clientId, db);
                db.SaveChanges();
            }
            Reload();
        }
    }

    public class BookingEditForm : Form
    {
        private readonly int? _bookingId;
        private ComboBox _cmbClients;
        private ComboBox _cmbTours;
        private DateTimePicker _dtpDate;
        private ComboBox _cmbStatus;
        private Label _lblLoyalty;
        private Label _lblFinalAmount;
        private Button _btnSpendPoints;
        private bool _pointsSpent;

        public BookingEditForm(int? bookingId = null)
        {
            _bookingId = bookingId;
            InitializeComponent();
            Load += BookingEditForm_Load;
        }

        private void InitializeComponent()
        {
            _cmbClients = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 28), Size = new Size(360, 23) };
            _cmbClients.SelectedIndexChanged += (_, __) => UpdateAmounts();

            _cmbTours = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 68), Size = new Size(360, 23) };
            _cmbTours.SelectedIndexChanged += (_, __) => UpdateAmounts();

            _dtpDate = new DateTimePicker { Location = new Point(12, 108), Size = new Size(200, 23), Format = DateTimePickerFormat.Short };

            _cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 148), Size = new Size(200, 23) };
            _cmbStatus.Items.AddRange(new object[] { BookingStatuses.AwaitingPayment, BookingStatuses.Paid, BookingStatuses.Cancelled });

            _lblLoyalty = new Label { Location = new Point(12, 180), AutoSize = true, Text = "Баллы: —  |  Скидка: —" };
            _lblFinalAmount = new Label
            {
                Location = new Point(12, 202),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Text = "Итоговая стоимость: —"
            };

            _btnSpendPoints = new Button { Text = "Списать баллы", Location = new Point(12, 230), Size = new Size(140, 28) };
            AppTheme.StylePrimaryButton(_btnSpendPoints);
            _btnSpendPoints.Click += BtnSpendPoints_Click;

            var btnSave = new Button { Text = "Сохранить", Location = new Point(12, 270), Size = new Size(100, 28) };
            var btnCancel = new Button { Text = "Отмена", Location = new Point(120, 270), Size = new Size(100, 28) };
            AppTheme.StylePrimaryButton(btnSave);
            AppTheme.StyleSecondaryButton(btnCancel);
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (_, __) => Close();

            Controls.AddRange(new Control[]
            {
                new Label { Text = "Клиент", Location = new Point(12, 12), AutoSize = true }, _cmbClients,
                new Label { Text = "Тур", Location = new Point(12, 52), AutoSize = true }, _cmbTours,
                new Label { Text = "Дата брони", Location = new Point(12, 92), AutoSize = true }, _dtpDate,
                new Label { Text = "Статус", Location = new Point(12, 132), AutoSize = true }, _cmbStatus,
                _lblLoyalty, _lblFinalAmount, _btnSpendPoints, btnSave, btnCancel
            });

            Text = _bookingId.HasValue ? "Редактирование брони" : "Новая бронь";
            ClientSize = new Size(390, 315);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppTheme.Background;

            if (!PermissionManager.CanEditData)
            {
                btnSave.Enabled = false;
                _cmbClients.Enabled = false;
                _cmbTours.Enabled = false;
                _dtpDate.Enabled = false;
                _cmbStatus.Enabled = false;
                _btnSpendPoints.Enabled = false;
            }
        }

        private void BookingEditForm_Load(object sender, EventArgs e)
        {
            using (var db = new ApplicationDbContext())
            {
                _cmbClients.DisplayMember = "FullName";
                _cmbClients.ValueMember = "Id";
                _cmbClients.DataSource = db.Clients.OrderBy(c => c.FullName).ToList();

                _cmbTours.DisplayMember = "TourName";
                _cmbTours.ValueMember = "Id";
                _cmbTours.DataSource = db.Tours.Where(t => t.IsAvailable).OrderBy(t => t.TourName).ToList();
            }

            if (_bookingId.HasValue)
            {
                using (var db = new ApplicationDbContext())
                {
                    var booking = db.Bookings.Find(_bookingId.Value);
                    if (booking == null) return;
                    _cmbClients.SelectedValue = booking.ClientId;
                    _cmbTours.SelectedValue = booking.TourId;
                    _dtpDate.Value = booking.DateCreated;
                    _cmbStatus.SelectedItem = booking.BookingStatus;
                }
            }
            else
            {
                _dtpDate.Value = DateTime.Now;
                _cmbStatus.SelectedItem = BookingStatuses.AwaitingPayment;
            }

            UpdateAmounts();
        }

        private Client GetSelectedClient()
        {
            return _cmbClients.SelectedItem as Client;
        }

        private Tour GetSelectedTour()
        {
            return _cmbTours.SelectedItem as Tour;
        }

        private void UpdateAmounts()
        {
            var client = GetSelectedClient();
            var tour = GetSelectedTour();

            if (client == null || tour == null)
            {
                _lblLoyalty.Text = "Баллы: —  |  Скидка: —";
                _lblFinalAmount.Text = "Итоговая стоимость: —";
                _btnSpendPoints.Enabled = false;
                return;
            }

            _lblLoyalty.Text = $"Баллы: {client.LoyaltyPoints}  |  Скидка: {client.CurrentDiscount:N0}%";

            var amount = LoyaltyManager.CalculateFinalAmount(tour.BaseCost, client.CurrentDiscount, _pointsSpent, client.LoyaltyPoints);
            _lblFinalAmount.Text = $"Итоговая стоимость: {amount:N2} ₽";

            _btnSpendPoints.Enabled = PermissionManager.CanEditData
                                      && !_pointsSpent
                                      && client.LoyaltyPoints >= LoyaltyManager.PointsToSpend;
        }

        private void BtnSpendPoints_Click(object sender, EventArgs e)
        {
            var client = GetSelectedClient();
            if (client == null || client.LoyaltyPoints < LoyaltyManager.PointsToSpend)
            {
                MessageBox.Show("Недостаточно баллов для списания.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _pointsSpent = true;
            _btnSpendPoints.Enabled = false;
            _btnSpendPoints.Text = "Баллы списаны (−1000 ₽)";
            UpdateAmounts();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cmbClients.SelectedValue == null || _cmbTours.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента и тур.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var clientId = (int)_cmbClients.SelectedValue;
            var tourId = (int)_cmbTours.SelectedValue;
            var status = _cmbStatus.SelectedItem?.ToString() ?? BookingStatuses.AwaitingPayment;

            using (var db = new ApplicationDbContext())
            {
                var client = db.Clients.Find(clientId);
                var tour = db.Tours.Find(tourId);
                if (client == null || tour == null) return;

                if (_pointsSpent)
                {
                    if (client.LoyaltyPoints < LoyaltyManager.PointsToSpend)
                    {
                        MessageBox.Show("Недостаточно баллов.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    client.LoyaltyPoints -= LoyaltyManager.PointsToSpend;
                }

                var finalAmount = LoyaltyManager.CalculateFinalAmount(
                    tour.BaseCost, client.CurrentDiscount, _pointsSpent, client.LoyaltyPoints + (_pointsSpent ? LoyaltyManager.PointsToSpend : 0));

                if (_bookingId.HasValue)
                {
                    var booking = db.Bookings.Find(_bookingId.Value);
                    if (booking == null) return;
                    booking.ClientId = clientId;
                    booking.TourId = tourId;
                    booking.DateCreated = _dtpDate.Value;
                    booking.BookingStatus = status;
                    booking.FinalAmount = finalAmount;
                }
                else
                {
                    db.Bookings.Add(new Booking
                    {
                        ClientId = clientId,
                        TourId = tourId,
                        DateCreated = _dtpDate.Value,
                        BookingStatus = status,
                        FinalAmount = finalAmount
                    });
                }

                db.SaveChanges();
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

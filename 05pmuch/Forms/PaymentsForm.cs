using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class PaymentsForm : Form
    {
        private DataGridView _grid;
        private TextBox _txtSearch;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;

        public PaymentsForm()
        {
            InitializeComponent();
            Load += (_, __) => Reload();
        }

        private void InitializeComponent()
        {
            _txtSearch = new TextBox { Location = new Point(12, 12), Size = new Size(200, 23) };
            _txtSearch.TextChanged += (_, __) => Reload();

            _btnAdd = new Button { Text = "Добавить", Location = new Point(220, 10), Size = new Size(90, 26) };
            _btnEdit = new Button { Text = "Изменить", Location = new Point(316, 10), Size = new Size(90, 26) };
            _btnDelete = new Button { Text = "Удалить", Location = new Point(412, 10), Size = new Size(90, 26) };

            _btnAdd.Click += (_, __) => EditPayment(null);
            _btnEdit.Click += (_, __) => EditSelected();
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

            Controls.AddRange(new Control[] { _txtSearch, _btnAdd, _btnEdit, _btnDelete, _grid });

            Text = "Оплаты";
            ClientSize = new Size(740, 360);
            StartPosition = FormStartPosition.CenterParent;

            var canEdit = PermissionManager.CanEditData;
            _btnAdd.Enabled = canEdit;
            _btnEdit.Enabled = canEdit;
            _btnDelete.Enabled = canEdit;

            AppTheme.ApplyCrudForm(this, _grid);
        }

        private void Reload()
        {
            var search = _txtSearch.Text?.Trim() ?? "";
            using (var db = new ApplicationDbContext())
            {
                var query =
                    from p in db.Payments
                    join b in db.Bookings on p.BookingId equals b.Id
                    join c in db.Clients on b.ClientId equals c.Id
                    join t in db.Tours on b.TourId equals t.Id
                    select new
                    {
                        p.Id,
                        p.BookingId,
                        Клиент = c.FullName,
                        Тур = t.TourName,
                        p.SumPaid,
                        p.DatePaid
                    };

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(x =>
                        x.Клиент.Contains(search) ||
                        x.Тур.Contains(search));
                }

                _grid.DataSource = query.OrderByDescending(x => x.DatePaid).ToList();
            }
        }

        private Payment GetSelected()
        {
            if (_grid.CurrentRow == null) return null;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            using (var db = new ApplicationDbContext())
                return db.Payments.Find(id);
        }

        private void EditSelected()
        {
            var item = GetSelected();
            if (item == null)
            {
                MessageBox.Show("Выберите оплату.", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            EditPayment(item);
        }

        private void EditPayment(Payment payment)
        {
            using (var dlg = new PaymentEditDialog(payment))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Reload();
            }
        }

        private void DeleteSelected()
        {
            var item = GetSelected();
            if (item == null) return;

            if (MessageBox.Show("Удалить оплату?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var bookingId = item.BookingId;
            using (var db = new ApplicationDbContext())
            {
                var entity = db.Payments.Find(item.Id);
                if (entity != null)
                {
                    db.Payments.Remove(entity);
                    db.SaveChanges();
                }
            }

            LoyaltyManager.ApplyPaymentEffects(bookingId);
            Reload();
        }
    }

    internal class PaymentEditDialog : Form
    {
        private readonly Payment _payment;
        private ComboBox _cmbBooking;
        private NumericUpDown _numAmount;
        private DateTimePicker _dtpDate;
        private Label _lblHint;

        public PaymentEditDialog(Payment payment)
        {
            _payment = payment;
            InitializeComponent();
            Load += PaymentEditDialog_Load;
        }

        private void InitializeComponent()
        {
            _cmbBooking = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 28), Size = new Size(400, 23) };
            _cmbBooking.SelectedIndexChanged += (_, __) => UpdateHint();

            _numAmount = new NumericUpDown { Location = new Point(12, 68), Size = new Size(120, 23), DecimalPlaces = 2, Maximum = 10000000, Minimum = 0 };
            _dtpDate = new DateTimePicker { Location = new Point(12, 108), Size = new Size(200, 23), Format = DateTimePickerFormat.Short };

            _lblHint = new Label { Location = new Point(12, 138), AutoSize = true, Text = "Итог по брони: —" };

            var btnOk = new Button { Text = "Сохранить", Location = new Point(12, 170), Size = new Size(100, 28) };
            var btnCancel = new Button { Text = "Отмена", Location = new Point(120, 170), Size = new Size(100, 28) };
            AppTheme.StylePrimaryButton(btnOk);
            AppTheme.StyleSecondaryButton(btnCancel);
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[]
            {
                new Label { Text = "Бронирование", Location = new Point(12, 12), AutoSize = true }, _cmbBooking,
                new Label { Text = "Сумма", Location = new Point(12, 52), AutoSize = true }, _numAmount,
                new Label { Text = "Дата оплаты", Location = new Point(12, 92), AutoSize = true }, _dtpDate,
                _lblHint, btnOk, btnCancel
            });

            Text = _payment == null ? "Новая оплата" : "Редактирование оплаты";
            ClientSize = new Size(430, 215);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.Background;
        }

        private void PaymentEditDialog_Load(object sender, EventArgs e)
        {
            using (var db = new ApplicationDbContext())
            {
                var bookings = (
                    from b in db.Bookings
                    join c in db.Clients on b.ClientId equals c.Id
                    join t in db.Tours on b.TourId equals t.Id
                    orderby b.DateCreated descending
                    select new { b.Id, c.FullName, t.TourName, b.BookingStatus, b.FinalAmount })
                    .AsEnumerable()
                    .Select(x => new BookingListItem
                    {
                        Id = x.Id,
                        Display = $"#{x.Id} — {x.FullName} / {x.TourName} ({x.BookingStatus})",
                        FinalAmount = x.FinalAmount
                    })
                    .ToList();

                _cmbBooking.DisplayMember = "Display";
                _cmbBooking.ValueMember = "Id";
                _cmbBooking.DataSource = bookings;
            }

            if (_payment != null)
            {
                _cmbBooking.SelectedValue = _payment.BookingId;
                _numAmount.Value = _payment.SumPaid;
                _dtpDate.Value = _payment.DatePaid;
            }
            else
            {
                _dtpDate.Value = DateTime.Now;
            }

            UpdateHint();
        }

        private void UpdateHint()
        {
            var item = _cmbBooking.SelectedItem as BookingListItem;
            _lblHint.Text = item == null
                ? "Итог по брони: —"
                : $"Итог по брони: {item.FinalAmount:N2} ₽";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_cmbBooking.SelectedValue == null)
            {
                MessageBox.Show("Выберите бронирование.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_numAmount.Value <= 0)
            {
                MessageBox.Show("Введите сумму больше нуля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var bookingId = (int)_cmbBooking.SelectedValue;
            int? oldBookingId = _payment?.BookingId;

            using (var db = new ApplicationDbContext())
            {
                if (_payment == null)
                {
                    db.Payments.Add(new Payment
                    {
                        BookingId = bookingId,
                        SumPaid = _numAmount.Value,
                        DatePaid = _dtpDate.Value
                    });
                }
                else
                {
                    var entity = db.Payments.Find(_payment.Id);
                    if (entity == null) return;
                    oldBookingId = entity.BookingId;
                    entity.BookingId = bookingId;
                    entity.SumPaid = _numAmount.Value;
                    entity.DatePaid = _dtpDate.Value;
                }
                db.SaveChanges();
            }

            LoyaltyManager.ApplyPaymentEffects(bookingId);
            if (oldBookingId.HasValue && oldBookingId.Value != bookingId)
                LoyaltyManager.ApplyPaymentEffects(oldBookingId.Value);

            DialogResult = DialogResult.OK;
        }

        private class BookingListItem
        {
            public int Id { get; set; }
            public string Display { get; set; }
            public decimal FinalAmount { get; set; }
        }
    }
}

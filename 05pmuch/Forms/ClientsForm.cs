using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class ClientsForm : Form
    {
        private DataGridView _grid;
        private TextBox _txtSearch;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;

        public ClientsForm()
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

            _btnAdd.Click += (_, __) => EditClient(null);
            _btnEdit.Click += (_, __) => EditSelected();
            _btnDelete.Click += (_, __) => DeleteSelected();

            _grid = new DataGridView
            {
                Location = new Point(12, 44),
                Size = new Size(660, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            Controls.AddRange(new Control[] { _txtSearch, _btnAdd, _btnEdit, _btnDelete, _grid });

            Text = "Клиенты";
            ClientSize = new Size(690, 340);
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
                var query = db.Clients.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.FullName.Contains(search) ||
                        c.PhoneNumber.Contains(search) ||
                        c.PassportNumber.Contains(search));
                }

                _grid.DataSource = query
                    .OrderBy(c => c.FullName)
                    .Select(c => new
                    {
                        c.Id,
                        c.FullName,
                        c.PassportNumber,
                        c.PhoneNumber,
                        c.LoyaltyPoints,
                        c.CurrentDiscount
                    })
                    .AsEnumerable()
                    .Select(c => new
                    {
                        c.Id,
                        c.FullName,
                        c.PassportNumber,
                        c.PhoneNumber,
                        c.LoyaltyPoints,
                        Скидка = c.CurrentDiscount + "%"
                    })
                    .ToList();
            }
        }

        private Client GetSelected()
        {
            if (_grid.CurrentRow == null) return null;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            using (var db = new ApplicationDbContext())
                return db.Clients.Find(id);
        }

        private void EditSelected()
        {
            var item = GetSelected();
            if (item == null)
            {
                MessageBox.Show("Выберите клиента.", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            EditClient(item);
        }

        private void EditClient(Client client)
        {
            using (var dlg = new ClientEditDialog(client))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Reload();
            }
        }

        private void DeleteSelected()
        {
            var item = GetSelected();
            if (item == null) return;

            using (var db = new ApplicationDbContext())
            {
                if (db.Bookings.Any(b => b.ClientId == item.Id))
                {
                    MessageBox.Show("Нельзя удалить клиента: есть бронирования.", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (MessageBox.Show($"Удалить «{item.FullName}»?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            using (var db = new ApplicationDbContext())
            {
                var entity = db.Clients.Find(item.Id);
                if (entity != null)
                {
                    db.Clients.Remove(entity);
                    db.SaveChanges();
                }
            }
            Reload();
        }
    }

    internal class ClientEditDialog : Form
    {
        private readonly Client _client;
        private TextBox _txtFullName;
        private TextBox _txtPassport;
        private TextBox _txtPhone;

        public ClientEditDialog(Client client)
        {
            _client = client;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _txtFullName = new TextBox { Location = new Point(12, 28), Size = new Size(300, 23) };
            _txtPassport = new TextBox { Location = new Point(12, 68), Size = new Size(300, 23) };
            _txtPhone = new TextBox { Location = new Point(12, 108), Size = new Size(300, 23) };

            if (_client != null)
            {
                _txtFullName.Text = _client.FullName;
                _txtPassport.Text = _client.PassportNumber;
                _txtPhone.Text = _client.PhoneNumber;
            }

            var btnOk = new Button { Text = "Сохранить", Location = new Point(12, 145), Size = new Size(100, 28) };
            var btnCancel = new Button { Text = "Отмена", Location = new Point(120, 145), Size = new Size(100, 28) };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[]
            {
                new Label { Text = "ФИО", Location = new Point(12, 12), AutoSize = true }, _txtFullName,
                new Label { Text = "Паспорт", Location = new Point(12, 52), AutoSize = true }, _txtPassport,
                new Label { Text = "Телефон", Location = new Point(12, 92), AutoSize = true }, _txtPhone,
                btnOk, btnCancel
            });

            Text = _client == null ? "Новый клиент" : "Редактирование клиента";
            ClientSize = new Size(330, 190);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.Background;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var name = _txtFullName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите ФИО.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var passport = _txtPassport.Text?.Trim() ?? "";

            using (var db = new ApplicationDbContext())
            {
                if (!string.IsNullOrEmpty(passport))
                {
                    var duplicate = db.Clients.Any(c =>
                        c.PassportNumber == passport &&
                        (_client == null || c.Id != _client.Id));
                    if (duplicate)
                    {
                        MessageBox.Show("Клиент с таким паспортом уже существует.", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (_client == null)
                {
                    db.Clients.Add(new Client
                    {
                        FullName = name,
                        PassportNumber = passport,
                        PhoneNumber = _txtPhone.Text?.Trim() ?? "",
                        LoyaltyPoints = 0,
                        CurrentDiscount = 0
                    });
                }
                else
                {
                    var entity = db.Clients.Find(_client.Id);
                    if (entity == null) return;
                    entity.FullName = name;
                    entity.PassportNumber = passport;
                    entity.PhoneNumber = _txtPhone.Text?.Trim() ?? "";
                }
                db.SaveChanges();
            }

            DialogResult = DialogResult.OK;
        }
    }
}

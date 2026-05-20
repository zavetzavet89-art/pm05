using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class CountriesForm : Form
    {
        private DataGridView _grid;
        private TextBox _txtSearch;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;

        public CountriesForm()
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

            _btnAdd.Click += (_, __) => EditCountry(null);
            _btnEdit.Click += (_, __) => EditSelected();
            _btnDelete.Click += (_, __) => DeleteSelected();

            _grid = new DataGridView
            {
                Location = new Point(12, 44),
                Size = new Size(490, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            Controls.AddRange(new Control[] { _txtSearch, _btnAdd, _btnEdit, _btnDelete, _grid });

            Text = "Страны";
            ClientSize = new Size(520, 340);
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
                var query = db.Countries.AsQueryable();
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(c => c.CountryName.Contains(search));

                _grid.DataSource = query.OrderBy(c => c.CountryName).Select(c => new { c.Id, c.CountryName }).ToList();
            }
        }

        private Country GetSelected()
        {
            if (_grid.CurrentRow == null) return null;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            using (var db = new ApplicationDbContext())
                return db.Countries.Find(id);
        }

        private void EditSelected()
        {
            var item = GetSelected();
            if (item == null)
            {
                MessageBox.Show("Выберите страну.", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            EditCountry(item);
        }

        private void EditCountry(Country country)
        {
            using (var dlg = new CountryEditDialog(country))
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
                if (db.Tours.Any(t => t.CountryId == item.Id))
                {
                    MessageBox.Show("Нельзя удалить страну: есть связанные туры.", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (MessageBox.Show($"Удалить «{item.CountryName}»?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            using (var db = new ApplicationDbContext())
            {
                var entity = db.Countries.Find(item.Id);
                if (entity != null)
                {
                    db.Countries.Remove(entity);
                    db.SaveChanges();
                }
            }
            Reload();
        }
    }

    internal class CountryEditDialog : Form
    {
        private readonly Country _country;
        private TextBox _txtName;

        public CountryEditDialog(Country country)
        {
            _country = country;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _txtName = new TextBox { Location = new Point(12, 28), Size = new Size(300, 23) };
            if (_country != null)
                _txtName.Text = _country.CountryName;

            var btnOk = new Button { Text = "Сохранить", Location = new Point(12, 65), Size = new Size(100, 28) };
            var btnCancel = new Button { Text = "Отмена", Location = new Point(120, 65), Size = new Size(100, 28) };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[]
            {
                new Label { Text = "Название", Location = new Point(12, 12), AutoSize = true },
                _txtName, btnOk, btnCancel
            });

            Text = _country == null ? "Новая страна" : "Редактирование страны";
            ClientSize = new Size(330, 115);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.Background;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var name = _txtName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var db = new ApplicationDbContext())
            {
                if (_country == null)
                    db.Countries.Add(new Country { CountryName = name });
                else
                {
                    var entity = db.Countries.Find(_country.Id);
                    if (entity == null) return;
                    entity.CountryName = name;
                }
                db.SaveChanges();
            }

            DialogResult = DialogResult.OK;
        }
    }
}

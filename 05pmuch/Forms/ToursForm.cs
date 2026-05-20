using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class ToursForm : Form
    {
        private DataGridView _grid;
        private TextBox _txtSearch;
        private ComboBox _cmbMinStars;
        private CheckBox _chkAvailableOnly;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;

        public ToursForm()
        {
            InitializeComponent();
            Load += (_, __) => Reload();
        }

        private void InitializeComponent()
        {
            _txtSearch = new TextBox { Location = new Point(12, 12), Size = new Size(160, 23) };
            _txtSearch.TextChanged += (_, __) => Reload();

            _cmbMinStars = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 12),
                Size = new Size(150, 23)
            };
            _cmbMinStars.Items.AddRange(new object[] { "Все звёзды", "От 2★", "От 3★", "От 4★", "Только 5★" });
            _cmbMinStars.SelectedIndex = 0;
            _cmbMinStars.SelectedIndexChanged += (_, __) => Reload();

            _chkAvailableOnly = new CheckBox
            {
                Text = "Только доступные",
                Location = new Point(340, 14),
                AutoSize = true,
                Checked = true
            };
            _chkAvailableOnly.CheckedChanged += (_, __) => Reload();

            _btnAdd = new Button { Text = "Добавить", Location = new Point(12, 42), Size = new Size(90, 26) };
            _btnEdit = new Button { Text = "Изменить", Location = new Point(108, 42), Size = new Size(90, 26) };
            _btnDelete = new Button { Text = "Снять с продажи", Location = new Point(204, 42), Size = new Size(120, 26) };

            _btnAdd.Click += (_, __) => EditTour(null);
            _btnEdit.Click += (_, __) => EditSelected();
            _btnDelete.Click += (_, __) => DeleteSelected();

            _grid = new DataGridView
            {
                Location = new Point(12, 76),
                Size = new Size(660, 290),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _grid.CellFormatting += Grid_CellFormatting;

            Controls.AddRange(new Control[]
            {
                _txtSearch, _cmbMinStars, _chkAvailableOnly,
                _btnAdd, _btnEdit, _btnDelete, _grid
            });

            Text = "Каталог туров";
            ClientSize = new Size(690, 385);
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
            var starsCell = _grid.Rows[e.RowIndex].Cells["HotelStars"];
            if (starsCell?.Value == null) return;

            if (Convert.ToInt32(starsCell.Value) == 5)
                e.CellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
        }

        private int? GetMinStarsFilter()
        {
            switch (_cmbMinStars.SelectedIndex)
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 4;
                case 4: return 5;
                default: return null;
            }
        }

        private void Reload()
        {
            var search = _txtSearch.Text?.Trim() ?? "";
            var minStars = GetMinStarsFilter();

            using (var db = new ApplicationDbContext())
            {
                var query =
                    from t in db.Tours
                    join c in db.Countries on t.CountryId equals c.Id
                    select new { t, CountryName = c.CountryName };

                if (_chkAvailableOnly.Checked)
                    query = query.Where(x => x.t.IsAvailable);

                if (minStars.HasValue)
                    query = query.Where(x => x.t.HotelStars >= minStars.Value);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.t.TourName.Contains(search));

                _grid.DataSource = query
                    .OrderByDescending(x => x.t.HotelStars)
                    .ThenBy(x => x.t.TourName)
                    .Select(x => new
                    {
                        x.t.Id,
                        x.t.TourName,
                        Страна = x.CountryName,
                        x.t.BaseCost,
                        HotelStars = x.t.HotelStars,
                        Доступен = x.t.IsAvailable ? "Да" : "Нет"
                    })
                    .ToList();
            }
        }

        private Tour GetSelected()
        {
            if (_grid.CurrentRow == null) return null;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            using (var db = new ApplicationDbContext())
                return db.Tours.Find(id);
        }

        private void EditSelected()
        {
            var item = GetSelected();
            if (item == null)
            {
                MessageBox.Show("Выберите тур.", "Подсказка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            EditTour(item);
        }

        private void EditTour(Tour tour)
        {
            using (var dlg = new TourEditDialog(tour))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Reload();
            }
        }

        private void DeleteSelected()
        {
            var item = GetSelected();
            if (item == null) return;

            if (MessageBox.Show($"Снять с продажи тур «{item.TourName}»?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            using (var db = new ApplicationDbContext())
            {
                var entity = db.Tours.Find(item.Id);
                if (entity != null)
                {
                    entity.IsAvailable = false;
                    db.SaveChanges();
                }
            }
            Reload();
        }
    }

    internal class TourEditDialog : Form
    {
        private readonly Tour _tour;
        private TextBox _txtName;
        private ComboBox _cmbCountry;
        private NumericUpDown _numCost;
        private NumericUpDown _numStars;
        private CheckBox _chkAvailable;

        public TourEditDialog(Tour tour)
        {
            _tour = tour;
            InitializeComponent();
            Load += TourEditDialog_Load;
        }

        private void InitializeComponent()
        {
            _txtName = new TextBox { Location = new Point(12, 28), Size = new Size(320, 23) };
            _cmbCountry = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 68), Size = new Size(320, 23) };
            _numCost = new NumericUpDown { Location = new Point(12, 108), Size = new Size(120, 23), DecimalPlaces = 2, Maximum = 10000000, Minimum = 0 };
            _numStars = new NumericUpDown { Location = new Point(12, 148), Size = new Size(60, 23), Maximum = 5, Minimum = 1, Value = 3 };
            _chkAvailable = new CheckBox { Text = "Доступен", Location = new Point(12, 178), AutoSize = true, Checked = true };

            var btnOk = new Button { Text = "Сохранить", Location = new Point(12, 210), Size = new Size(100, 28) };
            var btnCancel = new Button { Text = "Отмена", Location = new Point(120, 210), Size = new Size(100, 28) };
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[]
            {
                new Label { Text = "Название", Location = new Point(12, 12), AutoSize = true }, _txtName,
                new Label { Text = "Страна", Location = new Point(12, 52), AutoSize = true }, _cmbCountry,
                new Label { Text = "Базовая цена", Location = new Point(12, 92), AutoSize = true }, _numCost,
                new Label { Text = "Звёзды отеля", Location = new Point(12, 132), AutoSize = true }, _numStars,
                _chkAvailable, btnOk, btnCancel
            });

            Text = _tour == null ? "Новый тур" : "Редактирование тура";
            ClientSize = new Size(350, 255);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AppTheme.Background;
        }

        private void TourEditDialog_Load(object sender, EventArgs e)
        {
            using (var db = new ApplicationDbContext())
            {
                _cmbCountry.DisplayMember = "CountryName";
                _cmbCountry.ValueMember = "Id";
                _cmbCountry.DataSource = db.Countries.OrderBy(c => c.CountryName).ToList();
            }

            if (_tour != null)
            {
                _txtName.Text = _tour.TourName;
                _cmbCountry.SelectedValue = _tour.CountryId;
                _numCost.Value = _tour.BaseCost;
                _numStars.Value = _tour.HotelStars;
                _chkAvailable.Checked = _tour.IsAvailable;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var name = _txtName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название тура.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbCountry.SelectedValue == null)
            {
                MessageBox.Show("Выберите страну.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var countryId = (int)_cmbCountry.SelectedValue;

            using (var db = new ApplicationDbContext())
            {
                if (_tour == null)
                {
                    db.Tours.Add(new Tour
                    {
                        TourName = name,
                        CountryId = countryId,
                        BaseCost = _numCost.Value,
                        HotelStars = (int)_numStars.Value,
                        IsAvailable = _chkAvailable.Checked
                    });
                }
                else
                {
                    var entity = db.Tours.Find(_tour.Id);
                    if (entity == null) return;
                    entity.TourName = name;
                    entity.CountryId = countryId;
                    entity.BaseCost = _numCost.Value;
                    entity.HotelStars = (int)_numStars.Value;
                    entity.IsAvailable = _chkAvailable.Checked;
                }
                db.SaveChanges();
            }

            DialogResult = DialogResult.OK;
        }
    }
}

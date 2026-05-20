using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class UsersManageForm : Form
    {
        private DataGridView _grid;
        private ComboBox _cmbRole;
        private CheckBox _chkActive;
        private Button _btnSave;

        public UsersManageForm()
        {
            InitializeComponent();
            Load += (_, __) => ReloadGrid();
        }

        private void InitializeComponent()
        {
            _grid = new DataGridView
            {
                Location = new Point(12, 12),
                Size = new Size(520, 240),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _grid.SelectionChanged += (_, __) => LoadSelectedUser();

            _cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 268), Size = new Size(150, 23) };
            _cmbRole.Items.AddRange(new object[] { "admin", "operator", "user" });

            _chkActive = new CheckBox { Text = "Активен", Location = new Point(180, 270), AutoSize = true };

            _btnSave = new Button { Text = "Сохранить", Location = new Point(12, 300), Size = new Size(120, 28) };
            _btnSave.Click += BtnSave_Click;

            Controls.AddRange(new Control[]
            {
                _grid,
                new Label { Text = "Роль", Location = new Point(12, 252), AutoSize = true },
                _cmbRole, _chkActive, _btnSave
            });

            Text = "Управление пользователями";
            ClientSize = new Size(550, 345);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            AppTheme.ApplyCrudForm(this, _grid);
            AppTheme.StylePrimaryButton(_btnSave);
        }

        private void ReloadGrid()
        {
            using (var db = new ApplicationDbContext())
            {
                _grid.DataSource = db.Users
                    .OrderBy(u => u.Login)
                    .Select(u => new
                    {
                        u.Id,
                        u.Login,
                        u.FullName,
                        Role = u.RoleId == RoleIds.Admin ? "admin" :
                               u.RoleId == RoleIds.Operator ? "operator" : "user",
                        u.RoleId,
                        Active = u.IsActive ? "Да" : "Нет"
                    })
                    .ToList();
            }
        }

        private void LoadSelectedUser()
        {
            if (_grid.CurrentRow == null) return;
            var roleId = Convert.ToInt32(_grid.CurrentRow.Cells["RoleId"].Value);
            _cmbRole.SelectedIndex = roleId - 1;
            _chkActive.Checked = _grid.CurrentRow.Cells["Active"].Value?.ToString() == "Да";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null) return;
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            var roleId = _cmbRole.SelectedIndex + 1;

            using (var db = new ApplicationDbContext())
            {
                var user = db.Users.Find(id);
                if (user == null) return;
                user.RoleId = roleId;
                user.IsActive = _chkActive.Checked;
                db.SaveChanges();
            }

            ReloadGrid();
            MessageBox.Show("Сохранено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

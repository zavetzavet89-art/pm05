using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _05pmuch.Data;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class LoginAttemptsForm : Form
    {
        private DataGridView _grid;

        public LoginAttemptsForm()
        {
            InitializeComponent();
            Load += (_, __) => LoadData();
        }

        private void InitializeComponent()
        {
            _grid = new DataGridView
            {
                Location = new Point(12, 12),
                Size = new Size(460, 280),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Controls.Add(_grid);
            Text = "Журнал входов";
            ClientSize = new Size(490, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            AppTheme.ApplyCrudForm(this, _grid);
        }

        private void LoadData()
        {
            using (var db = new ApplicationDbContext())
            {
                _grid.DataSource = db.LoginAttempts
                    .OrderByDescending(a => a.AttemptedAt)
                    .Take(200)
                    .Select(a => new
                    {
                        a.Login,
                        a.AttemptedAt,
                        Успешно = a.IsSuccessful ? "Да" : "Нет"
                    })
                    .ToList();
            }
        }
    }
}

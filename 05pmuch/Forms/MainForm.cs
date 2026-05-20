using System;
using System.Drawing;
using System.Windows.Forms;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class MainForm : Form
    {
        private readonly User _currentUser;
        private Label _lblWelcome;
        private FlowLayoutPanel _toolbar;
        private Button _btnCountries;
        private Button _btnTours;
        private Button _btnClients;
        private Button _btnBookings;
        private Button _btnPayments;
        private Button _btnUsers;
        private Button _btnLogs;

        public MainForm(User currentUser)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            InitializeComponent();
            Load += MainForm_Load;
        }

        private void InitializeComponent()
        {
            var header = AppTheme.CreateHeaderPanel("TravelDesk", "Панель управления туристическим агентством");

            _lblWelcome = new Label
            {
                AutoSize = false,
                Location = new Point(16, header.Height + 10),
                Size = new Size(600, 36),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AppTheme.Primary
            };

            _toolbar = new FlowLayoutPanel
            {
                Location = new Point(12, header.Height + 52),
                Size = new Size(610, 120),
                BackColor = AppTheme.Background,
                WrapContents = true
            };

            _btnCountries = CreateToolbarButton("Страны", BtnCountries_Click);
            _btnTours = CreateToolbarButton("Каталог туров", BtnTours_Click);
            _btnClients = CreateToolbarButton("Клиенты", BtnClients_Click);
            _btnBookings = CreateToolbarButton("Бронирования", BtnBookings_Click);
            _btnPayments = CreateToolbarButton("Оплаты", BtnPayments_Click);
            _btnUsers = CreateToolbarButton("Пользователи", BtnUsers_Click);
            _btnLogs = CreateToolbarButton("Журнал входов", BtnLogs_Click);

            var btnLogout = CreateToolbarButton("Выход", (_, __) => Close());
            AppTheme.StyleSecondaryButton(btnLogout);
            btnLogout.Width = 100;

            _toolbar.Controls.AddRange(new Control[]
            {
                _btnCountries, _btnTours, _btnClients, _btnBookings,
                _btnPayments, _btnUsers, _btnLogs, btnLogout
            });

            var lblHint = new Label
            {
                Text = "Выберите раздел для работы с данными",
                Location = new Point(16, header.Height + 182),
                AutoSize = true,
                ForeColor = AppTheme.TextMuted,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            };

            Controls.Add(header);
            Controls.Add(_lblWelcome);
            Controls.Add(_toolbar);
            Controls.Add(lblHint);

            BackColor = AppTheme.Background;
            Font = new Font("Segoe UI", 9f);
            Text = "TravelDesk — главная";
            ClientSize = new Size(640, header.Height + 220);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
        }

        private static Button CreateToolbarButton(string text, EventHandler click)
        {
            var btn = new Button
            {
                Text = text,
                Width = 140,
                Height = 36
            };
            AppTheme.StyleToolbarButton(btn);
            btn.Click += click;
            return btn;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var roleName = RoleIds.GetName(_currentUser.RoleId);
            PermissionManager.SetRole(roleName);
            _lblWelcome.Text = $"{_currentUser.FullName}  ·  {roleName}  ·  {_currentUser.Login}";

            var isAdmin = _currentUser.RoleId == RoleIds.Admin;
            var isOperator = _currentUser.RoleId == RoleIds.Operator;

            _btnUsers.Visible = isAdmin;
            _btnLogs.Visible = isAdmin;
            _btnClients.Visible = isAdmin || isOperator;
            _btnBookings.Visible = isAdmin || isOperator;
            _btnPayments.Visible = isAdmin || isOperator;
        }

        private void BtnCountries_Click(object sender, EventArgs e)
        {
            using (var form = new CountriesForm())
                form.ShowDialog(this);
        }

        private void BtnTours_Click(object sender, EventArgs e)
        {
            using (var form = new ToursForm())
                form.ShowDialog(this);
        }

        private void BtnClients_Click(object sender, EventArgs e)
        {
            using (var form = new ClientsForm())
                form.ShowDialog(this);
        }

        private void BtnBookings_Click(object sender, EventArgs e)
        {
            using (var form = new BookingsForm())
                form.ShowDialog(this);
        }

        private void BtnPayments_Click(object sender, EventArgs e)
        {
            using (var form = new PaymentsForm())
                form.ShowDialog(this);
        }

        private void BtnUsers_Click(object sender, EventArgs e)
        {
            using (var form = new UsersManageForm())
                form.ShowDialog(this);
        }

        private void BtnLogs_Click(object sender, EventArgs e)
        {
            using (var form = new LoginAttemptsForm())
                form.ShowDialog(this);
        }
    }
}

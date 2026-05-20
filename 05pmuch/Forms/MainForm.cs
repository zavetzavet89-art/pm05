using System;
using System.Drawing;
using System.Windows.Forms;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    /// <summary>
    /// MainDashboard — главная панель управления (прототип §1.3 роадмапа).
    /// Навигационное меню слева, рабочая область, строка состояния с данными сессии.
    /// </summary>
    public class MainForm : Form
    {
        private readonly User _currentUser;
        private Panel _pnlNav;
        private Panel _pnlContent;
        private Label _lblWelcomeTitle;
        private Label _lblWelcomeBody;
        private Label _lblModules;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusUser;
        private ToolStripStatusLabel _statusRole;
        private ToolStripStatusLabel _statusTime;
        private System.Windows.Forms.Timer _clockTimer;

        private Button _btnCountries;
        private Button _btnTours;
        private Button _btnClients;
        private Button _btnBookings;
        private Button _btnPayments;
        private Button _btnUsers;
        private Button _btnLogs;
        private Button _btnActive;

        public MainForm(User currentUser)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            InitializeComponent();
            Load += MainForm_Load;
        }

        private void InitializeComponent()
        {
            var header = AppTheme.CreateHeaderPanel(
                "Туристическое агентство",
                "Главная панель управления · TravelDesk");

            // --- Боковое навигационное меню ---
            _pnlNav = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = AppTheme.Background,
                Padding = new Padding(8, 12, 8, 12)
            };

            var lblNavTitle = new Label
            {
                Text = "НАВИГАЦИЯ",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };

            var navStack = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Background
            };

            _btnCountries = CreateNavButton("Страны", BtnCountries_Click);
            _btnTours = CreateNavButton("Каталог туров", BtnTours_Click);
            _btnClients = CreateNavButton("Клиенты", BtnClients_Click);
            _btnBookings = CreateNavButton("Бронирования", BtnBookings_Click);
            _btnPayments = CreateNavButton("Оплаты", BtnPayments_Click);
            _btnUsers = CreateNavButton("Пользователи", BtnUsers_Click);
            _btnLogs = CreateNavButton("Журнал входов", BtnLogs_Click);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 8 };
            var btnLogout = CreateNavButton("Выход из системы", (_, __) => Close());
            AppTheme.StyleLogoutNavButton(btnLogout);

            navStack.Controls.Add(btnLogout);
            navStack.Controls.Add(spacer);
            navStack.Controls.Add(_btnLogs);
            navStack.Controls.Add(_btnUsers);
            navStack.Controls.Add(_btnPayments);
            navStack.Controls.Add(_btnBookings);
            navStack.Controls.Add(_btnClients);
            navStack.Controls.Add(_btnTours);
            navStack.Controls.Add(_btnCountries);

            _pnlNav.Controls.Add(navStack);
            _pnlNav.Controls.Add(lblNavTitle);

            // --- Центральная рабочая область (прототип) ---
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Background,
                Padding = new Padding(20, 16, 20, 16)
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Card,
                Padding = new Padding(24, 20, 24, 20)
            };

            _lblWelcomeTitle = new Label
            {
                Text = "Добро пожаловать",
                AutoSize = true,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = AppTheme.Primary,
                Location = new Point(0, 0)
            };

            _lblWelcomeBody = new Label
            {
                AutoSize = false,
                Location = new Point(0, 36),
                Size = new Size(480, 56),
                Font = new Font("Segoe UI", 10f),
                ForeColor = AppTheme.TextMuted
            };

            _lblModules = new Label
            {
                AutoSize = false,
                Location = new Point(0, 100),
                Size = new Size(480, 200),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.PrimaryDark
            };

            var lblProtoNote = new Label
            {
                Text = "Рисунок 1 — прототип главного окна приложения",
                AutoSize = true,
                Location = new Point(0, 310),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 140, 130)
            };

            card.Controls.AddRange(new Control[]
            {
                _lblWelcomeTitle, _lblWelcomeBody, _lblModules, lblProtoNote
            });
            _pnlContent.Controls.Add(card);

            // --- Строка состояния (данные сессии) ---
            _statusStrip = new StatusStrip
            {
                BackColor = AppTheme.PrimaryDark,
                SizingGrip = false
            };

            _statusUser = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = AppTheme.TextOnPrimary,
                Font = new Font("Segoe UI", 8.5f)
            };

            _statusRole = new ToolStripStatusLabel
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ForeColor = AppTheme.Accent,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0)
            };

            _statusTime = new ToolStripStatusLabel
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched,
                ForeColor = Color.FromArgb(220, 210, 200),
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(8, 0, 4, 0)
            };

            _statusStrip.Items.AddRange(new ToolStripItem[]
            {
                _statusUser, _statusRole, _statusTime
            });

            Controls.Add(_pnlContent);
            Controls.Add(_pnlNav);
            Controls.Add(header);
            Controls.Add(_statusStrip);

            BackColor = AppTheme.Background;
            Font = new Font("Segoe UI", 9f);
            Text = "Туристическое агентство — главная панель";
            ClientSize = new Size(780, 520);
            MinimumSize = new Size(720, 480);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
        }

        private Button CreateNavButton(string text, EventHandler click)
        {
            var btn = new Button { Text = text };
            AppTheme.StyleNavButton(btn);
            btn.Click += (s, e) =>
            {
                SetActiveNavButton((Button)s);
                click(s, e);
            };
            return btn;
        }

        private void SetActiveNavButton(Button active)
        {
            foreach (Control c in GetNavButtons())
            {
                if (c is Button b)
                    AppTheme.StyleNavButton(b);
            }
            _btnActive = active;
            AppTheme.StyleNavButtonActive(active);
        }

        private Control[] GetNavButtons()
        {
            return new Control[]
            {
                _btnCountries, _btnTours, _btnClients, _btnBookings,
                _btnPayments, _btnUsers, _btnLogs
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var roleName = RoleIds.GetName(_currentUser.RoleId);
            PermissionManager.SetRole(roleName);

            _lblWelcomeBody.Text =
                $"{_currentUser.FullName}\r\n" +
                $"Роль: {roleName}  ·  Логин: {_currentUser.Login}";

            _lblModules.Text = BuildModulesDescription(roleName);

            UpdateStatusBar(roleName);

            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (_, __) => UpdateStatusBar(roleName);
            _clockTimer.Start();

            ApplyRoleVisibility();

            SetActiveNavButton(_btnTours);
        }

        private static string BuildModulesDescription(string roleName)
        {
            switch (roleName)
            {
                case "admin":
                    return "Доступные разделы:\r\n" +
                           "• Справочники: страны, каталог туров (фильтр по звёздности)\r\n" +
                           "• Операции: клиенты, бронирования, оплаты\r\n" +
                           "• Администрирование: пользователи, журнал входов\r\n" +
                           "• Модификация: программа лояльности и скидки";
                case "operator":
                    return "Доступные разделы:\r\n" +
                           "• Справочники: страны, каталог туров\r\n" +
                           "• Операции: регистрация клиентов, бронирования, проведение оплат\r\n" +
                           "• Расчёт итоговой стоимости со скидкой и списание баллов";
                default:
                    return "Доступные разделы (только просмотр):\r\n" +
                           "• Каталог туров и стран\r\n" +
                           "• Изменение данных отключено для роли user";
            }
        }

        private void UpdateStatusBar(string roleName)
        {
            _statusUser.Text = $"  Сессия: {_currentUser.FullName} ({_currentUser.Login})";
            _statusRole.Text = $"Роль: {roleName}";
            _statusTime.Text = DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss");
        }

        private void ApplyRoleVisibility()
        {
            var isAdmin = _currentUser.RoleId == RoleIds.Admin;
            var isOperator = _currentUser.RoleId == RoleIds.Operator;

            _btnUsers.Visible = isAdmin;
            _btnLogs.Visible = isAdmin;
            _btnClients.Visible = isAdmin || isOperator;
            _btnBookings.Visible = isAdmin || isOperator;
            _btnPayments.Visible = isAdmin || isOperator;
            _btnCountries.Visible = true;
            _btnTours.Visible = true;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            base.OnFormClosed(e);
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

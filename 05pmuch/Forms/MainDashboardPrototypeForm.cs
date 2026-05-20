using System.Drawing;
using System.Windows.Forms;

namespace _05pmuch.Forms
{
    /// <summary>
    /// Заготовка главного окна для отчёта (рис. 1, §1.3).
    /// Статический макет без логики и без открытия разделов.
    /// </summary>
    public class MainDashboardPrototypeForm : Form
    {
        public MainDashboardPrototypeForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var wire = Color.FromArgb(180, 180, 180);
            var fill = Color.FromArgb(245, 245, 245);
            var text = Color.FromArgb(60, 60, 60);

            // --- Шапка (макет) ---
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "MainDashboard — главная панель управления  [ПРОТОТИП]",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = text,
                AutoSize = true,
                Location = new Point(12, 8)
            });
            pnlHeader.Controls.Add(new Label
            {
                Text = "Туристическое агентство",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(14, 30)
            });

            // --- Меню слева (макет) ---
            var grpMenu = new GroupBox
            {
                Text = " Навигационное меню ",
                Location = new Point(12, 64),
                Size = new Size(200, 320),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = text
            };

            var menuItems = new[]
            {
                "▢  Страны",
                "▢  Каталог туров",
                "▢  Клиенты",
                "▢  Бронирования",
                "▢  Оплаты",
                "▢  Пользователи (admin)",
                "▢  Журнал входов (admin)",
                "▢  Выход"
            };

            var y = 24;
            foreach (var item in menuItems)
            {
                grpMenu.Controls.Add(new Label
                {
                    Text = item,
                    Location = new Point(12, y),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = text,
                    BackColor = fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    Padding = new Padding(4, 2, 4, 2)
                });
                y += 32;
            }

            // --- Рабочая область (макет) ---
            var grpWork = new GroupBox
            {
                Text = " Рабочая область (содержимое раздела) ",
                Location = new Point(224, 64),
                Size = new Size(520, 320),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = text
            };

            grpWork.Controls.Add(new Label
            {
                Text = "Здесь отображается форма выбранного раздела:\r\n" +
                       "DataGridView, фильтры, кнопки CRUD…",
                Location = new Point(16, 28),
                Size = new Size(480, 48),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.Gray
            });

            var fakeGrid = new DataGridView
            {
                Location = new Point(16, 84),
                Size = new Size(488, 180),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                Enabled = false
            };
            fakeGrid.Columns.Add("col1", "Название");
            fakeGrid.Columns.Add("col2", "Страна");
            fakeGrid.Columns.Add("col3", "Цена");
            fakeGrid.Rows.Add("Золотые пески", "Греция", "72 000");
            fakeGrid.Rows.Add("Крит, all inclusive", "Греция", "95 000");
            fakeGrid.Rows.Add("…", "…", "…");
            grpWork.Controls.Add(fakeGrid);

            grpWork.Controls.Add(new Label
            {
                Text = "[ Добавить ]  [ Изменить ]  [ Удалить ]  [ Поиск: ________ ]",
                Location = new Point(16, 272),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = text
            });

            // --- Строка состояния (макет) ---
            var pnlStatus = new Panel
            {
                Location = new Point(12, 392),
                Size = new Size(732, 28),
                BackColor = Color.FromArgb(230, 230, 230),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlStatus.Controls.Add(new Label
            {
                Text = "Строка состояния:  Иванов И.И.  |  operator  |  agent_ivanov  |  20.05.2026  17:30:00",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = text,
                Padding = new Padding(8, 0, 0, 0)
            });

            var lblCaption = new Label
            {
                Text = "Рисунок 1 — Прототип главного окна приложения",
                Location = new Point(12, 428),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            var btnClose = new Button
            {
                Text = "Закрыть",
                Location = new Point(660, 424),
                Size = new Size(84, 28)
            };
            btnClose.Click += (_, __) => Close();

            Controls.AddRange(new Control[]
            {
                pnlHeader, grpMenu, grpWork, pnlStatus, lblCaption, btnClose
            });

            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            Text = "Прототип — MainDashboard (для отчёта)";
            ClientSize = new Size(760, 470);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
        }
    }
}

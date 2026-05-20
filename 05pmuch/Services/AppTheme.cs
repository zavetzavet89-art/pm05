using System.Drawing;
using System.Windows.Forms;

namespace _05pmuch.Services
{
    public static class AppTheme
    {
        public static readonly Color Primary = Color.FromArgb(107, 39, 55);
        public static readonly Color PrimaryDark = Color.FromArgb(74, 26, 38);
        public static readonly Color Accent = Color.FromArgb(201, 162, 39);
        public static readonly Color Background = Color.FromArgb(250, 244, 235);
        public static readonly Color Card = Color.White;
        public static readonly Color TextOnPrimary = Color.White;
        public static readonly Color TextMuted = Color.FromArgb(110, 90, 80);
        public static readonly Color HeaderStrip = Color.FromArgb(45, 80, 22);

        public static Font TitleFont => new Font("Segoe UI", 14f, FontStyle.Bold);
        public static Font SubtitleFont => new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font MenuFont => new Font("Segoe UI", 9.5f, FontStyle.Regular);

        public static void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Primary;
            button.ForeColor = TextOnPrimary;
            button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleAccentButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Accent;
            button.ForeColor = PrimaryDark;
            button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Primary;
            button.BackColor = Card;
            button.ForeColor = Primary;
            button.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleToolbarButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.FromArgb(255, 252, 247);
            button.ForeColor = Primary;
            button.Font = MenuFont;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.Margin = new Padding(4, 0, 4, 0);
        }

        public static void StyleDataGridView(DataGridView grid)
        {
            grid.BackgroundColor = Card;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryDark;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextOnPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4);
            grid.ColumnHeadersHeight = 34;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 210, 180);
            grid.DefaultCellStyle.SelectionForeColor = PrimaryDark;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 248, 242);
            grid.GridColor = Color.FromArgb(220, 200, 180);
            grid.RowTemplate.Height = 30;
        }

        public static void ApplyCrudForm(Form form, DataGridView grid)
        {
            form.BackColor = Background;
            form.Font = new Font("Segoe UI", 9f);
            if (grid != null)
                StyleDataGridView(grid);

            foreach (Control control in form.Controls)
            {
                if (control is Button btn && btn.Text != null)
                {
                    if (btn.Text.Contains("Удалить"))
                        continue;
                    if (btn.Text.Contains("Добавить") || btn.Text.Contains("Оформить") || btn.Text.Contains("Создать") || btn.Text.Contains("Списать"))
                        StylePrimaryButton(btn);
                    else if (btn.Text.Contains("Изменить") || btn.Text.Contains("Сохранить"))
                        StyleSecondaryButton(btn);
                }
            }
        }

        public static Panel CreateHeaderPanel(string title, string subtitle = null)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = subtitle == null ? 56 : 68,
                BackColor = Primary
            };

            panel.Controls.Add(new Panel
            {
                Dock = DockStyle.Left,
                Width = 6,
                BackColor = Accent
            });

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = TextOnPrimary,
                Font = TitleFont,
                AutoSize = true,
                Location = new Point(18, subtitle == null ? 16 : 12)
            };
            panel.Controls.Add(lblTitle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                panel.Controls.Add(new Label
                {
                    Text = subtitle,
                    ForeColor = Color.FromArgb(230, 210, 200),
                    Font = SubtitleFont,
                    AutoSize = true,
                    Location = new Point(20, 38)
                });
            }

            return panel;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using _05pmuch.Services;

namespace _05pmuch.Forms
{
    public class RegisterForm : Form
    {
        private const int MinPasswordLength = 6;

        private readonly AuthService _auth = new AuthService();
        private TextBox _txtFullName;
        private TextBox _txtLogin;
        private TextBox _txtPassword;
        private TextBox _txtConfirm;

        public RegisterForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var header = AppTheme.CreateHeaderPanel("Регистрация", "Новая учётная запись сотрудника");

            var body = new Panel
            {
                Location = new Point(12, header.Height + 8),
                Size = new Size(316, 230),
                BackColor = AppTheme.Card
            };

            var lblFullName = new Label { Text = "ФИО", Location = new Point(16, 12), AutoSize = true, ForeColor = AppTheme.TextMuted };
            _txtFullName = new TextBox { Location = new Point(16, 28), Size = new Size(284, 24), BorderStyle = BorderStyle.FixedSingle };

            var lblLogin = new Label { Text = "Логин", Location = new Point(16, 58), AutoSize = true, ForeColor = AppTheme.TextMuted };
            _txtLogin = new TextBox { Location = new Point(16, 74), Size = new Size(284, 24), BorderStyle = BorderStyle.FixedSingle };

            var lblPassword = new Label
            {
                Text = $"Пароль (мин. {MinPasswordLength} символов)",
                Location = new Point(16, 104),
                AutoSize = true,
                ForeColor = AppTheme.TextMuted
            };
            _txtPassword = new TextBox
            {
                Location = new Point(16, 120),
                Size = new Size(284, 24),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            var lblConfirm = new Label { Text = "Подтвердите пароль", Location = new Point(16, 150), AutoSize = true, ForeColor = AppTheme.TextMuted };
            _txtConfirm = new TextBox
            {
                Location = new Point(16, 166),
                Size = new Size(284, 24),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            var btnRegister = new Button { Text = "Зарегистрироваться", Location = new Point(16, 200), Size = new Size(140, 32) };
            AppTheme.StylePrimaryButton(btnRegister);
            btnRegister.Click += BtnRegister_Click;

            var btnBack = new Button { Text = "К входу", Location = new Point(164, 200), Size = new Size(136, 32) };
            AppTheme.StyleSecondaryButton(btnBack);
            btnBack.Click += (_, __) => Close();

            body.Controls.AddRange(new Control[]
            {
                lblFullName, _txtFullName, lblLogin, _txtLogin,
                lblPassword, _txtPassword, lblConfirm, _txtConfirm,
                btnRegister, btnBack
            });

            Controls.Add(header);
            Controls.Add(body);

            BackColor = AppTheme.Background;
            Font = new Font("Segoe UI", 9f);
            Text = "Регистрация";
            ClientSize = new Size(340, header.Height + 250);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            var fullName = _txtFullName.Text?.Trim() ?? "";
            var login = _txtLogin.Text?.Trim() ?? "";
            var password = _txtPassword.Text ?? "";
            var confirm = _txtConfirm.Text ?? "";

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Заполните ФИО и логин.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password.Length < MinPasswordLength)
            {
                MessageBox.Show($"Пароль должен быть не короче {MinPasswordLength} символов.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_auth.Register(login, password, fullName, RoleIds.User))
            {
                MessageBox.Show("Такой логин уже занят.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Регистрация успешна. Теперь можно войти.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

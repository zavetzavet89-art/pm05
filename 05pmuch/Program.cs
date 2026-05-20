using System;
using System.Windows.Forms;
using _05pmuch.Data;

namespace _05pmuch
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);

            try
            {
                DatabaseBootstrap.EnsureReady();
            }
            catch (Exception ex)
            {
                var details = DatabaseDiagnostics.FormatException(ex);
                var report = DatabaseDiagnostics.RunFullReportSafe();
                DatabaseDiagnostics.SaveReportToFile(report + Environment.NewLine + details);
                MessageBox.Show(
                    "Не удалось инициализировать базу данных.\n\n" + details +
                    "\n\nПодробный отчёт: bin\\Debug\\db-diagnostic.log",
                    "Ошибка БД",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

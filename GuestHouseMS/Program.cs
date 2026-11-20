using GuestHouseMS.Controllers;
using GuestHouseMS.Data;
using GuestHouseMS.Views;

namespace GuestHouseMS;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Db.Init();   // creates guesthouse.db + tables + demo data on first run

        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK) break;

            using var main = new MainForm();
            Application.Run(main);
            AuthController.Logout();
            if (!main.LoggedOut) break;   // window closed with X -> exit app
        }
    }
}

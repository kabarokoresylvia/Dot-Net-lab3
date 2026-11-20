using GuestHouseMS.Controllers;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public class MainForm : Form
{
    public bool LoggedOut { get; private set; }

    readonly IdleFilter idle = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 30_000 };
    const int TimeoutMinutes = 15;

    public MainForm()
    {
        var user = AuthController.Current!;
        Text = "Sunrise Guest House";
        Size = new Size(1150, 720);
        StartPosition = FormStartPosition.CenterScreen;

        var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Ui.Green };
        var who = new Label
        {
            Text = $"Sunrise Guest House   |   {user.FullName} ({user.Role})",
            ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold),
            AutoSize = true, Location = new Point(12, 12)
        };
        var logout = new Button { Text = "Log out", Dock = DockStyle.Right, Width = 90, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        logout.Click += (_, _) => { LoggedOut = true; Close(); };
        var pw = new Button { Text = "Change password", Dock = DockStyle.Right, Width = 130, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        pw.Click += (_, _) => ChangePassword();
        top.Controls.AddRange(new Control[] { who, pw, logout });

        var tabs = new TabControl { Dock = DockStyle.Fill };
        void Add(string title, Control c)
        {
            c.Dock = DockStyle.Fill;
            var page = new TabPage(title);
            page.Controls.Add(c);
            tabs.TabPages.Add(page);
        }

        // Role-based access
        if (user.Role is Roles.Admin or Roles.Receptionist)
        {
            Add("Rooms", new RoomsView());
            Add("Guests", new GuestsView());
            Add("Reservations", new ReservationsView());
            Add("Billing", new BillingView());
        }
        if (user.Role is Roles.Admin or Roles.Housekeeping) Add("Housekeeping", new HousekeepingView());
        if (user.Role is Roles.Admin or Roles.Receptionist) Add("Reports", new ReportsView());
        if (user.Role == Roles.Admin) Add("Users", new UsersView());

        tabs.SelectedIndexChanged += (_, _) =>
            (tabs.SelectedTab?.Controls.Count > 0 ? tabs.SelectedTab.Controls[0] as IRefreshable : null)?.Reload();

        Controls.Add(tabs);   // fill control first, then the top bar
        Controls.Add(top);

        // Session timeout after 15 minutes of inactivity
        Application.AddMessageFilter(idle);
        timer.Tick += (_, _) =>
        {
            if ((DateTime.Now - idle.Last).TotalMinutes >= TimeoutMinutes)
            {
                timer.Stop();
                Ui.Info("Your session timed out because of inactivity.");
                LoggedOut = true;
                Close();
            }
        };
        timer.Start();
    }

    void ChangePassword()
    {
        var old = Ui.Ask("Change password", "Current password:");
        if (old == null) return;
        var neu = Ui.Ask("Change password", "New password (min 6 characters):");
        if (neu == null) return;
        Ui.Show(AuthController.ChangePassword(old, neu));
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        Application.RemoveMessageFilter(idle);
        base.OnFormClosed(e);
    }

    sealed class IdleFilter : IMessageFilter
    {
        public DateTime Last = DateTime.Now;
        public bool PreFilterMessage(ref Message m)
        {
            // mouse move, mouse down, key down
            if (m.Msg is 0x0200 or 0x0201 or 0x0100) Last = DateTime.Now;
            return false;
        }
    }
}

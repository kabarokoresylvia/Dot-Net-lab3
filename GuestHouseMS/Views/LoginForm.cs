using GuestHouseMS.Controllers;

namespace GuestHouseMS.Views;

public class LoginForm : Form
{
    readonly TextBox user = new() { Location = new Point(110, 70), Width = 200 };
    readonly TextBox pass = new() { Location = new Point(110, 105), Width = 200, UseSystemPasswordChar = true };
    int attempts;

    public LoginForm()
    {
        Text = "Sunrise Guest House - Login";
        ClientSize = new Size(340, 200);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var title = new Label
        {
            Text = "Guest House Management System",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Ui.Green, AutoSize = true, Location = new Point(20, 20)
        };
        var login = new Button { Text = "Log in", Location = new Point(110, 145), Width = 100 };
        login.Click += (_, _) => DoLogin();
        Controls.AddRange(new Control[]
        {
            title,
            new Label { Text = "Username", Location = new Point(20, 73), AutoSize = true }, user,
            new Label { Text = "Password", Location = new Point(20, 108), AutoSize = true }, pass,
            login
        });
        AcceptButton = login;
    }

    void DoLogin()
    {
        var u = AuthController.Login(user.Text, pass.Text);
        if (u == null)
        {
            attempts++;
            if (attempts >= 5) { Ui.Err("Too many failed attempts. The application will close."); DialogResult = DialogResult.Cancel; return; }
            Ui.Err("Invalid username or password.");
            pass.Clear(); pass.Focus();
            return;
        }
        DialogResult = DialogResult.OK;
    }
}

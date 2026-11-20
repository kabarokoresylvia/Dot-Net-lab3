using GuestHouseMS.Controllers;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public class UsersView : UserControl, IRefreshable
{
    readonly DataGridView grid = Ui.Grid();
    readonly TextBox username = Ui.Txt(110), fullName = Ui.Txt(150), password = Ui.Txt(110);
    readonly ComboBox role = Ui.Combo(Roles.All, 120);

    public UsersView()
    {
        password.UseSystemPasswordChar = true;

        var bar = Ui.Bar();
        bar.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Username"), username, Ui.Lbl("Full name"), fullName, Ui.Lbl("Role"), role,
            Ui.Lbl("Password"), password,
            Ui.Btn("Add user", (_, _) => Do(UserController.Add(username.Text, fullName.Text, role.Text, password.Text))),
            Ui.Btn("Save name / role", (_, _) => Do(UserController.Update(Ui.SelId(grid), fullName.Text, role.Text))),
            Ui.Btn("Reset password", (_, _) =>
            {
                int id = Ui.SelId(grid);
                if (id == 0) { Ui.Err("Select a user first."); return; }
                Do(UserController.ResetPassword(id, password.Text));
            })
        });

        var bar2 = Ui.Bar();
        bar2.Controls.AddRange(new Control[]
        {
            Ui.Btn("Activate", (_, _) => Do(UserController.SetActive(Ui.SelId(grid), true))),
            Ui.Btn("Deactivate", (_, _) => Do(UserController.SetActive(Ui.SelId(grid), false))),
            Ui.Btn("Delete user", (_, _) =>
            {
                int id = Ui.SelId(grid);
                if (id == 0) { Ui.Err("Select a user first."); return; }
                if (Ui.Confirm("Delete this user permanently?")) Do(UserController.Delete(id));
            })
        });

        Controls.Add(grid);
        Controls.Add(bar2);
        Controls.Add(bar);
        grid.SelectionChanged += (_, _) =>
        {
            if (grid.CurrentRow == null) return;
            fullName.Text = Ui.Cell(grid, "FullName");
            role.Text = Ui.Cell(grid, "Role");
        };
        Reload();
    }

    void Do(Models.Result r)
    {
        Ui.Show(r);
        if (r.Ok) { password.Clear(); Reload(); }
    }

    public void Reload() => grid.DataSource = UserController.List();
}

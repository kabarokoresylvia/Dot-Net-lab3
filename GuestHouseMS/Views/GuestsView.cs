using GuestHouseMS.Controllers;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public class GuestsView : UserControl, IRefreshable
{
    readonly DataGridView grid = Ui.Grid();
    readonly DataGridView hist = Ui.Grid();
    readonly TextBox search = Ui.Txt(170);
    readonly TextBox name = Ui.Txt(140), idn = Ui.Txt(110), phone = Ui.Txt(100);
    readonly TextBox email = Ui.Txt(150), addr = Ui.Txt(150), nat = Ui.Txt(90);

    public GuestsView()
    {
        var searchBar = Ui.Bar();
        searchBar.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Search (name, ID or phone)"), search,
            Ui.Btn("Find", (_, _) => Reload()),
            Ui.Btn("Clear search", (_, _) => { search.Clear(); Reload(); })
        });

        var editBar = Ui.Bar();
        editBar.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Name"), name, Ui.Lbl("ID/Passport"), idn, Ui.Lbl("Phone"), phone,
            Ui.Lbl("Email"), email, Ui.Lbl("Address"), addr, Ui.Lbl("Nationality"), nat,
            Ui.Btn("Add", (_, _) => Do(GuestController.Add(Read()))),
            Ui.Btn("Update", (_, _) => Do(GuestController.Update(Read()))),
            Ui.Btn("Delete", (_, _) =>
            {
                int id = Ui.SelId(grid);
                if (id == 0) { Ui.Err("Select a guest first."); return; }
                if (Ui.Confirm("Delete this guest?")) Do(GuestController.Delete(id));
            }),
            Ui.Btn("Clear form", (_, _) => ClearForm())
        });

        var box = new GroupBox { Text = "Stay history of the selected guest", Dock = DockStyle.Bottom, Height = 170 };
        box.Controls.Add(hist);

        Controls.Add(grid);
        Controls.Add(box);
        Controls.Add(editBar);
        Controls.Add(searchBar);

        grid.SelectionChanged += (_, _) => OnSelect();
        Reload();
    }

    Guest Read() => new()
    {
        Id = Ui.SelId(grid), FullName = name.Text, IdNumber = idn.Text, Phone = phone.Text,
        Email = email.Text, Address = addr.Text, Nationality = nat.Text
    };

    void Do(Models.Result r) { Ui.Show(r); if (r.Ok) { ClearForm(); Reload(); } }

    public void Reload() => grid.DataSource = GuestController.List(search.Text);

    void ClearForm()
    {
        foreach (var t in new[] { name, idn, phone, email, addr, nat }) t.Clear();
    }

    void OnSelect()
    {
        int id = Ui.SelId(grid);
        if (id == 0) { hist.DataSource = null; return; }
        name.Text = Ui.Cell(grid, "Name");
        idn.Text = Ui.Cell(grid, "ID / Passport");
        phone.Text = Ui.Cell(grid, "Phone");
        email.Text = Ui.Cell(grid, "Email");
        addr.Text = Ui.Cell(grid, "Address");
        nat.Text = Ui.Cell(grid, "Nationality");
        hist.DataSource = GuestController.History(id);
    }
}

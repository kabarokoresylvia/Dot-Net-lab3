using GuestHouseMS.Controllers;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public class RoomsView : UserControl, IRefreshable
{
    readonly DataGridView grid = Ui.Grid();
    readonly TextBox number = Ui.Txt(70);
    readonly ComboBox type = Ui.Combo(RoomStatus.Types, 90);
    readonly NumericUpDown cap = Ui.Num(1, 20, 2, 0, 55);
    readonly NumericUpDown price = Ui.Num(1, 100000, 50, 2, 90);
    readonly ComboBox status = Ui.Combo(RoomStatus.All, 120);
    readonly DateTimePicker dFrom = Ui.Date();
    readonly DateTimePicker dTo = Ui.Date(DateTime.Today.AddDays(1));
    readonly NumericUpDown guests = Ui.Num(1, 20, 1, 0, 55);

    public RoomsView()
    {
        bool admin = AuthController.Current!.Role == Roles.Admin;

        var search = Ui.Bar();
        search.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Check availability - From"), dFrom, Ui.Lbl("To"), dTo, Ui.Lbl("Guests"), guests,
            Ui.Btn("Search", (_, _) => Search()),
            Ui.Btn("Show all rooms", (_, _) => Reload())
        });

        var edit = Ui.Bar();
        edit.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Number"), number, Ui.Lbl("Type"), type, Ui.Lbl("Capacity"), cap,
            Ui.Lbl("Price/night"), price, Ui.Lbl("Status"), status,
            Ui.Btn("Add", (_, _) => Do(RoomController.Add(Read()))),
            Ui.Btn("Update", (_, _) => Do(RoomController.Update(Read()))),
            Ui.Btn("Delete", (_, _) =>
            {
                int id = Ui.SelId(grid);
                if (id == 0) { Ui.Err("Select a room first."); return; }
                if (Ui.Confirm("Delete this room?")) Do(RoomController.Delete(id));
            })
        });

        Controls.Add(grid);               // fill control first
        if (admin) Controls.Add(edit);    // only Admin can add/edit/delete rooms
        Controls.Add(search);

        grid.SelectionChanged += (_, _) => FillFields();
        Reload();
    }

    Room Read() => new()
    {
        Id = Ui.SelId(grid), Number = number.Text, Type = type.Text,
        Capacity = (int)cap.Value, Price = price.Value, Status = status.Text
    };

    void Do(Models.Result r) { Ui.Show(r); if (r.Ok) Reload(); }

    public void Reload() => grid.DataSource = RoomController.List();

    void Search()
    {
        if (dTo.Value.Date <= dFrom.Value.Date) { Ui.Err("The 'To' date must be after the 'From' date."); return; }
        grid.DataSource = RoomController.Available(dFrom.Value, dTo.Value, (int)guests.Value);
    }

    void FillFields()
    {
        if (grid.CurrentRow == null || !grid.Columns.Contains("Number")) return;
        number.Text = Ui.Cell(grid, "Number");
        type.Text = Ui.Cell(grid, "Type");
        status.Text = Ui.Cell(grid, "Status");
        if (int.TryParse(Ui.Cell(grid, "Capacity"), out var c)) cap.Value = Math.Clamp(c, 1, 20);
        if (decimal.TryParse(Ui.Cell(grid, "Price"), out var p)) price.Value = Math.Clamp(p, 1m, 100000m);
    }
}

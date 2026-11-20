using System.Data;
using GuestHouseMS.Controllers;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public class ReservationsView : UserControl, IRefreshable
{
    readonly DataGridView grid = Ui.Grid();
    readonly ComboBox guest = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, Margin = new Padding(2, 6, 2, 2) };
    readonly ComboBox room = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, Margin = new Padding(2, 6, 2, 2) };
    readonly DateTimePicker dIn = Ui.Date();
    readonly DateTimePicker dOut = Ui.Date(DateTime.Today.AddDays(1));
    readonly DateTimePicker newOut = Ui.Date(DateTime.Today.AddDays(2));
    readonly NumericUpDown guests = Ui.Num(1, 20, 1, 0, 55);
    bool loading;

    public ReservationsView()
    {
        var book = Ui.Bar();
        book.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Guest"), guest, Ui.Lbl("Check-in"), dIn, Ui.Lbl("Check-out"), dOut,
            Ui.Lbl("Guests"), guests, Ui.Lbl("Available room"), room,
            Ui.Btn("Book", (_, _) => Book())
        });

        var actions = Ui.Bar();
        actions.Controls.AddRange(new Control[]
        {
            Ui.Btn("Check in", (_, _) => Act(ReservationController.CheckIn(Ui.SelId(grid)))),
            Ui.Btn("Check out", (_, _) => Act(ReservationController.CheckOut(Ui.SelId(grid)))),
            Ui.Btn("Cancel booking", (_, _) =>
            {
                if (Ui.Confirm("Cancel the selected reservation?")) Act(ReservationController.Cancel(Ui.SelId(grid)));
            }),
            Ui.Lbl("New check-out date"), newOut,
            Ui.Btn("Extend / shorten stay", (_, _) => Act(ReservationController.ChangeCheckOut(Ui.SelId(grid), newOut.Value))),
            Ui.Btn("Refresh", (_, _) => Reload())
        });

        Controls.Add(grid);
        Controls.Add(actions);
        Controls.Add(book);

        dIn.ValueChanged += (_, _) => LoadRooms();
        dOut.ValueChanged += (_, _) => LoadRooms();
        guests.ValueChanged += (_, _) => LoadRooms();
        Reload();
    }

    public void Reload()
    {
        loading = true;
        Ui.Bind(guest, GuestController.Lookup());
        loading = false;
        LoadRooms();
        grid.DataSource = ReservationController.List();
    }

    void LoadRooms()
    {
        if (loading) return;
        if (dOut.Value.Date <= dIn.Value.Date) { room.DataSource = null; return; }
        var dt = RoomController.Available(dIn.Value, dOut.Value, (int)guests.Value);
        dt.Columns.Add("Label", typeof(string));
        foreach (DataRow r in dt.Rows)
            r["Label"] = $"{r["Number"]} - {r["Type"]} - {Convert.ToDecimal(r["Price"]):N2}/night";
        Ui.Bind(room, dt);
    }

    void Book()
    {
        var r = ReservationController.Create(Ui.SelValue(guest), Ui.SelValue(room), dIn.Value, dOut.Value, (int)guests.Value);
        Act(r);
    }

    void Act(Models.Result r)
    {
        Ui.Show(r);
        Reload();
    }
}

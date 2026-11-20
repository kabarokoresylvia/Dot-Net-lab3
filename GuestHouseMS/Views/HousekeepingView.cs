using GuestHouseMS.Controllers;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public class HousekeepingView : UserControl, IRefreshable
{
    readonly DataGridView rooms = Ui.Grid();
    readonly DataGridView issues = Ui.Grid();
    readonly TextBox issue = Ui.Txt(300);

    public HousekeepingView()
    {
        var clean = Ui.Bar();
        clean.Controls.AddRange(new Control[]
        {
            Ui.Btn("Mark room clean (Available)", (_, _) => MarkClean()),
            Ui.Btn("Mark as Needs Cleaning", (_, _) => MarkDirty()),
            Ui.Btn("Refresh", (_, _) => Reload())
        });

        var log = Ui.Bar();
        log.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Maintenance issue for the selected room"), issue,
            Ui.Btn("Log issue", (_, _) => LogIssue()),
            Ui.Btn("Mark selected issue resolved", (_, _) => Resolve())
        });

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        tbl.Controls.Add(rooms, 0, 0);
        tbl.Controls.Add(issues, 0, 1);

        Controls.Add(tbl);
        Controls.Add(log);
        Controls.Add(clean);
        Reload();
    }

    public void Reload()
    {
        rooms.DataSource = RoomController.List();
        issues.DataSource = RoomController.OpenIssues();
    }

    void MarkClean()
    {
        int id = Ui.SelId(rooms);
        if (id == 0) { Ui.Err("Select a room first."); return; }
        if (Ui.Cell(rooms, "Status") != RoomStatus.NeedsCleaning) { Ui.Err("That room is not waiting for cleaning."); return; }
        RoomController.SetStatus(id, RoomStatus.Available);
        Reload();
    }

    void MarkDirty()
    {
        int id = Ui.SelId(rooms);
        if (id == 0) { Ui.Err("Select a room first."); return; }
        if (Ui.Cell(rooms, "Status") != RoomStatus.Available) { Ui.Err("Only an Available room can be marked as Needs Cleaning."); return; }
        RoomController.SetStatus(id, RoomStatus.NeedsCleaning);
        Reload();
    }

    void LogIssue()
    {
        int id = Ui.SelId(rooms);
        if (id == 0) { Ui.Err("Select a room first."); return; }
        var r = RoomController.LogIssue(id, issue.Text);
        Ui.Show(r);
        if (r.Ok) { issue.Clear(); Reload(); }
    }

    void Resolve()
    {
        int id = Ui.SelId(issues);
        if (id == 0) { Ui.Err("Select an open issue first."); return; }
        var r = RoomController.ResolveIssue(id);
        Ui.Show(r);
        Reload();
    }
}

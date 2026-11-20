using System.Data;
using GuestHouseMS.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Controllers;

public static class RoomController
{
    public static DataTable List() => Db.Table(
        "SELECT Id, Number, Type, Capacity, Price, Status FROM Rooms ORDER BY Number");

    /// <summary>Rooms that are not under maintenance and have no overlapping active reservation.</summary>
    public static DataTable Available(DateTime from, DateTime to, int guests, int excludeReservationId = 0) => Db.Table(@"
SELECT r.Id, r.Number, r.Type, r.Capacity, r.Price, r.Status
FROM Rooms r
WHERE r.Status <> 'Maintenance' AND r.Capacity >= @g
  AND NOT EXISTS (SELECT 1 FROM Reservations x
                  WHERE x.RoomId = r.Id AND x.Status IN ('Reserved','Checked In')
                    AND x.CheckIn < @out AND x.CheckOut > @in AND x.Id <> @ex)
ORDER BY r.Number",
        ("@g", guests), ("@in", from.ToString("yyyy-MM-dd")), ("@out", to.ToString("yyyy-MM-dd")), ("@ex", excludeReservationId));

    static Result? Validate(Room r)
    {
        if (r.Number.Trim().Length == 0) return new(false, "Room number is required.");
        if (r.Capacity < 1) return new(false, "Capacity must be at least 1.");
        if (r.Price <= 0) return new(false, "Price per night must be greater than zero.");
        return null;
    }

    public static Result Add(Room r)
    {
        var bad = Validate(r); if (bad != null) return bad;
        try
        {
            Db.Exec("INSERT INTO Rooms(Number,Type,Capacity,Price,Status) VALUES(@n,@t,@c,@p,@s)",
                ("@n", r.Number.Trim()), ("@t", r.Type), ("@c", r.Capacity), ("@p", r.Price), ("@s", r.Status));
            return new(true, $"Room {r.Number} added.");
        }
        catch (Exception e) when (Db.IsConstraint(e)) { return new(false, "That room number already exists."); }
    }

    public static Result Update(Room r)
    {
        if (r.Id == 0) return new(false, "Select a room first.");
        var bad = Validate(r); if (bad != null) return bad;
        try
        {
            Db.Exec("UPDATE Rooms SET Number=@n, Type=@t, Capacity=@c, Price=@p, Status=@s WHERE Id=@i",
                ("@n", r.Number.Trim()), ("@t", r.Type), ("@c", r.Capacity), ("@p", r.Price), ("@s", r.Status), ("@i", r.Id));
            return new(true, "Room updated.");
        }
        catch (Exception e) when (Db.IsConstraint(e)) { return new(false, "That room number already exists."); }
    }

    public static Result Delete(int id)
    {
        if (Db.Count("SELECT COUNT(*) FROM Reservations WHERE RoomId=@i", ("@i", id)) > 0)
            return new(false, "This room has reservations and cannot be deleted. Set it to Maintenance instead.");
        Db.Exec("DELETE FROM Maintenance WHERE RoomId=@i", ("@i", id));
        Db.Exec("DELETE FROM Rooms WHERE Id=@i", ("@i", id));
        return new(true, "Room deleted.");
    }

    public static void SetStatus(int id, string status) =>
        Db.Exec("UPDATE Rooms SET Status=@s WHERE Id=@i", ("@s", status), ("@i", id));

    // ---- maintenance log ----
    public static DataTable OpenIssues() => Db.Table(@"
SELECT m.Id, r.Number AS Room, m.Issue, m.ReportedOn AS Reported
FROM Maintenance m JOIN Rooms r ON r.Id = m.RoomId WHERE m.Resolved = 0 ORDER BY m.Id DESC");

    public static Result LogIssue(int roomId, string issue)
    {
        if (issue.Trim().Length == 0) return new(false, "Describe the issue.");
        Db.Exec("INSERT INTO Maintenance(RoomId,Issue,ReportedOn,Resolved) VALUES(@r,@i,@d,0)",
            ("@r", roomId), ("@i", issue.Trim()), ("@d", DateTime.Today.ToString("yyyy-MM-dd")));
        SetStatus(roomId, RoomStatus.Maintenance);
        return new(true, "Issue logged. Room set to Maintenance.");
    }

    public static Result ResolveIssue(int issueId)
    {
        var roomId = Db.Scalar("SELECT RoomId FROM Maintenance WHERE Id=@i", ("@i", issueId));
        if (roomId == null) return new(false, "Issue not found.");
        Db.Exec("UPDATE Maintenance SET Resolved=1 WHERE Id=@i", ("@i", issueId));
        int rid = Convert.ToInt32(roomId);
        if (Db.Count("SELECT COUNT(*) FROM Maintenance WHERE RoomId=@r AND Resolved=0", ("@r", rid)) == 0)
            SetStatus(rid, RoomStatus.NeedsCleaning);
        return new(true, "Issue resolved. Room sent to housekeeping.");
    }
}

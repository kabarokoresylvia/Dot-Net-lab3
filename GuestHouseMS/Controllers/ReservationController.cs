using System.Data;
using GuestHouseMS.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Controllers;

public static class ReservationController
{
    const string Fmt = "yyyy-MM-dd";

    public static DataTable List() => Db.Table(@"
SELECT r.Id, g.FullName AS Guest, rm.Number AS Room, r.CheckIn, r.CheckOut, r.Status
FROM Reservations r
JOIN Guests g ON g.Id = r.GuestId
JOIN Rooms rm ON rm.Id = r.RoomId
ORDER BY r.Id DESC");

    static bool Overlaps(int roomId, DateTime from, DateTime to, int excludeId) => Db.Count(@"
SELECT COUNT(*) FROM Reservations
WHERE RoomId=@r AND Status IN ('Reserved','Checked In') AND CheckIn < @out AND CheckOut > @in AND Id <> @ex",
        ("@r", roomId), ("@in", from.ToString(Fmt)), ("@out", to.ToString(Fmt)), ("@ex", excludeId)) > 0;

    public static Result Create(int guestId, int roomId, DateTime checkIn, DateTime checkOut, int guests)
    {
        checkIn = checkIn.Date; checkOut = checkOut.Date;
        if (guestId == 0) return new(false, "Choose a guest (register the guest first if new).");
        if (roomId == 0) return new(false, "Choose a room.");
        if (checkOut <= checkIn) return new(false, "Check-out must be after check-in.");
        if (checkIn < DateTime.Today) return new(false, "Check-in date cannot be in the past.");

        var room = Db.Table("SELECT Status, Capacity FROM Rooms WHERE Id=@i", ("@i", roomId));
        if (room.Rows.Count == 0) return new(false, "Room not found.");
        if ((string)room.Rows[0]["Status"] == RoomStatus.Maintenance) return new(false, "That room is under maintenance.");
        if (guests > Convert.ToInt32(room.Rows[0]["Capacity"])) return new(false, "Too many guests for that room.");
        if (Overlaps(roomId, checkIn, checkOut, 0)) return new(false, "That room is already booked for those dates.");

        Db.Exec("INSERT INTO Reservations(GuestId,RoomId,CheckIn,CheckOut,Status,CreatedAt) VALUES(@g,@r,@a,@b,'Reserved',@c)",
            ("@g", guestId), ("@r", roomId), ("@a", checkIn.ToString(Fmt)), ("@b", checkOut.ToString(Fmt)),
            ("@c", DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
        return new(true, "Reservation created.");
    }

    static DataRow? Get(int id)
    {
        var dt = Db.Table("SELECT * FROM Reservations WHERE Id=@i", ("@i", id));
        return dt.Rows.Count == 0 ? null : dt.Rows[0];
    }

    /// <summary>Extend or shorten a stay by changing the check-out date.</summary>
    public static Result ChangeCheckOut(int id, DateTime newOut)
    {
        var r = Get(id); if (r == null) return new(false, "Select a reservation first.");
        var status = (string)r["Status"];
        if (status != ResStatus.Reserved && status != ResStatus.CheckedIn)
            return new(false, "Only active reservations can be changed.");
        var inD = DateTime.ParseExact((string)r["CheckIn"], Fmt, null);
        if (newOut.Date <= inD) return new(false, "Check-out must be after check-in.");
        if (Overlaps(Convert.ToInt32(r["RoomId"]), inD, newOut.Date, id))
            return new(false, "The room is booked by someone else for those dates.");
        Db.Exec("UPDATE Reservations SET CheckOut=@o WHERE Id=@i", ("@o", newOut.ToString(Fmt)), ("@i", id));
        return new(true, "Stay updated.");
    }

    public static Result Cancel(int id)
    {
        var r = Get(id); if (r == null) return new(false, "Select a reservation first.");
        if ((string)r["Status"] != ResStatus.Reserved) return new(false, "Only reserved bookings can be cancelled.");
        Db.Exec("UPDATE Reservations SET Status='Cancelled' WHERE Id=@i", ("@i", id));
        var paid = Db.Sum("SELECT SUM(Amount) FROM Payments WHERE ReservationId=@i", ("@i", id));
        if (paid > 0)
        {
            Db.Exec("INSERT INTO Payments(ReservationId,Amount,Method,PaidOn) VALUES(@i,@a,'Refund',@d)",
                ("@i", id), ("@a", -(double)paid), ("@d", DateTime.Today.ToString(Fmt)));
            return new(true, $"Reservation cancelled. Refund of {paid:N2} recorded.");
        }
        return new(true, "Reservation cancelled.");
    }

    public static Result CheckIn(int id)
    {
        var r = Get(id); if (r == null) return new(false, "Select a reservation first.");
        if ((string)r["Status"] != ResStatus.Reserved) return new(false, "Only reserved bookings can be checked in.");
        if (DateTime.ParseExact((string)r["CheckIn"], Fmt, null) > DateTime.Today)
            return new(false, "Arrival date is in the future.");
        int roomId = Convert.ToInt32(r["RoomId"]);
        var status = (string)Db.Scalar("SELECT Status FROM Rooms WHERE Id=@i", ("@i", roomId))!;
        if (status != RoomStatus.Available) return new(false, $"The room is '{status}' and cannot be checked in yet.");
        Db.Exec("UPDATE Reservations SET Status='Checked In' WHERE Id=@i", ("@i", id));
        RoomController.SetStatus(roomId, RoomStatus.Occupied);
        return new(true, "Guest checked in. Room is now Occupied.");
    }

    public static Result CheckOut(int id)
    {
        var r = Get(id); if (r == null) return new(false, "Select a reservation first.");
        if ((string)r["Status"] != ResStatus.CheckedIn) return new(false, "Only checked-in guests can be checked out.");
        var s = BillingController.Statement(id);
        if (s.Balance > 0.005m) return new(false, $"Outstanding balance of {s.Balance:N2} must be paid before check-out.");
        Db.Exec("UPDATE Reservations SET Status='Checked Out' WHERE Id=@i", ("@i", id));
        RoomController.SetStatus(Convert.ToInt32(r["RoomId"]), RoomStatus.NeedsCleaning);
        return new(true, "Guest checked out. Room sent to housekeeping.");
    }
}

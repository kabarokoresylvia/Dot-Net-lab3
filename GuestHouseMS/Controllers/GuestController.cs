using System.Data;
using GuestHouseMS.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Controllers;

public static class GuestController
{
    public static DataTable List(string search = "")
    {
        var s = "%" + search.Trim() + "%";
        return Db.Table(@"SELECT Id, FullName AS Name, IdNumber AS 'ID / Passport', Phone, Email, Address, Nationality
FROM Guests WHERE FullName LIKE @s OR IdNumber LIKE @s OR Phone LIKE @s ORDER BY FullName", ("@s", s));
    }

    public static DataTable Lookup() => Db.Table(
        "SELECT Id, FullName || ' (' || IdNumber || ')' AS Label FROM Guests ORDER BY FullName");

    public static DataTable History(int guestId) => Db.Table(@"
SELECT r.Id, rm.Number AS Room, r.CheckIn, r.CheckOut, r.Status
FROM Reservations r JOIN Rooms rm ON rm.Id = r.RoomId
WHERE r.GuestId = @g ORDER BY r.CheckIn DESC", ("@g", guestId));

    static Result? Validate(Guest g)
    {
        if (g.FullName.Trim().Length == 0) return new(false, "Guest name is required.");
        if (g.IdNumber.Trim().Length == 0) return new(false, "ID / passport number is required.");
        if (g.Email.Trim().Length > 0 && !g.Email.Contains('@')) return new(false, "Email address is not valid.");
        return null;
    }

    public static Result Add(Guest g)
    {
        var bad = Validate(g); if (bad != null) return bad;
        try
        {
            Db.Exec("INSERT INTO Guests(FullName,IdNumber,Phone,Email,Address,Nationality) VALUES(@n,@id,@p,@e,@a,@c)",
                ("@n", g.FullName.Trim()), ("@id", g.IdNumber.Trim()), ("@p", g.Phone.Trim()),
                ("@e", g.Email.Trim()), ("@a", g.Address.Trim()), ("@c", g.Nationality.Trim()));
            return new(true, "Guest registered.");
        }
        catch (Exception e) when (Db.IsConstraint(e)) { return new(false, "A guest with that ID / passport already exists."); }
    }

    public static Result Update(Guest g)
    {
        if (g.Id == 0) return new(false, "Select a guest first.");
        var bad = Validate(g); if (bad != null) return bad;
        try
        {
            Db.Exec("UPDATE Guests SET FullName=@n, IdNumber=@id, Phone=@p, Email=@e, Address=@a, Nationality=@c WHERE Id=@i",
                ("@n", g.FullName.Trim()), ("@id", g.IdNumber.Trim()), ("@p", g.Phone.Trim()),
                ("@e", g.Email.Trim()), ("@a", g.Address.Trim()), ("@c", g.Nationality.Trim()), ("@i", g.Id));
            return new(true, "Guest updated.");
        }
        catch (Exception e) when (Db.IsConstraint(e)) { return new(false, "A guest with that ID / passport already exists."); }
    }

    public static Result Delete(int id)
    {
        if (Db.Count("SELECT COUNT(*) FROM Reservations WHERE GuestId=@i", ("@i", id)) > 0)
            return new(false, "This guest has stay history and cannot be deleted.");
        Db.Exec("DELETE FROM Guests WHERE Id=@i", ("@i", id));
        return new(true, "Guest deleted.");
    }
}

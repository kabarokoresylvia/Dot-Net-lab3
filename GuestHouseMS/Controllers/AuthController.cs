using GuestHouseMS.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Controllers;

public static class AuthController
{
    public static User? Current { get; private set; }

    public static User? Login(string username, string password)
    {
        var dt = Db.Table("SELECT * FROM Users WHERE Username=@u AND Active=1", ("@u", username.Trim()));
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        if (Security.Hash(password, (string)r["Salt"]) != (string)r["PasswordHash"]) return null;
        Current = new User
        {
            Id = Convert.ToInt32(r["Id"]),
            Username = (string)r["Username"],
            FullName = (string)r["FullName"],
            Role = (string)r["Role"],
            Active = true
        };
        return Current;
    }

    public static void Logout() => Current = null;

    public static void SetPassword(int userId, string password)
    {
        var salt = Security.NewSalt();
        Db.Exec("UPDATE Users SET Salt=@s, PasswordHash=@h WHERE Id=@i",
            ("@s", salt), ("@h", Security.Hash(password, salt)), ("@i", userId));
    }

    public static Result ChangePassword(string oldPw, string newPw)
    {
        if (Current == null) return new(false, "Not logged in.");
        if (newPw.Length < 6) return new(false, "New password must be at least 6 characters.");
        var row = Db.Table("SELECT Salt, PasswordHash FROM Users WHERE Id=@i", ("@i", Current.Id)).Rows[0];
        if (Security.Hash(oldPw, (string)row["Salt"]) != (string)row["PasswordHash"])
            return new(false, "Current password is incorrect.");
        SetPassword(Current.Id, newPw);
        return new(true, "Password changed.");
    }
}

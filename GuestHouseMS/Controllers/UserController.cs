using System.Data;
using GuestHouseMS.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Controllers;

public static class UserController
{
    public static DataTable List() => Db.Table(
        "SELECT Id, Username, FullName, Role, CASE Active WHEN 1 THEN 'Yes' ELSE 'No' END AS Active FROM Users ORDER BY Username");

    public static Result Add(string username, string fullName, string role, string password)
    {
        username = username.Trim(); fullName = fullName.Trim();
        if (username.Length < 3) return new(false, "Username must be at least 3 characters.");
        if (fullName.Length == 0) return new(false, "Full name is required.");
        if (password.Length < 6) return new(false, "Password must be at least 6 characters.");
        try
        {
            var salt = Security.NewSalt();
            Db.Exec("INSERT INTO Users(Username,FullName,Role,Salt,PasswordHash,Active) VALUES(@u,@n,@r,@s,@h,1)",
                ("@u", username), ("@n", fullName), ("@r", role), ("@s", salt), ("@h", Security.Hash(password, salt)));
            return new(true, "User created.");
        }
        catch (Exception e) when (Db.IsConstraint(e)) { return new(false, "That username already exists."); }
    }

    public static Result Update(int id, string fullName, string role)
    {
        if (fullName.Trim().Length == 0) return new(false, "Full name is required.");
        if (id == AuthController.Current?.Id && role != Roles.Admin)
            return new(false, "You cannot remove your own Admin role.");
        Db.Exec("UPDATE Users SET FullName=@n, Role=@r WHERE Id=@i", ("@n", fullName.Trim()), ("@r", role), ("@i", id));
        return new(true, "User updated.");
    }

    public static Result SetActive(int id, bool active)
    {
        if (id == AuthController.Current?.Id) return new(false, "You cannot deactivate your own account.");
        Db.Exec("UPDATE Users SET Active=@a WHERE Id=@i", ("@a", active ? 1 : 0), ("@i", id));
        return new(true, active ? "User activated." : "User deactivated.");
    }

    public static Result Delete(int id)
    {
        if (id == AuthController.Current?.Id) return new(false, "You cannot delete your own account.");
        Db.Exec("DELETE FROM Users WHERE Id=@i", ("@i", id));
        return new(true, "User deleted.");
    }

    public static Result ResetPassword(int id, string password)
    {
        if (password.Length < 6) return new(false, "Password must be at least 6 characters.");
        AuthController.SetPassword(id, password);
        return new(true, "Password reset.");
    }
}

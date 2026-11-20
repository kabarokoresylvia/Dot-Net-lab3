using System.Data;
using Microsoft.Data.Sqlite;

namespace GuestHouseMS.Data;

/// <summary>Small data-access helper. All SQL is parameterised (protects against SQL injection).</summary>
public static class Db
{
    public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "guesthouse.db");

    static SqliteConnection Open()
    {
        var c = new SqliteConnection($"Data Source={FilePath}");
        c.Open();
        using var p = c.CreateCommand();
        p.CommandText = "PRAGMA foreign_keys = ON;";
        p.ExecuteNonQuery();
        return c;
    }

    static SqliteCommand Make(SqliteConnection c, string sql, (string Name, object? Value)[] ps)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return cmd;
    }

    public static int Exec(string sql, params (string Name, object? Value)[] ps)
    {
        using var c = Open();
        using var cmd = Make(c, sql, ps);
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(string sql, params (string Name, object? Value)[] ps)
    {
        using var c = Open();
        using var cmd = Make(c, sql, ps);
        var v = cmd.ExecuteScalar();
        return v == DBNull.Value ? null : v;
    }

    public static long Count(string sql, params (string Name, object? Value)[] ps) =>
        Convert.ToInt64(Scalar(sql, ps) ?? 0);

    public static decimal Sum(string sql, params (string Name, object? Value)[] ps) =>
        Convert.ToDecimal(Scalar(sql, ps) ?? 0m);

    public static DataTable Table(string sql, params (string Name, object? Value)[] ps)
    {
        using var c = Open();
        using var cmd = Make(c, sql, ps);
        using var rd = cmd.ExecuteReader();
        var dt = new DataTable();
        dt.Load(rd);
        return dt;
    }

    public static bool IsConstraint(Exception e) => e is SqliteException s && s.SqliteErrorCode == 19;

    public static void Init()
    {
        Exec(@"
CREATE TABLE IF NOT EXISTS Users(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  FullName TEXT NOT NULL,
  Role TEXT NOT NULL,
  Salt TEXT NOT NULL,
  PasswordHash TEXT NOT NULL,
  Active INTEGER NOT NULL DEFAULT 1);

CREATE TABLE IF NOT EXISTS Rooms(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Number TEXT NOT NULL UNIQUE,
  Type TEXT NOT NULL,
  Capacity INTEGER NOT NULL,
  Price REAL NOT NULL,
  Status TEXT NOT NULL DEFAULT 'Available');

CREATE TABLE IF NOT EXISTS Guests(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  FullName TEXT NOT NULL,
  IdNumber TEXT NOT NULL UNIQUE,
  Phone TEXT, Email TEXT, Address TEXT, Nationality TEXT);

CREATE TABLE IF NOT EXISTS Reservations(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  GuestId INTEGER NOT NULL REFERENCES Guests(Id),
  RoomId INTEGER NOT NULL REFERENCES Rooms(Id),
  CheckIn TEXT NOT NULL,
  CheckOut TEXT NOT NULL,
  Status TEXT NOT NULL DEFAULT 'Reserved',
  CreatedAt TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS Charges(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ReservationId INTEGER NOT NULL REFERENCES Reservations(Id),
  Description TEXT NOT NULL,
  Amount REAL NOT NULL,
  AddedOn TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS Payments(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ReservationId INTEGER NOT NULL REFERENCES Reservations(Id),
  Amount REAL NOT NULL,
  Method TEXT NOT NULL,
  PaidOn TEXT NOT NULL);

CREATE TABLE IF NOT EXISTS Maintenance(
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  RoomId INTEGER NOT NULL REFERENCES Rooms(Id),
  Issue TEXT NOT NULL,
  ReportedOn TEXT NOT NULL,
  Resolved INTEGER NOT NULL DEFAULT 0);");

        if (Count("SELECT COUNT(*) FROM Users") == 0)
        {
            SeedUser("admin", "System Administrator", "Admin", "admin123");
            SeedUser("reception", "Front Desk", "Receptionist", "reception123");
            SeedUser("housekeeping", "Housekeeping Staff", "Housekeeping", "clean123");
        }
        if (Count("SELECT COUNT(*) FROM Rooms") == 0)
        {
            string[] nums = { "101", "102", "103", "104", "201", "202", "203", "204" };
            for (int i = 0; i < nums.Length; i++)
            {
                string type = i < 3 ? "Single" : i < 6 ? "Double" : "Family";
                int cap = i < 3 ? 1 : i < 6 ? 2 : 4;
                int price = i < 3 ? 30 : i < 6 ? 50 : 80;
                Exec("INSERT INTO Rooms(Number,Type,Capacity,Price,Status) VALUES(@n,@t,@c,@p,'Available')",
                    ("@n", nums[i]), ("@t", type), ("@c", cap), ("@p", price));
            }
        }
    }

    static void SeedUser(string user, string name, string role, string pw)
    {
        var salt = Security.NewSalt();
        Exec("INSERT INTO Users(Username,FullName,Role,Salt,PasswordHash,Active) VALUES(@u,@n,@r,@s,@h,1)",
            ("@u", user), ("@n", name), ("@r", role), ("@s", salt), ("@h", Security.Hash(pw, salt)));
    }
}

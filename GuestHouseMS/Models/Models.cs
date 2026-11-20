namespace GuestHouseMS.Models;

public record Result(bool Ok, string Message);

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool Active { get; set; }
}

public class Room
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "Single";
    public int Capacity { get; set; } = 1;
    public decimal Price { get; set; }
    public string Status { get; set; } = RoomStatus.Available;
}

public class Guest
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string IdNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string Nationality { get; set; } = "";
}

public class Reservation
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Status { get; set; } = ResStatus.Reserved;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Housekeeping = "Housekeeping";
    public static readonly string[] All = { Admin, Receptionist, Housekeeping };
}

public static class RoomStatus
{
    public const string Available = "Available";
    public const string Occupied = "Occupied";
    public const string Maintenance = "Maintenance";
    public const string NeedsCleaning = "Needs Cleaning";
    public static readonly string[] All = { Available, Occupied, NeedsCleaning, Maintenance };
    public static readonly string[] Types = { "Single", "Double", "Family", "Suite" };
}

public static class ResStatus
{
    public const string Reserved = "Reserved";
    public const string CheckedIn = "Checked In";
    public const string CheckedOut = "Checked Out";
    public const string Cancelled = "Cancelled";
}

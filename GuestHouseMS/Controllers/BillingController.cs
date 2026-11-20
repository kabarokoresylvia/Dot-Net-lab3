using System.Data;
using System.Text;
using GuestHouseMS.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Controllers;

public record BillStatement(int Nights, decimal Rate, decimal RoomTotal, decimal Extras, decimal Total, decimal Paid, decimal Balance);

public static class BillingController
{
    const string Fmt = "yyyy-MM-dd";

    public static DataTable Billable() => Db.Table(@"
SELECT r.Id, '#' || r.Id || ' - ' || g.FullName || ' - Room ' || rm.Number AS Label
FROM Reservations r JOIN Guests g ON g.Id = r.GuestId JOIN Rooms rm ON rm.Id = r.RoomId
WHERE r.Status <> 'Cancelled' ORDER BY r.Id DESC");

    public static BillStatement Statement(int resId)
    {
        var dt = Db.Table(@"SELECT r.CheckIn, r.CheckOut, rm.Price FROM Reservations r
JOIN Rooms rm ON rm.Id = r.RoomId WHERE r.Id=@i", ("@i", resId));
        if (dt.Rows.Count == 0) return new(0, 0, 0, 0, 0, 0, 0);
        var row = dt.Rows[0];
        var a = DateTime.ParseExact((string)row["CheckIn"], Fmt, null);
        var b = DateTime.ParseExact((string)row["CheckOut"], Fmt, null);
        int nights = Math.Max(1, (b - a).Days);
        decimal rate = Convert.ToDecimal(row["Price"]);
        decimal roomTotal = nights * rate;
        decimal extras = Db.Sum("SELECT SUM(Amount) FROM Charges WHERE ReservationId=@i", ("@i", resId));
        decimal paid = Db.Sum("SELECT SUM(Amount) FROM Payments WHERE ReservationId=@i", ("@i", resId));
        decimal total = roomTotal + extras;
        return new(nights, rate, roomTotal, extras, total, paid, total - paid);
    }

    public static DataTable Charges(int resId) => Db.Table(
        "SELECT Id, Description, Amount, AddedOn AS Date FROM Charges WHERE ReservationId=@i ORDER BY Id", ("@i", resId));

    public static DataTable Payments(int resId) => Db.Table(
        "SELECT Id, Amount, Method, PaidOn AS Date FROM Payments WHERE ReservationId=@i ORDER BY Id", ("@i", resId));

    public static Result AddCharge(int resId, string description, decimal amount)
    {
        if (resId == 0) return new(false, "Select a booking first.");
        if (description.Trim().Length == 0) return new(false, "Enter a description (meal, laundry, transport...).");
        if (amount <= 0) return new(false, "Amount must be greater than zero.");
        Db.Exec("INSERT INTO Charges(ReservationId,Description,Amount,AddedOn) VALUES(@r,@d,@a,@t)",
            ("@r", resId), ("@d", description.Trim()), ("@a", (double)amount), ("@t", DateTime.Today.ToString(Fmt)));
        return new(true, "Charge added to the bill.");
    }

    public static Result AddPayment(int resId, decimal amount, string method)
    {
        if (resId == 0) return new(false, "Select a booking first.");
        if (amount <= 0) return new(false, "Payment must be greater than zero.");
        var s = Statement(resId);
        if (amount > s.Balance + 0.005m) return new(false, $"Payment is more than the balance due ({s.Balance:N2}).");
        Db.Exec("INSERT INTO Payments(ReservationId,Amount,Method,PaidOn) VALUES(@r,@a,@m,@t)",
            ("@r", resId), ("@a", (double)amount), ("@m", method), ("@t", DateTime.Today.ToString(Fmt)));
        return new(true, "Payment recorded.");
    }

    public static string ReceiptText(int resId)
    {
        var d = Db.Table(@"SELECT g.FullName, rm.Number, rm.Type, r.CheckIn, r.CheckOut
FROM Reservations r JOIN Guests g ON g.Id = r.GuestId JOIN Rooms rm ON rm.Id = r.RoomId WHERE r.Id=@i", ("@i", resId));
        if (d.Rows.Count == 0) return "";
        var x = d.Rows[0];
        var s = Statement(resId);
        var sb = new StringBuilder();
        sb.AppendLine("SUNRISE GUEST HOUSE");
        sb.AppendLine($"Receipt / Invoice - Booking #{resId}");
        sb.AppendLine(new string('-', 44));
        sb.AppendLine($"Guest : {x["FullName"]}");
        sb.AppendLine($"Room  : {x["Number"]} ({x["Type"]})");
        sb.AppendLine($"Stay  : {x["CheckIn"]} to {x["CheckOut"]}");
        sb.AppendLine(new string('-', 44));
        sb.AppendLine($"{s.Nights} night(s) x {s.Rate:N2}".PadRight(30) + $"{s.RoomTotal,14:N2}");
        foreach (DataRow c in Charges(resId).Rows)
            sb.AppendLine(c["Description"].ToString()!.PadRight(30) + $"{Convert.ToDecimal(c["Amount"]),14:N2}");
        sb.AppendLine(new string('-', 44));
        sb.AppendLine("TOTAL".PadRight(30) + $"{s.Total,14:N2}");
        foreach (DataRow p in Payments(resId).Rows)
            sb.AppendLine($"Paid {p["Date"]} ({p["Method"]})".PadRight(30) + $"{Convert.ToDecimal(p["Amount"]),14:N2}");
        sb.AppendLine("BALANCE DUE".PadRight(30) + $"{s.Balance,14:N2}");
        sb.AppendLine();
        sb.AppendLine($"Printed {DateTime.Now:yyyy-MM-dd HH:mm}. Thank you for staying with us!");
        return sb.ToString();
    }
}

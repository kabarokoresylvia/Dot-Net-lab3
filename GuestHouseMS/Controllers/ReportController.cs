using System.Data;
using System.Text;
using GuestHouseMS.Data;

namespace GuestHouseMS.Controllers;

public static class ReportController
{
    public static readonly string[] Names =
    {
        "Today's arrivals and departures",
        "Room occupancy",
        "Revenue - daily",
        "Revenue - weekly",
        "Revenue - monthly",
        "Revenue - yearly",
        "Outstanding balances",
        "All bookings"
    };

    public static DataTable Run(string name)
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd");
        return name switch
        {
            "Today's arrivals and departures" => Db.Table(@"
SELECT 'Arrival' AS Type, g.FullName AS Guest, rm.Number AS Room, r.CheckIn AS Date, r.Status
FROM Reservations r JOIN Guests g ON g.Id=r.GuestId JOIN Rooms rm ON rm.Id=r.RoomId
WHERE r.CheckIn=@t AND r.Status IN ('Reserved','Checked In')
UNION ALL
SELECT 'Departure', g.FullName, rm.Number, r.CheckOut, r.Status
FROM Reservations r JOIN Guests g ON g.Id=r.GuestId JOIN Rooms rm ON rm.Id=r.RoomId
WHERE r.CheckOut=@t AND r.Status='Checked In'", ("@t", today)),

            "Room occupancy" => Db.Table(@"
SELECT Status, COUNT(*) AS Rooms,
       ROUND(100.0 * COUNT(*) / (SELECT COUNT(*) FROM Rooms), 1) AS 'Percent'
FROM Rooms GROUP BY Status ORDER BY Status"),

            "Revenue - daily" => Revenue("substr(PaidOn,1,10)"),
            "Revenue - weekly" => Revenue("strftime('%Y-W%W', PaidOn)"),
            "Revenue - monthly" => Revenue("substr(PaidOn,1,7)"),
            "Revenue - yearly" => Revenue("substr(PaidOn,1,4)"),

            "Outstanding balances" => Db.Table(@"
SELECT * FROM (
  SELECT r.Id, g.FullName AS Guest, rm.Number AS Room,
    ROUND(MAX(1, CAST(julianday(r.CheckOut) - julianday(r.CheckIn) AS INTEGER)) * rm.Price
      + IFNULL((SELECT SUM(Amount) FROM Charges c WHERE c.ReservationId = r.Id), 0), 2) AS Total,
    ROUND(IFNULL((SELECT SUM(Amount) FROM Payments p WHERE p.ReservationId = r.Id), 0), 2) AS Paid
  FROM Reservations r JOIN Guests g ON g.Id=r.GuestId JOIN Rooms rm ON rm.Id=r.RoomId
  WHERE r.Status <> 'Cancelled')
WHERE Total - Paid > 0.005"),

            _ => ReservationController.List()
        };
    }

    static DataTable Revenue(string periodExpr) => Db.Table(
        $"SELECT {periodExpr} AS Period, ROUND(SUM(Amount),2) AS Revenue, COUNT(*) AS Transactions FROM Payments GROUP BY Period ORDER BY Period DESC");

    /// <summary>Exports any report to a CSV file (opens directly in Excel).</summary>
    public static void ExportCsv(DataTable dt, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => Quote(c.ColumnName))));
        foreach (DataRow r in dt.Rows)
            sb.AppendLine(string.Join(",", r.ItemArray.Select(v => Quote(v?.ToString() ?? ""))));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}

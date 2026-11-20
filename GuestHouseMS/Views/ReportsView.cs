using System.Data;
using GuestHouseMS.Controllers;

namespace GuestHouseMS.Views;

public class ReportsView : UserControl, IRefreshable
{
    readonly DataGridView grid = Ui.Grid();
    readonly ComboBox report = Ui.Combo(ReportController.Names, 300);
    DataTable? current;

    public ReportsView()
    {
        var bar = Ui.Bar();
        bar.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Report"), report,
            Ui.Btn("Run", (_, _) => Run()),
            Ui.Btn("Export to CSV (opens in Excel)", (_, _) => Export())
        });
        Controls.Add(grid);
        Controls.Add(bar);
        Reload();
    }

    public void Reload() => Run();

    void Run()
    {
        current = ReportController.Run(report.Text);
        grid.DataSource = current;
    }

    void Export()
    {
        if (current == null || current.Rows.Count == 0) { Ui.Err("Nothing to export. Run a report with data first."); return; }
        using var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = report.Text.Replace(' ', '_').Replace('\'', '_') + ".csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        ReportController.ExportCsv(current, dlg.FileName);
        Ui.Info("Report exported.");
    }
}

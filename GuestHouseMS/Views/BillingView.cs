using System.Drawing.Printing;
using GuestHouseMS.Controllers;

namespace GuestHouseMS.Views;

public class BillingView : UserControl, IRefreshable
{
    readonly ComboBox booking = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360, Margin = new Padding(2, 6, 2, 2) };
    readonly Label summary = new()
    {
        Dock = DockStyle.Top, Height = 62, Font = new Font("Segoe UI", 10),
        Padding = new Padding(8, 6, 8, 0), Text = "Select a booking."
    };
    readonly DataGridView charges = Ui.Grid();
    readonly DataGridView pays = Ui.Grid();
    readonly TextBox desc = Ui.Txt(200);
    readonly NumericUpDown chargeAmt = Ui.Num(0, 1_000_000, 0, 2, 110);
    readonly NumericUpDown payAmt = Ui.Num(0, 1_000_000, 0, 2, 110);
    readonly ComboBox method = Ui.Combo(new[] { "Cash", "Card", "Mobile money", "Bank transfer" }, 130);
    bool loading;

    public BillingView()
    {
        var sel = Ui.Bar();
        sel.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Booking"), booking,
            Ui.Btn("Print receipt / invoice", (_, _) => PrintReceipt()),
            Ui.Btn("Refresh", (_, _) => Reload())
        });

        var chargeBar = Ui.Bar();
        chargeBar.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Extra service (meal, laundry, transport, tour...)"), desc,
            Ui.Lbl("Amount"), chargeAmt,
            Ui.Btn("Add charge", (_, _) => Act(BillingController.AddCharge(Ui.SelValue(booking), desc.Text, chargeAmt.Value), clear: true))
        });

        var payBar = Ui.Bar();
        payBar.Controls.AddRange(new Control[]
        {
            Ui.Lbl("Payment amount"), payAmt, Ui.Lbl("Method"), method,
            Ui.Btn("Record payment", (_, _) => Act(BillingController.AddPayment(Ui.SelValue(booking), payAmt.Value, method.Text)))
        });

        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tbl.Controls.Add(charges, 0, 0);
        tbl.Controls.Add(pays, 1, 0);

        Controls.Add(tbl);
        Controls.Add(payBar);
        Controls.Add(chargeBar);
        Controls.Add(summary);
        Controls.Add(sel);

        booking.SelectedIndexChanged += (_, _) => { if (!loading) ShowDetails(); };
        Reload();
    }

    public void Reload()
    {
        int prev = Ui.SelValue(booking);
        loading = true;
        Ui.Bind(booking, BillingController.Billable());
        if (prev != 0) booking.SelectedValue = (long)prev;
        loading = false;
        ShowDetails();
    }

    void ShowDetails()
    {
        int id = Ui.SelValue(booking);
        if (id == 0)
        {
            summary.Text = "Select a booking.";
            charges.DataSource = null; pays.DataSource = null;
            return;
        }
        var s = BillingController.Statement(id);
        summary.Text =
            $"{s.Nights} night(s) x {s.Rate:N2} = {s.RoomTotal:N2}      Extras: {s.Extras:N2}\n" +
            $"TOTAL: {s.Total:N2}      PAID: {s.Paid:N2}      BALANCE DUE: {s.Balance:N2}";
        charges.DataSource = BillingController.Charges(id);
        pays.DataSource = BillingController.Payments(id);
        payAmt.Value = Math.Clamp(s.Balance, 0m, payAmt.Maximum);
    }

    void Act(Models.Result r, bool clear = false)
    {
        Ui.Show(r);
        if (!r.Ok) return;
        if (clear) { desc.Clear(); chargeAmt.Value = 0; }
        ShowDetails();
    }

    void PrintReceipt()
    {
        int id = Ui.SelValue(booking);
        if (id == 0) { Ui.Err("Select a booking first."); return; }
        var text = BillingController.ReceiptText(id);
        var doc = new PrintDocument();
        doc.PrintPage += (_, e) =>
        {
            using var f = new Font("Consolas", 11);
            e.Graphics!.DrawString(text, f, Brushes.Black, 60, 60);
        };
        using var dlg = new PrintPreviewDialog { Document = doc, Width = 800, Height = 900 };
        dlg.ShowDialog();
    }
}

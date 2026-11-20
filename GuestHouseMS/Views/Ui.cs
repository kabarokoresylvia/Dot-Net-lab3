using System.Data;
using GuestHouseMS.Models;

namespace GuestHouseMS.Views;

public interface IRefreshable { void Reload(); }

/// <summary>Small helpers so the views stay short and consistent.</summary>
public static class Ui
{
    public static readonly Color Green = Color.FromArgb(47, 107, 79);

    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None
    };

    public static FlowLayoutPanel Bar() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true,
        Padding = new Padding(4)
    };

    public static Button Btn(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, AutoSize = true, Padding = new Padding(6, 2, 6, 2), Margin = new Padding(4) };
        b.Click += onClick;
        return b;
    }

    public static Label Lbl(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(8, 10, 2, 0) };

    public static TextBox Txt(int width = 120) => new() { Width = width, Margin = new Padding(2, 6, 2, 2) };

    public static ComboBox Combo(IEnumerable<string> items, int width = 120)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Margin = new Padding(2, 6, 2, 2) };
        c.Items.AddRange(items.Cast<object>().ToArray());
        if (c.Items.Count > 0) c.SelectedIndex = 0;
        return c;
    }

    public static DateTimePicker Date(DateTime? value = null) => new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 105,
        Value = value ?? DateTime.Today,
        Margin = new Padding(2, 6, 2, 2)
    };

    public static NumericUpDown Num(decimal min, decimal max, decimal value, int decimals = 0, int width = 80) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        DecimalPlaces = decimals,
        Width = width,
        Margin = new Padding(2, 6, 2, 2)
    };

    public static void Bind(ComboBox cb, DataTable dt, string display = "Label")
    {
        cb.DataSource = dt;
        cb.DisplayMember = display;
        cb.ValueMember = "Id";
    }

    public static int SelValue(ComboBox cb) =>
        cb.SelectedValue == null || cb.SelectedValue is DataRowView ? 0 : Convert.ToInt32(cb.SelectedValue);

    public static int SelId(DataGridView g)
    {
        if (g.CurrentRow == null || !g.Columns.Contains("Id")) return 0;
        var v = g.CurrentRow.Cells["Id"].Value;
        return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    }

    public static string Cell(DataGridView g, string col)
    {
        if (g.CurrentRow == null || !g.Columns.Contains(col)) return "";
        return g.CurrentRow.Cells[col].Value?.ToString() ?? "";
    }

    public static void Show(Result r)
    {
        if (r.Ok) Info(r.Message); else Err(r.Message);
    }

    public static void Info(string m) => MessageBox.Show(m, "Guest House", MessageBoxButtons.OK, MessageBoxIcon.Information);
    public static void Err(string m) => MessageBox.Show(m, "Guest House", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public static bool Confirm(string m) =>
        MessageBox.Show(m, "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>Tiny input dialog (used for passwords).</summary>
    public static string? Ask(string title, string prompt, bool password = true)
    {
        using var f = new Form
        {
            Text = title, Size = new Size(360, 160), StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
        };
        var l = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
        var t = new TextBox { Location = new Point(12, 36), Width = 320, UseSystemPasswordChar = password };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(176, 76) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(257, 76) };
        f.Controls.AddRange(new Control[] { l, t, ok, cancel });
        f.AcceptButton = ok; f.CancelButton = cancel;
        return f.ShowDialog() == DialogResult.OK ? t.Text : null;
    }
}

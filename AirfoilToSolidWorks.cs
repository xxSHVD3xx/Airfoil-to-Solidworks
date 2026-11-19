using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;


public static class Converter {
    public static int ConvertFile(string input, string output) {
        if (String.Equals(Path.GetFullPath(input), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose an output file different from the input file.");
        var points = new List<double[]>();
        bool surface = false;
        using (var reader = new StreamReader(input, Encoding.UTF8, true)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                string[] row = ParseLine(line);
                var cells = new List<string>();
                foreach (string value in row) if (!String.IsNullOrWhiteSpace(value)) cells.Add(value.Trim());
                if (cells.Count == 0) { if (surface && points.Count > 0) break; continue; }
                if (!surface) { if (cells[0].StartsWith("airfoil surface", StringComparison.OrdinalIgnoreCase)) surface = true; continue; }
                double x, y;
                if (cells.Count >= 2 && Double.TryParse(cells[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) && Double.TryParse(cells[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) points.Add(new double[] { x, y });
                else if (points.Count > 0) break;
            }
        }
        if (points.Count == 0) throw new InvalidDataException("No points were found in the 'Airfoil surface' section.");
        string temp = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)), ".airfoil-" + Guid.NewGuid().ToString("N") + ".tmp");
        try {
            using (var writer = new StreamWriter(temp, false, new UTF8Encoding(false)))
                foreach (var p in points) writer.WriteLine(Format(p[0]) + "mm\t" + Format(p[1]) + "mm\t0mm");
            if (File.Exists(output)) File.Replace(temp, output, null); else File.Move(temp, output);
        } finally { if (File.Exists(temp)) File.Delete(temp); }
        return points.Count;
    }
    public static Dictionary<string, string> ReadMetadata(string path) {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] keys = { "Name", "Chord(mm)", "Radius(mm)", "Thickness(%)", "Origin(%)", "Pitch(deg)" };
        using (var reader = new StreamReader(path, Encoding.UTF8, true)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                var cells = ParseLine(line);
                if (cells.Length == 0) continue;
                string key = cells[0].Trim();
                if (key.StartsWith("airfoil surface", StringComparison.OrdinalIgnoreCase)) break;
                foreach (string expected in keys)
                    if (String.Equals(key, expected, StringComparison.OrdinalIgnoreCase) && cells.Length > 1 && !String.IsNullOrWhiteSpace(cells[1]))
                        result[expected] = cells[1].Trim();
            }
        }
        return result;
    }
    static string[] ParseLine(string line) {
        var fields = new List<string>(); var field = new StringBuilder(); bool quoted = false;
        for (int i = 0; i < line.Length; i++) {
            char c = line[i];
            if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; } else quoted = !quoted; }
            else if (c == ',' && !quoted) { fields.Add(field.ToString()); field.Length = 0; }
            else field.Append(c);
        }
        fields.Add(field.ToString()); return fields.ToArray();
    }
    static string Format(double value) { if (Math.Abs(value) < 1e-12) value = 0; return value.ToString("0.######", CultureInfo.InvariantCulture); }
}

public class MainForm : Form {
    TextBox source = new TextBox(), destination = new TextBox();
    Label status = new Label(), drop = new Label();
    Label[] metadataValues = new Label[6];
    public MainForm() {
        Text = "Airfoil to SolidWorks"; ClientSize = new Size(620, 540); MinimumSize = new Size(550, 570); BackColor = Color.White; AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 9F); ForeColor = Color.FromArgb(36, 48, 65); AllowDrop = true;
        var heading = new Label { Text = "Airfoil to SolidWorks", Font = new Font("Segoe UI", 17F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 15) };
        var subtitle = new Label { Text = "Convert AirfoilTools coordinates into a SolidWorks curve file.", ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(23, 51) };
        drop.Text = "Drop an AirfoilTools CSV file here\r\nor click Browse to select a file"; drop.TextAlign = ContentAlignment.MiddleCenter;
        drop.Location = new Point(23, 82); drop.Size = new Size(574, 91); drop.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        drop.BorderStyle = BorderStyle.FixedSingle; drop.BackColor = Color.FromArgb(245, 249, 255); drop.ForeColor = Color.FromArgb(36, 88, 150); drop.Font = new Font("Segoe UI", 10F); drop.AllowDrop = true;
        var srcLabel = new Label { Text = "Input CSV", AutoSize = true, Location = new Point(20, 187) };
        source.ReadOnly = true; source.SetBounds(23, 209, 475, 25); source.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var browse = new Button { Text = "Browse...", Location = new Point(508, 207), Size = new Size(89, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        var outLabel = new Label { Text = "Save as", AutoSize = true, Location = new Point(20, 245) };
        destination.SetBounds(23, 267, 475, 25); destination.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var save = new Button { Text = "Browse...", Location = new Point(508, 265), Size = new Size(89, 28), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        var convert = new Button { Text = "Convert", Location = new Point(483, 474), Size = new Size(114, 36), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, BackColor = Color.FromArgb(30, 105, 175), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        status.SetBounds(23, 473, 445, 43); status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; status.Text = "Select a CSV file to get started.";
        var metadataTitle = new Label { Text = "AIRFOIL DETAILS", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(20, 312) };
        var details = new TableLayoutPanel { Location = new Point(23, 339), Size = new Size(574, 105), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, ColumnCount = 3, RowCount = 2, BackColor = Color.FromArgb(245, 249, 255), Padding = new Padding(10, 6, 10, 4) };
        for (int i = 0; i < 3; i++) details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        string[] captions = { "Name", "Chord (mm)", "Radius (mm)", "Thickness (%)", "Origin (%)", "Pitch (deg)" };
        for (int i = 0; i < captions.Length; i++) {
            var box = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var caption = new Label { Text = captions[i], Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(3, 0) };
            metadataValues[i] = new Label { Text = "—", Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoEllipsis = true, Location = new Point(3, 18), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Size = new Size(165, 22) };
            box.Controls.Add(caption); box.Controls.Add(metadataValues[i]); details.Controls.Add(box, i % 3, i / 3);
        }
        Controls.AddRange(new Control[] { metadataTitle, details, heading, subtitle, drop, srcLabel, source, browse, outLabel, destination, save, convert, status });
        browse.FlatStyle = FlatStyle.Flat; browse.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 222); browse.BackColor = Color.White;
        save.FlatStyle = FlatStyle.Flat; save.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 222); save.BackColor = Color.White;
        convert.FlatAppearance.BorderSize = 0;
        browse.Click += delegate { using (var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Title = "Select an AirfoilTools CSV file" }) if (dlg.ShowDialog(this) == DialogResult.OK) SetSource(dlg.FileName); };
        save.Click += delegate { using (var dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt|SolidWorks curve files (*.sldcrv)|*.sldcrv", Title = "Save converted points", FileName = destination.Text, AddExtension = true, DefaultExt = "txt", OverwritePrompt = true }) if (dlg.ShowDialog(this) == DialogResult.OK) destination.Text = dlg.FileName; };

        DragEnter += OnDragEnter; DragDrop += OnDrop; drop.DragEnter += OnDragEnter; drop.DragDrop += OnDrop;
        convert.Click += delegate {
            try {
                if (String.IsNullOrWhiteSpace(source.Text) || String.IsNullOrWhiteSpace(destination.Text)) throw new InvalidOperationException("Select an input file and an output path.");
                if (File.Exists(destination.Text) && MessageBox.Show(this, "The output file already exists. Replace it?", "Confirm overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                int count = Converter.ConvertFile(source.Text.Trim(), destination.Text.Trim());
                status.ForeColor = Color.DarkGreen; status.Text = "Converted successfully: " + count + " points.";
                MessageBox.Show(this, "Conversion complete.\n\n" + count + " points saved to:\n" + destination.Text, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) { status.ForeColor = Color.Firebrick; status.Text = "Conversion failed. Check the input and output paths."; MessageBox.Show(this, ex.Message, "Conversion failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
    }
    void SetSource(string path) {
        source.Text = path;
        destination.Text = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), Path.GetFileNameWithoutExtension(path) + "_sw.txt");
        foreach (Label value in metadataValues) value.Text = "—";
        try {
            var metadata = Converter.ReadMetadata(path);
            string[] keys = { "Name", "Chord(mm)", "Radius(mm)", "Thickness(%)", "Origin(%)", "Pitch(deg)" };
            for (int i = 0; i < keys.Length; i++) { string value; if (metadata.TryGetValue(keys[i], out value)) metadataValues[i].Text = value; }
            status.ForeColor = SystemColors.ControlText; status.Text = "Ready to convert.";
        } catch (Exception ex) {
            status.ForeColor = Color.Firebrick; status.Text = "Could not read the selected file.";
            MessageBox.Show(this, ex.Message, "Could not read file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    void OnDragEnter(object sender, DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; }
    void OnDrop(object sender, DragEventArgs e) { var files = (string[])e.Data.GetData(DataFormats.FileDrop); if (files != null && files.Length > 0) { if (files.Length != 1 || !String.Equals(Path.GetExtension(files[0]), ".csv", StringComparison.OrdinalIgnoreCase)) MessageBox.Show(this, "Please drop one CSV file.", "Select a CSV file", MessageBoxButtons.OK, MessageBoxIcon.Information); else SetSource(files[0]); } }
    [STAThread] static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
}




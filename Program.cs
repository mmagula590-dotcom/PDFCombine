using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            // Drag-and-drop files onto an EXE = Windows passes them as command-line args.
            var inputs = args
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim('"'))
                .Where(File.Exists)
                .Where(p => Path.GetExtension(p).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (inputs.Count == 0)
            {
                MessageBox.Show(
                    "Drag one or more PDF files onto this EXE to merge them.",
                    "PDF Combine",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return 1;
            }

            // Optional: make merge order predictable (alphabetical by filename)
            inputs.Sort(StringComparer.OrdinalIgnoreCase);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string? fileName = PromptForFileName();
            if (string.IsNullOrWhiteSpace(fileName))
                return 0; // user cancelled

            fileName = fileName.Trim();

            // Basic filename cleanup: forbid illegal Windows filename characters.
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');

            if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                fileName += ".pdf";

            // Output to the folder where the EXE is located.
            string exeDir = AppContext.BaseDirectory;
            string outputPath = Path.Combine(exeDir, fileName);

            if (File.Exists(outputPath))
            {
                var overwrite = MessageBox.Show(
                    $"\"{fileName}\" already exists in:\n{exeDir}\n\nOverwrite?",
                    "Confirm overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (overwrite != DialogResult.Yes)
                    return 0;
            }

            using var output = new PdfDocument();

            foreach (var path in inputs)
            {
                using var inputDoc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                for (int i = 0; i < inputDoc.PageCount; i++)
                    output.AddPage(inputDoc.Pages[i]);
            }

            output.Save(outputPath);

            MessageBox.Show(
                $"Merged {inputs.Count} PDF(s) into:\n{outputPath}",
                "PDF Combine",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Error:\n" + ex.Message,
                "PDF Combine",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return 2;
        }
    }

    static string? PromptForFileName()
    {
        using var form = new Form
        {
            Text = "Output file name",
            Width = 360,
            Height = 150,
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label
        {
            Text = "Enter output file name (no folder):",
            Left = 10,
            Top = 12,
            AutoSize = true
        };

        var textBox = new TextBox
        {
            Left = 10,
            Top = 38,
            Width = 320
        };

        var ok = new Button
        {
            Text = "OK",
            Left = 170,
            Width = 75,
            Top = 75,
            DialogResult = DialogResult.OK
        };

        var cancel = new Button
        {
            Text = "Cancel",
            Left = 255,
            Width = 75,
            Top = 75,
            DialogResult = DialogResult.Cancel
        };

        form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog() == DialogResult.OK
            ? textBox.Text
            : null;
    }
}

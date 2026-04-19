using MuseumXMLAPI;
using MuseumXMLAPI.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MuseumXMLDemo
{
    public partial class ReportForm : Form
    {
        private readonly IMuseumXMLAPI _api;

        // Controls
        private ComboBox reportTypeComboBox;
        private RichTextBox reportTextBox;
        private Button generateButton;
        private Button closeButton;

        public ReportForm(IMuseumXMLAPI api)
        {
            _api = api;
            InitializeComponentReport();
        }

        private void InitializeComponentReport()
        {
            this.Text = "Generowanie raportów";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // Report type combo
            reportTypeComboBox = new ComboBox
            {
                Location = new Point(20, 20),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            reportTypeComboBox.Items.AddRange(new[] {
                "Raport archiwum",
                "Statystyki kategorii",
                "Dokumenty wymagające konserwacji",
                "Ostatnio dodane dokumenty"
            });
            reportTypeComboBox.SelectedIndex = 0;

            // Generate button
            generateButton = new Button
            {
                Text = "Generuj",
                Location = new Point(230, 19),
                Width = 80
            };
            generateButton.Click += GenerateButton_Click;

            // Report text box
            reportTextBox = new RichTextBox
            {
                Location = new Point(20, 60),
                Size = new Size(640, 350),
                Font = new Font("Consolas", 10),
                ReadOnly = true
            };

            // Close button
            closeButton = new Button
            {
                Text = "Zamknij",
                DialogResult = DialogResult.Cancel,
                Location = new Point(580, 420),
                Width = 80
            };

            this.Controls.AddRange(new Control[] {
                reportTypeComboBox, generateButton,
                reportTextBox, closeButton
            });
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            try
            {
                switch (reportTypeComboBox.SelectedIndex)
                {
                    case 0: // Archive report
                        var archiveReport = _api.GenerateArchiveReport();
                        reportTextBox.Text = FormatArchiveReport(archiveReport);
                        break;

                    case 1: // Category stats
                        var stats = _api.GetCategoryStatistics();
                        reportTextBox.Text = "STATYSTYKI KATEGORII\n====================\n\n";
                        foreach (var stat in stats)
                        {
                            reportTextBox.Text += $"{stat.Key}: {stat.Value} dokumentów\n";
                        }
                        break;

                    case 2: // Conservation
                        var docs = _api.GetDocumentsRequiringConservation();
                        reportTextBox.Text = "DOKUMENTY WYMAGAJĄCE KONSERWACJI\n===============================\n\n";
                        foreach (var doc in docs)
                        {
                            reportTextBox.Text += $"ID: {doc.DocumentId}, Eksponat: {doc.ExhibitId}, Nazwa: {doc.DocumentName}\n";
                        }
                        break;

                    case 3: // Recent documents
                        var recent = _api.GetRecentDocuments(30);
                        reportTextBox.Text = "OSTATNIO DODANE DOKUMENTY (30 DNI)\n==============================\n\n";
                        foreach (var doc in recent)
                        {
                            reportTextBox.Text += $"ID: {doc.DocumentId}, Eksponat: {doc.ExhibitId}, Nazwa: {doc.DocumentName}, Data: {doc.CreatedDate}\n";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd generowania raportu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatArchiveReport(ArchiveReport report)
        {
            return $"RAPORT ARCHIWUM\n==============\n\n" +
                   $"Łączna liczba dokumentów: {report.TotalDocuments}\n" +
                   $"Aktywne dokumenty: {report.ActiveDocuments}\n" +
                   $"Nieaktywne dokumenty: {report.InactiveDocuments}\n" +
                   $"Eksponaty wymagające konserwacji: {report.DocumentsRequiringConservation}\n\n";
        }
    }
}
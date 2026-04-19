using MuseumXMLAPI;
using MuseumXMLAPI.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MuseumXMLDemo
{
    public partial class MainForm : Form
    {
        private IMuseumXMLAPI _api;
        private string _connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MuseumXMLArchive;Integrated Security=True";
        bool _isConnected = false;

        // Controls
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private TabControl mainTabControl;

        // Tabs
        private TabPage documentsTab;
        private TabPage searchTab;
        private TabPage reportsTab;

        // Documents tab controls
        private DataGridView documentsGridView;
        private ToolStripButton addDocumentButton;
        private ToolStripButton editDocumentButton;
        private ToolStripButton deleteDocumentButton;
        private ToolStripButton refreshButton;

        // Search tab controls
        private GroupBox searchGroupBox;
        private RadioButton categoryRadioButton;
        private RadioButton xpathRadioButton;
        private RadioButton fullTextRadioButton;
        private RadioButton periodRadioButton;
        private RadioButton conditionRadioButton;
        private TextBox searchTextBox;
        private Button searchButton;
        private DataGridView searchResultsGridView;

        // Reports tab controls
        private Button generateReportButton;
        private Button categoryStatsButton;
        private Button conservationReportButton;
        private Button recentDocumentsButton;
        private RichTextBox reportTextBox;

        public MainForm()
        {
            InitializeComponentMain();
            InitializeAPI();
            if (_api != null) SetupEventHandlers();
            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadDocuments();
        }

        private void InitializeComponentMain()
        {
            // Form properties
            this.Text = "Museum XML Archive - Demo Application";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // Create menu strip
            CreateMenuStrip();

            // Create status strip
            CreateStatusStrip();

            // Create main tab control
            CreateMainTabControl();

            // Create tabs
            CreateDocumentsTab();
            CreateSearchTab();
            CreateReportsTab();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            // File menu
            var fileMenu = new ToolStripMenuItem("&Plik");
            fileMenu.DropDownItems.Add("&Połącz z bazą danych", null, (s, e) => ConnectToDatabase());
            fileMenu.DropDownItems.Add("&Rozłącz", null, (s, e) => DisconnectFromDatabase());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Wyjście", null, (s, e) => this.Close());

            // Document menu
            var documentMenu = new ToolStripMenuItem("&Dokumenty");
            documentMenu.DropDownItems.Add("&Dodaj dokument", null, (s, e) => ShowAddDocumentForm());
            documentMenu.DropDownItems.Add("&Edytuj dokument", null, (s, e) => EditSelectedDocument());
            documentMenu.DropDownItems.Add("&Usuń dokument", null, (s, e) => DeleteSelectedDocument());
            documentMenu.DropDownItems.Add(new ToolStripSeparator());
            documentMenu.DropDownItems.Add("&Importuj z pliku", null, (s, e) => ImportDocument());
            documentMenu.DropDownItems.Add("&Eksportuj do pliku", null, (s, e) => ExportDocument());

            // Help menu
            var helpMenu = new ToolStripMenuItem("&Pomoc");
            helpMenu.DropDownItems.Add("&O programie", null, (s, e) => ShowAbout());

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, documentMenu, helpMenu });
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Gotowy");
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);
        }

        private void CreateMainTabControl()
        {
            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;
            this.Controls.Add(mainTabControl);
        }

        private void CreateDocumentsTab()
        {
            documentsTab = new TabPage("Dokumenty");
            mainTabControl.TabPages.Add(documentsTab);

            // Create toolbar
            var toolbar = new ToolStrip();
            addDocumentButton = new ToolStripButton("Dodaj dokument");
            addDocumentButton.Click += (s, e) => ShowAddDocumentForm();

            editDocumentButton = new ToolStripButton("Edytuj dokument");
            editDocumentButton.Click += (s, e) => EditSelectedDocument();

            deleteDocumentButton = new ToolStripButton("Usuń dokument");
            deleteDocumentButton.Click += (s, e) => DeleteSelectedDocument();

            refreshButton = new ToolStripButton("Odśwież");
            refreshButton.Click += (s, e) => LoadDocuments();

            toolbar.Items.AddRange(new ToolStripItem[] {
                addDocumentButton, editDocumentButton, deleteDocumentButton,
                new ToolStripSeparator(), refreshButton
            });

            // Create data grid view
            documentsGridView = new DataGridView();
            documentsGridView.Dock = DockStyle.Fill;
            documentsGridView.AutoGenerateColumns = false;
            documentsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            documentsGridView.MultiSelect = false;
            documentsGridView.ReadOnly = true;

            // Add columns
            documentsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DocumentId",
                HeaderText = "ID",
                DataPropertyName = "DocumentId",
                Width = 50
            });
            documentsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ExhibitId",
                HeaderText = "ID Eksponatu",
                DataPropertyName = "ExhibitId",
                Width = 100
            });
            documentsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DocumentName",
                HeaderText = "Nazwa dokumentu",
                DataPropertyName = "DocumentName",
                Width = 200
            });
            documentsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CreatedBy",
                HeaderText = "Utworzony przez",
                DataPropertyName = "CreatedBy",
                Width = 120
            });
            documentsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CreatedDate",
                HeaderText = "Data utworzenia",
                DataPropertyName = "CreatedDate",
                Width = 120
            });
            documentsGridView.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                HeaderText = "Aktywny",
                DataPropertyName = "IsActive",
                Width = 60
            });

            // Create container panel
            var containerPanel = new Panel();
            containerPanel.Dock = DockStyle.Fill;
            containerPanel.Controls.Add(documentsGridView);
            containerPanel.Controls.Add(toolbar);
            toolbar.Dock = DockStyle.Top;

            documentsTab.Controls.Add(containerPanel);
        }

        private void CreateSearchTab()
        {
            searchTab = new TabPage("Wyszukiwanie");
            mainTabControl.TabPages.Add(searchTab);

            // Create search group box
            searchGroupBox = new GroupBox();
            searchGroupBox.Text = "Typ wyszukiwania";
            searchGroupBox.Size = new Size(300, 180);
            searchGroupBox.Location = new Point(10, 10);

            // Create radio buttons
            categoryRadioButton = new RadioButton { Text = "Według kategorii", Location = new Point(10, 20), Checked = true };
            xpathRadioButton = new RadioButton { Text = "Zapytanie XPath", Location = new Point(10, 45) };
            fullTextRadioButton = new RadioButton { Text = "Wyszukiwanie pełnotekstowe", Location = new Point(10, 70) };
            periodRadioButton = new RadioButton { Text = "Według okresu", Location = new Point(10, 95) };
            conditionRadioButton = new RadioButton { Text = "Według stanu zachowania", Location = new Point(10, 120) };

            searchGroupBox.Controls.AddRange(new Control[] {
                categoryRadioButton, xpathRadioButton, fullTextRadioButton,
                periodRadioButton, conditionRadioButton
            });

            // Create search text box
            searchTextBox = new TextBox();
            searchTextBox.Location = new Point(320, 30);
            searchTextBox.Size = new Size(250, 20);

            // Create search button
            searchButton = new Button();
            searchButton.Text = "Szukaj";
            searchButton.Location = new Point(580, 28);
            searchButton.Size = new Size(75, 25);
            searchButton.Click += SearchButton_Click;

            // Create search results grid
            searchResultsGridView = new DataGridView();
            searchResultsGridView.Location = new Point(10, 200);
            searchResultsGridView.Size = new Size(750, 300);
            searchResultsGridView.AutoGenerateColumns = false;
            searchResultsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            searchResultsGridView.ReadOnly = true;

            // Add columns for search results
            searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DocumentId",
                HeaderText = "ID Dokumentu",
                DataPropertyName = "DocumentId",
                Width = 80
            });
            searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ExhibitId",
                HeaderText = "ID Eksponatu",
                DataPropertyName = "ExhibitId",
                Width = 100
            });
            searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DocumentName",
                HeaderText = "Nazwa dokumentu",
                DataPropertyName = "DocumentName",
                Width = 200
            });
            searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MatchedContent",
                HeaderText = "Znaleziona zawartość",
                DataPropertyName = "MatchedContent",
                Width = 300
            });

            searchTab.Controls.AddRange(new Control[] {
                searchGroupBox, searchTextBox, searchButton, searchResultsGridView
            });
        }

        private void CreateReportsTab()
        {
            reportsTab = new TabPage("Raporty");
            mainTabControl.TabPages.Add(reportsTab);

            // Create report buttons
            generateReportButton = new Button();
            generateReportButton.Text = "Raport archiwum";
            generateReportButton.Location = new Point(10, 10);
            generateReportButton.Size = new Size(120, 30);
            generateReportButton.Click += (s, e) => GenerateArchiveReport();

            categoryStatsButton = new Button();
            categoryStatsButton.Text = "Statystyki kategorii";
            categoryStatsButton.Location = new Point(140, 10);
            categoryStatsButton.Size = new Size(120, 30);
            categoryStatsButton.Click += (s, e) => GenerateCategoryStats();

            conservationReportButton = new Button();
            conservationReportButton.Text = "Raport konserwacji";
            conservationReportButton.Location = new Point(270, 10);
            conservationReportButton.Size = new Size(120, 30);
            conservationReportButton.Click += (s, e) => GenerateConservationReport();

            recentDocumentsButton = new Button();
            recentDocumentsButton.Text = "Najnowsze dokumenty";
            recentDocumentsButton.Location = new Point(400, 10);
            recentDocumentsButton.Size = new Size(120, 30);
            recentDocumentsButton.Click += (s, e) => GenerateRecentDocuments();

            // Create report text box
            reportTextBox = new RichTextBox();
            reportTextBox.Location = new Point(10, 50);
            reportTextBox.Size = new Size(750, 400);
            reportTextBox.ReadOnly = true;
            reportTextBox.Font = new Font("Consolas", 9);

            reportsTab.Controls.AddRange(new Control[] {
                generateReportButton, categoryStatsButton, conservationReportButton,
                recentDocumentsButton, reportTextBox
            });
        }

        private void InitializeAPI()
        {
            try
            {
                _api = new MuseumXMLAPI.MuseumXMLAPI();
                UpdateStatus("API zainicjalizowane");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd inicjalizacji API: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConnectToDatabase()
        {
            try
            {
                if (_api?.Connect(_connectionString) == true)
                {
                    UpdateStatus("Połączono z bazą danych");
                    _isConnected = true;
                    LoadDocuments();
                }
                else
                {
                    UpdateStatus("Błąd połączenia z bazą danych");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisconnectFromDatabase()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Błąd rozłączania: najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                _api?.Disconnect();
                UpdateStatus("Rozłączono z bazą danych");
                _isConnected = false;
                documentsGridView.DataSource = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd rozłączania: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDocuments()
        {
            try
            {
                if (_api?.IsConnected() == true)
                {
                    // Get all active documents
                    var recentDocs = _api.GetRecentDocuments(3650); // 10 years
                    documentsGridView.DataSource = recentDocs;
                    UpdateStatus($"Załadowano {recentDocs?.Count ?? 0} dokumentów");
                }
                else
                {
                    UpdateStatus("Brak połączenia z bazą danych");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania dokumentów: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddDocumentForm()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var addForm = new AddDocumentForm(_api);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadDocuments();
            }
        }

        private void EditSelectedDocument()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (documentsGridView.SelectedRows.Count > 0)
            {
                var documentId = (int)documentsGridView.SelectedRows[0].Cells["DocumentId"].Value;
                var editForm = new AddDocumentForm(_api, documentId);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadDocuments();
                }
            }
            else
            {
                MessageBox.Show("Proszę wybrać dokument do edycji.", "Informacja",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteSelectedDocument()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (documentsGridView.SelectedRows.Count > 0)
            {
                var documentId = (int)documentsGridView.SelectedRows[0].Cells["DocumentId"].Value;
                var documentName = documentsGridView.SelectedRows[0].Cells["DocumentName"].Value.ToString();

                var result = MessageBox.Show($"Czy na pewno chcesz usunąć dokument '{documentName}'?",
                    "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        if (_api?.DeleteDocument(documentId) == true)
                        {
                            UpdateStatus("Dokument usunięty");
                            LoadDocuments();
                        }
                        else
                        {
                            MessageBox.Show("Błąd usuwania dokumentu.", "Błąd",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd usuwania dokumentu: {ex.Message}", "Błąd",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Proszę wybrać dokument do usunięcia.", "Informacja",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                MessageBox.Show("Proszę wprowadzić tekst do wyszukania.", "Informacja",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var searchText = searchTextBox.Text.Trim();
                List<SearchResult> results = null;

                if (categoryRadioButton.Checked)
                {
                    results = _api?.SearchByCategory(searchText);
                }
                else if (xpathRadioButton.Checked)
                {
                    results = _api?.SearchByXPath(searchText);
                }
                else if (fullTextRadioButton.Checked)
                {
                    results = _api?.SearchFullText(searchText);
                }
                else if (periodRadioButton.Checked)
                {
                    results = _api?.SearchByPeriod(searchText);
                }
                else if (conditionRadioButton.Checked)
                {
                    results = _api?.SearchByCondition(searchText);
                }

                searchResultsGridView.DataSource = results;
                UpdateStatus($"Znaleziono {results?.Count ?? 0} wyników");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wyszukiwania: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateArchiveReport()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var report = _api?.GenerateArchiveReport();
                if (report != null)
                {
                    reportTextBox.Text = FormatArchiveReport(report);
                    UpdateStatus("Raport archiwum wygenerowany");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd generowania raportu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateCategoryStats()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var stats = _api?.GetCategoryStatistics();
                if (stats != null)
                {
                    var report = "STATYSTYKI KATEGORII\n" + new string('=', 50) + "\n\n";
                    foreach (var stat in stats)
                    {
                        report += $"{stat.Key}: {stat.Value} dokumentów\n";
                    }
                    reportTextBox.Text = report;
                    UpdateStatus("Statystyki kategorii wygenerowane");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd generowania statystyk: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateConservationReport()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var documents = _api?.GetDocumentsRequiringConservation();
                if (documents != null)
                {
                    var report = "DOKUMENTY WYMAGAJĄCE KONSERWACJI\n" + new string('=', 50) + "\n\n";
                    foreach (var doc in documents)
                    {
                        report += $"ID: {doc.DocumentId}, Eksponat: {doc.ExhibitId}, Nazwa: {doc.DocumentName}\n";
                    }
                    reportTextBox.Text = report;
                    UpdateStatus("Raport konserwacji wygenerowany");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd generowania raportu konserwacji: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateRecentDocuments()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var documents = _api?.GetRecentDocuments(30);
                if (documents != null)
                {
                    var report = "NAJNOWSZE DOKUMENTY (30 DNI)\n" + new string('=', 50) + "\n\n";
                    foreach (var doc in documents)
                    {
                        report += $"ID: {doc.DocumentId}, Eksponat: {doc.ExhibitId}, Nazwa: {doc.DocumentName}, Data: {doc.CreatedDate}\n";
                    }
                    reportTextBox.Text = report;
                    UpdateStatus("Raport najnowszych dokumentów wygenerowany");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd generowania raportu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportDocument()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Pliki XML (*.xml)|*.xml|Wszystkie pliki (*.*)|*.*";
                openFileDialog.Title = "Wybierz plik XML do importu";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var importForm = new AddDocumentForm(_api))
                    {
                        importForm.LoadXmlFromFile(openFileDialog.FileName);
                        if (importForm.ShowDialog() == DialogResult.OK)
                        {
                            LoadDocuments();
                        }
                    }
                }
            }
        }

        private void ExportDocument()
        {
            if (!_isConnected)
            {
                MessageBox.Show($"Najpierw połącz się z bazą danych", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (documentsGridView.SelectedRows.Count > 0)
            {
                var documentId = (int)documentsGridView.SelectedRows[0].Cells["DocumentId"].Value;
                var documentName = documentsGridView.SelectedRows[0].Cells["DocumentName"].Value.ToString();

                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Pliki XML (*.xml)|*.xml";
                saveFileDialog.Title = "Zapisz dokument jako";
                saveFileDialog.FileName = $"{documentName}.xml";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (_api?.ExportDocumentToFile(documentId, saveFileDialog.FileName) == true)
                        {
                            MessageBox.Show("Dokument został wyeksportowany.", "Sukces",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Błąd eksportowania dokumentu.", "Błąd",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd eksportowania: {ex.Message}", "Błąd",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Proszę wybrać dokument do eksportu.", "Informacja",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show("Museum XML Archive Demo\nVersja 1.0\n\nAplikacja demonstracyjna systemu zarządzania dokumentami XML w archiwum muzealnym.",
                "O programie", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string FormatArchiveReport(ArchiveReport report)
        {
            var formatted = "RAPORT ARCHIWUM\n" + new string('=', 50) + "\n\n";
            formatted += $"Całkowita liczba dokumentów: {report.TotalDocuments}\n";
            formatted += $"Aktywne dokumenty: {report.ActiveDocuments}\n";
            formatted += $"Nieaktywne dokumenty: {report.InactiveDocuments}\n";
            formatted += $"Dokumenty wymagające konserwacji: {report.DocumentsRequiringConservation}\n\n";

            return formatted;
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _api?.Disconnect();
                _api?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
            base.OnFormClosing(e);
        }
        private void SetupEventHandlers()
        {
            _api.DocumentAdded += (sender, args) => MessageBox.Show($"Błąd przy dodawaniu dokumentu: {args.DocumentId}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            _api.DocumentUpdated += (sender, args) => MessageBox.Show($"Błąd przy edycji dokumentu: {args.DocumentId}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            _api.DocumentDeactivated += (sender, args) => MessageBox.Show($"Błąd przy miękkim usuwaniu dokumentu: {args.DocumentId}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            _api.DocumentDeleted += (sender, args) => MessageBox.Show($"Błąd przy usuwaniu dokumentu: {args.DocumentId}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            _api.ErrorOccurred += (sender, args) => MessageBox.Show($"Błąd: {args.ErrorMessage}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
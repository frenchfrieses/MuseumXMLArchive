using MuseumXMLAPI;
using MuseumXMLAPI.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MuseumXMLDemo
{
    public partial class SearchForm : Form
    {
        private readonly IMuseumXMLAPI _api;

        // Controls
        private ComboBox searchTypeComboBox;
        private TextBox searchTextBox;
        private Button searchButton;
        private DataGridView resultsGridView;
        private Button selectButton;
        private Button cancelButton;

        public DocumentInfo SelectedDocument { get; private set; }

        public SearchForm(IMuseumXMLAPI api)
        {
            _api = api;
            InitializeComponentSearch();
        }

        private void InitializeComponentSearch()
        {
            this.Text = "Wyszukiwanie dokumentów";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // Search type combo
            searchTypeComboBox = new ComboBox
            {
                Location = new Point(20, 20),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            searchTypeComboBox.Items.AddRange(new[] { "Kategoria", "XPath", "Pełny tekst", "Okres", "Stan zachowania" });
            searchTypeComboBox.SelectedIndex = 0;

            // Search text box
            searchTextBox = new TextBox
            {
                Location = new Point(230, 20),
                Width = 300
            };

            // Search button
            searchButton = new Button
            {
                Text = "Szukaj",
                Location = new Point(540, 19),
                Width = 80
            };
            searchButton.Click += SearchButton_Click;

            // Results grid
            resultsGridView = new DataGridView
            {
                Location = new Point(20, 60),
                Size = new Size(740, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AutoGenerateColumns = false
            };
            resultsGridView.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "DocumentId", HeaderText = "ID", DataPropertyName = "DocumentId" },
                new DataGridViewTextBoxColumn { Name = "ExhibitId", HeaderText = "ID Eksponatu", DataPropertyName = "ExhibitId" },
                new DataGridViewTextBoxColumn { Name = "DocumentName", HeaderText = "Nazwa", DataPropertyName = "DocumentName" }
            );

            // Action buttons
            selectButton = new Button
            {
                Text = "Wybierz",
                DialogResult = DialogResult.OK,
                Location = new Point(600, 470),
                Width = 80
            };
            selectButton.Click += (s, e) =>
            {
                if (resultsGridView.SelectedRows.Count > 0)
                {
                    SelectedDocument = resultsGridView.SelectedRows[0].DataBoundItem as DocumentInfo;
                }
            };

            cancelButton = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Location = new Point(680, 470),
                Width = 80
            };

            this.Controls.AddRange(new Control[] {
                searchTypeComboBox, searchTextBox, searchButton,
                resultsGridView, selectButton, cancelButton
            });
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            try
            {
                List<SearchResult> results = null;
                var query = searchTextBox.Text.Trim();

                switch (searchTypeComboBox.SelectedIndex)
                {
                    case 0: results = _api.SearchByCategory(query); break;
                    case 1: results = _api.SearchByXPath(query); break;
                    case 2: results = _api.SearchFullText(query); break;
                    case 3: results = _api.SearchByPeriod(query); break;
                    case 4: results = _api.SearchByCondition(query); break;
                }

                resultsGridView.DataSource = results?.ConvertAll(r => new DocumentInfo
                {
                    DocumentId = r.DocumentId,
                    ExhibitId = r.ExhibitId,
                    DocumentName = r.DocumentName
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wyszukiwania: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
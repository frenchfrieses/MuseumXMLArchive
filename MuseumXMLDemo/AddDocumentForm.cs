using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using MuseumXMLAPI;
using MuseumXMLAPI.Models;

namespace MuseumXMLDemo
{
    public partial class AddDocumentForm : Form
    {
        private IMuseumXMLAPI _api;
        private int? _documentId;
        private bool _isEditMode;

        // Controls
        private Label exhibitIdLabel;
        private TextBox exhibitIdTextBox;
        private Label documentNameLabel;
        private TextBox documentNameTextBox;
        private Label createdByLabel;
        private TextBox createdByTextBox;
        private Label xmlContentLabel;
        private RichTextBox xmlContentTextBox;
        private Button loadFromFileButton;
        private Button validateButton;
        private Button previewButton;
        private Button saveButton;
        private Button cancelButton;
        private GroupBox sampleDataGroupBox;
        private ComboBox sampleDataComboBox;
        private Button loadSampleButton;

        public AddDocumentForm(IMuseumXMLAPI api, int? documentId = null)
        {
            _api = api;
            _documentId = documentId;
            _isEditMode = documentId.HasValue;

            InitializeComponentAddDocument();

            if (_isEditMode)
            {
                LoadDocumentForEdit();
            }
            else
            {
                LoadSampleData();
            }
        }

        public void LoadXmlFromFile(string filePath)
        {
            try
            {
                var xmlContent = File.ReadAllText(filePath);
                xmlContentTextBox.Text = xmlContent;

                if (string.IsNullOrWhiteSpace(documentNameTextBox.Text))
                {
                    documentNameTextBox.Text = Path.GetFileNameWithoutExtension(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania pliku: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponentAddDocument()
        {
            // Form properties
            this.Text = _isEditMode ? "Edytuj dokument" : "Dodaj nowy dokument";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Exhibit ID
            exhibitIdLabel = new Label();
            exhibitIdLabel.Text = "ID Eksponatu:";
            exhibitIdLabel.Location = new Point(12, 15);
            exhibitIdLabel.Size = new Size(100, 23);

            exhibitIdTextBox = new TextBox();
            exhibitIdTextBox.Location = new Point(120, 12);
            exhibitIdTextBox.Size = new Size(200, 20);

            // Document Name
            documentNameLabel = new Label();
            documentNameLabel.Text = "Nazwa dokumentu:";
            documentNameLabel.Location = new Point(12, 45);
            documentNameLabel.Size = new Size(100, 23);

            documentNameTextBox = new TextBox();
            documentNameTextBox.Location = new Point(120, 42);
            documentNameTextBox.Size = new Size(300, 20);

            // Created By
            createdByLabel = new Label();
            createdByLabel.Text = "Utworzony przez:";
            createdByLabel.Location = new Point(12, 75);
            createdByLabel.Size = new Size(100, 23);

            createdByTextBox = new TextBox();
            createdByTextBox.Location = new Point(120, 72);
            createdByTextBox.Size = new Size(200, 20);
            createdByTextBox.Text = Environment.UserName;

            // Sample data section
            sampleDataGroupBox = new GroupBox();
            sampleDataGroupBox.Text = "Dane przykładowe";
            sampleDataGroupBox.Location = new Point(450, 12);
            sampleDataGroupBox.Size = new Size(320, 90);

            sampleDataComboBox = new ComboBox();
            sampleDataComboBox.Location = new Point(10, 25);
            sampleDataComboBox.Size = new Size(200, 21);
            sampleDataComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

            loadSampleButton = new Button();
            loadSampleButton.Text = "Załaduj";
            loadSampleButton.Location = new Point(220, 23);
            loadSampleButton.Size = new Size(75, 25);
            loadSampleButton.Click += LoadSampleButton_Click;

            sampleDataGroupBox.Controls.Add(sampleDataComboBox);
            sampleDataGroupBox.Controls.Add(loadSampleButton);

            // XML Content
            xmlContentLabel = new Label();
            xmlContentLabel.Text = "Zawartość XML:";
            xmlContentLabel.Location = new Point(12, 115);
            xmlContentLabel.Size = new Size(100, 23);

            xmlContentTextBox = new RichTextBox();
            xmlContentTextBox.Location = new Point(12, 140);
            xmlContentTextBox.Size = new Size(760, 350);
            xmlContentTextBox.Font = new Font("Consolas", 10);
            xmlContentTextBox.WordWrap = false;
            xmlContentTextBox.ScrollBars = RichTextBoxScrollBars.Both;

            // Action buttons
            loadFromFileButton = new Button();
            loadFromFileButton.Text = "Załaduj z pliku";
            loadFromFileButton.Location = new Point(12, 500);
            loadFromFileButton.Size = new Size(100, 30);
            loadFromFileButton.Click += LoadFromFileButton_Click;

            validateButton = new Button();
            validateButton.Text = "Waliduj XML";
            validateButton.Location = new Point(125, 500);
            validateButton.Size = new Size(100, 30);
            validateButton.Click += ValidateButton_Click;

            previewButton = new Button();
            previewButton.Text = "Podgląd";
            previewButton.Location = new Point(238, 500);
            previewButton.Size = new Size(100, 30);
            previewButton.Click += PreviewButton_Click;

            saveButton = new Button();
            saveButton.Text = _isEditMode ? "Zapisz zmiany" : "Dodaj dokument";
            saveButton.Location = new Point(580, 500);
            saveButton.Size = new Size(120, 30);
            saveButton.BackColor = Color.LightGreen;
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button();
            cancelButton.Text = "Anuluj";
            cancelButton.Location = new Point(710, 500);
            cancelButton.Size = new Size(75, 30);
            cancelButton.Click += (s, e) => this.Close();

            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                exhibitIdLabel, exhibitIdTextBox,
                documentNameLabel, documentNameTextBox,
                createdByLabel, createdByTextBox,
                sampleDataGroupBox,
                xmlContentLabel, xmlContentTextBox,
                loadFromFileButton, validateButton, previewButton,
                saveButton, cancelButton
            });
        }

        private void LoadDocumentForEdit()
        {
            try
            {
                if (_documentId.HasValue && _api != null)
                {
                    var document = _api.GetDocument(_documentId.Value);
                    if (document != null)
                    {
                        exhibitIdTextBox.Text = document.ExhibitId;
                        documentNameTextBox.Text = document.DocumentName;
                        createdByTextBox.Text = document.CreatedBy;
                        xmlContentTextBox.Text = document.ToFormattedXmlString();

                        exhibitIdTextBox.ReadOnly = true;
                        exhibitIdTextBox.BackColor = SystemColors.Control;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania dokumentu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSampleData()
        {
            sampleDataComboBox.Items.Clear();
            sampleDataComboBox.Items.AddRange(new string[] {
                "Obraz - Mona Lisa",
                "Rzeźba - Dawid Michała Anioła",
                "Naczynie ceramiczne - Waza grecka",
                "Biżuteria - Naszyjnik egipski",
                "Broń - Miecz średniowieczny",
                "Dokument historyczny - Pergamin",
                "Moneta - Denar rzymski",
                "Tkanina - Gobeliny flamandzkie"
            });
            sampleDataComboBox.SelectedIndex = 0;
        }

        private void LoadSampleButton_Click(object sender, EventArgs e)
        {
            if (sampleDataComboBox.SelectedItem == null) return;

            var selectedSample = sampleDataComboBox.SelectedItem.ToString();
            var sampleXml = GenerateSampleXML(selectedSample);

            xmlContentTextBox.Text = sampleXml;

            switch (selectedSample)
            {
                case "Obraz - Mona Lisa":
                    exhibitIdTextBox.Text = "EXH001";
                    documentNameTextBox.Text = "Mona Lisa - Opis katalogowy";
                    break;
                case "Rzeźba - Dawid Michała Anioła":
                    exhibitIdTextBox.Text = "EXH002";
                    documentNameTextBox.Text = "Dawid - Dokumentacja konserwatorska";
                    break;
                case "Naczynie ceramiczne - Waza grecka":
                    exhibitIdTextBox.Text = "EXH003";
                    documentNameTextBox.Text = "Waza attycka - Analiza archeologiczna";
                    break;
                case "Biżuteria - Naszyjnik egipski":
                    exhibitIdTextBox.Text = "EXH004";
                    documentNameTextBox.Text = "Naszyjnik faraona - Ekspertyza";
                    break;
                case "Broń - Miecz średniowieczny":
                    exhibitIdTextBox.Text = "EXH005";
                    documentNameTextBox.Text = "Miecz rycerski - Dokumentacja muzealnicza";
                    break;
                case "Dokument historyczny - Pergamin":
                    exhibitIdTextBox.Text = "EXH006";
                    documentNameTextBox.Text = "Pergamin królewski - Transkrypcja";
                    break;
                case "Moneta - Denar rzymski":
                    exhibitIdTextBox.Text = "EXH007";
                    documentNameTextBox.Text = "Denar Trajana - Katalog numizmatyczny";
                    break;
                case "Tkanina - Gobeliny flamandzkie":
                    exhibitIdTextBox.Text = "EXH008";
                    documentNameTextBox.Text = "Gobelin flamandzki - Raport konserwacji";
                    break;
            }
        }

        private string GenerateSampleXML(string sampleType)
        {
            switch (sampleType)
            {
                case "Obraz - Mona Lisa":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH001' status='Active'>
  <BasicInfo>
    <Title>Mona Lisa</Title>
    <Category>Painting</Category>
    <Creator>Leonardo da Vinci</Creator>
    <DateCreated>1503-1519</DateCreated>
    <Period>Renaissance</Period>
    <Culture>Italian</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>The Mona Lisa is a half-length portrait painting by Italian artist Leonardo da Vinci.</ShortDescription>
    <DetailedDescription>Considered an archetypal masterpiece of the Italian Renaissance, it has been described as 'the best known, the most visited, the most written about, the most sung about, the most parodied work of art in the world'.</DetailedDescription>
    <Significance>One of the most valuable paintings in the world.</Significance>
    <Tags>
      <Tag>Renaissance</Tag>
      <Tag>Portrait</Tag>
      <Tag>Masterpiece</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Height>77</Height>
      <Width>53</Width>
    </Dimensions>
    <Material>Oil paint</Material>
    <Material>Poplar panel</Material>
    <Technique>Sfumato</Technique>
    <Condition>Excellent</Condition>
    <ConservationNotes>Last conservation in 2019. Requires controlled environment.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Main Gallery</Building>
    <Floor>1</Floor>
    <Room>Room 6</Room>
    <Display>Wall A12</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1797-01-01</Date>
      <Method>Purchase</Method>
      <Source>French Royal Collection</Source>
    </Acquisition>
    <Provenance>Originally painted in Florence, Italy. Acquired by King Francis I of France.</Provenance>
    <Exhibitions>
      <Exhibition>
        <Name>Masterpieces of the Renaissance</Name>
        <Location>Louvre Museum, Paris</Location>
        <StartDate>2019-10-24</StartDate>
        <EndDate>2020-02-15</EndDate>
      </Exhibition>
    </Exhibitions>
  </History>
  <Media>
    <Image primary='true'>
      <FileName>mona_lisa.jpg</FileName>
      <Description>Front view of the painting</Description>
    </Image>
  </Media>
</Exhibit>";

                case "Rzeźba - Dawid Michała Anioła":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH002' status='Active'>
  <BasicInfo>
    <Title>David</Title>
    <Category>Sculpture</Category>
    <Creator>Michelangelo Buonarroti</Creator>
    <DateCreated>1501-1504</DateCreated>
    <Period>Renaissance</Period>
    <Culture>Italian</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>Marble statue of the Biblical hero David, a masterpiece of Renaissance sculpture.</ShortDescription>
    <DetailedDescription>The statue represents the Biblical hero David, a favoured subject in the art of Florence. Unlike earlier depictions of David which portray the hero after his victory over Goliath, Michelangelo chose to represent David before the battle.</DetailedDescription>
    <Significance>One of the most recognizable works of Renaissance sculpture.</Significance>
    <Tags>
      <Tag>Marble</Tag>
      <Tag>Biblical</Tag>
      <Tag>Masterpiece</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Height>517</Height>
    </Dimensions>
    <Weight>6000</Weight>
    <Material>Marble</Material>
    <Condition>Good</Condition>
    <ConservationNotes>Regular cleaning required due to dust accumulation.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Main Gallery</Building>
    <Floor>1</Floor>
    <Room>Room 3</Room>
    <Display>Central pedestal</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1504-09-08</Date>
      <Method>Purchase</Method>
      <Source>Opera del Duomo</Source>
    </Acquisition>
    <Provenance>Originally commissioned for the roof of Florence Cathedral but placed in a public square instead.</Provenance>
  </History>
  <Media>
    <Image primary='true'>
      <FileName>david_front.jpg</FileName>
      <Description>Front view of the sculpture</Description>
    </Image>
    <Image primary='false'>
      <FileName>david_detail.jpg</FileName>
      <Description>Detail of the face</Description>
    </Image>
  </Media>
</Exhibit>";

                case "Naczynie ceramiczne - Waza grecka":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH003' status='Active'>
  <BasicInfo>
    <Title>Attic Black-Figure Amphora</Title>
    <Category>Pottery</Category>
    <SubCategory>Amphora</SubCategory>
    <DateCreated>530 p.n.e.</DateCreated>
    <Period>Archaic</Period>
    <Culture>Greek</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>Black-figure amphora depicting Dionysus with satyrs.</ShortDescription>
    <DetailedDescription>This amphora is a fine example of Attic black-figure pottery, showing the god Dionysus surrounded by satyrs. The scene represents a Dionysian revel, a common theme in Greek art.</DetailedDescription>
    <Significance>Excellent example of Athenian black-figure technique.</Significance>
    <Tags>
      <Tag>Ceramic</Tag>
      <Tag>Black-figure</Tag>
      <Tag>Mythology</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Height>42.5</Height>
      <Diameter>28.3</Diameter>
    </Dimensions>
    <Material>Terracotta</Material>
    <Technique>Black-figure painting</Technique>
    <Condition>Fair</Condition>
    <ConservationNotes>Several fragments have been reattached. Stable condition.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Ancient Art Wing</Building>
    <Floor>2</Floor>
    <Room>Room 15</Room>
    <Display>Case 7</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1925-05-12</Date>
      <Method>Purchase</Method>
      <Source>Private collection</Source>
      <Price>1200</Price>
      <Currency>USD</Currency>
    </Acquisition>
    <Provenance>Excavated in Attica, Greece. Previously in private collections in Germany and France.</Provenance>
  </History>
</Exhibit>";

                case "Biżuteria - Naszyjnik egipski":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH004' status='Active'>
  <BasicInfo>
    <Title>Egyptian Faience Necklace</Title>
    <Category>Jewelry</Category>
    <DateCreated>1350 p.n.e.</DateCreated>
    <Period>New Kingdom</Period>
    <Culture>Egyptian</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>Faience bead necklace with amulet pendants.</ShortDescription>
    <DetailedDescription>This necklace is composed of blue-green faience beads with several amulet pendants including the Eye of Horus (wedjat) and the ankh symbol. Such necklaces were worn for both adornment and protection.</DetailedDescription>
    <Significance>Excellent example of New Kingdom jewelry.</Significance>
    <Tags>
      <Tag>Faience</Tag>
      <Tag>Amulet</Tag>
      <Tag>Funerary</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Width>45</Width>
    </Dimensions>
    <Material>Faience</Material>
    <Condition>Good</Condition>
    <ConservationNotes>Some beads show minor wear. Overall stable.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Ancient Civilizations Wing</Building>
    <Floor>1</Floor>
    <Room>Room 8</Room>
    <Display>Case 3</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1902-11-05</Date>
      <Method>Donation</Method>
      <Source>Egypt Exploration Fund</Source>
    </Acquisition>
    <Provenance>Excavated from a tomb in Thebes. Part of the 1902 excavation season.</Provenance>
  </History>
</Exhibit>";

                case "Broń - Miecz średniowieczny":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH005' status='Active'>
  <BasicInfo>
    <Title>Medieval Longsword</Title>
    <Category>Weapon</Category>
    <SubCategory>Sword</SubCategory>
    <DateCreated>14th century</DateCreated>
    <Period>Medieval</Period>
    <Culture>European</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>German longsword with cruciform hilt.</ShortDescription>
    <DetailedDescription>This Oakeshott Type XVa longsword features a double-edged blade, cruciform hilt with straight quillons, and a scent-stopper pommel. The blade shows pattern welding indicative of high-quality manufacture.</DetailedDescription>
    <Significance>Fine example of 14th century swordsmithing.</Significance>
    <Tags>
      <Tag>Weapon</Tag>
      <Tag>Knight</Tag>
      <Tag>Armory</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Height>120</Height>
      <Width>4.5</Width>
    </Dimensions>
    <Weight>1.4</Weight>
    <Material>Steel</Material>
    <Material>Wood</Material>
    <Material>Leather</Material>
    <Condition>Fair</Condition>
    <ConservationNotes>Some rust spots on blade. Hilt wrapping partially missing.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Medieval Wing</Building>
    <Floor>2</Floor>
    <Room>Room 12</Room>
    <Display>Wall mount</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1898-03-22</Date>
      <Method>Purchase</Method>
      <Source>Private collection</Source>
      <Price>250</Price>
      <Currency>GBP</Currency>
    </Acquisition>
    <Provenance>Reportedly found in a castle armory in Bavaria. Owned by several collectors before museum acquisition.</Provenance>
  </History>
</Exhibit>";

                case "Dokument historyczny - Pergamin":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH006' status='Active'>
  <BasicInfo>
    <Title>Royal Charter of 1324</Title>
    <Category>Manuscript</Category>
    <SubCategory>Charter</SubCategory>
    <DateCreated>1324-06-15</DateCreated>
    <Period>Medieval</Period>
    <Culture>English</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>Royal charter granting land to a monastery.</ShortDescription>
    <DetailedDescription>This vellum charter, written in Latin, grants lands in Yorkshire to the Cistercian Abbey of Rievaulx. It bears the seal of King Edward II and is an important document for understanding medieval land tenure.</DetailedDescription>
    <Significance>Well-preserved example of 14th century royal administration.</Significance>
    <Tags>
      <Tag>Document</Tag>
      <Tag>Medieval</Tag>
      <Tag>Latin</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Height>45</Height>
      <Width>30</Width>
    </Dimensions>
    <Material>Vellum</Material>
    <Technique>Iron gall ink</Technique>
    <Condition>Good</Condition>
    <ConservationNotes>Some fading of ink. Seal partially damaged but mostly intact.</ConservationNotes>
  </Technical>
  <Location onDisplay='false'>
    <Building>Special Collections</Building>
    <Room>Climate-controlled room</Room>
    <StorageLocation>Climate-controlled cabinet 12</StorageLocation>
  </Location>
  <History>
    <Acquisition>
      <Date>1853-09-10</Date>
      <Method>Bequest</Method>
      <Source>Duke of Northumberland</Source>
    </Acquisition>
    <Provenance>Originally part of the monastery archives. Passed through several private collections after the Dissolution.</Provenance>
  </History>
</Exhibit>";

                case "Moneta - Denar rzymski":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH007' status='Active'>
  <BasicInfo>
    <Title>Roman Denarius of Trajan</Title>
    <Category>Coin</Category>
    <DateCreated>103 n.e.</DateCreated>
    <Period>Imperial</Period>
    <Culture>Roman</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>Silver denarius minted during Trajan's Dacian Wars.</ShortDescription>
    <DetailedDescription>This silver denarius shows the laureate head of Trajan on the obverse and a standing figure of Victory writing on a shield on the reverse. Minted to celebrate victories in the Dacian Wars.</DetailedDescription>
    <Significance>Excellent example of Roman imperial coinage.</Significance>
    <Tags>
      <Tag>Numismatics</Tag>
      <Tag>Silver</Tag>
      <Tag>Imperial</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='mm'>
      <Diameter>19</Diameter>
    </Dimensions>
    <Weight>3.2</Weight>
    <Material>Silver</Material>
    <Condition>Excellent</Condition>
    <ConservationNotes>Minimal wear. Sharp details.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Ancient Civilizations Wing</Building>
    <Floor>1</Floor>
    <Room>Room 5</Room>
    <Display>Coin case 8</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1920-04-15</Date>
      <Method>Purchase</Method>
      <Source>Numismatic Society</Source>
      <Price>75</Price>
      <Currency>GBP</Currency>
    </Acquisition>
    <Provenance>Part of a hoard found in Romania in 1912.</Provenance>
  </History>
</Exhibit>";

                case "Tkanina - Gobeliny flamandzkie":
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH008' status='Active'>
  <BasicInfo>
    <Title>Flemish Hunting Tapestry</Title>
    <Category>Textile</Category>
    <SubCategory>Tapestry</SubCategory>
    <DateCreated>1520-1530</DateCreated>
    <Period>Renaissance</Period>
    <Culture>Flemish</Culture>
  </BasicInfo>
  <Description>
    <ShortDescription>Wool and silk tapestry depicting a hunting scene.</ShortDescription>
    <DetailedDescription>This tapestry from Brussels shows a noble hunting party in a forest setting. The detailed weaving includes numerous animals, trees, and figures in contemporary dress. The borders feature floral motifs and heraldic devices.</DetailedDescription>
    <Significance>Fine example of early 16th century Flemish tapestry work.</Significance>
    <Tags>
      <Tag>Textile</Tag>
      <Tag>Hunting</Tag>
      <Tag>Nobility</Tag>
    </Tags>
  </Description>
  <Technical>
    <Dimensions unit='cm'>
      <Height>320</Height>
      <Width>280</Width>
    </Dimensions>
    <Material>Wool</Material>
    <Material>Silk</Material>
    <Technique>Tapestry weaving</Technique>
    <Condition>Good</Condition>
    <ConservationNotes>Some fading of colors. Minor repairs to edges.</ConservationNotes>
  </Technical>
  <Location onDisplay='true'>
    <Building>Decorative Arts Wing</Building>
    <Floor>3</Floor>
    <Room>Room 21</Room>
    <Display>Wall hanging</Display>
  </Location>
  <History>
    <Acquisition>
      <Date>1887-07-03</Date>
      <Method>Purchase</Method>
      <Source>French dealer</Source>
      <Price>5000</Price>
      <Currency>FRF</Currency>
    </Acquisition>
    <Provenance>Originally made for a noble family in the Low Countries. Later in the collection of a French chateau.</Provenance>
  </History>
</Exhibit>";

                default:
                    return @"<Exhibit xmlns='http://museum.example.com/exhibit' id='EXH000' status='Active'>
  <BasicInfo>
    <Title>Sample Exhibit</Title>
    <Category>Pottery</Category>
    <DateCreated>1000 p.n.e.</DateCreated>
    <Period>Ancient</Period>
  </BasicInfo>
  <Description>
    <ShortDescription>Sample exhibit description</ShortDescription>
  </Description>
  <Technical>
    <Material>Clay</Material>
    <Condition>Good</Condition>
  </Technical>
  <Location onDisplay='true'>
    <Building>Main Gallery</Building>
    <Room>Main Hall</Room>
  </Location>
  <History>
    <Acquisition>
      <Date>1900-01-01</Date>
      <Method>Purchase</Method>
      <Source>Unknown</Source>
    </Acquisition>
  </History>
</Exhibit>";
            }
        }

        private void LoadFromFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Pliki XML (*.xml)|*.xml|Wszystkie pliki (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadXmlFromFile(openFileDialog.FileName);
                }
            }
        }

        private void ValidateButton_Click(object sender, EventArgs e)
        {
            if (_api.ValidateDocument(xmlContentTextBox.Text))
            {
                MessageBox.Show("XML jest poprawny względem schematu.", "Walidacja",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            else
            {
                MessageBox.Show("Błąd walidacji XML", "Błąd",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContentTextBox.Text);

                using (Form previewForm = new Form())
                {
                    previewForm.Text = "Podgląd XML";
                    previewForm.Size = new Size(600, 400);
                    previewForm.StartPosition = FormStartPosition.CenterParent;

                    RichTextBox previewBox = new RichTextBox();
                    previewBox.Dock = DockStyle.Fill;
                    previewBox.Font = new Font("Consolas", 10);
                    previewBox.Text = FormatXml(doc);
                    previewBox.ReadOnly = true;

                    previewForm.Controls.Add(previewBox);
                    previewForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd przetwarzania XML: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatXml(XmlDocument doc)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (XmlTextWriter tx = new XmlTextWriter(sw))
                {
                    tx.Formatting = Formatting.Indented;
                    doc.WriteTo(tx);
                    return sw.ToString();
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(exhibitIdTextBox.Text))
                {
                    MessageBox.Show("ID Eksponatu jest wymagane.", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(documentNameTextBox.Text))
                {
                    MessageBox.Show("Nazwa dokumentu jest wymagana.", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(xmlContentTextBox.Text))
                {
                    MessageBox.Show("Zawartość XML jest wymagana.", "Błąd",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create document model
                var document = new DocumentInfo
                {
                    ExhibitId = exhibitIdTextBox.Text,
                    DocumentName = documentNameTextBox.Text,
                    CreatedBy = createdByTextBox.Text,
                    XMLContent = XmlStringIntoXmlDocument(xmlContentTextBox.Text),
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                };

                // Save via API
                if (_isEditMode && _documentId.HasValue)
                {
                    document.DocumentId = _documentId.Value;
                    _api.UpdateDocument(document.DocumentId, document.DocumentName, document.ToFormattedXmlString(), document.ModifiedBy );
                    MessageBox.Show("Dokument został zaktualizowany.", "Sukces",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _api.AddDocument(document.ExhibitId, document.DocumentName, document.ToFormattedXmlString(), document.CreatedBy);
                    MessageBox.Show("Dokument został dodany.", "Sukces",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisywania dokumentu: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private XmlDocument XmlStringIntoXmlDocument(string XMLString)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(XMLString);
            return xmlDocument;
        }

        private string GetExhibitXSD()
        {
            return @"<?xml version='1.0' encoding='UTF-8'?>
<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'
           targetNamespace='http://museum.example.com/exhibit'
           xmlns:tns='http://museum.example.com/exhibit'
           elementFormDefault='qualified'>

  <!-- Główny element Exhibit -->
  <xs:element name='Exhibit'>
    <xs:complexType>
      <xs:sequence>
        <xs:element name='BasicInfo' type='tns:BasicInfoType'/>
        <xs:element name='Description' type='tns:DescriptionType'/>
        <xs:element name='Technical' type='tns:TechnicalType'/>
        <xs:element name='Location' type='tns:LocationType'/>
        <xs:element name='History' type='tns:HistoryType'/>
        <xs:element name='Media' type='tns:MediaType' minOccurs='0'/>
      </xs:sequence>
      <xs:attribute name='id' type='tns:ExhibitIDType' use='required'/>
      <xs:attribute name='status' type='tns:StatusType' use='required'/>
    </xs:complexType>
  </xs:element>

  <!-- Typy złożone -->
  <xs:complexType name='BasicInfoType'>
    <xs:sequence>
      <xs:element name='Title' type='xs:string'/>
      <xs:element name='Category' type='tns:CategoryType'/>
      <xs:element name='SubCategory' type='xs:string' minOccurs='0'/>
      <xs:element name='Creator' type='xs:string' minOccurs='0'/>
      <xs:element name='DateCreated' type='xs:string' minOccurs='0'/>
      <xs:element name='Period' type='xs:string' minOccurs='0'/>
      <xs:element name='Culture' type='xs:string' minOccurs='0'/>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='DescriptionType'>
    <xs:sequence>
      <xs:element name='ShortDescription' type='xs:string'/>
      <xs:element name='DetailedDescription' type='xs:string' minOccurs='0'/>
      <xs:element name='Significance' type='xs:string' minOccurs='0'/>
      <xs:element name='Tags' minOccurs='0'>
        <xs:complexType>
          <xs:sequence>
            <xs:element name='Tag' type='xs:string' maxOccurs='unbounded'/>
          </xs:sequence>
        </xs:complexType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='TechnicalType'>
    <xs:sequence>
      <xs:element name='Dimensions' type='tns:DimensionsType' minOccurs='0'/>
      <xs:element name='Weight' type='xs:string' minOccurs='0'/>
      <xs:element name='Material' type='xs:string' maxOccurs='unbounded'/>
      <xs:element name='Technique' type='xs:string' minOccurs='0'/>
      <xs:element name='Condition' type='tns:ConditionType'/>
      <xs:element name='ConservationNotes' type='xs:string' minOccurs='0'/>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='DimensionsType'>
    <xs:sequence>
      <xs:element name='Height' type='xs:decimal' minOccurs='0'/>
      <xs:element name='Width' type='xs:decimal' minOccurs='0'/>
      <xs:element name='Depth' type='xs:decimal' minOccurs='0'/>
      <xs:element name='Diameter' type='xs:decimal' minOccurs='0'/>
    </xs:sequence>
    <xs:attribute name='unit' type='tns:UnitType' use='required'/>
  </xs:complexType>

  <xs:complexType name='LocationType'>
    <xs:sequence>
      <xs:element name='Building' type='xs:string'/>
      <xs:element name='Floor' type='xs:string' minOccurs='0'/>
      <xs:element name='Room' type='xs:string' minOccurs='0'/>
      <xs:element name='Display' type='xs:string' minOccurs='0'/>
      <xs:element name='StorageLocation' type='xs:string' minOccurs='0'/>
    </xs:sequence>
    <xs:attribute name='onDisplay' type='xs:boolean' use='required'/>
  </xs:complexType>

  <xs:complexType name='HistoryType'>
    <xs:sequence>
      <xs:element name='Acquisition' type='tns:AcquisitionType'/>
      <xs:element name='Provenance' type='xs:string' minOccurs='0'/>
      <xs:element name='Exhibitions' minOccurs='0'>
        <xs:complexType>
          <xs:sequence>
            <xs:element name='Exhibition' type='tns:ExhibitionType' maxOccurs='unbounded'/>
          </xs:sequence>
        </xs:complexType>
      </xs:element>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='AcquisitionType'>
    <xs:sequence>
      <xs:element name='Date' type='xs:date'/>
      <xs:element name='Method' type='tns:AcquisitionMethodType'/>
      <xs:element name='Source' type='xs:string'/>
      <xs:element name='Price' type='xs:decimal' minOccurs='0'/>
      <xs:element name='Currency' type='xs:string' minOccurs='0'/>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='ExhibitionType'>
    <xs:sequence>
      <xs:element name='Name' type='xs:string'/>
      <xs:element name='Location' type='xs:string'/>
      <xs:element name='StartDate' type='xs:date'/>
      <xs:element name='EndDate' type='xs:date'/>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='MediaType'>
    <xs:sequence>
      <xs:element name='Image' type='tns:ImageType' maxOccurs='unbounded'/>
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name='ImageType'>
    <xs:sequence>
      <xs:element name='FileName' type='xs:string'/>
      <xs:element name='Description' type='xs:string' minOccurs='0'/>
    </xs:sequence>
    <xs:attribute name='primary' type='xs:boolean'/>
  </xs:complexType>

  <!-- Typy proste -->
  <xs:simpleType name='ExhibitIDType'>
    <xs:restriction base='xs:string'>
      <xs:pattern value='EXH[0-9]{3}'/>
    </xs:restriction>
  </xs:simpleType>

  <xs:simpleType name='StatusType'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value='Active'/>
      <xs:enumeration value='Inactive'/>
      <xs:enumeration value='OnLoan'/>
      <xs:enumeration value='InConservation'/>
    </xs:restriction>
  </xs:simpleType>

  <xs:simpleType name='CategoryType'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value='Pottery'/>
      <xs:enumeration value='Weapon'/>
      <xs:enumeration value='Painting'/>
      <xs:enumeration value='Coin'/>
      <xs:enumeration value='Sculpture'/>
      <xs:enumeration value='Manuscript'/>
      <xs:enumeration value='Jewelry'/>
      <xs:enumeration value='Textile'/>
    </xs:restriction>
  </xs:simpleType>

  <xs:simpleType name='ConditionType'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value='Excellent'/>
      <xs:enumeration value='Good'/>
      <xs:enumeration value='Fair'/>
      <xs:enumeration value='Poor'/>
    </xs:restriction>
  </xs:simpleType>

  <xs:simpleType name='UnitType'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value='cm'/>
      <xs:enumeration value='mm'/>
      <xs:enumeration value='m'/>
    </xs:restriction>
  </xs:simpleType>

  <xs:simpleType name='AcquisitionMethodType'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value='Purchase'/>
      <xs:enumeration value='Donation'/>
      <xs:enumeration value='Bequest'/>
      <xs:enumeration value='Exchange'/>
      <xs:enumeration value='Transfer'/>
    </xs:restriction>
  </xs:simpleType>

</xs:schema>";
        }
    }
}
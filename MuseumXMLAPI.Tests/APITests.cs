using Microsoft.VisualStudio.TestTools.UnitTesting;
using MuseumXMLAPI;
using MuseumXMLAPI.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace MuseumXMLAPI.Tests
{
    [TestClass]
    public class APITests
    {
        #region Test Setup and Configuration

        private MuseumXMLAPI _api;
        private string _testConnectionString;
        private string _testExhibitId;
        private int _testDocumentId;
        private List<int> _createdDocumentIds;
        private List<string> _createdExhibitIds;

        // Test XML samples
        private readonly string _validXmlContent = @"<Exhibit xmlns=""http://museum.example.com/exhibit"" id=""EXH001"" status=""Active"">
    <BasicInfo>
        <Title>Starożytna Amfora Grecka</Title>
        <Category>Pottery</Category>
        <SubCategory>Amfora</SubCategory>
        <Creator>Nieznany Ceramik</Creator>
        <DateCreated>500 p.n.e. - 400 p.n.e.</DateCreated>
        <Period>Klasyczny</Period>
        <Culture>Grecka</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Piękna grecka amfora z okresu klasycznego</ShortDescription>
        <DetailedDescription>Amfora wykonana z czerwonej gliny, ozdobiona scenami mitologicznymi przedstawiającymi bogów olimpijskich.</DetailedDescription>
        <Significance>Reprezentuje szczyt greckiej sztuki ceramicznej</Significance>
        <Tags>
            <Tag>mitologia</Tag>
            <Tag>ceramika</Tag>
            <Tag>starożytność</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit=""cm"">
            <Height>45.5</Height>
            <Width>28.0</Width>
            <Depth>28.0</Depth>
        </Dimensions>
        <Weight>3.2 kg</Weight>
        <Material>Glina</Material>
        <Material>Farba ceramiczna</Material>
        <Technique>Toczenie, malowanie</Technique>
        <Condition>Good</Condition>
        <ConservationNotes>Niewielkie pęknięcia na podstawie, wymagana konserwacja prewencyjna</ConservationNotes>
    </Technical>
    <Location onDisplay=""true"">
        <Building>Budynek Główny</Building>
        <Floor>Pierwsze piętro</Floor>
        <Room>Sala Grecka</Room>
        <Display>Gablota G-15</Display>
    </Location>
    <History>
        <Acquisition>
            <Date>1995-03-15</Date>
            <Method>Purchase</Method>
            <Source>Kolekcja Prywatna - Dr. Kowalski</Source>
            <Price>15000</Price>
            <Currency>PLN</Currency>
        </Acquisition>
        <Provenance>Kolekcja prywatna, wcześniej w posiadaniu rodziny Kowalskich od 1960 roku</Provenance>
    </History>
    <Media>
        <Image primary=""true"">
            <FileName>amfora_exh001_front.jpg</FileName>
            <Description>Widok z przodu</Description>
        </Image>
        <Image primary=""false"">
            <FileName>amfora_exh001_back.jpg</FileName>
            <Description>Widok z tyłu</Description>
        </Image>
    </Media>
</Exhibit>";

        private readonly string _invalidXmlContent = @"
            <!-- INSERT INVALID XML CONTENT HERE -->
            <exhibit>
                <title>Unclosed Title
                <category>Test Category</category>
                <period>Test Period
                <description>Test Description</description>
            </exhibit>";

        private readonly string _complexValidXmlContent = @"<Exhibit xmlns=""http://museum.example.com/exhibit"" id=""EXH002"" status=""Active"">
    <BasicInfo>
        <Title>Miecz Rycerski XIV wieku</Title>
        <Category>Weapon</Category>
        <SubCategory>Miecz jednoręczny</SubCategory>
        <Creator>Mistrz Kowalski z Krakowa</Creator>
        <DateCreated>1350-1370</DateCreated>
        <Period>Średniowiecze</Period>
        <Culture>Polska</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Dobrze zachowany miecz rycerski z XIV wieku</ShortDescription>
        <DetailedDescription>Miecz o długości 95 cm z charakterystyczną krzyżową rękojeścią. Klinga wykonana ze stali wysokiej jakości.</DetailedDescription>
        <Significance>Przykład średniowiecznego rzemiosła zbrojeniowego</Significance>
        <Tags>
            <Tag>średniowiecze</Tag>
            <Tag>broń</Tag>
            <Tag>rycerstwo</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit=""cm"">
            <Height>95.0</Height>
            <Width>15.0</Width>
        </Dimensions>
        <Weight>1.8 kg</Weight>
        <Material>Stal</Material>
        <Material>Skóra</Material>
        <Material>Drewno</Material>
        <Technique>Kucie, hartowanie</Technique>
        <Condition>Fair</Condition>
        <ConservationNotes>Ślady korozji na klidze, rękojeść wymaga renowacji</ConservationNotes>
    </Technical>
    <Location onDisplay=""false"">
        <Building>Magazyn A</Building>
        <Room>Pomieszczenie 12</Room>
        <StorageLocation>Szafa S-45, Półka 3</StorageLocation>
    </Location>
    <History>
        <Acquisition>
            <Date>2010-08-20</Date>
            <Method>Donation</Method>
            <Source>Fundacja Dziedzictwa Kulturowego</Source>
        </Acquisition>
    </History>
</Exhibit>";

        #endregion

        #region Test Initialization and Cleanup

        [TestInitialize]
        public void TestInitialize()
        {
            // Initialize test environment
            _api = new MuseumXMLAPI();
            _testConnectionString = GetTestConnectionString();
            _testExhibitId = GenerateTestExhibitId();
            _createdDocumentIds = new List<int>();
            _createdExhibitIds = new List<string>();

            // Setup event handlers for testing
            SetupEventHandlers();

            // Connect to test database
            Assert.IsTrue(_api.Connect(_testConnectionString), "Failed to connect to test database");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                // Clean up created documents
                foreach (int docId in _createdDocumentIds)
                {
                    try
                    {
                        _api.DeleteDocument(docId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to delete document {docId}: {ex.Message}");
                    }
                }

                // Clean up created exhibits
                foreach (string exhibitId in _createdExhibitIds)
                {
                    try
                    {
                        _api.DeleteDocumentByExhibitId(exhibitId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to delete documents for exhibit {exhibitId}: {ex.Message}");
                    }
                }

                // Clear collections
                _createdDocumentIds.Clear();
                _createdExhibitIds.Clear();

                // Disconnect from database
                _api?.Disconnect();
                _api?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test cleanup: {ex.Message}");
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Perform any class-level cleanup if needed
            // This could include cleaning up test database artifacts
        }

        #endregion

        #region Connection Management Tests

        [TestMethod]
        public void Connect_ValidConnectionString_ReturnsTrue()
        {
            // Arrange
            var api = new MuseumXMLAPI();

            // Act
            bool result = api.Connect(_testConnectionString);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(api.IsConnected());

            // Cleanup
            api.Disconnect();
            api.Dispose();
        }

        [TestMethod]
        public void Connect_InvalidConnectionString_ReturnsFalse()
        {
            // Arrange
            var api = new MuseumXMLAPI();
            string invalidConnectionString = "Invalid Connection String";

            // Act
            bool result = api.Connect(invalidConnectionString);

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(api.IsConnected());

            // Cleanup
            api.Dispose();
        }

        [TestMethod]
        public void IsConnected_WhenConnected_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(_api.IsConnected());
        }

        [TestMethod]
        public void Disconnect_WhenConnected_DisconnectsSuccessfully()
        {
            // Act
            _api.Disconnect();

            // Assert
            Assert.IsFalse(_api.IsConnected());
        }

        #endregion

        #region Document Management Tests

        [TestMethod]
        public void AddDocument_ValidDocument_ReturnsValidDocumentId()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            // Act
            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);

            // Assert
            Assert.IsTrue(documentId > 0, "Document ID should be greater than 0");

            // Track for cleanup
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);
        }

        [TestMethod]
        public void AddDocument_InvalidXML_ReturnsNegativeOne()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Invalid Document";
            string createdBy = "TestUser";

            // Act
            int documentId = _api.AddDocument(exhibitId, documentName, _invalidXmlContent, createdBy);

            // Assert
            Assert.AreEqual(-1, documentId, "Should return -1 for invalid XML");
        }

        [TestMethod]
        public void AddDocument_NullOrEmptyParameters_ReturnsNegativeOne()
        {
            // Test null exhibit ID
            int result1 = _api.AddDocument(null, "Test", _validXmlContent, "TestUser");
            Assert.AreEqual(-1, result1);

            // Test empty document name
            int result2 = _api.AddDocument("EXH001", "", _validXmlContent, "TestUser");
            Assert.AreEqual(-1, result2);
        }

        [TestMethod]
        public void GetDocument_ExistingDocument_ReturnsDocumentInfo()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            DocumentInfo document = _api.GetDocument(documentId);

            // Assert
            Assert.IsNotNull(document);
            Assert.AreEqual(documentId, document.DocumentId);
            Assert.AreEqual(exhibitId, document.ExhibitId);
            Assert.AreEqual(documentName, document.DocumentName);
            Assert.AreEqual(createdBy, document.CreatedBy);
            Assert.IsFalse(document.IsXMLContentEmpty());
            Assert.IsTrue(document.IsActive);
        }

        [TestMethod]
        public void GetDocument_NonExistentDocument_ReturnsNull()
        {
            // Arrange
            int nonExistentId = 999999;

            // Act
            DocumentInfo document = _api.GetDocument(nonExistentId);

            // Assert
            Assert.IsNull(document);
        }

        [TestMethod]
        public void GetDocumentByExhibitId_ExistingExhibit_ReturnsDocumentInfo()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            DocumentInfo document = _api.GetDocumentByExhibitId(exhibitId);

            // Assert
            Assert.IsNotNull(document);
            Assert.AreEqual(exhibitId, document.ExhibitId);
            Assert.AreEqual(documentName, document.DocumentName);
        }

        [TestMethod]
        public void UpdateDocument_ValidUpdate_ReturnsTrue()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string originalName = "Original Document";
            string updatedName = "Updated Document";
            string createdBy = "TestUser";
            string modifiedBy = "DESKTOP-PUPMGII\\mcbar";

            int documentId = _api.AddDocument(exhibitId, originalName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            bool result = _api.UpdateDocument(documentId, updatedName, _complexValidXmlContent, modifiedBy);

            // Assert
            Assert.IsTrue(result);

            // Verify update
            DocumentInfo updatedDoc = _api.GetDocument(documentId);
            Assert.AreEqual(updatedName, updatedDoc.DocumentName);
            Assert.AreEqual(modifiedBy, updatedDoc.ModifiedBy);
            Assert.IsNotNull(updatedDoc.ModifiedDate);
        }

        [TestMethod]
        public void UpdateDocument_InvalidXML_ReturnsFalse()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string originalName = "Original Document";
            string createdBy = "TestUser";
            string modifiedBy = "ModifierUser";

            int documentId = _api.AddDocument(exhibitId, originalName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            bool result = _api.UpdateDocument(documentId, "Updated Name", _invalidXmlContent, modifiedBy);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DeactivateDocument_ExistingDocument_ReturnsTrue()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string deactivatedBy = "DeactivatorUser";
            string reason = "Test deactivation";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            bool result = _api.DeactivateDocument(documentId, deactivatedBy, reason);

            // Assert
            Assert.IsTrue(result);

            // Verify deactivation
            DocumentInfo doc = _api.GetDocument(documentId, true);
            Assert.IsFalse(doc.IsActive);
        }

        [TestMethod]
        public void DeleteDocument_ExistingDocument_ReturnsTrue()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdExhibitIds.Add(exhibitId);

            // Act
            bool result = _api.DeleteDocument(documentId);

            // Assert
            Assert.IsTrue(result);

            // Verify deletion
            DocumentInfo doc = _api.GetDocument(documentId);
            Assert.IsNull(doc);
        }

        [TestMethod]
        public void DeleteDocumentByExhibitId_ExistingExhibit_ReturnsTrue()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);

            // Act
            bool result = _api.DeleteDocumentByExhibitId(exhibitId);

            // Assert
            Assert.IsTrue(result);

            // Verify deletion
            DocumentInfo doc = _api.GetDocumentByExhibitId(exhibitId);
            Assert.IsNull(doc);

            // Remove from cleanup list since it's already deleted
            _createdDocumentIds.Remove(documentId);
        }

        #endregion

        #region Validation Tests

        [TestMethod]
        public void ValidateDocument_ValidXML_ReturnsTrue()
        {
            // Act
            bool result = _api.ValidateDocument(_validXmlContent);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateDocument_InvalidXML_ReturnsFalse()
        {
            // Act
            bool result = _api.ValidateDocument(_invalidXmlContent);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateDocument_ComplexValidXML_ReturnsTrue()
        {
            // Act
            bool result = _api.ValidateDocument(_complexValidXmlContent);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateDocument_NullOrEmptyXML_ReturnsFalse()
        {
            // Test null XML
            bool result1 = _api.ValidateDocument(null);
            Assert.IsFalse(result1);

            // Test empty XML
            bool result2 = _api.ValidateDocument("");
            Assert.IsFalse(result2);

            // Test whitespace XML
            bool result3 = _api.ValidateDocument("   ");
            Assert.IsFalse(result3);
        }

        #endregion

        #region Search Operations Tests

        [TestMethod]
        public void SearchByCategory_ExistingCategory_ReturnsResults()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string category = "Pottery";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            List<SearchResult> results = _api.SearchByCategory(category);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
            Assert.IsTrue(results.Any(r => r.DocumentId == documentId));
        }

        [TestMethod]
        public void SearchByCategory_NonExistentCategory_ReturnsEmptyList()
        {
            // Act
            List<SearchResult> results = _api.SearchByCategory("NonExistent Category");

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void SearchFullText_ExistingText_ReturnsResults()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string searchText = "Test";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            List<SearchResult> results = _api.SearchFullText(searchText);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void SearchByPeriod_ExistingPeriod_ReturnsResults()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string period = "Klasyczny";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            List<SearchResult> results = _api.SearchByPeriod(period);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void SearchByCondition_ExistingCondition_ReturnsResults()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string condition = "Good";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            List<SearchResult> results = _api.SearchByCondition(condition);

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
        }

        [TestMethod]
        public void SearchByXPath_ValidExpression_ReturnsResults()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string xpathExpression = "//title[text()='Test Exhibit']";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            List<SearchResult> results = _api.SearchByXPath(xpathExpression);

            // Assert
            Assert.IsNotNull(results);
            // Note: Results depend on XPath implementation in stored procedure
        }

        #endregion

        #region Reporting Tests

        [TestMethod]
        public void GenerateArchiveReport_WhenCalled_ReturnsReport()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            ArchiveReport report = _api.GenerateArchiveReport();

            // Assert
            Assert.IsNotNull(report);
            Assert.IsTrue(report.TotalDocuments >= 0);
            Assert.IsTrue(report.ActiveDocuments >= 0);
            Assert.IsTrue(report.InactiveDocuments >= 0);
        }

        [TestMethod]
        public void GetCategoryStatistics_WhenCalled_ReturnsDictionary()
        {
            // Act
            Dictionary<string, int> statistics = _api.GetCategoryStatistics();

            // Assert
            Assert.IsNotNull(statistics);
        }

        [TestMethod]
        public void GetDocumentsRequiringConservation_WhenCalled_ReturnsList()
        {
            // Act
            List<DocumentInfo> documents = _api.GetDocumentsRequiringConservation();

            // Assert
            Assert.IsNotNull(documents);
        }

        [TestMethod]
        public void GetRecentDocuments_WithValidDays_ReturnsList()
        {
            // Arrange
            int days = 30;

            // Act
            List<DocumentInfo> documents = _api.GetRecentDocuments(days);

            // Assert
            Assert.IsNotNull(documents);
        }

        #endregion

        #region Utility Tests

        [TestMethod]
        public void CleanExpiredSearchCache_WhenCalled_RunsWithoutError()
        {
            // Act & Assert (should not throw exception)
            _api.CleanExpiredSearchCache(60);
        }

        [TestMethod]
        public void GetDocumentOperationLog_ExistingDocument_ReturnsList()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            // Act
            List<OperationLog> logs = _api.GetDocumentOperationLog(documentId);

            // Assert
            Assert.IsNotNull(logs);
        }

        [TestMethod]
        public void ExportDocumentToFile_ExistingDocument_ReturnsTrue()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";
            string tempFilePath = Path.GetTempFileName();

            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);

            try
            {
                // Act
                bool result = _api.ExportDocumentToFile(documentId, tempFilePath);

                // Assert
                Assert.IsTrue(result);
                Assert.IsTrue(File.Exists(tempFilePath));
                Assert.IsTrue(new FileInfo(tempFilePath).Length > 0);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        [TestMethod]
        public void ImportDocumentFromFile_ValidFile_ReturnsValidDocumentId()
        {
            // Arrange
            string exhibitId = GenerateTestExhibitId();
            string documentName = "Imported Document";
            string createdBy = "TestUser";
            string tempFilePath = Path.GetTempFileName();

            try
            {
                // Create temporary file with valid XML
                File.WriteAllText(tempFilePath, _validXmlContent);

                // Act
                int documentId = _api.ImportDocumentFromFile(tempFilePath, exhibitId, documentName, createdBy);

                // Assert
                Assert.IsTrue(documentId > 0);

                // Track for cleanup
                _createdDocumentIds.Add(documentId);
                _createdExhibitIds.Add(exhibitId);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        #endregion

        #region Event Handling Tests

        [TestMethod]
        public void DocumentAdded_EventFired_WhenDocumentAdded()
        {
            // Arrange
            bool eventFired = false;

            _api.DocumentAdded += (sender, args) =>
            {
                eventFired = true;
            };

            string exhibitId = GenerateTestExhibitId();
            string documentName = "Test Document";
            string createdBy = "TestUser";

            // Act
            int documentId = _api.AddDocument(exhibitId, documentName, _validXmlContent, createdBy);

            // Assert
            Assert.IsTrue(eventFired);

            // Track for cleanup
            _createdDocumentIds.Add(documentId);
            _createdExhibitIds.Add(exhibitId);
        }

        #endregion

        #region Disconnected State Tests

        [TestMethod]
        public void AddDocument_WhenDisconnected_ReturnsNegativeOne()
        {
            // Arrange
            _api.Disconnect();

            // Act
            int result = _api.AddDocument("EXH001", "Test", _validXmlContent, "TestUser");

            // Assert
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void GetDocument_WhenDisconnected_ReturnsNull()
        {
            // Arrange
            _api.Disconnect();

            // Act
            DocumentInfo result = _api.GetDocument(1);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Helper Methods

        private string GetTestConnectionString()
        {
            // Return your test database connection string
            return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MuseumXMLArchive;Integrated Security=True";
        }

        private string GenerateTestExhibitId()
        {
            // Generate unique exhibit ID in format EXH[][][]
            Random random = new Random();
            return $"EXH{random.Next(100, 999)}{random.Next(100, 999)}";
        }

        private void SetupEventHandlers()
        {
            _api.DocumentAdded += (sender, args) => Console.WriteLine($"Document added: {args.DocumentId}");
            _api.DocumentUpdated += (sender, args) => Console.WriteLine($"Document updated: {args.DocumentId}");
            _api.DocumentDeactivated += (sender, args) => Console.WriteLine($"Document deactivated: {args.DocumentId}");
            _api.DocumentDeleted += (sender, args) => Console.WriteLine($"Document deleted: {args.DocumentId}");
            _api.ErrorOccurred += (sender, args) => Console.WriteLine($"Error occurred: {args.ErrorMessage}");
        }

        #endregion
    }
}
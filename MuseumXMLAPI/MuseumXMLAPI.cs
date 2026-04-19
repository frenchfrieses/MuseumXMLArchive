using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Linq;
using MuseumXMLAPI.Models;
using MuseumXMLAPI.Utilities;

namespace MuseumXMLAPI
{
    /// <summary>
    /// Główna implementacja API do zarządzania archiwum dokumentów XML muzeum
    /// </summary>
    public class MuseumXMLAPI : IMuseumXMLAPI
    {
        #region Private Fields
        private string _connectionString;
        private SqlConnection _connection;
        private bool _isConnected;
        private XMLValidator _xmlValidator;
        private readonly Dictionary<string, DateTime> _searchCache;

        // Event handlers
        public event EventHandler<DocumentEventArgs> DocumentAdded;
        public event EventHandler<DocumentEventArgs> DocumentUpdated;
        public event EventHandler<DocumentEventArgs> DocumentDeactivated;
        public event EventHandler<DocumentEventArgs> DocumentDeleted;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;
        #endregion

        #region Constructor
        public MuseumXMLAPI()
        {
            _searchCache = new Dictionary<string, DateTime>();
            _isConnected = false;
        }
        #endregion

        #region Connection Management

        /// <summary>
        /// Nawiązuje połączenie z bazą danych
        /// </summary>
        /// <param name="connectionString">Ciąg połączenia z bazą danych</param>
        /// <returns>True jeśli połączenie zostało nawiązane pomyślnie</returns>
        public bool Connect(string connectionString)
        {
            try
            {
                _connectionString = connectionString;
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
                _isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas nawiązywania połączenia: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Sprawdza czy połączenie z bazą danych jest aktywne
        /// </summary>
        /// <returns>True jeśli połączenie jest aktywne</returns>
        public bool IsConnected()
        {
            return _isConnected && _connection != null && _connection.State == ConnectionState.Open;
        }

        /// <summary>
        /// Zamyka połączenie z bazą danych
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }
                _isConnected = false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas zamykania połączenia: {ex.Message}"));
            }
        }
        #endregion

        #region Document Management

        /// <summary>
        /// Dodaje nowy dokument XML do archiwum
        /// </summary>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <param name="documentName">Nazwa dokumentu</param>
        /// <param name="xmlContent">Zawartość XML</param>
        /// <param name="createdBy">Autor dokumentu</param>
        /// <returns>ID utworzonego dokumentu lub -1 w przypadku błędu</returns>
        public int AddDocument(string exhibitId, string documentName, string xmlContent, string createdBy)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return -1;
            }

            try
            {
                // Walidacja XML
                if (!ValidateDocument(xmlContent))
                {
                    OnErrorOccurred(new ErrorEventArgs("Dokument XML nie jest poprawny\n" + _xmlValidator.GetValidationReport()));
                    return -1;
                }

                // Walidacja pozostałych parametrów
                if (exhibitId == null)
                {
                    OnErrorOccurred(new ErrorEventArgs("exhibitId nie może mieć wartości null"));
                    return -1;
                }

                if (documentName == null || documentName == "")
                {
                    OnErrorOccurred(new ErrorEventArgs("documentName nie może być puste"));
                    return -1;
                }

                using (var cmd = new SqlCommand("SP_InsertDocument", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ExhibitID", exhibitId);
                    cmd.Parameters.AddWithValue("@DocumentName", documentName);
                    cmd.Parameters.AddWithValue("@XMLContent", xmlContent);
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy);

                    var outputParam = new SqlParameter("@DocumentID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    int documentId = (int)outputParam.Value;

                    // Wywołanie eventu
                    OnDocumentAdded(new DocumentEventArgs(documentId, exhibitId, documentName));

                    return documentId;
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas dodawania dokumentu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas dodawania dokumentu: {ex.Message}"));
            }
            return -1;
        }

        /// <summary>
        /// Pobiera dokument po ID
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <param name="getDeactivated">Flaga oznaczająca czy szukać również w dezaktywowanych</param>
        /// <returns>Informacje o dokumencie lub null w przypadku błędu</returns>
        public DocumentInfo GetDocument(int documentId, bool getDeactivated = false)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return null;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GetDocumentById", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    if( getDeactivated ) cmd.Parameters.AddWithValue("@IncludeInactive", getDeactivated);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return CreateDocumentInfoFromReader(reader);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania dokumentu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania dokumentu: {ex.Message}"));
            }

            return null;
        }

        /// <summary>
        /// Pobiera dokument po ID eksponatu
        /// </summary>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <returns>Informacje o dokumencie lub null w przypadku błędu</returns>
        public DocumentInfo GetDocumentByExhibitId(string exhibitId)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return null;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GetDocumentByExhibitID", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ExhibitID", exhibitId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return CreateDocumentInfoFromReader(reader);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania dokumentu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania dokumentu: {ex.Message}"));
            }

            return null;
        }

        /// <summary>
        /// Aktualizuje istniejący dokument
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <param name="documentName">Nowa nazwa dokumentu</param>
        /// <param name="xmlContent">Nowa zawartość XML</param>
        /// <param name="modifiedBy">Autor modyfikacji</param>
        /// <returns>True jeśli aktualizacja przebiegła pomyślnie</returns>
        public bool UpdateDocument(int documentId, string documentName, string xmlContent, string modifiedBy)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return false;
            }

            try
            {
                // Walidacja XML
                if (!ValidateDocument(xmlContent))
                {
                    OnErrorOccurred(new ErrorEventArgs("Dokument XML nie jest poprawny"));
                    return false;
                }

                using (var cmd = new SqlCommand("SP_UpdateDocument", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.Parameters.AddWithValue("@DocumentName", documentName);
                    cmd.Parameters.AddWithValue("@XMLContent", xmlContent);
                    if (string.IsNullOrWhiteSpace(modifiedBy))
                    {
                        cmd.Parameters.AddWithValue("@ModifiedBy", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                    }
                    cmd.ExecuteNonQuery();

                    var doc = GetDocument(documentId);
                    OnDocumentUpdated(new DocumentEventArgs(documentId, doc?.ExhibitId ?? "", documentName));
                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas aktualizacji dokumentu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas aktualizacji dokumentu: {ex.Message}"));
            }

            return false;
        }

        /// <summary>
        /// Dezaktywuje dokument (oznacza jako nieaktywny)
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <param name="deactivatedBy">Osoba dezaktywująca</param>
        /// <param name="reason">Powód deaktywacji</param>
        /// <returns>True jeśli dezaktywacja przebiegła pomyślnie</returns>
        public bool DeactivateDocument(int documentId, string deactivatedBy, string reason = null)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return false;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_DeactivateDocument", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    Console.WriteLine($"Executing SP with DocumentID: {documentId}, ModifiedBy: {deactivatedBy}");

                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.Parameters.AddWithValue("@ModifiedBy", deactivatedBy);

                    if (!string.IsNullOrEmpty(reason))
                        cmd.Parameters.AddWithValue("@Reason", reason);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas dezaktywacji dokumentu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas dezaktywacji dokumentu: {ex.Message}"));
            }
            return false;
        }

        /// <summary>
        /// Waliduje dokument XML
        /// </summary>
        /// <param name="xmlContent">Zawartość XML do walidacji</param>
        /// <returns>True jeśli dokument jest poprawny</returns>
        public bool ValidateDocument(string xmlContent)
        {
            _xmlValidator = new XMLValidator();
            bool result = _xmlValidator.ValidateExhibitXML(xmlContent);
            Console.WriteLine(_xmlValidator.GetValidationReport());
            return result;
        }

        /// <summary>
        /// Usuwa dokument na podstawie jego ID
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <returns>True jeśli usunięto, false w przypadku błędu</returns>
        public bool DeleteDocument(int documentId)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return false;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_DeleteDocumentByID", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);

                    cmd.ExecuteNonQuery();

                    OnDocumentDeleted(new DocumentEventArgs(documentId, null, null));

                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas usuwania dokumentu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas usuwania dokumentu: {ex.Message}"));
            }

            return false;
        }

        /// <summary>
        /// Usuwa dokument powiązany z danym eksponatem
        /// </summary>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <returns>True jeśli operacja zakończyła się powodzeniem, false w przypadku błędu</returns>
        public bool DeleteDocumentByExhibitId(string exhibitId)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return false;
            }

            try
            {
                int documentId = -1;
                using (var cmd = new SqlCommand("SP_DeleteDocumentsByExhibitID", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ExhibitID", exhibitId);

                    cmd.ExecuteNonQuery();

                    OnDocumentDeleted(new DocumentEventArgs(documentId, null, null));

                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas usuwania dokumentów eksponatu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas usuwania dokumentów eksponatu: {ex.Message}"));
            }

            return false;
        }

        #endregion

        #region Search Operations

        /// <summary>
        /// Wyszukuje dokumenty po kategorii
        /// </summary>
        /// <param name="category">Kategoria eksponatów</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        public List<SearchResult> SearchByCategory(string category)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();

            try
            {
                using (var cmd = new SqlCommand("SP_SearchByCategory", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Category", category);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateSearchResultFromReader(reader));
                        }
                    }
                }

                // Dodaj do cache
                _searchCache[$"category_{category}"] = DateTime.Now;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania po kategorii: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania po kategorii: {ex.Message}"));
            }

            return results;
        }

        /// <summary>
        /// Wyszukuje dokumenty używając wyrażenia XPath
        /// </summary>
        /// <param name="xpathExpression">Wyrażenie XPath</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        public List<SearchResult> SearchByXPath(string xpathExpression)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();

            try
            {
                using (var cmd = new SqlCommand("SP_SearchByXPath", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@XPathExpression", xpathExpression);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateSearchResultFromReader(reader));
                        }
                    }
                }

                _searchCache[$"xpath_{xpathExpression.GetHashCode()}"] = DateTime.Now;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania XPath: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania XPath: {ex.Message}"));
            }

            return results;
        }

        /// <summary>
        /// Wyszukiwanie pełnotekstowe
        /// </summary>
        /// <param name="searchText">Tekst do wyszukania</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        public List<SearchResult> SearchFullText(string searchText)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();

            try
            {
                using (var cmd = new SqlCommand("SP_SearchFullText", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SearchText", searchText);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateSearchResultFromReader(reader));
                        }
                    }
                }

                _searchCache[$"fulltext_{searchText}"] = DateTime.Now;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania pełnotekstowego: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania pełnotekstowego: {ex.Message}"));
            }

            return results;
        }

        /// <summary>
        /// Wyszukuje dokumenty po okresie historycznym
        /// </summary>
        /// <param name="period">Okres historyczny</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        public List<SearchResult> SearchByPeriod(string period)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();

            try
            {
                using (var cmd = new SqlCommand("SP_SearchByPeriod", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Period", period);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateSearchResultFromReader(reader));
                        }
                    }
                }

                _searchCache[$"period_{period}"] = DateTime.Now;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania po okresie: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania po okresie: {ex.Message}"));
            }

            return results;
        }

        /// <summary>
        /// Wyszukuje dokumenty po stanie zachowania
        /// </summary>
        /// <param name="condition">Stan zachowania</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        public List<SearchResult> SearchByCondition(string condition)
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();

            try
            {
                using (var cmd = new SqlCommand("SP_SearchByCondition", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Condition", condition);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateSearchResultFromReader(reader));
                        }
                    }
                }

                _searchCache[$"condition_{condition}"] = DateTime.Now;
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas wyszukiwania po stanie: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas wyszukiwania po stanie: {ex.Message}"));
            }

            return results;
        }
        #endregion

        #region Reporting

        /// <summary>
        /// Generuje raport archiwum
        /// </summary>
        /// <returns>Raport archiwum</returns>
        public ArchiveReport GenerateArchiveReport()
        {
            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return null;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GenerateArchiveReport", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ArchiveReport
                            {
                                TotalDocuments = reader.GetInt32(reader.GetOrdinal("TotalDocuments")),
                                ActiveDocuments = reader.GetInt32(reader.GetOrdinal("ActiveDocuments")),
                                InactiveDocuments = reader.GetInt32(reader.GetOrdinal("InactiveDocuments")),
                                DocumentsOnDisplay = reader.GetInt32(reader.GetOrdinal("DocumentsOnDisplay")),
                                DocumentsInStorage = reader.GetInt32(reader.GetOrdinal("DocumentsInStorage")),
                                DocumentsRequiringConservation = reader.GetInt32(reader.GetOrdinal("DocumentsRequiringConservation"))
                            };
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas generowania raportu: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas generowania raportu: {ex.Message}"));
            }

            return null;
        }

        /// <summary>
        /// Pobiera statystyki kategorii
        /// </summary>
        /// <returns>Słownik z liczbą dokumentów w każdej kategorii</returns>
        public Dictionary<string, int> GetCategoryStatistics()
        {
            var statistics = new Dictionary<string, int>();

            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return statistics;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GetCategoryStatistics", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            statistics[reader.GetString(reader.GetOrdinal("Category"))] = reader.GetInt32(reader.GetOrdinal("Count"));
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania statystyk: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania statystyk: {ex.Message}"));
            }

            return statistics;
        }

        /// <summary>
        /// Pobiera dokumenty wymagające konserwacji
        /// </summary>
        /// <returns>Lista dokumentów wymagających konserwacji</returns>
        public List<DocumentInfo> GetDocumentsRequiringConservation()
        {
            var results = new List<DocumentInfo>();

            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return results;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GetDocumentByIdsRequiringConservation", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateDocumentInfoFromReader(reader));
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania dokumentów po konserwacji: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania dokumentów do konserwacji: {ex.Message}"));
            }

            return results;
        }

        /// <summary>
        /// Pobiera ostatnio dodane dokumenty
        /// </summary>
        /// <param name="days">Liczba dni wstecz</param>
        /// <returns>Lista ostatnio dodanych dokumentów</returns>
        public List<DocumentInfo> GetRecentDocuments(int days = 30)
        {
            var results = new List<DocumentInfo>();

            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return results;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GetRecentDocuments", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Days", days);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(CreateDocumentInfoFromReader(reader));
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania ostatnich dokumentów: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania ostatnich dokumentów: {ex.Message}"));
            }

            return results;
        }
        #endregion

        #region Utility Methods

        /// <summary>
        /// Czyści wygasłe wpisy z cache wyszukiwania
        /// </summary>
        /// <param name="maxAge">Maksymalny wiek wpisu w minutach</param>
        public void CleanExpiredSearchCache(int maxAge = 60)
        {
            try
            {
                var expiredKeys = _searchCache
                    .Where(kvp => DateTime.Now.Subtract(kvp.Value).TotalMinutes > maxAge)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _searchCache.Remove(key);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas czyszczenia cache: {ex.Message}"));
            }
        }

        /// <summary>
        /// Pobiera log operacji na dokumencie
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <returns>Lista operacji wykonanych na dokumencie</returns>
        public List<OperationLog> GetDocumentOperationLog(int documentId)
        {
            var operations = new List<OperationLog>();

            if (!IsConnected())
            {
                OnErrorOccurred(new ErrorEventArgs("Brak połączenia z bazą danych"));
                return operations;
            }

            try
            {
                using (var cmd = new SqlCommand("SP_GetDocumentByIdOperationLog", _connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            operations.Add(new OperationLog
                            {
                                LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentID")),
                                OperationType = reader.GetString(reader.GetOrdinal("OperationType")),
                                OperationDescription = reader.GetString(reader.GetOrdinal("OperationDescription")),
                                ExecutedBy = reader.GetString(reader.GetOrdinal("ExecutedBy")),
                                ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage"))
                            });
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"SQL Error Message: {sqlEx.Message}");
                OnErrorOccurred(new ErrorEventArgs($"SQL błąd podczas pobierania logu operacji: {sqlEx.Message}"));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas pobierania logu operacji: {ex.Message}"));
            }

            return operations;
        }

        /// <summary>
        /// Eksportuje dokument do pliku
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <param name="filePath">Ścieżka do pliku</param>
        /// <returns>True jeśli eksport przebiegł pomyślnie</returns>
        public bool ExportDocumentToFile(int documentId, string filePath)
        {
            try
            {
                var document = GetDocument(documentId);
                if (document == null)
                {
                    OnErrorOccurred(new ErrorEventArgs("Nie znaleziono dokumentu"));
                    return false;
                }

                File.WriteAllText(filePath, document.ToFormattedXmlString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas eksportu dokumentu: {ex.Message}"));
                return false;
            }
        }

        /// <summary>
        /// Importuje dokument z pliku
        /// </summary>
        /// <param name="filePath">Ścieżka do pliku</param>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <param name="documentName">Nazwa dokumentu</param>
        /// <param name="createdBy">Autor importu</param>
        /// <returns>ID utworzonego dokumentu lub -1 w przypadku błędu</returns>
        public int ImportDocumentFromFile(string filePath, string exhibitId, string documentName, string createdBy)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    OnErrorOccurred(new ErrorEventArgs("Plik nie istnieje"));
                    return -1;
                }

                string xmlContent = File.ReadAllText(filePath, Encoding.UTF8);
                return AddDocument(exhibitId, documentName, xmlContent, createdBy);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ErrorEventArgs($"Błąd podczas importu dokumentu: {ex.Message}"));
                return -1;
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Tworzy obiekt DocumentInfo z SqlDataReader
        /// </summary>
        private DocumentInfo CreateDocumentInfoFromReader(SqlDataReader reader)
        {
            return new DocumentInfo
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentID")),
                ExhibitId = reader.GetString(reader.GetOrdinal("ExhibitID")),
                DocumentName = reader.GetString(reader.GetOrdinal("DocumentName")),

                XMLContent = XmlStringIntoXmlDocument(reader.GetString(reader.GetOrdinal("XMLContent"))),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate")),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader.GetString(reader.GetOrdinal("ModifiedBy")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }

        private XmlDocument XmlStringIntoXmlDocument(string XMLString)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(XMLString);
            return xmlDocument;
        }

        /// <summary>
        /// Tworzy obiekt SearchResult z SqlDataReader
        /// </summary>
        private SearchResult CreateSearchResultFromReader(SqlDataReader reader)
        {
            return new SearchResult
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentID")),
                ExhibitId = reader.GetString(reader.GetOrdinal("ExhibitID")),
                DocumentName = reader.GetString(reader.GetOrdinal("DocumentName")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                Period = reader.GetString(reader.GetOrdinal("Period")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                OnDisplay = reader.GetBoolean(reader.GetOrdinal("OnDisplay")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
            };
        }

        /// <summary>
        /// Wywołuje event DocumentAdded
        /// </summary>
        private void OnDocumentAdded(DocumentEventArgs e)
        {
            DocumentAdded?.Invoke(this, e);
        }

        /// <summary>
        /// Wywołuje event DocumentUpdated
        /// </summary>
        private void OnDocumentUpdated(DocumentEventArgs e)
        {
            DocumentUpdated?.Invoke(this, e);
        }

        /// <summary>
        /// Wywołuje event DocumentDeactivated
        /// </summary>
        private void OnDocumentDeactivated(DocumentEventArgs e)
        {
            DocumentDeactivated?.Invoke(this, e);
        }

        /// <summary>
        /// Wywołuje event ErrorOccurred
        /// </summary>
        private void OnDocumentDeleted(DocumentEventArgs e)
        {
            DocumentDeleted?.Invoke(this, e);
        }

        /// <summary>
        /// Wywołuje event ErrorOccurred
        /// </summary>
        private void OnErrorOccurred(ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Disconnect();
        }
        #endregion
    }
}
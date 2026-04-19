using System;
using System.Collections.Generic;
using System.Xml;
using MuseumXMLAPI.Models;

namespace MuseumXMLAPI
{
    /// <summary>
    /// Interfejs główny API dla zarządzania dokumentami XML w archiwum muzealnym
    /// </summary>
    public interface IMuseumXMLAPI : IDisposable
    {
        #region Connection Management
        /// <summary>
        /// Łączy się z bazą danych
        /// </summary>
        /// <param name="connectionString">String połączenia z SQL Server</param>
        /// <returns>True jeśli połączenie nawiązano pomyślnie</returns>
        bool Connect(string connectionString);

        /// <summary>
        /// Sprawdza status połączenia z bazą danych
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// Rozłącza się z bazą danych
        /// </summary>
        void Disconnect();
        #endregion

        #region Document Management
        /// <summary>
        /// Dodaje nowy dokument XML do archiwum
        /// </summary>
        /// <param name="exhibitId">Unikalny identyfikator eksponatu</param>
        /// <param name="documentName">Nazwa dokumentu</param>
        /// <param name="xmlContent">Zawartość XML dokumentu</param>
        /// <param name="createdBy">Użytkownik tworzący dokument</param>
        /// <returns>ID utworzonego dokumentu lub -1 w przypadku błędu</returns>
        int AddDocument(string exhibitId, string documentName, string xmlContent, string createdBy);

        /// <summary>
        /// Pobiera dokument po ID
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <param name="getDeactivated">Flaga oznaczająca czy szukać również w dezaktywowanych</param>
        /// <returns>Informacje o dokumencie lub null w przypadku błędu</returns>
        DocumentInfo GetDocument(int documentId, bool getDeactivated = false);

        /// <summary>
        /// Pobiera dokument po ExhibitID
        /// </summary>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <returns>Informacje o dokumencie lub null jeśli nie znaleziono</returns>
        DocumentInfo GetDocumentByExhibitId(string exhibitId);

        /// <summary>
        /// Aktualizuje istniejący dokument
        /// </summary>
        /// <param name="documentId">ID dokumentu do aktualizacji</param>
        /// <param name="documentName">Nowa nazwa dokumentu (opcjonalne)</param>
        /// <param name="xmlContent">Nowa zawartość XML (opcjonalne)</param>
        /// <param name="modifiedBy">Użytkownik modyfikujący</param>
        /// <returns>True jeśli aktualizacja powiodła się</returns>
        bool UpdateDocument(int documentId, string documentName, string xmlContent, string modifiedBy);

        /// <summary>
        /// Deaktywuje dokument (soft delete)
        /// </summary>
        /// <param name="documentId">ID dokumentu do deaktywacji</param>
        /// <param name="modifiedBy">Użytkownik deaktywujący</param>
        /// <param name="reason">Powód deaktywacji</param>
        /// <returns>True jeśli deaktywacja powiodła się</returns>
        bool DeactivateDocument(int documentId, string deactivatedBy, string reason = null);

        /// <summary>
        /// Waliduje dokument XML względem schematu
        /// </summary>
        /// <param name="xmlContent">Zawartość XML dokumentu do walidacji</param>
        /// <returns>True jeśli dokument jest poprawny</returns>
        bool ValidateDocument(string xmlContent);

        /// <summary>
        /// Usuwa dokument na podstawie jego ID
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <returns>True jeśli usunięto, false w przypadku błędu</returns>
        bool DeleteDocument(int documentId);

        /// <summary>
        /// Usuwa dokument powiązany z danym eksponatem
        /// </summary>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <returns>True jeśli operacja zakończyła się powodzeniem, false w przypadku błędu</returns>
        bool DeleteDocumentByExhibitId(string exhibitId);

        #endregion

        #region Search Operations
        /// <summary>
        /// Wyszukuje dokumenty po kategorii
        /// </summary>
        /// <param name="category">Kategoria eksponatu</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        List<SearchResult> SearchByCategory(string category);

        /// <summary>
        /// Wyszukuje dokumenty używając zapytania XPath
        /// </summary>
        /// <param name="xpathQuery">Zapytanie XPath</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        List<SearchResult> SearchByXPath(string xpathQuery);

        /// <summary>
        /// Wyszukuje dokumenty używając pełnotekstowego wyszukiwania
        /// </summary>
        /// <param name="searchText">Tekst do wyszukania</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        List<SearchResult> SearchFullText(string searchText);

        /// <summary>
        /// Wyszukuje dokumenty według okresu/epoki
        /// </summary>
        /// <param name="period">Okres historyczny</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        List<SearchResult> SearchByPeriod(string period);

        /// <summary>
        /// Wyszukuje dokumenty według stanu zachowania
        /// </summary>
        /// <param name="condition">Stan zachowania (Excellent, Good, Fair, Poor, Critical)</param>
        /// <returns>Lista wyników wyszukiwania</returns>
        List<SearchResult> SearchByCondition(string condition);
        #endregion

        #region Reporting
        /// <summary>
        /// Generuje raport statystyczny archiwum
        /// </summary>
        /// <returns>Obiekt z danymi raportu</returns>
        ArchiveReport GenerateArchiveReport();

        /// <summary>
        /// Pobiera statystyki według kategorii
        /// </summary>
        /// <returns>Słownik kategoria -> liczba dokumentów</returns>
        Dictionary<string, int> GetCategoryStatistics();

        /// <summary>
        /// Pobiera listę dokumentów wymagających konserwacji
        /// </summary>
        /// <returns>Lista dokumentów w złym stanie</returns>
        List<DocumentInfo> GetDocumentsRequiringConservation();

        /// <summary>
        /// Pobiera listę ostatnio dodanych dokumentów
        /// </summary>
        /// <param name="days">Liczba dni wstecz</param>
        /// <returns>Lista najnowszych dokumentów</returns>
        List<DocumentInfo> GetRecentDocuments(int days = 30);
        #endregion

        #region Utility Methods
        /// <summary>
        /// Czyści wygasły cache wyszukiwań
        /// </summary>
        /// <param name="maxAge">Maksymalny wiek wpisu w minutach</param>
        /// <returns>Liczba usuniętych rekordów cache</returns>
        void CleanExpiredSearchCache(int maxAge = 60);

        /// <summary>
        /// Pobiera logi operacji dla dokumentu
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <returns>Lista logów operacji</returns>
        List<OperationLog> GetDocumentOperationLog(int documentId);

        /// <summary>
        /// Eksportuje dokument do pliku XML
        /// </summary>
        /// <param name="documentId">ID dokumentu</param>
        /// <param name="filePath">Ścieżka docelowa</param>
        /// <returns>True jeśli eksport się powiódł</returns>
        bool ExportDocumentToFile(int documentId, string filePath);

        /// <summary>
        /// Importuje dokument z pliku XML
        /// </summary>
        /// <param name="filePath">Ścieżka do pliku</param>
        /// <param name="exhibitId">ID eksponatu</param>
        /// <param name="documentName">Nazwa dokumentu</param>
        /// <param name="createdBy">Użytkownik importujący</param>
        /// <returns>ID utworzonego dokumentu lub -1 w przypadku błędu</returns>
        int ImportDocumentFromFile(string filePath, string exhibitId, string documentName, string createdBy = null);
        #endregion

        #region Events
        /// <summary>
        /// Zdarzenie wywoływane po dodaniu nowego dokumentu
        /// </summary>
        event EventHandler<DocumentEventArgs> DocumentAdded;

        /// <summary>
        /// Zdarzenie wywoływane po aktualizacji dokumentu
        /// </summary>
        event EventHandler<DocumentEventArgs> DocumentUpdated;

        /// <summary>
        /// Zdarzenie wywoływane po deaktywacji dokumentu
        /// </summary>
        event EventHandler<DocumentEventArgs> DocumentDeactivated;

        /// <summary>
        /// Zdarzenie wywoływane po usunięciu dokumentu
        /// </summary>
        event EventHandler<DocumentEventArgs> DocumentDeleted;

        /// <summary>
        /// Zdarzenie wywoływane w przypadku błędu
        /// </summary>
        event EventHandler<ErrorEventArgs> ErrorOccurred;
        #endregion
    }

    #region Event Args Classes
    /// <summary>
    /// Argumenty zdarzenia dla operacji na dokumentach
    /// </summary>
    public class DocumentEventArgs : EventArgs
    {
        public int DocumentId { get; set; }
        public string ExhibitId { get; set; }
        public string DocumentName { get; set; }
        public DateTime Timestamp { get; set; }
        public DocumentEventArgs(int documentId, string exhibitId, string documentName)
        {
            DocumentId = documentId;
            ExhibitId = exhibitId;
            DocumentName = documentName;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Argumenty zdarzenia dla błędów
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public string Operation { get; set; }
        public DateTime Timestamp { get; set; }

        public ErrorEventArgs(string errorMessage, Exception exception = null, string operation = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            Operation = operation;
            Timestamp = DateTime.Now;
        }
    }
    #endregion
}
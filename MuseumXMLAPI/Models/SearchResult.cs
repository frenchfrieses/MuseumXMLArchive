using System;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Wynik wyszukiwania dokumentów
    /// </summary>
    public class SearchResult
    {
        public int DocumentId { get; set; }
        public string ExhibitId { get; set; }
        public string DocumentName { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Creator { get; set; }
        public string Period { get; set; }
        public string Status { get; set; }
        public bool OnDisplay { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string MatchedFragment { get; set; }
        public double RelevanceScore { get; set; }

        public SearchResult()
        {
            RelevanceScore = 1.0;
        }

        /// <summary>
        /// Zwraca opis wyniku wyszukiwania
        /// </summary>
        public override string ToString()
        {
            return $"{Title} ({Category}) - {Creator}";
        }
    }
}

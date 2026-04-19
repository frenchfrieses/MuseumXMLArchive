using System;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Kryteria wyszukiwania
    /// </summary>
    public class SearchCriteria
    {
        public string Category { get; set; }
        public string Period { get; set; }
        public string Creator { get; set; }
        public string Condition { get; set; }
        public string Status { get; set; }
        public bool? OnDisplay { get; set; }
        public string FullTextSearch { get; set; }
        public string XPathQuery { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ModifiedAfter { get; set; }
        public DateTime? ModifiedBefore { get; set; }

        /// <summary>
        /// Sprawdza czy kryteria są puste
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Category) &&
                   string.IsNullOrEmpty(Period) &&
                   string.IsNullOrEmpty(Creator) &&
                   string.IsNullOrEmpty(Condition) &&
                   string.IsNullOrEmpty(Status) &&
                   !OnDisplay.HasValue &&
                   string.IsNullOrEmpty(FullTextSearch) &&
                   string.IsNullOrEmpty(XPathQuery) &&
                   !CreatedAfter.HasValue &&
                   !CreatedBefore.HasValue &&
                   !ModifiedAfter.HasValue &&
                   !ModifiedBefore.HasValue;
        }
    }
}

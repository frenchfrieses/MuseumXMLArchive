using System;
using System.Collections.Generic;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Raport statystyczny archiwum
    /// </summary>
    public class ArchiveReport
    {
        public int TotalDocuments { get; set; }
        public int ActiveDocuments { get; set; }
        public int InactiveDocuments { get; set; }
        public int DocumentsOnDisplay { get; set; }
        public int DocumentsInStorage { get; set; }
        public int DocumentsRequiringConservation { get; set; }
        public DateTime ReportGenerated { get; set; }

        public Dictionary<string, int> CategoryStatistics { get; set; }
        public Dictionary<string, int> StatusStatistics { get; set; }
        public Dictionary<string, int> ConditionStatistics { get; set; }
        public Dictionary<string, int> PeriodStatistics { get; set; }

        public List<DocumentInfo> RecentDocuments { get; set; }
        public List<DocumentInfo> DocumentsNeedingAttention { get; set; }

        public ArchiveReport()
        {
            CategoryStatistics = new Dictionary<string, int>();
            StatusStatistics = new Dictionary<string, int>();
            ConditionStatistics = new Dictionary<string, int>();
            PeriodStatistics = new Dictionary<string, int>();
            RecentDocuments = new List<DocumentInfo>();
            DocumentsNeedingAttention = new List<DocumentInfo>();
            ReportGenerated = DateTime.Now;
        }

        /// <summary>
        /// Zwraca podsumowanie raportu
        /// </summary>
        public string GetSummary()
        {
            return $"Total: {TotalDocuments}, Active: {ActiveDocuments}, On Display: {DocumentsOnDisplay}, " +
                   $"Need Conservation: {DocumentsRequiringConservation}";
        }
    }

}

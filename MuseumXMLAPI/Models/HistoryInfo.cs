using System;
using System.Collections.Generic;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Informacje o historii eksponatu
    /// </summary>
    public class HistoryInfo
    {
        public DateTime? AcquisitionDate { get; set; }
        public string AcquisitionMethod { get; set; }
        public string AcquisitionSource { get; set; }
        public decimal? AcquisitionPrice { get; set; }
        public string AcquisitionCurrency { get; set; }
        public string Provenance { get; set; }
        public List<ExhibitionInfo> Exhibitions { get; set; }

        public HistoryInfo()
        {
            Exhibitions = new List<ExhibitionInfo>();
        }

        /// <summary>
        /// Zwraca opis nabycia
        /// </summary>
        public string GetAcquisitionSummary()
        {
            var summary = AcquisitionMethod ?? "Unknown method";
            if (AcquisitionDate.HasValue)
            {
                summary += $" ({AcquisitionDate.Value:yyyy-MM-dd})";
            }
            if (!string.IsNullOrEmpty(AcquisitionSource))
            {
                summary += $" from {AcquisitionSource}";
            }
            return summary;
        }
    }
}

using System;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Informacje o wystawie
    /// </summary>
    public class ExhibitionInfo
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Sprawdza czy wystawa jest aktywna
        /// </summary>
        public bool IsActive()
        {
            var now = DateTime.Now;
            return now >= StartDate && now <= EndDate;
        }

        /// <summary>
        /// Zwraca opis wystawy
        /// </summary>
        public override string ToString()
        {
            return $"{Name} at {Location} ({StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd})";
        }
    }
}

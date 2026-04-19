using System;
using System.Collections.Generic;
using System.Xml;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Informacje o lokalizacji eksponatu
    /// </summary>
    public class LocationInfo
    {
        public string Building { get; set; }
        public string Floor { get; set; }
        public string Room { get; set; }
        public string Display { get; set; }
        public string StorageLocation { get; set; }
        public bool OnDisplay { get; set; }

        /// <summary>
        /// Zwraca pełny opis lokalizacji
        /// </summary>
        public override string ToString()
        {
            if (OnDisplay)
            {
                return $"{Building}, {Floor}, {Room}, {Display}";
            }
            else
            {
                return $"{Building}, {Room}" +
                       (string.IsNullOrEmpty(StorageLocation) ? "" : $", {StorageLocation}");
            }
        }
    }
}

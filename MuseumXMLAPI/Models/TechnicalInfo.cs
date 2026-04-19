using System.Collections.Generic;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Informacje techniczne o eksponacie
    /// </summary>
    public class TechnicalInfo
    {
        public string Height { get; set; }
        public string Width { get; set; }
        public string Depth { get; set; }
        public string Diameter { get; set; }
        public string DimensionUnit { get; set; }
        public string Weight { get; set; }
        public List<string> Materials { get; set; }
        public string Technique { get; set; }
        public string Condition { get; set; }
        public string ConservationNotes { get; set; }

        public TechnicalInfo()
        {
            Materials = new List<string>();
        }

        /// <summary>
        /// Sprawdza czy eksponat wymaga konserwacji
        /// </summary>
        public bool NeedsConservation()
        {
            return Condition?.ToLower() == "poor" ||
                   Condition?.ToLower() == "critical" ||
                   !string.IsNullOrEmpty(ConservationNotes);
        }

        /// <summary>
        /// Zwraca opis wymiarów
        /// </summary>
        public string GetDimensions()
        {
            var dims = new List<string>();
            if (!string.IsNullOrEmpty(Height)) dims.Add($"H: {Height}");
            if (!string.IsNullOrEmpty(Width)) dims.Add($"W: {Width}");
            if (!string.IsNullOrEmpty(Depth)) dims.Add($"D: {Depth}");
            if (!string.IsNullOrEmpty(Diameter)) dims.Add($"Ø: {Diameter}");

            var result = string.Join(", ", dims);
            if (!string.IsNullOrEmpty(DimensionUnit) && !string.IsNullOrEmpty(result))
            {
                result += $" ({DimensionUnit})";
            }

            return result;
        }
    }

}

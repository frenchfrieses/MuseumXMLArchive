using System;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Log operacji wykonanej na dokumencie
    /// </summary>
    public class OperationLog
    {
        public int LogId { get; set; }
        public int? DocumentId { get; set; }
        public string OperationType { get; set; }
        public string OperationDescription { get; set; }
        public string ExecutedBy { get; set; }
        public DateTime ExecutedDate { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public OperationLog()
        {
            Success = true;
            ExecutedDate = DateTime.Now;
        }

        /// <summary>
        /// Zwraca opis operacji
        /// </summary>
        public override string ToString()
        {
            return $"{ExecutedDate:yyyy-MM-dd HH:mm:ss} - {OperationType}: {OperationDescription}";
        }
    }

}

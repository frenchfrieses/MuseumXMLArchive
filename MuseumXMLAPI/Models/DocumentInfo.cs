using System;
using System.Xml;

namespace MuseumXMLAPI.Models
{
    /// <summary>
    /// Informacje o dokumencie w archiwum
    /// </summary>
    public class DocumentInfo
    {
        public int DocumentId { get; set; }
        public string ExhibitId { get; set; }
        public string DocumentName { get; set; }
        public XmlDocument XMLContent { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string ValidationStatus { get; set; }
        public string Notes { get; set; }

        // Właściwości wyprowadzone z XML
        public string Title { get; set; }
        public string Category { get; set; }
        public string Creator { get; set; }
        public string Period { get; set; }
        public string Status { get; set; }
        public bool OnDisplay { get; set; }

        public DocumentInfo()
        {
            XMLContent = new XmlDocument();
            IsActive = true;
            ValidationStatus = "Pending";
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Wyprowadza podstawowe informacje z dokumentu XML
        /// </summary>
        public void ExtractBasicInfoFromXML()
        {
            if (XMLContent?.DocumentElement == null) return;

            try
            {
                var nsmgr = new XmlNamespaceManager(XMLContent.NameTable);
                nsmgr.AddNamespace("ns", "http://museum.example.com/exhibit");

                var titleNode = XMLContent.SelectSingleNode("//ns:Title", nsmgr);
                Title = titleNode?.InnerText ?? "";

                var categoryNode = XMLContent.SelectSingleNode("//ns:Category", nsmgr);
                Category = categoryNode?.InnerText ?? "";

                var creatorNode = XMLContent.SelectSingleNode("//ns:Creator", nsmgr);
                Creator = creatorNode?.InnerText ?? "";

                var periodNode = XMLContent.SelectSingleNode("//ns:Period", nsmgr);
                Period = periodNode?.InnerText ?? "";

                var statusAttr = XMLContent.DocumentElement.GetAttribute("status");
                Status = statusAttr ?? "";

                var onDisplayAttr = XMLContent.SelectSingleNode("//ns:Location/@onDisplay", nsmgr);
                OnDisplay = onDisplayAttr?.Value?.ToLower() == "true";
            }
            catch (Exception ex)
            {
                // Log error but don't throw - basic info extraction is optional
                Console.WriteLine($"Error extracting basic info from XML: {ex.Message}");
            }
        }

        /// <summary>
        /// Zwraca sformatowaną zawartość XML z wcięciami
        /// </summary>
        public string ToFormattedXmlString()
        {
            if (XMLContent?.DocumentElement == null)
            {
                return "<empty>";
            }

            try
            {
                using (var stringWriter = new System.IO.StringWriter())
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = false
                }))
                {
                    XMLContent.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"<error formatting XML: {ex.Message}>";
            }
        }

        /// <summary>
        /// Zwraca czy XMLContent jest pusty
        /// </summary>
        public bool IsXMLContentEmpty()
        {
            if (XMLContent?.DocumentElement == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Zwraca opis dokumentu
        /// </summary>
        public override string ToString()
        {
            return $"{DocumentName} ({ExhibitId}) - {Title}";
        }
    }
}

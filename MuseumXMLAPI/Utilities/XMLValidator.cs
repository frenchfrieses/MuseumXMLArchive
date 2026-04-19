using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace MuseumXMLAPI.Utilities
{
    /// <summary>
    /// Klasa do walidacji dokumentów XML eksponatów muzealnych
    /// </summary>
    public class XMLValidator
    {
        private const string MUSEUM_NAMESPACE = "http://museum.example.com/exhibit";
        private readonly XmlSchemaSet _schemaSet;
        private readonly List<string> _validationErrors;

        public XMLValidator()
        {
            _schemaSet = new XmlSchemaSet();
            _validationErrors = new List<string>();
            InitializeSchema();
        }

        /// <summary>
        /// Lista błędów walidacji z ostatniej operacji
        /// </summary>
        public IReadOnlyList<string> ValidationErrors => _validationErrors.AsReadOnly();

        /// <summary>
        /// Czy ostatnia walidacja zakończyła się sukcesem
        /// </summary>
        public bool IsValid => _validationErrors.Count == 0;

        /// <summary>
        /// Waliduje dokument XML eksponatu
        /// </summary>
        /// <param name="xmlContent">Zawartość XML do walidacji</param>
        /// <returns>True jeśli dokument jest poprawny</returns>
        public bool ValidateExhibitXML(string xmlContent)
        {
            _validationErrors.Clear();

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                _validationErrors.Add("Zawartość XML nie może być pusta");
                return false;
            }

            if (xmlContent == "<empty>")
            {
                _validationErrors.Add("Zawartość XML nie może być pusta");
                return false;
            }

            try
            {
                // Parsowanie XML z walidacją przeciwko schematowi
                var doc = XDocument.Parse(xmlContent);

                // Walidacja przeciwko schematowi XSD - główna walidacja
                if (!ValidateAgainstSchema(doc))
                    return false;

                // Dodatkowa walidacja biznesowa
                if (!ValidateBusinessRules(doc))
                    return false;

                return true;
            }
            catch (XmlException ex)
            {
                _validationErrors.Add($"Błąd parsowania XML: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Nieoczekiwany błąd: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waliduje dokument przeciwko schematowi XSD
        /// </summary>
        private bool ValidateAgainstSchema(XDocument doc)
        {
            try
            {
                var schemaValidationErrors = new List<string>();

                // Ustawienie walidacji przeciwko schematowi
                doc.Validate(_schemaSet, (sender, e) =>
                {
                    if (e.Severity == XmlSeverityType.Error)
                    {
                        schemaValidationErrors.Add($"Błąd schematu: {e.Message}");
                    }
                    else if (e.Severity == XmlSeverityType.Warning)
                    {
                        schemaValidationErrors.Add($"Ostrzeżenie schematu: {e.Message}");
                    }
                });

                _validationErrors.AddRange(schemaValidationErrors);
                return schemaValidationErrors.Count == 0;
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Błąd walidacji schematu: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waliduje reguły biznesowe specyficzne dla muzeum
        /// </summary>
        private bool ValidateBusinessRules(XDocument doc)
        {
            var ns = XNamespace.Get(MUSEUM_NAMESPACE);
            var exhibit = doc.Root;
            bool isValid = true;

            if (exhibit == null)
            {
                _validationErrors.Add("Brak głównego elementu Exhibit");
                return false;
            }

            // Walidacja ID eksponatu
            isValid &= ValidateExhibitID(exhibit);

            // Walidacja BasicInfo
            isValid &= ValidateBasicInfo(exhibit.Element(ns + "BasicInfo"), ns);

            // Walidacja wymiarów
            isValid &= ValidateDimensions(exhibit.Element(ns + "Technical")?.Element(ns + "Dimensions"), ns);

            // Walidacja lokalizacji
            isValid &= ValidateLocation(exhibit.Element(ns + "Location"), ns);

            // Walidacja dat
            isValid &= ValidateDates(exhibit.Element(ns + "History"), ns);

            // Walidacja mediów
            isValid &= ValidateMedia(exhibit.Element(ns + "Media"), ns);

            // Walidacja stanu zachowania
            isValid &= ValidateCondition(exhibit.Element(ns + "Technical"), ns);

            return isValid;
        }

        /// <summary>
        /// Waliduje ID eksponatu
        /// </summary>
        private bool ValidateExhibitID(XElement exhibit)
        {
            var id = exhibit.Attribute("id")?.Value;

            if (string.IsNullOrEmpty(id))
            {
                _validationErrors.Add("Atrybut 'id' jest wymagany");
                return false;
            }

            if (!IsValidExhibitID(id))
            {
                _validationErrors.Add("Niepoprawny format ID eksponatu (oczekiwany: EXH001, EXH002, etc.)");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Waliduje sekcję BasicInfo
        /// </summary>
        private bool ValidateBasicInfo(XElement basicInfo, XNamespace ns)
        {
            if (basicInfo == null)
            {
                _validationErrors.Add("Sekcja BasicInfo jest wymagana");
                return false;
            }

            var title = basicInfo.Element(ns + "Title")?.Value;
            var category = basicInfo.Element(ns + "Category")?.Value;

            if (string.IsNullOrWhiteSpace(title))
            {
                _validationErrors.Add("Tytuł eksponatu jest wymagany");
                return false;
            }

            if (title.Length > 255)
            {
                _validationErrors.Add("Tytuł eksponatu nie może być dłuższy niż 255 znaków");
                return false;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                _validationErrors.Add("Kategoria eksponatu jest wymagana");
                return false;
            }

            // Walidacja daty stworzenia jeśli istnieje
            var dateCreated = basicInfo.Element(ns + "DateCreated")?.Value;
            if (!string.IsNullOrEmpty(dateCreated))
            {
                if (!IsValidHistoricalDate(dateCreated))
                {
                    _validationErrors.Add($"Niepoprawna data stworzenia: {dateCreated}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Waliduje wymiary eksponatu
        /// </summary>
        private bool ValidateDimensions(XElement dimensions, XNamespace ns)
        {
            if (dimensions == null) return true; // Wymiary są opcjonalne

            var unit = dimensions.Attribute("unit")?.Value;
            if (string.IsNullOrEmpty(unit))
            {
                _validationErrors.Add("Jednostka wymiarów jest wymagana");
                return false;
            }

            var validUnits = new[] { "cm", "mm", "m", "in", "ft" };
            if (!validUnits.Contains(unit))
            {
                _validationErrors.Add($"Niepoprawna jednostka wymiarów: {unit}. Dozwolone: {string.Join(", ", validUnits)}");
                return false;
            }

            // Sprawdzenie czy wartości wymiarów są liczbami
            var dimensionElements = new[] { "Height", "Width", "Depth", "Diameter" };
            bool hasDimensions = false;

            foreach (var dimName in dimensionElements)
            {
                var dimElement = dimensions.Element(ns + dimName);
                if (dimElement != null)
                {
                    hasDimensions = true;
                    if (!double.TryParse(dimElement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double value) || value <= 0.0)
                    {
                        _validationErrors.Add($"Niepoprawna wartość wymiaru {dimName}: {dimElement.Value}");
                        return false;
                    }

                    if (value > 10000) // Maximum reasonable dimension
                    {
                        _validationErrors.Add($"Wymiar {dimName} wydaje się być za duży: {value} {unit}");
                        return false;
                    }
                }
            }

            if (!hasDimensions)
            {
                _validationErrors.Add("Jeśli sekcja Dimensions istnieje, musi zawierać co najmniej jeden wymiar");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Waliduje lokalizację eksponatu
        /// </summary>
        private bool ValidateLocation(XElement location, XNamespace ns)
        {
            if (location == null)
            {
                _validationErrors.Add("Sekcja Location jest wymagana");
                return false;
            }

            var onDisplay = location.Attribute("onDisplay")?.Value;
            if (string.IsNullOrEmpty(onDisplay))
            {
                _validationErrors.Add("Atrybut 'onDisplay' jest wymagany w lokalizacji");
                return false;
            }

            var building = location.Element(ns + "Building")?.Value;
            if (string.IsNullOrWhiteSpace(building))
            {
                _validationErrors.Add("Budynek jest wymagany w lokalizacji");
                return false;
            }

            var room = location.Element(ns + "Room")?.Value;
            if (string.IsNullOrWhiteSpace(room))
            {
                _validationErrors.Add("Pokój jest wymagany w lokalizacji");
                return false;
            }

            // Jeśli eksponat jest na wystawie, sprawdź czy ma określone miejsce ekspozycji
            if (onDisplay == "true")
            {
                var display = location.Element(ns + "Display")?.Value;
                if (string.IsNullOrWhiteSpace(display))
                {
                    _validationErrors.Add("Jeśli eksponat jest na wystawie, pole Display jest wymagane");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Waliduje daty w historii eksponatu
        /// </summary>
        private bool ValidateDates(XElement history, XNamespace ns)
        {
            if (history == null) return true;

            var acquisition = history.Element(ns + "Acquisition");
            if (acquisition != null)
            {
                var dateElement = acquisition.Element(ns + "Date");
                if (dateElement != null)
                {
                    if (!DateTime.TryParse(dateElement.Value, out DateTime acquisitionDate))
                    {
                        _validationErrors.Add($"Niepoprawna data nabycia: {dateElement.Value}");
                        return false;
                    }

                    if (acquisitionDate > DateTime.Now)
                    {
                        _validationErrors.Add("Data nabycia nie może być w przyszłości");
                        return false;
                    }
                }

                // Walidacja ceny jeśli istnieje
                var priceElement = acquisition.Element(ns + "Price");
                if (priceElement != null)
                {
                    if (!decimal.TryParse(priceElement.Value, out decimal price) || price < 0)
                    {
                        _validationErrors.Add($"Niepoprawna cena nabycia: {priceElement.Value}");
                        return false;
                    }
                }
            }

            // Walidacja dat wystaw
            var exhibitions = history.Element(ns + "Exhibitions");
            if (exhibitions != null)
            {
                foreach (var exhibition in exhibitions.Elements(ns + "Exhibition"))
                {
                    if (!ValidateExhibitionDates(exhibition, ns))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Waliduje daty wystawy
        /// </summary>
        private bool ValidateExhibitionDates(XElement exhibition, XNamespace ns)
        {
            var startDateElement = exhibition.Element(ns + "StartDate");
            var endDateElement = exhibition.Element(ns + "EndDate");

            if (startDateElement != null && endDateElement != null)
            {
                if (DateTime.TryParse(startDateElement.Value, out DateTime startDate) &&
                    DateTime.TryParse(endDateElement.Value, out DateTime endDate))
                {
                    if (startDate >= endDate)
                    {
                        _validationErrors.Add("Data rozpoczęcia wystawy musi być wcześniejsza niż data zakończenia");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Waliduje sekcję mediów
        /// </summary>
        private bool ValidateMedia(XElement media, XNamespace ns)
        {
            if (media == null) return true; // Media są opcjonalne

            var images = media.Elements(ns + "Image").ToList();
            var primaryImages = images.Where(img => img.Attribute("primary")?.Value == "true").ToList();

            if (primaryImages.Count > 1)
            {
                _validationErrors.Add("Może być tylko jeden główny obraz (primary='true')");
                return false;
            }

            foreach (var image in images)
            {
                var fileName = image.Element(ns + "FileName")?.Value;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    _validationErrors.Add("Nazwa pliku obrazu jest wymagana");
                    return false;
                }

                if (!IsValidImageFileName(fileName))
                {
                    _validationErrors.Add($"Niepoprawne rozszerzenie pliku obrazu: {fileName}");
                    return false;
                }

                // Sprawdzenie długości nazwy pliku
                if (fileName.Length > 255)
                {
                    _validationErrors.Add($"Nazwa pliku obrazu jest za długa: {fileName}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Waliduje stan zachowania eksponatu
        /// </summary>
        private bool ValidateCondition(XElement technical, XNamespace ns)
        {
            if (technical == null) return false;

            var condition = technical.Element(ns + "Condition")?.Value;
            if (string.IsNullOrWhiteSpace(condition))
            {
                _validationErrors.Add("Stan zachowania (Condition) jest wymagany");
                return false;
            }

            var validConditions = new[] { "Excellent", "Good", "Fair", "Poor", "Critical" };
            if (!validConditions.Contains(condition))
            {
                _validationErrors.Add($"Niepoprawny stan zachowania: {condition}. Dozwolone: {string.Join(", ", validConditions)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sprawdza czy ID eksponatu ma poprawny format
        /// </summary>
        private bool IsValidExhibitID(string id)
        {
            return !string.IsNullOrEmpty(id) &&
                   id.StartsWith("EXH") &&
                   id.Length == 6 &&
                   int.TryParse(id.Substring(3), out int number) &&
                   number > 0;
        }

        /// <summary>
        /// Sprawdza czy nazwa pliku obrazu ma poprawne rozszerzenie
        /// </summary>
        private bool IsValidImageFileName(string fileName)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            return validExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sprawdza czy data historyczna jest poprawna
        /// </summary>
        private bool IsValidHistoricalDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            dateString = dateString.Trim().ToLower();

            // Obsługa zakresów np. "500 p.n.e. - 400 p.n.e."
            var separators = new[] { " - ", " – ", " — ", "-" };
            foreach (var sep in separators)
            {
                if (dateString.Contains(sep))
                {
                    var parts = dateString.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        return IsValidHistoricalDate(parts[0]) && IsValidHistoricalDate(parts[1]);
                    }
                }
            }

            // Obsługa dat przed naszą erą
            if (dateString.Contains("p.n.e."))
            {
                var numberPart = dateString.Replace("p.n.e.", "").Trim();
                if (int.TryParse(numberPart, out int year))
                {
                    return year > 0;
                }
                return false;
            }

            // Obsługa samych lat np. "1500" lub "200 n.e."
            if (dateString.Contains("n.e."))
            {
                var numberPart = dateString.Replace("n.e.", "").Trim();
                if (int.TryParse(numberPart, out int year))
                {
                    return year >= 1 && year <= DateTime.Now.Year;
                }
                return false;
            }

            // Jeśli sama liczba - traktuj jako rok n.e.
            if (int.TryParse(dateString, out int numericYear))
            {
                return numericYear >= 1 && numericYear <= DateTime.Now.Year;
            }

            // Próba standardowego parsowania np. "12.05.1900"
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                return parsedDate <= DateTime.Now;
            }

            return false;
        }

        /// <summary>
        /// Inicjalizuje schemat XSD dla dokumentów eksponatów
        /// </summary>
        private void InitializeSchema()
        {
            try
            {
                string xsdContent = GetExhibitXSD();

                using (var reader = new StringReader(xsdContent))
                {
                    var schema = XmlSchema.Read(reader, (sender, e) =>
                    {
                        throw new XmlSchemaException($"Błąd ładowania schematu: {e.Message}");
                    });

                    if (schema != null)
                    {
                        _schemaSet.Add(schema);
                        _schemaSet.Compile();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Nie można zainicjalizować schematu XSD: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Zwraca definicję schematu XSD dla eksponatów muzealnych
        /// </summary>
        private string GetExhibitXSD()
        {
            return @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" 
           targetNamespace=""http://museum.example.com/exhibit""
           xmlns:tns=""http://museum.example.com/exhibit""
           elementFormDefault=""qualified"">
    
    <xs:element name=""Exhibit"">
        <xs:complexType>
            <xs:sequence>
                <xs:element name=""BasicInfo"" type=""tns:BasicInfoType""/>
                <xs:element name=""Description"" type=""tns:DescriptionType""/>
                <xs:element name=""Technical"" type=""tns:TechnicalType""/>
                <xs:element name=""Location"" type=""tns:LocationType""/>
                <xs:element name=""History"" type=""tns:HistoryType"" minOccurs=""0""/>
                <xs:element name=""Media"" type=""tns:MediaType"" minOccurs=""0""/>
            </xs:sequence>
            <xs:attribute name=""id"" type=""xs:string"" use=""required""/>
            <xs:attribute name=""status"" type=""tns:StatusType"" use=""required""/>
        </xs:complexType>
    </xs:element>
    
    <xs:complexType name=""BasicInfoType"">
        <xs:sequence>
            <xs:element name=""Title"" type=""xs:string""/>
            <xs:element name=""Category"" type=""tns:CategoryType""/>
            <xs:element name=""SubCategory"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Creator"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""DateCreated"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Period"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Culture"" type=""xs:string"" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name=""DescriptionType"">
        <xs:sequence>
            <xs:element name=""ShortDescription"" type=""xs:string""/>
            <xs:element name=""DetailedDescription"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Significance"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Tags"" minOccurs=""0"">
                <xs:complexType>
                    <xs:sequence>
                        <xs:element name=""Tag"" type=""xs:string"" maxOccurs=""unbounded""/>
                    </xs:sequence>
                </xs:complexType>
            </xs:element>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name=""TechnicalType"">
        <xs:sequence>
            <xs:element name=""Dimensions"" type=""tns:DimensionsType"" minOccurs=""0""/>
            <xs:element name=""Weight"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Material"" type=""xs:string"" maxOccurs=""unbounded""/>
            <xs:element name=""Technique"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Condition"" type=""tns:ConditionType""/>
            <xs:element name=""ConservationNotes"" type=""xs:string"" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name=""DimensionsType"">
        <xs:sequence>
            <xs:element name=""Height"" type=""xs:double"" minOccurs=""0""/>
            <xs:element name=""Width"" type=""xs:double"" minOccurs=""0""/>
            <xs:element name=""Depth"" type=""xs:double"" minOccurs=""0""/>
            <xs:element name=""Diameter"" type=""xs:double"" minOccurs=""0""/>
        </xs:sequence>
        <xs:attribute name=""unit"" type=""xs:string"" use=""required""/>
    </xs:complexType>
    
    <xs:complexType name=""LocationType"">
        <xs:sequence>
            <xs:element name=""Building"" type=""xs:string""/>
            <xs:element name=""Floor"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Room"" type=""xs:string""/>
            <xs:element name=""Display"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""StorageLocation"" type=""xs:string"" minOccurs=""0""/>
        </xs:sequence>
        <xs:attribute name=""onDisplay"" type=""xs:boolean"" use=""required""/>
    </xs:complexType>
    
    <xs:complexType name=""HistoryType"">
        <xs:sequence>
            <xs:element name=""Acquisition"" type=""tns:AcquisitionType"" minOccurs=""0""/>
            <xs:element name=""Provenance"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""Exhibitions"" minOccurs=""0"">
                <xs:complexType>
                    <xs:sequence>
                        <xs:element name=""Exhibition"" type=""tns:ExhibitionType"" maxOccurs=""unbounded""/>
                    </xs:sequence>
                </xs:complexType>
            </xs:element>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name=""AcquisitionType"">
        <xs:sequence>
            <xs:element name=""Date"" type=""xs:date""/>
            <xs:element name=""Method"" type=""tns:AcquisitionMethodType""/>
            <xs:element name=""Source"" type=""xs:string""/>
            <xs:element name=""Price"" type=""xs:decimal"" minOccurs=""0""/>
            <xs:element name=""Currency"" type=""xs:string"" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name=""ExhibitionType"">
        <xs:sequence>
            <xs:element name=""Name"" type=""xs:string""/>
            <xs:element name=""Location"" type=""xs:string""/>
            <xs:element name=""StartDate"" type=""xs:date""/>
            <xs:element name=""EndDate"" type=""xs:date""/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name=""MediaType"">
        <xs:sequence>
            <xs:element name=""Image"" maxOccurs=""unbounded"" minOccurs=""0"">
                <xs:complexType>
                    <xs:sequence>
                        <xs:element name=""FileName"" type=""xs:string""/>
                        <xs:element name=""Description"" type=""xs:string"" minOccurs=""0""/>
                    </xs:sequence>
                    <xs:attribute name=""primary"" type=""xs:boolean""/>
                </xs:complexType>
            </xs:element>
        </xs:sequence>
    </xs:complexType>
    
    <xs:simpleType name=""CategoryType"">
        <xs:restriction base=""xs:string"">
            <xs:enumeration value=""Painting""/>
            <xs:enumeration value=""Sculpture""/>
            <xs:enumeration value=""Pottery""/>
            <xs:enumeration value=""Jewelry""/>
            <xs:enumeration value=""Textile""/>
            <xs:enumeration value=""Weapon""/>
            <xs:enumeration value=""Tool""/>
            <xs:enumeration value=""Coin""/>
            <xs:enumeration value=""Manuscript""/>
            <xs:enumeration value=""Photograph""/>
            <xs:enumeration value=""Other""/>
        </xs:restriction>
    </xs:simpleType>
    
    <xs:simpleType name=""StatusType"">
        <xs:restriction base=""xs:string"">
            <xs:enumeration value=""Active""/>
            <xs:enumeration value=""OnLoan""/>
            <xs:enumeration value=""InConservation""/>
            <xs:enumeration value=""Deaccessioned""/>
            <xs:enumeration value=""Missing""/>
        </xs:restriction>
    </xs:simpleType>
    
    <xs:simpleType name=""ConditionType"">
        <xs:restriction base=""xs:string"">
            <xs:enumeration value=""Excellent""/>
            <xs:enumeration value=""Good""/>
            <xs:enumeration value=""Fair""/>
            <xs:enumeration value=""Poor""/>
            <xs:enumeration value=""Critical""/>
        </xs:restriction>
    </xs:simpleType>
    
    <xs:simpleType name=""AcquisitionMethodType"">
        <xs:restriction base=""xs:string"">
            <xs:enumeration value=""Purchase""/>
            <xs:enumeration value=""Donation""/>
            <xs:enumeration value=""Bequest""/>
            <xs:enumeration value=""Exchange""/>
            <xs:enumeration value=""Transfer""/>
            <xs:enumeration value=""Found""/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>";
        }

        /// <summary>
        /// Zwraca sformatowany raport walidacji
        /// </summary>
        /// <returns>Raport tekstowy z wynikami walidacji</returns>
        public string GetValidationReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== RAPORT WALIDACJI XML ===");
            sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Status: {(IsValid ? "POPRAWNY" : "BŁĘDY ZNALEZIONE")}");
            sb.AppendLine($"Liczba błędów: {ValidationErrors.Count}");
            sb.AppendLine();

            if (!IsValid)
            {
                sb.AppendLine("BŁĘDY WALIDACJI:");
                for (int i = 0; i < ValidationErrors.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {ValidationErrors[i]}");
                }
            }
            else
            {
                sb.AppendLine("Dokument XML jest poprawny i zgodny ze schematem.");
            }

            sb.AppendLine();
            sb.AppendLine("=== KONIEC RAPORTU ===");
            return sb.ToString();
        }

        /// <summary>
        /// Sprawdza czy element istnieje i ma niepustą wartość
        /// </summary>
        public static bool HasValue(XElement element)
        {
            return element != null && !string.IsNullOrWhiteSpace(element.Value);
        }

        /// <summary>
        /// Sanityzuje XML usuwając potencjalnie niebezpieczne elementy
        /// </summary>
        /// <param name="xmlContent">Zawartość XML do oczyszczenia</param>
        /// <returns>Oczyszczona zawartość XML</returns>
        public static string SanitizeXML(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                return string.Empty;

            // Usuń komentarze XML
            xmlContent = System.Text.RegularExpressions.Regex.Replace(
                xmlContent, @"<!--.*?-->", "",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Usuń instrukcje przetwarzania
            xmlContent = System.Text.RegularExpressions.Regex.Replace(
                xmlContent, @"<\?.*?\?>", "",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            return xmlContent.Trim();
        }
    }

    /// <summary>
    /// Wynik walidacji XML
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime ValidationTime { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"Walidacja: {(IsValid ? "Sukces" : "Błąd")}, Błędów: {Errors.Count}, Ostrzeżeń: {Warnings.Count}";
        }
    }
}
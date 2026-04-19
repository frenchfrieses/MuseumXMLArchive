-- =====================================================
-- Muzealne Archiwum Dokumentów XML - Tworzenie Bazy Danych
-- Autor: Bartosz Fryska
-- Data: 13 maja 2025
-- =====================================================

USE master;
GO

-- Sprawdzenie czy baza istnieje i jej usuniêcie w razie potrzeby
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'MuseumXMLArchive')
BEGIN
    ALTER DATABASE MuseumXMLArchive SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MuseumXMLArchive;
END
GO

-- Utworzenie nowej bazy danych
CREATE DATABASE MuseumXMLArchive
ON 
(
    NAME = 'MuseumXMLArchive_Data',
    FILENAME = 'C:\Database\MuseumXMLArchive_Data.mdf'
)
LOG ON 
(
    NAME = 'MuseumXMLArchive_Log',
    FILENAME = 'C:\Database\MuseumXMLArchive_Log.ldf'
);
GO

USE MuseumXMLArchive;
GO

-- Tworzenie schematu XML
-- Schemat XSD dla dokumentów eksponatów muzealnych
CREATE XML SCHEMA COLLECTION ExhibitSchemaCollection AS '
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" 
           targetNamespace="http://museum.example.com/exhibit"
           xmlns:tns="http://museum.example.com/exhibit"
           elementFormDefault="qualified">
    
    <xs:element name="Exhibit">
        <xs:complexType>
            <xs:sequence>
                <xs:element name="BasicInfo" type="tns:BasicInfoType"/>
                <xs:element name="Description" type="tns:DescriptionType"/>
                <xs:element name="Technical" type="tns:TechnicalType"/>
                <xs:element name="Location" type="tns:LocationType"/>
                <xs:element name="History" type="tns:HistoryType" minOccurs="0"/>
                <xs:element name="Media" type="tns:MediaType" minOccurs="0"/>
            </xs:sequence>
            <xs:attribute name="id" type="xs:string" use="required"/>
            <xs:attribute name="status" type="tns:StatusType" use="required"/>
        </xs:complexType>
    </xs:element>
    
    <xs:complexType name="BasicInfoType">
        <xs:sequence>
            <xs:element name="Title" type="xs:string"/>
            <xs:element name="Category" type="tns:CategoryType"/>
            <xs:element name="SubCategory" type="xs:string" minOccurs="0"/>
            <xs:element name="Creator" type="xs:string" minOccurs="0"/>
            <xs:element name="DateCreated" type="xs:string" minOccurs="0"/>
            <xs:element name="Period" type="xs:string" minOccurs="0"/>
            <xs:element name="Culture" type="xs:string" minOccurs="0"/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name="DescriptionType">
        <xs:sequence>
            <xs:element name="ShortDescription" type="xs:string"/>
            <xs:element name="DetailedDescription" type="xs:string" minOccurs="0"/>
            <xs:element name="Significance" type="xs:string" minOccurs="0"/>
            <xs:element name="Tags" minOccurs="0">
                <xs:complexType>
                    <xs:sequence>
                        <xs:element name="Tag" type="xs:string" maxOccurs="unbounded"/>
                    </xs:sequence>
                </xs:complexType>
            </xs:element>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name="TechnicalType">
        <xs:sequence>
            <xs:element name="Dimensions" type="tns:DimensionsType" minOccurs="0"/>
            <xs:element name="Weight" type="xs:string" minOccurs="0"/>
            <xs:element name="Material" type="xs:string" maxOccurs="unbounded"/>
            <xs:element name="Technique" type="xs:string" minOccurs="0"/>
            <xs:element name="Condition" type="tns:ConditionType"/>
            <xs:element name="ConservationNotes" type="xs:string" minOccurs="0"/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name="DimensionsType">
        <xs:sequence>
            <xs:element name="Height" type="xs:double" minOccurs="0"/>
            <xs:element name="Width" type="xs:double" minOccurs="0"/>
            <xs:element name="Depth" type="xs:double" minOccurs="0"/>
            <xs:element name="Diameter" type="xs:double" minOccurs="0"/>
        </xs:sequence>
        <xs:attribute name="unit" type="xs:string" use="required"/>
    </xs:complexType>
    
    <xs:complexType name="LocationType">
        <xs:sequence>
            <xs:element name="Building" type="xs:string"/>
            <xs:element name="Floor" type="xs:string" minOccurs="0"/>
            <xs:element name="Room" type="xs:string"/>
            <xs:element name="Display" type="xs:string" minOccurs="0"/>
            <xs:element name="StorageLocation" type="xs:string" minOccurs="0"/>
        </xs:sequence>
        <xs:attribute name="onDisplay" type="xs:boolean" use="required"/>
    </xs:complexType>
    
    <xs:complexType name="HistoryType">
        <xs:sequence>
            <xs:element name="Acquisition" type="tns:AcquisitionType" minOccurs="0"/>
            <xs:element name="Provenance" type="xs:string" minOccurs="0"/>
            <xs:element name="Exhibitions" minOccurs="0">
                <xs:complexType>
                    <xs:sequence>
                        <xs:element name="Exhibition" type="tns:ExhibitionType" maxOccurs="unbounded"/>
                    </xs:sequence>
                </xs:complexType>
            </xs:element>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name="AcquisitionType">
        <xs:sequence>
            <xs:element name="Date" type="xs:date"/>
            <xs:element name="Method" type="tns:AcquisitionMethodType"/>
            <xs:element name="Source" type="xs:string"/>
            <xs:element name="Price" type="xs:decimal" minOccurs="0"/>
            <xs:element name="Currency" type="xs:string" minOccurs="0"/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name="ExhibitionType">
        <xs:sequence>
            <xs:element name="Name" type="xs:string"/>
            <xs:element name="Location" type="xs:string"/>
            <xs:element name="StartDate" type="xs:date"/>
            <xs:element name="EndDate" type="xs:date"/>
        </xs:sequence>
    </xs:complexType>
    
    <xs:complexType name="MediaType">
        <xs:sequence>
            <xs:element name="Image" maxOccurs="unbounded" minOccurs="0">
                <xs:complexType>
                    <xs:sequence>
                        <xs:element name="FileName" type="xs:string"/>
                        <xs:element name="Description" type="xs:string" minOccurs="0"/>
                    </xs:sequence>
                    <xs:attribute name="primary" type="xs:boolean"/>
                </xs:complexType>
            </xs:element>
        </xs:sequence>
    </xs:complexType>
    
    <xs:simpleType name="CategoryType">
        <xs:restriction base="xs:string">
            <xs:enumeration value="Painting"/>
            <xs:enumeration value="Sculpture"/>
            <xs:enumeration value="Pottery"/>
            <xs:enumeration value="Jewelry"/>
            <xs:enumeration value="Textile"/>
            <xs:enumeration value="Weapon"/>
            <xs:enumeration value="Tool"/>
            <xs:enumeration value="Coin"/>
            <xs:enumeration value="Manuscript"/>
            <xs:enumeration value="Photograph"/>
            <xs:enumeration value="Other"/>
        </xs:restriction>
    </xs:simpleType>
    
    <xs:simpleType name="StatusType">
        <xs:restriction base="xs:string">
            <xs:enumeration value="Active"/>
            <xs:enumeration value="OnLoan"/>
            <xs:enumeration value="InConservation"/>
            <xs:enumeration value="Deaccessioned"/>
            <xs:enumeration value="Missing"/>
        </xs:restriction>
    </xs:simpleType>
    
    <xs:simpleType name="ConditionType">
        <xs:restriction base="xs:string">
            <xs:enumeration value="Excellent"/>
            <xs:enumeration value="Good"/>
            <xs:enumeration value="Fair"/>
            <xs:enumeration value="Poor"/>
            <xs:enumeration value="Critical"/>
        </xs:restriction>
    </xs:simpleType>
    
    <xs:simpleType name="AcquisitionMethodType">
        <xs:restriction base="xs:string">
            <xs:enumeration value="Purchase"/>
            <xs:enumeration value="Donation"/>
            <xs:enumeration value="Bequest"/>
            <xs:enumeration value="Exchange"/>
            <xs:enumeration value="Transfer"/>
            <xs:enumeration value="Found"/>
        </xs:restriction>
    </xs:simpleType>
</xs:schema>';
GO

PRINT 'Baza danych MuseumXMLArchive i schemat XML zosta³y utworzone pomyœlnie!';
GO
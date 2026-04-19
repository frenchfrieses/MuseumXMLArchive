-- =====================================================
-- Muzealne Archiwum Dokumentów XML - Dane Testowe
-- Autor: Bartosz Fryska
-- Data: 13 maja 2025
-- =====================================================

USE MuseumXMLArchive;
GO

-- Wstawienie przyk³adowych dokumentów XML
INSERT INTO Documents (ExhibitID, DocumentName, XMLContent) VALUES 
('EXH001', 'Staro¿ytna Amfora', '
<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH001" status="Active">
    <BasicInfo>
        <Title>Staro¿ytna Amfora Grecka</Title>
        <Category>Pottery</Category>
        <SubCategory>Amfora</SubCategory>
        <Creator>Nieznany Ceramik</Creator>
        <DateCreated>500 p.n.e. - 400 p.n.e.</DateCreated>
        <Period>Klasyczny</Period>
        <Culture>Grecka</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Piêkna grecka amfora z okresu klasycznego</ShortDescription>
        <DetailedDescription>Amfora wykonana z czerwonej gliny, ozdobiona scenami mitologicznymi przedstawiaj¹cymi bogów olimpijskich.</DetailedDescription>
        <Significance>Reprezentuje szczyt greckiej sztuki ceramicznej</Significance>
        <Tags>
            <Tag>mitologia</Tag>
            <Tag>ceramika</Tag>
            <Tag>staro¿ytnoœæ</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit="cm">
            <Height>45.5</Height>
            <Width>28.0</Width>
            <Depth>28.0</Depth>
        </Dimensions>
        <Weight>3.2 kg</Weight>
        <Material>Glina</Material>
        <Material>Farba ceramiczna</Material>
        <Technique>Toczenie, malowanie</Technique>
        <Condition>Good</Condition>
        <ConservationNotes>Niewielkie pêkniêcia na podstawie, wymagana konserwacja prewencyjna</ConservationNotes>
    </Technical>
    <Location onDisplay="true">
        <Building>Budynek G³ówny</Building>
        <Floor>Pierwsze piêtro</Floor>
        <Room>Sala Grecka</Room>
        <Display>Gablota G-15</Display>
    </Location>
    <History>
        <Acquisition>
            <Date>1995-03-15</Date>
            <Method>Purchase</Method>
            <Source>Kolekcja Prywatna - Dr. Kowalski</Source>
            <Price>15000</Price>
            <Currency>PLN</Currency>
        </Acquisition>
        <Provenance>Kolekcja prywatna, wczeœniej w posiadaniu rodziny Kowalskich od 1960 roku</Provenance>
    </History>
    <Media>
        <Image primary="true">
            <FileName>amfora_exh001_front.jpg</FileName>
            <Description>Widok z przodu</Description>
        </Image>
        <Image primary="false">
            <FileName>amfora_exh001_back.jpg</FileName>
            <Description>Widok z ty³u</Description>
        </Image>
    </Media>
</Exhibit>'),

('EXH002', 'Œredniowieczny Miecz', '
<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH002" status="Active">
    <BasicInfo>
        <Title>Miecz Rycerski XIV wieku</Title>
        <Category>Weapon</Category>
        <SubCategory>Miecz jednorêczny</SubCategory>
        <Creator>Mistrz Kowalski z Krakowa</Creator>
        <DateCreated>1350-1370</DateCreated>
        <Period>Œredniowiecze</Period>
        <Culture>Polska</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Dobrze zachowany miecz rycerski z XIV wieku</ShortDescription>
        <DetailedDescription>Miecz o d³ugoœci 95 cm z charakterystyczn¹ krzy¿ow¹ rêkojeœci¹. Klinga wykonana ze stali wysokiej jakoœci.</DetailedDescription>
        <Significance>Przyk³ad œredniowiecznego rzemios³a zbrojeniowego</Significance>
        <Tags>
            <Tag>œredniowiecze</Tag>
            <Tag>broñ</Tag>
            <Tag>rycerstwo</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit="cm">
            <Height>95.0</Height>
            <Width>15.0</Width>
        </Dimensions>
        <Weight>1.8 kg</Weight>
        <Material>Stal</Material>
        <Material>Skóra</Material>
        <Material>Drewno</Material>
        <Technique>Kucie, hartowanie</Technique>
        <Condition>Fair</Condition>
        <ConservationNotes>Œlady korozji na klidze, rêkojeœæ wymaga renowacji</ConservationNotes>
    </Technical>
    <Location onDisplay="false">
        <Building>Magazyn A</Building>
        <Room>Pomieszczenie 12</Room>
        <StorageLocation>Szafa S-45, Pó³ka 3</StorageLocation>
    </Location>
    <History>
        <Acquisition>
            <Date>2010-08-20</Date>
            <Method>Donation</Method>
            <Source>Fundacja Dziedzictwa Kulturowego</Source>
        </Acquisition>
    </History>
</Exhibit>'),

('EXH003', 'Renesansowy Obraz', '
<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH003" status="Active">
    <BasicInfo>
        <Title>Portret Damy z Per³ami</Title>
        <Category>Painting</Category>
        <SubCategory>Portret</SubCategory>
        <Creator>Giovanni da Milano</Creator>
        <DateCreated>1520-1530</DateCreated>
        <Period>Renesans</Period>
        <Culture>W³oska</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Elegancki portret renesansowy przedstawiaj¹cy arystokratkê</ShortDescription>
        <DetailedDescription>Portret namalowany technik¹ olejn¹ na desce dêbowej. Przedstawia m³od¹ kobietê w bogatych szatach z per³owym naszyjnikiem.</DetailedDescription>
        <Significance>Doskona³y przyk³ad renesansowego malarstwa portretowego</Significance>
        <Tags>
            <Tag>renesans</Tag>
            <Tag>portret</Tag>
            <Tag>arystokracja</Tag>
            <Tag>per³y</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit="cm">
            <Height>65.0</Height>
            <Width>50.0</Width>
        </Dimensions>
        <Material>Farba olejna</Material>
        <Material>Deska dêbowa</Material>
        <Technique>Malarstwo olejne</Technique>
        <Condition>Excellent</Condition>
        <ConservationNotes>Ostatnia konserwacja w 2018 roku, stan doskona³y</ConservationNotes>
    </Technical>
    <Location onDisplay="true">
        <Building>Budynek G³ówny</Building>
        <Floor>Drugie piêtro</Floor>
        <Room>Sala Renesansowa</Room>
        <Display>Œciana pó³nocna</Display>
    </Location>
    <History>
        <Acquisition>
            <Date>2005-11-10</Date>
            <Method>Bequest</Method>
            <Source>Spadek po Hrabinie Zamoyskiej</Source>
        </Acquisition>
        <Provenance>Dziedziczony w rodzinie Zamoyskich od XVII wieku</Provenance>
        <Exhibitions>
            <Exhibition>
                <Name>Skarby Renesansu</Name>
                <Location>Muzeum Narodowe w Warszawie</Location>
                <StartDate>2019-05-15</StartDate>
                <EndDate>2019-09-30</EndDate>
            </Exhibition>
        </Exhibitions>
    </History>
    <Media>
        <Image primary="true">
            <FileName>portret_exh003_main.jpg</FileName>
            <Description>G³ówne zdjêcie obrazu</Description>
        </Image>
        <Image primary="false">
            <FileName>portret_exh003_detail_pearls.jpg</FileName>
            <Description>Detal - naszyjnik z pere³</Description>
        </Image>
    </Media>
</Exhibit>'),

('EXH004', 'Rzymska Moneta', '
<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH004" status="Active">
    <BasicInfo>
        <Title>Aureus Cesarza Trajana</Title>
        <Category>Coin</Category>
        <SubCategory>Aureus</SubCategory>
        <DateCreated>98-117 n.e.</DateCreated>
        <Period>Cesarstwo Rzymskie</Period>
        <Culture>Rzymska</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Z³ota moneta rzymska z okresu panowania Trajana</ShortDescription>
        <DetailedDescription>Aureus ze z³ota próby 900, przedstawiaj¹cy popiersi cesarza Trajana na awersie i alegoriê zwyciêstwa na rewersie.</DetailedDescription>
        <Significance>Rzadka moneta z okresu najwiêkszego rozmachu Imperium Rzymskiego</Significance>
        <Tags>
            <Tag>numizmatyka</Tag>
            <Tag>Trajan</Tag>
            <Tag>cesarstwo</Tag>
            <Tag>z³oto</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit="mm">
            <Diameter>20.5</Diameter>
        </Dimensions>
        <Weight>7.8 g</Weight>
        <Material>Z³oto</Material>
        <Condition>Good</Condition>
        <ConservationNotes>Lekkie œlady zu¿ycia, typowe dla monet w obiegu</ConservationNotes>
    </Technical>
    <Location onDisplay="true">
        <Building>Budynek G³ówny</Building>
        <Floor>Parter</Floor>
        <Room>Sala Numizmatyczna</Room>
        <Display>Gablota N-08</Display>
    </Location>
    <History>
        <Acquisition>
            <Date>2001-04-22</Date>
            <Method>Purchase</Method>
            <Source>Dom Aukcyjny Numis</Source>
            <Price>12000</Price>
            <Currency>USD</Currency>
        </Acquisition>
        <Provenance>Kolekcja prywatna, znaleziona w rejonie Dacji (dzisiejsza Rumunia)</Provenance>
    </History>
    <Media>
        <Image primary="true">
            <FileName>aureus_exh004_avers.jpg</FileName>
            <Description>Awers monety</Description>
        </Image>
        <Image primary="false">
            <FileName>aureus_exh004_rewers.jpg</FileName>
            <Description>Rewers monety</Description>
        </Image>
    </Media>
</Exhibit>'),

('EXH005', 'Barokowa RzeŸba', '
<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH005" status="OnLoan">
    <BasicInfo>
        <Title>Anio³ z Tr¹b¹</Title>
        <Category>Sculpture</Category>
        <SubCategory>RzeŸba religijna</SubCategory>
        <Creator>Johann Baptist Straub</Creator>
        <DateCreated>1750-1760</DateCreated>
        <Period>Barok</Period>
        <Culture>Austriacka</Culture>
    </BasicInfo>
    <Description>
        <ShortDescription>Barokowa rzeŸba anio³a z poz³acanymi detalami</ShortDescription>
        <DetailedDescription>RzeŸba wykonana z lipowego drewna, przedstawiaj¹ca anio³a w dynamicznej pozie z tr¹b¹ w d³oni. Bogate poz³acane detale na skrzyd³ach i szatach.</DetailedDescription>
        <Significance>Reprezentuje najwy¿szy poziom barokowego snycerstwa religijnego</Significance>
        <Tags>
            <Tag>barok</Tag>
            <Tag>anio³</Tag>
            <Tag>snycerstwo</Tag>
            <Tag>poz³ota</Tag>
        </Tags>
    </Description>
    <Technical>
        <Dimensions unit="cm">
            <Height>85.0</Height>
            <Width>45.0</Width>
            <Depth>30.0</Depth>
        </Dimensions>
        <Weight>12.5 kg</Weight>
        <Material>Drewno lipowe</Material>
        <Material>Poz³ota</Material>
        <Technique>Snycerstwo, poz³acanie</Technique>
        <Condition>Excellent</Condition>
        <ConservationNotes>Konserwacja w 2020 roku, odnowiono poz³otê</ConservationNotes>
    </Technical>
    <Location onDisplay="false">
        <Building>Wypo¿yczenie zewnêtrzne</Building>
        <Room>Muzeum Sztuki Sakralnej w Salzburgu</Room>
    </Location>
    <History>
        <Acquisition>
            <Date>1998-06-30</Date>
            <Method>Exchange</Method>
            <Source>Wymiana z Muzeum w Salzburgu</Source>
        </Acquisition>
        <Provenance>Pierwotnie w koœciele œw. Piotra w Salzburgu, od 1780 roku</Provenance>
        <Exhibitions>
            <Exhibition>
                <Name>Barokowe Skarby Austrii</Name>
                <Location>Muzeum Sztuki Sakralnej w Salzburgu</Location>
                <StartDate>2023-03-01</StartDate>
                <EndDate>2024-02-28</EndDate>
            </Exhibition>
        </Exhibitions>
    </History>
</Exhibit>');
GO

-- Dodanie dodatkowych przyk³adów u¿ywaj¹c procedur sk³adowanych
DECLARE @DocumentID INT;

-- Przyk³ad u¿ycia procedury SP_InsertDocument
EXEC SP_InsertDocument 
    @ExhibitID = 'EXH006',
    @DocumentName = 'Egipski Papirus',
    @XMLContent = '<Exhibit xmlns="http://museum.example.com/exhibit" id="EXH006" status="InConservation">
        <BasicInfo>
            <Title>Papirus z Ksiêgi Umar³ych</Title>
            <Category>Manuscript</Category>
            <SubCategory>Papirus</SubCategory>
            <DateCreated>1550-1070 p.n.e.</DateCreated>
            <Period>Nowe Pañstwo</Period>
            <Culture>Egipska</Culture>
        </BasicInfo>
        <Description>
            <ShortDescription>Fragment staro¿ytnego egipskiego papirusu z hieroglifami</ShortDescription>
            <DetailedDescription>Papirus zawieraj¹cy fragmenty Ksiêgi Umar³ych z hieroglifami i winietami przedstawiaj¹cymi podró¿ duszy w zaœwiatach.</DetailedDescription>
            <Significance>Cenny dokument religii i kultury staro¿ytnego Egiptu</Significance>
            <Tags>
                <Tag>Egipt</Tag>
                <Tag>hieroglify</Tag>
                <Tag>Ksiêga Umar³ych</Tag>
                <Tag>papirus</Tag>
            </Tags>
        </Description>
        <Technical>
            <Dimensions unit="cm">
                <Height>25.0</Height>
                <Width>180.0</Width>
            </Dimensions>
            <Material>Papirus</Material>
            <Material>Farba mineralna</Material>
            <Technique>Kaligrafía hieroglificzna</Technique>
            <Condition>Poor</Condition>
            <ConservationNotes>Wymaga pilnej konserwacji - kruchy papirus, blakn¹ce farby</ConservationNotes>
        </Technical>
        <Location onDisplay="false">
            <Building>Laboratorium Konserwatorskie</Building>
            <Room>Pracownia Papirusów</Room>
            <StorageLocation>Klimatyzowana szafa K-12</StorageLocation>
        </Location>
        <History>
            <Acquisition>
                <Date>1923-11-15</Date>
                <Method>Purchase</Method>
                <Source>Ekspedycja Archeologiczna Prof. Majewskiego</Source>
                <Price>5000</Price>
                <Currency>USD</Currency>
            </Acquisition>
            <Provenance>Znaleziony w dolinie Królów w 1923 roku</Provenance>
        </History>
    </Exhibit>',
    @CreatedBy = 'Bartosz Fryska',
    @DocumentID = @DocumentID OUTPUT;

PRINT 'Dodano dokument EXH006 z ID: ' + CAST(@DocumentID AS NVARCHAR(10));

-- Sprawdzenie poprawnoœci instalacji i danych
SELECT 'Wstawiono dane testowe pomyœlnie' as Status;
SELECT COUNT(*) as '£¹czna liczba dokumentów' FROM Documents;
SELECT COUNT(*) as 'Dokumenty aktywne' FROM Documents WHERE IsActive = 1;
WITH XMLNAMESPACES('http://museum.example.com/exhibit' as ns)
SELECT COUNT(*) as 'Dokumenty wystawione' FROM Documents WHERE XMLContent.value('(/ns:Exhibit/ns:Location/@onDisplay)[1]', 'BIT') = 1;

-- Wyœwietlenie podsumowania eksponatów
SELECT * FROM VW_ExhibitSummary ORDER BY Title;

-- Test procedury wyszukiwania
PRINT 'Test wyszukiwania eksponatów w kategorii "Pottery":';
EXEC SP_SearchByCategory @Category = 'Pottery';

PRINT 'Dane testowe zosta³y wstawione pomyœlnie!';
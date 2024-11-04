using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TestExternalConnector.Data;

public static class CsvDataLoader
{
    public static List<SubjectMatter> LoadFromCsv(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<SubjectMatterMap>();

        return new List<SubjectMatter>(csv.GetRecords<SubjectMatter>());
    }
}

public class KeywordListConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var keywords = text?.Split(';') ?? Array.Empty<string>();
        return new List<string>(keywords);
    }
}

public class SubjectMatterMap : ClassMap<SubjectMatter>
{
    public SubjectMatterMap()
    {
        Map(m => m.Id);
        Map(m => m.Title);
        Map(m => m.Description);
        Map(m => m.Url);
        Map(m => m.Keywords).TypeConverter<KeywordListConverter>();
    }
}
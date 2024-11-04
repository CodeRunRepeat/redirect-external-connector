using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Graph.Models.ExternalConnectors;

namespace TestExternalConnector.Data
{
    public class SubjectMatter
    {
        private const string KeywordsODataProperty = "keywords@odata.type";
        [JsonPropertyName(KeywordsODataProperty)]
        private const string KeywordsODataType = "Collection(String)";

        [Key]
        public string Id { get; set; } = default!;
        public string? Title { get; set; } = default!;
        public string? Description { get; set; } = default!;
        public string? Url { get; set; } = default!;
        public List<string>? Keywords { get; set; }

        public Properties AsExternalItemProperties()
        {
            return new Properties
            {
                AdditionalData = new Dictionary<string, object?> {
                    { "id", Id },
                    { "title", Title },
                    { "description", Description },
                    { "url", Url },
                    { "keywords", Keywords },
                    { KeywordsODataProperty, KeywordsODataType },
                }
            };
        }

        public static Schema GenerateSchema()
        {
            return new Schema
            {
                BaseType = "microsoft.graph.externalItem",
                Properties = new List<Property>
                {
                    new Property { Name = "id", Type = PropertyType.String, IsQueryable = true, IsSearchable = false, IsRetrievable = true, IsRefinable = true },
                    new Property { Name = "title", Type = PropertyType.String, IsQueryable = true, IsSearchable = true, IsRetrievable = true, IsRefinable = false, Labels = new List<Label?>() { Label.Title }},
                    new Property { Name = "description", Type = PropertyType.String, IsQueryable = false, IsSearchable = true, IsRetrievable = true, IsRefinable = false },
                    new Property { Name = "url", Type = PropertyType.String, IsQueryable = true, IsSearchable = false, IsRetrievable = true, IsRefinable = true, Labels = new List<Label?>() { Label.Url } },
                    new Property { Name = "keywords", Type = PropertyType.StringCollection, IsQueryable = true, IsSearchable = true, IsRetrievable = true, IsRefinable = false }
                },
            };
        }
    }
}
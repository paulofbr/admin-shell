using System.Text.Json.Serialization;

namespace AdminShell.Contracts;

public class QuerySpecification
{
    [JsonPropertyName("filters")]
    public List<Filter> Filters { get; set; } = new();

    [JsonPropertyName("sorts")]
    public List<Sort> Sorts { get; set; } = new();

    [JsonPropertyName("skip")]
    public int Skip { get; set; } = 0;

    [JsonPropertyName("take")]
    public int Take { get; set; } = 20;
}

public class Filter
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class Sort
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public SortOrder Order { get; set; } = SortOrder.Ascending;
}

public enum SortOrder
{
    Ascending,
    Descending
}

using System.Text.Json.Serialization;

namespace GoveeDashboard.Models;

public class Capability
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("instance")] public string Instance { get; set; }

    [JsonPropertyName("parameters")] public Parameters Parameters { get; set; }
}

public class Datum
{
    [JsonPropertyName("sku")] public string Sku { get; set; }

    [JsonPropertyName("device")] public string Device { get; set; }

    [JsonPropertyName("deviceName")] public string DeviceName { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("capabilities")] public List<Capability> Capabilities { get; set; }
}

public class ElementRange
{
    [JsonPropertyName("min")] public int Min { get; set; }

    [JsonPropertyName("max")] public int Max { get; set; }
}

public class Field
{
    [JsonPropertyName("fieldName")] public string FieldName { get; set; }

    [JsonPropertyName("size")] public Size Size { get; set; }

    [JsonPropertyName("dataType")] public string DataType { get; set; }

    [JsonPropertyName("elementRange")] public ElementRange ElementRange { get; set; }

    [JsonPropertyName("elementType")] public string ElementType { get; set; }

    [JsonPropertyName("required")] public bool Required { get; set; }

    [JsonPropertyName("range")] public Range Range { get; set; }

    [JsonPropertyName("options")] public List<Option> Options { get; set; }

    [JsonPropertyName("unit")] public string Unit { get; set; }
}

public class Option
{
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("value")] public int Value { get; set; }
}

public class Parameters
{
    [JsonPropertyName("dataType")] public string DataType { get; set; }

    [JsonPropertyName("options")] public List<Option> Options { get; set; }

    [JsonPropertyName("unit")] public string Unit { get; set; }

    [JsonPropertyName("range")] public Range Range { get; set; }

    [JsonPropertyName("fields")] public List<Field> Fields { get; set; }
}

public class Range
{
    [JsonPropertyName("min")] public int Min { get; set; }

    [JsonPropertyName("max")] public int Max { get; set; }

    [JsonPropertyName("precision")] public int Precision { get; set; }
}

public class Device
{
    [JsonPropertyName("code")] public int Code { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public List<Datum> Data { get; set; }
}

public class Size
{
    [JsonPropertyName("min")] public int Min { get; set; }

    [JsonPropertyName("max")] public int Max { get; set; }
}
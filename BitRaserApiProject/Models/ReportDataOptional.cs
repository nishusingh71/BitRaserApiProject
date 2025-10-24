using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace BitRaserApiProject.Models;

public class ReportData
{
    [JsonPropertyName("report_id")]
    public string? ReportId { get; set; }

    [JsonPropertyName("datetime")]
    public string? ReportDate { get; set; } 

    [JsonPropertyName("software_name")]
    public string? SoftwareName { get; set; }

    [JsonPropertyName("product_version")]
    public string? ProductVersion { get; set; }

    [JsonPropertyName("digital_signature")]
    public string? DigitalSignature { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("process_mode")]
  public string? ProcessMode { get; set; }

 [JsonPropertyName("os")]
    public string? OS { get; set; }

    [JsonPropertyName("os_version")]
  public string? OSVersion { get; set; }

    [JsonPropertyName("computer_name")]
    public string? ComputerName { get; set; }

    [JsonPropertyName("mac_address")]
    public string? MacAddress { get; set; }

  [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("Eraser_Start_Time")]
    public string? EraserStartTime { get; set; } 

    [JsonPropertyName("Eraser_End_Time")]
    public string? EraserEndTime { get; set; } 

    [JsonPropertyName("eraser_method")]
    public string? EraserMethod { get; set; }

    [JsonPropertyName("validation_method")]
    public string? ValidationMethod { get; set; } 

    [JsonPropertyName("Erasure_Type")]
    public string? ErasureType { get; set; }

    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("erased_files")]
    public int ErasedFiles { get; set; }

 [JsonPropertyName("failed_files")]
    public int FailedFiles { get; set; }

    [JsonPropertyName("erasure_log")]
    public List<ErasureLogEntry>? ErasureLog { get; set; } = new();
}

public class ErasureLogEntry
{
    [JsonPropertyName("target")]
public string? Target { get; set; }

    [JsonPropertyName("free_space")]
    public string? Capacity { get; set; }

    // Flexible converter - accepts both string and number
    [JsonPropertyName("total_sectors")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TotalSectors { get; set; }

    // Flexible converter - accepts both string and number
    [JsonPropertyName("sectors_erased")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SectorsErased { get; set; }

    [JsonPropertyName("dummy_file_size")]
    public string? Size { get; set; }

 [JsonPropertyName("status")]
    public string? Status { get; set; } 
}

/// <summary>
/// Custom JSON converter that accepts both string and number types
/// Converts numbers to strings automatically
/// </summary>
public class FlexibleStringConverter : JsonConverter<string>
{
  public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
  return reader.GetString();
  
     case JsonTokenType.Number:
       // Handle both int and long
      if (reader.TryGetInt64(out long longValue))
           {
            return longValue.ToString();
      }
   if (reader.TryGetDouble(out double doubleValue))
           {
        return doubleValue.ToString("F0"); // No decimal places
 }
           return reader.GetString();
     
   case JsonTokenType.Null:
      return null;
  
        default:
    // Fallback - try to get as string
        return reader.GetString();
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
     {
         writer.WriteNullValue();
        }
      else
        {
      writer.WriteStringValue(value);
   }
    }
}
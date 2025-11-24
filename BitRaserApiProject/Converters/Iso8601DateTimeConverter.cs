using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitRaserApiProject.Converters
{
    /// <summary>
    /// Custom JSON converter for DateTime to ensure ISO 8601 format
    /// Format: 2025-11-24T05:07:11.3895396Z
    /// All DateTime values are serialized in UTC with Z suffix
    /// </summary>
    public class Iso8601DateTimeConverter : JsonConverter<DateTime>
    {
      public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
   var dateTimeString = reader.GetString();
      
            if (string.IsNullOrEmpty(dateTimeString))
       throw new JsonException("DateTime value cannot be null or empty");

            // Parse with RoundtripKind to preserve timezone info
       var dateTime = DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            
  // Ensure we return UTC
            return dateTime.Kind == DateTimeKind.Utc 
   ? dateTime 
      : dateTime.ToUniversalTime();
        }

  public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
 {
      // Ensure DateTime is in UTC
       var utcDateTime = value.Kind == DateTimeKind.Utc 
 ? value 
   : value.ToUniversalTime();

       // Format with 7 decimal places for fractional seconds + Z suffix
          var iso8601String = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
  
         writer.WriteStringValue(iso8601String);
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable DateTime to ensure ISO 8601 format
    /// Handles null values appropriately
    /// </summary>
    public class Iso8601NullableDateTimeConverter : JsonConverter<DateTime?>
    {
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
   {
        var dateTimeString = reader.GetString();
   
          if (string.IsNullOrEmpty(dateTimeString))
    return null;

     // Parse with RoundtripKind to preserve timezone info
          var dateTime = DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
  
        // Ensure we return UTC
      return dateTime.Kind == DateTimeKind.Utc 
   ? dateTime 
                : dateTime.ToUniversalTime();
        }

     public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
      {
    if (!value.HasValue)
     {
    writer.WriteNullValue();
  return;
            }

      // Ensure DateTime is in UTC
            var utcDateTime = value.Value.Kind == DateTimeKind.Utc 
 ? value.Value 
       : value.Value.ToUniversalTime();

            // Format with 7 decimal places for fractional seconds + Z suffix
        var iso8601String = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
      
         writer.WriteStringValue(iso8601String);
        }
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gpxoverlay.garmin
{
    public class GarminActivity
    {
        [JsonPropertyName("activityId")]
        public long ActivityId { get; set; }
        [JsonPropertyName("startTimeGMT")]
        public DateTime StartTimeGMT { get; set; }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            throw new ArgumentException("write not supported");
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using OfficeIMO.Project;
namespace ProjectDesk;

// The vendor's readonly value types do not opt into System.Text.Json constructor binding.
internal sealed class DurationJsonConverter : JsonConverter<ProjectDuration> {
 public override ProjectDuration Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options){using var document=JsonDocument.ParseValue(ref reader);var v=document.RootElement;return new(v.GetProperty("Value").GetDecimal(),(ProjectDurationUnit)v.GetProperty("Unit").GetInt32(),v.GetProperty("IsElapsed").GetBoolean(),v.GetProperty("IsEstimated").GetBoolean());}
 public override void Write(Utf8JsonWriter writer,ProjectDuration value,JsonSerializerOptions options){writer.WriteStartObject();writer.WriteNumber("Value",value.Value);writer.WriteNumber("Unit",(int)value.Unit);writer.WriteBoolean("IsElapsed",value.IsElapsed);writer.WriteBoolean("IsEstimated",value.IsEstimated);writer.WriteEndObject();}
}
internal sealed class WorkingTimeJsonConverter : JsonConverter<ProjectWorkingTime> {
 public override ProjectWorkingTime Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options){using var document=JsonDocument.ParseValue(ref reader);var v=document.RootElement;return new(TimeSpan.FromTicks(v.GetProperty("FromTicks").GetInt64()),TimeSpan.FromTicks(v.GetProperty("ToTicks").GetInt64()));}
 public override void Write(Utf8JsonWriter writer,ProjectWorkingTime value,JsonSerializerOptions options){writer.WriteStartObject();writer.WriteNumber("FromTicks",value.From.Ticks);writer.WriteNumber("ToTicks",value.To.Ticks);writer.WriteEndObject();}
}

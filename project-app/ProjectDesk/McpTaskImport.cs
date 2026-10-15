using System.Globalization;
using System.Text.Json;
using OfficeIMO.Project;
namespace ProjectDesk;
public sealed record McpImportedTask(string Name,string Duration,string Start,string Finish,string Progress,string Notes);
public sealed class McpProjectRevision {
 private ProjectDocument? _document;private long _revision=-1,_generation;private readonly string _id=Guid.NewGuid().ToString("N");
 public string Current(ProjectSession session){var doc=session.Document;if(!ReferenceEquals(doc,_document)||(doc?.Revision??-1)!=_revision){_generation++;_document=doc;_revision=doc?.Revision??-1;}return _id+":"+_generation;}
}
public static class McpTaskImport {
 public static IReadOnlyList<McpImportedTask> Parse(string resultJson){
  using var result=JsonDocument.Parse(resultJson,new(){MaxDepth=32});var root=result.RootElement;if(root.TryGetProperty("isError",out var error)&&error.ValueKind==JsonValueKind.True)throw new ArgumentException("The server returned a tool error; it cannot be imported.");
  if(root.TryGetProperty("structuredContent",out var data)&&data.ValueKind==JsonValueKind.Object&&data.TryGetProperty("tasks",out var tasks))return Rows(tasks);
  if(root.TryGetProperty("content",out var blocks))foreach(var block in blocks.EnumerateArray())if(block.TryGetProperty("type",out var type)&&type.GetString()=="text"&&block.TryGetProperty("text",out var text)){try{using var body=JsonDocument.Parse(text.GetString()!);if(body.RootElement.TryGetProperty("tasks",out var values))return Rows(values);}catch(JsonException){}}
  throw new ArgumentException("This result has no tasks array. Use {\"tasks\":[{\"name\":\"Task\",\"duration\":\"2d\"}]} in structuredContent or JSON text.");
 }
 private static IReadOnlyList<McpImportedTask> Rows(JsonElement data){if(data.ValueKind!=JsonValueKind.Array||data.GetArrayLength() is <1 or >500)throw new ArgumentException("Import between 1 and 500 tasks.");var rows=new List<McpImportedTask>();foreach(var row in data.EnumerateArray()){
  string Value(string name,string fallback=""){if(!row.TryGetProperty(name,out var value)||value.ValueKind==JsonValueKind.Null)return fallback;if(value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))throw new ArgumentException(name+" must be text or a number.");return value.ValueKind==JsonValueKind.String?value.GetString()!:value.GetRawText();}
  if(row.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Each task must be an object.");var item=new McpImportedTask(Value("name"),Value("duration","1d"),Value("start"),Value("finish"),Value("percentComplete","0"),Value("notes"));if(string.IsNullOrWhiteSpace(item.Name)||item.Name.Length>1000||item.Notes.Length>32768)throw new ArgumentException("Check task names and notes lengths.");var duration=EditorInput.Duration(item.Duration);if(duration?.Value<0)throw new ArgumentException("Duration cannot be negative.");var start=EditorInput.Date(item.Start);var finish=EditorInput.Date(item.Finish);if(start.HasValue&&finish.HasValue&&finish<start)throw new ArgumentException("Finish must be on or after Start.");if(!int.TryParse(item.Progress,NumberStyles.Integer,CultureInfo.InvariantCulture,out int progress)||progress is <0 or >100)throw new ArgumentException("Completion must be a whole percentage from 0 to 100.");rows.Add(item);
 }return rows;}
 public static IReadOnlyList<int> Apply(ProjectSession session,IReadOnlyList<McpImportedTask> selected){if(selected.Count is <1 or >500)throw new ArgumentException("Select between 1 and 500 tasks.");string Quote(string value)=>"\""+value.Replace("\"","\"\"")+"\"";string text=string.Join("\n",selected.Select(t=>string.Join("\t",new[]{t.Name,t.Duration,"Auto",t.Start,t.Finish,t.Progress,t.Notes}.Select(Quote))));return session.PasteTasks(null,["Name","Duration","TaskMode","Start","Finish","PercentComplete","Notes"],"Name",text);}
}

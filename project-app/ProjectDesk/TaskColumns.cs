using OfficeIMO.Project;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace ProjectDesk;

public sealed record TaskColumn(string Key, string Title, double Width, Func<ProjectTask, ProjectDocument, string?> Read) {
    public override string ToString() => Title;
}

public static class TaskColumns {
    public static readonly string[] ProjectEntryPreset = ["Indicators", "TaskMode", "Name", "Duration", "Start", "Finish", "Predecessors", "ResourceNames"];
    public static readonly string[] Defaults = ["DisplayId", "TaskMode", "Name", "Duration", "Start", "Finish", "Predecessors", "ResourceNames"];
    public static IReadOnlyList<TaskColumn> Catalogue(ProjectDocument? document) {
        var columns = new List<TaskColumn> {
            new("Indicators", "Indicators", 95, (t,p) => string.Join(", ", new[]{ t.IsMilestone == true ? "Milestone" : null, t.IsCritical == true ? "Critical" : null, t.Deadline.HasValue ? "Deadline" : null }.Where(v=>v!=null))),
            new("TaskMode", "Task Mode", 70, (t,p)=>t.IsManual.HasValue ? t.IsManual.Value ? "Manual" : "Auto" : null),
            new("Predecessors", "Predecessors", 100, (t,p)=>Links(p.Dependencies.Where(d=>d.Successor?.Uid==t.Uid), true)),
            new("Successors", "Successors", 140, (t,p)=>Links(p.Dependencies.Where(d=>d.Predecessor?.Uid==t.Uid), false)),
            new("ResourceNames", "Resource Names", 130, (t,p)=>string.Join(", ", p.Assignments.Where(a=>a.Task?.Uid==t.Uid && a.Resource?.Uid>0).Select(a=>a.Resource!.Name))),
            new("CalendarName", "Task Calendar", 160, (t,p)=>t.Calendar?.Name),
            new("OutlineLevel", "Outline Level", 100, (t,p)=>Depth(t).ToString())
        };
        foreach (var property in typeof(ProjectTask).GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
                type == typeof(ProjectDuration) || type == typeof(ProjectWork) || type == typeof(ProjectUnits) || type == typeof(Guid))) continue;
            if (property.Name is "Revision" or "IsModified" or "IsAttached") continue;
            var title = property.Name switch {
                "DisplayId" => "ID", "Uid" => "Unique ID", "Name" => "Task Name", "Wbs" => "WBS", "Type" => "Task Type",
                "IsManual" => "Manually Scheduled", "IsCritical" => "Critical", "IsActive" => "Active", "IsNull" => "Blank Task",
                "IsMilestone" => "Milestone", "IsSummary" => "Summary", "PercentComplete" => "% Complete", "PercentWorkComplete" => "% Work Complete",
                "PhysicalPercentComplete" => "Physical % Complete", "TotalSlackMinutes" => "Total Slack (min)", "FreeSlackMinutes" => "Free Slack (min)",
                _ => Regex.Replace(property.Name, "(?<=[a-z])([A-Z])", " $1")
            };
            columns.Add(new(property.Name, title, property.Name switch{"DisplayId"=>72,"Name"=>240,"Duration"=>85,_=>type==typeof(DateTime)?110:120},
                (task, project) => property.Name == "Name" ? new string(' ', Math.Min(20, Depth(task)*2)) + task.Name : Format(property.GetValue(task))));
        }
        for (int n = 0; n <= 10; n++) {
            int number = n;
            foreach (var property in typeof(ProjectBaseline).GetProperties().Where(p=>p.Name is "Start" or "Finish" or "Duration" or "Work" or "Cost" or "FixedCost" or "Bcws" or "Bcwp")) {
                var title = "Baseline" + (n == 0 ? "" : n.ToString()) + " " + Regex.Replace(property.Name, "(?<=[a-z])([A-Z])", " $1");
                columns.Add(new($"Baseline{n}.{property.Name}", title, 160,
                    (t,p)=>Format(t.Baselines.FirstOrDefault(b=>(b.Number??0)==number) is ProjectBaseline b ? property.GetValue(b) : null)));
            }
        }
        if (document != null) {
            var ids = document.AllTasks.SelectMany(t=>t.CustomFields).Select(f=>f.FieldId)
                .Concat(document.CustomFields.Select(f=>f.FieldId)).Where(id=>id!=null).Distinct();
            foreach (var id in ids) {
                var definition = document.CustomFields.FirstOrDefault(f=>f.FieldId==id);
                columns.Add(new("Custom."+id, definition?.Alias ?? definition?.FieldName ?? "Custom field "+id, 180,
                    (t,p)=>t.CustomFields.FirstOrDefault(f=>f.FieldId==id)?.Value));
            }
        }
        return columns.GroupBy(c=>c.Key).Select(g=>g.First()).OrderBy(c=>c.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }
    public static string? Format(object? value) => value switch {
        null => null, DateTime d => d.ToString("dd/MM/yy", CultureInfo.InvariantCulture),
        ProjectDuration d => $"{d.Value:0.##} {(d.IsElapsed ? "elapsed " : "")}{d.Unit.ToString().ToLowerInvariant()}{(d.Value==1 ? "" : "s")}{(d.IsEstimated ? "?" : "")}",
        ProjectWork w => $"{w.Minutes/60m:0.##} h", ProjectUnits u => $"{u.Value*100:0.##}%",
        bool b => b ? "Yes" : "No", decimal m => m.ToString("0.##", CultureInfo.InvariantCulture),
        Enum e => Regex.Replace(e.ToString(), "(?<=[a-z])([A-Z])", " $1"), _ => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
    private static string Links(IEnumerable<ProjectDependency> links, bool predecessor) => string.Join(", ", links.Select(d=> {
        var task = predecessor ? d.Predecessor : d.Successor;
        var kind = d.Type switch { ProjectDependencyType.FinishToStart=>"FS", ProjectDependencyType.StartToStart=>"SS", ProjectDependencyType.FinishToFinish=>"FF", ProjectDependencyType.StartToFinish=>"SF", _=>"?" };
        var lag = d.Lag is ProjectDuration l && l.Value != 0 ? (l.Value>0 ? "+" : "") + Format(l) : "";
        return $"{task?.DisplayId ?? task?.Uid}{kind}{lag}";
    }));
    private static int Depth(ProjectTask task) { int depth=0; for(var p=task.Parent;p!=null;p=p.Parent) depth++; return depth; }
}

public sealed class TaskTableRow : System.ComponentModel.INotifyPropertyChanged {
    public int Uid { get; }
    public string? RawName { get; }
    public bool IsSummary { get; }
    public Avalonia.Media.FontWeight Weight => IsSummary ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal;
    public Dictionary<string,string?> Cells { get; private set; }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private TaskTableRow(IReadOnlyList<TaskColumn> columns,int uid){Uid=uid;Cells=columns.ToDictionary(c=>c.Key,_=>(string?)"");}
    public static TaskTableRow EntryRow(IReadOnlyList<TaskColumn> columns,int uid=-1)=>new(columns,uid);
    public void UpdateCell(string key,string? value){
        if(Cells.GetValueOrDefault(key)==value)return;
        Cells=new(Cells){[key]=value};PropertyChanged?.Invoke(this,new(nameof(Cells)));
    }
    public void Refresh(ProjectTask task,ProjectDocument document,IReadOnlyList<TaskColumn> columns){Cells=columns.ToDictionary(c=>c.Key,c=>c.Read(task,document));PropertyChanged?.Invoke(this,new(nameof(Cells)));}
    public TaskTableRow(ProjectTask task, ProjectDocument document, IReadOnlyList<TaskColumn> columns) {
        Uid=task.Uid; RawName=task.Name; IsSummary=task.IsSummary;
        Cells=columns.ToDictionary(c=>c.Key,c=>c.Read(task,document));
    }
}

public sealed record ColumnSetting(string Key, double Width);
public sealed class ViewPreferences {
    public List<SavedWorkspaceView> SavedViews {get;set;}=[];
    public bool CompactWorkspace {get;set;}=true;
    public bool ShowInspector {get;set;}
    public double InspectorWidth {get;set;}=300;
    public string ThemeMode {get;set;}="Automatic";
    public bool GanttColorByProgress {get;set;}
    public bool GanttProgressLabels {get;set;}
    public Dictionary<string,ProjectAppearance> Appearance {get;set;}=[];
    public List<string> RecentColors {get;set;}=[];
    public List<ColumnSetting> Columns { get; set; } = TaskColumns.Defaults.Select(k=>new ColumnSetting(k,0)).ToList();
    public bool ShowTimeline { get; set; } = true;
    public bool ReduceTransparency {get;set;}
    public double TimelineHeight { get; set; } = 130;
    public double FontSize {get;set;}=13;
    public string? FontFamilyName {get;set;}
    public double TableFraction {get;set;}=.38;
    public double RowHeight {get;set;}=28;
    public bool Gridlines {get;set;}=true;
    public bool CriticalTasks {get;set;}=true;
    public int BaselineNumber {get;set;}
    public bool ShowSidebar { get; set; } = false;
    public bool ClaudeCodeControl {get;set;}=true;
    public Dictionary<string,List<int>> TimelineTasks { get; set; } = [];
    public Dictionary<string,TimelineLayout> TimelineLayouts {get;set;}=[];
    public Dictionary<string,DateAxisView> GanttAxes {get;set;}=[];
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjectDesk", "views.json");
    public static ViewPreferences Load(string path) {
        if (!File.Exists(path)) return new();
        try { return JsonSerializer.Deserialize<ViewPreferences>(File.ReadAllText(path)) ?? new(); }
        catch (JsonException) { return new(); }
    }
    public void Save(string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp=path+"."+Guid.NewGuid()+".tmp";
        try { File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true})); File.Move(temp,path,true); }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
}

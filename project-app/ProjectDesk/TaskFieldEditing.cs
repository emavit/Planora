using Avalonia.Controls;
using OfficeIMO.Project;
using System.Globalization;
using System.Reflection;

namespace ProjectDesk;

public sealed record TaskFieldChange(string Key,object? Value);
public static class TaskFieldEditing {
    private static readonly HashSet<string> Fields=["Name","Duration","IsManual","IsActive","IsMilestone","EffortDriven","Type","Start","Finish","ConstraintType","ConstraintDate","Deadline","Priority","FixedCost","Wbs","Contact","Notes","PercentComplete","PhysicalPercentComplete"];
    public static string PropertyKey(string key)=>key=="TaskMode"?"IsManual":key;
    public static PropertyInfo? Property(string key){key=PropertyKey(key);return Fields.Contains(key)?typeof(ProjectTask).GetProperty(key):null;}
    public static object? Parse(string key,string text) {
        var p=Property(key)??throw new ArgumentException("This column is calculated or read-only. Use the task, resource or baseline commands to change its inputs.");
        var type=Nullable.GetUnderlyingType(p.PropertyType)??p.PropertyType;
        if(string.IsNullOrWhiteSpace(text)){if(key is "Name" or "PercentComplete")throw new ArgumentException("This field requires a value.");if(p.PropertyType==typeof(string))return text;if(Nullable.GetUnderlyingType(p.PropertyType)!=null)return null;throw new ArgumentException("This field requires a value.");}
        text=text.Trim();
        object value=type==typeof(string)?text:type==typeof(ProjectDuration)?EditorInput.Duration(text)!.Value:type==typeof(DateTime)?EditorInput.Date(text)!.Value:type==typeof(bool)?ParseBoolean(text):type.IsEnum?Enum.Parse(type,text.Replace(" ",""),true):Convert.ChangeType(text.TrimEnd('%'),type,CultureInfo.InvariantCulture);
        if(key is "PercentComplete" or "PhysicalPercentComplete" && Convert.ToDecimal(value) is decimal percent && (percent<0||percent>100))throw new ArgumentException("Completion must be between 0 and 100.");
        if(key=="Priority"&&(Convert.ToInt32(value)<0||Convert.ToInt32(value)>1000))throw new ArgumentException("Priority must be between 0 and 1000.");
        if(key=="Name"&&string.IsNullOrWhiteSpace(text))throw new ArgumentException("Enter a task name.");
        return value;
    }
    private static bool ParseBoolean(string text)=>text.ToLowerInvariant() switch{"yes" or "true" or "manual" or "manually scheduled"=>true,"no" or "false" or "auto" or "auto scheduled" or "automatically scheduled"=>false,_=>throw new ArgumentException("Enter Yes/No or Auto Scheduled/Manually Scheduled.")};
}

public sealed class TaskFieldEditorWindow : EditorForm<TaskFieldChange> {
    private readonly string _key;
    private readonly TextBox? _text;
    private readonly ComboBox? _choice;
    public TaskFieldEditorWindow(ProjectTask task,string key,string title):base("Edit "+title) {
        _key=TaskFieldEditing.PropertyKey(key);var p=TaskFieldEditing.Property(key)??throw new InvalidOperationException("This column is calculated or read-only. Use its planning commands to edit the inputs.");
        var type=Nullable.GetUnderlyingType(p.PropertyType)??p.PropertyType;var value=p.GetValue(task);Height=330;
        if(type==typeof(bool)||type.IsEnum){var options=new List<string>{""};options.AddRange(type==typeof(bool)?new[]{"False","True"}:Enum.GetNames(type));_choice=new(){ItemsSource=options,SelectedItem=value?.ToString()??""};Field(title,_choice,type==typeof(bool)&&_key=="IsManual"?"True = manually scheduled; False = automatically scheduled.":null);}
        else {_text=new(){Text=value switch{ProjectDuration d=>EditorInput.DurationText(d),DateTime date=>EditorInput.DateText(date),_=>Convert.ToString(value,CultureInfo.InvariantCulture)}};Field(title,_text,type==typeof(ProjectDuration)?"Examples: 2d, 4h, 1w.":type==typeof(DateTime)?"Local date and time: yyyy-MM-dd HH:mm.":null);}
    }
    protected override TaskFieldChange Read()=>new(_key,TaskFieldEditing.Parse(_key,_text?.Text??_choice?.SelectedItem?.ToString()??""));
}

public sealed record CalculationEdit(bool ActualCosts,bool Assignments);
public sealed class CalculationEditorWindow : EditorForm<CalculationEdit> {
    private readonly CheckBox _actual=new(){Content="Recalculate actual costs from resource rates",IsChecked=false};
    private readonly CheckBox _assignments=new(){Content="Calculate resource work and costs",IsChecked=true};
    public CalculationEditorWindow(bool imported=false):base("Calculate Project"){Height=370;_assignments.IsChecked=!imported;Field("Resource calculation",_assignments,"Task-only calculation preserves stored resource totals. Imported resource distributions require additional compatibility support.");Field("Cost calculation",_actual,"Leave this unchecked to retain recorded actual costs.");}
    protected override CalculationEdit Read()=>new(_actual.IsChecked==true,_assignments.IsChecked==true);
}

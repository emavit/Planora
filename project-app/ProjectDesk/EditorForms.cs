using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using OfficeIMO.Project;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectDesk;

public static class EditorInput {
    public static decimal? Number(string? text)=>string.IsNullOrWhiteSpace(text)?null:decimal.Parse(text.Trim().TrimEnd('%'),CultureInfo.InvariantCulture);
    public static DateTime? Date(string? text)=>string.IsNullOrWhiteSpace(text)?null:DateTime.SpecifyKind(DateTime.Parse(text,CultureInfo.InvariantCulture),DateTimeKind.Unspecified);
    public static string DateText(DateTime? value)=>value?.ToString("yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture)??"";
    public static string DurationText(ProjectDuration? duration)=>duration is ProjectDuration d?$"{d.Value.ToString(CultureInfo.InvariantCulture)}{(d.IsElapsed?"e":"")}{d.Unit switch{ProjectDurationUnit.Minute=>"min",ProjectDurationUnit.Hour=>"h",ProjectDurationUnit.Week=>"w",ProjectDurationUnit.Month=>"mo",_=>"d"}}{(d.IsEstimated?"?":"")}":"";
    public static ProjectDuration? Duration(string? text,bool allowNegative=false) {
        if(string.IsNullOrWhiteSpace(text))return null;
        var m=Regex.Match(text.Trim(),@"^([+-]?\d+(?:\.\d+)?)\s*(e|elapsed\s+)?\s*(min(?:ute)?s?|h(?:our)?s?|d(?:ay)?s?|w(?:eek)?s?|mo(?:nth)?s?)?\s*(\?)?$",RegexOptions.IgnoreCase);
        if(!m.Success)throw new ArgumentException("Enter a duration such as 2d, 4h, 30min, 1w, 2mo or 3ed.");
        var value=decimal.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture);if(value<0&&!allowNegative)throw new ArgumentException("Task durations cannot be negative.");
        string unit=m.Groups[3].Value.ToLowerInvariant();var kind=unit.StartsWith("min")?ProjectDurationUnit.Minute:unit.StartsWith("h")?ProjectDurationUnit.Hour:unit.StartsWith("w")?ProjectDurationUnit.Week:unit.StartsWith("mo")?ProjectDurationUnit.Month:ProjectDurationUnit.Day;
        return new(value,kind,m.Groups[2].Success,m.Groups[4].Success);
    }
    public static ProjectWorkingTime[] WorkingTimes(string? text)=>string.IsNullOrWhiteSpace(text)?[]:text.Split(';',StringSplitOptions.RemoveEmptyEntries).Select(range=>{
        var p=range.Split('-',StringSplitOptions.TrimEntries);if(p.Length!=2)throw new ArgumentException("Enter working intervals such as 08:00-12:00; 13:00-17:00.");
        return new ProjectWorkingTime(TimeSpan.Parse(p[0],CultureInfo.InvariantCulture),TimeSpan.Parse(p[1],CultureInfo.InvariantCulture));
    }).ToArray();
}
public sealed record Choice(int? Uid,string Name){public override string ToString()=>Name;}

public abstract class EditorForm<T> : Window {
    protected readonly StackPanel Fields=new(){Spacing=12};
    protected readonly TextBlock Error=new(){Foreground=ChartDrawing.Critical,TextWrapping=TextWrapping.Wrap};
    protected EditorForm(string title){Title=title;Width=620;Height=740;MinWidth=480;MinHeight=380;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=DesignTokens.Background;FontFamily=DesignTokens.Font;
        var root=new DockPanel{Margin=new Thickness(22)};var label=new TextBlock{Text=title,FontSize=22,FontWeight=FontWeight.SemiBold,Margin=new Thickness(0,0,0,18)};DockPanel.SetDock(label,Dock.Top);root.Children.Add(label);
        var footer=new StackPanel{Spacing=12,Margin=new Thickness(0,16,0,0)};footer.Children.Add(Error);var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right};
        var cancel=new Button{Content="Cancel"};cancel.Click+=(_,_)=>Close(null);var save=new Button{Content="Apply"};save.Classes.Add("primary");save.Click+=(_,_)=>{try{Close(Read());}catch(Exception e){Error.Text=e.Message;}};actions.Children.Add(cancel);actions.Children.Add(save);footer.Children.Add(actions);DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        root.Children.Add(new ScrollViewer{Content=Fields});Content=root;
    }
    protected void Field(string label,Control control,string? hint=null){Avalonia.Automation.AutomationProperties.SetName(control,label);var p=new StackPanel{Spacing=4};p.Children.Add(new TextBlock{Text=label,FontSize=12,FontWeight=FontWeight.SemiBold});p.Children.Add(control);if(hint!=null)p.Children.Add(new TextBlock{Text=hint,FontSize=11,Foreground=DesignTokens.Secondary,TextWrapping=TextWrapping.Wrap});Fields.Children.Add(p);}
    protected static ComboBox EnumChoice<TEnum>(TEnum? current)where TEnum:struct,Enum=>new(){ItemsSource=Enum.GetValues<TEnum>(),SelectedItem=current,HorizontalAlignment=HorizontalAlignment.Stretch};
    protected static ComboBox CalendarChoice(ProjectDocument document,int? uid){var values=new[]{new Choice(null,"Use the project calendar")}.Concat(document.Calendars.Where(c=>c.Uid>0).Select(c=>new Choice(c.Uid,c.Name??"Calendar "+c.Uid))).ToList();return new(){ItemsSource=values,SelectedItem=values.FirstOrDefault(c=>c.Uid==uid)??values[0],HorizontalAlignment=HorizontalAlignment.Stretch};}
    protected abstract T Read();
}

public sealed class TaskEditorWindow : EditorForm<TaskEdit> {
    private readonly TextBox _name,_duration,_start,_finish,_constraintDate,_work,_cost,_progress,_notes;
    private readonly CheckBox _manual,_milestone,_effort;
    private readonly ComboBox _type,_constraint,_calendar;
    public TaskEditorWindow(ProjectDocument document,int uid):base("Task information") {
        var t=document.Tasks.GetByUid(uid);var v=TaskEdit.From(t);
        _name=new(){Text=v.Name};_duration=new(){Text=EditorInput.DurationText(v.Duration)};_start=new(){Text=EditorInput.DateText(v.Start)};_finish=new(){Text=EditorInput.DateText(v.Finish)};
        _manual=new(){Content="Manually scheduled",IsChecked=v.Manual};_milestone=new(){Content="Milestone",IsChecked=v.Milestone};_effort=new(){Content="Effort driven",IsChecked=v.EffortDriven};
        _type=EnumChoice(v.Type);_constraint=EnumChoice(v.ConstraintType);_calendar=CalendarChoice(document,v.CalendarUid);
        _constraintDate=new(){Text=EditorInput.DateText(v.ConstraintDate)};_work=new(){Text=v.WorkHours?.ToString(CultureInfo.InvariantCulture)};_cost=new(){Text=v.FixedCost?.ToString(CultureInfo.InvariantCulture)};
        _progress=new(){Text=v.PercentComplete?.ToString()};_notes=new(){Text=v.Notes,AcceptsReturn=true,Height=100};
        Field("Task name",_name);Field("Duration",_duration,"Examples: 2d, 4h, 1w, 2mo. Use e for elapsed time, ? for an estimate.");Field("Task mode",_manual);Field("Task type",_type);Field("Task options",new StackPanel{Children={_milestone,_effort}});
        Field("Start",_start,"Local project date and time: yyyy-MM-dd HH:mm. Calculate Project applies scheduling rules explicitly.");Field("Finish",_finish);Field("Constraint type",_constraint);Field("Constraint date",_constraintDate);
        Field("Task calendar",_calendar);Field("Work (hours)",_work);Field("Fixed cost",_cost);Field("% Complete",_progress);Field("Notes",_notes);
    }
    protected override TaskEdit Read(){var p=EditorInput.Number(_progress.Text);if(p.HasValue&&p.Value!=decimal.Truncate(p.Value))throw new ArgumentException("Completion must be a whole percentage.");var value=new TaskEdit(_name.Text??"",EditorInput.Duration(_duration.Text),_manual.IsChecked,_type.SelectedItem as ProjectTaskType?,_milestone.IsChecked,_effort.IsChecked,EditorInput.Date(_start.Text),EditorInput.Date(_finish.Text),_constraint.SelectedItem as ProjectConstraintType?,EditorInput.Date(_constraintDate.Text),EditorInput.Number(_work.Text),EditorInput.Number(_cost.Text),p.HasValue?(int)p.Value:null,_notes.Text,(_calendar.SelectedItem as Choice)?.Uid);value.Validate();return value;}
}

public sealed record DependencyEdit(int PredecessorUid,int SuccessorUid,ProjectDependencyType Type,ProjectDuration Lag);
public sealed class DependencyEditorWindow : EditorForm<DependencyEdit> {
    private readonly ComboBox _from,_to,_type;
    private readonly TextBox _lag=new(){Text="0d"};
    public DependencyEditorWindow(IEnumerable<ProjectTask> tasks,int? from,int? to):base("Link tasks"){
        var values=tasks.Select(t=>new Choice(t.Uid,$"{t.DisplayId} · {t.Name}")).ToList();_from=new(){ItemsSource=values,SelectedItem=values.FirstOrDefault(c=>c.Uid==from)};_to=new(){ItemsSource=values,SelectedItem=values.FirstOrDefault(c=>c.Uid==to)};_type=EnumChoice<ProjectDependencyType>(ProjectDependencyType.FinishToStart);
        Field("Predecessor",_from);Field("Successor",_to);Field("Link type",_type);Field("Lag / lead",_lag,"Examples: +2d delay, -4h lead, +1ed elapsed delay.");Height=470;
    }
    protected override DependencyEdit Read(){if(_from.SelectedItem is not Choice{Uid:int a}||_to.SelectedItem is not Choice{Uid:int b}||a==b)throw new ArgumentException("Choose two different tasks.");return new(a,b,(ProjectDependencyType)_type.SelectedItem!,EditorInput.Duration(_lag.Text,true)??ProjectDuration.WorkingDays(0));}
}

public sealed class ResourceEditorWindow : EditorForm<ResourceEdit> {
    private readonly TextBox _name,_units,_rate,_overtime,_perUse,_group,_initials;
    private readonly ComboBox _type,_calendar;
    public ResourceEditorWindow(ProjectDocument document,ProjectResource? resource=null):base(resource==null?"Add resource":"Resource information"){
        _name=new(){Text=resource?.Name};_units=new(){Text=((resource?.MaxUnits?.Value??1)*100).ToString(CultureInfo.InvariantCulture)};_rate=new(){Text=(resource?.StandardRate??0).ToString(CultureInfo.InvariantCulture)};_overtime=new(){Text=(resource?.OvertimeRate??0).ToString(CultureInfo.InvariantCulture)};
        _perUse=new(){Text=resource?.CostPerUse?.ToString(CultureInfo.InvariantCulture)};_group=new(){Text=resource?.Group};_initials=new(){Text=resource?.Initials};_type=EnumChoice<ProjectResourceType>(resource?.Type??ProjectResourceType.Work);_calendar=CalendarChoice(document,resource?.Calendar?.Uid);
        Field("Resource name",_name);Field("Resource type",_type);Field("Maximum units (%)",_units);Field("Standard rate",_rate);Field("Overtime rate",_overtime);Field("Cost per use",_perUse);Field("Initials",_initials);Field("Group",_group);Field("Calendar",_calendar);
    }
    protected override ResourceEdit Read(){var value=new ResourceEdit(_name.Text??"",_type.SelectedItem as ProjectResourceType?,EditorInput.Number(_units.Text)??100,EditorInput.Number(_rate.Text)??0,EditorInput.Number(_overtime.Text)??0,EditorInput.Number(_perUse.Text),_group.Text,_initials.Text,(_calendar.SelectedItem as Choice)?.Uid);if(string.IsNullOrWhiteSpace(value.Name)||value.MaxUnitsPercent<0||value.StandardRate<0||value.OvertimeRate<0)throw new ArgumentException("Check the resource name, capacity and rates.");return value;}
}

public sealed record AssignmentEdit(int ResourceUid,decimal UnitsPercent,decimal? WorkHours,bool Remove);
public sealed class AssignmentEditorWindow : EditorForm<AssignmentEdit> {
    private readonly ComboBox _resource;
    private readonly TextBox _units=new(){Text="100"},_work=new();
    private readonly CheckBox _remove=new(){Content="Remove this resource from the task"};
    public AssignmentEditorWindow(ProjectDocument document,int taskUid,int? resourceUid=null):base("Assign resources"){
        var values=document.Resources.Where(r=>r.Uid>0).Select(r=>new Choice(r.Uid,r.Name??"Resource "+r.Uid)).ToList();_resource=new(){ItemsSource=values};
        _resource.SelectionChanged+=(_,_)=>{var id=(_resource.SelectedItem as Choice)?.Uid;var a=document.Assignments.FirstOrDefault(a=>a.Task?.Uid==taskUid&&a.Resource?.Uid==id);_units.Text=((a?.Units?.Value??1)*100).ToString(CultureInfo.InvariantCulture);_work.Text=a?.Work?.Minutes is decimal m?(m/60).ToString(CultureInfo.InvariantCulture):"";};
        if(resourceUid.HasValue)_resource.SelectedItem=values.FirstOrDefault(r=>r.Uid==resourceUid);
        Field("Task",new TextBlock{Text=document.Tasks.GetByUid(taskUid).Name});Field("Resource",_resource);Field("Assignment units (%)",_units);Field("Work (hours)",_work,"Leave empty to retain current work or let the scheduler calculate it.");Field("Remove assignment",_remove);Height=530;
    }
    protected override AssignmentEdit Read(){if(_resource.SelectedItem is not Choice{Uid:int id})throw new ArgumentException("Choose a resource. Add it in the Resource tab if needed.");var units=EditorInput.Number(_units.Text)??100;var work=EditorInput.Number(_work.Text);if(units<0||work<0)throw new ArgumentException("Units and work cannot be negative.");return new(id,units,work,_remove.IsChecked==true);}
}

public sealed record BaselineEdit(int Number,bool Clear,bool Overwrite);
public sealed class BaselineEditorWindow : EditorForm<BaselineEdit> {
    private readonly ComboBox _number=new(){ItemsSource=Enumerable.Range(0,11).ToList(),SelectedIndex=0};
    private readonly CheckBox _clear=new(){Content="Clear this baseline"},_overwrite=new(){Content="Replace an existing baseline"};
    public BaselineEditorWindow(bool clear=false):base(clear?"Clear baseline":"Set baseline"){_clear.IsChecked=clear;Field("Baseline number",_number);Field("Existing baseline",_overwrite);Field("Clear",_clear);Fields.Children.Add(new TextBlock{Text="Captures the currently stored dates, duration, work and costs. Calculate the project first if you want a fresh schedule. Detailed timephased baseline curves are not captured by this preview.",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary});Height=400;}
    protected override BaselineEdit Read()=>new((int)_number.SelectedItem!,_clear.IsChecked==true,_overwrite.IsChecked==true);
}

public sealed record CalendarEdit(int CalendarUid,Dictionary<DayOfWeek,ProjectWorkingTime[]> Days);
public sealed class CalendarEditorWindow : EditorForm<CalendarEdit> {
    private readonly ComboBox _calendar;
    private readonly Dictionary<DayOfWeek,(CheckBox Working,TextBox Times)> _days=[];
    private readonly ProjectDocument _document;
    public CalendarEditorWindow(ProjectDocument document):base("Change working time"){
        _document=document;var values=document.Calendars.Where(c=>c.Uid>0).Select(c=>new Choice(c.Uid,c.Name??"Calendar "+c.Uid)).ToList();_calendar=new(){ItemsSource=values,SelectedItem=values.FirstOrDefault(c=>c.Uid==document.Calendar?.Uid)??values.FirstOrDefault()};Field("Calendar",_calendar);
        foreach(var day in new[]{DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday,DayOfWeek.Friday,DayOfWeek.Saturday,DayOfWeek.Sunday}){var working=new CheckBox{Content="Working day"};var times=new TextBox();_days[day]=(working,times);Field(day.ToString(),new StackPanel{Spacing=4,Children={working,times}});}
        _calendar.SelectionChanged+=(_,_)=>LoadDays();LoadDays();
    }
    private void LoadDays(){if(_calendar.SelectedItem is not Choice{Uid:int uid})return;var calendar=_document.Calendars.GetByUid(uid);foreach(var (day,controls)in _days){ProjectWeekDay? definition=null;for(var c=calendar;c!=null&&definition==null;c=c.BaseCalendar)definition=c.WeekDays.FirstOrDefault(d=>d.Day==day);controls.Working.IsChecked=definition?.IsWorking??false;controls.Times.Text=definition==null?"":string.Join("; ",definition.WorkingTimes.Select(t=>$"{t.From:hh\\:mm}-{t.To:hh\\:mm}"));}}
    protected override CalendarEdit Read(){if(_calendar.SelectedItem is not Choice{Uid:int uid})throw new ArgumentException("Choose a calendar.");var days=_days.ToDictionary(p=>p.Key,p=>p.Value.Working.IsChecked==true?EditorInput.WorkingTimes(p.Value.Times.Text):Array.Empty<ProjectWorkingTime>());if(!days.Values.Any(v=>v.Length>0))throw new ArgumentException("A calendar needs at least one working interval.");return new(uid,days);}
}

public sealed record ProjectInformationEdit(string Name,DateTime Start,DateTime? Status,int? CalendarUid);
public sealed record CalendarExceptionEdit(int CalendarUid,string Name,DateTime Start,DateTime Finish,ProjectWorkingTime[] Times,bool Delete);
public sealed class CalendarExceptionEditorWindow : EditorForm<CalendarExceptionEdit> {
    private readonly ComboBox _calendar;private readonly TextBox _name=new(),_start=new(){Text=DateTime.Today.ToString("yyyy-MM-dd")},_finish=new(){Text=DateTime.Today.ToString("yyyy-MM-dd")},_times=new();private readonly CheckBox _delete=new(){Content="Delete the exception with this name"};
    public CalendarExceptionEditorWindow(ProjectDocument document):base("Calendar exceptions"){
        _calendar=CalendarChoice(document,document.Calendar?.Uid);Field("Calendar",_calendar);Field("Exception name",_name,"An existing name updates that exception; a new name adds one.");Field("First date",_start);Field("Last date (inclusive)",_finish);Field("Working intervals",_times,"Leave blank for a holiday. Example special working day: 09:00-13:00.");Field("Remove exception",_delete);
        var list=new TextBlock{TextWrapping=TextWrapping.Wrap};Field("Existing exceptions",list);void Load(){if(_calendar.SelectedItem is Choice{Uid:int uid})list.Text=string.Join("\n",document.Calendars.GetByUid(uid).Exceptions.Select(e=>$"{e.Name}: {e.FromDate:yyyy-MM-dd} – {e.ToDate:yyyy-MM-dd}"));} _calendar.SelectionChanged+=(_,_)=>Load();Load();
    }
    protected override CalendarExceptionEdit Read(){if(_calendar.SelectedItem is not Choice{Uid:int uid})throw new ArgumentException("Choose a calendar.");return new(uid,_name.Text??"",EditorInput.Date(_start.Text)??throw new ArgumentException("Enter the first date."),EditorInput.Date(_finish.Text)??throw new ArgumentException("Enter the last date."),EditorInput.WorkingTimes(_times.Text),_delete.IsChecked==true);}
}
public sealed class ProjectInformationWindow : EditorForm<ProjectInformationEdit> {
    private readonly TextBox _name,_start,_status;private readonly ComboBox _calendar;
    public ProjectInformationWindow(ProjectDocument document):base("Project information"){
        _name=new(){Text=document.Name};_start=new(){Text=EditorInput.DateText(document.Settings.StartDate)};_status=new(){Text=EditorInput.DateText(document.Settings.StatusDate)};_calendar=CalendarChoice(document,document.Calendar?.Uid);
        Field("Project name",_name);Field("Start date and time",_start,"yyyy-MM-dd HH:mm");Field("Status date and time",_status);Field("Project calendar",_calendar);Height=510;
    }
    protected override ProjectInformationEdit Read(){if(string.IsNullOrWhiteSpace(_name.Text))throw new ArgumentException("Enter a project name.");return new(_name.Text,EditorInput.Date(_start.Text)??throw new ArgumentException("Enter the project start."),EditorInput.Date(_status.Text),(_calendar.SelectedItem as Choice)?.Uid);}
}

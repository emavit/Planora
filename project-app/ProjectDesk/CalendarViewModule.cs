using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using OfficeIMO.Project;
using System.Globalization;

namespace ProjectDesk;

/// <summary>Monthly stored-date task view; navigation has no effect on the project schedule.</summary>
public sealed class CalendarViewModule : UserControl {
    private readonly TextBlock _heading=new(){FontSize=18,FontWeight=FontWeight.SemiBold,VerticalAlignment=VerticalAlignment.Center};
    private readonly Grid _month=new(){ColumnDefinitions=new ColumnDefinitions("*,*,*,*,*,*,*"),RowDefinitions=new RowDefinitions("26,*,*,*,*,*,*"),MinHeight=680};
    private readonly CheckBox _summaries=new(){Content="Show summary tasks",VerticalAlignment=VerticalAlignment.Center};
    private readonly TextBlock _selectedHeading=new(){Foreground=DesignTokens.Secondary};
    private readonly ListBox _selectedTasks=new(){Height=72};
    private List<ProjectTask> _tasks=[];
    private DateTime _monthDate=new(DateTime.Today.Year,DateTime.Today.Month,1);
    private DateTime _selectedDate=DateTime.Today;
    private bool _initialised;
    public DateTime DisplayedMonth=>_monthDate;
    public int VisibleTaskCount{get;private set;}
    public event Action<int>? TaskActivated;
    public CalendarViewModule(){
        var root=new DockPanel();var toolbar=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(10,6),Spacing=10};
        foreach(var (text,months) in new[]{("Previous month",-1),("Today",0),("Next month",1)}){var b=new Button{Content=text};b.Click+=(_,_)=>{if(months==0){_selectedDate=DateTime.Today;SetMonth(DateTime.Today);}else SetMonth(_monthDate.AddMonths(months));};toolbar.Children.Add(b);}
        toolbar.Children.Add(_heading);toolbar.Children.Add(_summaries);DockPanel.SetDock(toolbar,Dock.Top);root.Children.Add(toolbar);
        var footer=new StackPanel{Margin=new Thickness(10,6),Spacing=5};footer.Children.Add(new TextBlock{Text="Stored task dates • milestones use ◆ • double-click a task to edit • select a day to see every matching task",Foreground=DesignTokens.Secondary,TextWrapping=TextWrapping.Wrap});footer.Children.Add(_selectedHeading);footer.Children.Add(_selectedTasks);DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        root.Children.Add(new ScrollViewer{Content=_month,AllowAutoHide=false});Content=root;
        _summaries.IsCheckedChanged+=(_,_)=>Rebuild();
        _selectedTasks.DoubleTapped+=(_,_)=>{if(_selectedTasks.SelectedItem is DayTask row)TaskActivated?.Invoke(row.Uid);};
    }
    private string? _projectKey;
    public void Refresh(ProjectSession session){string? key=session.SourcePath??session.Document?.Guid?.ToString();if(key!=_projectKey){_initialised=false;_projectKey=key;}_tasks=session.Tasks;if(!_initialised){_initialised=true;var earliest=_tasks.Where(t=>t.Start.HasValue).Select(t=>t.Start!.Value).Order().FirstOrDefault();if(earliest!=default){_selectedDate=earliest.Date;_monthDate=new(earliest.Year,earliest.Month,1);}}Rebuild();}
    public void SetMonth(DateTime date){_monthDate=new(date.Year,date.Month,1);Rebuild();}
    private IEnumerable<ProjectTask> DisplayTasks()=>_tasks.Where(t=>_summaries.IsChecked==true||!t.IsSummary);
    private static bool OccursOn(ProjectTask task,DateTime day){if(task.Start is not DateTime start)return false;var finish=task.Finish??start;return finish>=start&&start.Date<=day&&finish.Date>=day;}
    private void Rebuild(){_month.Children.Clear();_heading.Text=_monthDate.ToString("MMMM yyyy",CultureInfo.InvariantCulture);var tasks=DisplayTasks().ToArray();var nextMonth=_monthDate.AddMonths(1);VisibleTaskCount=tasks.Count(t=>t.Start.HasValue&&t.Start<nextMonth&&(t.Finish??t.Start)>=_monthDate);
        string[] names=["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"];for(int column=0;column<7;column++){var label=new TextBlock{Text=names[column],HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Foreground=DesignTokens.Secondary};Grid.SetColumn(label,column);_month.Children.Add(label);}
        int offset=((int)_monthDate.DayOfWeek+6)%7;var first=_monthDate.AddDays(-offset);
        int weeks=(offset+DateTime.DaysInMonth(_monthDate.Year,_monthDate.Month)+6)/7;_month.RowDefinitions=new RowDefinitions("26,"+string.Join(",",Enumerable.Repeat("*",weeks)));_month.MinHeight=26+weeks*80;
        for(int index=0;index<weeks*7;index++){var day=first.AddDays(index);var matches=tasks.Where(t=>OccursOn(t,day)).OrderBy(t=>t.DisplayId??t.Uid).ToArray();var stack=new StackPanel{Spacing=4,Margin=new Thickness(6)};stack.Children.Add(new TextBlock{Text=day.ToString(day.Day==1?"d MMM":"%d",CultureInfo.InvariantCulture),Foreground=day.Month==_monthDate.Month?DesignTokens.Text:DesignTokens.Secondary,FontWeight=day==DateTime.Today?FontWeight.Bold:FontWeight.Normal});
            foreach(var task in matches.Take(2)){int uid=task.Uid;var marker=task.IsMilestone==true||task.Duration?.Value==0?"◆ ":"";var caption=new TextBlock{Text=$"{marker}{task.DisplayId??uid}  {task.Name}",FontSize=11,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=task.IsCritical==true?ChartDrawing.Critical:DesignTokens.Text};var card=new Border{Background=task.IsSummary?DesignTokens.Sidebar:DesignTokens.Selected,Padding=new Thickness(4,3),CornerRadius=new CornerRadius(3),Child=caption};ToolTip.SetTip(card,$"{task.Name}\n{TaskColumns.Format(task.Start)} → {TaskColumns.Format(task.Finish)}");card.DoubleTapped+=(_,e)=>{TaskActivated?.Invoke(uid);e.Handled=true;};stack.Children.Add(card);}
            if(matches.Length>2)stack.Children.Add(new TextBlock{Text=$"+ {matches.Length-2} more — select day",FontSize=11,Foreground=DesignTokens.Accent});
            var cell=new Border{Tag=day,Child=stack,BorderBrush=day==_selectedDate?DesignTokens.Accent:DesignTokens.Separator,BorderThickness=new Thickness(day==_selectedDate?2:0.5),Background=day.Month==_monthDate.Month?DesignTokens.Surface:DesignTokens.Background,MinHeight=80};cell.PointerPressed+=(_,_)=>{_selectedDate=day;UpdateSelected(tasks);foreach(var border in _month.Children.OfType<Border>()){bool selected=border.Tag is DateTime date&&date==day;border.BorderBrush=selected?DesignTokens.Accent:DesignTokens.Separator;border.BorderThickness=new Thickness(selected?2:0.5);}};Grid.SetRow(cell,1+index/7);Grid.SetColumn(cell,index%7);_month.Children.Add(cell);
        }
        UpdateSelected(tasks);
    }
    private void UpdateSelected(ProjectTask[] tasks){var rows=tasks.Where(t=>OccursOn(t,_selectedDate)).Select(t=>new DayTask(t.Uid,$"{(t.IsMilestone==true?"◆ ":"")}{t.DisplayId??t.Uid}  {t.Name} — {TaskColumns.Format(t.Start)} → {TaskColumns.Format(t.Finish)}")).ToArray();_selectedHeading.Text=_selectedDate.ToString("dddd, dd MMMM yyyy",CultureInfo.InvariantCulture)+$" · {rows.Length} tasks";_selectedTasks.ItemsSource=rows;}
    private sealed record DayTask(int Uid,string Caption){public override string ToString()=>Caption;}
}


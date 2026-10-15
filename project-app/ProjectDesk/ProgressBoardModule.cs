using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using OfficeIMO.Project;

namespace ProjectDesk;

/// <summary>Simple task progress lanes. This is not an imported native board/sprint field editor.</summary>
public sealed class ProgressBoardModule : UserControl {
    private readonly TextBox _search=new(){Width=300,Watermark="Find a task"};
    private readonly Grid _lanes=new(){ColumnDefinitions=new ColumnDefinitions("*,*,*")};
    private List<ProjectTask> _tasks=[];
    public int NotStartedCount{get;private set;}
    public int InProgressCount{get;private set;}
    public int CompletedCount{get;private set;}
    public int MissingProgressCount{get;private set;}
    public event Action<int>? TaskActivated;
    public event Action<int,int>? ProgressRequested;
    public ProgressBoardModule(){var root=new DockPanel();var header=new StackPanel{Margin=new Thickness(14,10),Spacing=8};header.Children.Add(new TextBlock{Text="Progress Board",FontSize=21,FontWeight=FontWeight.SemiBold});header.Children.Add(new TextBlock{Text="Track task completion across three lanes. Double-click a card to edit its details.",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary});header.Children.Add(_search);DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);root.Children.Add(_lanes);Content=root;_search.TextChanged+=(_,_)=>Rebuild();}
    public void Refresh(ProjectSession session){_tasks=session.Tasks.Where(t=>!t.IsSummary).ToList();Rebuild();}
    private void Rebuild(){_lanes.Children.Clear();string query=_search.Text?.Trim()??"";var tasks=_tasks.Where(t=>(t.Name??"").Contains(query,StringComparison.OrdinalIgnoreCase)||(t.DisplayId??t.Uid).ToString().Contains(query,StringComparison.OrdinalIgnoreCase)).ToArray();
        var notStarted=tasks.Where(t=>!t.PercentComplete.HasValue||t.PercentComplete<=0).ToArray();var inProgress=tasks.Where(t=>t.PercentComplete>0&&t.PercentComplete<100).ToArray();var complete=tasks.Where(t=>t.PercentComplete>=100).ToArray();NotStartedCount=notStarted.Length;InProgressCount=inProgress.Length;CompletedCount=complete.Length;MissingProgressCount=notStarted.Count(t=>!t.PercentComplete.HasValue);
        var groups=new[]{("Not started",notStarted),("In progress",inProgress),("Completed",complete)};
        for(int index=0;index<groups.Length;index++){var (title,rows)=groups[index];var panel=new DockPanel{Margin=new Thickness(6)};var heading=new TextBlock{Text=$"{title} ({rows.Length})",FontSize=16,FontWeight=FontWeight.SemiBold,Margin=new Thickness(12,10)};DockPanel.SetDock(heading,Dock.Top);panel.Children.Add(heading);var cards=new StackPanel{Spacing=10,Margin=new Thickness(8)};
            foreach(var task in rows)cards.Children.Add(Card(task));if(rows.Length==0)cards.Children.Add(new TextBlock{Text="No matching tasks",Foreground=DesignTokens.Secondary,Margin=new Thickness(6)});panel.Children.Add(new ScrollViewer{Content=cards});var lane=new Border{Child=panel,Background=DesignTokens.Sidebar,CornerRadius=DesignTokens.Radius,Margin=new Thickness(8,0,8,10)};Grid.SetColumn(lane,index);_lanes.Children.Add(lane);
        }
    }
    private Control Card(ProjectTask task){int uid=task.Uid;var stack=new StackPanel{Spacing=8};stack.Children.Add(new TextBlock{Text=$"{task.DisplayId??uid}  {task.Name}",FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap});stack.Children.Add(new TextBlock{Text=$"{TaskColumns.Format(task.Start)??"—"} → {TaskColumns.Format(task.Finish)??"—"}",FontSize=11,Foreground=DesignTokens.Secondary});stack.Children.Add(new TextBlock{Text=task.PercentComplete.HasValue?$"{task.PercentComplete}% complete":"Progress not stored",FontSize=12,Foreground=task.PercentComplete.HasValue?DesignTokens.Accent:DesignTokens.Secondary});
        var progressBar=new ProgressBar{Minimum=0,Maximum=100,Value=task.PercentComplete??0,Height=4,Foreground=task.PercentComplete>=100?RibbonModule.Green:DesignTokens.Accent,Background=DesignTokens.Separator};stack.Children.Add(progressBar);
        var actions=new WrapPanel{Orientation=Orientation.Horizontal};foreach(var (text,progress) in new[]{("Reset",0),("50%",50),("Complete",100)}){var button=new Button{Content=text,Tag=uid,FontSize=11,Padding=new Thickness(8,4),Margin=new Thickness(0,0,5,0),MinHeight=26};button.Click+=(_,_)=>ProgressRequested?.Invoke(uid,progress);button.DoubleTapped+=(_,e)=>e.Handled=true;actions.Children.Add(button);}stack.Children.Add(actions);
        var card=new Border{Child=stack,Background=DesignTokens.Surface,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(1),CornerRadius=DesignTokens.Radius,Padding=new Thickness(12)};card.DoubleTapped+=(_,_)=>TaskActivated?.Invoke(uid);return card;
    }
}


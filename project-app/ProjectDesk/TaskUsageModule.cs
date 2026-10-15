using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using OfficeIMO.Project;

namespace ProjectDesk;

/// <summary>Task and assignment totals as stored in the document. No daily work is invented.</summary>
public sealed class TaskUsageModule : UserControl {
    public DataGrid Grid { get; } = new() { AutoGenerateColumns=false,IsReadOnly=true,
        CanUserResizeColumns=true,CanUserReorderColumns=true,RowHeight=26,ColumnHeaderHeight=32,
        GridLinesVisibility=DataGridGridLinesVisibility.All,BorderThickness=new Thickness(0),FontSize=12 };
    public event Action<int>? TaskActivated;
    public event Action<int>? AssignmentActivated;
    public int TaskCount {get;private set;}
    public int AssignmentCount {get;private set;}
    private readonly TextBox _search=new(){Watermark="Find task or resource",Width=260};
    private List<UsageRow> _rows=[];
    public TaskUsageModule() {
        foreach(var (title,key,width) in new[]{("Task / resource","Name",300d),("ID","Id",55d),("Start","Start",110d),("Finish","Finish",110d),("Units","Units",85d),("Work","Work",95d),("Actual Work","Actual",100d),("Remaining Work","Remaining",115d),("Cost","Cost",95d),("Actual Cost","ActualCost",100d),("Remaining Cost","RemainingCost",110d)})
            Grid.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(key),Width=new DataGridLength(width)});
        var panel=new DockPanel();
        var toolbar=new StackPanel{Orientation=Avalonia.Layout.Orientation.Horizontal,Spacing=18,Margin=new Thickness(10,6)};
        toolbar.Children.Add(_search);toolbar.Children.Add(new TextBlock{Text="Stored totals • double-click a task or assignment to edit",VerticalAlignment=Avalonia.Layout.VerticalAlignment.Center,Foreground=DesignTokens.Secondary});
        DockPanel.SetDock(toolbar,Dock.Top);panel.Children.Add(toolbar);panel.Children.Add(Grid);Content=panel;
        _search.TextChanged+=(_,_)=>ApplyFilter();
        Grid.DoubleTapped+=(_,_)=>{if(Grid.SelectedItem is UsageRow row){if(row.AssignmentUid is int assignment)AssignmentActivated?.Invoke(assignment);else TaskActivated?.Invoke(row.TaskUid);}};
    }
    public void Refresh(ProjectSession session) {
        _rows=[];TaskCount=session.Tasks.Count;AssignmentCount=0;
        var assignments=session.Document?.Assignments.Where(a=>a.Task?.Uid>0&&a.Resource?.Uid>0).GroupBy(a=>a.Task!.Uid).ToDictionary(g=>g.Key,g=>g.ToArray())??[];
        foreach(var t in session.Tasks) {
            int depth=0;for(var p=t.Parent;p!=null;p=p.Parent)depth++;
            _rows.Add(new(t.Uid,null,new string(' ',depth*2)+(t.Name??"Untitled task"),t.DisplayId?.ToString()??"",t.Start,t.Finish,null,t.Work,t.ActualWork,t.RemainingWork,t.Cost,t.ActualCost,t.RemainingCost));
            if(!assignments.TryGetValue(t.Uid,out var bound))continue;
            foreach(var a in bound){AssignmentCount++;_rows.Add(new(t.Uid,a.Uid,new string(' ',depth*2+4)+(a.Resource?.Name??"Resource"),"",a.Start,a.Finish,a.Units,a.Work,a.ActualWork,a.RemainingWork,a.Cost,a.ActualCost,a.RemainingCost));}
        }
        ApplyFilter();
    }
    private void ApplyFilter(){var query=_search.Text?.Trim()??"";var selectedTasks=_rows.Where(r=>r.Name.Contains(query,StringComparison.OrdinalIgnoreCase)).Select(r=>r.TaskUid).ToHashSet();Grid.ItemsSource=query.Length==0?_rows:_rows.Where(r=>selectedTasks.Contains(r.TaskUid)).ToArray();}
    public sealed record UsageRow {
        public int TaskUid{get;}public int? AssignmentUid{get;}public string Name{get;}public string Id{get;}
        public string? Start{get;}public string? Finish{get;}public string? Units{get;}public string? Work{get;}public string? Actual{get;}public string? Remaining{get;}public string? Cost{get;}public string? ActualCost{get;}public string? RemainingCost{get;}
        public UsageRow(int taskUid,int? assignmentUid,string name,string id,DateTime? start,DateTime? finish,ProjectUnits? units,ProjectWork? work,ProjectWork? actual,ProjectWork? remaining,decimal? cost,decimal? actualCost,decimal? remainingCost){TaskUid=taskUid;AssignmentUid=assignmentUid;Name=name;Id=id;Start=TaskColumns.Format(start);Finish=TaskColumns.Format(finish);Units=TaskColumns.Format(units);Work=TaskColumns.Format(work);Actual=TaskColumns.Format(actual);Remaining=TaskColumns.Format(remaining);Cost=TaskColumns.Format(cost);ActualCost=TaskColumns.Format(actualCost);RemainingCost=TaskColumns.Format(remainingCost);}
    }
}

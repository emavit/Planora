using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OfficeIMO.Project;

namespace ProjectDesk;

public sealed class NetworkDiagramModule : UserControl {
    public NetworkCanvas Diagram {get;}=new();
    public event Action<int>? TaskActivated {add=>Diagram.TaskActivated+=value;remove=>Diagram.TaskActivated-=value;}
    public NetworkDiagramModule(){
        var root=new DockPanel();var bar=new StackPanel{Orientation=Avalonia.Layout.Orientation.Horizontal,Spacing=8,Margin=new Thickness(8,4)};
        foreach(var (label,factor) in new[]{("Zoom out",0.8),("Zoom in",1.25),("Reset zoom",0.0)}){var b=new Button{Content=label};b.Click+=(_,_)=>Diagram.SetZoom(factor==0?1:Diagram.Zoom*factor);bar.Children.Add(b);}
        bar.Children.Add(new TextBlock{Text="Dependency network • double-click a task to edit",Foreground=DesignTokens.Secondary,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Center});
        DockPanel.SetDock(bar,Dock.Top);root.Children.Add(bar);root.Children.Add(new ScrollViewer{Content=Diagram,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,VerticalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto});Content=root;
    }
    public void Refresh(ProjectSession session)=>Diagram.SetProject(session.Tasks,session.Document?.Dependencies??Enumerable.Empty<ProjectDependency>());
}

public sealed class NetworkCanvas : Control {
    private List<ProjectTask> _tasks=[];private List<ProjectDependency> _links=[];private readonly Dictionary<int,Rect> _boxes=[];
    public double Zoom{get;private set;}=1;
    public int NodeCount=>_boxes.Count;public int LinkCount=>_links.Count;
    public bool HasCycle{get;private set;}
    public event Action<int>? TaskActivated;
    public NetworkCanvas(){DoubleTapped+=(_,e)=>{var p=e.GetPosition(this);foreach(var box in _boxes)if(box.Value.Contains(new Point(p.X/Zoom,p.Y/Zoom))){TaskActivated?.Invoke(box.Key);break;}};}
    public void SetZoom(double zoom){Zoom=Math.Clamp(zoom,0.35,2.5);UpdateSize();}
    public void SetProject(IEnumerable<ProjectTask> tasks,IEnumerable<ProjectDependency> links){
        _tasks=tasks.ToList();var ids=_tasks.Select(t=>t.Uid).ToHashSet();_links=links.Where(d=>d.Predecessor!=null&&d.Successor!=null&&ids.Contains(d.Predecessor.Uid)&&ids.Contains(d.Successor.Uid)).ToList();_boxes.Clear();
        var indegree=ids.ToDictionary(id=>id,_=>0);var ranks=ids.ToDictionary(id=>id,_=>0);var outgoing=_links.GroupBy(d=>d.Predecessor!.Uid).ToDictionary(g=>g.Key,g=>g.ToArray());
        foreach(var link in _links)indegree[link.Successor!.Uid]++;
        var queue=new Queue<int>(_tasks.Where(t=>indegree[t.Uid]==0).Select(t=>t.Uid));int processed=0;
        while(queue.TryDequeue(out var uid)){processed++;if(!outgoing.TryGetValue(uid,out var next))continue;foreach(var link in next){int successor=link.Successor!.Uid;ranks[successor]=Math.Max(ranks[successor],ranks[uid]+1);if(--indegree[successor]==0)queue.Enqueue(successor);}}
        HasCycle=processed!=ids.Count;var lanes=new Dictionary<int,int>();
        foreach(var task in _tasks){int rank=ranks[task.Uid];int lane=lanes.GetValueOrDefault(rank);lanes[rank]=lane+1;_boxes[task.Uid]=new Rect(24+rank*290,40+lane*130,230,96);}
        UpdateSize();
    }
    private void UpdateSize(){Width=Math.Max(700,(_boxes.Count==0?700:_boxes.Values.Max(b=>b.Right)+40)*Zoom);Height=Math.Max(400,(_boxes.Count==0?400:_boxes.Values.Max(b=>b.Bottom)+40)*Zoom);InvalidateVisual();}
    public override void Render(DrawingContext context){base.Render(context);context.DrawRectangle(DesignTokens.Surface,null,new Rect(Bounds.Size));using(context.PushTransform(Matrix.CreateScale(Zoom,Zoom))){
        if(HasCycle)ChartDrawing.Text(context,"Circular links detected; network ordering is incomplete.",20,10,650,ChartDrawing.Critical);
        foreach(var link in _links){var a=_boxes[link.Predecessor!.Uid];var b=_boxes[link.Successor!.Uid];bool fromStart=link.Type is ProjectDependencyType.StartToStart or ProjectDependencyType.StartToFinish;bool toFinish=link.Type is ProjectDependencyType.FinishToFinish or ProjectDependencyType.StartToFinish;var start=new Point(fromStart?a.Left:a.Right,a.Center.Y);var end=new Point(toFinish?b.Right:b.Left,b.Center.Y);double turn=fromStart?start.X-12:start.X+18;ChartDrawing.Arrow(context,[start,new Point(turn,start.Y),new Point(turn,end.Y),end],toFinish?-1:1,DesignTokens.Secondary);string kind=link.Type switch{ProjectDependencyType.StartToStart=>"SS",ProjectDependencyType.FinishToFinish=>"FF",ProjectDependencyType.StartToFinish=>"SF",_=>"FS"};ChartDrawing.Text(context,kind+(link.Lag is {Value:not 0} lag?" "+TaskColumns.Format(lag):""),start.X+4,start.Y-20,150);}
        foreach(var task in _tasks){var b=_boxes[task.Uid];var line=task.IsCritical==true?ChartDrawing.Critical:DesignTokens.Accent;context.DrawRectangle(DesignTokens.Surface,new Pen(line,task.IsCritical==true?2:1),b);context.DrawRectangle(task.IsSummary?DesignTokens.Sidebar:DesignTokens.Selected,null,new Rect(b.X,b.Y,b.Width,27));ChartDrawing.Text(context,$"{task.DisplayId??task.Uid}  {task.Name}",b.X+8,b.Y+5,214,bold:true);ChartDrawing.Text(context,$"Start: {TaskColumns.Format(task.Start)}",b.X+8,b.Y+34,214);ChartDrawing.Text(context,$"Finish: {TaskColumns.Format(task.Finish)}",b.X+8,b.Y+53,214);ChartDrawing.Text(context,$"{TaskColumns.Format(task.Duration)}   {task.PercentComplete?.ToString()??"—"}% complete",b.X+8,b.Y+73,214);}
    }}
}


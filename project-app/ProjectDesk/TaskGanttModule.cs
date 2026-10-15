using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.VisualTree;
using OfficeIMO.Project;

namespace ProjectDesk;

public sealed class TaskGanttModule : UserControl {
    public TaskSheetModule Sheet {get;}
    public GanttCanvas Chart {get;}
    private readonly Grid _split;
    private readonly GridSplitter _divider=new(){Width=6,Background=DesignTokens.Separator};
    private readonly ScrollViewer _chartScroll;
    private bool _showChart=true;
    private double _tableFraction;
    public event Action<double>? SplitChanged;
    public double TableFraction=>_tableFraction;
    public double HorizontalOffset=>_chartScroll.Offset.X;
    public TaskGanttModule(TaskSheetModule sheet,DateAxis axis,double tableFraction=.38) {
        Sheet=sheet;Chart=new(sheet.Grid,axis);_tableFraction=Math.Clamp(tableFraction,.2,.8);
        _split=new Grid{ColumnDefinitions=new ColumnDefinitions("0.38*,6,0.62*")};
        _split.ColumnDefinitions[0].Width=new GridLength(_tableFraction,GridUnitType.Star);_split.ColumnDefinitions[2].Width=new GridLength(1-_tableFraction,GridUnitType.Star);
        _divider.PointerReleased+=(_,_)=>{double total=_split.ColumnDefinitions[0].ActualWidth+_split.ColumnDefinitions[2].ActualWidth;if(_showChart&&total>0){_tableFraction=Math.Clamp(_split.ColumnDefinitions[0].ActualWidth/total,.2,.8);SplitChanged?.Invoke(_tableFraction);}};
        _split.Children.Add(sheet);Grid.SetColumn(_divider,1);_split.Children.Add(_divider);
        _chartScroll=new ScrollViewer{Content=Chart,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};
        _chartScroll.SizeChanged+=(_,_)=>Chart.SetViewport(_chartScroll.Bounds.Width);
        Grid.SetColumn(_chartScroll,2);_split.Children.Add(_chartScroll);Content=_split;
        sheet.Grid.LayoutUpdated+=(_,_)=>Chart.InvalidateVisual();sheet.Grid.SelectionChanged+=(_,_)=>Chart.InvalidateVisual();
        _chartScroll.PointerWheelChanged+=(_,e)=>{
            if(e.KeyModifiers.HasFlag(OperatingSystem.IsMacOS()?Avalonia.Input.KeyModifiers.Meta:Avalonia.Input.KeyModifiers.Control)){var date=axis.DateAt(e.GetPosition(Chart).X,Chart.Width-24);double x=e.GetPosition(_chartScroll).X;if(e.Delta.Y>0)axis.ZoomIn();else axis.ZoomOut();Avalonia.Threading.Dispatcher.UIThread.Post(()=>_chartScroll.Offset=new Vector(Math.Max(0,axis.X(date,Chart.Width-24)-x),0));e.Handled=true;return;}
            if(e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift)){_chartScroll.Offset=new Vector(Math.Max(0,_chartScroll.Offset.X-e.Delta.Y*96),0);e.Handled=true;return;}
            var scrollbar=sheet.Grid.GetVisualDescendants().OfType<ScrollBar>().FirstOrDefault(s=>s.Orientation==Avalonia.Layout.Orientation.Vertical&&s.IsVisible);
            if(scrollbar!=null){scrollbar.Value=Math.Clamp(scrollbar.Value-e.Delta.Y*72,scrollbar.Minimum,scrollbar.Maximum);e.Handled=true;}
        };
    }
    public void ShowChart(bool visible) {
        if(_showChart==visible)return;_showChart=visible;_chartScroll.IsVisible=visible;_divider.IsVisible=visible;
        _split.ColumnDefinitions[0].Width=visible?new GridLength(_tableFraction,GridUnitType.Star):new GridLength(1,GridUnitType.Star);
        _split.ColumnDefinitions[1].Width=new GridLength(visible?6:0);_split.ColumnDefinitions[2].Width=visible?new GridLength(1-_tableFraction,GridUnitType.Star):new GridLength(0);
    }
    public void ScrollToTask(int uid){
        var node=Chart.VisibleNodes().FirstOrDefault(n=>n.Uid==uid)??throw new InvalidOperationException("This task needs scheduled dates before it can be shown on the Gantt chart.");
        _chartScroll.Offset=new Vector(Math.Max(0,node.StartX-40),0);
    }
    public bool ScrollToDate(DateTime date){
        if(date<Chart.Axis.Start||date>=Chart.Axis.Finish)return false;
        _chartScroll.Offset=new Vector(Math.Max(0,Chart.Axis.X(date,Chart.Width-24)-_chartScroll.Bounds.Width/2),0);return true;
    }
    public void Refresh(ProjectSession session){Sheet.Refresh(session);Chart.SetProject(session.Document,session.Tasks);}
}

public sealed record GanttNode(int Uid,double StartX,double FinishX,double CenterY,double Height);
public sealed class GanttCanvas : Control {
    private readonly DataGrid _sheet;
    private readonly DateAxis _axis;
    public DateAxis Axis=>_axis;
    private ProjectDocument? _document;
    private Dictionary<int,ProjectTask> _tasks=[];
    private double _viewport=700;
    private int? _hoveredUid;
    public FormattingStore? Formatting {get;set;}
    public bool ShowDependencies {get;set;}=true;
    public bool ShowBaseline {get;set;}=true;
    public bool ShowCritical {get;set;}=true;
    public bool ColorByProgress {get;set;}
    public bool ShowProgressLabels {get;set;}
    public int DrawnProgressLabelCount {get;private set;}
    public static readonly IBrush NotStartedBrush=new SolidColorBrush(Color.Parse("#718399"));
    public static readonly IBrush InProgressBrush=new SolidColorBrush(Color.Parse("#397FD1"));
    public static readonly IBrush CompletedBrush=new SolidColorBrush(Color.Parse("#31865F"));
    public static IBrush ProgressBrush(int? percent)=>percent>=100?CompletedBrush:percent>0?InProgressBrush:NotStartedBrush;
    private static IBrush Darker(IBrush brush)=>brush is SolidColorBrush solid?new SolidColorBrush(Color.FromArgb(solid.Color.A,(byte)(solid.Color.R*.75),(byte)(solid.Color.G*.75),(byte)(solid.Color.B*.75))):brush;
    public int BaselineNumber {get;set;}
    public int DrawnDependencyCount {get;private set;}
    public int RenderCount {get;private set;}
    public GanttCanvas(DataGrid sheet,DateAxis axis){
        _sheet=sheet;_axis=axis;axis.Changed+=UpdateDimensions;ClipToBounds=true;
        PointerMoved+=(_,e)=>{
            var node=HitRow(e.GetPosition(this));if(_hoveredUid==node?.Uid)return;_hoveredUid=node?.Uid;
            ToolTip.SetTip(this,node!=null&&_tasks.TryGetValue(node.Uid,out var task)?$"{task.Name}\n{task.Start:dd MMM yyyy} – {task.Finish:dd MMM yyyy}\n{Math.Clamp(task.PercentComplete??0,0,100)}% complete":null);InvalidateVisual();
        };
        PointerExited+=(_,_)=>{_hoveredUid=null;ToolTip.SetTip(this,null);InvalidateVisual();};
        PointerPressed+=(_,e)=>{
            if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed||HitRow(e.GetPosition(this)) is not {} node)return;
            var row=_sheet.ItemsSource?.OfType<TaskTableRow>().FirstOrDefault(r=>r.Uid==node.Uid);if(row!=null){_sheet.SelectedItem=row;e.Handled=true;}
        };
    }
    public GanttNode? HitRow(Point point)=>point.Y<36?null:VisibleNodes().FirstOrDefault(n=>point.Y>=n.CenterY-n.Height/2&&point.Y<n.CenterY+n.Height/2);
    public void SetViewport(double value){_viewport=Math.Max(300,value);UpdateDimensions();}
    public void SetProject(ProjectDocument? document,IEnumerable<ProjectTask> tasks){_document=document;_tasks=tasks.ToDictionary(t=>t.Uid);UpdateDimensions();}
    private void UpdateDimensions(){Width=_axis.PlotWidth(_viewport)+24;InvalidateVisual();}
    public IReadOnlyList<GanttNode> VisibleNodes() {
        var result=new List<GanttNode>();double plot=Width-24;var current=new HashSet<TaskTableRow>(_sheet.ItemsSource?.OfType<TaskTableRow>()??[]);var seen=new HashSet<int>();
        foreach(var row in _sheet.GetVisualDescendants().OfType<DataGridRow>()) {
            if(!row.IsVisible||row.DataContext is not TaskTableRow display||!current.Contains(display)||!_tasks.TryGetValue(display.Uid,out var task)||task.Start is not DateTime a||task.Finish is not DateTime b)continue;
            var point=row.TranslatePoint(new Point(0,0),this);if(point==null)continue;
            double y=point.Value.Y,h=row.Bounds.Height;
            if(h<=0||y+h<=36||y>=Bounds.Height||!seen.Add(display.Uid))continue;
            result.Add(new(display.Uid,_axis.X(a,plot),_axis.X(b,plot),y+h/2,h));
        }
        return result;
    }
    internal static Dictionary<int,GanttNode> NodeMap(IEnumerable<GanttNode> nodes)=>nodes.GroupBy(n=>n.Uid).ToDictionary(g=>g.Key,g=>g.First());
    public override void Render(DrawingContext ctx) {
        base.Render(ctx);RenderCount++;double plot=Width-24;IBrush Palette(string key,IBrush fallback)=>FormatVisuals.Brush(Formatting?.Document.ChartColors.GetValueOrDefault(key),fallback);ChartDrawing.DateHeader(ctx,_axis,plot,Bounds.Height,true,true,Palette("Weekend",DesignTokens.NonWorking));
        var nodes=VisibleNodes();var map=NodeMap(nodes);DrawnDependencyCount=0;DrawnProgressLabelCount=0;
        using(ctx.PushClip(new Rect(0,36,Width,Math.Max(0,Bounds.Height-36)))) {
            foreach(var node in nodes) {
                var t=_tasks[node.Uid];double y=node.CenterY;
                if(_hoveredUid==t.Uid)ctx.DrawRectangle(DesignTokens.NonWorking,null,new Rect(0,y-node.Height/2,Width,node.Height));
                if(_sheet.SelectedItems.OfType<TaskTableRow>().Any(r=>r.Uid==t.Uid))ctx.DrawRectangle(DesignTokens.Selected,null,new Rect(0,y-node.Height/2,Width,node.Height));
                ctx.DrawLine(new Pen(DesignTokens.Separator,0.5),new Point(0,y+node.Height/2),new Point(Width,y+node.Height/2));
            }
            if(ShowDependencies&&_document!=null)foreach(var dependency in _document.Dependencies) {
                if(dependency.Predecessor==null||dependency.Successor==null||!map.TryGetValue(dependency.Predecessor.Uid,out var from)||!map.TryGetValue(dependency.Successor.Uid,out var to))continue;
                bool sourceStart=dependency.Type is ProjectDependencyType.StartToStart or ProjectDependencyType.StartToFinish;
                bool targetFinish=dependency.Type is ProjectDependencyType.FinishToFinish or ProjectDependencyType.StartToFinish;
                double x0=sourceStart?from.StartX:from.FinishX,x1=targetFinish?to.FinishX:to.StartX;
                int fromDirection=sourceStart?-1:1,toDirection=targetFinish?-1:1;
                double routeY=from.CenterY+(to.CenterY>=from.CenterY?1:-1)*(from.Height/2-3);
                ChartDrawing.Arrow(ctx,[new(x0,from.CenterY),new(x0+fromDirection*12,from.CenterY),new(x0+fromDirection*12,routeY),new(x1-toDirection*10,routeY),new(x1-toDirection*10,to.CenterY),new(x1,to.CenterY)],toDirection,Palette("Links",ChartDrawing.Progress));
                DrawnDependencyCount++;
            }
            foreach(var node in nodes) {
                var t=_tasks[node.Uid];double x=node.StartX,right=node.FinishX,y=node.CenterY;
                var brush=FormatVisuals.Brush(Formatting?.Document.Bars.GetValueOrDefault(t.Uid),ColorByProgress?ProgressBrush(t.PercentComplete):ShowCritical&&t.IsCritical==true?Palette("Critical",ChartDrawing.Critical):Palette("Task",ChartDrawing.Bars));
                if(t.IsMilestone==true||t.Duration?.Value==0||t.Start==t.Finish) {
                    ChartDrawing.Diamond(ctx,new Point(x,y),5,FormatVisuals.Brush(Formatting?.Document.Bars.GetValueOrDefault(t.Uid),Palette("Milestone",brush)));ChartDrawing.Text(ctx,$"{t.Finish:dd/MM}",x+9,y-8,90,bold:true);
                } else if(t.IsSummary) {
                    ctx.DrawRectangle(Palette("Summary",DesignTokens.Text),null,new Rect(x,y-4,Math.Max(4,right-x),5));
                    ChartDrawing.Diamond(ctx,new Point(x,y),3,Palette("Summary",DesignTokens.Text));ChartDrawing.Diamond(ctx,new Point(right,y),3,Palette("Summary",DesignTokens.Text));
                } else {
                    ctx.DrawRectangle(brush,ShowCritical&&t.IsCritical==true?new Pen(Palette("Critical",ChartDrawing.Critical),1.5):null,new Rect(x,y-6,Math.Max(4,right-x),12),2,2);
                    if(t.PercentComplete>0)ctx.DrawRectangle(ColorByProgress?Darker(brush):Palette("Progress",ChartDrawing.Progress),null,new Rect(x,y-6,Math.Max(4,right-x)*Math.Clamp(t.PercentComplete.Value/100d,0,1),12),2,2);
                }
                if(ShowProgressLabels&&!t.IsSummary&&t.IsMilestone!=true&&t.Duration?.Value!=0&&t.Start!=t.Finish){
                    ChartDrawing.Text(ctx,$"{Math.Clamp(t.PercentComplete??0,0,100):0}%",right+8,y-8,48,DesignTokens.Text,11,true);DrawnProgressLabelCount++;
                }
                if(ShowBaseline&&t.Baselines.FirstOrDefault(b=>(b.Number??0)==BaselineNumber)is ProjectBaseline baseline&&baseline.Start is DateTime ba&&baseline.Finish is DateTime bb&&bb>=ba)
                    ctx.DrawRectangle(Palette("Baseline",DesignTokens.Secondary),null,new Rect(_axis.X(ba,plot),y+9,Math.Max(3,_axis.X(bb,plot)-_axis.X(ba,plot)),3));
            }
            if(DateTime.Today>=_axis.Start&&DateTime.Today<=_axis.Finish) {
                double x=_axis.X(DateTime.Today,plot);ctx.DrawLine(new Pen(Palette("Today",new SolidColorBrush(Color.Parse("#298A45"))),1),new Point(x,36),new Point(x,Bounds.Height));
            }
        }
    }
}



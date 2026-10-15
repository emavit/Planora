using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OfficeIMO.Project;

namespace ProjectDesk;

/// <summary>Interval allocation view. Stored assignment units are not daily timephased work.</summary>
public sealed class ResourceGraphModule : UserControl {
    public AllocationCanvas Chart {get;}=new();
    private readonly ComboBox _resource=new(){Width=260};
    private readonly TextBlock _totals=new(){Foreground=DesignTokens.Secondary,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Center};
    private ProjectSession? _session;
    public ResourceGraphModule(){var root=new DockPanel();var bar=new StackPanel{Orientation=Avalonia.Layout.Orientation.Horizontal,Margin=new Thickness(10,6),Spacing=18};bar.Children.Add(_resource);bar.Children.Add(_totals);DockPanel.SetDock(bar,Dock.Top);root.Children.Add(bar);
        var note=new TextBlock{Text="Stored assignment units by start/finish. Calendar breaks, contours and timephased work are not applied; this graph does not certify overallocation.",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary,Margin=new Thickness(12,6)};DockPanel.SetDock(note,Dock.Bottom);root.Children.Add(note);
        root.Children.Add(new ScrollViewer{Content=Chart,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto});Content=root;_resource.SelectionChanged+=(_,_)=>SelectResource();}
    public void Refresh(ProjectSession session){int? uid=(_resource.SelectedItem as ResourceChoice)?.Uid;_session=session;var choices=session.Resources.Where(r=>r.Type==null||r.Type==ProjectResourceType.Work).Select(r=>new ResourceChoice(r.Uid,r.Name??$"Resource {r.Uid}")).ToArray();_resource.ItemsSource=choices;_resource.SelectedItem=choices.FirstOrDefault(r=>r.Uid==uid)??choices.FirstOrDefault();SelectResource();}
    private void SelectResource(){var resource=_session?.Resources.FirstOrDefault(r=>r.Uid==(_resource.SelectedItem as ResourceChoice)?.Uid);var assignments=_session?.Document?.Assignments.Where(a=>a.Resource?.Uid==resource?.Uid&&a.Task?.Uid>0).ToArray()??[];Chart.SetResource(resource,assignments);_totals.Text=resource==null?"No work resource selected":$"Stored resource totals: Work {TaskColumns.Format(resource.Work)??"—"} · Actual {TaskColumns.Format(resource.ActualWork)??"—"} · Cost {TaskColumns.Format(resource.Cost)??"—"}";}
    private sealed record ResourceChoice(int Uid,string Name){public override string ToString()=>Name;}
}

public sealed record AllocationInterval(DateTime Start,DateTime Finish,decimal Units);
public sealed class AllocationCanvas : Control {
    public IReadOnlyList<AllocationInterval> Intervals{get;private set;}=[];
    public int OmittedAssignmentCount{get;private set;}
    private ProjectResource? _resource;private DateTime _start,_finish;private decimal? _capacity;
    public AllocationCanvas(){Width=1100;Height=540;}
    public void SetResource(ProjectResource? resource,IEnumerable<ProjectAssignment> source){_resource=resource;_capacity=resource?.MaxUnits?.Value;var assignments=source.ToArray();var valid=assignments.Where(a=>a.Start.HasValue&&a.Finish>a.Start&&a.Units.HasValue).ToArray();OmittedAssignmentCount=assignments.Length-valid.Length;
        var changes=new SortedDictionary<DateTime,decimal>();foreach(var a in valid){changes[a.Start!.Value]=changes.GetValueOrDefault(a.Start.Value)+a.Units!.Value.Value;changes[a.Finish!.Value]=changes.GetValueOrDefault(a.Finish.Value)-a.Units.Value.Value;}
        var intervals=new List<AllocationInterval>();decimal units=0;DateTime? previous=null;foreach(var change in changes){if(previous is DateTime before&&change.Key>before)intervals.Add(new(before,change.Key,units));units+=change.Value;previous=change.Key;}Intervals=intervals;
        if(changes.Count>0){_start=changes.Keys.First();_finish=changes.Keys.Last();}else{_start=DateTime.Today;_finish=_start.AddDays(1);}InvalidateVisual();}
    public override void Render(DrawingContext context){base.Render(context);context.DrawRectangle(DesignTokens.Surface,null,new Rect(Bounds.Size));if(_resource==null){ChartDrawing.Text(context,"Select a work resource to see its assignments.",24,30,750);return;}
        ChartDrawing.Text(context,_resource.Name??"Resource",70,12,600,size:14,bold:true);
        if(Intervals.Count==0){ChartDrawing.Text(context,"No assignments with stored dates and units are available.",70,70,750);if(OmittedAssignmentCount>0)ChartDrawing.Text(context,$"{OmittedAssignmentCount} assignments have missing dates/units or no duration.",70,95,750);return;}
        double left=70,top=70,width=Math.Max(300,Bounds.Width-100),height=340;decimal max=Math.Max(1,Math.Max(_capacity??0,Intervals.Max(i=>i.Units)))*1.15m;double total=Math.Max(1,(_finish-_start).TotalMinutes);
        double X(DateTime date)=>left+(date-_start).TotalMinutes/total*width;double Y(decimal unit)=>top+height-(double)(unit/max)*height;
        for(int i=0;i<=4;i++){decimal value=max*i/4;double y=Y(value);context.DrawLine(new Pen(DesignTokens.Separator),new Point(left,y),new Point(left+width,y));ChartDrawing.Text(context,$"{value*100:0}%",4,y-8,60);}
        foreach(var interval in Intervals){if(interval.Units<=0)continue;double x=X(interval.Start),end=X(interval.Finish),y=Y(interval.Units);context.DrawRectangle(_capacity.HasValue&&interval.Units>_capacity.Value?ChartDrawing.Critical:ChartDrawing.Bars,null,new Rect(x,y,Math.Max(0.5,end-x),top+height-y));}
        if(_capacity is decimal capacity){double y=Y(capacity);context.DrawLine(new Pen(DesignTokens.Accent,1.5),new Point(left,y),new Point(left+width,y));ChartDrawing.Text(context,$"Stored maximum units: {capacity*100:0.##}%",left+8,y-20,350,DesignTokens.Accent);}
        for(int i=0;i<=5;i++){var date=_start.AddMinutes(total*i/5);double x=X(date);context.DrawLine(new Pen(DesignTokens.Separator),new Point(x,top+height),new Point(x,top+height+6));ChartDrawing.Text(context,date.ToString("dd MMM yy",System.Globalization.CultureInfo.InvariantCulture),Math.Min(x,left+width-90),top+height+12,100);}
        ChartDrawing.Text(context,$"Peak stored interval units: {Intervals.Max(i=>i.Units)*100:0.##}% · {OmittedAssignmentCount} assignments omitted for missing dates/units or zero duration",left,top+height+45,width,DesignTokens.Secondary);
    }
}

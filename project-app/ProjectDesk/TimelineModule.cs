using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using OfficeIMO.Project;
namespace ProjectDesk;
public sealed record TimelineLane(string Id,string Name,List<int> TaskUids,DateAxisView View,TimelineStyle? Style=null,Dictionary<int,TimelineItemStyle>? ItemStyles=null);
public sealed class TimelineLayout {public List<TimelineLane> Lanes {get;set;}=[];public string? ActiveId {get;set;}}
public sealed class TimelineModule : UserControl {
 private readonly ViewPreferences _preferences;private readonly string _settingsPath;
 private readonly StackPanel _cards=new(){Spacing=8};private readonly ComboBox _selector=new(){Width=170,MinHeight=28};private readonly TextBlock _hint=new(){Text="Drag blocks · right-click to format",Foreground=DesignTokens.Secondary,FontSize=11,VerticalAlignment=VerticalAlignment.Center};
 private readonly Border _ghost=new(){IsVisible=false,IsHitTestVisible=false,Background=DesignTokens.Selected,BorderBrush=DesignTokens.Accent,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8),Padding=new Thickness(10,6),Child=new TextBlock{MaxWidth=240,TextTrimming=TextTrimming.CharacterEllipsis}};
 private readonly Canvas _overlay=new(){IsHitTestVisible=false};private readonly ScrollViewer _outer;
 private readonly List<LaneView> _views=[];private List<ProjectTask> _tasks=[];private string _projectKey="";private string? _active;private bool _updating;
 private DragState? _drag;private Point _dragPoint;private readonly DispatcherTimer _dragScroll=new(){Interval=TimeSpan.FromMilliseconds(50)};
 private sealed record DragState(string LaneId,int Uid,Point Origin,IPointer Pointer,bool Moving=false);
 private sealed record LaneChoice(string Id,string Name){public override string ToString()=>Name;}
 private sealed class LaneView {public string Id="",Name="";public List<int> Uids=[];public TimelineStyle Style=new();public Dictionary<int,TimelineItemStyle> ItemStyles=[];public DateAxis Axis=new();public TimelineCanvas Canvas=null!;public Border Card=null!;public ScrollViewer Scroll=null!;public TextBlock Caption=null!;}
 public event Action<double>? HeightRequested;public event Action<double>? FitHeightRequested;public event Action<string>? Error;
 public DateAxis Axis=>Active.Axis;
 public void FitPanel()=>FitHeightRequested?.Invoke(Math.Clamp(50+_views.Count*150,200,600));
 public IReadOnlyList<TimelineLane> Lanes=>_views.Select(v=>new TimelineLane(v.Id,v.Name,v.Uids.ToList(),v.Axis.Capture(),v.Style,new(v.ItemStyles))).ToArray();
 public string ActiveId=>Active.Id;
 public IReadOnlyList<int> SelectedUids=>_views.SelectMany(v=>v.Uids).Distinct().ToArray();
 public IReadOnlyList<TimelineCanvas> Canvases=>_views.Select(v=>v.Canvas).ToArray();
 public bool IsDragging=>_drag?.Moving==true;
 private LaneView Active=>_views.FirstOrDefault(v=>v.Id==_active)??_views.First();
 public TimelineModule(DateAxis unused,ViewPreferences preferences,string settingsPath){_preferences=preferences;_settingsPath=settingsPath;ClipToBounds=true;
  var heading=new WrapPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(10,5)};
  Button Command(string label,Func<Task> action){var button=new Button{Content=label,MinHeight=28,Padding=new Thickness(9,4),Margin=new Thickness(3,0)};button.Classes.Add("toolbar");button.Click+=async(_,_)=>{try{await action();}catch(Exception e){Error?.Invoke(e.Message);}};return button;}
  heading.Children.Add(new TextBlock{Text="Timelines",FontWeight=FontWeight.SemiBold,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(2,0,12,0)});
  heading.Children.Add(_selector);heading.Children.Add(Command("New timeline",()=>{AddTimeline();return Task.CompletedTask;}));
  var fit=Command("Fit panel",()=>{FitPanel();return Task.CompletedTask;});fit.Name="TimelineFitPanel";ToolTip.SetTip(fit,"Resize the timeline panel to its lanes and return unused space to Gantt");heading.Children.Add(fit);
  var more=Command("Options",()=>Task.CompletedTask);var menu=new ContextMenu();
  var rename=new MenuItem{Header="Rename active timeline…"};rename.Click+=async(_,_)=>{try{if(TopLevel.GetTopLevel(this)is Window owner){var lane=Active;var name=await new TimelineNameWindow(lane.Name).ShowDialog<string?>(owner);if(name!=null)RenameTimeline(lane.Id,name);}}catch(Exception e){Error?.Invoke(e.Message);}};
  var remove=new MenuItem{Header="Remove active timeline"};remove.Click+=(_,_)=>{try{RemoveTimeline(Active.Id);}catch(Exception e){Error?.Invoke(e.Message);}};
  menu.Items.Add(rename);menu.Items.Add(remove);more.ContextMenu=menu;more.Click+=(_,_)=>{remove.IsEnabled=_views.Count>1;menu.Open(more);};heading.Children.Add(more);
  _hint.Margin=new Thickness(12,0,0,0);heading.Children.Add(_hint);
  _selector.SelectionChanged+=(_,_)=>{if(!_updating&&_selector.SelectedItem is LaneChoice lane)SelectLane(lane.Id);};
  _outer=new ScrollViewer{Content=_cards,AllowAutoHide=false,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Margin=new Thickness(8,0,8,8)};var root=new DockPanel();DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);root.Children.Add(_outer);_overlay.Children.Add(_ghost);var layers=new Grid();layers.Children.Add(root);layers.Children.Add(_overlay);Content=layers;
  PointerMoved+=(_,e)=>MovePointer(e);PointerReleased+=(_,e)=>ReleasePointer(e);PointerCaptureLost+=(_,_)=>CancelDrag();KeyDown+=(_,e)=>{if(e.Key==Key.Escape){CancelDrag();e.Handled=true;}};
  _dragScroll.Tick+=(_,_)=>{if(_drag?.Moving!=true)return;double delta=_dragPoint.Y>Bounds.Height-24?12:_dragPoint.Y<50?-12:0;if(delta!=0)_outer.Offset=new Vector(0,Math.Clamp(_outer.Offset.Y+delta,0,Math.Max(0,_outer.Extent.Height-_outer.Viewport.Height)));};DetachedFromVisualTree+=(_,_)=>CancelDrag();AddLane("Timeline 1",[],new());UpdateSelector();
 }
 public void Refresh(ProjectSession session){_tasks=session.Tasks;string key=session.SourcePath??session.Document?.Guid?.ToString()??"empty";
  if(key!=_projectKey){CancelDrag();_updating=true;try{_projectKey=key;_views.Clear();_cards.Children.Clear();if(_preferences.TimelineLayouts.TryGetValue(key,out var layout)&&layout.Lanes.Count>0){foreach(var lane in layout.Lanes.GroupBy(l=>l.Id).Select(g=>g.First()))AddLane(string.IsNullOrWhiteSpace(lane.Name)?"Timeline":lane.Name,lane.TaskUids,lane.View,lane.Id,lane.Style,lane.ItemStyles);_active=layout.ActiveId;}
   else{var defaults=_tasks.Where(t=>t.IsSummary||t.IsMilestone==true||t.Duration?.Value==0).Take(12).Select(t=>t.Uid).ToList();if(defaults.Count==0)defaults=_tasks.Take(3).Select(t=>t.Uid).ToList();AddLane("Timeline 1",_preferences.TimelineTasks.GetValueOrDefault(key)??defaults,new());}_active=Active.Id;UpdateSelector();}finally{_updating=false;}}
  _updating=true;try{var available=_tasks.Select(t=>t.Uid).ToHashSet();foreach(var view in _views){view.Uids=view.Uids.Where(available.Contains).Distinct().ToList();view.Axis.SetProject(_tasks);view.Canvas.SetTasks(_tasks.Where(t=>view.Uids.Contains(t.Uid)).ToList());}}finally{_updating=false;}UpdateSelector();
 }
 private void AddLane(string name,IEnumerable<int> uids,DateAxisView state,string? id=null,TimelineStyle? style=null,Dictionary<int,TimelineItemStyle>? itemStyles=null){var view=new LaneView{Id=id??Guid.NewGuid().ToString("N"),Name=name,Uids=uids.Distinct().ToList(),Style=SafeStyle(style),ItemStyles=SafeItems(itemStyles)};view.Axis.SetProject(_tasks);try{view.Axis.Restore(state);}catch(ArgumentException){view.Axis.EntireProject();}view.Canvas=new(view.Axis){Focusable=true};view.Canvas.SetAppearance(view.Style,view.ItemStyles);
  var heading=new DockPanel{Margin=new Thickness(10,6),Background=DesignTokens.Surface};var title=new Button{Content=name,Background=Brushes.Transparent,BorderThickness=new Thickness(0),FontWeight=FontWeight.SemiBold,Padding=new Thickness(3),MinHeight=28,MaxWidth=180};title.Click+=(_,_)=>SelectLane(view.Id);DockPanel.SetDock(title,Dock.Left);heading.Children.Add(title);
  var commands=new StackPanel{Orientation=Orientation.Horizontal,Spacing=4};Button Action(string label,Func<Task> action){var button=new Button{Content=label,MinHeight=28,Padding=new Thickness(8,3)};button.Classes.Add("toolbar");ToolTip.SetTip(button,label=="+"?"Zoom in":label=="−"?"Zoom out":label=="Fit"?"Fit timeline dates":label);button.Click+=async(_,_)=>{SelectLane(view.Id);try{await action();}catch(Exception e){Error?.Invoke(e.Message);}};return button;}
  commands.Children.Add(Action("Tasks…",async()=>{if(TopLevel.GetTopLevel(this)is Window owner){var selected=await new TimelineTaskPicker(_tasks,view.Uids).ShowDialog<List<int>?>(owner);if(selected!=null)SetSelection(selected,true);}}));commands.Children.Add(Action("Dates…",()=>EditDates(view.Id)));commands.Children.Add(Action("Format…",()=>EditAppearance(view.Id)));commands.Children.Add(Action("−",()=>{view.Axis.ZoomOut();return Task.CompletedTask;}));commands.Children.Add(Action("Fit",()=>{view.Axis.Fit();return Task.CompletedTask;}));commands.Children.Add(Action("+",()=>{view.Axis.ZoomIn();return Task.CompletedTask;}));DockPanel.SetDock(commands,Dock.Right);heading.Children.Add(commands);view.Caption=new(){FontSize=11,Foreground=DesignTokens.Secondary,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,Margin=new Thickness(12,0)};heading.Children.Add(view.Caption);
  view.Scroll=new(){Content=view.Canvas,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};view.Scroll.SizeChanged+=(_,_)=>view.Canvas.SetViewport(view.Scroll.Bounds.Width);
  view.Scroll.PointerWheelChanged+=(_,e)=>{if(e.KeyModifiers.HasFlag(OperatingSystem.IsMacOS()?KeyModifiers.Meta:KeyModifiers.Control)){var date=view.Axis.DateAt(e.GetPosition(view.Canvas).X,view.Canvas.Width-24);double x=e.GetPosition(view.Scroll).X;if(e.Delta.Y>0)view.Axis.ZoomIn();else view.Axis.ZoomOut();Dispatcher.UIThread.Post(()=>view.Scroll.Offset=new Vector(Math.Max(0,view.Axis.X(date,view.Canvas.Width-24)-x),0));e.Handled=true;}else if(e.KeyModifiers.HasFlag(KeyModifiers.Shift)){view.Scroll.Offset=new Vector(Math.Max(0,view.Scroll.Offset.X-e.Delta.Y*96),0);e.Handled=true;}};
  var body=new DockPanel();DockPanel.SetDock(heading,Dock.Top);body.Children.Add(heading);body.Children.Add(view.Scroll);view.Card=new(){Background=DesignTokens.Surface,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Child=body};_views.Add(view);_cards.Children.Add(view.Card);
  void Caption(){view.Caption.Text=FormattableString.Invariant($"{view.Axis.Start:dd MMM yy} – {view.Axis.LastDate:dd MMM yy} · {view.Axis.Zoom*100:0}%");ToolTip.SetTip(view.Caption,view.Caption.Text);if(!_updating&&_projectKey.Length>0)Persist();}view.Axis.Changed+=Caption;Caption();
  view.Canvas.PointerPressed+=(_,e)=>{var block=view.Canvas.HitBlock(e.GetPosition(view.Canvas));if(block==null)return;SelectLane(view.Id);view.Canvas.SelectedUid=block.Uid;view.Canvas.Focus();view.Canvas.InvalidateVisual();if(e.GetCurrentPoint(view.Canvas).Properties.IsLeftButtonPressed){_drag=new(view.Id,block.Uid,e.GetPosition(this),e.Pointer);e.Pointer.Capture(this);e.Handled=true;}else if(e.GetCurrentPoint(view.Canvas).Properties.IsRightButtonPressed){var menu=new ContextMenu();var format=new MenuItem{Header="Format block…"};format.Click+=async(_,_)=>{try{await EditBlockAppearance(view.Id,block.Uid);}catch(Exception error){Error?.Invoke(error.Message);}};menu.Items.Add(format);foreach(var target in _views.Where(v=>v.Id!=view.Id)){var entry=new MenuItem{Header="Move to "+target.Name};entry.Click+=(_,_)=>MoveTask(view.Id,target.Id,block.Uid);menu.Items.Add(entry);}var remove=new MenuItem{Header="Remove from timeline"};remove.Click+=(_,_)=>{view.Uids.Remove(block.Uid);RefreshLanes();Persist();};menu.Items.Add(remove);view.Canvas.ContextMenu=menu;menu.Open(view.Canvas);e.Handled=true;}};
  view.Canvas.KeyDown+=(_,e)=>{if(view.Canvas.SelectedUid is not int uid||e.KeyModifiers!=KeyModifiers.Alt||e.Key is not (Key.Up or Key.Down))return;int index=_views.IndexOf(view)+(e.Key==Key.Down?1:-1);if(index>=0&&index<_views.Count)MoveTask(view.Id,_views[index].Id,uid);e.Handled=true;};
 }
 private void UpdateSelector(){bool was=_updating;_updating=true;var choices=_views.Select(v=>new LaneChoice(v.Id,v.Name)).ToArray();_selector.ItemsSource=choices;_selector.SelectedItem=choices.FirstOrDefault(c=>c.Id==Active.Id);foreach(var v in _views)v.Card.BorderBrush=v.Id==Active.Id?DesignTokens.Accent:DesignTokens.Separator;_updating=was;}
 public void SelectLane(string id){if(!_views.Any(v=>v.Id==id))throw new ArgumentException("Timeline not found.");_active=id;UpdateSelector();Persist();}
 public string AddTimeline(string? name=null){string id=Guid.NewGuid().ToString("N");int number=1;while(_views.Any(v=>v.Name=="Timeline "+number))number++;AddLane(name??$"Timeline {number}",[],new(),id);_active=id;UpdateSelector();Persist();HeightRequested?.Invoke(Math.Min(600,60+_views.Count*150));return id;}
 public void RenameTimeline(string id,string name){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a timeline name.");var layout=Lanes.Select(l=>l.Id==id?l with{Name=name.Trim()}:l).ToArray();Rebuild(layout);Persist();}
 public void RemoveTimeline(string id){if(_views.Count==1)throw new InvalidOperationException("Keep at least one timeline. Use Show / Hide Timeline to hide the module.");if(!_views.Any(v=>v.Id==id))return;var layout=Lanes.Where(l=>l.Id!=id).ToArray();Rebuild(layout);Persist();}
 public void RestoreLayout(TimelineLayout layout){Rebuild(layout.Lanes);if(layout.ActiveId!=null&&_views.Any(v=>v.Id==layout.ActiveId))SelectLane(layout.ActiveId);Persist();}
 private void Rebuild(IEnumerable<TimelineLane> layout){CancelDrag();_updating=true;try{_views.Clear();_cards.Children.Clear();foreach(var lane in layout)AddLane(lane.Name,lane.TaskUids,lane.View,lane.Id,lane.Style,lane.ItemStyles);_active=Active.Id;RefreshLanes();UpdateSelector();}finally{_updating=false;}}
 public void SetSelection(IEnumerable<int> uids,bool persist){Active.Uids=uids.Distinct().Where(id=>_tasks.Any(t=>t.Uid==id)).ToList();RefreshLanes();if(persist)Persist();}
 public void AddSelection(IEnumerable<int> uids)=>SetSelection(Active.Uids.Concat(uids),true);
 public void RemoveSelection(IEnumerable<int> uids)=>SetSelection(Active.Uids.Except(uids),true);
 public void MoveTask(string sourceId,string targetId,int uid){var source=_views.Single(v=>v.Id==sourceId);var target=_views.Single(v=>v.Id==targetId);if(source==target||!source.Uids.Contains(uid))return;source.Uids.Remove(uid);if(!target.Uids.Contains(uid)){target.Uids.Add(uid);if(source.ItemStyles.TryGetValue(uid,out var style))target.ItemStyles[uid]=style;}source.ItemStyles.Remove(uid);_active=target.Id;target.Canvas.SelectedUid=uid;RefreshLanes();UpdateSelector();Persist();_hint.Text=$"Moved to {target.Name} · task dates unchanged";}
 private void RefreshLanes(){foreach(var view in _views){view.Canvas.SetAppearance(view.Style,view.ItemStyles);view.Canvas.SetTasks(_tasks.Where(t=>view.Uids.Contains(t.Uid)).ToList());}}
 public async Task EditDates(string? id=null){var view=id==null?Active:_views.Single(v=>v.Id==id);if(TopLevel.GetTopLevel(this)is Window owner){var range=await new DateRangeWindow(view.Name+" · Zoom and dates",view.Axis).ShowDialog<DateAxisView?>(owner);if(range!=null)view.Axis.Restore(range);}}
 private static TimelineStyle SafeStyle(TimelineStyle? value){try{return(value??new()).Checked();}catch(ArgumentException){return new();}}
 private static Dictionary<int,TimelineItemStyle> SafeItems(Dictionary<int,TimelineItemStyle>? values){var result=new Dictionary<int,TimelineItemStyle>();foreach(var item in values??[]){try{result[item.Key]=item.Value.Checked();}catch(ArgumentException){}}return result;}
 public void SetAppearance(string id,TimelineStyle style){var view=_views.Single(v=>v.Id==id);view.Style=style.Checked();RefreshLanes();Persist();}
 public void SetBlockAppearance(string id,int uid,TimelineItemStyle style){var view=_views.Single(v=>v.Id==id);if(!view.Uids.Contains(uid))throw new ArgumentException("Choose a block on this timeline.");view.ItemStyles[uid]=style.Checked();RefreshLanes();Persist();}
 public async Task EditAppearance(string? id=null){var view=id==null?Active:_views.Single(v=>v.Id==id);if(TopLevel.GetTopLevel(this)is Window owner){var style=await new TimelineStyleWindow(view.Name,view.Style).ShowDialog<TimelineStyle?>(owner);if(style!=null)SetAppearance(view.Id,style);}}
 public async Task EditBlockAppearance(string id,int uid){var view=_views.Single(v=>v.Id==id);var task=_tasks.Single(t=>t.Uid==uid);if(TopLevel.GetTopLevel(this)is Window owner){var style=await new TimelineItemStyleWindow(task.Name??"",view.ItemStyles.GetValueOrDefault(uid)??new()).ShowDialog<TimelineItemStyle?>(owner);if(style!=null)SetBlockAppearance(id,uid,style);}}
 public void RefreshAppearance(){foreach(var view in _views)view.Canvas.InvalidateVisual();}
 private void Persist(){if(_updating||_projectKey.Length==0)return;_preferences.TimelineLayouts[_projectKey]=new(){Lanes=Lanes.ToList(),ActiveId=Active.Id};_preferences.TimelineTasks[_projectKey]=SelectedUids.ToList();try{_preferences.Save(_settingsPath);}catch(Exception e){Error?.Invoke("Could not save timeline preferences: "+e.Message);}}
 private LaneView? TargetAt(Point point)=>_views.FirstOrDefault(v=>{var p=v.Card.TranslatePoint(new Point(),this);return p.HasValue&&new Rect(p.Value,v.Card.Bounds.Size).Contains(point)&&new Rect(Bounds.Size).Contains(point);});
 private void MovePointer(PointerEventArgs e){if(_drag==null)return;var point=e.GetPosition(this);if(!_drag.Moving&&Math.Sqrt(Math.Pow(point.X-_drag.Origin.X,2)+Math.Pow(point.Y-_drag.Origin.Y,2))<6)return;_drag=_drag with{Moving=true};_dragPoint=point;_dragScroll.Start();_ghost.IsVisible=true;((TextBlock)_ghost.Child!).Text=_tasks.First(t=>t.Uid==_drag.Uid).Name;Canvas.SetLeft(_ghost,point.X+10);Canvas.SetTop(_ghost,point.Y+10);var target=TargetAt(point);foreach(var view in _views)view.Card.BorderBrush=view==target?DesignTokens.Accent:DesignTokens.Separator;}
 private void ReleasePointer(PointerReleasedEventArgs e){var drag=_drag;if(drag==null)return;var target=TargetAt(e.GetPosition(this));CancelDrag();if(drag.Moving&&target!=null)MoveTask(drag.LaneId,target.Id,drag.Uid);e.Handled=true;}
 private void CancelDrag(){var drag=_drag;_drag=null;_dragScroll.Stop();_ghost.IsVisible=false;if(drag?.Pointer.Captured==this)drag.Pointer.Capture(null);if(_views.Count>0)UpdateSelector();}
}
public sealed record TimelineBlock(int Uid,Rect Bounds,bool Milestone,Rect? MarkerBounds=null);
public sealed class TimelineCanvas : Control {
 private readonly DateAxis _axis;private List<ProjectTask> _tasks=[];private double _viewport=1000;private List<(ProjectTask Task,int Lane)> _bars=[],_milestones=[];private double _lineY=80;
 private TimelineStyle _style=new();private IReadOnlyDictionary<int,TimelineItemStyle> _items=new Dictionary<int,TimelineItemStyle>();
 public int? SelectedUid {get;set;}public IReadOnlyList<TimelineBlock> Blocks {get;private set;}=[];
 public TimelineStyle Style=>_style;
 public void SetAppearance(TimelineStyle style,IReadOnlyDictionary<int,TimelineItemStyle> items){_style=style;_items=items;InvalidateVisual();}
 private string Label(ProjectTask task)=>_items.GetValueOrDefault(task.Uid)?.Label??task.Name??"";
 private IBrush BlockBrush(ProjectTask task,bool milestone)=>FormatVisuals.Brush(_items.GetValueOrDefault(task.Uid)?.Color??(milestone?_style.MilestoneColor:_style.BarColor),milestone?DesignTokens.Link:DesignTokens.TimelineFill);
 public TimelineCanvas(DateAxis axis){_axis=axis;axis.Changed+=Update;ClipToBounds=true;}
 public void SetViewport(double width){_viewport=Math.Max(300,width);Update();}
 public void SetTasks(List<ProjectTask> tasks){_tasks=tasks;Update();}
 public TimelineBlock? HitBlock(Point point)=>Blocks.LastOrDefault(b=>b.Bounds.Contains(point)||b.MarkerBounds?.Contains(point)==true);
 private double BarX(ProjectTask t)=>Math.Max(12,_axis.X(t.Start!.Value,Width-24));
 private double BarRight(ProjectTask t)=>Math.Min(Width-12,_axis.X(t.Finish!.Value,Width-24));
 private void Update(){Width=_axis.PlotWidth(_viewport)+24;_bars=[];_milestones=[];var ends=new List<double>();var callouts=new List<double>();foreach(var t in _tasks.Where(t=>t.Start.HasValue&&t.Finish.HasValue&&t.Finish>=_axis.Start&&t.Start<_axis.Finish).OrderBy(t=>t.Start)){double a=_axis.X(t.Start!.Value,Width-24),b=_axis.X(t.Finish!.Value,Width-24);if(t.IsMilestone==true||t.Duration?.Value==0||t.Start==t.Finish){double label=Math.Clamp(a-65,12,Math.Max(12,Width-162));int lane=callouts.FindIndex(end=>end+6<label);if(lane<0){lane=callouts.Count;callouts.Add(label+150);}else callouts[lane]=label+150;_milestones.Add((t,lane));}else{int lane=ends.FindIndex(end=>end+12<a);if(lane<0){lane=ends.Count;ends.Add(b);}else ends[lane]=b;_bars.Add((t,lane));}}
  _lineY=45+Math.Max(1,ends.Count)*42;Height=_lineY+(_milestones.Count>0?Math.Max(1,callouts.Count)*42+20:18);Blocks=_bars.Select(item=>new TimelineBlock(item.Task.Uid,new Rect(BarX(item.Task),43+item.Lane*42,Math.Max(4,BarRight(item.Task)-BarX(item.Task)),34),false)).Concat(_milestones.Select(item=>new TimelineBlock(item.Task.Uid,new Rect(Math.Clamp(_axis.X(item.Task.Start!.Value,Width-24)-65,12,Math.Max(12,Width-162)),_lineY+14+item.Lane*42,150,36),true,new Rect(_axis.X(item.Task.Start!.Value,Width-24)-8,_lineY-8,16,16)))).ToArray();InvalidateVisual();
 }
 public override void Render(DrawingContext ctx){
  base.Render(ctx);double plot=Width-24;ChartDrawing.DateHeader(ctx,_axis,plot,36,false);
  var textBrush=FormatVisuals.Brush(_style.TextColor,DesignTokens.Text);double size=_style.FontSize,dateY=Math.Max(18,size+4);
  if(_bars.Count+_milestones.Count==0){ChartDrawing.Text(ctx,_tasks.Count==0?"Choose tasks to build this timeline":"No dated tasks in this range",20,58,450,size:13);return;}
  foreach(var (t,lane) in _bars){double x=BarX(t),right=BarRight(t),y=43+lane*42;var rect=new Rect(x,y,Math.Max(4,right-x),34);
   ctx.DrawRectangle(BlockBrush(t,false),t.Uid==SelectedUid?new Pen(DesignTokens.Accent,2):null,rect,_style.RoundedBars?5:0,_style.RoundedBars?5:0);
   using(ctx.PushClip(rect)){ChartDrawing.Text(ctx,Label(t),x+6,y+(_style.ShowDates?2:8),Math.Max(1,right-x-12),textBrush,size,bold:true);if(_style.ShowDates)ChartDrawing.Text(ctx,FormattableString.Invariant($"{t.Start:dd MMM yy} – {t.Finish:dd MMM yy}"),x+6,y+dateY,Math.Max(1,right-x-12),textBrush,10);}
  }
  ctx.DrawLine(new Pen(DesignTokens.Secondary),new Point(12,_lineY),new Point(Width-12,_lineY));
  foreach(var(t,lane)in _milestones){double x=_axis.X(t.Start!.Value,plot),y=_lineY+14+lane*42,label=Math.Clamp(x-65,12,Math.Max(12,Width-162));
   var milestoneBrush=BlockBrush(t,true);ChartDrawing.Diamond(ctx,new Point(x,_lineY),t.Uid==SelectedUid?7:5,milestoneBrush);ctx.DrawLine(new Pen(DesignTokens.Separator),new Point(x,_lineY+5),new Point(x,y));
   ChartDrawing.Text(ctx,Label(t),label,y,150,FormatVisuals.Brush(_style.TextColor,milestoneBrush),size);if(_style.ShowDates)ChartDrawing.Text(ctx,FormattableString.Invariant($"{t.Start:dd MMM yy}"),label,y+dateY,150,FormatVisuals.Brush(_style.TextColor,DesignTokens.Secondary),10);
  }
 }
}
public sealed class TimelineTaskPicker : Window {
    public TimelineTaskPicker(IReadOnlyList<ProjectTask> tasks,IEnumerable<int> selected) {
        var ids=selected.ToHashSet();Title="Choose timeline tasks";Width=600;Height=600;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=DesignTokens.Background;
        var root=new DockPanel{Margin=new Thickness(18)};var search=new TextBox{Watermark="Search tasks and milestones",Margin=new Thickness(0,0,0,12)};DockPanel.SetDock(search,Dock.Top);root.Children.Add(search);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12,Margin=new Thickness(0,12,0,0),HorizontalAlignment=HorizontalAlignment.Right};
        var clear=new Button{Content="Clear selection"};var cancel=new Button{Content="Cancel"};var apply=new Button{Content="Apply"};
        buttons.Children.Add(clear);buttons.Children.Add(cancel);buttons.Children.Add(apply);DockPanel.SetDock(buttons,Dock.Bottom);root.Children.Add(buttons);
        var list=new StackPanel{Spacing=6};root.Children.Add(new ScrollViewer{Content=list});
        void Refresh(){list.Children.Clear();foreach(var task in tasks.Where(t=>(t.Name??"").Contains(search.Text??"",StringComparison.OrdinalIgnoreCase))){var cb=new CheckBox{Content=$"{task.DisplayId} · {task.Name}  ({task.Start:dd MMM yy})",IsChecked=ids.Contains(task.Uid)};cb.IsCheckedChanged+=(_,_)=>{if(cb.IsChecked==true)ids.Add(task.Uid);else ids.Remove(task.Uid);};list.Children.Add(cb);}}
        search.TextChanged+=(_,_)=>Refresh();clear.Click+=(_,_)=>{ids.Clear();Refresh();};cancel.Click+=(_,_)=>Close(null);apply.Click+=(_,_)=>Close(ids.ToList());Content=root;Refresh();
    }
}

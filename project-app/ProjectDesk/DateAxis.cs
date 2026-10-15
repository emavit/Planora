using OfficeIMO.Project;
namespace ProjectDesk;
public sealed record DateAxisView(DateTime? Start=null,DateTime? Finish=null,double Zoom=1);
public sealed class DateAxis {
 private DateTime _projectStart=DateTime.Today,_projectFinish=DateTime.Today.AddDays(30);
 public DateTime Start {get;private set;}=DateTime.Today;
 public DateTime Finish {get;private set;}=DateTime.Today.AddDays(30);
 public DateTime LastDate=>Finish.AddTicks(-1).Date;
 public bool HasCustomRange {get;private set;}
 public double Zoom {get;private set;}=1;
 public double Days=>Math.Max(1,(Finish-Start).TotalDays);
 public event Action? Changed;
 public void SetProject(IEnumerable<ProjectTask> tasks){var dated=tasks.Where(t=>t.Start.HasValue&&t.Finish.HasValue&&t.Finish>=t.Start).ToArray();if(dated.Length>0){_projectStart=dated.Min(t=>t.Start!.Value).Date.AddDays(-2);_projectFinish=dated.Max(t=>t.Finish!.Value).Date.AddDays(3);}else{_projectStart=DateTime.Today;_projectFinish=_projectStart.AddDays(30);}if(!HasCustomRange){Start=_projectStart;Finish=_projectFinish;}Changed?.Invoke();}
 public void SetRange(DateTime start,DateTime lastDate){start=DateTime.SpecifyKind(start.Date,DateTimeKind.Unspecified);lastDate=DateTime.SpecifyKind(lastDate.Date,DateTimeKind.Unspecified);if(lastDate<start||lastDate>=DateTime.MaxValue.Date||(lastDate-start).TotalDays>36600)throw new ArgumentException("Finish must be on or after Start, within a 100-year range.");Start=start;Finish=lastDate.AddDays(1);HasCustomRange=true;Changed?.Invoke();}
 public void ResetRange(){HasCustomRange=false;Start=_projectStart;Finish=_projectFinish;Changed?.Invoke();}
 public void EntireProject(){HasCustomRange=false;Start=_projectStart;Finish=_projectFinish;Zoom=1;Changed?.Invoke();}
 public void ZoomIn()=>SetZoom(Math.Min(64,Zoom*1.5));
 public void ZoomOut()=>SetZoom(Math.Max(1,Zoom/1.5));
 public void Fit()=>SetZoom(1);
 public void SetZoom(double zoom){if(!double.IsFinite(zoom))throw new ArgumentException("Enter a finite zoom value.");Zoom=Math.Clamp(zoom,1,64);Changed?.Invoke();}
 public DateAxisView Capture()=>new(HasCustomRange?Start:null,HasCustomRange?LastDate:null,Zoom);
 public void Restore(DateAxisView view){if(!double.IsFinite(view.Zoom)||view.Zoom<1||view.Zoom>64)throw new ArgumentException("Invalid saved zoom.");if(view.Start.HasValue!=view.Finish.HasValue)throw new ArgumentException("Invalid saved date range.");if(view.Start.HasValue)SetRange(view.Start.Value,view.Finish!.Value);else ResetRange();SetZoom(view.Zoom);}
 public double PlotWidth(double viewport)=>Math.Max(300,viewport-24)*Zoom;
 public double X(DateTime date,double plotWidth)=>12+(date-Start).TotalDays/Days*plotWidth;
 public DateTime DateAt(double x,double plotWidth)=>Start.AddDays((x-12)/Math.Max(1,plotWidth)*Days);
}

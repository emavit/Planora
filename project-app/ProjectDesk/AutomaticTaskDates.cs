using OfficeIMO.Project;
using OfficeIMO;
using System.Text;
namespace ProjectDesk;
public sealed partial class ProjectSession {
 private bool _deferAutoDates;
 private static readonly HashSet<string> DateInputs=["Predecessors","Duration","IsManual","TaskMode","IsMilestone","ConstraintType","ConstraintDate","Start","Finish"];
 // Schedule an affected dependency chain in a disposable date-only projection.
 // Imported predecessors keep their recorded dates; work/cost/actual curves stay in the original model.
 private static void ComputeAutoDates(ProjectDocument document,IEnumerable<int> roots,IReadOnlySet<(int Uid,string Key)>? explicitDates=null){
  var source=document.AllTasks.Where(t=>t.Uid>0&&t.IsNull!=true).ToDictionary(t=>t.Uid);
  var outgoing=document.Dependencies.ToLookup(l=>l.Predecessor!.Uid);var incoming=document.Dependencies.ToLookup(l=>l.Successor!.Uid);
  var affected=new HashSet<int>();var pending=new Queue<int>(roots.Distinct());
  while(pending.TryDequeue(out int uid)){if(!source.ContainsKey(uid)||!affected.Add(uid))continue;foreach(var link in outgoing[uid])pending.Enqueue(link.Successor.Uid);}
  bool HasActuals(ProjectTask t)=>t.ActualStart.HasValue||t.ActualFinish.HasValue||t.ActualWork?.Minutes>0||t.ActualDuration?.Value>0||t.PercentComplete>0||t.PercentWorkComplete>0||document.Assignments.Any(a=>a.Task?.Uid==t.Uid&&(a.ActualWork?.Minutes>0||a.ActualStart.HasValue||a.ActualFinish.HasValue||a.PercentWorkComplete>0));
  var automatic=affected.Where(uid=>source[uid].IsManual!=true&&!source[uid].IsSummary&&source[uid].IsActive!=false&&!HasActuals(source[uid])).ToHashSet();
  if(automatic.Count==0)return;
  foreach(int uid in automatic){var task=source[uid];bool start=explicitDates?.Contains((uid,"Start"))==true,finish=explicitDates?.Contains((uid,"Finish"))==true;if(start&&finish&&task.Finish<task.Start)throw new ArgumentException("Finish must be on or after Start.");if(start&&!finish)task.Finish=null;else if(finish&&!start)task.Start=null;}

  // An undated automatic predecessor must be calculated too, rather than using a fabricated date.
  pending=new Queue<int>(automatic);while(pending.TryDequeue(out int uid))foreach(var link in incoming[uid]){var pred=source[link.Predecessor!.Uid];if((!pred.Start.HasValue||!pred.Finish.HasValue)&&pred.IsManual!=true&&!pred.IsSummary&&automatic.Add(pred.Uid))pending.Enqueue(pred.Uid);}
  var keep=automatic.Concat(automatic.SelectMany(uid=>incoming[uid]).Select(l=>l.Predecessor!.Uid)).ToHashSet();
  DateTime origin=document.Settings.StartDate??source.Values.Where(t=>t.Start.HasValue).Select(t=>t.Start!.Value).DefaultIfEmpty().Min();if(origin==default)throw new InvalidOperationException("Set a project start date before automatically scheduling tasks.");
  foreach(int uid in automatic){var calendar=source[uid].Calendar??document.Calendar??throw new InvalidOperationException("Choose a project or task calendar.");calendar.GetWorkingIntervals(origin);}
  // Legacy native files can omit the ISO currency code. Date scheduling does
  // not infer money: XXX is a neutral currency only in the disposable XML
  // projection. Restore the original even when serialization rejects input.
  string? currency=document.Settings.CurrencyCode;byte[] projectionBytes;
  try{if(currency==null)document.Settings.CurrencyCode="XXX";projectionBytes=Encoding.UTF8.GetBytes(document.ToXml(new ProjectSaveOptions{LossPolicy=OfficeConversionLossPolicy.Allow}));}
  finally{if(currency==null)document.Settings.CurrencyCode=null;}
  using var projection=ProjectDocument.Load(new MemoryStream(projectionBytes));
  var projectedTasks=projection.AllTasks.Where(t=>t.Uid>0).ToArray();foreach(var t in projectedTasks)if(t.Parent!=null)t.MoveTo(null);
  foreach(var a in projection.Assignments.ToArray())projection.Assignments.Remove(a);
  foreach(var link in projection.Dependencies.Where(l=>!automatic.Contains(l.Successor!.Uid)||!keep.Contains(l.Predecessor!.Uid)).ToArray())projection.Dependencies.Remove(link);
  foreach(var t in projectedTasks.Where(t=>!keep.Contains(t.Uid)))projection.Tasks.Remove(t,ProjectRemovalMode.Cascade);
  foreach(var resource in projection.Resources)foreach(var interval in resource.TimephasedData.ToArray())resource.TimephasedData.Remove(interval);
  projection.Settings.StartDate=origin;
  foreach(int uid in keep){var t=projection.Tasks.GetByUid(uid);var original=source[uid];
   foreach(var interval in t.TimephasedData.ToArray())t.TimephasedData.Remove(interval);
   t.ActualStart=null;t.ActualFinish=null;t.ActualWork=null;t.ActualDuration=null;t.PercentComplete=0;t.PercentWorkComplete=0;t.IsActive=true;t.IsNull=false;t.Type=ProjectTaskType.FixedDuration;t.EffortDriven=false;
   if(!automatic.Contains(uid)){
    if(!original.Start.HasValue||!original.Finish.HasValue)throw new InvalidOperationException($"Predecessor {original.DisplayId} needs Start and Finish dates.");
    t.IsManual=true;t.ConstraintType=ProjectConstraintType.AsSoonAsPossible;t.ConstraintDate=null;t.Deadline=null;
   }else{
    t.IsManual=false;t.Duration=original.IsMilestone==true?ProjectDuration.WorkingDays(0):original.Duration??ProjectDuration.WorkingDays(1);
    if(explicitDates?.Contains((uid,"Start"))==true&&original.Start.HasValue){t.ConstraintType=ProjectConstraintType.StartNoEarlierThan;t.ConstraintDate=original.Start;}
    else if(explicitDates?.Contains((uid,"Finish"))==true&&original.Finish.HasValue){t.ConstraintType=ProjectConstraintType.FinishNoEarlierThan;t.ConstraintDate=original.Finish;}
    foreach(var a in document.Assignments.Where(a=>a.Task?.Uid==uid&&a.Resource?.Type==ProjectResourceType.Work)){
     if(a.DelayMinutes>0||a.WorkContour is not null and not ProjectWorkContour.Flat)throw new InvalidOperationException("This task uses an assignment delay or work contour. Use Calculate Project to review its resource schedule.");
     var assignment=projection.Assignments.Add(t,projection.Resources.GetByUid(a.Resource!.Uid));assignment.Units=a.Units;
    }
   }
  }
  var result=projection.CalculateSchedule(new ProjectScheduleOptions{CalculateAssignments=false});result.Report.ThrowIfErrors();
  using(document.BeginUpdate())foreach(var plan in result.Tasks.Where(t=>automatic.Contains(t.TaskUid))){var task=source[plan.TaskUid];if(explicitDates?.Contains((task.Uid,"Start"))!=true)task.Start=plan.Start;if(explicitDates?.Contains((task.Uid,"Finish"))!=true)task.Finish=plan.Finish;task.Duration??=plan.Duration;}
  var parents=automatic.SelectMany(uid=>Ancestors(source[uid])).Distinct().OrderByDescending(t=>Depth(t)).ToArray();
  foreach(var parent in parents){var children=parent.Children.Where(t=>t.IsNull!=true&&t.IsActive!=false).ToArray();if(children.Length==0)continue;if(children.All(t=>t.Start.HasValue&&t.Finish.HasValue)){parent.Start=children.Min(t=>t.Start);parent.Finish=children.Max(t=>t.Finish);var calendar=parent.Calendar??document.Calendar;if(calendar!=null)parent.Duration=ProjectDuration.WorkingMinutes(calendar.WorkingMinutesBetween(parent.Start!.Value,parent.Finish!.Value));}}
 }
 private static IEnumerable<ProjectTask> Ancestors(ProjectTask t){for(var parent=t.Parent;parent!=null;parent=parent.Parent)yield return parent;}
 private static int Depth(ProjectTask t)=>Ancestors(t).Count();
}

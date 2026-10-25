using OfficeIMO;
using OfficeIMO.Project;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectDesk;

public sealed partial class ProjectSession : IDisposable {
    public ProjectDocument? Document { get; private set; }
    public string? SourcePath { get; private set; }
    public string? LastSavedPath { get; private set; }
    public ProjectScheduleResult? LastCalculation {get;private set;}
    public List<ProjectTask> Tasks=>Document?.AllTasks.Where(t=>t.Uid!=0&&t.IsNull!=true).ToList()??[];
    public List<ProjectResource> Resources=>Document?.Resources.Where(r=>r.Uid!=0).ToList()??[];
    public bool HasEdits=>Document!=null&&(_savedFingerprint!=HistoryFingerprint()||SourcePath==null&&LastSavedPath==null);
    private byte[]? _baseBytes;
    private readonly List<SessionCommand> _commands=[];
    private int _position,_savedPosition;
    public void Open(string path){var bytes=File.ReadAllBytes(path);ReplaceBase(bytes,ProjectDocument.Load(new MemoryStream(bytes)),Path.GetFullPath(path));}
    public void CloseProject(){Document?.Dispose();Document=null;SourcePath=null;LastSavedPath=null;LastCalculation=null;_baseBytes=null;_commands.Clear();_position=0;_savedPosition=0;}
    public async Task OpenAsync(string path){var loaded=await Task.Run(()=>{var bytes=File.ReadAllBytes(path);return(bytes,document:ProjectDocument.Load(new MemoryStream(bytes)));});ReplaceBase(loaded.bytes,loaded.document,Path.GetFullPath(path));}
    private void ReplaceBase(byte[] bytes,ProjectDocument document,string? path){Document?.Dispose();Document=document;_baseBytes=bytes;SourcePath=path;LastSavedPath=null;_commands.Clear();_position=0;_savedPosition=0;_savedFingerprint="";ProjectIdentity=document.Guid is Guid id&&id!=Guid.Empty?id:new Guid(System.Security.Cryptography.SHA256.HashData(bytes).AsSpan(0,16));StateChanged?.Invoke();}
    public void NewProject(string name,DateTime start) {
        // Project dates are local wall-clock values, never timezone-converted instants.
        start=DateTime.SpecifyKind(start,DateTimeKind.Unspecified);
        var project=ProjectDocument.Create();project.Name=name;project.Guid=Guid.NewGuid();project.Settings.StartDate=start;project.Settings.ScheduleFromStart=true;
        project.Settings.MinutesPerDay=480;project.Settings.MinutesPerWeek=2400;project.Settings.DaysPerMonth=20;
        project.Settings.DefaultStartTime=TimeSpan.FromHours(8);project.Settings.DefaultFinishTime=TimeSpan.FromHours(17);
        project.Calendar=project.Calendars.AddStandardWorkingWeek();
        ReplaceBase(Encoding.UTF8.GetBytes(project.ToXml()),project,null);
    }
    private ProjectDocument Replay(int count){if(_baseBytes==null)throw new InvalidOperationException("Open or create a project first.");var document=ProjectDocument.Load(new MemoryStream(_baseBytes));try{for(int i=0;i<count;i++)_commands[i].Apply(document);return document;}catch{document.Dispose();throw;}}
    public void Execute(string label,Action<ProjectDocument> change,DurableCommand? command=null) {
        if(Document==null)throw new InvalidOperationException("Open or create a project first.");
        try{change(Document);}catch{var restored=Replay(_position);Document.Dispose();Document=restored;throw;}
        if(_position<_commands.Count)_commands.RemoveRange(_position,_commands.Count-_position);
        _commands.Add(new(label,change,command,Guid.NewGuid()));_position++;StateChanged?.Invoke();
    }
    public void Undo(){if(_position==0)return;var replacement=Replay(_position-1);Document!.Dispose();Document=replacement;_position--;StateChanged?.Invoke();}
    public void Redo(){if(_position>=_commands.Count)return;var replacement=Replay(_position+1);Document!.Dispose();Document=replacement;_position++;StateChanged?.Invoke();}
    public int HistoryPosition=>_position;
    public void DiscardRedo(){if(_position<_commands.Count)_commands.RemoveRange(_position,_commands.Count-_position);}
    public bool CanUndo=>_position>0;
    public bool CanRedo=>_position<_commands.Count;
    public void EditCells(IEnumerable<TextCellChange> changes,string label="Paste cells"){
        var values=changes.ToArray();if(values.Length==0)return;
        using var staging=new ProjectSession{_baseBytes=_baseBytes,Document=Replay(_position),SourcePath=SourcePath,_position=_position,_deferAutoDates=true};staging._commands.AddRange(_commands.Take(_position));
        foreach(var change in values)staging.EditText(change.TaskUid,change.Key,change.Text);
        var affected=values.Where(c=>DateInputs.Contains(c.Key)).Select(c=>c.TaskUid).Distinct().ToArray();var explicitDates=values.Where(c=>c.Key is "Start" or "Finish"&&!string.IsNullOrWhiteSpace(c.Text)).Select(c=>(c.TaskUid,c.Key)).ToHashSet();if(affected.Length>0)staging.Execute("Update automatic dates",d=>ComputeAutoDates(d,affected,explicitDates), DurableCommand.Dates(affected,explicitDates));
        var steps=staging._commands.Skip(_position).ToArray();if(steps.Length>0)Execute(label,d=>{foreach(var step in steps)step.Apply(d);}, DurableCommand.Batch(steps.Select(s=>s.Command)));
    }
    public void RenumberWbs()=>Execute("Renumber WBS",d=>{void Number(IEnumerable<ProjectTask> tasks,string prefix){int i=0;foreach(var t in tasks.Where(t=>t.Uid!=0)){string code=prefix+(++i);t.Wbs=code;Number(t.Children,code+".");}}Number(d.Tasks,"");}, DurableCommand.For("RenumberWbs"));
    public void Rename(int uid,string name){if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a task name.");if(Document!.Tasks.GetByUid(uid).Name==name)return;Execute("Rename task",d=>d.Tasks.GetByUid(uid).Name=name, DurableCommand.For("Rename",uid,name));}
    public void EditField(int uid,TaskFieldChange change) {
        if(change.Key=="PercentComplete"&&change.Value is int percent){SetProgress([uid],percent);return;}
        if(change.Key=="Name"){Rename(uid,change.Value as string??"");return;}
        var property=TaskFieldEditing.Property(change.Key)??throw new ArgumentException("This column is read-only.");
        bool schedule=!_deferAutoDates&&DateInputs.Contains(change.Key);Execute("Edit "+change.Key,d=>{property.SetValue(d.Tasks.GetByUid(uid),change.Value);if(schedule){var explicitDates=change.Key is "Start" or "Finish"&&change.Value!=null?new HashSet<(int,string)>{(uid,change.Key)}:null;ComputeAutoDates(d,[uid],explicitDates);}}, DurableCommand.For("EditFieldValue",uid,change.Key,change.Value));
    }
    public static bool CanEditText(string key)=>TaskFieldEditing.Property(key)!=null||key is "Predecessors" or "ResourceNames";
    public string EditText(int uid,string key){
        var t=Document!.Tasks.GetByUid(uid);
        if(key=="Predecessors")return TaskColumns.Catalogue(Document).First(c=>c.Key==key).Read(t,Document)??"";
        if(key=="ResourceNames")return string.Join("; ",Document.Assignments.Where(a=>a.Task?.Uid==uid&&a.Resource?.Uid>0).Select(a=>$"{a.Resource!.Name}[{(a.Units?.Value??1)*100:0.##}%]"));
        var p=TaskFieldEditing.Property(key)??throw new ArgumentException("This column is read-only.");var value=p.GetValue(t);
        return value switch{ProjectDuration duration=>EditorInput.DurationText(duration),DateTime date=>EditorInput.DateText(date),bool manual when key=="TaskMode"=>manual?"Manually Scheduled":"Auto Scheduled",bool b=>b?"Yes":"No",_=>Convert.ToString(value,CultureInfo.InvariantCulture)??""};
    }
    public void EditText(int uid,string key,string text){
        if(key=="Predecessors"){SetPredecessors(uid,text);return;}
        if(key=="ResourceNames"){SetResourceNames(uid,text);return;}
        var propertyKey=TaskFieldEditing.PropertyKey(key);
        EditField(uid,new(propertyKey,TaskFieldEditing.Parse(propertyKey,text)));
    }
    private void SetPredecessors(int uid,string text){
        var links=new List<(int Uid,ProjectDependencyType Type,ProjectDuration Lag)>();
        foreach(var token in text.Split([',',';'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)){
            var match=Regex.Match(token,@"^(\d+)\s*(FS|SS|FF|SF)?\s*([+-].+)?$",RegexOptions.IgnoreCase);
            if(!match.Success)throw new ArgumentException("Enter predecessors such as 3, 5FS+2d or 8SS-4h.");
            int id=int.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture);var task=Tasks.SingleOrDefault(t=>t.DisplayId==id)??throw new ArgumentException($"Task ID {id} was not found.");
            if(task.Uid==uid||links.Any(l=>l.Uid==task.Uid))throw new ArgumentException("A predecessor cannot be this task or appear twice.");
            var type=match.Groups[2].Value.ToUpperInvariant() switch{"SS"=>ProjectDependencyType.StartToStart,"FF"=>ProjectDependencyType.FinishToFinish,"SF"=>ProjectDependencyType.StartToFinish,_=>ProjectDependencyType.FinishToStart};
            var lag=EditorInput.Duration(match.Groups[3].Value,true)??ProjectDuration.WorkingDays(0);links.Add((task.Uid,type,lag));
        }
        bool schedule=!_deferAutoDates;Execute("Edit predecessors",d=>{foreach(var old in d.Dependencies.Where(l=>l.Successor.Uid==uid).ToArray())d.Dependencies.Remove(old);var to=d.Tasks.GetByUid(uid);foreach(var l in links){var from=d.Tasks.GetByUid(l.Uid);if(CreatesCycle(d,from,to))throw new InvalidOperationException("These predecessors would create a circular dependency.");d.Dependencies.Add(from,to,l.Type).Lag=l.Lag;}if(schedule)ComputeAutoDates(d,[uid]);}, DurableCommand.For("SetPredecessors",uid,text));
    }
    private void SetResourceNames(int uid,string text){
        var chosen=new List<(int Uid,decimal Units)>();
        foreach(var token in text.Split([',',';'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)){
            var m=Regex.Match(token,@"^(.+?)(?:\[(\d+(?:\.\d+)?)%\])?$");if(!m.Success)throw new ArgumentException("Use resource names, optionally with units: Engineer[50%].");
            var matches=Resources.Where(r=>string.Equals(r.Name,m.Groups[1].Value.Trim(),StringComparison.OrdinalIgnoreCase)).ToArray();
            if(matches.Length!=1)throw new ArgumentException($"Resource '{m.Groups[1].Value}' must match one existing resource. Add it in the Resource tab first.");
            if(chosen.Any(r=>r.Uid==matches[0].Uid))throw new ArgumentException("A resource cannot appear twice.");
            chosen.Add((matches[0].Uid,m.Groups[2].Success?decimal.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture):100));
        }
        Execute("Edit resource assignments",d=>{foreach(var a in d.Assignments.Where(a=>a.Task?.Uid==uid&&a.Resource?.Uid>0&&!chosen.Any(r=>r.Uid==a.Resource.Uid)).ToArray())d.Assignments.Remove(a);foreach(var r in chosen){var a=d.Assignments.FirstOrDefault(a=>a.Task?.Uid==uid&&a.Resource?.Uid==r.Uid)??d.Assignments.Add(d.Tasks.GetByUid(uid),d.Resources.GetByUid(r.Uid));a.Units=ProjectUnits.Percent(r.Units);}}, DurableCommand.For("SetResourceNames",uid,text));
    }
    public int AddTask(string name="New task",bool milestone=false,int? parentUid=null,int? beforeUid=null,bool calculateDates=true,Guid? identity=null) {
        int added=0;identity??=Guid.NewGuid();bool initialiseDates=calculateDates&&!_deferAutoDates;
        Execute("Insert task",d=>{var collection=parentUid.HasValue?d.Tasks.GetByUid(parentUid.Value).Children:d.Tasks;var task=collection.Add(name);added=task.Uid;task.Guid=d.NativeInfo?.Generation is "MPP8" or "MPP9"?null:identity;
            task.DisplayId=d.AllTasks.Count(t=>t.Uid!=0);task.Duration=ProjectDuration.WorkingDays(milestone?0:1);task.IsManual=false;task.IsMilestone=milestone;
            task.IsActive=true;task.IsNull=false;task.IsCritical=false;task.EffortDriven=false;task.Type=ProjectTaskType.FixedUnits;
            if(beforeUid.HasValue){var before=d.Tasks.GetByUid(beforeUid.Value);var siblings=(before.Parent?.Children??d.Tasks).ToList();task.MoveTo(before.Parent,siblings.IndexOf(before));Renumber(d);}
            if(parentUid.HasValue)Renumber(d);if(initialiseDates)ComputeAutoDates(d,[task.Uid]);
        }, DurableCommand.For("AddTask",name,milestone,parentUid,beforeUid,calculateDates,identity));
        return added;
    }
    public void DeleteTasks(IEnumerable<int> uids){var ids=uids.Distinct().ToArray();Execute("Delete tasks",d=>{foreach(var uid in ids){var t=d.AllTasks.FirstOrDefault(t=>t.Uid==uid);if(t!=null)(t.Parent?.Children??d.Tasks).Remove(t,ProjectRemovalMode.Cascade);}Renumber(d);}, DurableCommand.For("DeleteTasks",(object)ids));}
    public void Indent(int uid){Execute("Indent task",d=>{var t=d.Tasks.GetByUid(uid);var siblings=(t.Parent?.Children??d.Tasks).ToList();int index=siblings.IndexOf(t);if(index<1)throw new InvalidOperationException("The first task in this level cannot be indented.");t.MoveTo(siblings[index-1]);Renumber(d);}, DurableCommand.For("Indent",uid));}
    public void Outdent(int uid){Execute("Outdent task",d=>{var t=d.Tasks.GetByUid(uid);var parent=t.Parent;if(parent==null)return;var target=parent.Parent;var siblings=(target?.Children??d.Tasks).ToList();t.MoveTo(target,siblings.IndexOf(parent)+1);Renumber(d);}, DurableCommand.For("Outdent",uid));}
    private static void Renumber(ProjectDocument d){int id=1;foreach(var task in d.AllTasks.Where(t=>t.Uid!=0))task.DisplayId=id++;}
    public void EditTask(int uid,TaskEdit value) {
        value.Validate();bool schedule=!_deferAutoDates;Execute("Task information",d=>{var t=d.Tasks.GetByUid(uid);using(d.BeginUpdate()){
            t.Name=value.Name;t.Duration=value.Duration;t.IsManual=value.Manual;t.Type=value.Type;t.IsMilestone=value.Milestone;t.EffortDriven=value.EffortDriven;
            t.Start=value.Start;t.Finish=value.Finish;t.ConstraintType=value.ConstraintType;t.ConstraintDate=value.ConstraintDate;
            t.Work=value.WorkHours.HasValue?new ProjectWork(value.WorkHours.Value*60):null;t.FixedCost=value.FixedCost;
            if(value.PercentComplete is int progress && progress!=t.PercentComplete) ApplyProgress(d,[uid],progress);else t.PercentComplete=value.PercentComplete;
            t.Notes=value.Notes;t.Calendar=value.CalendarUid.HasValue?d.Calendars.GetByUid(value.CalendarUid.Value):null;
        }if(schedule)ComputeAutoDates(d,[uid]);}, DurableCommand.For("EditTask",uid,value));
    }
    public void SetProgress(IEnumerable<int> uids,int percent){
        if(percent<0||percent>100)throw new ArgumentOutOfRangeException(nameof(percent));var ids=uids.ToArray();
        Execute("Update progress",d=>ApplyProgress(d,ids,percent), DurableCommand.For("SetProgress",ids,percent));
    }
    private static void ApplyProgress(ProjectDocument d,IEnumerable<int> ids,int percent){foreach(var uid in ids){
            var task=d.Tasks.GetByUid(uid);
            var targets=task.IsSummary?Descendants(task).Where(t=>!t.IsSummary):new[]{task};
            foreach(var t in targets){
                t.PercentComplete=percent;
                if(t.Duration is ProjectDuration duration){t.ActualDuration=new(duration.Value*percent/100m,duration.Unit,duration.IsElapsed);t.RemainingDuration=new(duration.Value*(100-percent)/100m,duration.Unit,duration.IsElapsed);}
                t.ActualStart=percent>0?t.Start:null;t.ActualFinish=percent==100?t.Finish:null;
                if(t.Work is ProjectWork work){t.ActualWork=new ProjectWork(work.Minutes*percent/100m);t.RemainingWork=new ProjectWork(work.Minutes*(100-percent)/100m);t.PercentWorkComplete=percent;}
                foreach(var a in d.Assignments.Where(a=>a.Task?.Uid==t.Uid&&a.Resource?.Type==ProjectResourceType.Work))
                    if(a.Work is ProjectWork assigned){a.ActualWork=new ProjectWork(assigned.Minutes*percent/100m);a.RemainingWork=new ProjectWork(assigned.Minutes*(100-percent)/100m);}
            }
        }}
    private static IEnumerable<ProjectTask> Descendants(ProjectTask task){foreach(var child in task.Children){yield return child;foreach(var inner in Descendants(child))yield return inner;}}    public void LinkTasks(int predecessorUid,int successorUid,ProjectDependencyType type,ProjectDuration lag){bool schedule=!_deferAutoDates;Execute("Link tasks",d=>{var a=d.Tasks.GetByUid(predecessorUid);var b=d.Tasks.GetByUid(successorUid);if(CreatesCycle(d,a,b))throw new InvalidOperationException("This link would create a circular dependency.");var existing=d.Dependencies.FirstOrDefault(l=>l.Predecessor?.Uid==a.Uid&&l.Successor?.Uid==b.Uid);if(existing!=null){existing.Type=type;existing.Lag=lag;}else{var link=d.Dependencies.Add(a,b,type);link.Lag=lag;}if(schedule)ComputeAutoDates(d,[successorUid]);}, DurableCommand.For("LinkTasks",predecessorUid,successorUid,type,lag));}
    public void UnlinkTasks(IEnumerable<int> uids){var ids=uids.ToHashSet();bool schedule=!_deferAutoDates;Execute("Unlink tasks",d=>{foreach(var link in d.Dependencies.Where(l=>ids.Contains(l.Predecessor?.Uid??-1)&&ids.Contains(l.Successor?.Uid??-1)).ToArray())d.Dependencies.Remove(link);if(schedule)ComputeAutoDates(d,ids);}, DurableCommand.For("UnlinkTasks",(object)ids.ToArray()));}
    private static bool CreatesCycle(ProjectDocument d,ProjectTask from,ProjectTask to){var pending=new Stack<int>();var seen=new HashSet<int>();pending.Push(to.Uid);while(pending.TryPop(out int id)){if(id==from.Uid)return true;if(!seen.Add(id))continue;foreach(var next in d.Dependencies.Where(l=>l.Predecessor?.Uid==id).Select(l=>l.Successor?.Uid).OfType<int>())pending.Push(next);}return false;}
    public int AddResource(string name,ProjectResourceType type,decimal maxUnitsPercent,decimal standardRate,Guid? identity=null){int uid=0;identity??=Guid.NewGuid();if(string.IsNullOrWhiteSpace(name)||maxUnitsPercent<0||standardRate<0)throw new ArgumentException("Enter a name, nonnegative capacity and rate.");Execute("Add resource",d=>{var r=type switch{ProjectResourceType.Material=>d.Resources.AddMaterial(name),ProjectResourceType.Cost=>d.Resources.AddCost(name),_=>d.Resources.AddWork(name)};r.Guid=d.NativeInfo?.Generation is "MPP8" or "MPP9"?null:identity;r.IsNull=false;r.DisplayId=d.Resources.Count(x=>x.Uid!=0);uid=r.Uid;r.MaxUnits=ProjectUnits.Percent(maxUnitsPercent);r.StandardRate=standardRate;}, DurableCommand.For("AddResource",name,type,maxUnitsPercent,standardRate,identity));return uid;}
    public void EditResource(int uid,ResourceEdit value){if(string.IsNullOrWhiteSpace(value.Name)||value.MaxUnitsPercent<0||value.StandardRate<0||value.OvertimeRate<0)throw new ArgumentException("Enter a valid resource name, capacity and rates.");Execute("Edit resource",d=>{var r=d.Resources.GetByUid(uid);r.Name=value.Name;r.Type=value.Type;r.MaxUnits=ProjectUnits.Percent(value.MaxUnitsPercent);r.StandardRate=value.StandardRate;r.OvertimeRate=value.OvertimeRate;r.CostPerUse=value.CostPerUse;r.Group=value.Group;r.Initials=value.Initials;r.Calendar=value.CalendarUid.HasValue?d.Calendars.GetByUid(value.CalendarUid.Value):null;}, DurableCommand.For("EditResource",uid,value));}
    public string ResourceText(int uid,string key){var r=Document!.Resources.GetByUid(uid);object? value=key switch{"Capacity"=>(r.MaxUnits?.Value??1)*100,"Rate"=>r.StandardRate,"OvertimeRate"=>r.OvertimeRate,"CostPerUse"=>r.CostPerUse,"Type"=>r.Type,"Group"=>r.Group,"Initials"=>r.Initials,_=>r.Name};return Convert.ToString(value,CultureInfo.InvariantCulture)??"";}
    public void EditResourceText(int uid,string key,string text){
        text=text.Trim();object? value=key switch{"Name" or "Group" or "Initials"=>text,"Type"=>Enum.Parse<ProjectResourceType>(text,true),"Capacity" or "Rate" or "OvertimeRate" or "CostPerUse"=>decimal.Parse(text.Replace("/h","",StringComparison.OrdinalIgnoreCase).TrimEnd('%'),CultureInfo.InvariantCulture),_=>throw new ArgumentException("This resource field is calculated.")};
        if(key=="Name"&&text.Length==0)throw new ArgumentException("Enter a resource name.");if(value is decimal n&&n<0)throw new ArgumentException("Resource units and rates cannot be negative.");
        Execute("Edit resource "+key,d=>{var r=d.Resources.GetByUid(uid);switch(key){case "Capacity":r.MaxUnits=ProjectUnits.Percent((decimal)value!);break;case "Rate":r.StandardRate=(decimal)value!;break;case "OvertimeRate":r.OvertimeRate=(decimal)value!;break;case "CostPerUse":r.CostPerUse=(decimal)value!;break;default:typeof(ProjectResource).GetProperty(key)!.SetValue(r,value);break;}}, DurableCommand.For("EditResourceText",uid,key,text));
    }
    public string AssignmentText(int uid,string key){var a=Document!.Assignments.Single(a=>a.Uid==uid);return key=="Units"?((a.Units?.Value??1)*100).ToString(CultureInfo.InvariantCulture)+"%":((a.Work?.Minutes??0)/60).ToString(CultureInfo.InvariantCulture)+"h";}
    public void EditAssignmentText(int uid,string key,string text){
        if(key=="Units"){var units=EditorInput.Number(text)??throw new ArgumentException("Enter assignment units.");if(units<0)throw new ArgumentException("Units cannot be negative.");Execute("Edit assignment units",d=>d.Assignments.Single(a=>a.Uid==uid).Units=ProjectUnits.Percent(units), DurableCommand.For("EditAssignmentText",uid,key,text));return;}
        if(key!="Work")throw new ArgumentException("This assignment field is calculated.");
        if(decimal.TryParse(text,NumberStyles.Number,CultureInfo.InvariantCulture,out _))text+="h";
        var duration=EditorInput.Duration(text)??throw new ArgumentException("Enter work such as 8h or 120min.");decimal minutes=duration.Value*(duration.Unit switch{ProjectDurationUnit.Minute=>1,ProjectDurationUnit.Hour=>60,ProjectDurationUnit.Week=>Document!.Settings.MinutesPerWeek??2400,ProjectDurationUnit.Month=>(Document!.Settings.MinutesPerDay??480)*(Document.Settings.DaysPerMonth??20),_=>Document!.Settings.MinutesPerDay??480});
        Execute("Edit assignment work",d=>{var a=d.Assignments.Single(a=>a.Uid==uid);if(minutes<(a.ActualWork?.Minutes??0))throw new ArgumentException("Total work cannot be less than actual work.");a.Work=new ProjectWork(minutes);a.RemainingWork=new ProjectWork(minutes-(a.ActualWork?.Minutes??0));}, DurableCommand.For("EditAssignmentText",uid,key,text));
    }
    public void DeleteResource(int uid)=>Execute("Delete resource",d=>{d.Resources.Remove(d.Resources.GetByUid(uid),ProjectRemovalMode.Cascade);int id=1;foreach(var resource in d.Resources.Where(r=>r.Uid!=0))resource.DisplayId=id++;}, DurableCommand.For("DeleteResource",uid));
    public void Assign(int taskUid,int resourceUid,decimal unitsPercent,decimal? workHours=null){if(unitsPercent<0||workHours<0)throw new ArgumentException("Units and work cannot be negative.");Execute("Assign resource",d=>{var assignment=d.Assignments.FirstOrDefault(a=>a.Task?.Uid==taskUid&&a.Resource?.Uid==resourceUid)??d.Assignments.Add(d.Tasks.GetByUid(taskUid),d.Resources.GetByUid(resourceUid));assignment.Units=ProjectUnits.Percent(unitsPercent);if(workHours.HasValue)assignment.Work=new ProjectWork(workHours.Value*60);}, DurableCommand.For("Assign",taskUid,resourceUid,unitsPercent,workHours));}
    public void Unassign(int taskUid,int resourceUid)=>Execute("Remove assignment",d=>{foreach(var a in d.Assignments.Where(a=>a.Task?.Uid==taskUid&&a.Resource?.Uid==resourceUid).ToArray())d.Assignments.Remove(a);}, DurableCommand.For("Unassign",taskUid,resourceUid));
    public void Recalculate(bool recalculateActualCosts=false,bool calculateAssignments=true)=>Execute("Calculate project",d=>{
        LastCalculation=d.CalculateSchedule(new ProjectScheduleOptions{CalculateAssignments=calculateAssignments,RecalculateActualCosts=recalculateActualCosts});
        LastCalculation.Report.ThrowIfErrors();
        d.ApplyScheduleTotals(LastCalculation);
    }, DurableCommand.For("Recalculate",recalculateActualCosts,calculateAssignments));    public void CaptureBaseline(int number,bool overwrite=false){if(number<0||number>10)throw new ArgumentOutOfRangeException(nameof(number));Execute("Set baseline",d=>{
        if(!overwrite&&(d.AllTasks.Any(t=>t.Baselines.Any(b=>(b.Number??0)==number))||d.Assignments.Any(a=>a.Baselines.Any(b=>(b.Number??0)==number))))throw new InvalidOperationException("This baseline already exists. Choose another number or explicitly overwrite it.");
        foreach(var t in d.AllTasks){Replace(t.Baselines,number,b=>{b.Start=t.Start;b.Finish=t.Finish;b.Duration=t.Duration;b.Work=t.Work;b.Cost=t.Cost;
            // Native baseline fixed cost has its own field from MPP9 onward; zero carries no additional amount
            // and stays absent so generations without that field remain writable.
            b.FixedCost=d.NativeInfo!=null&&t.FixedCost==0?null:t.FixedCost;});}
        foreach(var a in d.Assignments){Replace(a.Baselines,number,b=>{b.Start=a.Start;b.Finish=a.Finish;b.Work=a.Work;b.Cost=a.Cost;});}
        foreach(var r in d.Resources){Replace(r.Baselines,number,b=>{b.Work=r.Work;b.Cost=r.Cost;});}
    }, DurableCommand.For("CaptureBaseline",number,overwrite));}
    private static void Replace(ProjectCollection<ProjectBaseline> baselines,int number,Action<ProjectBaseline> fill){foreach(var old in baselines.Where(b=>(b.Number??0)==number).ToArray())baselines.Remove(old);var b=baselines.Add();b.Number=number;fill(b);}
    public void ClearBaseline(int number)=>Execute("Clear baseline",d=>{foreach(var group in d.AllTasks.Select(t=>t.Baselines).Concat(d.Assignments.Select(a=>a.Baselines)).Concat(d.Resources.Select(r=>r.Baselines)))foreach(var b in group.Where(b=>(b.Number??0)==number).ToArray())group.Remove(b);}, DurableCommand.For("ClearBaseline",number));
    public void SetWorkingWeek(int calendarUid,IReadOnlyDictionary<DayOfWeek,ProjectWorkingTime[]> days){var copy=days.ToDictionary(p=>p.Key,p=>p.Value.ToArray());Execute("Change working time",d=>{var c=d.Calendars.GetByUid(calendarUid);foreach(var (day,times)in copy)c.SetWorkingDay(day,times);}, DurableCommand.For("SetWorkingWeek",calendarUid,copy));}
    public void SetCalendarException(CalendarExceptionEdit value){if(value.Finish.Date<value.Start.Date)throw new ArgumentException("The last date must be on or after the first date.");if(string.IsNullOrWhiteSpace(value.Name))throw new ArgumentException("Enter an exception name.");Execute("Calendar exception",d=>{var c=d.Calendars.GetByUid(value.CalendarUid);var matches=c.Exceptions.Where(e=>string.Equals(e.Name,value.Name,StringComparison.OrdinalIgnoreCase)).ToArray();if(matches.Length>1)throw new ArgumentException("Several exceptions have this name. Choose a unique name.");if(value.Delete){if(matches.Length==0)throw new ArgumentException("No exception has this name.");c.Exceptions.Remove(matches[0]);return;}var e=matches.FirstOrDefault()??c.Exceptions.Add();e.Name=value.Name;e.FromDate=value.Start.Date;e.ToDate=value.Finish.Date.AddHours(23).AddMinutes(59);e.IsWorking=value.Times.Length>0;foreach(var old in e.WorkingTimes.ToArray())e.WorkingTimes.Remove(old);foreach(var t in value.Times){var interval=e.WorkingTimes.Add();interval.From=t.From;interval.To=t.To;}}, DurableCommand.For("SetCalendarException",value));}
    public void SetProjectDates(DateTime start,DateTime? status,int? calendarUid)=>Execute("Project information",d=>{d.Settings.StartDate=start;d.Settings.StatusDate=status;d.Calendar=calendarUid.HasValue?d.Calendars.GetByUid(calendarUid.Value):d.Calendar;}, DurableCommand.For("SetProjectDates",start,status,calendarUid));
    public ProjectReport AssessSave(string path)=>Document?.AssessSave(path,new ProjectSaveOptions{LossPolicy=OfficeConversionLossPolicy.Allow})??throw new InvalidOperationException("No project open.");
    public void SaveCopy(string path,bool acceptPreservationWarnings=false){if(Document==null)throw new InvalidOperationException("Open a project first.");if(!string.Equals(Path.GetExtension(path),".mpp",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose an .mpp filename.");if(string.Equals(Path.GetFullPath(path),SourcePath,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Choose a new filename for this copy.");var options=new ProjectSaveOptions{FileConflictPolicy=OfficeConversionFileConflictPolicy.FailIfExists,LossPolicy=acceptPreservationWarnings?OfficeConversionLossPolicy.Allow:OfficeConversionLossPolicy.Block};Document.AssessSave(path,options).ThrowIfErrors();var calculation=LastCalculation;try{using var export=Replay(_position);export.Save(path,options);}finally{LastCalculation=calculation;}LastSavedPath=Path.GetFullPath(path);_savedPosition=_position;_savedFingerprint=HistoryFingerprint();StateChanged?.Invoke();}
    public void Dispose()=>Document?.Dispose();
}

public sealed record TextCellChange(int TaskUid,string Key,string Text);
public sealed record TaskEdit(string Name,ProjectDuration? Duration,bool? Manual,ProjectTaskType? Type,bool? Milestone,bool? EffortDriven,DateTime? Start,DateTime? Finish,ProjectConstraintType? ConstraintType,DateTime? ConstraintDate,decimal? WorkHours,decimal? FixedCost,int? PercentComplete,string? Notes,int? CalendarUid){
    public static TaskEdit From(ProjectTask t)=>new(t.Name??"",t.Duration,t.IsManual,t.Type,t.IsMilestone,t.EffortDriven,t.Start,t.Finish,t.ConstraintType,t.ConstraintDate,t.Work?.Minutes/60m,t.FixedCost,t.PercentComplete,t.Notes,t.Calendar?.Uid);
    public void Validate(){if(string.IsNullOrWhiteSpace(Name)||Duration?.Value<0||WorkHours<0||PercentComplete<0||PercentComplete>100||Finish<Start)throw new ArgumentException("Check the name, duration, dates, work and completion percentage.");}
}
public sealed record ResourceEdit(string Name,ProjectResourceType? Type,decimal MaxUnitsPercent,decimal StandardRate,decimal OvertimeRate,decimal? CostPerUse,string? Group,string? Initials,int? CalendarUid);

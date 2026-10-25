using OfficeIMO.Project;
using System.Text.Json;

namespace ProjectDesk;

public static class CoreValidation {
    public static int Run(string folder) {
        Directory.CreateDirectory(folder);
        using var session=new ProjectSession();
        session.NewProject("ProjectDesk core validation",new DateTime(2026,10,16,8,0,0));
        int a=session.AddTask("Design"),b=session.AddTask("Build"),milestone=session.AddTask("Release",true);
        session.LinkTasks(a,b,ProjectDependencyType.FinishToStart,ProjectDuration.WorkingDays(0));
        session.LinkTasks(b,milestone,ProjectDependencyType.FinishToStart,ProjectDuration.WorkingDays(0));
        int resource=session.AddResource("Engineer",ProjectResourceType.Work,100,50);
        session.Assign(a,resource,100,8);
        session.Recalculate();
        var design=session.Document!.Tasks.GetByUid(a);var build=session.Document.Tasks.GetByUid(b);
        Assert(design.Start==new DateTime(2026,10,16,8,0,0),"Design start");
        Assert(design.Finish==new DateTime(2026,10,16,17,0,0),"Design finish");
        Assert(build.Start==new DateTime(2026,10,19,8,0,0),"Weekend dependency");
        Assert(design.Work?.Minutes==480,"Resource work");Assert(design.Cost==400,"Resource cost");
        session.CaptureBaseline(0);session.CaptureBaseline(10);
        session.SetProgress([a],50);
        session.Recalculate(true);
        var expected=Snapshot(session.Document);
        var destination=Path.Combine(folder,"core-functions.mpp");
        var assessment=session.AssessSave(destination);
        File.WriteAllText(Path.Combine(folder,"core-save-assessment.json"),JsonSerializer.Serialize(assessment.Diagnostics.Select(d=>new{d.Code,severity=d.Severity.ToString(),d.Message,d.Location,d.RepresentsLoss}),new JsonSerializerOptions{WriteIndented=true}));
        assessment.ThrowIfErrors();
        session.SaveCopy(destination);
        using var reopened=ProjectDocument.Load(destination);
        Assert(reopened.Tasks.GetByUid(a).PercentComplete==50,"Progress save");
        Assert(reopened.Tasks.GetByUid(a).Baselines.Any(x=>x.Number==10),"Baseline 10 save");
        Assert(reopened.Dependencies.Count==2,"Dependency save");
        Assert(reopened.Assignments.Count(a=>a.Resource?.Uid>0)==1,"Resource assignment save");
        int before=session.Tasks.Count;int extra=session.AddTask("Temporary");session.Undo();Assert(session.Tasks.Count==before,"Undo structural edit");session.Redo();Assert(session.Tasks.Any(t=>t.Uid==extra),"Redo stable identity");session.DeleteTasks([extra]);Assert(session.Tasks.Count==before,"Delete task");
        session.Indent(b);Assert(session.Document!.Tasks.GetByUid(b).Parent?.Uid==a,"Indent");session.Outdent(b);Assert(session.Document.Tasks.GetByUid(b).Parent==null,"Outdent");
        int links=session.Document.Dependencies.Count;try{session.LinkTasks(milestone,a,ProjectDependencyType.FinishToStart,ProjectDuration.WorkingDays(0));throw new Exception("Cycle accepted");}catch(InvalidOperationException){}Assert(session.Document.Dependencies.Count==links,"Atomic cycle rejection");
        session.ClearBaseline(10);Assert(!session.Document.Tasks.GetByUid(a).Baselines.Any(x=>x.Number==10),"Clear baseline");session.Undo();Assert(session.Document!.Tasks.GetByUid(a).Baselines.Any(x=>x.Number==10),"Baseline undo");
        session.EditField(b,new("Priority",TaskFieldEditing.Parse("Priority","700")));Assert(session.Document.Tasks.GetByUid(b).Priority==700,"Selected field edit");session.Undo();Assert(session.Document!.Tasks.GetByUid(b).Priority!=700,"Selected field undo");
        Assert(TaskFieldEditing.Property("TaskMode")?.Name=="IsManual","Task Mode editor");Assert(TaskFieldEditing.Property("Uid")==null,"Identity remains read-only");
        try{TaskFieldEditing.Parse("PercentComplete","101");throw new Exception("Invalid progress accepted");}catch(ArgumentException){}
        var originalA=session.Document!.Tasks.GetByUid(a).Name;var originalB=session.Document.Tasks.GetByUid(b).Name;
        session.EditCells([new(a,"Name","Pasted A"),new(b,"Name","Pasted B")]);Assert(session.Document.Tasks.GetByUid(a).Name=="Pasted A"&&session.Document.Tasks.GetByUid(b).Name=="Pasted B","Atomic multi-cell paste");session.Undo();Assert(session.Document!.Tasks.GetByUid(a).Name==originalA&&session.Document.Tasks.GetByUid(b).Name==originalB,"One undo for pasted block");
        try{session.EditCells([new(a,"Name","Should not persist"),new(b,"Duration","bad")]);throw new Exception("Invalid paste accepted");}catch(ArgumentException){}Assert(session.Document.Tasks.GetByUid(a).Name==originalA,"Invalid paste leaves project unchanged");
        int inserted=session.AddTask("Inserted before Build",beforeUid:b);Assert(session.Tasks.FindIndex(t=>t.Uid==inserted)<session.Tasks.FindIndex(t=>t.Uid==b),"Insert before selected row");session.Undo();
        session.EditText(b,"Predecessors","1SS+2d");Assert(session.Document!.Dependencies.Single(l=>l.Successor.Uid==b).Type==ProjectDependencyType.StartToStart,"Inline predecessor type");session.Undo();
        session.EditText(b,"ResourceNames","Engineer[50%]");Assert(session.Document!.Assignments.Single(x=>x.Task?.Uid==b).Units?.Value==.5m,"Inline resource assignment");session.Undo();
        session.EditResourceText(resource,"Rate","65/h");Assert(session.Document!.Resources.GetByUid(resource).StandardRate==65,"Inline resource rate");session.Undo();
        session.SetCalendarException(new(session.Document!.Calendar!.Uid,"Monday holiday",new DateTime(2026,10,19),new DateTime(2026,10,19),[],false));session.Recalculate(true);
        Assert(session.Document!.Tasks.GetByUid(b).Start==new DateTime(2026,10,20,8,0,0),"Holiday moves successor to Tuesday");
        session.SaveCopy(Path.Combine(folder,"calendar-holiday.mpp"));
        File.WriteAllText(Path.Combine(folder,"core-expected.json"),JsonSerializer.Serialize(expected,new JsonSerializerOptions{WriteIndented=true}));
        ValidateProgressMatrix(folder);
        MppFidelityValidation.Run(folder);
        Console.WriteLine("PASS: core authoring, FS weekend scheduling, resource work/cost, baseline 0/10, progress, native save/reopen, structural undo/redo, outline, cycle rejection.");
        return 0;
    }
    private static void Assert(bool condition,string message){if(!condition)throw new Exception("Core validation failed: "+message);}
    private static void ValidateProgressMatrix(string folder) {
        var rows=new List<object>();
        foreach(int days in new[]{1,2}) foreach(int units in new[]{50,100}) foreach(int percent in new[]{0,25,50,100}) {
            using var s=new ProjectSession();s.NewProject("Progress matrix",new DateTime(2026,10,16,8,0,0));
            int task=s.AddTask("Uniform work");var edit=TaskEdit.From(s.Document!.Tasks.GetByUid(task));
            s.EditTask(task,edit with {Duration=ProjectDuration.WorkingDays(days)});
            int resource=s.AddResource("Engineer",ProjectResourceType.Work,100,50);
            s.Assign(task,resource,units,days*8m*units/100m);s.Recalculate();s.SetProgress([task],percent);s.Recalculate(true);
            var t=s.Document!.Tasks.GetByUid(task);var a=s.Document.Assignments.Single();
            string file=$"progress-{days}d-{units}units-{percent}pct.mpp";s.SaveCopy(Path.Combine(folder,file));
            rows.Add(new {file,start=t.Start!.Value.ToString("yyyy-MM-ddTHH:mm:ss"),finish=t.Finish!.Value.ToString("yyyy-MM-ddTHH:mm:ss"),durationMinutes=days*480,percentComplete=percent,workMinutes=t.Work!.Value.Minutes,actualWorkMinutes=t.ActualWork!.Value.Minutes,cost=t.Cost,actualCost=t.ActualCost,assignmentWork=a.Work!.Value.Minutes,assignmentRemaining=a.RemainingWork!.Value.Minutes});
        }
        File.WriteAllText(Path.Combine(folder,"progress-matrix-expected.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
    }
    private static object Snapshot(ProjectDocument d)=>new{
        tasks=d.AllTasks.Select(t=>new{uid=t.Uid,name=t.Name,start=t.Start?.ToString("yyyy-MM-ddTHH:mm:ss"),finish=t.Finish?.ToString("yyyy-MM-ddTHH:mm:ss"),workMinutes=t.Work?.Minutes,cost=t.Cost,progress=t.PercentComplete,baselines=t.Baselines.Select(b=>new{number=b.Number,start=b.Start,finish=b.Finish,workMinutes=b.Work?.Minutes,cost=b.Cost}).ToArray()}).ToArray(),
        resources=d.Resources.Select(r=>new{uid=r.Uid,name=r.Name,workMinutes=r.Work?.Minutes,cost=r.Cost}).ToArray(),
        assignments=d.Assignments.Select(a=>new{uid=a.Uid,taskUid=a.Task?.Uid,resourceUid=a.Resource?.Uid,units=a.Units?.Value,workMinutes=a.Work?.Minutes,cost=a.Cost}).ToArray()
    };
}

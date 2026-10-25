using OfficeIMO.Project;

namespace ProjectDesk;

/// <summary>Native MPP fidelity regressions first exposed by the public corpus.</summary>
internal static class MppFidelityValidation {
    public static int Run(string folder) {
        int checks=0;
        void Check(bool condition,string label){if(!condition)throw new Exception("MPP fidelity check failed: "+label);checks++;}
        string corpus=FindCorpus();
        // Baseline fixed cost: Project stores it in a dedicated task field (MPXJ FieldMap9/12/14 BASELINE_FIXED_COST and BASELINE<N>_FIXED_COST).
        using(var s=new ProjectSession()){
            s.NewProject("Baseline fixed cost",new DateTime(2026,10,19,8,0,0));int t=s.AddTask("Fixed cost task");
            s.EditTask(t,TaskEdit.From(s.Document!.Tasks.GetByUid(t)) with {FixedCost=250m});
            s.CaptureBaseline(0);s.CaptureBaseline(10);
            string dest=Path.Combine(folder,"baseline-fixed-cost.mpp");s.SaveCopy(dest,true);
            using var reopened=ProjectDocument.Load(dest);var task=reopened.Tasks.GetByUid(t);
            Check(task.Baselines.Single(b=>b.Number==0).FixedCost==250m,"Baseline 0 fixed cost survives a native round trip");
            Check(task.Baselines.Single(b=>b.Number==10).FixedCost==250m,"Baseline 10 fixed cost survives a native round trip");
        }
        foreach(string name in new[]{"mpp14task.mpp","mpp12task.mpp","mpp9task.mpp"}){
            using var s=new ProjectSession();s.Open(Path.Combine(corpus,name));var generation=s.Document!.NativeInfo!.Generation;int number=generation=="MPP9"?0:10;
            var task=s.Document.Tasks.GetByUid(1);Check(task.FixedCost>0,name+": task 1 carries a nonzero fixed cost");
            s.CaptureBaseline(number,true);string dest=Path.Combine(folder,Path.GetFileNameWithoutExtension(name)+"-baseline.mpp");
            var report=s.AssessSave(dest);Check(!report.HasErrors,name+": captured baseline fixed cost has a native writer");
            s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);
            Check(reopened.Tasks.GetByUid(1).Baselines.Single(b=>b.Number==number).FixedCost==task.FixedCost,name+": captured baseline fixed cost reloads from "+generation);
        }
        // Manual task dates: MPP14 stores Start/Finish in 0x0b400503/0x0b400504 and the scheduled dates in 0x0b400023/0x0b400024 (MPXJ FieldMap14).
        using(var s=new ProjectSession()){
            s.Open(Path.Combine(corpus,"assignment-assignments-project2019-mpp14.mpp"));var task=s.Document!.Tasks.GetByUid(1);
            Check(task.IsManual==true&&task.Start==new DateTime(2016,1,4,8,0,0)&&task.Finish==new DateTime(2016,1,15,17,0,0),"Manual task reads the Start/Finish that Project exported to MSPDI");
            var manualStart=new DateTime(2016,2,1,8,0,0);var manualFinish=new DateTime(2016,2,12,17,0,0);
            s.Execute("Move manual task",d=>{var t=d.Tasks.GetByUid(1);t.Start=manualStart;t.Finish=manualFinish;});
            string dest=Path.Combine(folder,"manual-dates.mpp");s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);
            Check(reopened.Tasks.GetByUid(1).Start==manualStart&&reopened.Tasks.GetByUid(1).Finish==manualFinish,"Edited manual dates survive a native round trip");
            Check(reopened.Tasks.GetByUid(2).Start==new DateTime(2016,1,4,8,0,0),"Untouched manual task keeps its producer dates");
        }
        // Switching mode without moving the task changes which date pair is displayed, so both pairs must carry the model dates.
        using(var s=new ProjectSession()){
            s.Open(Path.Combine(corpus,"assignment-assignments-project2019-mpp14.mpp"));var before=s.Document!.Tasks.GetByUid(1);var start=before.Start;var finish=before.Finish;
            s.Execute("Switch to automatic without moving",d=>d.Tasks.GetByUid(1).IsManual=false);
            string dest=Path.Combine(folder,"manual-to-automatic.mpp");s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);var task=reopened.Tasks.GetByUid(1);
            Check(task.IsManual==false&&task.Start==start&&task.Finish==finish,"Manual-to-automatic switch keeps the task's dates on reload");
        }
        using(var s=new ProjectSession()){
            s.NewProject("Manual dates",new DateTime(2026,10,19,8,0,0));int t=s.AddTask("Manual",calculateDates:false);var start=new DateTime(2016,1,4,8,0,0);
            s.Execute("Author manual dates",d=>{var task=d.Tasks.GetByUid(t);task.IsManual=true;task.Start=start;task.Finish=start.AddDays(11).AddHours(9);});
            string dest=Path.Combine(folder,"manual-authored.mpp");s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);
            Check(reopened.Tasks.GetByUid(t).IsManual==true&&reopened.Tasks.GetByUid(t).Start==start&&reopened.Tasks.GetByUid(t).Finish==start.AddDays(11).AddHours(9),"Authored manual dates survive a new MPP14 file");
        }
        // Preserved producer values: Project 2010 wrote baselines whose start is later than their finish; unchanged records must not block unrelated edits.
        using(var s=new ProjectSession()){
            s.Open(Path.Combine(corpus,"mpp14baselines.mpp"));var baseline=s.Document!.Tasks.GetByUid(1).Baselines.Single(b=>b.Number==1);
            Check(baseline.Start==new DateTime(2001,1,1,9,0,0)&&baseline.Finish==new DateTime(2000,1,1,17,30,0),"Producer baseline keeps its inverted dates");
            s.Rename(1,"Renamed task");string dest=Path.Combine(folder,"inverted-baseline-rename.mpp");var report=s.AssessSave(dest);
            Check(!report.HasErrors&&report.Diagnostics.Any(d=>d.Code=="PROJECT_DATE_RANGE"&&d.Severity==ProjectDiagnosticSeverity.Warning&&d.Location=="/Task[UID=1]/Baseline"),"Unchanged inverted producer baseline is reported, not blocking");
            s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);var reloaded=reopened.Tasks.GetByUid(1).Baselines.Single(b=>b.Number==1);
            Check(reloaded.Start==baseline.Start&&reloaded.Finish==baseline.Finish&&reopened.Tasks.GetByUid(1).Name=="Renamed task","Inverted producer baseline survives the edited save unchanged");
            s.Execute("Author an inverted baseline",d=>{var b=d.Tasks.GetByUid(1).Baselines.Single(x=>x.Number==1);b.Start=b.Start!.Value.AddDays(1);});
            var authored=s.AssessSave(Path.Combine(folder,"inverted-baseline-authored.mpp"));
            Check(authored.Diagnostics.Any(d=>d.Code=="PROJECT_DATE_RANGE"&&d.Severity==ProjectDiagnosticSeverity.Error),"Authored inverted baseline dates still block saving");
        }
        using(var s=new ProjectSession()){
            s.Open(Path.Combine(corpus,"mpp14assignmentfields.mpp"));var calendar=s.Document!.Calendars.GetByUid(4);
            Check(calendar.IsBaseCalendar==false&&calendar.BaseCalendar==null,"Producer resource calendar without a resolvable base is retained");
            s.Rename(1,"Renamed task");string dest=Path.Combine(folder,"baseless-calendar-rename.mpp");var report=s.AssessSave(dest);
            Check(!report.HasErrors&&report.Diagnostics.Any(d=>d.Code=="PROJECT_CALENDAR_REFERENCE"&&d.Severity==ProjectDiagnosticSeverity.Warning&&d.Location=="/Calendar[UID=4]"),"Unchanged base-less producer calendar is reported, not blocking");
            s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);
            Check(reopened.Calendars.GetByUid(4).BaseCalendar==null&&reopened.Resources.GetByUid(2).Calendar?.Uid==4,"Base-less producer calendar survives the edited save unchanged");
            s.SetWorkingWeek(4,new Dictionary<DayOfWeek,ProjectWorkingTime[]>{[DayOfWeek.Monday]=Array.Empty<ProjectWorkingTime>()});
            var authored=s.AssessSave(Path.Combine(folder,"baseless-calendar-authored.mpp"));
            Check(authored.Diagnostics.Any(d=>d.Code=="PROJECT_CALENDAR_REFERENCE"&&d.Severity==ProjectDiagnosticSeverity.Error),"Editing a base-less calendar still blocks saving");
        }
        Console.WriteLine($"PASS: {checks} MPP fidelity checks (baseline fixed cost, manual dates, preserved producer values).");return checks;
    }
    private static string FindCorpus() {
        for(var directory=new DirectoryInfo(AppContext.BaseDirectory);directory!=null;directory=directory.Parent){
            string candidate=Path.Combine(directory.FullName,"examples","public-mpp");
            if(File.Exists(Path.Combine(candidate,"manifest.json")))return candidate;
        }
        throw new Exception("MPP fidelity check failed: examples/public-mpp corpus not found above "+AppContext.BaseDirectory);
    }
}

using OfficeIMO.Project;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProjectDesk;

public static class PublicMppValidation {
    public static int Run(string corpus,string output) {
        Directory.CreateDirectory(output);var results=new List<object>();int failures=0,checks=0;
        foreach(var file in Directory.GetFiles(corpus,"*.mpp").OrderBy(Path.GetFileName)) {
            var stages=new List<object>();var original=SHA256.HashData(File.ReadAllBytes(file));
            bool Stage(string name,Action work){try{work();stages.Add(new{stage=name,status="pass"});checks++;return true;}catch(Exception e){stages.Add(new{stage=name,status="fail",error=e.ToString()});failures++;return false;}}
            using var session=new ProjectSession();string stem=Path.GetFileNameWithoutExtension(file);var folder=Path.Combine(output,stem);Directory.CreateDirectory(folder);
            object? coverage=null;
            if(Stage("import",()=>session.Open(file))) {
                var d=session.Document!;string? generation=d.NativeInfo?.Generation;coverage=new{generation,producer=d.NativeInfo?.ProducerVersion,tasks=session.Tasks.Count,resources=session.Resources.Count,assignments=d.Assignments.Count,links=d.Dependencies.Count,calendars=d.Calendars.Count,taskBaselines=d.AllTasks.Sum(t=>t.Baselines.Count)};
                string snapshot=Snapshot(d);File.WriteAllText(Path.Combine(folder,"import.json"),snapshot);
                Stage("unchanged-native-save",()=>{var dest=Path.Combine(folder,"unchanged.mpp");session.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);Equal(snapshot,Snapshot(reopened),"Mapped data changed in unchanged native round trip");if(!original.SequenceEqual(SHA256.HashData(File.ReadAllBytes(dest))))throw new Exception("Unchanged output did not preserve all source bytes");});
                var xml=Path.Combine(corpus,stem.Replace("-mpp14","-mspdi")+".xml");
                if(File.Exists(xml))Stage("independent-producer-xml",()=>{using var expected=ProjectDocument.Load(xml);var actual=ProducerSnapshot(d);var wanted=ProducerSnapshot(expected);File.WriteAllText(Path.Combine(folder,"producer-actual.json"),actual);File.WriteAllText(Path.Combine(folder,"producer-expected.json"),wanted);Equal(wanted,actual,"MPP data differs from repository's matching producer XML");});
                var task=session.Tasks.FirstOrDefault(t=>!t.IsSummary);
                if(task!=null)Stage("rename-undo-redo-native-save",()=>{int uid=task.Uid;string? old=task.Name;session.Rename(uid,"Public compatibility task");session.Undo();if(session.Document!.Tasks.GetByUid(uid).Name!=old)throw new Exception("Undo lost original name");session.Redo();var dest=Path.Combine(folder,"renamed.mpp");session.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);if(reopened.Tasks.GetByUid(uid).Name!="Public compatibility task")throw new Exception("Rename did not persist");if(reopened.NativeInfo?.Generation!=generation)throw new Exception("Native generation changed");session.Undo();});
                session.Open(file);
                Stage("new-task-link-resource-native-save",()=>{
                    // Use manual explicit dates to test file authoring independently of an imported plan's scheduling profile.
                    int a=session.AddTask("Public added task A",calculateDates:false),b=session.AddTask("Public added task B",calculateDates:false);
                    var start=new DateTime(2026,10,19,8,0,0);bool manual=generation=="MPP14";
                    session.Execute("Set explicit authored dates",doc=>{var first=doc.Tasks.GetByUid(a);first.IsManual=manual;first.Start=start;first.Finish=start.AddHours(9);var second=doc.Tasks.GetByUid(b);second.IsManual=manual;second.Start=start.AddDays(1);second.Finish=start.AddDays(1).AddHours(9);});
                    string? currency=session.Document!.Settings.CurrencyCode;
                    session.LinkTasks(a,b,ProjectDependencyType.FinishToStart,ProjectDuration.WorkingDays(1));session.EditText(b,"Duration","2d");
                    if(session.Document!.Settings.CurrencyCode!=currency)throw new Exception("Automatic link/duration dates changed the source currency");
                    int r=session.AddResource("Public added engineer",ProjectResourceType.Work,50,45);session.Assign(a,r,50,4);
                    string expected=Snapshot(session.Document!);var dest=Path.Combine(folder,"authored.mpp");session.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);Equal(expected,Snapshot(reopened),"Authored native mapped data did not persist");
                    if(!reopened.AllTasks.Any(t=>t.Uid==a&&t.Name=="Public added task A")||!reopened.Dependencies.Any(l=>l.Predecessor?.Uid==a&&l.Successor?.Uid==b)||!reopened.Assignments.Any(x=>x.Task?.Uid==a&&x.Resource?.Uid==r))throw new Exception("New task, link or assignment missing");
                    if(reopened.NativeInfo?.Generation!=generation)throw new Exception("Native generation changed");
                    if(reopened.Settings.CurrencyCode!=currency)throw new Exception("Date-only projection currency leaked into native output");
                    // Deleting a resource must keep visible IDs contiguous while undo restores identities and assignments.
                    string before=Snapshot(session.Document!);session.DeleteResource(r);session.Undo();Equal(before,Snapshot(session.Document!),"Resource deletion undo lost data");session.Redo();var deleted=Path.Combine(folder,"resource-deleted.mpp");session.SaveCopy(deleted,true);using var afterDelete=ProjectDocument.Load(deleted);if(afterDelete.Resources.Any(x=>x.Uid==r)||afterDelete.Assignments.Any(x=>x.Resource?.Uid==r))throw new Exception("Deleted resource or its assignment persisted");
                });
                session.Open(file);
                int baseline=generation is "MPP8" or "MPP9"?0:10;
                Stage($"baseline-{baseline}-capture-native-save",()=>{session.CaptureBaseline(baseline,true);string expected=Snapshot(session.Document!);var dest=Path.Combine(folder,"baseline-captured.mpp");session.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);Equal(expected,Snapshot(reopened),"Captured baseline data did not persist");});
                File.WriteAllText(Path.Combine(folder,"save-diagnostics.json"),JsonSerializer.Serialize(session.AssessSave(Path.Combine(folder,"assessment.mpp")).Diagnostics.Select(x=>new{x.Code,severity=x.Severity.ToString(),x.Message,x.Location,x.RepresentsLoss}),Options));
            }
            Stage("original-preserved",()=>{if(!original.SequenceEqual(SHA256.HashData(File.ReadAllBytes(file))))throw new Exception("Fixture original changed");});
            results.Add(new{file=Path.GetFileName(file),sha256=Convert.ToHexString(original),coverage,stages});
        }
        File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{checks,failures,results,qualification="Standalone import/native mapped-data self-round-trip plus matching independent producer XML where available; no Microsoft Project reopen and no universal compatibility claim."},Options));
        Console.WriteLine($"Public MPP corpus: {checks} passed stages, {failures} failed stages. See {Path.Combine(output,"results.json")}");return failures==0?0:1;
    }
    private static void Equal(string expected,string actual,string message){if(expected!=actual)throw new Exception(message);}
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true};
    private static string Snapshot(ProjectDocument d)=>JsonSerializer.Serialize(new{
        tasks=d.AllTasks.Where(t=>t.IsNull!=true).OrderBy(t=>t.Uid).Select(t=>new{t.Uid,t.Guid,t.Name,parent=t.Parent?.Uid,t.Start,t.Finish,t.Duration,t.Work,t.Cost,t.PercentComplete,t.IsManual,t.IsMilestone,baselines=t.Baselines.OrderBy(b=>b.Number).Select(b=>new{b.Number,b.Start,b.Finish,b.Duration,b.Work,b.Cost})}),
        resources=d.Resources.Where(r=>r.IsNull!=true).OrderBy(r=>r.Uid).Select(r=>new{r.Uid,r.Guid,r.Name,r.Type,r.MaxUnits,r.StandardRate,r.OvertimeRate,r.CostPerUse,r.Work,r.Cost}),
        assignments=d.Assignments.OrderBy(a=>a.Uid).Select(a=>new{a.Uid,task=a.Task?.Uid,resource=a.Resource?.Uid,a.Units,a.Start,a.Finish,a.Work,a.Cost}),
        links=d.Dependencies.OrderBy(l=>l.Successor?.Uid).ThenBy(l=>l.Predecessor?.Uid).Select(l=>new{from=l.Predecessor?.Uid,to=l.Successor?.Uid,l.Type,l.Lag}),calendars=d.Calendars.OrderBy(c=>c.Uid).Select(c=>new{c.Uid,c.Name,c.IsBaseCalendar,parent=c.BaseCalendar?.Uid})},Options);
    // Independent producer comparison intentionally covers fields whose native and MSPDI representations have the same contract.
    private static string ProducerSnapshot(ProjectDocument d)=>JsonSerializer.Serialize(new{
        tasks=d.AllTasks.Where(t=>t.Uid!=0&&t.IsNull!=true).OrderBy(t=>t.Uid).Select(t=>new{t.Uid,t.Name,t.Start,t.Finish,baselines=t.Baselines.OrderBy(b=>b.Number??0).Select(b=>new{number=b.Number??0,b.Start,b.Finish})}),
        resources=d.Resources.Where(r=>r.Uid!=0&&r.IsNull!=true).OrderBy(r=>r.Uid).Select(r=>new{r.Uid,r.Name}),
        assignments=d.Assignments.Where(a=>a.Task?.Uid>0&&a.Resource?.Uid>0).OrderBy(a=>a.Task!.Uid).ThenBy(a=>a.Resource!.Uid).Select(a=>new{task=a.Task!.Uid,resource=a.Resource!.Uid,a.Units,a.Start,a.Finish}),
        links=d.Dependencies.OrderBy(l=>l.Successor?.Uid).ThenBy(l=>l.Predecessor?.Uid).Select(l=>new{from=l.Predecessor?.Uid,to=l.Successor?.Uid,l.Type})},Options);
}

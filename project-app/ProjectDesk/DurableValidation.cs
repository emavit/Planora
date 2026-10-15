using System.Text.Json;
using System.IO.Compression;
using OfficeIMO.Project;
namespace ProjectDesk;

public static class DurableValidation {
 public static int Run(string folder){
  Directory.CreateDirectory(folder);var checks=new List<string>();
  void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);}
  using var original=new ProjectSession();original.NewProject("Durable history",new DateTime(2026,10,19,8,0,0));
  int a=original.AddTask("Design"),b=original.AddTask("Build");
  original.EditCells([new(a,"Duration","3d"),new(b,"Duration","2d"),new(b,"Predecessors","1FS+1d")]);
  int resource=original.AddResource("Engineer",ProjectResourceType.Work,100,50);original.Assign(b,resource,50,8);
  original.SetWorkingWeek(original.Document!.Calendar!.Uid,new Dictionary<DayOfWeek,ProjectWorkingTime[]>{{DayOfWeek.Friday,[ProjectWorkingTime.Hours(9,13)]}});
  original.Rename(a,"Design updated");original.Undo();
  var state=original.CaptureState();var serialized=JsonSerializer.Serialize(state,DurableCommand.Json);
  using var restored=new ProjectSession();restored.RestoreState(JsonSerializer.Deserialize<DurableProjectState>(serialized,DurableCommand.Json)!);
  Check(restored.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow})==original.Document.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow}),"Full document matches after serialized replay with deferred batch scheduling");
  Check(restored.ProjectIdentity==original.ProjectIdentity,"Project identity preserved");
  Check(restored.CanRedo&&restored.HistoryPosition==original.HistoryPosition,"Redo branch and cursor preserved");
  original.Redo();restored.Redo();Check(restored.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow})==original.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow}),"Redo matches original");
  original.Undo();restored.Undo();Check(restored.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow})==original.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow}),"Undo matches original");
  Check(restored.Tasks.Single(t=>t.Uid==a).Duration?.Value==3,"Duration survives readonly struct serialization");
  var friday=restored.Document.Calendar!.WeekDays.Single(d=>d.Day==DayOfWeek.Friday).WorkingTimes.Single();
  Check(friday.From==TimeSpan.FromHours(9)&&friday.To==TimeSpan.FromHours(13),"Working intervals survive serialization");
  var lag=new ProjectDuration(-2.5m,ProjectDurationUnit.Hour,true,true);var lagCopy=JsonSerializer.Deserialize<ProjectDuration>(JsonSerializer.Serialize(lag,DurableCommand.Json),DurableCommand.Json);
  Check(lag.Equals(lagCopy),"Negative elapsed estimated lag survives serialization");
  string before=restored.Document.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow});
  try{restored.RestoreState(state with{History=[new("invalid",DurableCommand.For("Unknown"))],Position=1});throw new Exception("Unknown command accepted");}catch(InvalidDataException){}
  Check(restored.Document.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow})==before,"Invalid restore leaves live project unchanged");
  var nested=DurableCommand.For("Rename",a,"Nested");for(int i=0;i<18;i++)nested=DurableCommand.Batch([nested]);
  try{restored.RestoreState(state with{History=[new("nested",nested)],Position=1});throw new Exception("Excessive nesting accepted");}catch(InvalidDataException){}
  Check(restored.Document.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow})==before,"Excessive nesting rejected atomically");
  var package=Path.Combine(folder,Guid.NewGuid()+".planora");PlanoraPackage.WriteNew(package,original);
  using var reopened=new ProjectSession();var packaged=PlanoraPackage.Read(package);reopened.RestoreState(packaged);
  Check(packaged.BaseBytes.SequenceEqual(state.BaseBytes),"Package preserves exact original bytes");
  Check(reopened.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow})==original.Document!.ToXml(new ProjectSaveOptions{LossPolicy=OfficeIMO.OfficeConversionLossPolicy.Allow}),"Portable package model round trip");
  byte[] existing=File.ReadAllBytes(package);try{PlanoraPackage.WriteNew(package,original);throw new Exception("Existing package overwritten");}catch(IOException){}
  Check(File.ReadAllBytes(package).SequenceEqual(existing),"Existing package protected");
  using(var zip=ZipFile.Open(package,ZipArchiveMode.Update)){zip.GetEntry("base.bin")!.Delete();using var stream=zip.CreateEntry("base.bin").Open();stream.Write([1,2,3]);}
  try{PlanoraPackage.Read(package);throw new Exception("Corruption accepted");}catch(InvalidDataException){}
  Check(true,"Corrupt package rejected");File.Delete(package);
  var recovery=new RecoveryStore(Path.Combine(folder,"recovery"));Guid slot=Guid.NewGuid(),otherSlot=Guid.NewGuid();
  recovery.Checkpoint(slot,original);string prior=original.Tasks.Single(t=>t.Uid==a).Name!;original.Rename(a,"Checkpoint two");recovery.Checkpoint(slot,original);
  recovery.Recover(slot,reopened);Check(reopened.Tasks.Single(t=>t.Uid==a).Name=="Checkpoint two","Latest recovery generation restores");
  recovery.Recover(slot,reopened,true);Check(reopened.Tasks.Single(t=>t.Uid==a).Name==prior,"Previous recovery generation restores");
  recovery.Checkpoint(otherSlot,restored);Check(recovery.Sessions().Contains(slot)&&recovery.Sessions().Contains(otherSlot),"Independent sessions retain separate recovery slots");
  recovery.Discard(slot);Check(!recovery.Sessions().Contains(slot)&&recovery.Sessions().Contains(otherSlot),"Discard affects only chosen session");recovery.Discard(otherSlot);
  using var savedSession=new ProjectSession();savedSession.NewProject("Saved cursor",DateTime.Today.AddHours(8));int savedTask=savedSession.AddTask("Saved name");
  Check(savedSession.Document!.Settings.StartDate?.Kind==DateTimeKind.Unspecified,"New project accepts the UI's local date without timezone conversion");
  string savedCopy=Path.Combine(folder,Guid.NewGuid()+".mpp");savedSession.SaveCopy(savedCopy);savedSession.Rename(savedTask,"Later name");
  using var savedRestored=new ProjectSession();savedRestored.RestoreState(JsonSerializer.Deserialize<DurableProjectState>(JsonSerializer.Serialize(savedSession.CaptureState(),DurableCommand.Json),DurableCommand.Json)!);
  Check(savedRestored.HasEdits,"Recovered changes after saved cursor stay dirty");savedRestored.Undo();Check(!savedRestored.HasEdits,"Undo to saved ancestor after recovery becomes clean");savedRestored.Rename(savedTask,"Branch name");Check(savedRestored.HasEdits,"Edit on another history branch stays dirty");File.Delete(savedCopy);
  File.WriteAllText(Path.Combine(folder,"durable-validation.json"),JsonSerializer.Serialize(new{checks,Count=checks.Count},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Durable validation: {checks.Count} checks passed");return 0;
 }
}

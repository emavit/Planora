namespace ProjectDesk;

// Independent per-process session slots prevent one app instance from replacing another's recovery.
// The caller owns user consent and must surface write failures; this class never discards silently.
public sealed class RecoveryStore {
 private readonly string _directory;
 public RecoveryStore(string directory){_directory=Path.GetFullPath(directory);Directory.CreateDirectory(_directory);}
 private string Slot(Guid id){if(id==Guid.Empty)throw new ArgumentException("A recovery session needs an identity.");return Path.Combine(_directory,id.ToString("N")+".planora");}
 public void Checkpoint(Guid sessionId,ProjectSession session)=>Checkpoint(sessionId,session.CaptureState());
 public void Checkpoint(Guid sessionId,DurableProjectState session){
  string current=Slot(sessionId),pending=current+"."+Guid.NewGuid().ToString("N")+".pending",previous=current+".previous";
  try{PlanoraPackage.WriteNew(pending,session);if(File.Exists(current))File.Replace(pending,current,previous);else File.Move(pending,current,false);}
  finally{if(File.Exists(pending))File.Delete(pending);}
 }
 public IReadOnlyList<Guid> Sessions()=>Directory.EnumerateFiles(_directory,"*.planora").Select(Path.GetFileNameWithoutExtension).Select(name=>Guid.TryParseExact(name,"N",out var id)?id:Guid.Empty).Where(id=>id!=Guid.Empty).OrderBy(id=>id).ToArray();
 public void Recover(Guid sessionId,ProjectSession destination,bool previousGeneration=false){var state=PlanoraPackage.Read(Slot(sessionId)+(previousGeneration?".previous":""));destination.RestoreState(state);}
 public DurableProjectState Read(Guid sessionId,bool previousGeneration=false)=>PlanoraPackage.Read(Slot(sessionId)+(previousGeneration?".previous":""));
 public DateTime Modified(Guid sessionId)=>File.GetLastWriteTime(Slot(sessionId));
 public void Discard(Guid sessionId){string current=Slot(sessionId);File.Delete(current);File.Delete(current+".previous");}
}

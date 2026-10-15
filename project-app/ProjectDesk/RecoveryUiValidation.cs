using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text.Json;
namespace ProjectDesk;
internal static class RecoveryUiValidation {
 public static int Run(string folder){
  Directory.CreateDirectory(folder);AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();var checks=new List<string>();
  void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
  void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);Dispatcher.UIThread.RunJobs();}
  void Until(Func<bool> condition){var limit=DateTime.UtcNow.AddSeconds(20);while(!condition()&&DateTime.UtcNow<limit){Pump();Thread.Sleep(10);}Check(condition(),"Asynchronous UI operation completed");Pump();}
  using var session=new ProjectSession();session.NewProject("Recovery UI",new DateTime(2026,10,19,8,0,0));int a=session.AddTask("Design");int b=session.AddTask("Build");session.EditText(b,"Predecessors","1FS");
  var window=new PlannerWindow(session,Path.Combine(folder,"preferences.json"));window.Show();Pump();
  window.Formatting.Edit(session.HistoryPosition,d=>d.Cells[FormattingStore.CellKey("Task",a,"Name")]=new(){Bold=true,Foreground="#AA3344"});
  string second=window.Timeline.AddTimeline("Release");window.Timeline.SetSelection([b],true);window.Timeline.SetAppearance(second,new TimelineStyle(BarColor:"#123456"));window.Timeline.Axis.SetZoom(3);window.Axis.SetZoom(2);
  var snapshot=window.CaptureRecoveryState();var path=Path.Combine(folder,"presentation-"+Guid.NewGuid()+".planora");PlanoraPackage.WriteNew(path,snapshot);
  using var other=new ProjectSession();other.NewProject("Different",DateTime.Today);var restored=new PlannerWindow(other,Path.Combine(folder,"restored.json"));restored.Show();Pump();
  restored.RestoreRecoveryState(PlanoraPackage.Read(path) with{SourcePath=Path.Combine(folder,"missing-original.mpp")});Pump();
  Check(other.Tasks.Count==2&&other.Tasks.Single(t=>t.Uid==b).Start==session.Tasks.Single(t=>t.Uid==b).Start,"Recovery restores dependency dates without original source file");
  Check(restored.Formatting.Get("Task",a,"Name").Bold==true,"Formatting restored");
  Check(restored.Timeline.Lanes.Count==2&&restored.Timeline.ActiveId==second,"Timeline lanes and active lane restored");
  Check(restored.Timeline.Lanes.Single(l=>l.Id==second).TaskUids.SequenceEqual(new[]{b}),"Timeline task placement restored");
  Check(restored.Timeline.Lanes.Single(l=>l.Id==second).Style!.BarColor!.EndsWith("123456"),"Timeline colors restored");
  Check(restored.Timeline.Axis.Zoom==3&&restored.Axis.Zoom==2,"Independent timeline/Gantt zoom restored");
  Check(other.CanUndo&&other.HasEdits,"Recovered history remains editable and unsaved");
  string portable=Path.Combine(folder,"saved-"+Guid.NewGuid()+".planora");var saving=restored.SavePlanoraCopy(portable);Until(()=>saving.IsCompleted);saving.GetAwaiter().GetResult();Check(!other.HasEdits&&!restored.Formatting.HasChanges,"Saving a portable copy marks captured model and formatting saved");
  restored.OpenPlanoraCopy(portable);Pump();Check(!other.HasEdits&&restored.Timeline.Lanes.Count==2,"Opening portable copy retains presentation and clean state");
  restored.Timeline.RenameTimeline(second,"Edited release lane");restored.Close();Pump();Check(restored.ActiveSavePrompt!=null,"Timeline-only changes in portable project prompt before close");restored.ActiveSavePrompt!.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Cancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
  var invalid=snapshot with{Presentation=snapshot.Presentation! with{Gantt=new DateAxisView(Zoom:double.NaN)}};
  try{restored.RestoreRecoveryState(invalid);throw new Exception("Invalid presentation accepted");}catch(ArgumentException){}
  Check(other.Tasks.Count==2&&restored.Axis.Zoom==2,"Invalid presentation leaves live project unchanged");
  void Menu(string name){window.GetVisualDescendants().OfType<Menu>().Single().Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"File")).Items.OfType<MenuItem>().Single(m=>Equals(m.Header,name)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Pump();}
  Menu("Create Recovery Checkpoint");Until(()=>window.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Name=="RecoveryStatus"&&t.Text?.StartsWith("Recovery checkpoint saved")==true));
  var store=new RecoveryStore(Path.Combine(folder,"recovery"));Check(store.Sessions().Count==1,"Manual UI checkpoint persisted");
  Menu("Recovery…");if(!window.OwnedWindows.Any())throw new Exception(string.Join(" | ",window.GetVisualDescendants().OfType<TextBlock>().Select(t=>t.Text).Where(t=>t?.StartsWith("Could not")==true)));var dialog=window.OwnedWindows.Single(w=>w.Title?.StartsWith("Project Recovery")==true);Pump();Check(dialog.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="RecoverySessions").ItemCount==1,"Recovery chooser lists retained checkpoint");
  dialog.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Cancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(session.Tasks.Count==2,"Cancel recovery preserves live project");
  using(var frame=restored.CaptureRenderedFrame()!)frame.Save(Path.Combine(folder,"recovered-window.png"));
  session.Rename(a,"Automatically checkpointed");window.RefreshForValidation();window.StartRecoveryForValidation();
  Until(()=>{try{using var probe=new ProjectSession();store.Recover(store.Sessions().Single(),probe);return probe.Tasks.Single(t=>t.Uid==a).Name=="Automatically checkpointed";}catch(IOException){return false;}});
  Check(true,"Automatic timer persists later edits");
  window.Close();Pump();window.ActiveSavePrompt!.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Don't Save")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Until(()=>!window.IsVisible);
  Check(store.Sessions().Count==0,"Don't Save removes this session's retained checkpoint");restored.CloseForValidation();File.Delete(path);File.Delete(portable);
  File.WriteAllText(Path.Combine(folder,"recovery-ui.json"),JsonSerializer.Serialize(new{Count=checks.Count,checks},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS: {checks.Count} recovery UI checks");return 0;
 }
}

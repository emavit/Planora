using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
namespace ProjectDesk;
internal static class CloseFlowValidation {
 public static int Run(string output){Directory.CreateDirectory(output);AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();int checks=0;void Assert(bool ok,string why){if(!ok)throw new Exception(why);checks++;}void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();}
  using var session=new ProjectSession();session.NewProject("Close workflow",new DateTime(2026,10,16,8,0,0));int uid=session.AddTask("Unsaved task");var owner=new PlannerWindow(session,Path.Combine(output,"save-preferences.json"));owner.Show();Pump();bool closed=false;owner.Closed+=(_,_)=>closed=true;owner.Formatting.Edit(session.HistoryPosition,d=>d.Cells[FormattingStore.CellKey("Task",uid,"Name")]=new(){Bold=true});
  void Request(){owner.Close();Pump();Assert(owner.ActiveSavePrompt!=null,"Dirty project did not show Save/Don't Save/Cancel");Assert(!closed,"Dirty window closed before a choice");}
  void Click(string text){owner.ActiveSavePrompt!.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,text)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();}
  Request();Assert(owner.ActiveSavePrompt!.GetVisualDescendants().OfType<Button>().Count(b=>b.Content is "Save" or "Don't Save" or "Cancel")==3,"Close prompt lacks three choices");using(var frame=owner.ActiveSavePrompt!.CaptureRenderedFrame()!)frame.Save(Path.Combine(output,"save-dont-save-cancel.png"));Click("Cancel");Assert(!closed&&session.HasEdits&&owner.Formatting.HasChanges,"Cancel discarded edits or closed window");
  Request();owner.ActiveSavePrompt!.Close();Pump();Assert(!closed&&session.HasEdits,"Closing dialog X did not cancel");
  owner.SaveForValidation=()=>Task.FromResult(false);Request();Click("Save");Assert(!closed&&session.HasEdits,"Cancelled Save picker closed window");
  owner.SaveForValidation=()=>throw new IOException("Simulated save failure");Request();Click("Save");Assert(!closed&&session.HasEdits,"Failed save closed window");
  string saved=Path.Combine(output,"close-saved-"+Guid.NewGuid()+".mpp");owner.SaveForValidation=()=>{session.SaveCopy(saved);owner.Formatting.Export(saved);return Task.FromResult(true);};Request();Click("Save");Assert(closed&&File.Exists(saved)&&File.Exists(saved+".projectdesk.json"),"Successful Save did not write files before closing");Assert(!session.HasEdits&&!owner.Formatting.HasChanges,"Successful save did not mark changes saved");
  using var discardSession=new ProjectSession();discardSession.NewProject("Discard test",new DateTime(2026,10,16,8,0,0));int discardUid=discardSession.AddTask("Discard task");var discard=new PlannerWindow(discardSession,Path.Combine(output,"discard-preferences.json"));discard.Show();Pump();discard.Formatting.Edit(discardSession.HistoryPosition,d=>d.Cells[FormattingStore.CellKey("Task",discardUid,"Name")]=new(){Italic=true});bool discardedClosed=false;discard.Closed+=(_,_)=>discardedClosed=true;discard.Close();Pump();discard.ActiveSavePrompt!.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Don't Save")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Assert(discardedClosed&&!discard.Formatting.HasChanges&&discard.Formatting.Document.Cells.Count==0,"Don't Save did not close and discard session formatting");
  using var cleanSession=new ProjectSession();cleanSession.Open(saved);var clean=new PlannerWindow(cleanSession,Path.Combine(output,"clean-preferences.json"));bool cleanClosed=false;clean.Closed+=(_,_)=>cleanClosed=true;clean.Show();Pump();clean.Close();Pump();Assert(cleanClosed&&clean.ActiveSavePrompt==null,"Clean file unexpectedly prompted");
  Console.WriteLine($"PASS: {checks} close workflow checks; Save, Don't Save, Cancel, dialog X, cancelled picker, failed save, native save+companion before close, formatting discard and clean close.");return 0;
 }
}

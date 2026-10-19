using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OfficeIMO.Project;
namespace ProjectDesk;
internal static class ResourceSheetValidation {
 public static int Run(string output){
  Directory.CreateDirectory(output);
  AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
  using var session=new ProjectSession();session.NewProject("Resource edit regression",new DateTime(2026,10,19));
  var uid=session.AddResource("Engineer",ProjectResourceType.Work,100,0);
  var task=session.AddTask("Assigned task");var other=session.AddTask("Unrelated task");
  session.EditText(task,"ResourceNames","Engineer");
  session.Execute("Synthetic long project",d=>{for(int n=0;n<1000;n++){var t=d.Tasks.Add("Background task "+n);d.Assignments.Add(t,d.Resources.GetByUid(uid));}});
  var window=new PlannerWindow(session,Path.Combine(output,Guid.NewGuid()+".json"));window.Show();
  void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);Dispatcher.UIThread.RunJobs();}
  Pump();window.SelectPreviewTab(2);Pump();
  int unrelatedRefreshes=0;
  var unrelated=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==other);
  unrelated.PropertyChanged+=(_,_)=>unrelatedRefreshes++;
  var grid=window.GetVisualDescendants().OfType<DataGrid>().Single(g=>g.IsEffectivelyVisible&&g.Columns.Any(c=>Equals(c.Tag,"Capacity")));
  var elapsed=System.Diagnostics.Stopwatch.StartNew();
  for(int i=0;i<10;i++){
   Console.WriteLine("Begin resource edit "+i);
   grid.SelectedItem=grid.ItemsSource!.OfType<SheetRecord>().Single(r=>r.Uid==uid);grid.CurrentColumn=grid.Columns.Single(c=>Equals(c.Tag,"Name"));grid.Focus();grid.BeginEdit();Pump();
   var editor=grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.IsEffectivelyVisible);
   editor.Text="Engineer "+i;
   if(!grid.CommitEdit())throw new Exception("Name commit rejected");Pump();
   if(session.Resources.Single(r=>r.Uid==uid).Name!="Engineer "+i)throw new Exception("Name not committed");
  }
  if(unrelatedRefreshes!=0)throw new Exception("Resource name edits unnecessarily rebuild unrelated task rows: "+unrelatedRefreshes);
  var assigned=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==task);
  if(assigned.Cells["ResourceNames"]!="Engineer 9")throw new Exception("Assigned task resource label is stale");
  Console.WriteLine("Ten name edits on 1002 tasks: "+elapsed.ElapsedMilliseconds+" ms");
  grid.BeginEdit();Pump();var invalid=grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.IsEffectivelyVisible);invalid.Text="";
  if(grid.CommitEdit())throw new Exception("Empty resource name accepted");
  invalid.Text="Corrected resource";if(!grid.CommitEdit())throw new Exception("Cannot correct invalid name");Pump();
  session.Undo();window.RefreshForValidation();Pump();if(session.Resources.Single(r=>r.Uid==uid).Name!="Engineer 9")throw new Exception("Resource rename Undo failed");
  session.Redo();window.RefreshForValidation();Pump();if(session.Resources.Single(r=>r.Uid==uid).Name!="Corrected resource")throw new Exception("Resource rename Redo failed");
  // Save/reopen uses a fully initialized authoring fixture; bulk rows above measure UI refresh only.
  using var saved=new ProjectSession();saved.NewProject("Resource persistence",new DateTime(2026,10,19));saved.AddTask("Delivery");
  var savedUid=saved.AddResource("Engineer",ProjectResourceType.Work,100,0);saved.EditResourceText(savedUid,"Name","Corrected resource");
  var mpp=Path.Combine(output,"resource-"+Guid.NewGuid()+".mpp");saved.SaveCopy(mpp,true);
  using(var readback=new ProjectSession()){readback.Open(mpp);if(!readback.Resources.Any(r=>r.Name=="Corrected resource"))throw new Exception("Saved resource name missing");}
  window.SelectPreviewTab(1);Pump();window.SelectPreviewTab(2);Pump();
  window.CloseForValidation();Console.WriteLine("PASS: resource sheet repeated name editing and view navigation");return 0;
 }
}

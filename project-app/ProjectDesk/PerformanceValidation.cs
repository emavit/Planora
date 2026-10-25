using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OfficeIMO.Project;
namespace ProjectDesk;
// Measurement-only check for large plans (PERF-01). It reports timings for generated projects and
// fails only on functional errors; budgets need an agreed representative workload first.
internal static class PerformanceValidation {
 public static int Run(string output,int[] sizes){
  Directory.CreateDirectory(output);
  AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
  void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);Dispatcher.UIThread.RunJobs();}
  var results=new List<Dictionary<string,long>>();
  foreach(int size in sizes){
   if(size<50||size>20000)throw new ArgumentOutOfRangeException(nameof(sizes),"Sizes must be between 50 and 20000 tasks.");
   var timings=new Dictionary<string,long>{["tasks"]=size};var clock=Stopwatch.StartNew();
   long Lap(string name){timings[name]=clock.ElapsedMilliseconds;clock.Restart();return timings[name];}
   using var session=new ProjectSession();session.NewProject("Synthetic "+size+" task plan",new DateTime(2026,10,19,8,0,0));
   session.AddResource("Engineer",ProjectResourceType.Work,100,0);
   // Paste the plan as a user would from a spreadsheet: chains of 50 linked tasks, every third one assigned.
   var rows=new System.Text.StringBuilder();for(int n=0;n<size;n++)rows.Append("Work package "+(n+1)+"\t"+(1+n%5)+"d\t"+(n%50==0?"":n.ToString())+"\t"+(n%3==0?"Engineer":"")+"\n");
   clock.Restart();
   var created=session.PasteTasks(null,["Name","Duration","Predecessors","ResourceNames"],"Name",rows.ToString());var firstChain=created.Take(50).ToList();
   Lap("pasteMs");
   if(session.Tasks.Count!=size)throw new Exception("Generated plan has "+session.Tasks.Count+" tasks instead of "+size);
   session.Recalculate();Lap("recalculateMs");
   var window=new PlannerWindow(session,Path.Combine(output,"views-"+size+"-"+Guid.NewGuid()+".json"));window.Show();window.SetCompactWorkspace(false);Pump();Lap("openWindowMs");
   var tableRows=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Where(r=>r.Uid>0).ToList();
   if(tableRows.Count!=size)throw new Exception("Task table shows "+tableRows.Count+" rows instead of "+size);
   window.Sheet.Grid.ScrollIntoView(tableRows[^1],window.Sheet.Grid.Columns.First());Pump();Lap("scrollToEndMs");
   // Edits go through the task grid like a user typing and pressing Enter; each lap covers commit to updated screen.
   void BeginCell(int uid,string header,string value){var grid=window.Sheet.Grid;grid.SelectedItem=grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==uid);grid.CurrentColumn=grid.Columns.Single(c=>Equals(c.Header,header));grid.ScrollIntoView(grid.SelectedItem,grid.CurrentColumn);grid.Focus();Pump();if(!grid.BeginEdit())throw new Exception(header+" cell did not begin editing");Pump();grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text=value;clock.Restart();}
   void CommitCell(string header){if(!window.Sheet.Grid.CommitEdit())throw new Exception(header+" cell edit did not commit");Pump();}
   int middle=tableRows[size/2].Uid;BeginCell(middle,"Task Name","Renamed work package");CommitCell("Task Name");Lap("renameMs");
   if(session.Document!.Tasks.GetByUid(middle).Name!="Renamed work package")throw new Exception("Rename did not apply");
   var finishBefore=session.Document.Tasks.GetByUid(firstChain[^1]).Finish;
   BeginCell(firstChain[0],"Duration","10d");CommitCell("Duration");Lap("rescheduleChainMs");
   if(!(session.Document.Tasks.GetByUid(firstChain[^1]).Finish>finishBefore))throw new Exception("Duration edit did not reschedule the 50-task chain");
   clock.Restart();window.UndoEdit();Pump();Lap("undoMs");
   if(session.Document.Tasks.GetByUid(firstChain[^1]).Finish!=finishBefore)throw new Exception("Undo did not restore the chain dates");
   window.UndoEdit();Pump();clock.Restart();
   string path=Path.Combine(output,"synthetic-"+size+"-"+Guid.NewGuid()+".mpp");
   session.SaveCopy(path,true);Lap("saveMppMs");timings["mppBytes"]=new FileInfo(path).Length;
   using(var reopened=new ProjectSession()){reopened.Open(path);if(reopened.Tasks.Count!=size)throw new Exception("Reopened plan has "+reopened.Tasks.Count+" tasks");}
   Lap("reopenMppMs");
   window.CloseForValidation();
   results.Add(timings);
   Console.WriteLine($"{size} tasks: paste {timings["pasteMs"]} ms, recalculate {timings["recalculateMs"]} ms, open window {timings["openWindowMs"]} ms, scroll {timings["scrollToEndMs"]} ms, rename {timings["renameMs"]} ms, reschedule chain {timings["rescheduleChainMs"]} ms, undo {timings["undoMs"]} ms, save {timings["saveMppMs"]} ms, reopen {timings["reopenMppMs"]} ms");
  }
  File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{platform=System.Runtime.InteropServices.RuntimeInformation.OSDescription,architecture=System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),runtime=Environment.Version.ToString(),results},new JsonSerializerOptions{WriteIndented=true}));
  Console.WriteLine("PASS: performance measurement for "+string.Join("/",sizes)+" task plans; functional checks only, no timing budgets.");
  return 0;
 }
}

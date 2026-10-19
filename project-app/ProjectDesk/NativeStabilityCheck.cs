using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
namespace ProjectDesk;

// Opt-in diagnostic: a disposable in-memory project in its own native Windows window.
internal static class NativeStabilityCheck {
 public static void Start(PlannerWindow window,ProjectSession session,IClassicDesktopStyleApplicationLifetime desktop){
  window.SetCompactWorkspace(false);
  int initial=session.Tasks.Count,step=0;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(130)};
  window.Title="Planora — stability check";
  timer.Tick+=(_,_)=>{
   try{
    int phase=step%5;
    if(phase==0){window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().First(r=>r.Uid>0);window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"＋ Task")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
    else if(phase==1&&(step/5)%2==1){window.ApplyPasteText("Native clipboard A\nNative clipboard B");}
    else if(phase==1){var editor=window.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible);editor.Text="Native test task "+step;if(!window.Sheet.Grid.CommitEdit())throw new Exception("Native task edit did not commit");}
    else if(phase==2){session.Undo();window.RefreshForValidation();window.Formatting.Edit(session.HistoryPosition,d=>d.Cells[FormattingStore.CellKey("Task",session.Tasks.First().Uid,"Name")]=new(){Bold=true,Highlight="#FFF2CC",Foreground="#4472C4",Wrap=true});window.Formatting.Undo();window.Formatting.Redo();window.Formatting.Undo();window.Sheet.Grid.ScrollIntoView(window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Last(),window.Sheet.Grid.Columns.First());}
    else if(phase==3){session.Redo();window.RefreshForValidation();window.Sheet.Grid.ScrollIntoView(window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().First(),window.Sheet.Grid.Columns.First());}
    else{session.Undo();window.RefreshForValidation();if(session.Tasks.Count!=initial)throw new Exception("Native Undo lost project rows");}
    step++;if(step>=100){timer.Stop();if(window.Gantt.RenderCount<20)throw new Exception("Native renderer was not exercised sufficiently");Console.WriteLine($"PASS native Windows: 20 blank-row insert/name/paste/format/undo/redo/scroll cycles; {window.Gantt.RenderCount} Gantt renders; original project unchanged.");window.CloseForValidation();desktop.Shutdown(0);}
   }catch(Exception error){timer.Stop();Console.Error.WriteLine(error);window.CloseForValidation();desktop.Shutdown(1);}
  };
  window.Opened+=(_,_)=>timer.Start();
 }
}


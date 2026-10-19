using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text.Json;

namespace ProjectDesk;

internal static class PlanningLibraryValidation {
    public static int Run(string output){
        Directory.CreateDirectory(output);var checks=new List<string>();
        void Check(bool condition,string label){if(!condition)throw new Exception(label);checks.Add(label);}
        foreach(var template in StarterTemplates.All){
            using var model=new ProjectSession();model.RestoreState(StarterTemplates.CreateState(template.Id,template.Name,new DateTime(2026,10,19)));
            Check(model.Tasks.Count==template.Tasks.Length&&model.Document!.Dependencies.Count==template.Tasks.Length-1,template.Id+": tasks and links");
            Check(model.Tasks.All(t=>t.Start.HasValue&&t.Finish.HasValue)&&model.Tasks.Last().Duration?.Value==0,template.Id+": dates and milestone");
            Check(model.HistoryPosition==1,template.Id+": atomic creation");model.Undo();Check(model.Tasks.Count==0,template.Id+": Undo");model.Redo();Check(model.Tasks.Count==template.Tasks.Length,template.Id+": Redo");
            string path=Path.Combine(output,template.Id+"-"+Guid.NewGuid()+".mpp");model.SaveCopy(path,true);using var readback=new ProjectSession();readback.Open(path);
            Check(readback.Tasks.Select(t=>t.Name).SequenceEqual(model.Tasks.Select(t=>t.Name))&&readback.Document!.Dependencies.Count==model.Document!.Dependencies.Count,template.Id+": MPP readback");
        }
        AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
        void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);Dispatcher.UIThread.RunJobs();}
        using var session=new ProjectSession();session.RestoreState(StarterTemplates.CreateState("software","Saved views test",new DateTime(2026,10,19)));
        string settings=Path.Combine(output,"views-"+Guid.NewGuid()+".json");var window=new PlannerWindow(session,settings);window.Show();Pump();
        window.Sheet.ApplyColumns(["DisplayId","Name","Duration","Predecessors"]);window.SetTimelineVisible(false);window.SetInspectorVisible(true);window.Axis.SetRange(new DateTime(2026,10,1),new DateTime(2026,11,30));window.Axis.SetZoom(3);window.SaveWorkspaceView("Delivery");
        int history=session.HistoryPosition;window.Sheet.ApplyColumns(TaskColumns.Defaults);window.SetTimelineVisible(true);window.SetInspectorVisible(false);window.Axis.EntireProject();window.SelectPreviewTab(12);window.ApplyWorkspaceView("Delivery");Pump();
        Check(window.CurrentView==1&&window.InspectorVisible&&!window.Timeline.IsVisible,"Saved layout restores view, inspector and timeline");Check(window.Sheet.VisibleKeys.SequenceEqual(new[]{"DisplayId","Name","Duration","Predecessors"}),"Saved columns restore order");Check(window.Axis.Start==new DateTime(2026,10,1)&&window.Axis.LastDate==new DateTime(2026,11,30)&&window.Axis.Zoom==3,"Saved dates and zoom restore");Check(session.HistoryPosition==history,"Saved views do not edit project model");
        bool duplicate=false;try{window.SaveWorkspaceView("delivery");}catch(ArgumentException){duplicate=true;}Check(duplicate&&window.SavedViews.Count==1,"Duplicate view names rejected without overwriting");
        Check(ViewPreferences.Load(settings).SavedViews.Single().Name=="Delivery","Saved views persist");
        var gallery=new TemplateLibraryWindow();var result=gallery.ShowDialog<TemplateChoice?>(window);Pump();var search=gallery.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TemplateSearch");var list=gallery.GetVisualDescendants().OfType<ListBox>().Single();
        search.Text="finance";Pump();Check(list.ItemCount==1,"Template search filters categories");search.Text="no-matching-template-abc";Pump();Check(list.ItemCount==0&&!gallery.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="TemplateCreate").IsEnabled,"No-results template state disables creation");search.Text="";Pump();Check(list.ItemCount==10,"Clearing search restores ten original starters");
        using(var frame=gallery.CaptureRenderedFrame()??throw new Exception("No template preview"))frame.Save(Path.Combine(output,"template-library.png"));
        gallery.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TemplateProjectName").Text="My rollout";gallery.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="TemplateCreate").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();Check(result.IsCompleted&&result.Result?.ProjectName=="My rollout","Template picker returns reviewed project choice");Check(session.Tasks.First().Name=="Define scope","Template preview does not replace current project");
        window.CloseForValidation();var restored=new PlannerWindow(session,settings);restored.Show();Pump();restored.ApplyWorkspaceView("Delivery");Check(restored.InspectorVisible&&restored.Axis.Zoom==3,"Saved view applies after restart");restored.DeleteWorkspaceView("Delivery");Check(ViewPreferences.Load(settings).SavedViews.Count==0,"Saved view deletion persists");restored.CloseForValidation();
        File.WriteAllText(Path.Combine(output,"checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS: {checks.Count} template/saved-view checks");return 0;
    }
}

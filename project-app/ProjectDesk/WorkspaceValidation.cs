using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text.Json;

namespace ProjectDesk;

internal static class WorkspaceValidation {
    public static int Run(string output){
        Directory.CreateDirectory(output);
        AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
        var checks=new List<string>();
        void Check(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);}
        void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);Dispatcher.UIThread.RunJobs();}
        using var session=new ProjectSession();session.NewProject("Workspace release",new DateTime(2026,10,19,8,0,0));
        int a=session.AddTask("Design"),b=session.AddTask("Build");session.EditText(b,"Predecessors","1FS");
        string settings=Path.Combine(output,"views-"+Guid.NewGuid()+".json");var window=new PlannerWindow(session,settings);window.Show();Pump();
        Button FindButton(string name)=>window.GetVisualDescendants().OfType<Button>().Single(b=>b.Name==name);
        void Click(string name){FindButton(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();}
        TextBox Field(string key)=>window.Inspector.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="Inspector"+key);
        Check(window.CompactWorkspace,"Compact workspace is the default");int history=session.HistoryPosition;
        foreach(int view in new[]{0,1,12,11,2,9,1}){Click("WorkspaceView"+view);Check(window.CurrentView==view,"View navigation "+view);}
        Click("WorkspaceRibbonToggle");Check(!window.CompactWorkspace,"Ribbon expands");Click("WorkspaceRibbonToggle");Check(window.CompactWorkspace,"Ribbon collapses");
        Check(history==session.HistoryPosition,"Navigation does not change project history");
        Click("WorkspaceInspectorToggle");window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==b);Pump();
        Check(window.Inspector.TaskUid==b&&Field("Name").Text=="Build","Inspector follows selected task");
        Field("Name").Text="Implementation";Field("Duration").Text="3d";Click("InspectorApply");
        Check(session.Tasks.Single(t=>t.Uid==b).Name=="Implementation"&&session.Tasks.Single(t=>t.Uid==b).Duration?.Value==3,"Inspector applies name and duration");
        Check(session.HistoryPosition==history+1,"Inspector changes form one Undo step");
        Check(session.Tasks.Single(t=>t.Uid==b).Start.HasValue&&session.Tasks.Single(t=>t.Uid==b).Finish>session.Tasks.Single(t=>t.Uid==b).Start,"Inspector computes automatic dates");
        session.Undo();window.RefreshForValidation();Pump();Check(Field("Name").Text=="Build","Undo refreshes inspector");
        Field("Name").Text="Must not apply";Field("Duration").Text="invalid";Click("InspectorApply");
        Check(session.Tasks.Single(t=>t.Uid==b).Name=="Build"&&window.Inspector.HasDraft,"Invalid batch leaves model intact and retains input");
        Check(window.Inspector.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="InspectorErrorDuration").IsVisible,"Invalid duration has an adjacent error");
        window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==a);Pump();
        Check(window.Inspector.TaskUid==b&&Field("Name").Text=="Must not apply","Draft stays bound to original task on selection change");
        window.Close();Pump();Check(window.IsVisible&&window.ActiveSavePrompt==null,"Invalid inspector input prevents accidental close");
        Click("InspectorCancel");Check(window.Inspector.TaskUid==a&&!window.Inspector.HasDraft,"Cancel follows current selection");
        Field("Name").Text="Architecture";Click("WorkspaceView12");Check(session.Tasks.Single(t=>t.Uid==a).Name=="Architecture","Navigation commits pending inspector edit");Click("WorkspaceView1");
        window.Sheet.FocusName(-1,true);Pump();window.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text="Blank row entry";Check(window.Sheet.Grid.CommitEdit(),"Blank row commits in compact workspace");Pump();Check(session.Tasks.Any(t=>t.Name=="Blank row entry"),"Compact workspace preserves direct task entry");
        var previousHeight=window.Height;window.Height=800;Pump();
        var applyButton=FindButton("InspectorApply");var applyPoint=applyButton.TranslatePoint(new Point(0,applyButton.Bounds.Height),window);
        Check(applyPoint.HasValue&&applyPoint.Value.Y<=window.Bounds.Height,"Apply remains within the shorter window");
        var scroll=window.Inspector.GetVisualDescendants().OfType<ScrollViewer>().Single(v=>v.Name=="InspectorFieldsScroll");
        var before=applyButton.TranslatePoint(new Point(0,0),window);scroll.Offset=new Vector(0,10000);Pump();
        Check(applyButton.TranslatePoint(new Point(0,0),window)==before,"Inspector actions remain pinned while fields scroll");
        scroll.Offset=default;window.Height=previousHeight;Pump();
        window.SetCompactWorkspace(false);Pump();
        var ribbon=window.GetVisualDescendants().OfType<RibbonModule>().Single();
        Check(ribbon.SelectedTab=="Planning","Planning ribbon is the default");
        var beforeZoom=window.Axis.Capture();Click("PlanningZoomIn");Check(window.Axis.Capture()!=beforeZoom,"Planning zoom control changes Gantt axis");
        window.Axis.Restore(beforeZoom);
        Click("RibbonCollapse");Check(Equals(FindButton("RibbonCollapse").Content,"Show controls"),"Ribbon collapses with correct label");
        ribbon.SelectTab("Planning");Pump();Check(Equals(FindButton("RibbonCollapse").Content,"Hide controls")&&FindButton("PlanningZoomIn").IsEffectivelyVisible,"Selecting a tab restores commands and label");
        var timeline=window.GetVisualDescendants().OfType<TimelineModule>().Single();var originalLayout=new TimelineLayout{Lanes=timeline.Lanes.ToList(),ActiveId=timeline.ActiveId};
        Click("PlanningAddTimeline");Check(timeline.Lanes.Count==originalLayout.Lanes.Count+1,"Planning ribbon creates a timeline");
        Click("PlanningTimelineTask");Check(timeline.Lanes.Single(l=>l.Id==timeline.ActiveId).TaskUids.Contains(window.Sheet.SelectedUid!.Value),"Planning ribbon adds selected task to timeline");
        timeline.RestoreLayout(originalLayout);Pump();
        var timelineState=JsonSerializer.Serialize(timeline.Lanes);var timelineAxis=timeline.Axis.Capture();
        window.SetTimelineVisible(true);Pump();Click("WorkspaceTimelineToggle");
        Check(!timeline.IsVisible&&!ViewPreferences.Load(settings).ShowTimeline,"One-click hide persists timeline visibility");
        Check(Equals(FindButton("WorkspaceTimelineToggle").Content,"Show timelines"),"Hidden timelines have a visible restore control");
        Click("WorkspaceTimelineToggle");
        Check(timeline.IsVisible&&ViewPreferences.Load(settings).ShowTimeline,"One-click show restores timeline panel");
        Check(JsonSerializer.Serialize(timeline.Lanes)==timelineState&&timeline.Axis.Capture()==timelineAxis,"Hide/show preserves timeline contents and dates");
        var beforeFit=window.Axis.Capture();Click("TimelineFitPanel");
        Check(ViewPreferences.Load(settings).TimelineHeight==200,"Fit panel returns single-lane height to 200");
        Check(window.Axis.Capture()==beforeFit,"Fit panel leaves Gantt dates unchanged");
        var ribbonHistory=session.HistoryPosition;
        foreach(var tab in new[]{"Task","Resource","View","Timeline","Planning"}){ribbon.SelectTab(tab);Pump();Check(ribbon.SelectedTab==tab,"Ribbon tab remains available: "+tab);}
        Check(session.HistoryPosition==ribbonHistory,"Ribbon view controls do not edit project");
        foreach(var mode in new[]{"Light","Dark"}){window.SetThemeMode(mode);Pump();using var frame=window.CaptureRenderedFrame()??throw new Exception("No workspace image");frame.Save(Path.Combine(output,"workspace-"+mode.ToLowerInvariant()+".png"));}
        // Exercise actual popup controls in both themes, including their selection action.
        foreach(var mode in new[]{"Light","Dark"}){
            window.SetThemeMode(mode);Pump();
            bool invoked=false;var item=new MenuItem{Header="Select view"};item.Click+=(_,_)=>invoked=true;
            var menu=new ContextMenu{Items={item}};menu.Open(window.Sheet);Pump();
            Check(menu.IsOpen&&item.IsEffectivelyVisible,"Styled menu opens in "+mode);
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Check(invoked,"Styled menu action works in "+mode);
            menu.Close();Pump();
        }
        window.SetCompactWorkspace(true);Pump();
        var saved=ViewPreferences.Load(settings);Check(saved.CompactWorkspace&&saved.ShowInspector,"Workspace preferences saved");
        window.SetRowDensity(true);Pump();Check(window.Sheet.Grid.RowHeight==38,"Comfortable density applied");
        window.SetRowDensity(false);Pump();Check(window.Sheet.Grid.RowHeight==28,"Compact density restored");
        window.SetInspectorWidth(380);window.SetCompactWorkspace(false);Pump();
        var axis=window.Axis.Capture();int beforeFocus=session.HistoryPosition;
        window.SetPlanningFocus(true);Pump();Check(window.PlanningFocus&&!window.Inspector.IsEffectivelyVisible,"Focus hides inspector");
        Check(window.Axis.Capture()==axis&&session.HistoryPosition==beforeFocus,"Focus preserves chart dates and project history");
        window.SetPlanningFocus(false);Pump();Check(window.Inspector.IsEffectivelyVisible&&!window.CompactWorkspace&&window.InspectorWidth==380,"Focus restores panels and width");
        window.SetCompactWorkspace(true);
        Check(ViewPreferences.Load(settings).InspectorWidth==380,"Inspector width persists");
        string mpp=Path.Combine(output,"workspace-"+Guid.NewGuid()+".mpp");session.SaveCopy(mpp,true);using(var readback=new ProjectSession()){readback.Open(mpp);Check(readback.Tasks.Any(t=>t.Name=="Architecture")&&readback.Tasks.Any(t=>t.Name=="Blank row entry"),"Inspector and grid edits survive MPP readback");}
        window.CloseForValidation();var reopened=new PlannerWindow(session,settings);reopened.Show();Pump();Check(reopened.CompactWorkspace&&reopened.InspectorVisible,"Workspace preferences restore");reopened.CloseForValidation();
        File.WriteAllText(Path.Combine(output,"checks.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS: {checks.Count} workspace/inspector checks");return 0;
    }
}

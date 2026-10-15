using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OfficeIMO.Project;
using OfficeIMO;
using System.Security.Cryptography;

namespace ProjectDesk;

public static class ThemeValidation {
    public static int Run(string file,string output){
        Directory.CreateDirectory(output);var settings=Path.Combine(output,"theme-views.json");if(File.Exists(settings))File.Delete(settings);
        byte[] original=SHA256.HashData(File.ReadAllBytes(file));
        AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
        using var session=new ProjectSession();session.Open(file);string model=session.Document!.ToXml(new(){LossPolicy=OfficeConversionLossPolicy.Allow});
        var window=new PlannerWindow(session,settings);window.Show();Pump();int checks=0;
        void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
        MenuItem ThemeItem(string mode)=>window.GetVisualDescendants().OfType<Menu>().Single().Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"Appearance")).Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"Theme")).Items.OfType<MenuItem>().Single(m=>Equals(m.Header,mode));
        window.GetVisualDescendants().OfType<RibbonModule>().Single().SelectTab("Appearance");Pump();var picker=window.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="ThemeMode");
        foreach(string mode in new[]{"Dark","Light","Automatic"}){
            ThemeItem(mode).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Pump();
            Assert(window.ThemeMode==mode,"Menu did not apply theme");Assert(Equals(picker.SelectedItem,mode),"Theme picker is out of sync");
            Assert(ThemeItem(mode).IsChecked&&new[]{"Automatic","Light","Dark"}.Count(m=>ThemeItem(m).IsChecked)==1,"Theme checkmarks are out of sync");
            Assert(Application.Current!.RequestedThemeVariant==(mode=="Automatic"?ThemeVariant.Default:mode=="Light"?ThemeVariant.Light:ThemeVariant.Dark),"Theme request wrong");
            Assert(ViewPreferences.Load(settings).ThemeMode==mode,"Theme not persisted");
            if(mode!="Automatic")Assert((Application.Current.ActualThemeVariant==ThemeVariant.Dark)==(mode=="Dark"),"Rendered theme wrong");
            using var frame=window.CaptureRenderedFrame()??throw new Exception("Theme frame missing");frame.Save(Path.Combine(output,$"theme-{mode.ToLowerInvariant()}.png"));
        }
        picker.SelectedItem="Dark";Pump();Assert(window.ThemeMode=="Dark"&&ThemeItem("Dark").IsChecked,"Picker did not apply theme");
        window.SetChartOptions(true,true);window.SelectPreviewTab(1);Pump();
        var hit=window.Gantt.VisibleNodes().First();var point=window.Gantt.TranslatePoint(new Point(40,hit.CenterY),window)!.Value;window.MouseMove(point);Pump();
        Assert(ToolTip.GetTip(window.Gantt)?.ToString()?.Contains("% complete")==true,"Gantt hover has no task detail");
        window.Sheet.Grid.SelectedItem=null;window.MouseDown(point,MouseButton.Left);window.MouseUp(point,MouseButton.Left);Pump();Assert(window.Sheet.SelectedUid==hit.Uid,"Gantt row click did not select matching task");
        using(var frame=window.CaptureRenderedFrame()??throw new Exception("Gantt options frame missing"))frame.Save(Path.Combine(output,"progress-gantt.png"));
        Assert(window.Gantt.ColorByProgress&&window.Gantt.ShowProgressLabels,"Chart options not applied");Assert(window.Gantt.DrawnProgressLabelCount>0,"No progress labels rendered");
        Assert(ReferenceEquals(GanttCanvas.ProgressBrush(0),GanttCanvas.NotStartedBrush)&&ReferenceEquals(GanttCanvas.ProgressBrush(50),GanttCanvas.InProgressBrush)&&ReferenceEquals(GanttCanvas.ProgressBrush(100),GanttCanvas.CompletedBrush),"Completion status colors wrong");
        var saved=ViewPreferences.Load(settings);Assert(saved.GanttColorByProgress&&saved.GanttProgressLabels,"Chart options not persisted");
        var timeline=window.Timeline;string styledLane=timeline.ActiveId;var task=session.Tasks.First(t=>t.Start.HasValue&&t.Finish>t.Start&&t.IsMilestone!=true&&!t.IsSummary);timeline.SetSelection([task.Uid],true);Pump();
        void Click(Window dialog,string label){dialog.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();}
        Task appearanceTask=timeline.EditAppearance(styledLane);Pump();var appearanceDialog=window.OwnedWindows.OfType<TimelineStyleWindow>().Single();
        var barColor=appearanceDialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TimelineBarColor");barColor.Text="not a color";Click(appearanceDialog,"Apply");Assert(appearanceDialog.IsVisible&&timeline.Lanes.Single(l=>l.Id==styledLane).Style?.BarColor==null,"Invalid timeline color applied");
        barColor.Text="#7E52B5";appearanceDialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TimelineMilestoneColor").Text="#B56531";appearanceDialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TimelineTextColor").Text="#FFFFFF";appearanceDialog.GetVisualDescendants().OfType<NumericUpDown>().Single(t=>t.Name=="TimelineFontSize").Value=13;appearanceDialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Name=="TimelineShowDates").IsChecked=false;appearanceDialog.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Name=="TimelineRoundedBars").IsChecked=false;Click(appearanceDialog,"Apply");appearanceTask.GetAwaiter().GetResult();
        var style=timeline.Lanes.Single(l=>l.Id==styledLane).Style!;Assert(style.BarColor=="#FF7E52B5"&&style.FontSize==13&&!style.ShowDates&&!style.RoundedBars,"Timeline dialog lost graphics options");Assert(timeline.Canvases.First(c=>c.Style==style).Style.TextColor=="#FFFFFFFF","Timeline canvas did not receive colors");
        var blockTask=timeline.EditBlockAppearance(styledLane,task.Uid);Pump();var blockDialog=window.OwnedWindows.OfType<TimelineItemStyleWindow>().Single();blockDialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TimelineBlockLabel").Text="Delivery block";blockDialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TimelineBlockColor").Text="#397FD1";Click(blockDialog,"Apply");blockTask.GetAwaiter().GetResult();Assert(timeline.Lanes.Single(l=>l.Id==styledLane).ItemStyles![task.Uid].Label=="Delivery block"&&task.Name!="Delivery block","Timeline label renamed project task");
        var destination=timeline.AddTimeline("Styled destination");timeline.MoveTask(styledLane,destination,task.Uid);Assert(timeline.Lanes.Single(l=>l.Id==destination).ItemStyles![task.Uid].Color=="#FF397FD1"&&!timeline.Lanes.Single(l=>l.Id==styledLane).ItemStyles!.ContainsKey(task.Uid),"Dragged block lost its appearance");
        timeline.RenameTimeline(styledLane,"Styled roadmap");Assert(timeline.Lanes.Single(l=>l.Id==styledLane).Style==style,"Rename lost timeline style");
        var milestone=session.Tasks.FirstOrDefault(t=>t.IsMilestone==true||t.Duration?.Value==0);if(milestone!=null){timeline.SelectLane(styledLane);timeline.AddSelection([milestone.Uid]);timeline.SetBlockAppearance(styledLane,milestone.Uid,new("Release milestone","#B56531"));Assert(timeline.Lanes.Single(l=>l.Id==styledLane).ItemStyles![milestone.Uid].Label=="Release milestone","Milestone appearance lost");}
        Pump();using(var styledFrame=window.CaptureRenderedFrame()??throw new Exception("Styled timeline frame missing"))styledFrame.Save(Path.Combine(output,"timeline-graphics.png"));
        window.Axis.SetRange(new DateTime(2000,1,1),new DateTime(2000,2,1));var range=window.Axis.Capture();window.GoToToday().GetAwaiter().GetResult();Pump();Assert(window.Axis.Capture()==range,"Today changed explicit date limits");
        Assert(session.Document.ToXml(new(){LossPolicy=OfficeConversionLossPolicy.Allow})==model&&!session.HasEdits,"Appearance changed project data");window.CloseForValidation();
        var restored=new PlannerWindow(session,settings);restored.Show();Pump();Assert(restored.ThemeMode=="Dark"&&restored.Gantt.ColorByProgress&&restored.Gantt.ShowProgressLabels,"Appearance not restored");Assert(restored.Timeline.Lanes.Single(l=>l.Id==styledLane).Style==style&&restored.Timeline.Lanes.Single(l=>l.Id==destination).ItemStyles![task.Uid].Label=="Delivery block","Timeline colors/labels not restored");restored.SetThemeMode("invalid");Assert(restored.ThemeMode=="Automatic"&&Application.Current!.RequestedThemeVariant==ThemeVariant.Default,"Invalid theme fallback wrong");restored.CloseForValidation();
        Assert(original.SequenceEqual(SHA256.HashData(File.ReadAllBytes(file))),"Original MPP changed");Console.WriteLine($"PASS: {checks} theme/Gantt checks; menu, picker, live palette, Automatic, preference restoration, progress colors/labels, today range preservation and unchanged project.");return 0;
    }
    private static void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
}

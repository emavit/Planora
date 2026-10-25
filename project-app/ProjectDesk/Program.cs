using Avalonia;
using Avalonia.Headless;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Interactivity;

namespace ProjectDesk;

static class Program {
    [STAThread]
    public static int Main(string[] args) {
        try{return Run(args);}catch(Exception error){Console.Error.WriteLine(error.ToString());return 1;}
    }
    private static int Run(string[] args) {
        if(args.FirstOrDefault()=="--icons-ui-test"){
            Directory.CreateDirectory(args[1]);var fixture=Path.Combine(args[1],"icons-fixture.mpp");
            using(var session=new ProjectSession()){session.NewProject("Icon validation",new DateTime(2026,10,19,8,0,0));session.AddTask("Design");session.AddTask("Delivery");session.SaveCopy(fixture,true);}
            return IconValidation.Run(fixture,args[1]);
        }
        if(args.FirstOrDefault()=="--timeline-ui-test"){
            Directory.CreateDirectory(args[1]);var fixture=Path.Combine(args[1],"timeline-fixture-"+Guid.NewGuid()+".mpp");
            using(var session=new ProjectSession()){session.NewProject("Timeline UI fixture",new DateTime(2026,10,19,8,0,0));session.AddTask("Discovery");session.AddTask("Delivery");session.AddTask("Launch",true);session.SaveCopy(fixture,true);}
            return TimelineValidation.Run(fixture,args[1]);
        }
        if(args.FirstOrDefault()=="--resource-sheet-test")return ResourceSheetValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--perf-test")return PerformanceValidation.Run(args[1],args.Length>2?args[2].Split(',').Select(int.Parse).ToArray():[500,2000,5000]);
        if(args.FirstOrDefault()=="--planning-library-test")return PlanningLibraryValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--workspace-test")return WorkspaceValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--durable-test")return DurableValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--recovery-ui-test")return RecoveryUiValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--mcp-stall-fixture"){File.WriteAllText(args[1],Environment.ProcessId.ToString());Thread.Sleep(30000);return 0;}
        if(args.FirstOrDefault()=="--ai-cli-fixture"){if(args[1]=="stall"){File.WriteAllText(args[2],Environment.ProcessId.ToString());Thread.Sleep(30000);}else if(args[1]=="oversized")Console.Out.Write(new string('x',3*1024*1024));else Console.Out.Write(Console.In.ReadToEnd());return 0;}
        if(args.FirstOrDefault()=="--brand-assets")return AppBrand.WriteAssets(args[1]);
        if(args.FirstOrDefault()=="--ai-test")return AiValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--mcp-demo-server")return McpDemoServer.Run().GetAwaiter().GetResult();
        if(args.FirstOrDefault()=="--visual-review")return VisualReviewValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--mcp-test")return McpValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--public-mpp-test")return PublicMppValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--theme-test")return ThemeValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--icons-test")return IconValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--pdf-test")return PdfValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--timeline-test")return TimelineValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--close-test")return CloseFlowValidation.Run(args[1]);
        if(args.FirstOrDefault()=="--auto-model-test")return AutoSchedulingValidation.ModelChecks(args[1]);
        if(args.FirstOrDefault()=="--auto-date-test")return AutoSchedulingValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--format-test")return FormattingValidation.Run(args[1],args[2]);
        if(args.FirstOrDefault()=="--core-test")return CoreValidation.Run(args[1]);
        if (args.FirstOrDefault() == "--smoke") {
            AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
            foreach (var file in args.Skip(1)) {
                using var session = new ProjectSession();
                session.Open(file);
                var task = session.Tasks.First();
                var original = task.Name;
                session.Rename(task.Uid, original + " test");
                session.Undo();
                if (session.Document!.Tasks.GetByUid(task.Uid).Name != original) throw new Exception("Undo failed");
                session.Redo();
                if (session.Document!.Tasks.GetByUid(task.Uid).Name != original + " test") throw new Exception("Redo failed");
                var output = System.IO.Path.Combine("project-app/previews", Guid.NewGuid() + ".mpp");
                Directory.CreateDirectory("project-app/previews");
                try {
                    session.SaveCopy(output);
                    using var reopened = OfficeIMO.Project.ProjectDocument.Load(output);
                    if (reopened.AllTasks.Single(t => t.Uid == task.Uid).Name != task.Name) throw new Exception("Save copy edit lost");
                    try { session.SaveCopy(output); throw new Exception("Existing output was overwritten"); }
                    catch (IOException) { }
                } finally { if (File.Exists(output)) File.Delete(output); }
                session.Undo();
                var preferencesPath = System.IO.Path.Combine("project-app/previews", "views-" + Guid.NewGuid() + ".json");
                var window = new PlannerWindow(session, preferencesPath);
                window.Show(); window.SetCompactWorkspace(false); Dispatcher.UIThread.RunJobs();
                if (window.TaskCount != session.Tasks.Count) throw new Exception("Task view count mismatch");
                if (window.ResourceCount != session.Resources.Count) throw new Exception("Resource view count mismatch");
                if (window.AssignmentCount != session.Document!.Assignments.Count(a => a.Task?.Uid != 0 && a.Resource?.Uid > 0)) throw new Exception("Assignment view count mismatch");
                if (!window.Sheet.Catalogue.Any(c=>c.Key=="TaskMode") || !window.Sheet.Catalogue.Any(c=>c.Key=="ResourceNames")) throw new Exception("Required selectable columns missing");
                if(ProjectCommandCatalogue.Observed.Count!=212||ProjectCommandCatalogue.Observed.Select(c=>c.Id).Distinct().Count()!=212)throw new Exception("REA command catalogue mismatch");
                if(!window.CommandActions.ContainsKey(19)||!window.CommandActions.ContainsKey(297)||window.CommandActions.ContainsKey(50196))throw new Exception("Command availability is not truthful");
                var editableTask=session.Tasks.First(t=>!t.IsSummary);var editableUid=editableTask.Uid;var editableName=editableTask.Name;
                var editGrid=window.Sheet.Grid;editGrid.SelectedItem=((IEnumerable<TaskTableRow>)editGrid.ItemsSource!).Single(r=>r.Uid==editableUid);
                editGrid.CurrentColumn=editGrid.Columns.Single(c=>Equals(c.Header,"Task Name"));editGrid.ScrollIntoView(editGrid.SelectedItem,editGrid.CurrentColumn);editGrid.Focus();Dispatcher.UIThread.RunJobs();
                if(!editGrid.BeginEdit())throw new Exception("Task name cell could not enter edit mode");Dispatcher.UIThread.RunJobs();
                var inline=editGrid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible);
                if(inline.Text!=editableName)throw new Exception("Inline editor contains display indentation instead of task name");
                inline.Text=editableName+" inline";if(!editGrid.CommitEdit())throw new Exception("Inline task name did not commit");Dispatcher.UIThread.RunJobs();
                if(session.Document.Tasks.GetByUid(editableUid).Name!=editableName+" inline")throw new Exception("Cell edit was not applied to project model");
                session.Undo();window.Sheet.Refresh(session);Dispatcher.UIThread.RunJobs();
                editGrid.CurrentColumn=editGrid.Columns.Single(c=>Equals(c.Header,"Duration"));editGrid.Focus();
                if(!editGrid.BeginEdit())throw new Exception("Duration cell could not enter edit mode");Dispatcher.UIThread.RunJobs();
                inline=editGrid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible);inline.Text="invalid duration";
                if(editGrid.CommitEdit())throw new Exception("Invalid cell edit was accepted");editGrid.CancelEdit();Dispatcher.UIThread.RunJobs();
                if(session.Document.Tasks.GetByUid(editableUid).Name!=editableName)throw new Exception("Inline undo failed");
                var entry=((IEnumerable<TaskTableRow>)editGrid.ItemsSource!).Single(r=>r.Uid==-1);editGrid.SelectedItem=entry;editGrid.CurrentColumn=editGrid.Columns.Single(c=>Equals(c.Header,"Task Name"));editGrid.ScrollIntoView(entry,editGrid.CurrentColumn);Dispatcher.UIThread.RunJobs();
                if(!editGrid.BeginEdit())throw new Exception("Blank task row cannot be edited");Dispatcher.UIThread.RunJobs();
                editGrid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text="Entered in blank row";
                if(!editGrid.CommitEdit())throw new Exception("Blank task row did not commit");Dispatcher.UIThread.RunJobs();
                if(!session.Tasks.Any(t=>t.Name=="Entered in blank row"))throw new Exception("Blank task entry did not create a task");session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                window.SelectPreviewTab(2);Dispatcher.UIThread.RunJobs();var r=session.Resources.First();var resourceName=r.Name;
                CommitInline(window.ResourceGrid,r.Uid,"Resource Name",resourceName+" edited");Dispatcher.UIThread.RunJobs();
                if(session.Document!.Resources.GetByUid(r.Uid).Name!=resourceName+" edited")throw new Exception("Resource cell edit failed");session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                window.SelectPreviewTab(3);Dispatcher.UIThread.RunJobs();var assignment=session.Document!.Assignments.FirstOrDefault(a=>a.Task?.Uid>0&&a.Resource?.Uid>0);
                if(assignment!=null){CommitInline(window.AssignmentGrid,assignment.Uid,"Units","50%");Dispatcher.UIThread.RunJobs();if(session.Document.Assignments.Single(a=>a.Uid==assignment.Uid).Units?.Value!=.5m)throw new Exception("Assignment units cell edit failed");session.Undo();window.RefreshForValidation();}
                window.SelectPreviewTab(1);Dispatcher.UIThread.RunJobs();
                window.Sheet.SetFilter("", "Milestones");Dispatcher.UIThread.RunJobs();if(window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Where(r=>r.Uid>0).Any(r=>{var t=session.Document!.Tasks.GetByUid(r.Uid);return t.IsMilestone!=true&&t.Duration?.Value!=0;}))throw new Exception("Milestone filter showed ordinary tasks");
                window.Sheet.SetFilter("", "All tasks");Dispatcher.UIThread.RunJobs();var summary=session.Tasks.FirstOrDefault(t=>t.IsSummary);
                if(summary!=null){window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==summary.Uid);int count=window.TaskCount;window.Sheet.ToggleSubtasks();if(window.TaskCount>=count)throw new Exception("Outline collapse did not hide children");window.Sheet.ExpandAll();if(window.TaskCount!=count)throw new Exception("Outline expand did not restore children");}
                var modelOrder=session.Tasks.Select(t=>t.Uid).ToArray();window.Sheet.SortRows("Name",true);if(window.TaskCount!=modelOrder.Length||!session.Tasks.Select(t=>t.Uid).SequenceEqual(modelOrder))throw new Exception("Display sorting changed saved task order or lost rows");window.Sheet.SortRows("Project order",false);
                if(window.TaskUsage.TaskCount!=session.Tasks.Count||window.TaskUsage.AssignmentCount!=session.Document!.Assignments.Count(a=>a.Task?.Uid>0&&a.Resource?.Uid>0))throw new Exception("Task Usage totals row coverage failed");
                if(window.Network.Diagram.NodeCount!=session.Tasks.Count)throw new Exception("Network Diagram task coverage failed");
                if(window.ProgressBoard.NotStartedCount+window.ProgressBoard.InProgressCount+window.ProgressBoard.CompletedCount!=session.Tasks.Count(t=>!t.IsSummary))throw new Exception("Progress Board task coverage failed");
                window.CalendarView.SetMonth(new DateTime(2026,10,1));if(window.CalendarView.DisplayedMonth!=new DateTime(2026,10,1))throw new Exception("Calendar navigation failed");
                window.SelectPreviewTab(12);Dispatcher.UIThread.RunJobs();var boardTask=session.Tasks.First(t=>!t.IsSummary);window.ProgressBoard.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Complete")&&Equals(b.Tag,boardTask.Uid)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(session.Document!.Tasks.GetByUid(boardTask.Uid).PercentComplete!=100)throw new Exception("Progress Board command did not change completion");session.Undo();window.RefreshForValidation();window.SelectPreviewTab(1);
                var reportHtml=window.Reports.ToHtml(session);if(!reportHtml.Contains("Cost Overview")||window.Reports.LeafTaskCount!=session.Tasks.Count(t=>!t.IsSummary))throw new Exception("Report leaf task coverage failed");
                var beforeName=session.Tasks.First().Name;session.Rename(session.Tasks.First().Uid,"<script>alert(1)</script>&");if(window.Reports.ToHtml(session).Contains("<script>"))throw new Exception("Report name was not HTML escaped");session.Undo();window.RefreshForValidation();
                var commandPicker=new ProjectCommandsWindow(window.CommandActions);commandPicker.Show(window);Dispatcher.UIThread.RunJobs();using(var commandFrame=commandPicker.CaptureRenderedFrame()??throw new Exception("Command catalogue did not render"))commandFrame.Save("project-app/previews/all-project-commands.png");commandPicker.Close();
                // Exercise actual menu events, popup opening and Office theme switching.
                var appMenu=window.GetVisualDescendants().OfType<Menu>().Single();
                var viewMenu=appMenu.Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"View"));
                viewMenu.Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"Resource Sheet")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Dispatcher.UIThread.RunJobs();
                if(!window.ResourceGrid.IsVisible)throw new Exception("View menu did not open Resource Sheet");
                viewMenu.Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"Gantt Chart")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Dispatcher.UIThread.RunJobs();
                window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==editableUid);
                window.GetVisualDescendants().OfType<RibbonModule>().Single().SelectTab("Task");Dispatcher.UIThread.RunJobs();
                window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Manually Schedule")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
                if(session.Document.Tasks.GetByUid(editableUid).IsManual!=true)throw new Exception("Ribbon task mode did not apply");session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                var findDropdown=window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Find ▾"));findDropdown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
                if(findDropdown.ContextMenu?.IsOpen!=true)throw new Exception("Ribbon dropdown did not open");findDropdown.ContextMenu.Close();
                static bool IsDark(Avalonia.Media.Color c)=>0.2126*c.R+0.7152*c.G+0.0722*c.B<0.4*255;
                Application.Current!.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Dark;Dispatcher.UIThread.RunJobs();
                if(RibbonModule.Chrome.Color!=DesignTokens.Background.Color||!IsDark(RibbonModule.Chrome.Color))throw new Exception("Dark Office theme did not follow application theme");
                AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();using(var darkFrame=window.CaptureRenderedFrame()??throw new Exception("Dark theme did not render"))darkFrame.Save("project-app/previews/office-dark.png");
                Application.Current.RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light;Dispatcher.UIThread.RunJobs();
                if(RibbonModule.Chrome.Color!=DesignTokens.Background.Color||IsDark(RibbonModule.Chrome.Color))throw new Exception("Light Office theme did not follow application theme");
                window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==editableUid);
                int oldCount=session.Tasks.Count;var oldLinks=session.Document.Dependencies.Count;
                window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"＋ Task")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
                var insertedRow=(TaskTableRow)window.Sheet.Grid.SelectedItem!;
                if(insertedRow.Uid>=-1||session.Tasks.Count!=oldCount||window.Sheet.CurrentKey!="Name")throw new Exception("Insert did not create a focused blank row without modifying the project");
                var insertedEditor=window.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible);
                if(insertedEditor.Text!="")throw new Exception("Blank row contains a placeholder task name");
                AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();using(var insertionFrame=window.CaptureRenderedFrame()??throw new Exception("Insertion did not render"))insertionFrame.Save("project-app/previews/task-insertion.png");
                insertedEditor.Text="Inserted like Excel";if(!window.Sheet.Grid.CommitEdit())throw new Exception("New task name did not commit");Dispatcher.UIThread.RunJobs();
                var newTask=session.Tasks.Single(t=>t.Name=="Inserted like Excel");
                if(session.Tasks.FindIndex(t=>t.Uid==newTask.Uid)+1!=session.Tasks.FindIndex(t=>t.Uid==editableUid)||session.Document.Dependencies.Count!=oldLinks)throw new Exception("Blank-row commit changed insertion order or existing dependencies");
                session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();if(session.Tasks.Count!=oldCount)throw new Exception("Single Undo did not remove new task");
                window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==editableUid);
                window.Sheet.Grid.RaiseEvent(new Avalonia.Input.KeyEventArgs{RoutedEvent=Avalonia.Input.InputElement.KeyDownEvent,Key=Avalonia.Input.Key.Insert});Dispatcher.UIThread.RunJobs();
                if(window.Sheet.Grid.SelectedItem is not TaskTableRow{Uid:<-1})throw new Exception("Insert key did not insert blank row");window.Sheet.Grid.CancelEdit();Dispatcher.UIThread.RunJobs();
                if(window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Any(r=>r.Uid<-1)||session.Tasks.Count!=oldCount)throw new Exception("Escape did not discard empty insertion");
                window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==editableUid);
                window.Sheet.Grid.ContextMenu!.Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"Insert Milestone")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Dispatcher.UIThread.RunJobs();
                window.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text="Blank-line milestone";window.Sheet.Grid.CommitEdit();Dispatcher.UIThread.RunJobs();
                if(session.Tasks.Single(t=>t.Name=="Blank-line milestone").Duration?.Value!=0)throw new Exception("Context-menu milestone insertion failed");session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                window.Sheet.FocusName(-1,true);Dispatcher.UIThread.RunJobs();
                var entryEditor=window.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible);entryEditor.Text="Enter continuous entry";entryEditor.Focus();
                window.KeyPressQwerty(Avalonia.Input.PhysicalKey.Enter,Avalonia.Input.RawInputModifiers.None);window.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.Enter,Avalonia.Input.RawInputModifiers.None);Dispatcher.UIThread.RunJobs();
                if(!session.Tasks.Any(t=>t.Name=="Enter continuous entry")||window.Sheet.Grid.SelectedItem is not TaskTableRow{Uid:-1}||window.Sheet.CurrentKey!="Name")throw new Exception("Enter did not commit and advance to next blank task row");
                window.Sheet.Grid.CancelEdit();session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                var duplicateNodes=GanttCanvas.NodeMap([new(24,0,10,50,26),new(24,0,10,50,26),new(25,10,20,76,26)]);if(duplicateNodes.Count!=2)throw new Exception("Duplicate visual-row crash regression failed");
                for(int cycle=0;cycle<12;cycle++){
                    window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().First(r=>r.Uid>0);
                    window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"＋ Task")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);Dispatcher.UIThread.RunJobs();
                    window.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text="Stress insertion "+cycle;window.Sheet.Grid.CommitEdit();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);Dispatcher.UIThread.RunJobs();
                    session.Undo();window.RefreshForValidation();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);Dispatcher.UIThread.RunJobs();session.Redo();window.RefreshForValidation();AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);Dispatcher.UIThread.RunJobs();session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                    var rows=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().ToArray();window.Sheet.Grid.ScrollIntoView(rows.Last(),window.Sheet.Grid.Columns.First());AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);Dispatcher.UIThread.RunJobs();window.Sheet.Grid.ScrollIntoView(rows.First(),window.Sheet.Grid.Columns.First());Dispatcher.UIThread.RunJobs();
                    var nodes=window.Gantt.VisibleNodes();if(nodes.Select(n=>n.Uid).Distinct().Count()!=nodes.Count||session.Tasks.Count!=oldCount)throw new Exception("Rendering/scroll/undo stress regression failed");
                }
                var headerButton=window.Sheet.GetVisualDescendants().OfType<Button>().Single(b=>Avalonia.Automation.AutomationProperties.GetName(b)=="Task Name column menu");
                headerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();if(headerButton.ContextMenu?.IsOpen!=true)throw new Exception("Column header dropdown did not open");
                headerButton.ContextMenu.Items.OfType<MenuItem>().Single(m=>Equals(m.Header,"Sort A to Z")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));headerButton.ContextMenu.Close();Dispatcher.UIThread.RunJobs();
                if(!session.Tasks.Select(t=>t.Uid).SequenceEqual(modelOrder))throw new Exception("Header sort changed model task order");window.Sheet.SortRows("Project order",false);
                if(window.Sheet.VisibleKeys.Contains("Add New Column"))throw new Exception("UI column was saved as task data");
                window.Sheet.Grid.ScrollIntoView(window.Sheet.Grid.SelectedItem,window.Sheet.Grid.Columns.Last());Dispatcher.UIThread.RunJobs();
                window.Sheet.GetVisualDescendants().OfType<Button>().Single(b=>Avalonia.Automation.AutomationProperties.GetName(b)=="Add New Column").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
                var headerPicker=window.OwnedWindows.OfType<ColumnPickerWindow>().Single();headerPicker.Close();Dispatcher.UIThread.RunJobs();
                window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==editableUid);window.Sheet.InsertBlankRow(false);Dispatcher.UIThread.RunJobs();
                window.ApplyPasteText("Excel paste A\t2d\nExcel paste B\t3d");Dispatcher.UIThread.RunJobs();
                var pasted=session.Tasks.Where(t=>t.Name is "Excel paste A" or "Excel paste B").ToArray();if(pasted.Length!=2||session.Tasks.FindIndex(t=>t.Uid==pasted[1].Uid)+1!=session.Tasks.FindIndex(t=>t.Uid==editableUid)||window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Any(r=>r.Uid<-1))throw new Exception("Paste into blank inserted row failed");
                session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();if(session.Tasks.Count!=oldCount)throw new Exception("Paste UI did not Undo atomically");
                window.Sheet.FocusName(-1,true);Dispatcher.UIThread.RunJobs();window.ApplyPasteText("Pasted bottom A\nPasted bottom B");Dispatcher.UIThread.RunJobs();if(session.Tasks.Count!=oldCount+2)throw new Exception("Paste at bottom did not create rows");session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                var datedTask=session.Tasks.Where(t=>t.Start.HasValue&&t.Finish.HasValue).OrderBy(t=>t.Start).Last();window.Sheet.Grid.SelectedItem=window.Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Single(r=>r.Uid==datedTask.Uid);window.Sheet.Grid.ScrollIntoView(window.Sheet.Grid.SelectedItem,window.Sheet.Grid.Columns.First());window.Axis.SetZoom(16);Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();
                window.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Scroll to Task")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();if(window.ChartHorizontalOffset<=0)throw new Exception("Scroll to Task did not reveal the late task at detailed zoom");window.Axis.Fit();
                window.Sheet.SetFilter("","Milestones");window.KeyPressQwerty(Avalonia.Input.PhysicalKey.F3,Avalonia.Input.RawInputModifiers.None);window.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.F3,Avalonia.Input.RawInputModifiers.None);Dispatcher.UIThread.RunJobs();if(window.TaskCount!=oldCount)throw new Exception("F3 did not reset task filtering");
                window.Sheet.FocusName(-1,true);Dispatcher.UIThread.RunJobs();
                window.Clipboard!.SetTextAsync("Keyboard paste A\nKeyboard paste B").GetAwaiter().GetResult();
                var pasteModifier=OperatingSystem.IsMacOS()?Avalonia.Input.RawInputModifiers.Meta:Avalonia.Input.RawInputModifiers.Control;
                window.KeyPressQwerty(Avalonia.Input.PhysicalKey.V,pasteModifier);window.KeyReleaseQwerty(Avalonia.Input.PhysicalKey.V,pasteModifier);Dispatcher.UIThread.RunJobs();
                if(session.Tasks.Count!=oldCount+2||!session.Tasks.Any(t=>t.Name=="Keyboard paste B"))throw new Exception("Ctrl+V in the name editor did not create task rows");session.Undo();window.RefreshForValidation();Dispatcher.UIThread.RunJobs();
                var originalColumns=window.Sheet.VisibleKeys.ToArray();
                window.Sheet.ApplyColumns(["DisplayId","TaskMode","Name","ResourceNames","Baseline0.Start","PercentComplete","Cost"]);
                window.Sheet.Persist();
                var loadedPreferences=ViewPreferences.Load(preferencesPath);
                if (!loadedPreferences.Columns.Select(c=>c.Key).SequenceEqual(window.Sheet.VisibleKeys)) throw new Exception("Column layout persistence failed");
                window.Sheet.ApplyColumns(originalColumns);
                window.SetTimelineVisible(false);
                if (window.Timeline.IsVisible) throw new Exception("Timeline hide failed");
                window.SetTimelineVisible(true);
                window.Axis.ZoomIn(); window.Axis.ZoomOut(); window.Axis.Fit();
                Directory.CreateDirectory("project-app/previews");
                var picker=new ColumnPickerWindow(window.Sheet.Catalogue,originalColumns,"Name");picker.Show(window);Dispatcher.UIThread.RunJobs();
                picker.GetVisualDescendants().OfType<TextBox>().Single().Text="Priority";Dispatcher.UIThread.RunJobs();
                var available=picker.GetVisualDescendants().OfType<ListBox>().Single(l=>l.ItemsSource is IEnumerable<TaskColumn> c&&c.Count()==1&&c.First().Key=="Priority");
                available.SelectedItem=((IEnumerable<TaskColumn>)available.ItemsSource!).Single();
                picker.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Insert →")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var chosen=picker.GetVisualDescendants().OfType<ListBox>().Single(l=>l!=available);
                if(((IEnumerable<TaskColumn>)chosen.ItemsSource!).ElementAt(2).Key!="Priority")throw new Exception("Column search/insert before selection failed");
                picker.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"Move up")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if(((IEnumerable<TaskColumn>)chosen.ItemsSource!).ElementAt(1).Key!="Priority")throw new Exception("Column move up failed");
                Dispatcher.UIThread.RunJobs();using(var pickerFrame=picker.CaptureRenderedFrame()??throw new Exception("Column picker did not render"))pickerFrame.Save("project-app/previews/column-picker.png");picker.Close();
                var cellEditor=new TaskFieldEditorWindow(session.Document.Tasks.GetByUid(task.Uid),"TaskMode","Task Mode");cellEditor.Show(window);Dispatcher.UIThread.RunJobs();
                using(var cellFrame=cellEditor.CaptureRenderedFrame()??throw new Exception("Cell editor did not render"))cellFrame.Save("project-app/previews/task-mode-editor.png");cellEditor.Close();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();
                using (var frame = window.CaptureRenderedFrame() ?? throw new Exception("Window did not render"))
                    frame.Save("project-app/previews/" + System.IO.Path.GetFileNameWithoutExtension(file) + ".png");
                for (var tab = 1; tab < 13; tab++) {
                    window.SelectPreviewTab(tab);
                    Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();
                    using var frame = window.CaptureRenderedFrame() ?? throw new Exception("Tab did not render");
                    frame.Save($"project-app/previews/{System.IO.Path.GetFileNameWithoutExtension(file)}-tab{tab}.png");
                }
                window.CloseForValidation();
                if(File.Exists(preferencesPath))File.Delete(preferencesPath);
                Console.WriteLine($"PASS: {System.IO.Path.GetFileName(file)}; {session.Tasks.Count} tasks, {session.Resources.Count} resources, {window.Sheet.Catalogue.Count} selectable columns; {ProjectCommandCatalogue.Observed.Count} observed commands / {ProjectCommandCatalogue.Observed.Count(c=>window.CommandActions.ContainsKey(c.Id))} local routes; 13 views; Office menus, dropdown, task mode, light/dark, blank insertion/focus/keyboard/cancel/single undo/render stress/header menu/append paste PASS");
            }
            using(var empty=new ProjectSession()){
                empty.NewProject("Empty insertion check",new DateTime(2026,10,19));var blankWindow=new PlannerWindow(empty,"project-app/previews/empty-check-views.json");blankWindow.Show();blankWindow.SetCompactWorkspace(false);Dispatcher.UIThread.RunJobs();
                blankWindow.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,"＋ Task")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
                if(empty.Tasks.Count!=0||blankWindow.Sheet.Grid.SelectedItem is not TaskTableRow{Uid:<-1})throw new Exception("Empty project did not insert a blank row");
                blankWindow.Sheet.Grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text="First task";if(!blankWindow.Sheet.Grid.CommitEdit())throw new Exception("Empty project task did not commit");Dispatcher.UIThread.RunJobs();
                if(empty.Tasks.Count!=1||empty.Tasks[0].Name!="First task")throw new Exception("First task was lost");empty.Undo();blankWindow.RefreshForValidation();Dispatcher.UIThread.RunJobs();if(empty.Tasks.Count!=0)throw new Exception("First task undo failed");blankWindow.CloseForValidation();Console.WriteLine("PASS: empty project blank row, name commit and single Undo");
            }
            return 0;
        }
        return AppBuilder.Configure<DesktopApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
    // Command line that starts this app again, whether it runs from its apphost or as "dotnet Planora.dll".
    internal static (string FileName,string[] Arguments) SelfCommand(params string[] args){string executable=Environment.ProcessPath??throw new InvalidOperationException("Cannot locate Planora executable.");return Path.GetFileNameWithoutExtension(executable).Equals("dotnet",StringComparison.OrdinalIgnoreCase)?(executable,[typeof(Program).Assembly.Location,..args]):(executable,args);}
    private static void CommitInline(DataGrid grid,int uid,string header,string value){grid.SelectedItem=grid.ItemsSource!.OfType<SheetRecord>().Single(r=>r.Uid==uid);grid.CurrentColumn=grid.Columns.Single(c=>Equals(c.Header,header));grid.ScrollIntoView(grid.SelectedItem,grid.CurrentColumn);grid.Focus();Dispatcher.UIThread.RunJobs();if(!grid.BeginEdit())throw new Exception(header+" cannot begin edit");Dispatcher.UIThread.RunJobs();grid.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="CellTextBox"&&t.IsVisible).Text=value;if(!grid.CommitEdit())throw new Exception(header+" edit could not commit");}
}

public sealed class DesktopApp : Application {
    public override void Initialize() {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Planora/")) { Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml") });
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
        DesignTokens.Apply(this);
        AppBrand.Install();
        UiIcons.Install(this);
    }
    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var session = new ProjectSession();
            bool stability=desktop.Args?.FirstOrDefault()=="--stability-check";
            string? path=stability?desktop.Args?.ElementAtOrDefault(1):desktop.Args?.FirstOrDefault();bool planora=path!=null&&System.IO.Path.GetExtension(path).Equals(".planora",StringComparison.OrdinalIgnoreCase);if(path!=null&&System.IO.File.Exists(path)&&!planora)session.Open(path);
            desktop.MainWindow = new PlannerWindow(session,stability?Path.Combine("project-app/previews","native-check-views.json"):null);
            if(planora&&path!=null)((PlannerWindow)desktop.MainWindow).OpenPlanoraCopy(path);
            if(stability)NativeStabilityCheck.Start((PlannerWindow)desktop.MainWindow,session,desktop);
            desktop.Exit += (_, _) => session.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}





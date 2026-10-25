using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Platform.Storage;
using Avalonia.Input.Platform;
using OfficeIMO.Project;

namespace ProjectDesk;

// Composes independently replaceable ribbon, timeline, task-sheet and chart modules.
public sealed partial class PlannerWindow : Window {
    private readonly ProjectSession _session;
    private readonly ViewPreferences _preferences;
    private readonly string _settingsPath;
    private readonly TextBlock _status=new(){Margin=new Thickness(14,6),FontSize=11,TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock _title=new(){FontSize=18,FontWeight=FontWeight.SemiBold};
    private readonly TextBlock _viewTitle=new(){FontSize=14,FontWeight=FontWeight.SemiBold,Margin=new Thickness(12,8)};
    private readonly TextBox _name=new(){Watermark="Selected task name",Width=300};
    private readonly DateAxis _axis=new();
    private readonly ContentControl _page=new();
    private readonly TaskGanttModule _planning;
    private readonly TaskUsageModule _taskUsage=new();
    private readonly NetworkDiagramModule _network=new();
    private readonly ResourceGraphModule _resourceGraph=new();
    private readonly ProjectReportsModule _reports=new();
    private readonly CalendarViewModule _calendarView=new();
    private readonly ProgressBoardModule _progressBoard=new();
    private readonly RibbonModule _ribbon=new();
    private readonly GlassNavigationSurface _glassNavigation;
    private readonly ComboBox _taskFilter=new(){ItemsSource=new[]{"All tasks","Milestones","Critical tasks","Incomplete tasks","Completed tasks"},SelectedIndex=0,Width=160};
    private readonly TextBox _find=new(){Watermark="Find task…",Width=180};
    private readonly Dictionary<int,Func<Task>> _commandActions=[];
    private readonly TextBox _taskDrivers=new(){IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(12)};
    private readonly TimelineModule _timeline;
    private readonly Grid _workspace=new(){RowDefinitions=new RowDefinitions("180,6,*")};
    private readonly GridSplitter _timelineDivider=new(){Height=6,ResizeDirection=GridResizeDirection.Rows,Background=Brushes.Transparent};
    private readonly DataGrid _resources=MakeGrid(),_assignments=MakeGrid();
    private readonly TaskDetailsModule _details=new();
    private readonly ScrollViewer _noticesHost=new();
    private readonly TextBlock _notices=new(){Margin=new Thickness(16),TextWrapping=TextWrapping.Wrap};
    private readonly Grid _main=new(){ColumnDefinitions=new ColumnDefinitions("0,*,0,0")};
    private readonly Border _sidebar;
    private readonly List<Button> _navigation=[];
    private int _currentPage=1;
    private bool _busy;
    private bool _closeApproved;
    public TaskSheetModule Sheet=>_planning.Sheet;
    public TimelineModule Timeline=>_timeline;
    public GanttCanvas Gantt=>_planning.Chart;
    public DateAxis Axis=>_axis;
    public double ChartHorizontalOffset=>_planning.HorizontalOffset;
    public DataGrid ResourceGrid=>_resources;
    public DataGrid AssignmentGrid=>_assignments;
    public TaskUsageModule TaskUsage=>_taskUsage;
    public NetworkDiagramModule Network=>_network;
    public ResourceGraphModule ResourceGraph=>_resourceGraph;
    public ProjectReportsModule Reports=>_reports;
    public CalendarViewModule CalendarView=>_calendarView;
    public ProgressBoardModule ProgressBoard=>_progressBoard;
    public IReadOnlyDictionary<int,Func<Task>> CommandActions=>_commandActions;
    public void RefreshForValidation()=>Refresh();
    public int TaskCount=>Sheet.Grid.ItemsSource?.OfType<TaskTableRow>().Count(r=>r.Uid>0)??0;
    public int ResourceCount=>(_resources.ItemsSource as System.Collections.ICollection)?.Count??0;
    public int AssignmentCount=>(_assignments.ItemsSource as System.Collections.ICollection)?.Count??0;
    public PlannerWindow(ProjectSession session,string? settingsPath=null) {
        _noticesHost.Content=_notices;
        _session=session;_settingsPath=settingsPath??ViewPreferences.DefaultPath;_preferences=ViewPreferences.Load(_settingsPath);
        _inspector=new TaskInspectorModule(_session);_inspector.Applied+=()=>Refresh();
        _formats=new FormattingStore(_preferences,Persist,_session.DiscardRedo);
        Title=AppBrand.Name;Width=1550;Height=1000;MinWidth=1000;MinHeight=650;
        FontFamily=DesignTokens.Font;Foreground=DesignTokens.Text;Background=Brushes.Transparent;TransparencyLevelHint=[WindowTransparencyLevel.AcrylicBlur,WindowTransparencyLevel.Blur,WindowTransparencyLevel.None];
        var sheet=new TaskSheetModule(_preferences,_settingsPath){Formatting=_formats};_planning=new(sheet,_axis,_preferences.TableFraction);_planning.SplitChanged+=value=>{_preferences.TableFraction=value;Persist();};_timeline=new(_axis,_preferences,_settingsPath);_timeline.Error+=message=>_status.Text=message;_timeline.HeightRequested+=height=>{_preferences.TimelineHeight=Math.Max(_preferences.TimelineHeight,height);UpdateTimelineVisibility();Persist();};_timeline.FitHeightRequested+=height=>{_preferences.TimelineHeight=height;UpdateTimelineVisibility();Persist();};_axis.Changed+=SaveGanttAxis;
        sheet.Grid.FontFamily=new FontFamily(_preferences.FontFamilyName??DesignTokens.Font.Name);
        sheet.Grid.FontSize=Math.Clamp(_preferences.FontSize,9,24);sheet.Grid.RowHeight=Math.Clamp(_preferences.RowHeight,22,60);sheet.Grid.GridLinesVisibility=_preferences.Gridlines?DataGridGridLinesVisibility.All:DataGridGridLinesVisibility.None;Gantt.ShowCritical=_preferences.CriticalTasks;Gantt.BaselineNumber=Math.Clamp(_preferences.BaselineNumber,0,10);
        sheet.InsertTaskRequested+=async milestone=>await RunMenu(()=>InsertTask(milestone));
        _formats.Changed+=()=>{Gantt.InvalidateVisual();_resources.InvalidateVisual();_assignments.InvalidateVisual();UpdateFormattedRowHeights();};Gantt.Formatting=_formats;
        sheet.TaskSelected+=uid=>SelectTask(uid);sheet.Error+=message=>_status.Text=message;
        sheet.EditCellRequested+=async key=>{try{await EditField(key);}catch(Exception e){_status.Text=e.Message;}};
        _network.TaskActivated+=async uid=>{try{SelectPage(1);SelectUid(uid);await EditTask();}catch(Exception e){_status.Text=e.Message;}};
        _taskUsage.TaskActivated+=async uid=>{try{SelectPage(1);SelectUid(uid);await EditTask();}catch(Exception e){_status.Text=e.Message;}};
        _taskUsage.AssignmentActivated+=async uid=>{try{var a=_session.Document!.Assignments.Single(a=>a.Uid==uid);SelectPage(1);SelectUid(a.Task!.Uid);await AssignResource(a.Resource!.Uid);}catch(Exception e){_status.Text=e.Message;}};
        _calendarView.TaskActivated+=async uid=>{try{SelectPage(1);SelectUid(uid);await EditTask();}catch(Exception e){_status.Text=e.Message;}};
        _progressBoard.TaskActivated+=async uid=>{try{SelectPage(1);SelectUid(uid);await EditTask();}catch(Exception e){_status.Text=e.Message;}};
        _progressBoard.ProgressRequested+=(uid,value)=>{try{_session.SetProgress([uid],value);Refresh();}catch(Exception e){_status.Text=e.Message;}};
        sheet.ValuesChanged+=()=>{UpdateGanttAxis();Gantt.SetProject(_session.Document,_session.Tasks);_timeline.Refresh(_session);if(Sheet.SelectedUid is int uid)SelectTask(uid);_status.Text="Edit applied. Automatic task dates update on commit. Calculate Project reviews resource work and costs.";};
        var root=new DockPanel{Background=DesignTokens.Background};
        RibbonModule.ApplyAppearance(Application.Current?.ActualThemeVariant==ThemeVariant.Dark);
        var menus=new Menu{Background=RibbonModule.Chrome,Foreground=RibbonModule.Ink,FontSize=12};menus.Styles.Add(new Style(s=>s.OfType<MenuItem>()){Setters={new Setter(MenuItem.ForegroundProperty,RibbonModule.Ink)}});
        menus.Items.Add(Menu("File",("New",NewProject),("New from Template…",NewFromTemplate),("Open…",OpenProject),("Save a Copy…",SaveCopy),("Export PDF…",ExportPdf),("Recovery…",ShowRecovery),("Create Recovery Checkpoint",CheckpointNow),("Close Project",CloseProject),("Exit",ExitApplication)));
        menus.Items.Add(Menu("Edit",("Undo",()=>{UndoEdit();return Task.CompletedTask;}),("Redo",()=>{RedoEdit();return Task.CompletedTask;}),("Copy",CopyCell),("Paste",PasteCells),("Find…",FindTask),("Replace…",ReplaceTaskNames)));
        menus.Items.Add(Menu("Task",("Insert Task",()=>InsertTask(false)),("Insert Milestone",()=>InsertTask(true)),("Task Information…",EditTask),("Link Tasks…",LinkTasks),("Delete Tasks…",DeleteTasks)));
        menus.Items.Add(Menu("Resource",("Add Resource…",AddResource),("Assign Resources…",AssignResource),("Resource Information…",EditResource)));
        menus.Items.Add(Menu("Project",("Project Information…",ProjectInformation),("Change Working Time…",ChangeWorkingTime),("Calculate Project…",CalculateProject),("Set Baseline…",SetBaseline)));
        menus.Items.Add(Menu("View",("Saved Views…",ManageSavedViews),("Gantt Chart",()=>Page(1)),("Task Sheet",()=>Page(0)),("Task Usage",()=>Page(6)),("Network Diagram",()=>Page(7)),("Resource Sheet",()=>Page(2)),("Resource Graph",()=>Page(8)),("Calendar",()=>Page(11)),("Show / Hide Timeline",()=>{SetTimelineVisible(!_preferences.ShowTimeline);return Task.CompletedTask;}),("Insert Columns…",()=>Sheet.ChooseColumns(this)),("Zoom and Gantt Dates…",ZoomDialog),("Timeline Dates…",()=>_timeline.EditDates()),("Reduce Transparency",()=>{_preferences.ReduceTransparency=!_preferences.ReduceTransparency;UpdateGlassNavigation();Persist();return Task.CompletedTask;})));
        menus.Items.Add(Menu("Format",("Text and Color…",EditTextFormat),("Bold",()=>FormatText(f=>f with{Bold=f.Bold!=true})),("Italic",()=>FormatText(f=>f with{Italic=!f.Italic})),("Underline",()=>FormatText(f=>f with{Underline=f.Underline=="None"?"Single":"None"})),("Text Color…",()=>PickTextColor(false)),("Highlight…",()=>PickTextColor(true)),("Format Painter",CopyFormat),("Apply Painter",PasteFormat),("Clear Formatting",ClearFormat),("Formatted Notes…",EditRichNotes),("Gantt Colors…",EditChartColors)));
        menus.Items.Add(Menu("AI",("ChatGPT / Claude…",ShowAi)));
        menus.Items.Add(Menu("Connectors",("Manage MCP Servers…",ShowMcpConnectors)));
        menus.Items.Add(Menu("Help",("All Project Commands…",AllCommands),("Planora Help",ShowHelp)));
        DockPanel.SetDock(menus,Dock.Top);root.Children.Add(menus);
        var header=new Grid{ColumnDefinitions=new ColumnDefinitions("Auto,*,Auto"),Background=RibbonModule.Chrome,Height=52};
        var quick=new StackPanel{Orientation=Orientation.Horizontal,Spacing=4,Margin=new Thickness(12,3)};
        quick.Children.Add(new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,Margin=new Thickness(0,0,8,0),Children={new BrandMark{VerticalAlignment=VerticalAlignment.Center},new TextBlock{Text=AppBrand.Name,FontSize=18,FontWeight=FontWeight.SemiBold,Foreground=RibbonModule.Ink,VerticalAlignment=VerticalAlignment.Center}}});
        foreach(var button in new[]{Action("▣",SaveCopy),Action("↶",()=>{UndoEdit();return Task.CompletedTask;}),Action("↷",()=>{RedoEdit();return Task.CompletedTask;})}){button.Background=Brushes.Transparent;button.Foreground=RibbonModule.Ink;button.BorderThickness=new Thickness(0);button.Padding=new Thickness(7,3);quick.Children.Add(button);}
        ToolTip.SetTip(quick.Children[1],"Save a copy");ToolTip.SetTip(quick.Children[2],"Undo");ToolTip.SetTip(quick.Children[3],"Redo");header.Children.Add(quick);
        _title.Foreground=RibbonModule.Ink;_title.FontSize=16;_title.HorizontalAlignment=HorizontalAlignment.Center;_title.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(_title,1);header.Children.Add(_title);
        var search=Action("⌕  Search commands…",AllCommands);search.Background=Brushes.Transparent;search.Foreground=RibbonModule.Ink;search.BorderThickness=new Thickness(0);search.Margin=new Thickness(0,3,12,3);Grid.SetColumn(search,2);header.Children.Add(search);DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var ribbon=_ribbon;_glassNavigation=new GlassNavigationSurface(ribbon);
        void UpdateOfficeTheme(object? sender,EventArgs args){bool dark=Application.Current?.ActualThemeVariant==ThemeVariant.Dark;RibbonModule.ApplyAppearance(dark);UpdateGlassNavigation();Gantt.InvalidateVisual();_timeline.RefreshAppearance();foreach(var control in this.GetVisualDescendants().OfType<Control>())control.InvalidateVisual();}
        UpdateOfficeTheme(null,EventArgs.Empty);if(Application.Current is {} application){application.ActualThemeVariantChanged+=UpdateOfficeTheme;Closed+=(_,_)=>application.ActualThemeVariantChanged-=UpdateOfficeTheme;}
        AddPlanningRibbon(ribbon);
        ribbon.AddTab("File",RibbonModule.Group("Project files",Action("New",NewProject),Action("Open MPP",OpenProject),Action("Save a copy",SaveCopy,true)));
        var fontPicker=new ComboBox{ItemsSource=FontManager.Current.SystemFonts.Select(f=>f.Name).Distinct().OrderBy(f=>f).ToArray(),SelectedItem=Sheet.Grid.FontFamily.Name,Width=150,MinHeight=25,FontSize=12};fontPicker.SelectionChanged+=async(_,_)=>{if(fontPicker.SelectedItem is string family){if(Sheet.SelectedUid.HasValue)await RunMenu(()=>FormatText(f=>f with{Family=family}));else{Sheet.Grid.FontFamily=new FontFamily(family);_preferences.FontFamilyName=family;Persist();}}};
        ribbon.AddTab("Task",
            RibbonModule.Group("View",RibbonModule.Large(Dropdown("Gantt Chart",("Gantt Chart",()=>Page(1)),("Task Sheet",()=>Page(0)),("Task Usage",()=>Page(6)),("Network Diagram",()=>Page(7)),("Calendar",()=>Page(11))),"▤","Gantt Chart ▾")),
            RibbonModule.Group("Clipboard",Action("▣  Paste",PasteCells),Action("▱  Copy",CopyCell),Action("Fill Down",FillDown)),
            RibbonModule.Group("Font",fontPicker,RibbonModule.Row(Action("B",()=>FormatText(f=>f with{Bold=f.Bold!=true})),Action("I",()=>FormatText(f=>f with{Italic=!f.Italic})),Action("U",()=>FormatText(f=>f with{Underline=f.Underline=="None"?"Single":"None"}))),Action("Text & color…",EditTextFormat)),
            RibbonModule.Group("Schedule",RibbonModule.Row(Action("0%",()=>Progress(0)),Action("25%",()=>Progress(25)),Action("50%",()=>Progress(50)),Action("75%",()=>Progress(75)),Action("100%",()=>Progress(100))),
                RibbonModule.Row(Action("→ Indent",()=>{_session.Indent(RequireTask());Refresh();return Task.CompletedTask;}),Action("← Outdent",()=>{_session.Outdent(RequireTask());Refresh();return Task.CompletedTask;})),
                RibbonModule.Row(Action("Link…",LinkTasks),Action("Unlink",()=>{_session.UnlinkTasks(SelectedTaskUids());Refresh();return Task.CompletedTask;}))),
            RibbonModule.Group("Tasks",Action("Manually Schedule",()=>TaskMode(true)),Action("Auto Schedule",()=>TaskMode(false)),Dropdown("Inspect",("Task Drivers",()=>Page(10)),("Task Information…",EditTask))),
            RibbonModule.Group("Insert",RibbonModule.Row(Action("＋ Task",()=>InsertTask(false)),Dropdown("More",("Task",()=>InsertTask(false)),("Milestone",()=>InsertTask(true)),("Insert Columns…",()=>Sheet.ChooseColumns(this)))),Action("◇ Milestone",()=>InsertTask(true))),
            RibbonModule.Group("Properties",Action("ⓘ  Information…",EditTask),Action("Notes…",EditRichNotes),Action("Add to Timeline",()=>{_timeline.AddSelection(SelectedTaskUids());return Task.CompletedTask;})),
            RibbonModule.Group("Editing",Dropdown("Find",("Find…",FindTask),("Replace…",ReplaceTaskNames),("Go To…",GoToTask)),Dropdown("Clear",("Delete Selected Tasks…",DeleteTasks),("Remove Task Links",()=>{_session.UnlinkTasks(SelectedTaskUids());Refresh();return Task.CompletedTask;})),Dropdown("Fill",("Fill Down",FillDown)),Action("Scroll to Task",ScrollToSelectedTask)));
        ribbon.AddTab("Resource",RibbonModule.Group("Resource views",Action("Resource Sheet",()=>{SelectPage(2);return Task.CompletedTask;}),Action("Resource Usage",()=>{SelectPage(3);return Task.CompletedTask;})),
            RibbonModule.Group("Resource Graph",Action("Resource Graph",()=>{SelectPage(8);return Task.CompletedTask;})),
            RibbonModule.Group("Resources",Action("Add resource…",AddResource),Action("Resource information…",EditResource),Action("Assign resources…",AssignResource),Action("Delete resource",DeleteResource)));
        ribbon.AddTab("Report",RibbonModule.Group("View Reports",Action("Project Overview",()=>ShowReport(ProjectReportKind.ProjectOverview)),Action("Cost Overview",()=>ShowReport(ProjectReportKind.CostOverview)),Action("Work Overview",()=>ShowReport(ProjectReportKind.WorkOverview))),RibbonModule.Group("Export",Action("Export report…",ExportReport)));
        ribbon.AddTab("Project",RibbonModule.Group("Properties",Action("Project information…",ProjectInformation),Action("Change working time…",ChangeWorkingTime),Action("Calendar exceptions…",CalendarExceptions)),
            RibbonModule.Group("Schedule",Action("Calculate Project",CalculateProject)),
            RibbonModule.Group("Tracking",Action("Set baseline…",SetBaseline)),RibbonModule.Group("File",Action("File information",()=>{SelectPage(5);return Task.CompletedTask;})));
        var timelineToggle=new CheckBox{Content="Timeline",IsChecked=_preferences.ShowTimeline};timelineToggle.IsCheckedChanged+=(_,_)=>SetTimelineVisible(timelineToggle.IsChecked==true);
        var sidebarToggle=new CheckBox{Content="View bar",IsChecked=_preferences.ShowSidebar};sidebarToggle.IsCheckedChanged+=(_,_)=>{_preferences.ShowSidebar=sidebarToggle.IsChecked==true;SetSidebar();Persist();};
        var arrows=new CheckBox{Content="Dependencies",IsChecked=true};arrows.IsCheckedChanged+=(_,_)=>{Gantt.ShowDependencies=arrows.IsChecked==true;Gantt.InvalidateVisual();};
        var baseline=new CheckBox{Content="Baseline",IsChecked=true};baseline.IsCheckedChanged+=(_,_)=>{Gantt.ShowBaseline=baseline.IsChecked==true;Gantt.InvalidateVisual();};
        var find=_find;var filter=_taskFilter;
        find.TextChanged+=(_,_)=>Sheet.SetFilter(find.Text??"",filter.SelectedItem?.ToString()??"All tasks");filter.SelectionChanged+=(_,_)=>Sheet.SetFilter(find.Text??"",filter.SelectedItem?.ToString()??"All tasks");
        ribbon.AddTab("View",RibbonModule.Group("Task views",Action("Gantt Chart",()=>{SelectPage(1);return Task.CompletedTask;}),Action("Task Sheet",()=>{SelectPage(0);return Task.CompletedTask;}),Action("Task Usage",()=>{SelectPage(6);return Task.CompletedTask;}),Action("Network Diagram",()=>{SelectPage(7);return Task.CompletedTask;}),Action("Calendar",()=>{SelectPage(11);return Task.CompletedTask;}),Action("Progress Board",()=>{SelectPage(12);return Task.CompletedTask;})),
            RibbonModule.Group("Columns",Action("Insert Column…",()=>Sheet.ChooseColumns(this)),Dropdown("Tables",("Entry",()=>TablePreset("Entry")),("Cost",()=>TablePreset("Cost")),("Work",()=>TablePreset("Work")),("Tracking",()=>TablePreset("Tracking")),("Variance",()=>TablePreset("Variance")),("Relationships",()=>TablePreset("Relationships"))),Action("Reset Table",()=>{Sheet.ResetColumns();return Task.CompletedTask;})),
            RibbonModule.Group("Data",find,filter,Action("Sort By…",SortRows),Action("Show / hide subtasks",()=>{Sheet.ToggleSubtasks();return Task.CompletedTask;}),Action("Expand All",()=>{Sheet.ExpandAll();return Task.CompletedTask;})),
            RibbonModule.Group("Show / hide",timelineToggle,sidebarToggle,arrows,baseline));
        ribbon.AddTab("Text Format",RibbonModule.Group("Character",Action("Text & color…",EditTextFormat),Action("Font Color…",()=>PickTextColor(false)),Action("Highlight…",()=>PickTextColor(true))),RibbonModule.Group("Clipboard",Action("Format Painter",CopyFormat),Action("Apply Painter",PasteFormat),Action("Clear Formatting",ClearFormat)),RibbonModule.Group("Alignment",Action("Align Left",()=>FormatText(f=>f with{Alignment="Left"})),Action("Center",()=>FormatText(f=>f with{Alignment="Center"})),Action("Align Right",()=>FormatText(f=>f with{Alignment="Right"}))),RibbonModule.Group("Styles",Dropdown("Styles",("Normal",()=>StylePreset("Normal")),("Heading",()=>StylePreset("Heading")),("Emphasis",()=>StylePreset("Emphasis")),("Important",()=>StylePreset("Important")),("Completed",()=>StylePreset("Completed"))),Action("Save Style…",SaveTextStyle),Action("Apply Saved Style…",ApplySavedStyle),Action("Gantt Color…",PickBarColor)),RibbonModule.Group("Case",Dropdown("Change Case",("UPPERCASE",()=>ChangeCase("upper")),("lowercase",()=>ChangeCase("lower")),("Title Case",()=>ChangeCase("title")),("Sentence case",()=>ChangeCase("sentence")),("tOGGLE cASE",()=>ChangeCase("toggle")))));
        ribbon.AddTab("Gantt Chart Format",RibbonModule.Group("Gantt Chart",Action("Layout / Text Styles…",FormatView),Action("Bar Styles…",EditChartColors),Action("Timescale…",ZoomDialog)),RibbonModule.Group("Table",Action("Entry",()=>TablePreset("Entry")),Action("Cost",()=>TablePreset("Cost")),Action("Work",()=>TablePreset("Work")),Action("Tracking",()=>TablePreset("Tracking"))));
        ribbon.AddTab("Timeline",RibbonModule.Group("Tasks",Action("Add selected tasks",()=>{_timeline.AddSelection(SelectedTaskUids());return Task.CompletedTask;}),Action("Remove selected tasks",()=>{_timeline.RemoveSelection(SelectedTaskUids());return Task.CompletedTask;})),
            RibbonModule.Group("Timelines",Action("Add Timeline",()=>{_timeline.AddTimeline();return Task.CompletedTask;}),Action("Remove active timeline",()=>{_timeline.RemoveTimeline(_timeline.ActiveId);return Task.CompletedTask;})),RibbonModule.Group("Date zoom",Action("Zoom in",()=>{_timeline.Axis.ZoomIn();return Task.CompletedTask;}),Action("Zoom out",()=>{_timeline.Axis.ZoomOut();return Task.CompletedTask;}),Action("Entire Project",()=>{_timeline.Axis.EntireProject();return Task.CompletedTask;}),Action("Zoom / Dates…",()=>_timeline.EditDates())));
        ribbon.AddTab("AI",RibbonModule.Group("Planora AI",Action("ChatGPT / Claude",ShowAi,true)));
        ribbon.AddTab("Connectors",RibbonModule.Group("MCP connections",Action("Manage MCP Servers",ShowMcpConnectors,true)));
        ribbon.AddTab("Help",RibbonModule.Group("Commands",Action("All Project Commands…",AllCommands),Action("Go To…",GoToTask),Action("Replace…",ReplaceTaskNames)),RibbonModule.Group("Planora",Action("Help",ShowHelp)));
        AddAppearanceControls(menus,ribbon);InstallNativeMenu(menus);
        ribbon.SelectTab("Planning");DockPanel.SetDock(_glassNavigation,Dock.Top);root.Children.Add(_glassNavigation);Activated+=(_,_)=>UpdateGlassNavigation();
        var workspaceNavigation=BuildWorkspaceNavigation();DockPanel.SetDock(workspaceNavigation,Dock.Top);root.Children.Add(workspaceNavigation);
        _workspace.Margin=new Thickness(12,8,12,0);_workspace.Children.Add(_timeline);Grid.SetRow(_timelineDivider,1);_workspace.Children.Add(_timelineDivider);
        var sidebarContent=new StackPanel{Margin=new Thickness(8),Spacing=4};var names=new[]{"Task Sheet","Gantt Chart","Resource Sheet","Resource Usage","Task information","File information","Task Usage","Network Diagram","Resource Graph","Reports","Task Drivers"};
        names=names.Concat(new[]{"Calendar","Progress Board"}).ToArray();for(int i=0;i<names.Length;i++){int index=i;var b=Action(names[i],()=>{SelectPage(index);return Task.CompletedTask;});b.Classes.Add("navigation");b.HorizontalAlignment=HorizontalAlignment.Stretch;sidebarContent.Children.Add(b);_navigation.Add(b);}
        _sidebar=new Border{Background=DesignTokens.Sidebar,CornerRadius=new CornerRadius(8),Margin=new Thickness(0,0,10,0),Child=sidebarContent};_main.Children.Add(_sidebar);
        var body=new DockPanel();var viewHeading=new DockPanel{Margin=new Thickness(12,6),LastChildFill=true};_viewTitle.FontSize=14;_viewTitle.Margin=new Thickness(0,4);_viewTitle.Foreground=DesignTokens.Text;var formatInspector=Action("Aa  Format",EditTextFormat);formatInspector.MinHeight=28;formatInspector.Padding=new Thickness(10,3);DockPanel.SetDock(formatInspector,Dock.Right);viewHeading.Children.Add(formatInspector);viewHeading.Children.Add(_viewTitle);DockPanel.SetDock(viewHeading,Dock.Top);body.Children.Add(viewHeading);body.Children.Add(_page);
        var surface=new Border{Background=DesignTokens.Surface,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),ClipToBounds=true,Child=body};Grid.SetColumn(surface,1);_main.Children.Add(surface);
        _inspectorHost=new Border{Child=_inspector,Background=DesignTokens.Surface,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(1),CornerRadius=DesignTokens.Radius,Margin=new Thickness(8,0,0,0)};Grid.SetColumn(_inspectorHost,3);_main.Children.Add(_inspectorHost);
        _inspectorDivider=new GridSplitter{Width=6,ResizeDirection=GridResizeDirection.Columns,Background=Brushes.Transparent};Grid.SetColumn(_inspectorDivider,2);_main.Children.Add(_inspectorDivider);
        _inspectorDivider.DragCompleted+=(_,_)=>SetInspectorWidth(_main.ColumnDefinitions[3].ActualWidth);
        Grid.SetRow(_main,2);_workspace.Children.Add(_main);var statusBar=new DockPanel{Background=RibbonModule.Chrome};_status.Foreground=RibbonModule.Ink;var zoom=new StackPanel{Orientation=Orientation.Horizontal,Spacing=2};foreach(var b in new[]{Action("−",()=>{_axis.ZoomOut();return Task.CompletedTask;}),Action("Fit",()=>{_axis.Fit();return Task.CompletedTask;}),Action("+",()=>{_axis.ZoomIn();return Task.CompletedTask;})}){b.Background=Brushes.Transparent;b.Foreground=RibbonModule.Ink;b.BorderThickness=new Thickness(0);b.MinHeight=26;b.Padding=new Thickness(8,2);zoom.Children.Add(b);}DockPanel.SetDock(zoom,Dock.Right);statusBar.Children.Add(zoom);var statusMessages=new StackPanel();statusMessages.Children.Add(_status);statusMessages.Children.Add(_recoveryStatus);statusBar.Children.Add(statusMessages);DockPanel.SetDock(statusBar,Dock.Bottom);root.Children.Add(statusBar);root.Children.Add(_workspace);Content=root;
        _timelineDivider.PointerReleased+=(_,_)=>{if(_timeline.IsVisible){_preferences.TimelineHeight=Math.Clamp(_workspace.RowDefinitions[0].ActualHeight,200,900);Persist();}};
        foreach(var (label,key)in new[]{("Resource Name","Name"),("Type","Type"),("Initials","Initials"),("Group","Group"),("Max Units","Capacity"),("Standard Rate","Rate"),("Overtime Rate","OvertimeRate"),("Cost Per Use","CostPerUse"),("Work","Work"),("Cost","Cost")}){var c=new FormattedSheetColumn(key,"Resource",_resources,_formats){Header=label,Binding=new Avalonia.Data.Binding("Cells["+key+"]"),Width=new DataGridLength(140)};c.Tag=key;c.IsReadOnly=key is "Work" or "Cost";_resources.Columns.Add(c);}
        foreach(var (label,key)in new[]{("Task Name","Task"),("Resource Name","Resource"),("Units","Units"),("Work","Work"),("Actual Work","Actual"),("Cost","Cost")}){var c=new FormattedSheetColumn(key,"Assignment",_assignments,_formats){Header=label,Binding=new Avalonia.Data.Binding("Cells["+key+"]"),Width=new DataGridLength(180)};c.Tag=key;c.IsReadOnly=key is not ("Units" or "Work");_assignments.Columns.Add(c);}
        EditableSheetModule.Attach(_resources,_session.ResourceText,_session.EditResourceText,RefreshResourceSheetValues,message=>_status.Text=message);
        EditableSheetModule.Attach(_assignments,_session.AssignmentText,_session.EditAssignmentText,(_,_)=>RefreshSheetValues(),message=>_status.Text=message);
        RegisterCommands();
        SetSidebar();Refresh();SelectPage(1);
        Closing+=async(_,e)=>await HandleWindowClosing(e);
        InitializeRecovery(settingsPath==null);
        AddHandler(InputElement.KeyDownEvent,async(_,e)=>{
            if(_busy)return;
            bool primary=e.KeyModifiers.HasFlag(OperatingSystem.IsMacOS()?KeyModifiers.Meta:KeyModifiers.Control);
            try{
                if(e.Key==Key.F11){e.Handled=true;SetPlanningFocus(!_planningFocus);return;}
                if(primary&&e.Key==Key.S){e.Handled=true;await RunMenu(SaveCopy);return;}
                if(primary&&!e.KeyModifiers.HasFlag(KeyModifiers.Shift)&&e.Key==Key.V&&e.Source is TextBox editor&&editor.Name=="CellTextBox"&&_currentPage<=1){e.Handled=true;await PasteFromEditor(editor);return;}
                if(primary&&e.Key is Key.B or Key.I or Key.U){e.Handled=true;await RunMenu(()=>FormatText(f=>e.Key==Key.B?f with{Bold=f.Bold!=true}:e.Key==Key.I?f with{Italic=!f.Italic}:f with{Underline=f.Underline=="None"?"Single":"None"}));return;}
                if(primary&&e.KeyModifiers.HasFlag(KeyModifiers.Shift)&&e.Key is Key.C or Key.V){e.Handled=true;await RunMenu(e.Key==Key.C?CopyFormat:PasteFormat);return;}
                if(e.Source is TextBox)return;
                if(e.KeyModifiers==(KeyModifiers.Alt|KeyModifiers.Shift)&&e.Key is Key.Left or Key.Right){e.Handled=true;await RunMenu(()=>{if(e.Key==Key.Right)_session.Indent(RequireTask());else _session.Outdent(RequireTask());Refresh();return Task.CompletedTask;});return;}
                if(e.Key==Key.F3&&e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift){e.Handled=true;Sheet.SetFilter("","All tasks");_find.Text="";_taskFilter.SelectedIndex=0;if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))Sheet.SortRows("Project order",false);return;}
                if(e.Key==Key.F2&&e.KeyModifiers.HasFlag(KeyModifiers.Shift)){e.Handled=true;await RunMenu(_currentPage==2?EditResource:EditTask);return;}
                if(primary&&e.Key==Key.F5&&e.KeyModifiers.HasFlag(KeyModifiers.Shift)){e.Handled=true;await RunMenu(ScrollToSelectedTask);return;}
                if(!primary)return;
                e.Handled=true;
                if(e.Key==Key.C)await RunMenu(CopyCell);
                else if(e.Key==Key.V)await RunMenu(PasteCells);
                else if(e.Key==Key.D)await RunMenu(FillDown);
                else if(e.Key==Key.O)await RunMenu(OpenProject);
                else if(e.Key==Key.N)await RunMenu(NewProject);
                else if(e.Key==Key.H)await RunMenu(ReplaceTaskNames);
                else if(e.Key==Key.G)await RunMenu(GoToTask);
                else if(e.Key==Key.F)await RunMenu(FindTask);
                else if(e.Key==Key.P&&e.KeyModifiers.HasFlag(KeyModifiers.Shift))await RunMenu(AllCommands);
                else if(e.Key==Key.P)await RunMenu(ExportPdf);
                else if(e.Key==Key.I)await RunMenu(()=>Sheet.ChooseColumns(this));
                else if(e.Key==Key.Z)await RunMenu(()=>{UndoEdit();return Task.CompletedTask;});
                else if(e.Key==Key.Y)await RunMenu(()=>{RedoEdit();return Task.CompletedTask;});
                else e.Handled=false;
            }catch(Exception error){_status.Text=error.Message;}
        },Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    private void UpdateFormattedRowHeights(){void Apply(DataGrid grid,string entity,double minimum){var formats=_formats.Document.Cells.Where(p=>p.Key.StartsWith(entity+"/",StringComparison.Ordinal)).Select(p=>p.Value).ToArray();grid.RowHeight=formats.Any(f=>f.Wrap)?double.NaN:Math.Max(minimum,formats.Select(f=>f.Size??0).DefaultIfEmpty(0).Max()+8);}Apply(Sheet.Grid,"Task",_preferences.RowHeight);Apply(_resources,"Resource",28);Apply(_assignments,"Assignment",28);}
    public void UpdateGlassNavigation(){if(_glassNavigation is null)return;_glassNavigation.SetAppearance(Application.Current?.ActualThemeVariant==ThemeVariant.Dark,_preferences.ReduceTransparency||GlassNavigationSurface.SystemRequestsSolid(this));}
    public void SelectPreviewTab(int index){Sheet.Grid.SelectedIndex=0;SelectPage(index);}
    public void CloseForValidation(){_closeApproved=true;Close();}
    private int[] SelectedTaskUids()=>Sheet.Grid.SelectedItems.OfType<TaskTableRow>().Where(t=>t.Uid>0).Select(t=>t.Uid).ToArray();
    private int RequireTask()=>Sheet.SelectedUid??throw new InvalidOperationException("Select a task first.");
    private void SelectUid(int uid){if(Sheet.Grid.ItemsSource is IEnumerable<TaskTableRow> rows)Sheet.Grid.SelectedItem=rows.FirstOrDefault(t=>t.Uid==uid);}
    private async Task NewProject(){if(!await ResolveUnsavedChanges())return;_session.NewProject("Untitled Project",DateTime.Today.AddHours(8));Refresh();SelectPage(1);await ProjectInformation();}
    private void RegisterCommands(){
        void Add(Func<Task> action,params int[] ids){foreach(int id in ids)_commandActions[id]=action;}
        Func<Task> Do(System.Action action)=>()=>{action();return Task.CompletedTask;};
        Add(NewProject,18,2520);Add(OpenProject,23);Add(SaveCopy,3,748,50512);Add(CloseProject,106);Add(ExitApplication,752);Add(Do(()=>SelectPage(5)),750);
        Add(Do(UndoEdit),128);Add(Do(RedoEdit),129);Add(CopyCell,19);Add(PasteCells,22);Add(FillDown,372);Add(DeleteTasks,478,50143);
        Add(LinkTasks,2169);Add(Do(()=>{_session.UnlinkTasks(SelectedTaskUids());Refresh();}),2137);Add(GoToTask,757,2168);Add(ReplaceTaskNames,313);
        Add(FindTask,141);
        Add(()=>InsertTask(false),296);Add(()=>Sheet.ChooseColumns(this),297);Add(EditTask,2478);Add(()=>EditField("Notes"),1758);
        Add(Do(()=>{_session.Indent(RequireTask());Refresh();}),133);Add(Do(()=>{_session.Outdent(RequireTask());Refresh();}),132);
        Add(Do(()=>Sheet.SetSubtasks(true)),137);Add(Do(()=>Sheet.SetSubtasks(false)),138);Add(Do(Sheet.ExpandAll),2134);for(int i=1;i<=9;i++){int depth=i;Add(Do(()=>Sheet.SetOutlineLevel(depth)),70+i);}
        Add(Do(()=>{_session.RenumberWbs();Refresh();}),50302);Add(()=>EditField("Wbs"),50303);Add(ProjectInformation,50198);Add(ChangeWorkingTime,50191);Add(AssignResource,2131);Add(EditResource,2132);
        Add(SetBaseline,50203);Add(()=>SetBaseline(true),50309);Add(EditTask,2129);Add(Do(()=>SelectPage(10)),50516);Add(()=>ShowReport(ProjectReportKind.ProjectOverview),2130,591);
        Add(FormatView,253,50317,50221,50241,50227,50180);Add(Do(()=>_axis.ZoomIn()),444);Add(Do(()=>_axis.ZoomOut()),445);Add(ZoomDialog,925,50277);
        Add(SortRows,928);
        Add(Do(()=>TablePreset("Entry")),2098);Add(Do(()=>TablePreset("Cost")),2096);Add(Do(()=>TablePreset("Work")),2102);Add(Do(()=>TablePreset("Tracking")),2101,2099);Add(Do(()=>TablePreset("Relationships")),2100);
        Add(Do(()=>{_preferences.ShowSidebar=!_preferences.ShowSidebar;SetSidebar();Persist();}),50293);Add(ChooseView,50291);Add(ChooseTable,50275);Add(Do(()=>{_network.Refresh(_session);SelectPage(7);}),50223);
        Add(ShowHelp,984,927);for(int i=0;i<5;i++){int progress=i*25;Add(()=>Progress(progress),2118+i);}
    }
    private async Task AllCommands(){var selected=await new ProjectCommandsWindow(_commandActions).ShowDialog<int?>(this);if(selected is int id&&_commandActions.TryGetValue(id,out var action))await action();}
    private Task FindTask(){SelectPage(1);_ribbon.SelectTab("View");_find.Focus();return Task.CompletedTask;}
    private Task InsertTask(bool milestone){
        if(_currentPage>1)SelectPage(1);
        Sheet.PrepareInsertion();_find.Text="";_taskFilter.SelectedIndex=0;Sheet.InsertBlankRow(milestone);
        _status.Text="Blank row inserted. Type the task name; Enter or Tab keeps it.";return Task.CompletedTask;
    }
    private async Task ChooseView(){var names=new[]{"Gantt Chart","Task Sheet","Task Usage","Network Diagram","Resource Sheet","Resource Usage","Resource Graph","Reports","Task Drivers","Calendar","Progress Board"};var name=await new ChooseGuiItemWindow("More Views",names).ShowDialog<string?>(this);if(name==null)return;SelectPage(name switch{"Task Sheet"=>0,"Task Usage"=>6,"Network Diagram"=>7,"Resource Sheet"=>2,"Resource Usage"=>3,"Resource Graph"=>8,"Reports"=>9,"Task Drivers"=>10,"Calendar"=>11,"Progress Board"=>12,_=>1});}
    private async Task ChooseTable(){var name=await new ChooseGuiItemWindow("More Tables",new[]{"Entry","Cost","Work","Tracking","Variance","Relationships"}).ShowDialog<string?>(this);if(name!=null)await TablePreset(name);}
    private async Task ZoomDialog(){var value=await new DateRangeWindow("Gantt · Zoom and dates",_axis).ShowDialog<DateAxisView?>(this);if(value!=null)_axis.Restore(value);}
    private async Task SortRows(){var value=await new SortEditorWindow().ShowDialog<GuiSortEdit?>(this);if(value!=null)Sheet.SortRows(value.Field,value.Descending);}
    private async Task CopyCell(){if(Clipboard==null)throw new InvalidOperationException("The clipboard is not available.");string? value;if(_currentPage is 2 or 3){var grid=_currentPage==2?_resources:_assignments;if(grid.SelectedItem is not SheetRecord row||grid.CurrentColumn?.Tag is not string key)throw new InvalidOperationException("Select a cell to copy.");value=row.Cells.GetValueOrDefault(key);}else{if(_currentPage>1)throw new InvalidOperationException("Copy cells from the task, resource or assignment sheet.");int uid=RequireTask();string key=Sheet.CurrentKey??"Name";value=ProjectSession.CanEditText(key)?_session.EditText(uid,key):Sheet.Catalogue.First(c=>c.Key==key).Read(_session.Document!.Tasks.GetByUid(uid),_session.Document);}await Clipboard.SetTextAsync(value??"");_status.Text="Copied selected cell.";}
    private async Task PasteCells(){if(Clipboard==null)throw new InvalidOperationException("The clipboard is not available.");var text=await Clipboard.TryGetTextAsync();if(string.IsNullOrEmpty(text))return;if(_currentPage is 2 or 3){if(text.TrimEnd('\r','\n').Contains('\n')||text.Contains('\t'))throw new ArgumentException("Resource and assignment sheets currently accept one pasted cell at a time.");var grid=_currentPage==2?_resources:_assignments;if(grid.SelectedItem is not SheetRecord row||grid.CurrentColumn is not {IsReadOnly:false,Tag:string key})throw new InvalidOperationException("Select an editable cell.");if(_currentPage==2)_session.EditResourceText(row.Uid,key,text.TrimEnd('\r','\n'));else _session.EditAssignmentText(row.Uid,key,text.TrimEnd('\r','\n'));Refresh();return;}if(_currentPage>1)throw new InvalidOperationException("Paste cells into a task, resource or assignment sheet.");ApplyPasteText(text);}
    public void ApplyPasteText(string text){
        var row=Sheet.Grid.SelectedItem as TaskTableRow??throw new InvalidOperationException("Select the destination cell.");
        var uids=_session.PasteTasks(row.Uid>0?row.Uid:null,Sheet.VisibleKeys,Sheet.CurrentKey??"Name",text,Sheet.HasPendingInsertion?Sheet.PendingInsertionBeforeUid:null,Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Where(r=>r.Uid>0).Select(r=>r.Uid).ToArray());
        Sheet.DiscardSelectedDraft();Refresh();if(uids.Count>0)Sheet.FocusName(uids[0],false);_status.Text=$"Pasted {uids.Count} task rows. One Undo restores the block.";
    }
    private async Task PasteFromEditor(TextBox editor){
        var text=Clipboard==null?null:await Clipboard.TryGetTextAsync();if(string.IsNullOrEmpty(text))return;
        if(text.Contains('\n')||text.Contains('\t')){ApplyPasteText(text);Sheet.Grid.CancelEdit();return;}
        int from=Math.Min(editor.SelectionStart,editor.SelectionEnd),to=Math.Max(editor.SelectionStart,editor.SelectionEnd);string original=editor.Text??"";from=Math.Clamp(from,0,original.Length);to=Math.Clamp(to,from,original.Length);editor.Text=original[..from]+text+original[to..];editor.CaretIndex=from+text.Length;editor.SelectionStart=editor.SelectionEnd=editor.CaretIndex;
    }
    private Task ScrollToSelectedTask(){int uid=RequireTask();if(_currentPage!=1)SelectPage(1);Sheet.Grid.ScrollIntoView(Sheet.Grid.SelectedItem,Sheet.Grid.CurrentColumn);_planning.ScrollToTask(uid);return Task.CompletedTask;}
    private Task FillDown(){if(_currentPage>1)throw new InvalidOperationException("Fill Down currently applies to task rows.");string key=Sheet.CurrentKey??throw new InvalidOperationException("Select a column.");if(!ProjectSession.CanEditText(key))throw new InvalidOperationException("This column is read-only.");var selected=Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Where(r=>Sheet.Grid.SelectedItems.Contains(r)&&r.Uid>0).ToArray();if(selected.Length<2)throw new InvalidOperationException("Select at least two task rows.");string text=_session.EditText(selected[0].Uid,key);_session.EditCells(selected.Skip(1).Select(r=>new TextCellChange(r.Uid,key,text)),"Fill Down");Refresh();return Task.CompletedTask;}
    private async Task GoToTask(){int? id=await new GoToTaskWindow().ShowDialog<int?>(this);if(id==null)return;var task=_session.Tasks.FirstOrDefault(t=>t.DisplayId==id)??throw new ArgumentException("This task ID was not found.");Sheet.SetFilter("","All tasks");Sheet.ExpandAll();SelectPage(1);SelectUid(task.Uid);Sheet.Grid.ScrollIntoView(Sheet.Grid.SelectedItem,Sheet.Grid.Columns.FirstOrDefault());}
    private async Task ReplaceTaskNames(){var edit=await new FindReplaceWindow().ShowDialog<FindReplaceEdit?>(this);if(edit==null)return;var comparison=edit.MatchCase?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase;var changes=_session.Tasks.Where(t=>(t.Name??"").Contains(edit.Find,comparison)).Select(t=>new TextCellChange(t.Uid,"Name",(t.Name??"").Replace(edit.Find,edit.Replacement,comparison))).ToArray();_session.EditCells(changes,"Replace task names");Refresh();_status.Text=$"Replaced text in {changes.Length} task names.";}
    private async Task FormatView(){var current=new GuiFormatEdit(Sheet.Grid.FontSize,double.IsNaN(Sheet.Grid.RowHeight)?_preferences.RowHeight:Sheet.Grid.RowHeight,Sheet.Grid.GridLinesVisibility!=DataGridGridLinesVisibility.None,Gantt.ShowCritical,Gantt.ShowBaseline,Gantt.BaselineNumber);var edit=await new GuiFormatWindow(current).ShowDialog<GuiFormatEdit?>(this);if(edit==null)return;Sheet.Grid.FontSize=edit.FontSize;Sheet.Grid.RowHeight=edit.RowHeight;Sheet.Grid.GridLinesVisibility=edit.Gridlines?DataGridGridLinesVisibility.All:DataGridGridLinesVisibility.None;Gantt.ShowCritical=edit.Critical;Gantt.ShowBaseline=edit.Baseline;Gantt.BaselineNumber=edit.BaselineNumber;_preferences.FontSize=edit.FontSize;_preferences.RowHeight=edit.RowHeight;_preferences.Gridlines=edit.Gridlines;_preferences.CriticalTasks=edit.Critical;_preferences.BaselineNumber=edit.BaselineNumber;Persist();Gantt.InvalidateVisual();}
    private Task TablePreset(string name){var keys=name switch{"Cost"=>new[]{"DisplayId","Name","FixedCost","Cost","ActualCost","RemainingCost","Baseline0.Cost"},"Work"=>new[]{"DisplayId","Name","Work","ActualWork","RemainingWork","PercentWorkComplete","ResourceNames"},"Tracking"=>new[]{"DisplayId","Name","PercentComplete","Start","Finish","ActualStart","ActualFinish","ActualDuration","RemainingDuration"},"Variance"=>new[]{"DisplayId","Name","Start","Finish","Baseline0.Start","Baseline0.Finish","Cost","Baseline0.Cost"},"Relationships"=>new[]{"DisplayId","Name","Predecessors","Successors","ConstraintType","ConstraintDate"},_=>TaskColumns.Defaults};Sheet.ApplyColumns(keys);Sheet.Persist();SelectPage(1);return Task.CompletedTask;}
    private Task ShowReport(ProjectReportKind kind){_reports.SelectedReport=kind;SelectPage(9);return Task.CompletedTask;}
    private async Task ExportReport(){if(_session.Document==null)throw new InvalidOperationException("Open a project first.");var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title="Export project report",DefaultExtension="html",SuggestedFileName="Planora-report.html",FileTypeChoices=[new FilePickerFileType("HTML report"){Patterns=["*.html"]}]});if(file?.TryGetLocalPath()is string path){using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write);using var writer=new StreamWriter(output,System.Text.Encoding.UTF8);writer.Write(_reports.ToHtml(_session));_status.Text="Report exported: "+path;}}
    private async Task CloseProject(){if(!await ResolveUnsavedChanges())return;_session.CloseProject();Refresh();SelectPage(1);}
    private async Task ExitApplication(){if(!await ResolveUnsavedChanges())return;_closeApproved=true;Close();}
    private async Task ShowHelp(){var window=new Window{Title="Planora Help",Width=670,Height=400,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=new TextBlock{Margin=new Thickness(24),TextWrapping=TextWrapping.Wrap,Text="Edit a task cell with double-click or F2. Enter/Tab commits; Esc cancels. Type a name in the blank row to add a task.\n\nUse Task for clipboard, outline, links and progress; Resource for assignments; Report for project/cost/work overviews; Project for calendars, baselines and calculation; View and Format for the display.\n\nImported resource totals are preserved during task-only calculation. Save a copy keeps the original MPP protected. Some advanced commands are still unavailable; All Project Commands lists their status."}};await window.ShowDialog(this);}
    private async Task EditTask(){int uid=RequireTask();var value=await new TaskEditorWindow(_session.Document!,uid).ShowDialog<TaskEdit?>(this);if(value!=null){_session.EditTask(uid,value);Refresh();SelectUid(uid);}}
    private async Task EditField(string key){int uid=RequireTask();if(key is "Predecessors" or "ResourceNames"){Sheet.Grid.BeginEdit();return;}var column=Sheet.Catalogue.First(c=>c.Key==key);var value=await new TaskFieldEditorWindow(_session.Document!.Tasks.GetByUid(uid),key,column.Title).ShowDialog<TaskFieldChange?>(this);if(value!=null){_session.EditField(uid,value);Refresh();SelectUid(uid);}}
    private async Task CalculateProject(){var value=await new CalculationEditorWindow(_session.SourcePath!=null).ShowDialog<CalculationEdit?>(this);if(value!=null){_session.Recalculate(value.ActualCosts,value.Assignments);Refresh();}}
    private async Task DeleteTasks(){var ids=SelectedTaskUids();if(ids.Length==0)return;if(await Confirm("Delete the selected tasks, their child tasks, links and assignments?")){_session.DeleteTasks(ids);Refresh();}}
    private Task Progress(int value){var ids=SelectedTaskUids();if(ids.Length==0)throw new InvalidOperationException("Select the tasks to update.");_session.SetProgress(ids,value);Refresh();return Task.CompletedTask;}
    private async Task LinkTasks(){var ids=SelectedTaskUids();var edit=await new DependencyEditorWindow(_session.Tasks,ids.FirstOrDefault(),ids.Skip(1).FirstOrDefault()).ShowDialog<DependencyEdit?>(this);if(edit!=null){_session.LinkTasks(edit.PredecessorUid,edit.SuccessorUid,edit.Type,edit.Lag);Refresh();}}
    private async Task AddResource(){if(_session.Document==null)throw new InvalidOperationException("Open or create a project first.");var value=await new ResourceEditorWindow(_session.Document).ShowDialog<ResourceEdit?>(this);if(value!=null){int uid=_session.AddResource(value.Name,value.Type??ProjectResourceType.Work,value.MaxUnitsPercent,value.StandardRate);_session.EditResource(uid,value);Refresh();SelectPage(2);}}
    private int SelectedResourceUid()=>_resources.SelectedItem?.GetType().GetProperty("Uid")?.GetValue(_resources.SelectedItem)is int uid?uid:throw new InvalidOperationException("Select a resource in the Resource Sheet first.");
    private async Task EditResource(){int uid=SelectedResourceUid();var value=await new ResourceEditorWindow(_session.Document!,_session.Document!.Resources.GetByUid(uid)).ShowDialog<ResourceEdit?>(this);if(value!=null){_session.EditResource(uid,value);Refresh();}}
    private async Task DeleteResource(){int uid=SelectedResourceUid();if(await Confirm("Delete the resource and its assignments?")){_session.DeleteResource(uid);Refresh();}}
    private Task AssignResource()=>AssignResource(null);
    private async Task AssignResource(int? resourceUid){int uid=RequireTask();var value=await new AssignmentEditorWindow(_session.Document!,uid,resourceUid).ShowDialog<AssignmentEdit?>(this);if(value!=null){if(value.Remove)_session.Unassign(uid,value.ResourceUid);else _session.Assign(uid,value.ResourceUid,value.UnitsPercent,value.WorkHours);Refresh();}}
    private Task SetBaseline()=>SetBaseline(false);
    private async Task SetBaseline(bool clear){if(_session.Document==null)throw new InvalidOperationException("Open a project first.");var value=await new BaselineEditorWindow(clear).ShowDialog<BaselineEdit?>(this);if(value!=null){if(value.Clear)_session.ClearBaseline(value.Number);else _session.CaptureBaseline(value.Number,value.Overwrite);Refresh();}}
    private async Task ChangeWorkingTime(){if(_session.Document==null)throw new InvalidOperationException("Open a project first.");var value=await new CalendarEditorWindow(_session.Document).ShowDialog<CalendarEdit?>(this);if(value!=null){_session.SetWorkingWeek(value.CalendarUid,value.Days);Refresh();}}
    private async Task CalendarExceptions(){if(_session.Document==null)throw new InvalidOperationException("Open a project first.");var value=await new CalendarExceptionEditorWindow(_session.Document).ShowDialog<CalendarExceptionEdit?>(this);if(value!=null){_session.SetCalendarException(value);Refresh();}}
    private async Task ProjectInformation(){if(_session.Document==null)throw new InvalidOperationException("Open a project first.");var value=await new ProjectInformationWindow(_session.Document).ShowDialog<ProjectInformationEdit?>(this);if(value!=null){_session.RenameProject(value.Name);_session.SetProjectDates(value.Start,value.Status,value.CalendarUid);Refresh();}}
    public void SetTimelineVisible(bool visible){_preferences.ShowTimeline=visible;UpdateTimelineVisibility();Persist();}
    private void SetSidebar(){_sidebar.IsVisible=_preferences.ShowSidebar&&!_planningFocus;_main.ColumnDefinitions[0].Width=new GridLength(_preferences.ShowSidebar&&!_planningFocus?190:0);}
    private void UpdateTimelineVisibility(){if(_timelineToggle!=null){_timelineToggle.Content=_preferences.ShowTimeline?"Hide timelines":"Show timelines";_timelineToggle.IsVisible=_currentPage<=1;Avalonia.Automation.AutomationProperties.SetName(_timelineToggle,_preferences.ShowTimeline?"Hide timelines":"Show timelines");}bool visible=_preferences.ShowTimeline&&_currentPage<=1;_timeline.IsVisible=visible;_timelineDivider.IsVisible=visible;_workspace.RowDefinitions[0].Height=new GridLength(visible?Math.Clamp(_preferences.TimelineHeight,200,900):0);_workspace.RowDefinitions[1].Height=new GridLength(visible?6:0);}
    private void SelectPage(int index){
        _currentPage=index;
        _page.Content=index switch{0 or 1=>_planning,2=>_resources,3=>_assignments,4=>_details,5=>_noticesHost,6=>_taskUsage,7=>_network,8=>_resourceGraph,9=>_reports,11=>_calendarView,12=>_progressBoard,_=>_taskDrivers};
        _planning.ShowChart(index==1);_viewTitle.Text=new[]{"Task Sheet","Gantt Chart","Resource Sheet","Resource Usage","Task information","File information","Task Usage","Network Diagram","Resource Graph","Reports","Task Drivers","Calendar","Progress Board"}[index];
        for(int i=0;i<_navigation.Count;i++){if(i==index)_navigation[i].Classes.Add("selected");else _navigation[i].Classes.Remove("selected");}
        UpdateTimelineVisibility();UpdateWorkspaceControls();
    }
    private void SelectTask(int? uid){_inspector.SetTask(uid);if(uid is not int id)return;var t=_session.Tasks.FirstOrDefault(t=>t.Uid==id);if(t==null)return;_name.Text=t.Name;
        _details.SetTask(t);
        _taskDrivers.Text=$"{t.Name}\nStored relationships and constraints\n\nMode: {(t.IsManual==true?"Manual":"Auto Scheduled")}\nConstraint: {t.ConstraintType} {TaskColumns.Format(t.ConstraintDate)}\nCalendar: {t.Calendar?.Name??_session.Document?.Calendar?.Name}\nDeadline: {TaskColumns.Format(t.Deadline)}\n\nPredecessors:\n"+string.Join("\n",_session.Document!.Dependencies.Where(l=>l.Successor.Uid==id).Select(l=>$"{l.Predecessor?.DisplayId} · {l.Predecessor?.Name} · {l.Type} · lag {TaskColumns.Format(l.Lag)}"))+"\n\nSuccessors:\n"+string.Join("\n",_session.Document.Dependencies.Where(l=>l.Predecessor?.Uid==id).Select(l=>$"{l.Successor.DisplayId} · {l.Successor.Name} · {l.Type}"))+"\n\nCalculate Project to apply scheduling rules. Stored relationships do not by themselves prove which rule set a date.";
    }
    private void Persist(){try{_preferences.Save(_settingsPath);}catch(Exception e){_status.Text="Could not save view preferences: "+e.Message;}}
    private static DataGrid MakeGrid()=>new(){IsReadOnly=true,AutoGenerateColumns=false,CanUserResizeColumns=true,CanUserReorderColumns=true,RowHeight=36,ColumnHeaderHeight=36,FontSize=12,BorderThickness=new Thickness(0),GridLinesVisibility=DataGridGridLinesVisibility.Horizontal};
    private static DataGridTextColumn Column(string label,string key,double width)=>new(){Header=label,Binding=new Binding(key),Width=new DataGridLength(width)};
    private Task Page(int index){SelectPage(index);return Task.CompletedTask;}
    private Task SetSheetFontSize(double size){Sheet.Grid.FontSize=size;_preferences.FontSize=size;Persist();return Task.CompletedTask;}
    private Task TaskMode(bool manual){var ids=SelectedTaskUids();if(ids.Length==0)throw new InvalidOperationException("Select tasks first.");_session.EditCells(ids.Select(uid=>new TextCellChange(uid,"TaskMode",manual?"Manually Scheduled":"Auto Scheduled")),"Set task mode");Refresh();return Task.CompletedTask;}
    private async Task RunMenu(Func<Task> action){if(_busy)return;if(!_inspector.TryCommit())return;if(!Sheet.Grid.CommitEdit()||!_resources.CommitEdit()||!_assignments.CommitEdit()){_status.Text="Correct or cancel the current cell before continuing.";return;}_busy=true;try{await action();}catch(Exception e){_status.Text="Could not complete action: "+e.Message;}finally{_busy=false;}}
    private MenuItem Menu(string title,params (string Title,Func<Task> Action)[] actions){var menu=new MenuItem{Header=title};foreach(var item in actions){var entry=new MenuItem{Header=item.Title};entry.Click+=async(_,_)=>await RunMenu(item.Action);menu.Items.Add(entry);}return menu;}
    private Button Dropdown(string title,params (string Title,Func<Task> Action)[] actions){var button=new Button{Content=title+" ▾"};var menu=new ContextMenu();foreach(var item in actions){var entry=new MenuItem{Header=item.Title};entry.Click+=async(_,_)=>await RunMenu(item.Action);menu.Items.Add(entry);}button.ContextMenu=menu;button.Click+=(_,_)=>menu.Open(button);return button;}
    private Button Action(string title,Func<Task> action,bool primary=false){var b=new Button{Content=title};if(title=="B"){b.FontWeight=FontWeight.Bold;ToolTip.SetTip(b,"Bold (Ctrl+B)");}if(title=="I"){b.FontStyle=FontStyle.Italic;ToolTip.SetTip(b,"Italic (Ctrl+I)");}if(title=="U")ToolTip.SetTip(b,"Underline (Ctrl+U)");if(primary)b.Classes.Add("primary");b.Click+=async(_,_)=>{if(_busy)return;if(!_inspector.TryCommit())return;if(!Sheet.Grid.CommitEdit()||!_resources.CommitEdit()||!_assignments.CommitEdit()){_status.Text="Correct or cancel the current cell before continuing.";return;}_busy=true;b.IsEnabled=false;try{await action();}catch(Exception e){_status.Text="Could not complete action: "+e.Message;}finally{_busy=false;b.IsEnabled=true;}};return b;}
    private async Task OpenProject(){if(!await ResolveUnsavedChanges())return;var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{Title="Open project",AllowMultiple=false,FileTypeFilter=[new FilePickerFileType("Project files"){Patterns=["*.mpp","*.planora"]}]});if(files.FirstOrDefault()?.TryGetLocalPath()is string path){if(Path.GetExtension(path).Equals(".planora",StringComparison.OrdinalIgnoreCase)){OpenPlanoraCopy(path);return;}await _session.OpenAsync(path);Refresh();_formats.Import(path);}}
    private async Task<bool> SaveCopy(){var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title="Save project copy",DefaultExtension="mpp",SuggestedFileName=System.IO.Path.GetFileNameWithoutExtension(_session.SourcePath??"Project")+"-Planora.mpp",FileTypeChoices=[new FilePickerFileType("Microsoft Project"){Patterns=["*.mpp"]},new FilePickerFileType("Planora (includes formatting and timelines)"){Patterns=["*.planora"]}]});if(file?.TryGetLocalPath()is string path){if(Path.GetExtension(path).Equals(".planora",StringComparison.OrdinalIgnoreCase)){await SavePlanoraCopy(path);return true;}var assessment=_session.AssessSave(path);assessment.ThrowIfErrors();var notices=assessment.Diagnostics.Where(d=>d.RepresentsLoss).ToList();bool accepted=false;if(notices.Count>0){accepted=await Confirm("This edited copy needs a compatibility review. Existing views, stored work/cost distributions or other information may not fully follow the edits. Your original file will remain unchanged.\n\n"+string.Join("\n",notices.Select(d=>FriendlySaveNotice(d.Code)))+"\n\nContinue and save this copy?",true);if(!accepted)return false;}await Task.Run(()=>_session.SaveCopy(path,accepted));if(_formats.Document.Cells.Count+_formats.Document.Bars.Count+_formats.Document.Notes.Count+_formats.Document.ChartColors.Count+_formats.Document.Styles.Count>0)_formats.Export(path);_status.Text="Saved project copy: "+path;return true;}return false;}
    private static string FriendlySaveNotice(string code)=>code switch{"PROJECT_NATIVE_STRUCTURAL_OPAQUE"=>"• Existing layout and auxiliary references may need adjustment after task or resource changes.","PROJECT_NATIVE_STORED_TOTALS"=>"• Stored work/cost distributions and calculated totals may not match changed schedule values.","PROJECT_NATIVE_SIGNATURE_INVALIDATED"=>"• The existing digital signature will no longer be valid.",_=>"• Some project information cannot be fully preserved by this export."};
    private async Task<bool> Confirm(string text,bool detailed=false){var dialog=new Window{Title=AppBrand.Name,Width=detailed?720:480,Height=detailed?350:180,CanResize=false,WindowStartupLocation=WindowStartupLocation.CenterOwner};var panel=new StackPanel{Margin=new Thickness(18),Spacing=14};panel.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap});var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right};var no=new Button{Content="Cancel"};var yes=new Button{Content="Continue"};no.Click+=(_,_)=>dialog.Close(false);yes.Click+=(_,_)=>dialog.Close(true);row.Children.Add(no);row.Children.Add(yes);panel.Children.Add(row);dialog.Content=panel;return await dialog.ShowDialog<bool>(this);}
    private void Refresh(){_formats.Bind(_session);var d=_session.Document;Title=(d?.Name??"Untitled Project")+" · "+AppBrand.Name;_title.Text=d?.Name??(_session.SourcePath==null?"Untitled Project":System.IO.Path.GetFileNameWithoutExtension(_session.SourcePath));UpdateGanttAxis();_planning.Refresh(_session);_timeline.Refresh(_session);
        _taskUsage.Refresh(_session);_network.Refresh(_session);_resourceGraph.Refresh(_session);_reports.Refresh(_session);
        _calendarView.Refresh(_session);_progressBoard.Refresh(_session);
        _resources.ItemsSource=_session.Resources.Select(r=>new SheetRecord(r.Uid,ResourceCells(r))).ToList();
        _assignments.ItemsSource=d?.Assignments.Where(a=>a.Task?.Uid!=0&&a.Resource?.Uid>0).Select(a=>new SheetRecord(a.Uid,AssignmentCells(a))).ToList();
        _notices.Text=d==null?"No project loaded.":string.Join("\n\n",d.ReadDiagnostics.Select(n=>n.Code switch{"PROJECT_NATIVE_OPAQUE"=>"Additional project information is preserved in the file. Task-name edits are validated; other editing operations are still being checked.","PROJECT_NATIVE_TIMEPHASED_PRESERVED"=>"Detailed work/cost distributions and custom fields are kept in the file. Some values do not yet have a display or editing mapping in this preview.",_=>n.Message}));
        _status.Text=d==null?"Open an MPP project to begin.":$"{_session.Tasks.Count} tasks · {_session.Resources.Count} resources · Stored schedule · {Sheet.Catalogue.Count} selectable fields";
        if(Sheet.SelectedUid is int uid)SelectTask(uid);else {_details.SetTask(null);_inspector.SetTask(null);}
    }
    private static Dictionary<string,string?> ResourceCells(ProjectResource r)=>new(){["Name"]=r.Name,["Type"]=r.Type?.ToString(),["Capacity"]=TaskColumns.Format(r.MaxUnits),["Rate"]=TaskColumns.Format(r.StandardRate),["OvertimeRate"]=TaskColumns.Format(r.OvertimeRate),["CostPerUse"]=TaskColumns.Format(r.CostPerUse),["Group"]=r.Group,["Initials"]=r.Initials,["Work"]=TaskColumns.Format(r.Work),["Cost"]=TaskColumns.Format(r.Cost)};
    private static Dictionary<string,string?> AssignmentCells(ProjectAssignment a)=>new(){["Task"]=a.Task?.Name,["Resource"]=a.Resource?.Name,["Units"]=TaskColumns.Format(a.Units),["Work"]=TaskColumns.Format(a.Work),["Actual"]=TaskColumns.Format(a.ActualWork),["Cost"]=TaskColumns.Format(a.Cost)};
    private void RefreshResourceSheetValues(int uid,string key){
        var d=_session.Document;if(d==null)return;
        var resource=d.Resources.FirstOrDefault(r=>r.Uid==uid);if(resource==null)return;
        foreach(var row in _resources.ItemsSource?.OfType<SheetRecord>()??[])if(row.Uid==uid)row.Update(ResourceCells(resource));
        if(key=="Name"){
            var assignments=d.Assignments.Where(a=>a.Resource?.Uid==uid).ToDictionary(a=>a.Uid);
            foreach(var row in _assignments.ItemsSource?.OfType<SheetRecord>()??[])if(assignments.TryGetValue(row.Uid,out var assignment))row.Update(AssignmentCells(assignment));
            Sheet.RefreshResourceNames(uid);
        }
        _status.Text="Resource edit applied. Calculate Project updates work and cost totals.";
    }
    private void RefreshSheetValues(){var d=_session.Document;if(d==null)return;foreach(var row in _resources.ItemsSource?.OfType<SheetRecord>()??[])row.Update(ResourceCells(d.Resources.GetByUid(row.Uid)));foreach(var row in _assignments.ItemsSource?.OfType<SheetRecord>()??[])row.Update(AssignmentCells(d.Assignments.Single(a=>a.Uid==row.Uid)));Sheet.RefreshValues();_status.Text="Edit applied. Calculate Project updates work, cost and schedule totals.";}
}






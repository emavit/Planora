using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using Avalonia.Layout;
using Avalonia.Automation;
using OfficeIMO.Project;
using System.Reflection;

namespace ProjectDesk;

public sealed class TaskSheetModule : UserControl {
    public FormattingStore? Formatting {get;set;}
    public DataGrid Grid { get; } = new() { AutoGenerateColumns=false,IsReadOnly=false,CanUserResizeColumns=true,CanUserReorderColumns=true,CanUserSortColumns=false,FrozenColumnCount=1,
        GridLinesVisibility=DataGridGridLinesVisibility.All,RowHeight=26,ColumnHeaderHeight=36,FontSize=12,BorderThickness=new Thickness(0),
        SelectionMode=DataGridSelectionMode.Extended,Background=DesignTokens.Surface,HorizontalGridLinesBrush=DesignTokens.Separator,VerticalGridLinesBrush=DesignTokens.Separator };
    private IReadOnlyList<TaskColumn> _catalogue=[];
    private readonly Dictionary<DataGridColumn,string> _keys=[];
    private readonly ViewPreferences _preferences;
    private readonly string _settingsPath;
    private bool _updating;
    private bool _continueEntry;
    private readonly Dictionary<int,(int? BeforeUid,bool Milestone)> _drafts=[];
    private int _nextDraftUid=-2;
    private ProjectSession? _session;
    private string _query="",_filter="All tasks";
    private readonly HashSet<int> _collapsed=[];
    private string? _projectKey;
    private int _outlineLevel=int.MaxValue;
    private string _sortKey="Project order";
    private bool _descending;
    private DataGridColumn? _addColumn;
    private readonly Dictionary<string,string> _displayTitles=[];
    public event Action? ValuesChanged;
    public event Action<bool>? InsertTaskRequested;
    public event Action<int?>? TaskSelected;
    public event Action<string>? EditCellRequested;
    public event Action<string>? Error;
    public IReadOnlyList<TaskColumn> Catalogue=>_catalogue;
    public IReadOnlyList<string> VisibleKeys=>Grid.Columns.OrderBy(c=>c.DisplayIndex).Where(c=>_keys.ContainsKey(c)).Select(c=>_keys[c]).ToList();
    public string? CurrentKey=>Grid.CurrentColumn!=null&&_keys.TryGetValue(Grid.CurrentColumn,out var key)?key:null;
    public int? SelectedUid=>Grid.SelectedItem is TaskTableRow {Uid:>0} row?row.Uid:null;
    public bool HasPendingInsertion=>Grid.SelectedItem is TaskTableRow{Uid:<-1} row&&_drafts.ContainsKey(row.Uid);
    public int? PendingInsertionBeforeUid=>Grid.SelectedItem is TaskTableRow{Uid:<-1} row&&_drafts.TryGetValue(row.Uid,out var draft)?draft.BeforeUid:null;
    public void DiscardSelectedDraft(){if(Grid.SelectedItem is not TaskTableRow{Uid:<-1} row)return;Grid.CancelEdit();_drafts.Remove(row.Uid);if(_session!=null)Refresh(_session);}
    public TaskSheetModule(ViewPreferences preferences,string settingsPath) {
        _preferences=preferences;_settingsPath=settingsPath;Content=Grid;
        Grid.Styles.Add(new Style(s=>s.OfType<DataGridColumnHeader>()){Setters={new Setter(DataGridColumnHeader.BackgroundProperty,RibbonModule.Chrome),new Setter(DataGridColumnHeader.ForegroundProperty,RibbonModule.Ink),new Setter(DataGridColumnHeader.PaddingProperty,new Thickness(5,0))}});
        Grid.Styles.Add(new Style(s=>s.OfType<DataGridRow>()){Setters={new Setter(DataGridRow.FontWeightProperty,new Binding("Weight"))}});
        Grid.SelectionChanged+=(_,_)=>TaskSelected?.Invoke(SelectedUid);
        Grid.DoubleTapped+=(_,_)=>Grid.BeginEdit();
        Grid.AddHandler(InputElement.PointerPressedEvent,(_,e)=>{if(e.GetCurrentPoint(Grid).Properties.IsRightButtonPressed&&e.Source is Control source){var row=source.GetVisualAncestors().OfType<DataGridRow>().FirstOrDefault();if(row?.DataContext is TaskTableRow task&&!Grid.SelectedItems.Contains(task))Grid.SelectedItem=task;}},Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Grid.BeginningEdit+=(_,e)=>{if(e.Row.DataContext is TaskTableRow{Uid:<0}&&_keys.TryGetValue(e.Column,out var key)&&key!="Name")e.Cancel=true;};
        Grid.PreparingCellForEdit+=(_,e)=>{if(e.Row.DataContext is TaskTableRow row&&e.EditingElement is TextBox text&&_keys.TryGetValue(e.Column,out var key)&&_session!=null){text.Text=row.Uid<0?"":_session.EditText(row.Uid,key);text.SelectAll();}};
        Grid.CellEditEnding+=(_,e)=>{
            if(e.EditAction==DataGridEditAction.Cancel&&e.Row.DataContext is TaskTableRow{Uid:<-1} cancelled&&_drafts.Remove(cancelled.Uid)){Dispatcher.UIThread.Post(()=>{if(_session!=null)Refresh(_session);});return;}
            if(e.EditAction!=DataGridEditAction.Commit||e.Row.DataContext is not TaskTableRow row||e.EditingElement is not TextBox text||!_keys.TryGetValue(e.Column,out var key)||_session==null)return;
            int? inserted=null;
            try{if(row.Uid<0){if(string.IsNullOrWhiteSpace(text.Text))return;var draft=_drafts.GetValueOrDefault(row.Uid);inserted=_session.AddTask(text.Text.Trim(),draft.Milestone,beforeUid:draft.BeforeUid);_drafts.Remove(row.Uid);}else _session.EditText(row.Uid,key,text.Text??"");}
            catch(Exception error){e.Cancel=true;Error?.Invoke(error.Message);return;}
            Dispatcher.UIThread.Post(()=>{if(inserted.HasValue){Refresh(_session);Grid.SelectedItem=((IEnumerable<TaskTableRow>)Grid.ItemsSource!).FirstOrDefault(r=>r.Uid==inserted);if(_continueEntry){_continueEntry=false;FocusName(-1,true);}}else RefreshValues();ValuesChanged?.Invoke();});
        };
        Grid.AddHandler(InputElement.KeyDownEvent,(_,e)=>{_continueEntry=e.Key==Key.Enter&&e.KeyModifiers==KeyModifiers.None&&Grid.SelectedItem is TaskTableRow{Uid:-1}&&e.Source is TextBox;},Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Grid.KeyDown+=(_,e)=>{if(e.Source is TextBox)return;if(e.Key==Key.F2){Grid.BeginEdit();e.Handled=true;}else if(e.Key==Key.Insert&&e.KeyModifiers==KeyModifiers.None){InsertTaskRequested?.Invoke(false);e.Handled=true;}};
        Grid.ColumnReordered+=(_,_)=>{if(_addColumn!=null&&_addColumn.DisplayIndex!=Grid.Columns.Count-1)_addColumn.DisplayIndex=Grid.Columns.Count-1;Persist();};
        var menu=new ContextMenu();var insert=new MenuItem{Header="Insert / choose columns…"};insert.Click+=async(_,_)=>{if(TopLevel.GetTopLevel(this)is Window owner)await ChooseColumns(owner);};
        var hide=new MenuItem{Header="Hide this column"};hide.Click+=(_,_)=>{if(Grid.CurrentColumn is DataGridColumn c)HideColumn(c);};
        var edit=new MenuItem{Header="Edit selected field…"};edit.Click+=(_,_)=>{if(Grid.CurrentColumn!=null&&_keys.TryGetValue(Grid.CurrentColumn,out var key))EditCellRequested?.Invoke(key);};
        var outline=new MenuItem{Header="Show / hide subtasks"};outline.Click+=(_,_)=>ToggleSubtasks();
        var taskInsert=new MenuItem{Header="Insert Task",InputGesture=new KeyGesture(Key.Insert)};taskInsert.Click+=(_,_)=>InsertTaskRequested?.Invoke(false);var milestoneInsert=new MenuItem{Header="Insert Milestone"};milestoneInsert.Click+=(_,_)=>InsertTaskRequested?.Invoke(true);menu.Items.Add(taskInsert);menu.Items.Add(milestoneInsert);menu.Items.Add(new Separator());
        menu.Items.Add(edit);menu.Items.Add(insert);menu.Items.Add(hide);menu.Items.Add(outline);Grid.ContextMenu=menu;
    }
    public void Refresh(ProjectSession session) {
        var key=session.SourcePath??session.Document?.Guid?.ToString();if(_projectKey!=key){_collapsed.Clear();_drafts.Clear();_projectKey=key;}
        _session=session;var selected=SelectedUid;_catalogue=TaskColumns.Catalogue(session.Document);
        var desired=_preferences.Columns.Select(c=>c.Key).Where(k=>_catalogue.Any(c=>c.Key==k)).Distinct().ToArray();if(!VisibleKeys.SequenceEqual(desired))ApplyColumns(desired);
        var rows=session.Document==null ? new List<TaskTableRow>() : OrderedTasks().Where(VisibleTask).Select(t=>new TaskTableRow(t,session.Document,_catalogue)).ToList();
        foreach(var (draftUid,draft) in _drafts){int index=draft.BeforeUid.HasValue?rows.FindIndex(r=>r.Uid==draft.BeforeUid):rows.Count;rows.Insert(index<0?rows.Count:index,TaskTableRow.EntryRow(_catalogue,draftUid));}
        if(session.Document!=null)rows.Add(TaskTableRow.EntryRow(_catalogue));Grid.ItemsSource=rows;
        if(selected is int uid)Grid.SelectedItem=((IEnumerable<TaskTableRow>)Grid.ItemsSource).FirstOrDefault(r=>r.Uid==uid);
    }
    public int InsertBlankRow(bool milestone){
        if(_session?.Document==null)throw new InvalidOperationException("Open or create a project first.");
        int? before=SelectedUid;if(Grid.SelectedItem is TaskTableRow{Uid:<-1} selected&&_drafts.TryGetValue(selected.Uid,out var anchor))before=anchor.BeforeUid;
        PrepareInsertion();int uid=_nextDraftUid--;_drafts.Add(uid,(before,milestone));Refresh(_session);FocusName(uid,true);return uid;
    }
    public void PrepareInsertion(){_query="";_filter="All tasks";_outlineLevel=int.MaxValue;_sortKey="Project order";_descending=false;}
    public void FocusName(int uid,bool edit){
        if(!VisibleKeys.Contains("Name")){ApplyColumns(VisibleKeys.Append("Name"));Persist();}
        void FocusRow(){var row=Grid.ItemsSource?.OfType<TaskTableRow>().FirstOrDefault(r=>r.Uid==uid);if(row==null)return;Grid.SelectedItem=row;Grid.CurrentColumn=_keys.Single(p=>p.Value=="Name").Key;Grid.ScrollIntoView(row,Grid.CurrentColumn);Grid.Focus();if(edit)Grid.BeginEdit();}
        Dispatcher.UIThread.Post(FocusRow,DispatcherPriority.Background);
    }
    private bool VisibleTask(OfficeIMO.Project.ProjectTask task){int depth=1;for(var parent=task.Parent;parent!=null;parent=parent.Parent){depth++;if(_collapsed.Contains(parent.Uid))return false;}if(depth>_outlineLevel)return false;return (task.Name??"").Contains(_query,StringComparison.OrdinalIgnoreCase)&&(_filter switch{"Milestones"=>task.IsMilestone==true||task.Duration?.Value==0,"Critical tasks"=>task.IsCritical==true,"Incomplete tasks"=>(task.PercentComplete??0)<100,"Completed tasks"=>task.PercentComplete==100,_=>true});}
    public void SetFilter(string query,string filter){if(!Grid.CommitEdit()){Error?.Invoke("Correct or cancel the current edit before filtering.");return;}_query=query;_filter=filter;if(_session!=null)Refresh(_session);}
    public void ToggleSubtasks(){if(SelectedUid is not int uid||_session==null)return;if(!_session.Document!.Tasks.GetByUid(uid).IsSummary)return;if(!_collapsed.Add(uid))_collapsed.Remove(uid);Refresh(_session);}
    public void SetSubtasks(bool visible){if(SelectedUid is not int uid||_session==null)return;if(visible)_collapsed.Remove(uid);else _collapsed.Add(uid);Refresh(_session);}
    public void SetOutlineLevel(int depth){_outlineLevel=depth;if(_session!=null)Refresh(_session);}
    public void SortRows(string key,bool descending){
        if(!Grid.CommitEdit()){Error?.Invoke("Correct or cancel the current cell before sorting.");return;}
        var previousKey=_sortKey;bool previousDescending=_descending;
        try{_sortKey=key=="% Complete"?"PercentComplete":key;_descending=descending;if(_session!=null)Refresh(_session);ValuesChanged?.Invoke();}
        catch(Exception error){_sortKey=previousKey;_descending=previousDescending;Error?.Invoke("Could not sort this column: "+error.Message);}
    }
    private IEnumerable<ProjectTask> OrderedTasks(){
        if(_session==null)return [];if(_sortKey=="Project order")return _session.Tasks;
        var spec=_catalogue.FirstOrDefault(c=>c.Key==_sortKey);
        var comparer=Comparer<ProjectTask>.Create((a,b)=>{
            var x=SortValue(a,_sortKey,spec);var y=SortValue(b,_sortKey,spec);
            if(x==null)return y==null?0:1;if(y==null)return -1;
            int c=x is string text?StringComparer.OrdinalIgnoreCase.Compare(text,Convert.ToString(y)):x is IComparable comparable&&x.GetType()==y.GetType()?comparable.CompareTo(y):StringComparer.OrdinalIgnoreCase.Compare(Convert.ToString(x),Convert.ToString(y));
            return _descending?-c:c;
        });
        var groups=_session.Tasks.ToLookup(t=>t.Parent?.Uid??0);var result=new List<ProjectTask>();
        void Walk(int parent){foreach(var task in groups[parent].OrderBy(t=>t,comparer)){result.Add(task);Walk(task.Uid);}}Walk(0);return result;
    }
    private object? SortValue(ProjectTask task,string key,TaskColumn? spec){
        if(_session?.Document is not ProjectDocument document)return null;
        object? value;
        if(key.StartsWith("Baseline",StringComparison.Ordinal)&&key.Split('.') is [var prefix,var field]&&int.TryParse(prefix[8..],out int number))value=task.Baselines.FirstOrDefault(b=>(b.Number??0)==number)is ProjectBaseline baseline?typeof(ProjectBaseline).GetProperty(field)?.GetValue(baseline):null;
        else if(key=="TaskMode")value=task.IsManual;
        else value=typeof(ProjectTask).GetProperty(key,BindingFlags.Instance|BindingFlags.Public)?.GetValue(task)??spec?.Read(task,document);
        return value switch{ProjectWork work=>work.Minutes,ProjectUnits units=>units.Value,ProjectDuration duration=>DurationMinutes(duration,document),Enum e=>Convert.ToInt32(e),string s when string.IsNullOrWhiteSpace(s)=>null,_=>value};
    }
    private static decimal DurationMinutes(ProjectDuration duration,ProjectDocument document)=>duration.Value*(duration.Unit switch{
        ProjectDurationUnit.Hour=>60m,ProjectDurationUnit.Day=>duration.IsElapsed?1440m:document.Settings.MinutesPerDay??480,
        ProjectDurationUnit.Week=>duration.IsElapsed?10080m:document.Settings.MinutesPerWeek??2400,
        ProjectDurationUnit.Month=>duration.IsElapsed?43200m:(document.Settings.MinutesPerDay??480)*(document.Settings.DaysPerMonth??20),_=>1m});
    public void ExpandAll(){_collapsed.Clear();_outlineLevel=int.MaxValue;if(_session!=null)Refresh(_session);}
    public void RefreshResourceNames(int resourceUid){
        if(_session?.Document is not {} document)return;
        var affected=document.Assignments.Where(a=>a.Resource?.Uid==resourceUid&&a.Task!=null).Select(a=>a.Task!.Uid).ToHashSet();
        var names=document.Assignments.Where(a=>a.Task!=null&&affected.Contains(a.Task.Uid)&&a.Resource?.Uid>0)
            .GroupBy(a=>a.Task!.Uid).ToDictionary(g=>g.Key,g=>string.Join(", ",g.Select(a=>a.Resource!.Name)));
        foreach(var row in Grid.ItemsSource?.OfType<TaskTableRow>()??[])if(affected.Contains(row.Uid))row.UpdateCell("ResourceNames",names.GetValueOrDefault(row.Uid)??"");
    }
    public void RefreshValues(){if(_session?.Document==null)return;var tasks=_session.Tasks.ToDictionary(t=>t.Uid);foreach(var row in ((IEnumerable<TaskTableRow>?)Grid.ItemsSource??[])){if(tasks.TryGetValue(row.Uid,out var task))row.Refresh(task,_session.Document,_catalogue);}}
    public void ApplyColumns(IEnumerable<string> keys) {
        _updating=true;
        try {
            Grid.Columns.Clear();_keys.Clear();_addColumn=null;
            foreach(var key in keys.Distinct()) {
                var spec=_catalogue.FirstOrDefault(c=>c.Key==key);if(spec==null)continue;
                var stored=_preferences.Columns.FirstOrDefault(c=>c.Key==key)?.Width??0;
                var column=new FormattedTaskColumn(key,this){Header=spec.Title,Binding=new Binding($"Cells[{key}]"),IsReadOnly=!ProjectSession.CanEditText(key),Width=new DataGridLength(stored>=40?stored:spec.Width),MinWidth=key switch{"DisplayId"=>60,"TaskMode"=>95,"Duration"=>105,_=>60}};
                column.HeaderTemplate=new FuncDataTemplate<object>((_,_)=>ColumnHeader(column,spec));
                Grid.Columns.Add(column);_keys[column]=key;
            }
            if(Grid.Columns.Count==0) { var name=_catalogue.First(c=>c.Key=="Name");var column=new DataGridTextColumn{Header=name.Title,Binding=new Binding("Cells[Name]"),Width=new DataGridLength(300)};Grid.Columns.Add(column);_keys[column]="Name"; }
            _addColumn=new DataGridTextColumn{Header="Add New Column",IsReadOnly=true,CanUserReorder=false,CanUserResize=false,Width=new DataGridLength(150),MinWidth=150};
            _addColumn.HeaderTemplate=new FuncDataTemplate<object>((_,_)=>{
                var add=new Button{Content="＋ Add New Column",Padding=new Thickness(3,0),MinHeight=24,FontSize=11,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Foreground=RibbonModule.Ink};
                AutomationProperties.SetName(add,"Add New Column");add.Click+=async(_,_)=>{if(TopLevel.GetTopLevel(this)is Window owner)await ChooseColumns(owner,null);};return add;
            });
            Grid.Columns.Add(_addColumn);
        } finally {_updating=false;}
    }
    private Control ColumnHeader(DataGridColumn column,TaskColumn spec){
        var panel=new DockPanel{Margin=new Thickness(-4,0,-4,0),LastChildFill=true};
        var dropdown=new Button{Content="▾",Padding=new Thickness(2,0),MinHeight=22,MinWidth=18,Width=18,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Foreground=RibbonModule.Ink};
        AutomationProperties.SetName(dropdown,spec.Title+" column menu");ToolTip.SetTip(dropdown,"Insert, hide, rename or sort this column");
        DockPanel.SetDock(dropdown,Dock.Right);panel.Children.Add(dropdown);
        panel.Children.Add(new TextBlock{Text=spec.Key=="TaskMode"&&!_displayTitles.ContainsKey(spec.Key)?"Task\nMode":_displayTitles.GetValueOrDefault(spec.Key,spec.Title),TextWrapping=TextWrapping.NoWrap,TextTrimming=TextTrimming.CharacterEllipsis,FontSize=11,VerticalAlignment=VerticalAlignment.Center});
        dropdown.Click+=(_,_)=>{if(!Grid.CommitEdit()){Error?.Invoke("Correct or cancel the current cell before changing columns.");return;}Grid.CurrentColumn=column;var menu=ColumnMenu(column,spec);dropdown.ContextMenu=menu;menu.Open(dropdown);};
        return panel;
    }
    private ContextMenu ColumnMenu(DataGridColumn column,TaskColumn spec){
        var menu=new ContextMenu();
        MenuItem Command(string title,Action action){var item=new MenuItem{Header=title};item.Click+=(_,_)=>action();menu.Items.Add(item);return item;}
        var insert=new MenuItem{Header="Insert Column…"};insert.Click+=async(_,_)=>{if(TopLevel.GetTopLevel(this)is Window owner)await ChooseColumns(owner,spec.Key);};menu.Items.Add(insert);
        Command("Hide Column",()=>HideColumn(column)).IsEnabled=VisibleKeys.Count>1;
        var rename=new MenuItem{Header="Column Settings…"};rename.Click+=async(_,_)=>{
            if(TopLevel.GetTopLevel(this)is not Window owner)return;
            var form=new ColumnTitleWindow(spec.Title,_displayTitles.GetValueOrDefault(spec.Key,spec.Title));
            var title=await form.ShowDialog<string?>(owner);if(title==null)return;
            if(title==spec.Title)_displayTitles.Remove(spec.Key);else _displayTitles[spec.Key]=title;
            ApplyColumns(VisibleKeys.ToArray());
        };menu.Items.Add(rename);menu.Items.Add(new Separator());
        var kind=typeof(ProjectTask).GetProperty(spec.Key)?.PropertyType;kind=kind==null?null:Nullable.GetUnderlyingType(kind)??kind;
        bool date=kind==typeof(DateTime)||spec.Key.EndsWith(".Start",StringComparison.Ordinal)||spec.Key.EndsWith(".Finish",StringComparison.Ordinal);
        bool numeric=kind!=null&&(kind.IsPrimitive&&kind!=typeof(bool)||kind==typeof(decimal)||kind==typeof(ProjectDuration)||kind==typeof(ProjectWork)||kind==typeof(ProjectUnits))||spec.Key.StartsWith("Baseline",StringComparison.Ordinal)&&!date;
        Command(date?"Sort Earliest to Latest":numeric?"Sort Smallest to Largest":"Sort A to Z",()=>SortRows(spec.Key,false));
        Command(date?"Sort Latest to Earliest":numeric?"Sort Largest to Smallest":"Sort Z to A",()=>SortRows(spec.Key,true));
        Command("Restore Project Order",()=>SortRows("Project order",false));return menu;
    }
    public void HideColumn(DataGridColumn column){
        if(!_keys.ContainsKey(column)||VisibleKeys.Count<=1)return;
        if(!Grid.CommitEdit()){Error?.Invoke("Correct or cancel the current cell before hiding a column.");return;}
        Grid.Columns.Remove(column);_keys.Remove(column);Persist();
    }
    public Task ChooseColumns(Window owner)=>ChooseColumns(owner,CurrentKey);
    private async Task ChooseColumns(Window owner,string? before) {
        if(!Grid.CommitEdit()){Error?.Invoke("Correct or cancel the current cell before changing columns.");return;}
        var picker=new ColumnPickerWindow(_catalogue,VisibleKeys,before);var chosen=await picker.ShowDialog<List<string>?>(owner);
        if(chosen!=null){if(picker.ResetWidths)_preferences.Columns=chosen.Select(k=>new ColumnSetting(k,0)).ToList();ApplyColumns(chosen);Persist();}
    }
    public void ResetColumns(){_preferences.Columns=TaskColumns.Defaults.Select(k=>new ColumnSetting(k,0)).ToList();ApplyColumns(TaskColumns.Defaults);Persist();}
    public void Persist() {
        if(_updating)return;
        _preferences.Columns=Grid.Columns.OrderBy(c=>c.DisplayIndex).Where(c=>_keys.ContainsKey(c)).Select(c=>new ColumnSetting(_keys[c],c.ActualWidth)).ToList();
        try{_preferences.Save(_settingsPath);}catch(Exception e){Error?.Invoke("Could not save column preferences: "+e.Message);}
    }
}





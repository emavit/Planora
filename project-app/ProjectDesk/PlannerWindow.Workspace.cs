using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ProjectDesk;

public sealed partial class PlannerWindow {
    private readonly Dictionary<int,Button> _viewTabs=[];
    private readonly TaskInspectorModule _inspector;
    private Border? _inspectorHost;
    private Button? _ribbonToggle,_inspectorToggle,_focusToggle,_timelineToggle;
    private bool _planningFocus;
    private GridSplitter? _inspectorDivider;
    public bool PlanningFocus=>_planningFocus;
    public double InspectorWidth=>_preferences.InspectorWidth;
    public bool CompactWorkspace=>_preferences.CompactWorkspace;
    public bool InspectorVisible=>_preferences.ShowInspector;
    public TaskInspectorModule Inspector=>_inspector;
    public int CurrentView=>_currentPage;
    private Control BuildWorkspaceNavigation(){
        var row=new WrapPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(12,6)};
        var tabs=new StackPanel{Orientation=Orientation.Horizontal,Spacing=2};
        row.Children.Add(new Border{Background=DesignTokens.Sidebar,CornerRadius=new CornerRadius(8),Padding=new Thickness(3),Margin=new Thickness(0,0,8,4),Child=tabs});
        foreach(var (index,label) in new[]{(1,"Gantt"),(0,"Table"),(12,"Board"),(11,"Calendar"),(2,"Resources"),(9,"Reports")}){
            var button=Action(label,()=>Page(index));button.Name="WorkspaceView"+index;button.Classes.Add("workspaceTab");button.Padding=new Thickness(12,6);
            AutomationProperties.SetName(button,label+" view");_viewTabs.Add(index,button);tabs.Children.Add(button);
        }
        row.Children.Add(Dropdown("More views",("Task Usage",()=>Page(6)),("Network Diagram",()=>Page(7)),("Resource Usage",()=>Page(3)),("Resource Graph",()=>Page(8)),("Task Drivers",()=>Page(10)),("File Information",()=>Page(5))));
        row.Children.Add(Action("Saved views…",ManageSavedViews));
        _ribbonToggle=Action("",()=>{SetCompactWorkspace(!_preferences.CompactWorkspace);return Task.CompletedTask;});_ribbonToggle.Name="WorkspaceRibbonToggle";row.Children.Add(_ribbonToggle);
        _timelineToggle=Action("Hide timelines",()=>{SetTimelineVisible(!_preferences.ShowTimeline);return Task.CompletedTask;});
        _timelineToggle.Name="WorkspaceTimelineToggle";ToolTip.SetTip(_timelineToggle,"Show or hide all timelines without removing their tasks or formatting");row.Children.Add(_timelineToggle);
        _inspectorToggle=Action("",()=>{SetInspectorVisible(!_preferences.ShowInspector);return Task.CompletedTask;});_inspectorToggle.Name="WorkspaceInspectorToggle";row.Children.Add(_inspectorToggle);
        row.Children.Add(Dropdown("Row density",("Compact",()=>{SetRowDensity(false);return Task.CompletedTask;}),("Comfortable",()=>{SetRowDensity(true);return Task.CompletedTask;})));
        row.Children.Add(Dropdown("Inspector width",("Narrow",()=>{SetInspectorWidth(260);return Task.CompletedTask;}),("Standard",()=>{SetInspectorWidth(340);return Task.CompletedTask;}),("Wide",()=>{SetInspectorWidth(440);return Task.CompletedTask;})));
        _focusToggle=Action("Focus",()=>{SetPlanningFocus(!_planningFocus);return Task.CompletedTask;});_focusToggle.Name="WorkspaceFocusToggle";ToolTip.SetTip(_focusToggle,"Planning focus (F11): hide optional panels, keep timelines");row.Children.Add(_focusToggle);
        foreach(var control in row.Children.OfType<Button>()){control.Classes.Add("toolbar");control.Margin=new Thickness(2,3);}
        return new Border{Background=DesignTokens.Background,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(0,0,0,1),Child=row};
    }
    public void SetRowDensity(bool comfortable){
        if(!Sheet.Grid.CommitEdit())return;
        _preferences.RowHeight=comfortable?38:28;UpdateFormattedRowHeights();Gantt.InvalidateVisual();Persist();
    }
    public void SetInspectorWidth(double width){
        if(!double.IsFinite(width))return;
        _preferences.InspectorWidth=Math.Clamp(width,260,480);UpdateWorkspaceControls();Persist();
    }
    public void SetPlanningFocus(bool enabled){
        if(enabled&&!_inspector.TryCommit())return;
        _planningFocus=enabled;UpdateWorkspaceControls();SetSidebar();
    }
    public void SetCompactWorkspace(bool compact){_preferences.CompactWorkspace=compact;UpdateWorkspaceControls();Persist();}
    public void SetInspectorVisible(bool visible){if(!visible&&!_inspector.TryCommit())return;_preferences.ShowInspector=visible;UpdateWorkspaceControls();Persist();}
    private void UpdateWorkspaceControls(){
        _glassNavigation.IsVisible=!_planningFocus&&!_preferences.CompactWorkspace;
        if(_focusToggle!=null)_focusToggle.Content=_planningFocus?"Exit focus":"Focus";
        if(_ribbonToggle!=null)_ribbonToggle.Content=_preferences.CompactWorkspace?"Show ribbon":"Hide ribbon";
        if(_inspectorToggle!=null)_inspectorToggle.Content=_preferences.ShowInspector?"Hide inspector":"Inspector";
        bool show=_preferences.ShowInspector&&!_planningFocus;
        if(_inspectorHost!=null)_inspectorHost.IsVisible=show;
        if(_inspectorDivider!=null)_inspectorDivider.IsVisible=show;
        _main.ColumnDefinitions[2].Width=new GridLength(show?6:0);
        _main.ColumnDefinitions[3].Width=new GridLength(show?Math.Clamp(double.IsFinite(_preferences.InspectorWidth)?_preferences.InspectorWidth:300,260,480):0);
        foreach(var (index,button) in _viewTabs){button.Classes.Set("selected",index==_currentPage);AutomationProperties.SetHelpText(button,index==_currentPage?"Current view":"Switch view");}
    }
}

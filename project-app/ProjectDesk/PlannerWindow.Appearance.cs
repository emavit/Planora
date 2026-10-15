using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace ProjectDesk;

public sealed partial class PlannerWindow {
    private ComboBox? _themePicker;
    private readonly Dictionary<string,MenuItem> _themeItems=[];
    private bool _updatingTheme;
    public string ThemeMode=>NormalizeTheme(_preferences.ThemeMode);
    private static string NormalizeTheme(string? mode)=>mode is "Light" or "Dark"?mode:"Automatic";
    public void SetThemeMode(string mode){
        mode=NormalizeTheme(mode);_preferences.ThemeMode=mode;
        if(Application.Current is {} app)app.RequestedThemeVariant=mode switch{"Light"=>ThemeVariant.Light,"Dark"=>ThemeVariant.Dark,_=>ThemeVariant.Default};
        _updatingTheme=true;try{if(_themePicker!=null)_themePicker.SelectedItem=mode;foreach(var item in _themeItems)item.Value.IsChecked=item.Key==mode;}finally{_updatingTheme=false;}
        Persist();
    }
    private void AddAppearanceControls(Menu menus,RibbonModule ribbon){
        var appearance=new MenuItem{Header="Appearance"};
        var themeMenu=new MenuItem{Header="Theme"};
        foreach(string mode in new[]{"Automatic","Light","Dark"}){
            var item=new MenuItem{Header=mode,ToggleType=MenuItemToggleType.Radio,GroupName="ProjectDeskTheme"};item.Click+=(_,_)=>SetThemeMode(mode);_themeItems[mode]=item;themeMenu.Items.Add(item);
        }
        appearance.Items.Add(themeMenu);
        var colors=new MenuItem{Header="Color Gantt by Progress",ToggleType=MenuItemToggleType.CheckBox,IsChecked=_preferences.GanttColorByProgress};
        var labels=new MenuItem{Header="Gantt Progress Labels",ToggleType=MenuItemToggleType.CheckBox,IsChecked=_preferences.GanttProgressLabels};
        colors.Click+=(_,_)=>SetChartOptions(!Gantt.ColorByProgress,Gantt.ShowProgressLabels);
        labels.Click+=(_,_)=>SetChartOptions(Gantt.ColorByProgress,!Gantt.ShowProgressLabels);
        appearance.Items.Add(colors);appearance.Items.Add(labels);
        var today=new MenuItem{Header="Go to Today"};today.Click+=async(_,_)=>await RunMenu(GoToToday);appearance.Items.Add(today);menus.Items.Add(appearance);
        _themePicker=new ComboBox{Name="ThemeMode",ItemsSource=new[]{"Automatic","Light","Dark"},MinWidth=138,MinHeight=28};
        ToolTip.SetTip(_themePicker,"Automatic follows your computer’s light or dark appearance.");
        _themePicker.SelectionChanged+=(_,_)=>{if(!_updatingTheme&&_themePicker.SelectedItem is string selected)SetThemeMode(selected);};
        var colorToggle=new CheckBox{Name="GanttColorByProgress",Content="Color by progress",IsChecked=_preferences.GanttColorByProgress};
        var labelToggle=new CheckBox{Name="GanttProgressLabels",Content="Progress labels",IsChecked=_preferences.GanttProgressLabels};
        var legend=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12,Margin=new Thickness(2,3)};
        foreach(var (name,brush) in new[]{("Not started",GanttCanvas.NotStartedBrush),("In progress",GanttCanvas.InProgressBrush),("Completed",GanttCanvas.CompletedBrush)}){
            var entry=new StackPanel{Orientation=Orientation.Horizontal,Spacing=5};entry.Children.Add(new Border{Width=10,Height=10,Background=brush,CornerRadius=new CornerRadius(3),VerticalAlignment=VerticalAlignment.Center});entry.Children.Add(new TextBlock{Text=name,FontSize=11,Foreground=DesignTokens.Secondary});legend.Children.Add(entry);
        }
        void UpdateControls(){colors.IsChecked=Gantt.ColorByProgress;labels.IsChecked=Gantt.ShowProgressLabels;colorToggle.IsChecked=Gantt.ColorByProgress;labelToggle.IsChecked=Gantt.ShowProgressLabels;legend.IsVisible=Gantt.ColorByProgress;}
        _chartOptionsChanged=UpdateControls;
        colorToggle.IsCheckedChanged+=(_,_)=>{if(Gantt.ColorByProgress!=(colorToggle.IsChecked==true))SetChartOptions(colorToggle.IsChecked==true,Gantt.ShowProgressLabels);};
        labelToggle.IsCheckedChanged+=(_,_)=>{if(Gantt.ShowProgressLabels!=(labelToggle.IsChecked==true))SetChartOptions(Gantt.ColorByProgress,labelToggle.IsChecked==true);};
        ribbon.AddTab("Appearance",RibbonModule.Group("Theme",new TextBlock{Text="Appearance",Foreground=DesignTokens.Secondary,FontSize=11},_themePicker),RibbonModule.Group("Gantt clarity",colorToggle,labelToggle,legend),RibbonModule.Group("Navigate",Action("Go to Today",GoToToday),Action("Entire Project",()=>{_axis.EntireProject();return Task.CompletedTask;})));
        SetChartOptions(_preferences.GanttColorByProgress,_preferences.GanttProgressLabels);
        SetThemeMode(_preferences.ThemeMode);
    }
    private Action? _chartOptionsChanged;
    public void SetChartOptions(bool colorByProgress,bool progressLabels){
        Gantt.ColorByProgress=colorByProgress;Gantt.ShowProgressLabels=progressLabels;
        _preferences.GanttColorByProgress=colorByProgress;_preferences.GanttProgressLabels=progressLabels;
        _chartOptionsChanged?.Invoke();Gantt.InvalidateVisual();Persist();
    }
    public Task GoToToday(){
        SelectPage(1);
        if(!_planning.ScrollToDate(DateTime.Today)){
            _status.Text=$"Today ({DateTime.Today:dd MMM yyyy}) is outside the Gantt date range. Change Zoom and Gantt Dates to include it.";
        }else _status.Text="Gantt chart centered on today.";
        return Task.CompletedTask;
    }
}

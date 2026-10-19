using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ProjectDesk;

public sealed record SavedWorkspaceView(string Name,int Page,ColumnSetting[] Columns,string Search,string Filter,bool Timeline,bool Inspector,bool Compact,DateAxisView GanttDates);

public sealed partial class PlannerWindow {
    public IReadOnlyList<SavedWorkspaceView> SavedViews=>_preferences.SavedViews;
    public void SaveWorkspaceView(string name){
        name=name.Trim();if(name.Length is <1 or >80)throw new ArgumentException("Enter a view name of 1–80 characters.");
        if(_preferences.SavedViews.Any(v=>string.Equals(v.Name,name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("That name already exists. Choose another name or delete the old view.");
        if(_preferences.SavedViews.Count>=50)throw new ArgumentException("You can save up to 50 views.");
        Sheet.Persist();_preferences.SavedViews.Add(new(name,_currentPage,_preferences.Columns.ToArray(),_find.Text??"",_taskFilter.SelectedItem?.ToString()??"All tasks",_preferences.ShowTimeline,_preferences.ShowInspector,_preferences.CompactWorkspace,_axis.Capture()));Persist();
    }
    public void ApplyWorkspaceView(string name){
        var view=_preferences.SavedViews.Single(v=>v.Name==name);
        if(view.Page is <0 or >12||view.Columns==null||view.Columns.Length is <1 or >300||view.Columns.Any(c=>c==null||!double.IsFinite(c.Width)||c.Width<0)||view.GanttDates==null)throw new ArgumentException("This saved view is invalid.");
        var known=Sheet.Catalogue.Select(c=>c.Key).ToHashSet();if(view.Columns.Any(c=>!known.Contains(c.Key)))throw new ArgumentException("This project does not contain every field in the saved view.");
        if(!new[]{"All tasks","Milestones","Critical tasks","Incomplete tasks","Completed tasks"}.Contains(view.Filter))throw new ArgumentException("The saved task filter is unsupported.");
        var validationAxis=new DateAxis();validationAxis.Restore(view.GanttDates);
        if(!_inspector.TryCommit()||!Sheet.Grid.CommitEdit())return;
        _preferences.Columns=view.Columns.ToList();Sheet.ApplyColumns(view.Columns.Select(c=>c.Key));
        _find.Text=view.Search;_taskFilter.SelectedItem=view.Filter;Sheet.SetFilter(view.Search,view.Filter);
        _preferences.ShowTimeline=view.Timeline;_preferences.ShowInspector=view.Inspector;_preferences.CompactWorkspace=view.Compact;
        SelectPage(view.Page);_axis.Restore(view.GanttDates);Sheet.Persist();Persist();
    }
    public void DeleteWorkspaceView(string name){_preferences.SavedViews.RemoveAll(v=>v.Name==name);Persist();}
    private async Task ManageSavedViews(){
        var dialog=new Window{Title="Saved workspace views",Width=540,Height=480,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var body=new StackPanel{Margin=new Thickness(24),Spacing=12};body.Children.Add(new TextBlock{Text="Saved views",FontSize=22,FontWeight=FontWeight.SemiBold});body.Children.Add(new TextBlock{Text="Reuse columns, filters, dates and layout on this computer.",TextWrapping=TextWrapping.Wrap});
        var list=new ListBox{Height=175,Name="SavedViewList"};void Reload(){list.ItemsSource=SavedViews.Select(v=>v.Name).ToArray();}Reload();body.Children.Add(list);
        var name=new TextBox{Watermark="Name for the current workspace",Name="SavedViewName"};body.Children.Add(name);var status=new TextBlock{TextWrapping=TextWrapping.Wrap};body.Children.Add(status);
        var actions=new WrapPanel{Orientation=Orientation.Horizontal};
        void Add(string label,Action action){var b=new Button{Content=label,Margin=new Thickness(0,0,8,8)};b.Click+=(_,_)=>{try{action();}catch(Exception e){status.Text=e.Message;}};actions.Children.Add(b);}
        Add("Save current",()=>{SaveWorkspaceView(name.Text??"");Reload();status.Text="View saved.";});Add("Apply",()=>{if(list.SelectedItem is string selected){ApplyWorkspaceView(selected);dialog.Close();}});Add("Delete",()=>{if(list.SelectedItem is string selected){DeleteWorkspaceView(selected);Reload();status.Text="Saved view deleted.";}});Add("Close",()=>dialog.Close());body.Children.Add(actions);dialog.Content=body;await dialog.ShowDialog(this);
    }
    private async Task NewFromTemplate(){
        var choice=await new TemplateLibraryWindow().ShowDialog<TemplateChoice?>(this);if(choice==null)return;
        var staged=StarterTemplates.CreateState(choice.Id,choice.ProjectName,choice.Start);
        if(!await ResolveUnsavedChanges())return;
        _session.RestoreState(staged);Refresh();SelectPage(1);
    }
}

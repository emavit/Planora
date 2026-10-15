using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.ObjectModel;

namespace ProjectDesk;

public sealed class ColumnPickerWindow : Window {
    private readonly IReadOnlyList<TaskColumn> _catalogue;
    private readonly ListBox _available = new(), _chosen = new();
    private readonly ObservableCollection<TaskColumn> _selected;
    private readonly TextBox _search = new() { Watermark="Search all available task fields" };
    public bool ResetWidths {get;private set;}
    public ColumnPickerWindow(IReadOnlyList<TaskColumn> catalogue, IEnumerable<string> selected, string? insertBefore=null) {
        _catalogue=catalogue;
        _selected=new(selected.Select(k=>catalogue.FirstOrDefault(c=>c.Key==k)).OfType<TaskColumn>());
        Title="Insert and organise columns"; Width=790; Height=620; MinWidth=650; MinHeight=450;
        WindowStartupLocation=WindowStartupLocation.CenterOwner; Background=DesignTokens.Background; FontFamily=DesignTokens.Font;
        var root=new DockPanel{Margin=new Thickness(20)};
        var heading=new StackPanel{Spacing=8,Margin=new Thickness(0,0,0,16)};
        heading.Children.Add(new TextBlock{Text="Choose columns",FontSize=22});
        heading.Children.Add(new TextBlock{Text=$"{catalogue.Count} available fields · Insert before the selected column, or append at the end.",TextWrapping=Avalonia.Media.TextWrapping.Wrap});
        heading.Children.Add(_search); DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        var footer=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};
        footer.Children.Add(Action("Cancel",()=>Close(null)));
        footer.Children.Add(Action("Apply columns",()=>{if(_selected.Count>0)Close(_selected.Select(c=>c.Key).ToList());}));
        DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var layout=new Grid{ColumnDefinitions=new ColumnDefinitions("*,120,*")};
        var left=new DockPanel(); var leftTitle=new TextBlock{Text="Available fields",Margin=new Thickness(0,0,0,8)};DockPanel.SetDock(leftTitle,Dock.Top);left.Children.Add(leftTitle);left.Children.Add(_available);layout.Children.Add(left);
        var actions=new StackPanel{Spacing=10,Margin=new Thickness(10),VerticalAlignment=VerticalAlignment.Center};
        actions.Children.Add(Action("Insert →",Insert));actions.Children.Add(Action("Remove",()=>{if(_chosen.SelectedItem is TaskColumn c)_selected.Remove(c);Update();}));
        actions.Children.Add(Action("Move up",()=>Move(-1)));actions.Children.Add(Action("Move down",()=>Move(1)));
        actions.Children.Add(Action("Reset",()=>{ResetWidths=true;_selected.Clear();foreach(var k in TaskColumns.Defaults)if(catalogue.FirstOrDefault(c=>c.Key==k)is TaskColumn c)_selected.Add(c);Update();}));
        Grid.SetColumn(actions,1);layout.Children.Add(actions);
        var right=new DockPanel();var rightTitle=new TextBlock{Text="Visible columns, in order",Margin=new Thickness(0,0,0,8)};DockPanel.SetDock(rightTitle,Dock.Top);right.Children.Add(rightTitle);right.Children.Add(_chosen);Grid.SetColumn(right,2);layout.Children.Add(right);
        root.Children.Add(layout);Content=root;_chosen.ItemsSource=_selected;
        _search.TextChanged+=(_,_)=>Update();_available.DoubleTapped+=(_,_)=>Insert();
        if(insertBefore!=null)_chosen.SelectedItem=_selected.FirstOrDefault(c=>c.Key==insertBefore);Update();
    }
    private static Button Action(string text,Action action){var b=new Button{Content=text};b.Click+=(_,_)=>action();return b;}
    private void Update()=>_available.ItemsSource=_catalogue.Where(c=>!_selected.Any(s=>s.Key==c.Key)&&c.Title.Contains(_search.Text??"",StringComparison.OrdinalIgnoreCase)).ToList();
    private void Insert(){if(_available.SelectedItem is not TaskColumn c)return;int i=_chosen.SelectedIndex;if(i<0)i=_selected.Count;_selected.Insert(i,c);_chosen.SelectedItem=c;Update();}
    private void Move(int delta){int i=_chosen.SelectedIndex,j=i+delta;if(i<0||j<0||j>=_selected.Count)return;_selected.Move(i,j);_chosen.SelectedIndex=j;}
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Text.Json;

namespace ProjectDesk;

public sealed record ProjectGuiCommand(int Id,string Label,string Group,List<string> Paths,string Reason);
public static class ProjectCommandCatalogue {
    public static IReadOnlyList<ProjectGuiCommand> Observed {get;}=Load();
    private static IReadOnlyList<ProjectGuiCommand> Load(){using var stream=typeof(ProjectCommandCatalogue).Assembly.GetManifestResourceStream("ProjectDesk.Resources.ProjectCommands.json")??throw new InvalidOperationException("Command catalogue is missing.");return JsonSerializer.Deserialize<List<ProjectGuiCommand>>(stream)??[];}
}
public sealed class ProjectCommandsWindow : Window {
    private readonly ListBox _list=new();private readonly TextBox _search=new(){Watermark="Search commands",Width=420};private readonly CheckBox _ready=new(){Content="Available commands only",IsChecked=false};private readonly TextBlock _detail=new(){TextWrapping=Avalonia.Media.TextWrapping.Wrap};
    private readonly IReadOnlyDictionary<int,Func<Task>> _actions;
    public ProjectCommandsWindow(IReadOnlyDictionary<int,Func<Task>> actions){_actions=actions;Title="All Project commands";Width=800;Height=650;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=DesignTokens.Background;
        var root=new DockPanel{Margin=new Thickness(18)};var top=new StackPanel{Spacing=8};top.Children.Add(new TextBlock{Text="Project commands",FontSize=22});top.Children.Add(new TextBlock{Text=$"{ProjectCommandCatalogue.Observed.Count} commands found in installed Project · {ProjectCommandCatalogue.Observed.Count(c=>actions.ContainsKey(c.Id))} available here",TextWrapping=Avalonia.Media.TextWrapping.Wrap});top.Children.Add(_search);top.Children.Add(_ready);DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        var bottom=new StackPanel{Spacing=10,Margin=new Thickness(0,12,0,0)};bottom.Children.Add(_detail);var open=new Button{Content="Run command",HorizontalAlignment=HorizontalAlignment.Right,IsEnabled=false};open.Click+=(_,_)=>{if(_list.SelectedItem is CommandRow r&&r.Available)Close(r.Command.Id);};bottom.Children.Add(open);DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);root.Children.Add(_list);Content=root;
        _list.SelectionChanged+=(_,_)=>{if(_list.SelectedItem is CommandRow r){open.IsEnabled=r.Available;_detail.Text=(r.Available?"Available in Planora.":r.Command.Reason)+"\nProject location: "+r.Command.Paths.FirstOrDefault();}};
        _search.TextChanged+=(_,_)=>Filter();_ready.IsCheckedChanged+=(_,_)=>Filter();_list.DoubleTapped+=(_,_)=>{if(_list.SelectedItem is CommandRow{Available:true} r)Close(r.Command.Id);};Filter();
    }
    private void Filter()=>_list.ItemsSource=ProjectCommandCatalogue.Observed.Where(c=>(!(_ready.IsChecked==true)||_actions.ContainsKey(c.Id))&&(c.Label+" "+c.Group+" "+string.Join(" ",c.Paths)).Contains(_search.Text??"",StringComparison.OrdinalIgnoreCase)).OrderBy(c=>c.Group).ThenBy(c=>c.Label).Select(c=>new CommandRow(c,_actions.ContainsKey(c.Id))).ToArray();
    private sealed record CommandRow(ProjectGuiCommand Command,bool Available){public override string ToString()=>$"{Command.Group}  ›  {Command.Label}"+(Available?"":" — unavailable");}
}

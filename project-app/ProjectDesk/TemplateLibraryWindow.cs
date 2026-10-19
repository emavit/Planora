using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ProjectDesk;

public sealed record TemplateChoice(string Id,string ProjectName,DateTime Start);
public sealed class TemplateLibraryWindow : Window {
    public TemplateLibraryWindow(){
        Title="New project from template";Width=850;Height=650;MinWidth=700;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(24)};
        var heading=new StackPanel{Spacing=10,Margin=new Thickness(0,0,0,16)};
        heading.Children.Add(new TextBlock{Text="Start with a plan",FontSize=26,FontWeight=FontWeight.SemiBold});
        heading.Children.Add(new TextBlock{Text="Original schedule starters. Preview the tasks, then make them your own.",Foreground=DesignTokens.Secondary});
        var search=new TextBox{Name="TemplateSearch",Watermark="Search templates or categories"};AutomationProperties.SetName(search,"Search templates");heading.Children.Add(search);DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        var grid=new Grid{ColumnDefinitions=new ColumnDefinitions("260,24,*")};
        var list=new ListBox{Name="TemplateList",ItemsSource=StarterTemplates.All.Select(t=>t.Category+" · "+t.Name).ToArray(),ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<string>((text,_)=>new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,MaxWidth=225})};grid.Children.Add(list);
        var detail=new StackPanel{Spacing=12};Grid.SetColumn(detail,2);grid.Children.Add(new ScrollViewer{Content=detail});Grid.SetColumn(grid.Children[1],2);
        var title=new TextBlock{FontSize=20,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap};var description=new TextBlock{TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary};var tasks=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var name=new TextBox{Name="TemplateProjectName"};AutomationProperties.SetName(name,"Project name");var date=new DatePicker{Name="TemplateStart",SelectedDate=new DateTimeOffset(DateTime.Today)};AutomationProperties.SetName(date,"Project start date");
        detail.Children.Add(title);detail.Children.Add(description);detail.Children.Add(tasks);detail.Children.Add(new TextBlock{Text="Project name"});detail.Children.Add(name);detail.Children.Add(new TextBlock{Text="Start date"});detail.Children.Add(date);
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap};detail.Children.Add(status);
        var create=new Button{Content="Create project",Name="TemplateCreate"};create.Classes.Add("primary");var cancel=new Button{Content="Cancel"};cancel.Click+=(_,_)=>Close(null);detail.Children.Add(new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,Children={create,cancel}});
        StarterTemplate[] visible=StarterTemplates.All.ToArray();StarterTemplate? selected=null;
        void Select(){selected=list.SelectedIndex>=0&&list.SelectedIndex<visible.Length?visible[list.SelectedIndex]:null;create.IsEnabled=selected!=null;title.Text=selected?.Name??"No matching templates";description.Text=selected?.Description;tasks.Text=selected==null?"":string.Join("\n",selected.Tasks.Select((t,i)=>$"{i+1}. {t.Name} · {(t.Days==0?"milestone":t.Days+" working days")}"))+"\n\nTasks are linked finish-to-start using a standard working week.";name.Text=selected?.Name;}
        list.SelectionChanged+=(_,_)=>Select();search.TextChanged+=(_,_)=>{string query=search.Text??"";visible=StarterTemplates.All.Where(t=>(t.Category+" "+t.Name+" "+t.Description).Contains(query,StringComparison.OrdinalIgnoreCase)).ToArray();list.ItemsSource=visible.Select(t=>t.Category+" · "+t.Name).ToArray();list.SelectedIndex=visible.Length>0?0:-1;Select();};
        create.Click+=(_,_)=>{if(selected==null)return;if(string.IsNullOrWhiteSpace(name.Text)||name.Text.Length>200||date.SelectedDate==null){status.Text="Enter a project name and start date.";return;}Close(new TemplateChoice(selected.Id,name.Text.Trim(),date.SelectedDate.Value.Date));};
        root.Children.Add(grid);Content=root;list.SelectedIndex=0;
    }
}

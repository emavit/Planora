using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
namespace ProjectDesk;
public sealed class RibbonModule : UserControl {
 public static readonly SolidColorBrush Chrome=new SolidColorBrush(Color.Parse("#262626"));
 public static readonly SolidColorBrush Muted=new SolidColorBrush(Color.Parse("#CACACA"));
 public static readonly SolidColorBrush Green=new SolidColorBrush(Color.Parse("#63C28A"));
    public static readonly SolidColorBrush Ink=new(Color.Parse("#FFFFFF"));
    public static void ApplyAppearance(bool dark){DesignTokens.ApplyAppearance(dark);Chrome.Color=Color.Parse(dark?"#20242A":"#F3F5F7");Ink.Color=Color.Parse(dark?"#EFF1F4":"#20252B");Muted.Color=Color.Parse(dark?"#CACACA":"#626269");Green.Color=Color.Parse(dark?"#63C28A":"#217346");}
    private readonly StackPanel _tabs=new(){Orientation=Orientation.Horizontal};
 private readonly ContentControl _commands=new();
 private readonly ContextMenu _tabMenu=new();
 private readonly Dictionary<string,Control> _panels=[];
 private readonly Dictionary<string,Button> _buttons=[];
 public string SelectedTab {get;private set;}="";
 public RibbonModule(){
  Background=Brushes.Transparent;Foreground=Ink;
  Styles.Add(new Style(s=>s.OfType<Button>()){Setters={new Setter(Button.BackgroundProperty,Brushes.Transparent),new Setter(Button.ForegroundProperty,Ink),new Setter(Button.BorderThicknessProperty,new Thickness(0)),new Setter(Button.CornerRadiusProperty,new CornerRadius(6)),new Setter(Button.PaddingProperty,new Thickness(9,4)),new Setter(Button.MinHeightProperty,25d),new Setter(Button.FontSizeProperty,13d)}});
  Styles.Add(new Style(s=>s.OfType<Button>().Class("ribbonTab")){Setters={new Setter(Button.PaddingProperty,new Thickness(14,9)),new Setter(Button.FontSizeProperty,13d),new Setter(Button.CornerRadiusProperty,new CornerRadius(12))}});
  Styles.Add(new Style(s=>s.OfType<Button>().Class("ribbonTab").Class("active")){Setters={new Setter(Button.BorderBrushProperty,Green),new Setter(Button.BorderThicknessProperty,new Thickness(0,0,0,3)),new Setter(Button.BackgroundProperty,DesignTokens.Selected),new Setter(Button.FontWeightProperty,FontWeight.SemiBold)}});
  Styles.Add(new Style(s=>s.OfType<CheckBox>()){Setters={new Setter(CheckBox.ForegroundProperty,Ink)}});
  var collapse=new Button{Content="⌃",Width=34};ToolTip.SetTip(collapse,"Show / hide ribbon commands");collapse.Click+=(_,_)=>_commands.IsVisible=!_commands.IsVisible;
  var more=new Button{Content="▾",Width=30,ContextMenu=_tabMenu};Avalonia.Automation.AutomationProperties.SetName(more,"All ribbon tabs");ToolTip.SetTip(more,"All ribbon tabs");more.Click+=(_,_)=>_tabMenu.Open(more);
  var tabs=new DockPanel{Margin=new Thickness(6,0)};DockPanel.SetDock(collapse,Dock.Right);tabs.Children.Add(collapse);DockPanel.SetDock(more,Dock.Right);tabs.Children.Add(more);tabs.Children.Add(new ScrollViewer{Content=_tabs,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled});
  var root=new DockPanel();DockPanel.SetDock(tabs,Dock.Top);root.Children.Add(tabs);root.Children.Add(new Border{Background=Brushes.Transparent,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(0,0,0,1),Padding=new Thickness(6,5),Child=_commands});Content=root;
 }
 public void AddTab(string name,params Control[] groups){var panel=new StackPanel{Orientation=Orientation.Horizontal};foreach(var group in groups)panel.Children.Add(group);_panels[name]=new ScrollViewer{Content=panel,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};var b=new Button{Content=name};b.Classes.Add("ribbonTab");if(name.Contains("Format"))b.Foreground=Green;b.Click+=(_,_)=>SelectTab(name);_tabs.Children.Add(b);_buttons[name]=b;var menuItem=new MenuItem{Header=name,ToggleType=MenuItemToggleType.Radio,GroupName="RibbonTabs"};menuItem.Click+=(_,_)=>SelectTab(name);_tabMenu.Items.Add(menuItem);if(_panels.Count==1)SelectTab(name);}
 public void SelectTab(string name){SelectedTab=name;foreach(var item in _tabMenu.Items.OfType<MenuItem>())item.IsChecked=Equals(item.Header,name);_commands.Content=_panels[name];foreach(var (key,b) in _buttons){if(key==name)b.Classes.Add("active");else b.Classes.Remove("active");}}
 public static Control Group(string name,params Control[] commands){var body=new DockPanel{MinHeight=102};var caption=new TextBlock{Text=name,FontSize=11,Foreground=Muted,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,4,0,0)};DockPanel.SetDock(caption,Dock.Bottom);body.Children.Add(caption);var columns=new StackPanel{Orientation=Orientation.Horizontal,Spacing=4};StackPanel? column=null;for(int i=0;i<commands.Length;i++){if(i%3==0){column=new StackPanel{Spacing=1};columns.Children.Add(column);}column!.Children.Add(commands[i]);}body.Children.Add(columns);return new Border{Padding=new Thickness(10,0),BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(0,0,1,0),Child=body};}
 public static Control Row(params Control[] controls){var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=2};foreach(var c in controls)row.Children.Add(c);return row;}
 public static Button Large(Button b,string symbol,string label){b.Content=new StackPanel{Spacing=3,Children={new TextBlock{Text=symbol,FontSize=28,Foreground=new SolidColorBrush(Color.Parse("#1596AC")),HorizontalAlignment=HorizontalAlignment.Center},new TextBlock{Text=label,TextAlignment=TextAlignment.Center}}};b.MinHeight=76;return b;}
}

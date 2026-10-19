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
    public static void ApplyAppearance(bool dark){DesignTokens.ApplyAppearance(dark);Chrome.Color=DesignTokens.Background.Color;Ink.Color=DesignTokens.Text.Color;Muted.Color=Color.Parse(dark?"#CACACA":"#626269");Green.Color=Color.Parse(dark?"#63C28A":"#217346");}
    private readonly StackPanel _tabs=new(){Orientation=Orientation.Horizontal};
 private readonly ContentControl _commands=new();
 private readonly Button _collapse=new(){Content="Hide controls",Name="RibbonCollapse"};
 private readonly ContextMenu _tabMenu=new();
 private readonly Dictionary<string,Control> _panels=[];
 private readonly Dictionary<string,Button> _buttons=[];
 public string SelectedTab {get;private set;}="";
 public RibbonModule(){
  Background=Brushes.Transparent;Foreground=Ink;
  Styles.Add(new Style(s=>s.OfType<Button>()){Setters={new Setter(Button.BackgroundProperty,Brushes.Transparent),new Setter(Button.ForegroundProperty,Ink),new Setter(Button.BorderThicknessProperty,new Thickness(0)),new Setter(Button.CornerRadiusProperty,new CornerRadius(6)),new Setter(Button.PaddingProperty,new Thickness(9,4)),new Setter(Button.MinHeightProperty,25d),new Setter(Button.FontSizeProperty,13d)}});
  Styles.Add(new Style(s=>s.OfType<Button>().Class("ribbonTab")){Setters={new Setter(Button.PaddingProperty,new Thickness(13,6)),new Setter(Button.FontSizeProperty,13d),new Setter(Button.CornerRadiusProperty,new CornerRadius(7))}});
  Styles.Add(new Style(s=>s.OfType<Button>().Class("ribbonTab").Class("active")){Setters={new Setter(Button.BorderBrushProperty,DesignTokens.Accent),new Setter(Button.BorderThicknessProperty,new Thickness(0,0,0,2)),new Setter(Button.BackgroundProperty,DesignTokens.Selected),new Setter(Button.ForegroundProperty,DesignTokens.Accent),new Setter(Button.FontWeightProperty,FontWeight.SemiBold)}});
  Styles.Add(new Style(s=>s.OfType<CheckBox>()){Setters={new Setter(CheckBox.ForegroundProperty,Ink)}});
  var collapse=_collapse;ToolTip.SetTip(collapse,"Show or hide ribbon commands");Avalonia.Automation.AutomationProperties.SetName(collapse,"Show or hide ribbon commands");collapse.Click+=(_,_)=>{_commands.IsVisible=!_commands.IsVisible;collapse.Content=_commands.IsVisible?"Hide controls":"Show controls";};
  var more=new Button{Content="▾",Width=30,ContextMenu=_tabMenu};Avalonia.Automation.AutomationProperties.SetName(more,"All ribbon tabs");ToolTip.SetTip(more,"All ribbon tabs");more.Click+=(_,_)=>_tabMenu.Open(more);
  var tabs=new DockPanel{Margin=new Thickness(6,0)};DockPanel.SetDock(collapse,Dock.Right);tabs.Children.Add(collapse);DockPanel.SetDock(more,Dock.Right);tabs.Children.Add(more);tabs.Children.Add(new ScrollViewer{Content=_tabs,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled});
  var root=new DockPanel();DockPanel.SetDock(tabs,Dock.Top);root.Children.Add(tabs);root.Children.Add(new Border{Background=Brushes.Transparent,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(0,0,0,1),Padding=new Thickness(6,5),Child=_commands});Content=root;
 }
 public void AddTab(string name,params Control[] groups){var panel=new StackPanel{Orientation=Orientation.Horizontal,Spacing=6,Margin=new Thickness(2)};foreach(var group in groups)panel.Children.Add(group);var scroll=new ScrollViewer{Content=panel,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};
 var strip=new Grid{ColumnDefinitions=new ColumnDefinitions("Auto,*,Auto")};
 var left=new Button{Content="‹",Width=26,Height=32,VerticalAlignment=VerticalAlignment.Center,Padding=new Thickness(0),Name="RibbonPreviousGroups"};var right=new Button{Content="›",Width=26,Height=32,VerticalAlignment=VerticalAlignment.Center,Padding=new Thickness(0),Name="RibbonNextGroups"};
 Avalonia.Automation.AutomationProperties.SetName(left,"Previous ribbon commands");Avalonia.Automation.AutomationProperties.SetName(right,"Next ribbon commands");
 ToolTip.SetTip(left,"Scroll to previous commands");ToolTip.SetTip(right,"Scroll to more commands");
 left.Click+=(_,_)=>scroll.Offset=new Vector(Math.Max(0,scroll.Offset.X-Math.Max(240,scroll.Viewport.Width*.7)),0);
 right.Click+=(_,_)=>scroll.Offset=new Vector(Math.Min(Math.Max(0,scroll.Extent.Width-scroll.Viewport.Width),scroll.Offset.X+Math.Max(240,scroll.Viewport.Width*.7)),0);
 scroll.LayoutUpdated+=(_,_)=>{left.IsEnabled=scroll.Offset.X>0;right.IsEnabled=scroll.Offset.X<scroll.Extent.Width-scroll.Viewport.Width-1;left.Opacity=left.IsEnabled?1:0;right.Opacity=right.IsEnabled?1:0;};
 strip.Children.Add(left);Grid.SetColumn(scroll,1);strip.Children.Add(scroll);Grid.SetColumn(right,2);strip.Children.Add(right);_panels[name]=strip;var b=new Button{Content=name,Name="RibbonTab"+name.Replace(" ","")};Avalonia.Automation.AutomationProperties.SetName(b,name+" ribbon tab");b.Classes.Add("ribbonTab");if(name.Contains("Format"))b.Foreground=Green;b.Click+=(_,_)=>SelectTab(name);_tabs.Children.Add(b);_buttons[name]=b;var menuItem=new MenuItem{Header=name,ToggleType=MenuItemToggleType.Radio,GroupName="RibbonTabs"};menuItem.Click+=(_,_)=>SelectTab(name);_tabMenu.Items.Add(menuItem);if(_panels.Count==1)SelectTab(name);}
 public void SelectTab(string name){_commands.IsVisible=true;_collapse.Content="Hide controls";SelectedTab=name;foreach(var item in _tabMenu.Items.OfType<MenuItem>())item.IsChecked=Equals(item.Header,name);_commands.Content=_panels[name];foreach(var (key,b) in _buttons){if(key==name)b.Classes.Add("active");else b.Classes.Remove("active");}}
 public static Control Group(string name,params Control[] commands){
  var body=new DockPanel{MinHeight=92};
  var caption=new TextBlock{Text=name,FontSize=11,FontWeight=FontWeight.SemiBold,Foreground=DesignTokens.Secondary,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(3,5,3,0)};
  DockPanel.SetDock(caption,Dock.Bottom);body.Children.Add(caption);
  var columns=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};StackPanel? column=null;
  for(int i=0;i<commands.Length;i++){if(i%3==0){column=new StackPanel{Spacing=1};columns.Children.Add(column);}column!.Children.Add(commands[i]);}
  body.Children.Add(columns);
  return new Border{Padding=new Thickness(9,6),Background=DesignTokens.Surface,CornerRadius=new CornerRadius(6),BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(0,0,1,0),Child=body};
 }
 public static Control Row(params Control[] controls){var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=2};foreach(var c in controls)row.Children.Add(c);return row;}
 public static Button Large(Button b,string symbol,string label){b.Content=new StackPanel{Spacing=3,Children={new TextBlock{Text=symbol,FontSize=28,Foreground=new SolidColorBrush(Color.Parse("#1596AC")),HorizontalAlignment=HorizontalAlignment.Center},new TextBlock{Text=label,TextAlignment=TextAlignment.Center}}};b.MinHeight=76;return b;}
}

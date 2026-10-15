using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
namespace ProjectDesk;
public sealed class ChartColorsWindow : EditorForm<Dictionary<string,string>> {
 private readonly Dictionary<string,string> _colors;
 public ChartColorsWindow(Dictionary<string,string> current):base("Gantt colors"){Height=720;_colors=new(current);foreach(var (key,label) in new[]{("Task","Task bars"),("Progress","Progress fill"),("Critical","Critical outline / bars"),("Milestone","Milestones"),("Summary","Summary tasks"),("Baseline","Baseline bars"),("Links","Dependency arrows"),("Today","Today line"),("Weekend","Non-working days")}){
  string name=key;var swatch=new Border{Width=34,Height=24,CornerRadius=new CornerRadius(5),Background=FormatVisuals.Brush(_colors.GetValueOrDefault(name),DesignTokens.Secondary),Margin=new Thickness(0,0,10,0)};var value=new TextBlock{Text=_colors.GetValueOrDefault(name)??"Automatic",VerticalAlignment=VerticalAlignment.Center};var button=new Button{Content="Choose…"};var row=new DockPanel();DockPanel.SetDock(swatch,Dock.Left);DockPanel.SetDock(button,Dock.Right);row.Children.Add(swatch);row.Children.Add(button);row.Children.Add(value);button.Click+=async(_,_)=>{var color=await new OfficeColorWindow(label,_colors.GetValueOrDefault(name)).ShowDialog<ColorChoice?>(this);if(color==null)return;if(color.Color==null)_colors.Remove(name);else _colors[name]=color.Color;value.Text=color.Color??"Automatic";swatch.Background=FormatVisuals.Brush(color.Color,DesignTokens.Secondary);};Field(label,row);
 }
 }
 protected override Dictionary<string,string> Read()=>new(_colors);
}

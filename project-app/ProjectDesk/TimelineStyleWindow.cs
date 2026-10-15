using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ProjectDesk;

public sealed record TimelineStyle(string? BarColor=null,string? MilestoneColor=null,string? TextColor=null,double FontSize=11,bool ShowDates=true,bool RoundedBars=true){
    public TimelineStyle Checked(){
        if(!double.IsFinite(FontSize)||FontSize<8||FontSize>16)throw new ArgumentException("Timeline font size must be between 8 and 16.");
        return this with{BarColor=CheckColor(BarColor),MilestoneColor=CheckColor(MilestoneColor),TextColor=CheckColor(TextColor)};
    }
    public static string? CheckColor(string? value){if(string.IsNullOrWhiteSpace(value))return null;if(!Color.TryParse(value,out var color))throw new ArgumentException("Enter a valid color, such as #397FD1, or leave it blank for Automatic.");return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";}
}
public sealed record TimelineItemStyle(string? Label=null,string? Color=null){public TimelineItemStyle Checked()=>this with{Label=string.IsNullOrWhiteSpace(Label)?null:Label.Trim(),Color=TimelineStyle.CheckColor(Color)};}
public sealed class TimelineStyleWindow : EditorForm<TimelineStyle>{
    private readonly TextBox _bar,_milestone,_text;
    private readonly NumericUpDown _size;
    private readonly CheckBox _dates,_rounded;
    public TimelineStyleWindow(string name,TimelineStyle style):base(name+" · Timeline appearance"){
        Height=650;_bar=new(){Name="TimelineBarColor",Text=style.BarColor,Watermark="Automatic"};_milestone=new(){Name="TimelineMilestoneColor",Text=style.MilestoneColor,Watermark="Automatic"};_text=new(){Name="TimelineTextColor",Text=style.TextColor,Watermark="Automatic"};
        _size=new(){Name="TimelineFontSize",Minimum=8,Maximum=16,Value=(decimal)style.FontSize};_dates=new(){Name="TimelineShowDates",Content="Show start and finish dates",IsChecked=style.ShowDates};_rounded=new(){Name="TimelineRoundedBars",Content="Rounded task bars",IsChecked=style.RoundedBars};
        Field("Task bars",ColorRow(_bar,"Timeline bar color"));Field("Milestones",ColorRow(_milestone,"Timeline milestone color"));Field("Text",ColorRow(_text,"Timeline text color"),"Automatic follows the selected light or dark theme.");Field("Font size",_size);Field("Graphics",new StackPanel{Spacing=8,Children={_dates,_rounded}});
    }
    private Control ColorRow(TextBox input,string title){var row=new DockPanel();var choose=new Button{Content="Choose…",Margin=new Avalonia.Thickness(8,0,0,0)};DockPanel.SetDock(choose,Dock.Right);row.Children.Add(choose);row.Children.Add(input);choose.Click+=async(_,_)=>{var color=await new OfficeColorWindow(title,input.Text).ShowDialog<ColorChoice?>(this);if(color!=null)input.Text=color.Color;};return row;}
    protected override TimelineStyle Read()=>new TimelineStyle(_bar.Text,_milestone.Text,_text.Text,(double)(_size.Value??11),_dates.IsChecked==true,_rounded.IsChecked==true).Checked();
}
public sealed class TimelineItemStyleWindow : EditorForm<TimelineItemStyle>{
    private readonly TextBox _label,_color;
    public TimelineItemStyleWindow(string taskName,TimelineItemStyle style):base("Timeline block appearance"){
        Height=440;_label=new(){Name="TimelineBlockLabel",Text=style.Label,Watermark=taskName};_color=new(){Name="TimelineBlockColor",Text=style.Color,Watermark="Automatic"};
        Field("Display label",_label,"Leave blank to use the task name. This changes the timeline label only.");var choose=new Button{Content="Choose color…"};choose.Click+=async(_,_)=>{var choice=await new OfficeColorWindow("Block color",_color.Text).ShowDialog<ColorChoice?>(this);if(choice!=null)_color.Text=choice.Color;};Field("Bar / milestone color",new StackPanel{Spacing=6,Children={_color,choose}},"Leave blank to use this timeline’s default color.");
    }
    protected override TimelineItemStyle Read()=>new TimelineItemStyle(_label.Text,_color.Text).Checked();
}

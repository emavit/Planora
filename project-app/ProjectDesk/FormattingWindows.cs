using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Automation;
namespace ProjectDesk;

public sealed class OfficeColorWindow : Window {
 public static readonly string[] ThemeColors=["#FFFFFF","#000000","#E7E6E6","#44546A","#4472C4","#ED7D31","#A5A5A5","#FFC000","#5B9BD5","#70AD47"];
 private readonly TextBox _hex=new(){Watermark="#RRGGBB or #AARRGGBB",Width=200};
 private readonly NumericUpDown[] _channels=Enumerable.Range(0,4).Select(_=>new NumericUpDown{Minimum=0,Maximum=255,Increment=1,Width=100,ShowButtonSpinner=false,Padding=new Thickness(4)}).ToArray();
 private readonly NumericUpDown[] _hsl=[new(){Minimum=0,Maximum=360,Width=110,ShowButtonSpinner=false,FormatString="0.##",Padding=new Thickness(4)},new(){Minimum=0,Maximum=100,Width=110,ShowButtonSpinner=false,FormatString="0.##",Padding=new Thickness(4)},new(){Minimum=0,Maximum=100,Width=110,ShowButtonSpinner=false,FormatString="0.##",Padding=new Thickness(4)}];
 private readonly Border _sample=new(){Height=48,CornerRadius=new CornerRadius(8)};
 private readonly TextBlock _error=new(){Foreground=ChartDrawing.Critical};private bool _changing;
 public OfficeColorWindow(string title,string? current,IEnumerable<string>? recent=null){Title=title;Width=490;Height=790;CanResize=false;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=DesignTokens.Background;
  var root=new StackPanel{Margin=new Thickness(22),Spacing=14};root.Children.Add(new TextBlock{Text=title,FontSize=22,FontWeight=FontWeight.SemiBold});root.Children.Add(new TextBlock{Text="Theme colors",Foreground=DesignTokens.Secondary});
  var palette=new WrapPanel{ItemSpacing=6,LineSpacing=6};foreach(string color in ThemeColors.Concat(new[]{.8,.6,.4}.SelectMany(t=>ThemeColors.Select(hex=>Tint(hex,t)))).Concat(new[]{"#C00000","#FF0000","#FFC000","#FFFF00","#92D050","#00B050","#00B0F0","#0070C0","#002060","#7030A0"}).Concat(recent??[]).Distinct()){
   var b=new Button{Width=34,Height=30,MinHeight=30,Padding=new Thickness(0),Background=FormatVisuals.Brush(color,Brushes.Transparent),BorderBrush=DesignTokens.Separator};AutomationProperties.SetName(b,"Color "+color);ToolTip.SetTip(b,color);b.Click+=(_,_)=>Set(Color.Parse(color));palette.Children.Add(b);
  }root.Children.Add(palette);root.Children.Add(_sample);root.Children.Add(_hex);var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};string[] labels=["Red","Green","Blue","Alpha"];for(int i=0;i<4;i++){var item=new StackPanel{Spacing=3};item.Children.Add(new TextBlock{Text=labels[i],FontSize=11});item.Children.Add(_channels[i]);row.Children.Add(item);_channels[i].ValueChanged+=(_,_)=>{if(_changing)return;Set(Color.FromArgb((byte)(_channels[3].Value??255),(byte)(_channels[0].Value??0),(byte)(_channels[1].Value??0),(byte)(_channels[2].Value??0)));};}root.Children.Add(row);var hslRow=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10};string[] hslLabels=["Hue","Saturation %","Lightness %"];for(int i=0;i<3;i++){hslRow.Children.Add(new StackPanel{Children={new TextBlock{Text=hslLabels[i],FontSize=11},_hsl[i]}});_hsl[i].ValueChanged+=(_,_)=>{if(!_changing)Set(HslColor.ToRgb((double)(_channels[3].Value??255)/255,(double)(_hsl[0].Value??0),(double)(_hsl[1].Value??0)/100,(double)(_hsl[2].Value??0)/100));};}root.Children.Add(hslRow);
  _hex.TextChanged+=(_,_)=>{if(!_changing&&Color.TryParse(_hex.Text,out var c))Set(c);};root.Children.Add(_error);var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Spacing=8};
  var auto=new Button{Content="Automatic / None"};auto.Click+=(_,_)=>Close(new ColorChoice(true,null));var cancel=new Button{Content="Cancel"};cancel.Click+=(_,_)=>Close(null);var apply=new Button{Content="Apply"};apply.Classes.Add("primary");apply.Click+=(_,_)=>{if(!Color.TryParse(_hex.Text,out var c)){_error.Text="Enter a valid hexadecimal color.";return;}Close(new ColorChoice(true,c.ToString()));};actions.Children.Add(auto);actions.Children.Add(cancel);actions.Children.Add(apply);root.Children.Add(actions);Content=root;Set(current!=null&&Color.TryParse(current,out var selected)?selected:Colors.Black);
 }
 private static string Tint(string hex,double mix){var c=Color.Parse(hex);return Color.FromRgb((byte)Math.Round(c.R+(255-c.R)*mix),(byte)Math.Round(c.G+(255-c.G)*mix),(byte)Math.Round(c.B+(255-c.B)*mix)).ToString();}
 private void Set(Color color){_changing=true;_hex.Text=color.ToString();_channels[0].Value=color.R;_channels[1].Value=color.G;_channels[2].Value=color.B;_channels[3].Value=color.A;_sample.Background=new SolidColorBrush(color);var hsl=color.ToHsl();_hsl[0].Value=(decimal)hsl.H;_hsl[1].Value=(decimal)(hsl.S*100);_hsl[2].Value=(decimal)(hsl.L*100);_changing=false;}
}
public sealed record ColorChoice(bool Apply,string? Color);

public sealed class TextFormatWindow : EditorForm<TextFormat> {
 private readonly ComboBox _font,_underline,_strike,_vertical,_caps,_alignment;
 private readonly NumericUpDown _size,_spacing,_indent,_borderWidth;
 private readonly CheckBox _bold,_italic,_wrap;
 private readonly TextBox _foreground,_highlight,_border;
 private readonly TextBlock _preview=new(){Text="Aa  Project planning",FontSize=18,Margin=new Thickness(8)};
 private readonly TextFormat _original;
 private readonly ComboBox _scope=new(){ItemsSource=new[]{"Selected cells","Selected rows","All visible rows"}};
 public string Scope=>_scope.SelectedItem?.ToString()??"Selected cells";
 public TextFormatWindow(TextFormat current,bool showScope=false,string scope="Selected cells",bool forNotes=false):base("Text and color"){_original=current;Width=640;Height=850;
  _scope.SelectedItem=scope;if(showScope)Field("Apply to",_scope);
  _font=new(){ItemsSource=FontManager.Current.SystemFonts.Select(f=>f.Name).Distinct().OrderBy(f=>f).ToArray(),SelectedItem=current.Family??DesignTokens.Font.Name};_size=new(){Minimum=6,Maximum=96,Value=(decimal)(current.Size??13)};
  _bold=new(){Content="Bold",IsChecked=current.Bold==true};_italic=new(){Content="Italic",IsChecked=current.Italic};_wrap=new(){Content="Wrap text",IsChecked=current.Wrap};
  _underline=Choice(new[]{"None","Single","Double","Dotted","Dashed"},current.Underline);_strike=Choice(new[]{"None","Single","Double"},current.Strike);_vertical=Choice(new[]{"Normal","Superscript","Subscript"},current.Vertical);_caps=Choice(new[]{"Normal","All caps","Small caps"},current.Caps);_alignment=Choice(new[]{"Left","Center","Right","Justify"},current.Alignment);
  _spacing=new(){Minimum=-2,Maximum=20,Increment=.25m,Value=(decimal)current.Spacing};_indent=new(){Minimum=0,Maximum=240,Increment=4,Value=(decimal)current.Indent};_borderWidth=new(){Minimum=0,Maximum=6,Value=(decimal)current.BorderWidth};
  _foreground=new(){Text=current.Foreground??""};_highlight=new(){Text=current.Highlight??""};_border=new(){Text=current.BorderColor??""};
  Fields.Children.Add(new Border{Background=DesignTokens.Surface,CornerRadius=new CornerRadius(10),Child=_preview});Field("Font",_font);Field("Size",_size);Field("Weight and emphasis",new StackPanel{Orientation=Orientation.Horizontal,Spacing=20,Children={_bold,_italic}});if(!forNotes)Field("Cell wrapping",_wrap);Field("Underline",_underline);Field("Strikethrough",_strike);Field("Position",_vertical);Field("Capitalization",_caps);ColorField("Text color",_foreground);ColorField("Highlight / fill",_highlight);Field("Character spacing (px)",_spacing);if(!forNotes){Field("Alignment",_alignment);Field("Indent (px)",_indent);ColorField("Border color",_border);Field("Border width",_borderWidth);}
  foreach(var choice in new[]{_font,_underline,_strike,_vertical,_caps,_alignment})choice.SelectionChanged+=(_,_)=>Preview();foreach(var n in new[]{_size,_spacing,_indent,_borderWidth})n.ValueChanged+=(_,_)=>Preview();foreach(var check in new[]{_bold,_italic,_wrap})check.IsCheckedChanged+=(_,_)=>Preview();foreach(var t in new[]{_foreground,_highlight,_border})t.TextChanged+=(_,_)=>Preview();Preview();
 }
 private static ComboBox Choice(string[] options,string value)=>new(){ItemsSource=options,SelectedItem=value};
 private void ColorField(string title,TextBox box){var row=new DockPanel();var button=new Button{Content="Choose…",Margin=new Thickness(8,0,0,0)};DockPanel.SetDock(button,Dock.Right);row.Children.Add(button);row.Children.Add(box);button.Click+=async(_,_)=>{var choice=await new OfficeColorWindow(title,string.IsNullOrWhiteSpace(box.Text)?null:box.Text).ShowDialog<ColorChoice?>(this);if(choice!=null)box.Text=choice.Color??"";};Field(title,row,"Blank uses automatic text color or no fill.");}
 protected override TextFormat Read()=>(_original with{Family=_font.SelectedItem?.ToString(),Size=(double)(_size.Value??13),Bold=_bold.IsChecked==true,Italic=_italic.IsChecked==true,Wrap=_wrap.IsChecked==true,Underline=_underline.SelectedItem?.ToString()??"None",Strike=_strike.SelectedItem?.ToString()??"None",Vertical=_vertical.SelectedItem?.ToString()??"Normal",Caps=_caps.SelectedItem?.ToString()??"Normal",Alignment=_alignment.SelectedItem?.ToString()??"Left",Spacing=(double)(_spacing.Value??0),Indent=(double)(_indent.Value??0),Foreground=Empty(_foreground.Text),Highlight=Empty(_highlight.Text),BorderColor=Empty(_border.Text),BorderWidth=(double)(_borderWidth.Value??0)}).Checked();
 private static string? Empty(string? value)=>string.IsNullOrWhiteSpace(value)?null:value;
 private void Preview(){try{var f=Read().Checked();FormatVisuals.Apply(_preview,f,DesignTokens.Font,18,false);Error.Text="";}catch(Exception e){Error.Text=e.Message;}}
}

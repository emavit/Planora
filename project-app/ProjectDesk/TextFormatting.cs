using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Data;
using System.Globalization;
using System.Text.Json;
using System.Net;
namespace ProjectDesk;

public sealed record TextFormat {
 public string? Family {get;init;}
 public double? Size {get;init;}
 public bool? Bold {get;init;}
 public bool Italic {get;init;}
 public string Underline {get;init;}="None";
 public string Strike {get;init;}="None";
 public string Vertical {get;init;}="Normal";
 public string Caps {get;init;}="Normal";
 public string? Foreground {get;init;}
 public string? Highlight {get;init;}
 public string Alignment {get;init;}="Left";
 public double Spacing {get;init;}
 public double Indent {get;init;}
 public bool Wrap {get;init;}
 public string? BorderColor {get;init;}
 public double BorderWidth {get;init;}
 public TextFormat Checked(){
  if(Size.HasValue&&(!double.IsFinite(Size.Value)||Size<6||Size>96))throw new ArgumentException("Font size must be between 6 and 96.");
  if(!double.IsFinite(Spacing)||Spacing< -2||Spacing>20||!double.IsFinite(Indent)||Indent<0||Indent>240||!double.IsFinite(BorderWidth)||BorderWidth<0||BorderWidth>6)throw new ArgumentException("Text spacing, indent or border is outside its supported range.");
  foreach(var c in new[]{Foreground,Highlight,BorderColor})if(c!=null&&!Color.TryParse(c,out _))throw new ArgumentException("Enter a valid color.");
  if(!new[]{"None","Single","Double","Dotted","Dashed"}.Contains(Underline)||!new[]{"None","Single","Double"}.Contains(Strike)||!new[]{"Normal","Superscript","Subscript"}.Contains(Vertical)||!new[]{"Normal","All caps","Small caps"}.Contains(Caps)||!new[]{"Left","Center","Right","Justify"}.Contains(Alignment))throw new ArgumentException("Select a supported text style.");return this;
 }
 public static TextFormat Preset(string name)=>name switch{
  "Heading"=>new(){Size=16,Bold=true},"Emphasis"=>new(){Italic=true},"Important"=>new(){Bold=true,Foreground="#9C2B24",Highlight="#FFF0D6"},"Completed"=>new(){Strike="Single"},_=>new()};
}
public sealed record ParagraphFormat {
 public string Alignment {get;init;}="Left";
 public double Indent {get;init;}
 public double FirstLine {get;init;}
 public double LineSpacing {get;init;}=1.2;
 public double Before {get;init;}
 public double After {get;init;}=8;
 public string? Shading {get;init;}
 public string? Border {get;init;}
 public ParagraphFormat Checked(){if(!new[]{"Left","Center","Right","Justify"}.Contains(Alignment)||!double.IsFinite(Indent)||Indent<0||Indent>240||!double.IsFinite(FirstLine)||FirstLine< -120||FirstLine>240||!double.IsFinite(LineSpacing)||LineSpacing<1||LineSpacing>3||!double.IsFinite(Before)||Before<0||Before>60||!double.IsFinite(After)||After<0||After>60)throw new ArgumentException("Invalid paragraph spacing or alignment.");foreach(var c in new[]{Shading,Border})if(c!=null&&!Color.TryParse(c,out _))throw new ArgumentException("Invalid paragraph color.");return this;}
}
public sealed record RichRun(int Start,int Length,TextFormat Format);
public sealed class RichNote {
 public string Text {get;set;}="";
 public List<RichRun> Runs {get;set;}=[];
 public Dictionary<int,ParagraphFormat> Paragraphs {get;set;}=[];
 public RichNote Checked(){if(Text==null||Runs==null||Paragraphs==null)throw new ArgumentException("Invalid rich note.");foreach(var run in Runs){if(run.Start<0||run.Length<0||run.Start>Text.Length-run.Length)throw new ArgumentException("Invalid rich-text range.");run.Format.Checked();}foreach(var p in Paragraphs){if(p.Key<0||p.Key>Text.Length)throw new ArgumentException("Invalid paragraph position.");p.Value.Checked();}return this;}
 public RichNote Copy()=>JsonSerializer.Deserialize<RichNote>(JsonSerializer.Serialize(this))!;
 public void ReplaceText(string next){
  int prefix=0;while(prefix<Math.Min(Text.Length,next.Length)&&Text[prefix]==next[prefix])prefix++;
  int suffix=0;while(suffix<Math.Min(Text.Length-prefix,next.Length-prefix)&&Text[Text.Length-1-suffix]==next[next.Length-1-suffix])suffix++;
  int oldEnd=Text.Length-suffix,newEnd=next.Length-suffix,delta=newEnd-oldEnd;
  var updated=new List<RichRun>();foreach(var r in Runs){int end=r.Start+r.Length;if(end<=prefix)updated.Add(r);else if(r.Start>=oldEnd)updated.Add(r with{Start=r.Start+delta});else{if(r.Start<prefix)updated.Add(r with{Length=prefix-r.Start});if(end>oldEnd)updated.Add(r with{Start=newEnd,Length=end-oldEnd});}}
  var typing=FormatAt(Math.Max(0,prefix-1));if(newEnd>prefix)updated.Add(new(prefix,newEnd-prefix,typing));
  Paragraphs=Paragraphs.Where(p=>p.Key<=prefix||p.Key>=oldEnd).Select(p=>new KeyValuePair<int,ParagraphFormat>(p.Key>prefix&&p.Key>=oldEnd?p.Key+delta:p.Key,p.Value)).GroupBy(p=>p.Key).ToDictionary(g=>g.Key,g=>g.Last().Value);Text=next;Runs=updated;
 }
 public TextFormat FormatAt(int index)=>Runs.LastOrDefault(r=>index>=r.Start&&index<r.Start+r.Length)?.Format??new();
 public void Format(int start,int length,Func<TextFormat,TextFormat> edit){
  start=Math.Clamp(start,0,Text.Length);length=Math.Clamp(length,0,Text.Length-start);if(length==0)return;
  var chars=Enumerable.Range(0,Text.Length).Select(FormatAt).ToArray();for(int i=start;i<start+length;i++)chars[i]=edit(chars[i]).Checked();Runs=[];
  for(int i=0;i<chars.Length;){int end=i+1;while(end<chars.Length&&chars[end]==chars[i])end++;Runs.Add(new(i,end-i,chars[i]));i=end;}
 }
 public string Html(){
  var paragraphs=Text.Split('\n');var html=new System.Text.StringBuilder("<!doctype html><html><meta charset=\"utf-8\"><title>Project notes</title><body>");int offset=0;
  foreach(var p in paragraphs){var para=Paragraphs.GetValueOrDefault(offset)??new();html.Append("<p style=\"").Append($"text-align:{para.Alignment.ToLowerInvariant()};margin:{para.Before.ToString(CultureInfo.InvariantCulture)}px 0 {para.After.ToString(CultureInfo.InvariantCulture)}px {para.Indent.ToString(CultureInfo.InvariantCulture)}px;line-height:{para.LineSpacing.ToString(CultureInfo.InvariantCulture)};text-indent:{para.FirstLine.ToString(CultureInfo.InvariantCulture)}px;").Append(para.Shading!=null?"background:"+FormatVisuals.CssColor(para.Shading,"transparent")+";":"").Append(para.Border!=null?"border:1px solid "+FormatVisuals.CssColor(para.Border,"transparent")+";":"").Append("\">");
   for(int i=0;i<p.Length;){var f=FormatAt(offset+i);int end=i+1;while(end<p.Length&&FormatAt(offset+end)==f)end++;html.Append("<span style=\"").Append(WebUtility.HtmlEncode(FormatVisuals.Css(f))).Append("\">").Append(WebUtility.HtmlEncode(p[i..end])).Append("</span>");i=end;}html.Append("</p>");offset+=p.Length+1;
  }return html.Append("</body></html>").ToString();
 }
}
public sealed class ProjectAppearance {
 public int SchemaVersion {get;set;}=1;
 public string? ProjectId {get;set;}
 public Dictionary<string,TextFormat> Cells {get;set;}=[];
 public Dictionary<string,string> ChartColors {get;set;}=[];
 public Dictionary<int,string> Bars {get;set;}=[];
 public Dictionary<int,RichNote> Notes {get;set;}=[];
 public Dictionary<string,TextFormat> Styles {get;set;}=[];
 public ProjectAppearance Checked(){if(SchemaVersion!=1||Cells==null||ChartColors==null||Bars==null||Notes==null||Styles==null)throw new ArgumentException("Unsupported appearance file.");foreach(var f in Cells.Values.Concat(Styles.Values))f.Checked();foreach(var c in Bars.Values.Concat(ChartColors.Values))if(!Color.TryParse(c,out _))throw new ArgumentException("Invalid chart color.");foreach(var note in Notes.Values)note.Checked();return this;}
}
public sealed class FormattingStore {
 private readonly ViewPreferences _preferences;private readonly Action _persist;private readonly Action? _discardModelRedo;
 private string _key="";private readonly Stack<(ProjectAppearance Document,int Position)> _undo=new(),_redo=new();
 public ProjectAppearance Document {get;private set;}=new();
 private ProjectAppearance _savedDocument=new();
 public bool HasChanges=>JsonSerializer.Serialize(Document)!=JsonSerializer.Serialize(_savedDocument);
 public void DiscardChanges(){Document=Clone(_savedDocument);_undo.Clear();_redo.Clear();Save();}
 public event Action? Changed;
 public bool CanUndo=>_undo.Count>0;public bool CanRedo=>_redo.Count>0;
 public int ModelPositionAtEdit=>_undo.Count>0?_undo.Peek().Position:-1;
 public int RedoModelPosition=>_redo.Count>0?_redo.Peek().Position:-1;
 public FormattingStore(ViewPreferences preferences,Action persist,Action? discardModelRedo=null){_preferences=preferences;_persist=persist;_discardModelRedo=discardModelRedo;}
 public static string CellKey(string entity,int uid,string field)=>$"{entity}/{uid}/{field}";
 public void Bind(ProjectSession session){string key=session.Document?.Guid is Guid guid&&guid!=Guid.Empty?guid.ToString():session.Document!=null?session.BaseFingerprint:"empty";if(key==_key)return;_key=key;Document=_preferences.Appearance.TryGetValue(key,out var stored)?stored:new();try{Document.Checked();}catch(ArgumentException){Document=new();}Document.ProjectId=key;if(session.SourcePath!=null&&!_preferences.Appearance.ContainsKey(key)){try{Import(session.SourcePath);}catch(Exception){Document=new(){ProjectId=key};}}_preferences.Appearance[key]=Document;_undo.Clear();_redo.Clear();_savedDocument=Clone(Document);Changed?.Invoke();}
 public void Restore(ProjectAppearance appearance){Document=Clone(appearance.Checked());Document.ProjectId=_key;_undo.Clear();_redo.Clear();Save();}
 internal void MarkSaved(ProjectAppearance appearance)=>_savedDocument=Clone(appearance);
 public TextFormat Get(string entity,int uid,string field)=>Document.Cells.GetValueOrDefault(CellKey(entity,uid,field))??Document.Cells.GetValueOrDefault(CellKey(entity,uid,"*"))??new();
 private static ProjectAppearance Clone(ProjectAppearance d)=>JsonSerializer.Deserialize<ProjectAppearance>(JsonSerializer.Serialize(d))!;
 public void Edit(int modelPosition,Action<ProjectAppearance> edit){var next=Clone(Document);edit(next);next.Checked();_discardModelRedo?.Invoke();_undo.Push((Clone(Document),modelPosition));if(_undo.Count>128){var last=_undo.Take(128).Reverse().ToArray();_undo.Clear();foreach(var item in last)_undo.Push(item);}_redo.Clear();Document=next;Save();}
 public void Undo(){if(_undo.Count==0)return;var previous=_undo.Pop();_redo.Push((Clone(Document),previous.Position));Document=previous.Document;Save();}
 public void Redo(){if(_redo.Count==0)return;var next=_redo.Pop();_undo.Push((Clone(Document),next.Position));Document=next.Document;Save();}
 private void Save(){_preferences.Appearance[_key]=Document;_persist();Changed?.Invoke();}
 public void Export(string projectPath){var path=projectPath+".projectdesk.json";using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write)){JsonSerializer.Serialize(output,Document,new JsonSerializerOptions{WriteIndented=true});}_savedDocument=Clone(Document);}
 public void Import(string projectPath){var path=projectPath+".projectdesk.json";if(!File.Exists(path))return;var d=JsonSerializer.Deserialize<ProjectAppearance>(File.ReadAllText(path));if(d==null)return;if(d.ProjectId!=null&&d.ProjectId!=_key)throw new InvalidOperationException("The companion formatting file belongs to a different project.");d.Checked();Document=d;Save();_savedDocument=Clone(Document);}
}
public static class FormatVisuals {
 public static IBrush Brush(string? color,IBrush fallback)=>color!=null&&Color.TryParse(color,out var c)?new SolidColorBrush(c):fallback;
 public static TextDecorationCollection Decorations(TextFormat f){var result=new TextDecorationCollection();void Add(TextDecorationLocation location,double offset=0){var d=new TextDecoration{Location=location,StrokeThickness=1,StrokeThicknessUnit=TextDecorationUnit.Pixel};if(offset!=0){d.StrokeOffset=offset;d.StrokeOffsetUnit=TextDecorationUnit.Pixel;}if(f.Underline=="Dotted"&&location==TextDecorationLocation.Underline)d.StrokeDashArray=new Avalonia.Collections.AvaloniaList<double>{1,2};if(f.Underline=="Dashed"&&location==TextDecorationLocation.Underline)d.StrokeDashArray=new Avalonia.Collections.AvaloniaList<double>{4,2};result.Add(d);}if(f.Underline!="None"){Add(TextDecorationLocation.Underline);if(f.Underline=="Double")Add(TextDecorationLocation.Underline,2);}if(f.Strike!="None"){Add(TextDecorationLocation.Strikethrough);if(f.Strike=="Double")Add(TextDecorationLocation.Strikethrough,-2);}return result;}
 public static void Apply(TextBlock text,TextFormat f,FontFamily defaultFont,double defaultSize,bool summary){
  text.FontFamily=f.Family!=null?new FontFamily(f.Family):defaultFont;text.FontSize=(f.Size??defaultSize)*(f.Vertical=="Normal"?1:.75);text.FontWeight=f.Bold.HasValue?(f.Bold.Value?FontWeight.Bold:FontWeight.Normal):summary?FontWeight.SemiBold:FontWeight.Normal;text.FontStyle=f.Italic?FontStyle.Italic:FontStyle.Normal;text.TextDecorations=Decorations(f);text.Foreground=Brush(f.Foreground,DesignTokens.Text);text.Background=Brush(f.Highlight,Brushes.Transparent);text.TextAlignment=Enum.Parse<TextAlignment>(f.Alignment);text.LetterSpacing=f.Spacing;text.Padding=new Thickness(f.Indent+4,0,4,0);text.TextWrapping=f.Wrap?TextWrapping.Wrap:TextWrapping.NoWrap;if(text.Tag is string field){text.Inlines?.Clear();if(f.Caps=="Normal")text.Bind(TextBlock.TextProperty,new Binding("Cells["+field+"]"));}
  text.RenderTransform=new TranslateTransform(0,f.Vertical=="Superscript"?-4:f.Vertical=="Subscript"?4:0);
 }
 public static void ApplyEditor(TextBox editor,TextFormat f,DataGrid grid,FontWeight fallback){editor.FontFamily=f.Family!=null?new FontFamily(f.Family):grid.FontFamily;editor.FontSize=f.Size??grid.FontSize;editor.FontWeight=f.Bold.HasValue?(f.Bold.Value?FontWeight.Bold:FontWeight.Normal):fallback;editor.FontStyle=f.Italic?FontStyle.Italic:FontStyle.Normal;editor.Foreground=Brush(f.Foreground,DesignTokens.Text);editor.Background=Brush(f.Highlight,DesignTokens.Surface);editor.LetterSpacing=f.Spacing;editor.TextAlignment=Enum.Parse<TextAlignment>(f.Alignment);editor.TextWrapping=f.Wrap?TextWrapping.Wrap:TextWrapping.NoWrap;}
 public static void ApplyCaps(TextBlock text,TextFormat f,string value){if(f.Caps=="All caps")text.Text=value.ToUpperInvariant();if(f.Caps=="Small caps"){text.Text="";foreach(char c in value)text.Inlines!.Add(new Avalonia.Controls.Documents.Run{Text=char.ToUpperInvariant(c).ToString(),FontSize=text.FontSize*(char.IsLower(c)?.8:1)});}}
 public static string CssColor(string? hex,string fallback){if(hex==null)return fallback;var c=Color.Parse(hex);return $"rgba({c.R},{c.G},{c.B},{(c.A/255d).ToString(CultureInfo.InvariantCulture)})";}
 public static string Css(TextFormat f)=>$"font-family:'{(f.Family??"Arial").Replace("\\","\\\\").Replace("'","\\'")}' ;font-size:{(f.Size??13).ToString(CultureInfo.InvariantCulture)}px;font-weight:{(f.Bold==true?"bold":"normal")};font-style:{(f.Italic?"italic":"normal")};color:{CssColor(f.Foreground,"#242424")};background:{CssColor(f.Highlight,"transparent")};text-decoration:{(f.Underline!="None"?"underline ":"")}{(f.Strike!="None"?"line-through":"")};vertical-align:{(f.Vertical=="Superscript"?"super":f.Vertical=="Subscript"?"sub":"baseline")};letter-spacing:{f.Spacing.ToString(CultureInfo.InvariantCulture)}px;{(f.Caps=="All caps"?"text-transform:uppercase;":f.Caps=="Small caps"?"font-variant:small-caps;":"")}";
}
public sealed class FormattedTaskColumn : DataGridTextColumn {
 private readonly string _key;private readonly TaskSheetModule _sheet;
 public FormattedTaskColumn(string key,TaskSheetModule sheet){_key=key;_sheet=sheet;}
 protected override Control GenerateEditingElementDirect(DataGridCell cell,object dataItem){var control=base.GenerateEditingElementDirect(cell,dataItem);if(control is TextBox editor&&dataItem is TaskTableRow row){void Apply()=>FormatVisuals.ApplyEditor(editor,_sheet.Formatting?.Get("Task",row.Uid,_key)??new(),_sheet.Grid,row.Weight);editor.AttachedToVisualTree+=(_,_)=>{if(_sheet.Formatting!=null)_sheet.Formatting.Changed+=Apply;Apply();};editor.DetachedFromVisualTree+=(_,_)=>{if(_sheet.Formatting!=null)_sheet.Formatting.Changed-=Apply;};Apply();}return control;}
 protected override Control GenerateElement(DataGridCell cell,object dataItem){var control=base.GenerateElement(cell,dataItem);if(control is TextBlock text){void Apply(){if(text.DataContext is TaskTableRow row){var f=_sheet.Formatting?.Get("Task",row.Uid,_key)??new();text.Tag=_key;FormatVisuals.Apply(text,f,_sheet.Grid.FontFamily,_sheet.Grid.FontSize,row.IsSummary);FormatVisuals.ApplyCaps(text,f,row.Cells.GetValueOrDefault(_key)??"");cell.BorderBrush=FormatVisuals.Brush(f.BorderColor,DesignTokens.Separator);cell.BorderThickness=new Thickness(f.BorderColor!=null?f.BorderWidth:0);cell.Background=FormatVisuals.Brush(f.Highlight,Brushes.Transparent);}}
  text.DataContextChanged+=(_,_)=>Apply();text.AttachedToVisualTree+=(_,_)=>{if(_sheet.Formatting!=null){_sheet.Formatting.Changed-=Apply;_sheet.Formatting.Changed+=Apply;}Apply();};text.DetachedFromVisualTree+=(_,_)=>{if(_sheet.Formatting!=null)_sheet.Formatting.Changed-=Apply;};Apply();}return control;
 }
}


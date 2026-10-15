using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Globalization;
namespace ProjectDesk;

public sealed partial class PlannerWindow {
 private readonly FormattingStore _formats;
 private TextFormat? _formatPainter;
 private string _formatScope="Selected cells";
 public FormattingStore Formatting=>_formats;
 private (string Entity,string Key,int[] Uids) FormatTarget(){
  if(_currentPage<=1){string key=_formatScope=="Selected cells"?Sheet.CurrentKey??"Name":"*";var ids=_formatScope=="All visible rows"?Sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Where(r=>r.Uid>0).Select(r=>r.Uid).ToArray():SelectedTaskUids();if(ids.Length==0)throw new InvalidOperationException("Select one or more task cells first.");return("Task",key,ids);}
  if(_currentPage is 2 or 3){var grid=_currentPage==2?_resources:_assignments;string key=_formatScope!="Selected cells"?"*":grid.CurrentColumn?.Tag as string??throw new InvalidOperationException("Select a cell first.");var ids=(_formatScope=="All visible rows"?grid.ItemsSource!.OfType<SheetRecord>():grid.SelectedItems.OfType<SheetRecord>()).Select(r=>r.Uid).Where(uid=>uid>0).ToArray();if(ids.Length==0)throw new InvalidOperationException("Select one or more cells first.");return(_currentPage==2?"Resource":"Assignment",key,ids);}
  throw new InvalidOperationException("Choose the task, resource or assignment table to format cells.");
 }
 public async Task FormatText(Func<TextFormat,TextFormat> change){
  await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);var target=FormatTarget();_formats.Edit(_session.HistoryPosition,d=>{foreach(int uid in target.Uids){var key=FormattingStore.CellKey(target.Entity,uid,target.Key);var next=change(_formats.Get(target.Entity,uid,target.Key)).Checked();if(target.Key=="*")foreach(string cellKey in d.Cells.Keys.Where(k=>k.StartsWith($"{target.Entity}/{uid}/",StringComparison.Ordinal)).ToArray())d.Cells.Remove(cellKey);d.Cells[key]=next;}});_status.Text=$"Formatted {target.Uids.Length} selected cells. Appearance is saved in Planora.";
 }
 private TextFormat ActiveFormat(){var t=FormatTarget();return _formats.Get(t.Entity,t.Uids[0],t.Key);}
 private async Task EditTextFormat(){var dialog=new TextFormatWindow(ActiveFormat(),true,_formatScope);var format=await dialog.ShowDialog<TextFormat?>(this);if(format!=null){_formatScope=dialog.Scope;await FormatText(_=>format);}}
 private async Task PickTextColor(bool highlight){var current=ActiveFormat();var choice=await new OfficeColorWindow(highlight?"Highlight / fill":"Font color",highlight?current.Highlight:current.Foreground,_preferences.RecentColors).ShowDialog<ColorChoice?>(this);if(choice==null)return;if(choice.Color!=null){_preferences.RecentColors.Remove(choice.Color);_preferences.RecentColors.Insert(0,choice.Color);_preferences.RecentColors=_preferences.RecentColors.Take(16).ToList();}await FormatText(f=>highlight?f with{Highlight=choice.Color}:f with{Foreground=choice.Color});}
 private Task CopyFormat(){_formatPainter=ActiveFormat();_status.Text="Format copied. Select destination cells and choose Apply Painter.";return Task.CompletedTask;}
 private Task PasteFormat()=>_formatPainter==null?throw new InvalidOperationException("Use Format Painter to copy a cell's appearance first."):FormatText(_=>_formatPainter);
 private Task ClearFormat(){var t=FormatTarget();_formats.Edit(_session.HistoryPosition,d=>{foreach(int uid in t.Uids){if(t.Key=="*"){foreach(string key in d.Cells.Keys.Where(k=>k.StartsWith($"{t.Entity}/{uid}/",StringComparison.Ordinal)).ToArray())d.Cells.Remove(key);}else d.Cells[FormattingStore.CellKey(t.Entity,uid,t.Key)]=new();}});return Task.CompletedTask;}
 private Task StylePreset(string name)=>FormatText(_=>_formats.Document.Styles.GetValueOrDefault(name)??TextFormat.Preset(name));
 private async Task SaveTextStyle(){var style=ActiveFormat();var form=new StyleNameWindow();string? name=await form.ShowDialog<string?>(this);if(name!=null){_formats.Edit(_session.HistoryPosition,d=>d.Styles[name]=style);_status.Text="Style saved: "+name;}}
 private async Task ApplySavedStyle(){if(_formats.Document.Styles.Count==0)throw new InvalidOperationException("Save a text style first.");var choice=await new ChooseGuiItemWindow("Apply saved style",_formats.Document.Styles.Keys).ShowDialog<string?>(this);if(choice!=null)await StylePreset(choice);}
 private async Task PickBarColor(){int[] ids=SelectedTaskUids();if(ids.Length==0)throw new InvalidOperationException("Select task rows first.");var choice=await new OfficeColorWindow("Gantt bar color",_formats.Document.Bars.GetValueOrDefault(ids[0]),_preferences.RecentColors).ShowDialog<ColorChoice?>(this);if(choice!=null)_formats.Edit(_session.HistoryPosition,d=>{foreach(int uid in ids){if(choice.Color==null)d.Bars.Remove(uid);else d.Bars[uid]=choice.Color;}});}
 private async Task EditChartColors(){var colors=await new ChartColorsWindow(_formats.Document.ChartColors).ShowDialog<Dictionary<string,string>?>(this);if(colors!=null)_formats.Edit(_session.HistoryPosition,d=>d.ChartColors=colors);}
 private Task ChangeCase(string kind){var target=FormatTarget();if(target.Entity!="Task"||TaskFieldEditing.Property(target.Key)?.PropertyType!=typeof(string))throw new InvalidOperationException("Choose an editable text field, such as Task Name, Notes or Contact.");string Convert(string text)=>kind switch{"upper"=>text.ToUpperInvariant(),"lower"=>text.ToLowerInvariant(),"title"=>CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()),"sentence"=>text.Length>0?char.ToUpperInvariant(text[0])+text[1..].ToLowerInvariant():text,"toggle"=>new string(text.Select(c=>char.IsUpper(c)?char.ToLowerInvariant(c):char.ToUpperInvariant(c)).ToArray()),_=>text};_session.EditCells(target.Uids.Select(uid=>new TextCellChange(uid,target.Key,Convert(_session.EditText(uid,target.Key)))),"Change Case");Refresh();return Task.CompletedTask;}
 private async Task EditRichNotes(){int uid=RequireTask();var existing=_formats.Document.Notes.GetValueOrDefault(uid)??new RichNote{Text=_session.Document!.Tasks.GetByUid(uid).Notes??""};var edit=await new RichNotesWindow("Notes · "+_session.Document!.Tasks.GetByUid(uid).Name,existing).ShowDialog<RichNote?>(this);if(edit!=null){_formats.Edit(_session.HistoryPosition,d=>d.Notes[uid]=edit);_status.Text="Formatted notes saved in Planora. Existing native MPP notes remain preserved.";}}
 internal void UndoEdit(){if(_formats.CanUndo&&_formats.ModelPositionAtEdit==_session.HistoryPosition){_formats.Undo();}else{_session.Undo();}Refresh();}
 internal void RedoEdit(){if(_formats.CanRedo&&_formats.RedoModelPosition==_session.HistoryPosition)_formats.Redo();else _session.Redo();Refresh();}
}
public sealed class StyleNameWindow : EditorForm<string> {
 private readonly TextBox _name=new();
 public StyleNameWindow():base("Save text style"){Height=290;Field("Name",_name,"Named styles belong to this Planora project.");}
 protected override string Read()=>string.IsNullOrWhiteSpace(_name.Text)?throw new ArgumentException("Enter a style name."):_name.Text.Trim();
}
public sealed class FormattedSheetColumn : DataGridTextColumn {
 protected override Control GenerateEditingElementDirect(DataGridCell cell,object item){var result=base.GenerateEditingElementDirect(cell,item);if(result is TextBox editor&&item is SheetRecord row){void Apply()=>FormatVisuals.ApplyEditor(editor,_store.Get(_entity,row.Uid,_key),_grid,FontWeight.Normal);editor.AttachedToVisualTree+=(_,_)=>{_store.Changed+=Apply;Apply();};editor.DetachedFromVisualTree+=(_,_)=>_store.Changed-=Apply;Apply();}return result;}
 private readonly string _key,_entity;private readonly DataGrid _grid;private readonly FormattingStore _store;
 public FormattedSheetColumn(string key,string entity,DataGrid grid,FormattingStore store){_key=key;_entity=entity;_grid=grid;_store=store;}
 protected override Control GenerateElement(DataGridCell cell,object item){var result=base.GenerateElement(cell,item);if(result is TextBlock text){void Apply(){if(text.DataContext is SheetRecord row){var f=_store.Get(_entity,row.Uid,_key);text.Tag=_key;FormatVisuals.Apply(text,f,_grid.FontFamily,_grid.FontSize,false);FormatVisuals.ApplyCaps(text,f,row.Cells.GetValueOrDefault(_key)??"");cell.BorderBrush=FormatVisuals.Brush(f.BorderColor,DesignTokens.Separator);cell.BorderThickness=new Thickness(f.BorderColor!=null?f.BorderWidth:0);cell.Background=FormatVisuals.Brush(f.Highlight,Brushes.Transparent);}}text.DataContextChanged+=(_,_)=>Apply();text.AttachedToVisualTree+=(_,_)=>{_store.Changed-=Apply;_store.Changed+=Apply;Apply();};text.DetachedFromVisualTree+=(_,_)=>_store.Changed-=Apply;Apply();}return result;}
}

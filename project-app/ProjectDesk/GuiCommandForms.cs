using Avalonia.Controls;

namespace ProjectDesk;

public sealed record FindReplaceEdit(string Find,string Replacement,bool MatchCase);
public sealed class FindReplaceWindow : EditorForm<FindReplaceEdit> {
    private readonly TextBox _find=new(),_replace=new();private readonly CheckBox _case=new(){Content="Match case"};
    public FindReplaceWindow():base("Replace task names"){Height=400;Field("Find",_find);Field("Replace with",_replace);Field("Search options",_case,"Replaces matching text in task names across the project. Undo restores the whole operation.");}
    protected override FindReplaceEdit Read()=>new(string.IsNullOrEmpty(_find.Text)?throw new ArgumentException("Enter text to find."):_find.Text,_replace.Text??"",_case.IsChecked==true);
}
public sealed class GoToTaskWindow : EditorForm<int> {
    private readonly TextBox _id=new();
    public GoToTaskWindow():base("Go To task"){Height=290;Field("Task ID",_id,"Enter the ID shown in the task table.");}
    protected override int Read()=>int.TryParse(_id.Text,out int value)&&value>0?value:throw new ArgumentException("Enter a positive task ID.");
}
public sealed record GuiFormatEdit(double FontSize,double RowHeight,bool Gridlines,bool Critical,bool Baseline,int BaselineNumber);
public sealed class ChooseGuiItemWindow : EditorForm<string> {
    private readonly ComboBox _choice;
    public ChooseGuiItemWindow(string title,IEnumerable<string> choices):base(title){Height=300;_choice=new(){ItemsSource=choices.ToArray(),SelectedIndex=0};Field(title,_choice);}
    protected override string Read()=>_choice.SelectedItem?.ToString()??throw new ArgumentException("Select an item.");
}
public sealed class ZoomEditorWindow : EditorForm<double> {
    private readonly NumericUpDown _zoom;
    public ZoomEditorWindow(double current):base("Timescale / Zoom"){Height=290;_zoom=new(){Minimum=100,Maximum=6400,Value=(decimal)(current*100),Increment=50};Field("Date scale (%)",_zoom,"100% fits the whole project. Higher values show dates in more detail.");}
    protected override double Read()=>(double)(_zoom.Value??100)/100;
}
public sealed record GuiSortEdit(string Field,bool Descending);
public sealed class SortEditorWindow : EditorForm<GuiSortEdit> {
    private readonly ComboBox _field=new(){ItemsSource=new[]{"Project order","ID","Name","Start","Finish","Cost","% Complete"},SelectedIndex=0};private readonly CheckBox _descending=new(){Content="Descending"};
    public SortEditorWindow():base("Sort By"){Height=340;Field("Field",_field);Field("Order",_descending,"Sorts displayed siblings while preserving the outline. Saved task IDs, task order and links are unchanged.");}
    protected override GuiSortEdit Read()=>new(_field.SelectedItem?.ToString()??"Project order",_descending.IsChecked==true);
}
public sealed class GuiFormatWindow : EditorForm<GuiFormatEdit> {
    private readonly NumericUpDown _font,_rows;private readonly CheckBox _grid,_critical,_baseline;private readonly ComboBox _number;
    public GuiFormatWindow(GuiFormatEdit current):base("Gantt Chart Format"){
        Height=570;_font=new(){Minimum=9,Maximum=24,Value=(decimal)current.FontSize};_rows=new(){Minimum=22,Maximum=60,Value=(decimal)current.RowHeight};_grid=new(){Content="Show table gridlines",IsChecked=current.Gridlines};_critical=new(){Content="Highlight critical tasks",IsChecked=current.Critical};_baseline=new(){Content="Show baseline bars",IsChecked=current.Baseline};_number=new(){ItemsSource=Enumerable.Range(0,11).ToArray(),SelectedItem=current.BaselineNumber};
        Field("Table font size",_font);Field("Row height",_rows);Field("Gridlines",_grid);Field("Gantt bars",_critical);Field("Baselines",_baseline);Field("Baseline number",_number,"Select the captured baseline to compare with current dates.");
    }
    protected override GuiFormatEdit Read()=>new((double)(_font.Value??12),(double)(_rows.Value??26),_grid.IsChecked==true,_critical.IsChecked==true,_baseline.IsChecked==true,(int)(_number.SelectedItem??0));
}

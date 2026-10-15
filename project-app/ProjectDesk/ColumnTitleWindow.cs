using Avalonia.Controls;

namespace ProjectDesk;

/// <summary>A view-only header caption, separate from a Project field's identity.</summary>
public sealed class ColumnTitleWindow : EditorForm<string> {
    private readonly TextBox _title;
    public ColumnTitleWindow(string field,string title):base("Column settings"){
        Height=330;_title=new TextBox{Text=title};
        Field("Field",new TextBlock{Text=field});
        Field("Display title",_title,"Changes the caption for this window only. The project field and file stay unchanged. Clear to restore the field name.");
        _field=field;
    }
    private readonly string _field;
    protected override string Read()=>string.IsNullOrWhiteSpace(_title.Text)?_field:_title.Text.Trim();
}

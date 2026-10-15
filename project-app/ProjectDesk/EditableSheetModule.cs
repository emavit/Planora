using Avalonia.Controls;
using Avalonia.Threading;
using System.ComponentModel;

namespace ProjectDesk;

public sealed class SheetRecord : INotifyPropertyChanged {
    public int Uid {get;}
    public Dictionary<string,string?> Cells {get;private set;}
    public event PropertyChangedEventHandler? PropertyChanged;
    public SheetRecord(int uid,Dictionary<string,string?> cells){Uid=uid;Cells=cells;}
    public void Update(Dictionary<string,string?> cells){Cells=cells;PropertyChanged?.Invoke(this,new(nameof(Cells)));}
}

// Shared edit lifecycle for resource and assignment sheets. ProjectSession owns
// parsing, atomic validation and undo; displayed strings never become the model.
public static class EditableSheetModule {
    public static void Attach(DataGrid grid,Func<int,string,string> read,Action<int,string,string> apply,Action refresh,Action<string> error) {
        grid.IsReadOnly=false;grid.GridLinesVisibility=DataGridGridLinesVisibility.All;grid.RowHeight=26;
        grid.PreparingCellForEdit+=(_,e)=>{if(e.Row.DataContext is SheetRecord row&&e.EditingElement is TextBox text){text.Text=read(row.Uid,e.Column.Tag?.ToString()??"");text.SelectAll();}};
        grid.CellEditEnding+=(_,e)=>{if(e.EditAction!=DataGridEditAction.Commit||e.Row.DataContext is not SheetRecord row||e.EditingElement is not TextBox text)return;try{apply(row.Uid,e.Column.Tag?.ToString()??"",text.Text??"");}catch(Exception ex){e.Cancel=true;error(ex.Message);return;}Dispatcher.UIThread.Post(refresh);};
    }
}

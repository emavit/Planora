using Avalonia.Controls;
namespace ProjectDesk;
public sealed class DateRangeWindow : EditorForm<DateAxisView> {
 private readonly CalendarDatePicker _start,_finish;private readonly CheckBox _automatic;private readonly NumericUpDown _zoom;
 public DateRangeWindow(string title,DateAxis axis):base(title){Height=505;
  _automatic=new(){Name="AutomaticRange",Content="Use entire project date range",IsChecked=!axis.HasCustomRange};_start=new(){Name="StartDate",SelectedDate=axis.Start.Date,SelectedDateFormat=CalendarDatePickerFormat.Short};_finish=new(){Name="FinishDate",SelectedDate=axis.LastDate,SelectedDateFormat=CalendarDatePickerFormat.Short};_zoom=new(){Name="DateZoom",Minimum=100,Maximum=6400,Increment=50,Value=(decimal)(axis.Zoom*100)};
  Field("Date limits",_automatic,"These limits change the view, not task dates.");Field("Start",_start);Field("Finish (inclusive)",_finish);Field("Zoom (%)",_zoom,"100% fits the selected date range; higher values add detail.");void Update(){_start.IsEnabled=_finish.IsEnabled=_automatic.IsChecked!=true;}_automatic.IsCheckedChanged+=(_,_)=>Update();Update();
 }
 protected override DateAxisView Read(){var view=new DateAxisView(_automatic.IsChecked==true?null:_start.SelectedDate??throw new ArgumentException("Choose a start date."),_automatic.IsChecked==true?null:_finish.SelectedDate??throw new ArgumentException("Choose a finish date."),(double)(_zoom.Value??100)/100);var check=new DateAxis();check.Restore(view);return view;}
}
public sealed class TimelineNameWindow : EditorForm<string> {
 private readonly TextBox _name;
 public TimelineNameWindow(string current):base("Timeline name"){Height=285;_name=new(){Text=current};Field("Name",_name);}
 protected override string Read()=>string.IsNullOrWhiteSpace(_name.Text)?throw new ArgumentException("Enter a timeline name."):_name.Text.Trim();
}

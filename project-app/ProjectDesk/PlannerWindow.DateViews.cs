namespace ProjectDesk;
public sealed partial class PlannerWindow {
 private string? _axisProjectKey;private bool _restoringAxis;
 private void UpdateGanttAxis(){string key=_session.SourcePath??_session.Document?.Guid?.ToString()??"empty";_restoringAxis=true;try{if(key!=_axisProjectKey){_axisProjectKey=key;_axis.SetProject(_session.Tasks);try{_axis.Restore(_preferences.GanttAxes.GetValueOrDefault(key)??new());}catch(ArgumentException){_axis.EntireProject();}}else _axis.SetProject(_session.Tasks);}finally{_restoringAxis=false;}}
 private void SaveGanttAxis(){if(_restoringAxis||_axisProjectKey==null)return;var state=_axis.Capture();if(_preferences.GanttAxes.GetValueOrDefault(_axisProjectKey)==state)return;_preferences.GanttAxes[_axisProjectKey]=state;Persist();}
}

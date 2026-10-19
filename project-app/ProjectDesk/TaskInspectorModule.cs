using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ProjectDesk;

// Drafts stay bound to their original task until applied or cancelled.
public sealed class TaskInspectorModule : UserControl {
    private readonly ProjectSession _session;
    private readonly Dictionary<string,TextBox> _fields=[];
    private readonly Dictionary<string,string> _original=[];
    private readonly Dictionary<string,TextBlock> _errors=[];
    private readonly StackPanel _actions=new(){Orientation=Orientation.Horizontal,Spacing=8};
    private readonly TextBlock _heading=new(){FontSize=16,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock _message=new(){TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary};
    private readonly TextBlock _dates=new(){TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary};
    private readonly StackPanel _form=new(){Spacing=8};
    public int? TaskUid {get;private set;}
    public bool HasDraft=>_fields.Any(f=>f.Value.Text!=_original.GetValueOrDefault(f.Key));
    public event Action? Applied;
    public TaskInspectorModule(ProjectSession session){
        _session=session;
        var body=new DockPanel{Margin=new Thickness(14)};
        var header=new StackPanel{Spacing=8,Margin=new Thickness(0,0,0,10)};
        header.Children.Add(_heading);header.Children.Add(new Border{Background=DesignTokens.Background,CornerRadius=new CornerRadius(8),Padding=new Thickness(10),Child=_dates});
        DockPanel.SetDock(header,Dock.Top);body.Children.Add(header);
        StackPanel section=_form;
        foreach(var (key,label) in new[]{("Name","Task name"),("Duration","Duration"),("TaskMode","Scheduling mode"),("Predecessors","Predecessors"),("PercentComplete","Complete (%)")}){
            if(key is "Name" or "Predecessors" or "PercentComplete"){
                section=new StackPanel{Spacing=7};var title=key=="Name"?"Schedule":key=="Predecessors"?"Dependencies":"Progress";
                _form.Children.Add(new TextBlock{Text=title,FontWeight=FontWeight.SemiBold,Margin=new Thickness(0,8,0,0)});
                _form.Children.Add(new Border{Background=DesignTokens.Background,CornerRadius=new CornerRadius(8),Padding=new Thickness(10),Child=section});
            }
            var editor=new TextBox{Name="Inspector"+key,Text="",Watermark=key=="TaskMode"?"Auto Scheduled / Manually Scheduled":label};
            AutomationProperties.SetName(editor,label);
            var fieldError=new TextBlock{Name="InspectorError"+key,Foreground=ChartDrawing.Critical,FontSize=12,TextWrapping=TextWrapping.Wrap,IsVisible=false};_errors[key]=fieldError;
            editor.TextChanged+=(_,_)=>{if(!Equals(fieldError.Tag,editor.Text))fieldError.IsVisible=false;if(HasDraft&&!_errors.Values.Any(e=>e.IsVisible))_message.Text="Unsaved task edits. Apply or Cancel.";};
            _original[key]="";_fields.Add(key,editor);section.Children.Add(new TextBlock{Text=label,FontSize=12,Foreground=DesignTokens.Secondary});section.Children.Add(editor);
            if(key=="TaskMode"){
                editor.IsVisible=false;
                var mode=new ComboBox{Name="InspectorModeSelector",ItemsSource=new[]{"Auto Scheduled","Manually Scheduled"},HorizontalAlignment=HorizontalAlignment.Stretch};
                AutomationProperties.SetName(mode,"Scheduling mode");
                mode.SelectionChanged+=(_,_)=>{if(mode.SelectedItem is string value)editor.Text=value;};
                editor.TextChanged+=(_,_)=>mode.SelectedItem=editor.Text;
                section.Children.Add(mode);
            }
            section.Children.Add(fieldError);
        }
        var footer=new StackPanel{Spacing=8,Margin=new Thickness(0,10,0,0)};
        footer.Children.Add(_message);footer.Children.Add(_actions);
        DockPanel.SetDock(footer,Dock.Bottom);body.Children.Add(footer);
        var apply=new Button{Content="Apply",Name="InspectorApply"};apply.Classes.Add("primary");apply.Click+=(_,_)=>TryCommit();
        var cancel=new Button{Content="Cancel edits",Name="InspectorCancel"};cancel.Click+=(_,_)=>{ResetDraft();Applied?.Invoke();};
        _actions.Children.Add(apply);_actions.Children.Add(cancel);
        body.Children.Add(new ScrollViewer{Name="InspectorFieldsScroll",Content=_form,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled});
        Content=body;SetTask(null);
    }
    public void SetTask(int? uid){
        if(HasDraft)return;
        var task=_session.Tasks.FirstOrDefault(t=>t.Uid==uid);TaskUid=task?.Uid;
        _form.IsEnabled=task!=null;_actions.IsEnabled=task!=null;foreach(var error in _errors.Values)error.IsVisible=false;_heading.Text=task==null?"Task inspector":$"Task {task.DisplayId ?? task.Uid}";
        _message.Text=task==null?"Select a task in Table or Gantt.":"Apply saves these fields together. Undo restores them.";
        foreach(var (key,editor) in _fields){var value=task==null?"":_session.EditText(task.Uid,key)??"";_original[key]=value;editor.Text=value;}
        _dates.Text=task==null?"":$"Start: {TaskColumns.Format(task.Start) ?? "—"}\nFinish: {TaskColumns.Format(task.Finish) ?? "—"}";
    }
    private void ResetDraft(){foreach(var (key,editor) in _fields)editor.Text=_original.GetValueOrDefault(key)??"";SetTask(TaskUid);}
    public bool TryCommit(){
        if(!HasDraft)return true;
        bool invalid=false;
        foreach(var (key,editor) in _fields){
            _errors[key].IsVisible=false;
            if(editor.Text==_original.GetValueOrDefault(key)||TaskFieldEditing.Property(key)==null)continue;
            try{TaskFieldEditing.Parse(TaskFieldEditing.PropertyKey(key),editor.Text??"");}
            catch(Exception error){_errors[key].Tag=editor.Text;_errors[key].Text=error.Message;_errors[key].IsVisible=true;invalid=true;}
        }
        if(invalid){_message.Text="Correct the marked fields. No changes were applied.";return false;}
        try{
            if(TaskUid is not int uid||!_session.Tasks.Any(t=>t.Uid==uid))throw new InvalidOperationException("The edited task is no longer available. Cancel these edits.");
            _session.EditCells(_fields.Where(f=>f.Value.Text!=_original.GetValueOrDefault(f.Key)).Select(f=>new TextCellChange(uid,f.Key,f.Value.Text??"")),"Edit task inspector");
            foreach(var (key,editor) in _fields)_original[key]=editor.Text??"";
            SetTask(uid);Applied?.Invoke();return true;
        }catch(Exception error){_message.Text="Could not apply: "+error.Message;return false;}
    }
}

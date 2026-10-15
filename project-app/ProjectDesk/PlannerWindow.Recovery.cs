using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Text.Json;
namespace ProjectDesk;

public sealed partial class PlannerWindow {
 private RecoveryStore? _recovery;
 private readonly Dictionary<Guid,Guid> _recoverySlots=[];
 private readonly CancellationTokenSource _recoveryStop=new();
 private Task? _recoveryLoop;
 private Task _checkpoint=Task.CompletedTask;
 private bool _recoveryClosed;
 private long _recoveryRevision;
 private string? _checkpointSignature;
 private Guid? _portableProject;
 private string? _savedPresentation;
 private bool HasPortableViewChanges=>_session.Document!=null&&_portableProject==_session.ProjectIdentity&&_savedPresentation!=JsonSerializer.Serialize(CurrentPresentation(),DurableCommand.Json);
 private ProjectPresentation CurrentPresentation()=>new(_formats.Document,new TimelineLayout{Lanes=_timeline.Lanes.ToList(),ActiveId=_timeline.ActiveId},_axis.Capture(),_preferences.ShowTimeline);
 private readonly TextBlock _recoveryStatus=new(){Name="RecoveryStatus",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary,FontSize=11,Margin=new Thickness(14,2)};
 private void InitializeRecovery(bool automatic){
  try{_recovery=new RecoveryStore(Path.Combine(Path.GetDirectoryName(_settingsPath)!,"recovery"));}
  catch(Exception e){_recoveryStatus.Text="Recovery unavailable: "+e.Message;return;}
  _session.StateChanged+=RecoveryChanged;
  Opened+=(_,_)=>{if(automatic){StartRecoveryLoop(TimeSpan.FromSeconds(15));int count=_recovery!.Sessions().Count;if(count>0)_recoveryStatus.Text=$"{count} retained recovery session(s). Open File → Recovery to review.";else _recoveryStatus.Text="Recovery checkpoints every 15 seconds while working.";}};
  Closed+=(_,_)=>{_recoveryClosed=true;_recoveryStop.Cancel();_session.StateChanged-=RecoveryChanged;};
 }
 private void RecoveryChanged()=>Interlocked.Increment(ref _recoveryRevision);
 internal void StartRecoveryForValidation()=>StartRecoveryLoop(TimeSpan.FromMilliseconds(100));
 private void StartRecoveryLoop(TimeSpan interval){_recoveryLoop??=RecoveryLoop(interval);}
 private async Task RecoveryLoop(TimeSpan interval){
  try{while(true){await Task.Delay(interval,_recoveryStop.Token).ConfigureAwait(false);await Dispatcher.UIThread.InvokeAsync(()=>_busy||_recoveryClosed?Task.CompletedTask:WriteCheckpoint(false));}}
  catch(OperationCanceledException)when(_recoveryStop.IsCancellationRequested){}
  catch(Exception e){Dispatcher.UIThread.Post(()=>{if(!_recoveryClosed)_recoveryStatus.Text="Automatic recovery stopped: "+e.Message;});}
 }
 internal DurableProjectState CaptureRecoveryState()=>_session.CaptureState() with{Presentation=CurrentPresentation().Copy()};
 internal void RestoreRecoveryState(DurableProjectState state){
  state.Presentation?.Checked();_session.RestoreState(state);Refresh();
  if(state.Presentation is {} p){_formats.Restore(p.Appearance);_timeline.RestoreLayout(p.Timelines);_axis.Restore(p.Gantt);SetTimelineVisible(p.ShowTimeline);}
  _checkpointSignature=null;_status.Text="Recovered project. Review it, then save a new project copy.";
 }
 private Task CheckpointNow()=>WriteCheckpoint(true);
 internal async Task SavePlanoraCopy(string path){
  var snapshot=CaptureRecoveryState();snapshot=snapshot with{LastSavedPath=Path.GetFullPath(path),SavedPosition=snapshot.Position,SavedFingerprint=string.Join("|",snapshot.History.Take(snapshot.Position).Select(e=>e.Id))};
  await Task.Run(()=>PlanoraPackage.WriteNew(path,snapshot));_session.AcceptSavedState(snapshot);_formats.MarkSaved(snapshot.Presentation!.Appearance);_portableProject=snapshot.ProjectId;_savedPresentation=JsonSerializer.Serialize(snapshot.Presentation,DurableCommand.Json);_status.Text="Saved Planora project with formatting and timelines: "+path;
 }
 internal void OpenPlanoraCopy(string path){var state=PlanoraPackage.Read(path);RestoreRecoveryState(state);_session.AcceptSavedState(state with{LastSavedPath=Path.GetFullPath(path),SavedPosition=state.Position,SavedFingerprint=string.Join("|",state.History.Take(state.Position).Select(e=>e.Id))});if(state.Presentation!=null)_formats.MarkSaved(_formats.Document);_portableProject=state.ProjectId;_savedPresentation=JsonSerializer.Serialize(CurrentPresentation(),DurableCommand.Json);_status.Text="Opened Planora project: "+Path.GetFileName(path);}
 private async Task WriteCheckpoint(bool force){
  if(_recoveryClosed||_session.Document==null)return;
  if(_recovery==null){_recoveryStatus.Text="Recovery is unavailable. Save a project copy.";return;}
  if(!_checkpoint.IsCompleted){if(!force)return;await _checkpoint;}
  try{
   var presentation=new ProjectPresentation(_formats.Document,new TimelineLayout{Lanes=_timeline.Lanes.ToList(),ActiveId=_timeline.ActiveId},_axis.Capture(),_preferences.ShowTimeline);
   string signature=_session.ProjectIdentity+":"+Interlocked.Read(ref _recoveryRevision)+":"+JsonSerializer.Serialize(presentation,DurableCommand.Json);
   if(!force&&signature==_checkpointSignature)return;
   var snapshot=_session.CaptureState() with{Presentation=presentation.Copy()};
   if(!_recoverySlots.TryGetValue(snapshot.ProjectId,out var slot)){slot=Guid.NewGuid();_recoverySlots.Add(snapshot.ProjectId,slot);}
   _checkpoint=Task.Run(()=>_recovery.Checkpoint(slot,snapshot));await _checkpoint;
   _checkpointSignature=signature;
   if(!_recoveryClosed)_recoveryStatus.Text="Recovery checkpoint saved at "+DateTime.Now.ToString("HH:mm:ss")+". Save a copy for your project file.";
  }catch(Exception e){if(!_recoveryClosed)_recoveryStatus.Text="Recovery failed — save a project copy: "+e.Message;}
 }
 private async Task DiscardCurrentRecovery(){
  if(_recovery==null||!_recoverySlots.TryGetValue(_session.ProjectIdentity,out var slot))return;
  try{try{await _checkpoint;}catch{} _recovery.Discard(slot);_recoverySlots.Remove(_session.ProjectIdentity);_checkpointSignature=null;}catch(Exception e){_recoveryStatus.Text="Could not remove retained recovery: "+e.Message;}
 }
 private sealed record RecoveryChoice(Guid Id,string Label){public override string ToString()=>Label;}
 private async Task ShowRecovery(){
  if(_recovery==null)throw new InvalidOperationException("Recovery storage is unavailable.");
  var choices=_recovery.Sessions().Select(id=>new RecoveryChoice(id,_recovery.Modified(id).ToString("yyyy-MM-dd HH:mm:ss")+" · "+id.ToString("N")[..8])).Reverse().ToArray();
  var dialog=new Window{Title="Project Recovery",Width=700,Height=460,WindowStartupLocation=WindowStartupLocation.CenterOwner};
  var root=new StackPanel{Margin=new Thickness(20),Spacing=14};root.Children.Add(new TextBlock{Text="Recover a retained checkpoint",FontSize=22,FontWeight=FontWeight.SemiBold});
  root.Children.Add(new TextBlock{Text="Checkpoints include the project, Undo/Redo, formatting and timelines. They are retained locally until you delete them. The latest few seconds before an interruption may not be included.",TextWrapping=TextWrapping.Wrap});
  var list=new ComboBox{Name="RecoverySessions",ItemsSource=choices,SelectedIndex=choices.Length>0?0:-1,HorizontalAlignment=HorizontalAlignment.Stretch};root.Children.Add(list);
  var info=new TextBlock{Text=choices.Length==0?"No retained checkpoints.":"Choose a checkpoint to inspect.",TextWrapping=TextWrapping.Wrap};root.Children.Add(info);
  var previous=new CheckBox{Content="Use previous generation"};root.Children.Add(previous);
  var buttons=new WrapPanel();var recover=new Button{Content="Recover",IsEnabled=choices.Length>0};var delete=new Button{Content="Delete Checkpoint",Margin=new Thickness(8,0)};var cancel=new Button{Content="Cancel"};buttons.Children.Add(recover);buttons.Children.Add(delete);buttons.Children.Add(cancel);root.Children.Add(buttons);dialog.Content=root;
  void Inspect(){try{if(list.SelectedItem is RecoveryChoice item){var state=_recovery.Read(item.Id,previous.IsChecked==true);info.Text=$"Source: {Path.GetFileName(state.SourcePath)??"New project"}\n{state.History.Length} edits · {state.ProjectId}";}}catch(Exception e){info.Text="Cannot read this generation: "+e.Message;}}
  list.SelectionChanged+=(_,_)=>Inspect();previous.IsCheckedChanged+=(_,_)=>Inspect();Inspect();
  cancel.Click+=(_,_)=>dialog.Close(null);recover.Click+=(_,_)=>{if(list.SelectedItem is RecoveryChoice item)dialog.Close((item.Id,previous.IsChecked==true));};
  delete.Click+=async(_,_)=>{if(list.SelectedItem is not RecoveryChoice item)return;try{await _checkpoint;_recovery.Discard(item.Id);_checkpointSignature=null;list.ItemsSource=_recovery.Sessions().Select(id=>new RecoveryChoice(id,_recovery.Modified(id).ToString("yyyy-MM-dd HH:mm:ss")+" · "+id.ToString("N")[..8])).ToArray();list.SelectedIndex=-1;recover.IsEnabled=false;info.Text="Checkpoint deleted. Choose another session to recover.";}catch(Exception e){info.Text=e.Message;}};
  list.SelectionChanged+=(_,_)=>recover.IsEnabled=list.SelectedItem is RecoveryChoice;
  var selected=await dialog.ShowDialog<(Guid Id,bool Previous)?>(this);if(selected==null)return;
  var state=await Task.Run(()=>_recovery.Read(selected.Value.Id,selected.Value.Previous));state.Presentation?.Checked();
  if(!await ResolveUnsavedChanges())return;RestoreRecoveryState(state);
 }
}

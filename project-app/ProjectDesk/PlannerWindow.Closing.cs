namespace ProjectDesk;
public sealed partial class PlannerWindow {
 internal SaveChangesWindow? ActiveSavePrompt {get;private set;}
 internal Func<Task<bool>>? SaveForValidation {get;set;}
 private bool HasUnsavedChanges=>_session.HasEdits||_formats.HasChanges||HasPortableViewChanges;
 private async Task<bool> ResolveUnsavedChanges(){
  if(!HasUnsavedChanges)return true;
  try{
   ActiveSavePrompt=new SaveChangesWindow(_session.Document?.Name??"Untitled Project");var choice=await ActiveSavePrompt.ShowDialog<SaveChangesChoice>(this);ActiveSavePrompt=null;
   if(choice==SaveChangesChoice.Cancel)return false;
   if(choice==SaveChangesChoice.Save)return await (SaveForValidation?.Invoke()??SaveCopy());
   await DiscardCurrentRecovery();_formats.DiscardChanges();return true;
  }catch(Exception error){_status.Text="Could not save changes: "+error.Message;return false;}finally{ActiveSavePrompt=null;}
 }
 private async Task HandleWindowClosing(Avalonia.Controls.WindowClosingEventArgs e){
  if(_closeApproved)return;
  if(_busy){e.Cancel=true;return;}
  if(!Sheet.Grid.CommitEdit()||!_resources.CommitEdit()||!_assignments.CommitEdit()){e.Cancel=true;_status.Text="Correct or cancel the current cell before closing.";return;}
  Sheet.Persist();if(!HasUnsavedChanges)return;e.Cancel=true;_busy=true;
  try{if(await ResolveUnsavedChanges()){_closeApproved=true;Close();}}finally{_busy=false;}
 }
}

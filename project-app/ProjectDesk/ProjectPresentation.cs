using System.Text.Json;
namespace ProjectDesk;

public sealed record ProjectPresentation(ProjectAppearance Appearance,TimelineLayout Timelines,DateAxisView Gantt,bool ShowTimeline){
 public ProjectPresentation Checked(){
  if(Appearance==null||Timelines?.Lanes==null||Timelines.Lanes.Count is <1 or >100||Gantt==null)throw new InvalidDataException("Invalid project presentation.");
  Appearance.Checked();new DateAxis().Restore(Gantt);
  var ids=new HashSet<string>();foreach(var lane in Timelines.Lanes){
   if(lane==null||string.IsNullOrWhiteSpace(lane.Id)||!ids.Add(lane.Id)||string.IsNullOrWhiteSpace(lane.Name)||lane.Name.Length>1000||lane.TaskUids==null||lane.TaskUids.Count>100000||lane.View==null)throw new InvalidDataException("Invalid timeline layout.");
   new DateAxis().Restore(lane.View);lane.Style?.Checked();if(lane.ItemStyles!=null)foreach(var style in lane.ItemStyles.Values)style.Checked();
  }
  return this;
 }
 public ProjectPresentation Copy()=>JsonSerializer.Deserialize<ProjectPresentation>(JsonSerializer.Serialize(this,DurableCommand.Json),DurableCommand.Json)!.Checked();
}

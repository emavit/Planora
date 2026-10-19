using Avalonia.Controls;
namespace ProjectDesk;
public sealed partial class PlannerWindow {
 private void AddPlanningRibbon(RibbonModule ribbon){
  Button Command(string name,string label,Func<Task> action){var b=Action(label,action);b.Name=name;return b;}
  ribbon.AddTab("Planning",
   RibbonModule.Group("Create",Command("PlanningAddTask","＋ Task",()=>InsertTask(false)),Command("PlanningMilestone","Milestone",()=>InsertTask(true)),Command("PlanningColumns","Choose columns…",()=>Sheet.ChooseColumns(this))),
   RibbonModule.Group("Gantt",Command("PlanningZoomIn","Zoom in",()=>{_axis.ZoomIn();return Task.CompletedTask;}),Command("PlanningZoomOut","Zoom out",()=>{_axis.ZoomOut();return Task.CompletedTask;}),Command("PlanningFit","Fit project",()=>{_axis.EntireProject();return Task.CompletedTask;})),
   RibbonModule.Group("Dates & selection",Command("PlanningDates","Gantt dates…",ZoomDialog),Command("PlanningToday","Today",GoToToday),Command("PlanningSelected","Selected task",ScrollToSelectedTask)),
   RibbonModule.Group("Timeline",Command("PlanningAddTimeline","New timeline",()=>{_timeline.AddTimeline();SetTimelineVisible(true);return Task.CompletedTask;}),Command("PlanningTimelineTask","Add selected tasks",()=>{_timeline.AddSelection(SelectedTaskUids());SetTimelineVisible(true);return Task.CompletedTask;}),Command("PlanningTimelineDates","Timeline dates…",()=>_timeline.EditDates())),
   RibbonModule.Group("Schedule",Command("PlanningAutomatic","Auto schedule",()=>TaskMode(false)),Command("PlanningLinks","Link tasks…",LinkTasks),Command("PlanningCalculate","Calculate project",CalculateProject)),
   RibbonModule.Group("Presentation",Command("PlanningColors","Bar colors…",EditChartColors),Command("PlanningText","Text & color…",EditTextFormat),Command("PlanningPdf","Export PDF…",ExportPdf)));
 }
}

namespace ProjectDesk;

public sealed record StarterTask(string Name,int Days);
public sealed record StarterTemplate(string Id,string Category,string Name,string Description,StarterTask[] Tasks);

// Original Planora content. These are schedule starters, not copies of vendor templates or workflow engines.
public static class StarterTemplates {
    public static IReadOnlyList<StarterTemplate> All {get;}=[
        Make("software","Software development","Software delivery","Plan a small delivery from scope through release.",("Define scope",2),("Design solution",3),("Implement",5),("Validate",3),("Release",0)),
        Make("marketing","Marketing","Campaign launch","Coordinate briefing, assets and launch readiness.",("Campaign brief",2),("Create assets",4),("Review campaign",2),("Prepare launch",1),("Launch",0)),
        Make("design","Design","Design review","Move from discovery to a reviewed design handoff.",("Discover needs",3),("Explore concepts",3),("Prototype",4),("Review usability",2),("Handoff",0)),
        Make("sales","Sales","Sales proposal","Organize discovery and proposal preparation.",("Discovery",2),("Define solution",3),("Prepare proposal",2),("Internal review",1),("Proposal ready",0)),
        Make("operations","Operations","Process improvement","Define, trial and review an operational improvement.",("Map current process",2),("Design improvement",3),("Pilot change",5),("Review outcomes",2),("Rollout decision",0)),
        Make("service","Service management","Service readiness","Prepare a service for operational handover.",("Define service scope",2),("Prepare runbook",3),("Validate support process",2),("Readiness review",1),("Service ready",0)),
        Make("hr","Human resources","Employee onboarding","Coordinate an onboarding schedule using generic tasks.",("Prepare onboarding plan",1),("Prepare access and equipment",2),("Welcome and orientation",1),("Role training",5),("First review",0)),
        Make("legal","Legal","Document review","Track a document through preparation and review.",("Collect requirements",2),("Prepare draft",3),("Review draft",2),("Resolve comments",2),("Review complete",0)),
        Make("it","IT operations","System rollout","Plan a staged system rollout and verification.",("Assess requirements",2),("Prepare environment",3),("Pilot rollout",2),("Validate recovery",2),("Rollout ready",0)),
        Make("finance","Finance","Period close","Coordinate a generic period-close task schedule.",("Collect records",2),("Reconcile balances",3),("Review exceptions",2),("Prepare reports",2),("Close review",0))
    ];
    private static StarterTemplate Make(string id,string category,string name,string description,params (string Name,int Days)[] tasks)=>new(id,category,name,description,tasks.Select(t=>new StarterTask(t.Name,t.Days)).ToArray());
    public static DurableProjectState CreateState(string id,string name,DateTime start){
        var template=All.SingleOrDefault(t=>t.Id==id)??throw new ArgumentException("Unknown template.");
        if(string.IsNullOrWhiteSpace(name)||name.Length>200)throw new ArgumentException("Enter a project name of 1–200 characters.");
        using var staging=new ProjectSession();staging.NewProject(name.Trim(),DateTime.SpecifyKind(start.Date.AddHours(8),DateTimeKind.Unspecified));
        int? previous=null;
        foreach(var task in template.Tasks){int uid=staging.AddTask(task.Name,task.Days==0);var edits=new List<TextCellChange>{new(uid,"Duration",task.Days+"d"),new(uid,"TaskMode","Auto Scheduled")};if(previous is int predecessor)edits.Add(new(uid,"Predecessors",staging.Tasks.Single(t=>t.Uid==predecessor).DisplayId+"FS"));staging.EditCells(edits);previous=uid;}
        var state=staging.CaptureState();
        return state with{History=[new DurableHistoryEntry("Create from "+template.Name,DurableCommand.Batch(state.History.Select(h=>h.Command)),Guid.NewGuid())],Position=1,SavedPosition=0};
    }
}

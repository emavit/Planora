using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OfficeIMO.Project;
namespace ProjectDesk;

// Claude Code control: an MCP client drives the open project over the private socket and through the --mcp-connect relay.
// Approvals are scripted; no Claude Code installation or provider is contacted.
public static class PlanoraMcpValidation {
 public static int Run(string output){
  Directory.CreateDirectory(output);var checks=new List<string>();void Assert(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
  AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
  string folder=Path.Combine(Path.GetTempPath(),"planora-check-"+Guid.NewGuid().ToString("N")[..8]);string socket=Path.Combine(folder,"mcp.sock");
  using var session=new ProjectSession();session.NewProject("Claude Code control",new DateTime(2026,10,19,8,0,0));
  var window=new PlannerWindow(session,Path.Combine(output,"views.json"));window.Show();Pump();
  var approvals=new List<string>();Func<Task<bool>> answer=()=>Task.FromResult(true);
  window.McpApprovalOverride=(title,text,accept)=>{approvals.Add(title+"\n"+text);return answer();};
  McpClient? client=null;
  try{
   window.StartClaudeCodeControl(socket);Assert(window.ClaudeCodeListening&&File.Exists(socket),"Planora listens on its private control socket");
   if(!OperatingSystem.IsWindows()){Assert(File.GetUnixFileMode(socket)==(UnixFileMode.UserRead|UnixFileMode.UserWrite),"Socket is readable and writable by this user only");Assert(File.GetUnixFileMode(folder)==(UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute),"Socket folder is private to this user");}
   Assert(PlanoraMcpHost.Start(socket,(_,_,_)=>Task.FromResult<object>(new{}))==null&&PlanoraMcpHost.Answers(socket),"A second Planora does not take over a live socket");
   client=Wait(Task.Run(()=>Connect(socket,"Validation client")));
   var tools=Wait(Task.Run(async()=>await client.ListToolsAsync()));
   Assert(tools.Count==PlanoraMcpTools.All.Count&&tools.All(t=>t.Description.Length>0),"All Planora tools are listed with descriptions");
   Assert(new[]{"get_project","list_tasks","get_task","list_resources","list_assignments","show_view","select_task"}.All(n=>tools.Single(t=>t.Name==n).ProtocolTool.Annotations?.ReadOnlyHint==true)&&tools.Where(t=>t.Name is "delete_tasks" or "delete_resources").All(t=>t.ProtocolTool.Annotations?.DestructiveHint==true),"Read and destructive tools are annotated");
   JsonElement Call(string tool,object arguments,bool error=false,McpClient? through=null){
    var values=JsonSerializer.SerializeToElement(arguments).EnumerateObject().ToDictionary(p=>p.Name,p=>(object?)p.Value);
    var result=Wait(Task.Run(async()=>await (through??client!).CallToolAsync(tool,values)));
    if((result.IsError==true)!=error)throw new Exception(tool+(error?" unexpectedly succeeded: ":" failed: ")+result.StructuredContent);
    return result.StructuredContent!.Value;
   }
   string Revision()=>Call("get_project",new{}).GetProperty("revision").GetString()!;
   var first=client;Wait(Task.Run(async()=>{await first.DisposeAsync();return 0;}));window.ReleaseClaudeCodeControl();using(var empty=new ProjectSession()){var blank=new PlannerWindow(empty,Path.Combine(output,"empty-views.json"));blank.McpApprovalOverride=window.McpApprovalOverride;blank.Show();Pump();blank.StartClaudeCodeControl(socket);var direct=Wait(Task.Run(()=>Connect(socket,"claude-code")));
    var none=Call("add_tasks",new{revision=Call("get_project",new{},through:direct).GetProperty("revision").GetString(),tasks=new[]{new{name="Orphan"}}},error:true,through:direct);
    Assert(none.GetProperty("error").GetString()!.Contains("No project is open")&&approvals.Count==0,"Without an open project a change is refused before asking");
    Wait(Task.Run(async()=>{await direct.DisposeAsync();return 0;}));blank.ReleaseClaudeCodeControl();blank.CloseForValidation();Pump();}
   window.StartClaudeCodeControl(socket);client=Wait(Task.Run(()=>Connect(socket,"Validation client")));
   var project=Call("get_project",new{});Assert(project.GetProperty("name").GetString()=="Claude Code control"&&project.GetProperty("tasks").GetInt32()==0&&approvals.Count==0,"Reading the project needs no approval");

   int history=session.HistoryPosition;string revision=Revision();
   var added=Call("add_tasks",new{revision,tasks=new object[]{new{name="Design",duration="2d"},new{name="Build",duration="3d",predecessors="1"},new{name="Launch",milestone=true,predecessors="2"}}});
   var design=session.Tasks.Single(t=>t.Name=="Design");var build=session.Tasks.Single(t=>t.Name=="Build");var launch=session.Tasks.Single(t=>t.Name=="Launch");
   Assert(session.Tasks.Count==3&&session.HistoryPosition==history+1&&added.GetProperty("result").GetProperty("taskUids").GetArrayLength()==3,"Approved tasks are added as one Undo step");
   Assert(approvals.Single().Contains("Validation client wants to change")&&approvals[0].Contains("Design · 2d")&&approvals[0].Contains("one Undo step"),"The approval names the client and shows the proposed tasks");
   Assert(build.Start>=design.Finish&&launch.IsMilestone==true&&launch.Start>=build.Finish,"New tasks are scheduled from their predecessors");
   Assert(added.GetProperty("revision").GetString()!=revision,"The result carries the new revision");

   var stale=Call("update_tasks",new{revision,changes=new[]{new{uid=design.Uid,field="Duration",value="5d"}}},error:true);
   Assert(stale.GetProperty("error").GetString()!.Contains("changed since")&&approvals.Count==1&&session.Document!.Tasks.GetByUid(design.Uid).Duration?.Value==2,"A change against an old revision is refused without asking");
   answer=()=>Task.FromResult(false);history=session.HistoryPosition;
   var declined=Call("update_tasks",new{revision=Revision(),changes=new[]{new{uid=design.Uid,field="Duration",value="5d"}}},error:true);
   Assert(declined.GetProperty("error").GetString()!.Contains("declined")&&session.HistoryPosition==history&&session.Document!.Tasks.GetByUid(design.Uid).Duration?.Value==2,"A declined change leaves the project unchanged");
   answer=()=>Task.FromResult(true);var buildStart=session.Document!.Tasks.GetByUid(build.Uid).Start;
   Call("update_tasks",new{revision=Revision(),changes=new[]{new{uid=design.Uid,field="Duration",value="4d"},new{uid=design.Uid,field="Notes",value="Reviewed by Claude"}}});
   Assert(session.Document!.Tasks.GetByUid(design.Uid).Duration?.Value==4&&session.Document.Tasks.GetByUid(build.Uid).Start>buildStart&&session.HistoryPosition==history+1,"Field changes apply together and successors follow automatically");
   Assert(approvals.Last().Contains("Duration: 2d → 4d"),"The approval shows old and new values");
   Call("undo",new{revision=Revision()});Assert(session.Document!.Tasks.GetByUid(design.Uid).Duration?.Value==2&&session.Document.Tasks.GetByUid(build.Uid).Start==buildStart&&approvals.Last().Contains("Undo: Edit tasks"),"Undo through Claude Code reverses the whole change after approval");
   Call("redo",new{revision=Revision()});Assert(session.Document!.Tasks.GetByUid(design.Uid).Duration?.Value==4,"Redo through Claude Code");
   bool invalid=Call("update_tasks",new{revision=Revision(),changes=new[]{new{uid=design.Uid,field="Duration",value="soon"}}},error:true).GetProperty("error").GetString()!.Contains("duration");
   Assert(invalid&&!approvals.Last().Contains("soon"),"Invalid values are refused before the approval");
   bool badEnum=Call("update_tasks",new{revision=Revision(),changes=new[]{new{uid=design.Uid,field="ConstraintType",value="99"}}},error:true).GetProperty("error").GetString()!.Contains("valid");Assert(badEnum,"Undefined enumeration values are refused");

   var list=Call("list_tasks",new{});var rows=list.GetProperty("tasks").EnumerateArray().ToList();
   Assert(list.GetProperty("total").GetInt32()==3&&rows[1].GetProperty("predecessors").GetString()=="1FS"&&rows[2].GetProperty("milestone").GetBoolean(),"Tasks are listed in project order with predecessors");
   Assert(Call("list_tasks",new{query="bui"}).GetProperty("tasks").GetArrayLength()==1&&Call("list_tasks",new{offset=2,limit=5}).GetProperty("tasks").GetArrayLength()==1,"Task listing filters and pages");
   var task=Call("get_task",new{uid=design.Uid});Assert(task.GetProperty("fields").GetProperty("Notes").GetString()=="Reviewed by Claude"&&task.GetProperty("successors").GetArrayLength()==1,"A task's fields and successors are readable");
   Assert(Call("get_task",new{uid=9999},error:true).GetProperty("error").GetString()!.Contains("not found"),"Unknown task UIDs are reported");

   history=session.HistoryPosition;Call("outline_tasks",new{revision=Revision(),uids=new[]{build.Uid,launch.Uid},direction="indent"});
   Assert(session.Document!.Tasks.GetByUid(build.Uid).Parent?.Uid==design.Uid&&session.Document.Tasks.GetByUid(launch.Uid).Parent?.Uid==design.Uid&&session.HistoryPosition==history+1,"Indenting several tasks is one Undo step");
   Call("outline_tasks",new{revision=Revision(),uids=new[]{build.Uid,launch.Uid},direction="outdent"});
   Assert(session.Tasks.Select(t=>t.Name).SequenceEqual(["Design","Build","Launch"])&&session.Tasks.All(t=>t.Parent==null||t.Parent.Uid==0),"Outdenting restores the original order");

   var resources=Call("add_resources",new{revision=Revision(),resources=new object[]{new{name="Engineer",standardRate=80},new{name="Concrete",type="Material"}}});
   int engineer=resources.GetProperty("result").GetProperty("resourceUids")[0].GetInt32();
   Assert(session.Resources.Count==2&&session.Resources.Single(r=>r.Uid==engineer).Name=="Engineer","Resources are added");
   Call("assign_resources",new{revision=Revision(),assignments=new[]{new{taskUid=build.Uid,resourceUid=engineer,unitsPercent=50}}});
   var assignments=Call("list_assignments",new{taskUid=build.Uid}).GetProperty("assignments");Assert(assignments.GetArrayLength()==1&&assignments[0].GetProperty("unitsPercent").GetDecimal()==50,"Resources are assigned with units");
   Call("update_resources",new{revision=Revision(),changes=new[]{new{uid=engineer,field="Capacity",value="50"}}});Assert(decimal.Parse(session.ResourceText(engineer,"Capacity"),System.Globalization.CultureInfo.InvariantCulture)==50,"Resource fields change");
   Assert(Call("list_resources",new{}).GetProperty("resources").GetArrayLength()==2,"Resources are readable");
   Call("update_project",new{revision=Revision(),name="Claude Code plan",statusDate="2026-10-21"});Assert(session.Document!.Name=="Claude Code plan"&&session.Document.Settings.StatusDate==new DateTime(2026,10,21),"Project information changes");
   Call("set_baseline",new{revision=Revision(),number=1});Assert(session.Document!.Tasks.GetByUid(build.Uid).Baselines.Any(b=>b.Number==1),"A baseline is saved");
   Assert(Call("set_baseline",new{revision=Revision(),number=1},error:true).GetProperty("error").GetString()!.Contains("already exists"),"An existing baseline is not overwritten without explicit permission");
   Call("delete_tasks",new{revision=Revision(),uids=new[]{launch.Uid}});Assert(session.Tasks.Count==2&&approvals.Last().Contains("Launch"),"Tasks are deleted after approval");

   Call("show_view",new{view="Resource Sheet"});Pump();Assert(window.ResourceGrid.IsVisible,"Claude Code can switch views");
   Call("select_task",new{uid=build.Uid});Pump();Assert(window.Sheet.SelectedUid==build.Uid,"Claude Code can select a task");

   string saved=Path.Combine(output,"claude-code-copy.mpp");int asked=approvals.Count;
   Assert(Call("save_project_copy",new{path="relative.mpp"},error:true).GetProperty("error").GetString()!.Contains("absolute"),"Saving needs an absolute path");
   File.WriteAllText(Path.Combine(output,"existing.mpp"),"keep");Assert(Call("save_project_copy",new{path=Path.Combine(output,"existing.mpp")},error:true).GetProperty("error").GetString()!.Contains("never overwrites")&&File.ReadAllText(Path.Combine(output,"existing.mpp"))=="keep"&&approvals.Count==asked,"Existing files are never overwritten");
   Call("save_project_copy",new{path=saved});using(var reopened=ProjectDocument.Load(saved))Assert(reopened.AllTasks.Any(t=>t.Name=="Build")&&approvals.Last().Contains(saved),"An approved copy is saved and reopens");
   string package=Path.Combine(output,"claude-code-copy.planora");Call("save_project_copy",new{path=package});Assert(File.Exists(package),"A .planora copy can be saved");

   // A pending decision holds further proposals; reads continue.
   var pending=new TaskCompletionSource<bool>();answer=()=>pending.Task;
   string current=Revision();var waiting=Task.Run(async()=>await client.CallToolAsync("update_project",new Dictionary<string,object?>{["revision"]=current,["name"]="Pending"}));
   WaitUntil(()=>window.McpApprovalPending);
   Assert(Call("update_project",new{revision=Revision(),name="Second"},error:true).GetProperty("error").GetString()!.Contains("busy")&&Call("get_project",new{}).GetProperty("busy").GetBoolean(),"Only one change waits for approval at a time; reading continues");
   pending.SetResult(false);var waited=Wait(waiting);Assert(waited.IsError==true&&session.Document!.Name=="Claude Code plan","The pending change is resolved by the user's decision");answer=()=>Task.FromResult(true);
   Assert(Call("missing_tool",new{},error:true).GetProperty("error").GetString()!.Contains("Unknown"),"Unknown tools are reported");

   // Claude Code runs "Planora --mcp-connect"; the relay passes the client's name through to the approval.
   var self=Program.SelfCommand("--mcp-connect","--no-launch");var environment=StdioClientTransportOptions.GetDefaultEnvironmentVariables();environment["PLANORA_MCP_SOCKET"]=socket;
   var relayDefinition=McpConnectorsWindow.DemoDefinition() with{Address=self.FileName,Arguments=self.Arguments};
   McpLocalProcess? relayProcess=null;McpClient? relay=null;
   try{
    relayProcess=McpLocalProcess.Start(relayDefinition,environment);relay=Wait(Task.Run(()=>McpClient.CreateAsync(relayProcess.Transport,new(){ClientInfo=new(){Name="Relay client",Version="1"}})));
    Assert(Wait(Task.Run(async()=>await relay.ListToolsAsync())).Count==PlanoraMcpTools.All.Count,"The relay lists Planora tools");
    Call("update_project",new{revision=Revision(),name="Through relay"},through:relay);Assert(session.Document!.Name=="Through relay"&&approvals.Last().StartsWith("Relay client wants to change"),"Changes through the relay are approved under the client's name");
   }finally{if(relay!=null)Wait(Task.Run(async()=>{await relay.DisposeAsync();return 0;}));if(relayProcess!=null)Wait(Task.Run(async()=>{await relayProcess.DisposeAsync();return 0;}));}
   environment["PLANORA_MCP_SOCKET"]=Path.Combine(folder,"absent.sock");McpLocalProcess? offline=null;McpClient? offlineClient=null;
   try{
    offline=McpLocalProcess.Start(relayDefinition,environment);offlineClient=Wait(Task.Run(()=>McpClient.CreateAsync(offline.Transport)));
    Assert(Wait(Task.Run(async()=>await offlineClient.ListToolsAsync())).Count==PlanoraMcpTools.All.Count,"Claude Code starts without launching Planora: the relay lists tools itself");
    Assert(Call("get_project",new{},error:true,through:offlineClient).GetProperty("error").GetString()!.Contains("not running"),"Calls report when Planora is not running");
   }finally{if(offlineClient!=null)Wait(Task.Run(async()=>{await offlineClient.DisposeAsync();return 0;}));if(offline!=null)Wait(Task.Run(async()=>{await offline.DisposeAsync();return 0;}));}

   // The setup window: the control switch, Claude Code command and light/dark rendering.
   foreach(var mode in new[]{"Light","Dark"}){
    window.SetThemeMode(mode);var setup=new ClaudeCodeWindow(window,checkRegistration:false);setup.Show(window);Pump();
    using(var frame=setup.CaptureRenderedFrame()??throw new Exception("No Claude Code frame"))frame.Save(Path.Combine(output,"claude-code-"+mode+".png"));
    Assert(setup.GetVisualDescendants().OfType<Button>().Any(b=>Equals(b.Content,"Connect Claude Code"))&&setup.GetVisualDescendants().OfType<TextBox>().Any(t=>t.Text?.Contains("mcp add --scope user planora --")==true),"Claude Code setup offers Connect and the Terminal command in "+mode);
    if(mode=="Light"){var allow=setup.GetVisualDescendants().OfType<CheckBox>().Single(c=>c.Name=="ClaudeCodeAllow");allow.IsChecked=false;WaitUntil(()=>!window.ClaudeCodeListening&&!File.Exists(socket));Assert(!PlanoraMcpHost.Answers(socket),"Turning control off closes the socket");}
    setup.Close();Pump();
   }
   window.StartClaudeCodeControl(socket);Assert(!window.ClaudeCodeListening&&window.ClaudeCodeStatus.Contains("off"),"Control stays off until the user turns it on again");
   window.SetClaudeCodeControl(true).GetAwaiter().GetResult();Assert(window.ClaudeCodeListening,"Turning control on listens again");
   Wait(window.StopClaudeCodeControl().ContinueWith(_=>0));Assert(!File.Exists(socket),"Stopping removes the socket file");
   using(var leftover=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified))leftover.Bind(new UnixDomainSocketEndPoint(socket));
   window.StartClaudeCodeControl(socket);Assert(window.ClaudeCodeListening,"A socket left by a closed Planora is replaced");

   var editor=new AiConnectionEditor(new AiConnectionProfile(Guid.NewGuid().ToString(),AiProvider.Anthropic,AiConnectionMode.Browser),new AiSettingsStore(Path.Combine(output,"ai.json")),new McpCredentialStore());editor.Show(window);Pump();
   Assert(editor.GetVisualDescendants().OfType<Button>().Any(b=>Equals(b.Content,"Connect Claude Code…")&&b.IsEffectivelyVisible)&&!editor.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.Contains("requires Anthropic approval")==true),"Claude subscription settings offer Connect Claude Code");editor.Close();
   Assert(ClaudeCodeSetup.AddArguments(["/Apps/Planora","--mcp-connect"]).SequenceEqual(["mcp","add","--scope","user","planora","--","/Apps/Planora","--mcp-connect"]),"Claude Code registration uses the user scope");
   Assert(ClaudeCodeSetup.ManualCommand(["/My Apps/Planora","--mcp-connect"]).Contains(OperatingSystem.IsWindows()?"\"/My Apps/Planora\"":"'/My Apps/Planora'"),"The Terminal command quotes paths with spaces");
   Assert(ClaudeCodeSetup.LocationProblem("/private/var/folders/x/AppTranslocation/1/d/Planora.app/Contents/MacOS/Planora")!=null&&ClaudeCodeSetup.LocationProblem("/Applications/Planora.app/Contents/MacOS/Planora")==null,"A translocated app is not registered");
   string fake=Path.Combine(output,"cli-"+Guid.NewGuid().ToString("N")[..6]);Directory.CreateDirectory(fake);Assert(ClaudeCodeSetup.FindCli([fake])==null,"Missing Claude Code is reported");File.WriteAllText(Path.Combine(fake,ClaudeCodeSetup.ExecutableName),"");Assert(ClaudeCodeSetup.FindCli([fake])==Path.Combine(fake,ClaudeCodeSetup.ExecutableName),"Claude Code is found in known install folders");
   if(!OperatingSystem.IsWindows())Assert(PlanoraMcpBridge.BundleOf("/Applications/Planora.app/Contents/MacOS/Planora")=="/Applications/Planora.app"&&PlanoraMcpBridge.BundleOf("/opt/planora/Planora")==null,"The relay starts the app bundle through LaunchServices");
  }finally{
   if(client!=null)Wait(Task.Run(async()=>{await client.DisposeAsync();return 0;}));
   window.ReleaseClaudeCodeControl();window.CloseForValidation();Pump();
   try{Directory.Delete(folder,true);}catch(IOException){}
  }
  File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{passed=checks.Count,checks},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS: {checks.Count} Claude Code control checks.");return 0;
 }
 private static async Task<McpClient> Connect(string path,string name){
  var socket=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified);await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));var stream=new NetworkStream(socket,ownsSocket:true);
  return await McpClient.CreateAsync(new StreamClientTransport(stream,stream),new(){ClientInfo=new(){Name=name,Version="1"}});
 }
 private static T Wait<T>(Task<T> task){var clock=Stopwatch.StartNew();while(!task.IsCompleted){Pump();Thread.Sleep(5);if(clock.Elapsed>TimeSpan.FromSeconds(60))throw new TimeoutException("MCP call did not finish.");}return task.GetAwaiter().GetResult();}
 private static void WaitUntil(Func<bool> predicate){var clock=Stopwatch.StartNew();while(!predicate()){Pump();Thread.Sleep(5);if(clock.Elapsed>TimeSpan.FromSeconds(30))throw new TimeoutException("Condition was not reached.");}}
 private static void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick();Dispatcher.UIThread.RunJobs();}
}

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;
namespace ProjectDesk;
// A separate process for trying connectors. It has no access to the active UI project.
internal static class McpDemoServer {
 public static async Task<int> Run(){
  await using var server=McpServer.Create(new StdioServerTransport("Planora demo"),new(){ServerInfo=new(){Name="Planora demo",Version="1.0"},Capabilities=new(){Tools=new()},Handlers=new(){
   ListToolsHandler=(_,_)=>ValueTask.FromResult(new ListToolsResult{Tools=[new(){Name="echo",Description="Return the provided message. No files or project changes.",InputSchema=JsonSerializer.SerializeToElement(new{type="object",properties=new{message=new{type="string"}},required=new[]{"message"},additionalProperties=false}),Annotations=new(){ReadOnlyHint=true}},new(){Name="example_tasks",Description="Return three example tasks for a reviewed import.",InputSchema=JsonSerializer.SerializeToElement(new{type="object",properties=new{},additionalProperties=false}),Annotations=new(){ReadOnlyHint=true}}]}),
   CallToolHandler=(request,_)=>{string name=request.Params?.Name??"";object data=name switch{"echo"=>new{message=request.Params!.Arguments?["message"].GetString()??"",processId=Environment.ProcessId},"example_tasks"=>new{tasks=new[]{new{name="Define project scope",duration="2d"},new{name="Develop first version",duration="5d"},new{name="Review and test",duration="2d"}}},_=>throw new ArgumentException("Unknown demo tool.")};return ValueTask.FromResult(new CallToolResult{Content=[new TextContentBlock{Text=JsonSerializer.Serialize(data)}],StructuredContent=JsonSerializer.SerializeToElement(data)});}
  }});await server.RunAsync();return 0;
 }
}

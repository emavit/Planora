using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;
using System.Net;
using System.Text;
using System.Text.Json;
namespace ProjectDesk;

public sealed record McpToolInfo(string Name,string Description,JsonElement Schema,bool? ReadOnlyHint){public override string ToString()=>Name;}
public sealed class McpConnection : IAsyncDisposable {
 private McpClient? _client;private McpLocalProcess? _local;private HttpClientTransport? _http;private readonly IMcpSecrets _secrets;private readonly Func<AuthorizationCallbackContext,CancellationToken,Task<AuthorizationResult>>? _authorize;
 public McpServerDefinition Definition {get;}public IReadOnlyList<McpToolInfo> Tools {get;private set;}=[];public bool Connected=>_client!=null&&!_client.Completion.IsCompleted;public string ServerName=>_client?.ServerInfo.Name??Definition.Name;public string? Protocol=>_client?.NegotiatedProtocolVersion;
 public McpConnection(McpServerDefinition definition,IMcpSecrets secrets,Func<AuthorizationCallbackContext,CancellationToken,Task<AuthorizationResult>>? authorize=null){definition.Validate();Definition=definition with{Arguments=definition.Arguments.ToArray(),Scopes=definition.Scopes?.ToArray(),SecretEnvironmentNames=definition.SecretEnvironmentNames?.ToArray()};_secrets=secrets;_authorize=authorize;}
 public string ReviewRevision(string projectRevision)=>projectRevision+"|"+Definition.Fingerprint();
 public async Task ConnectAsync(CancellationToken token,HttpClient? testHttp=null,Uri? redirectUri=null){
  if(_client!=null)throw new InvalidOperationException("Disconnect before reconnecting.");IClientTransport transport;
  if(Definition.Transport==McpConnectorTransport.LocalStdio){var environment=StdioClientTransportOptions.GetDefaultEnvironmentVariables();foreach(var key in Definition.SecretEnvironmentNames??[])environment[key]=_secrets.Read(Definition.Id,"Env_"+key)??throw new InvalidOperationException("Set the credential for environment variable "+key+".");_local=McpLocalProcess.Start(Definition,environment);transport=_local.Transport;}
  else{var headers=new Dictionary<string,string>();if(Definition.Authentication==McpAuthentication.BearerToken){var bearer=_secrets.Read(Definition.Id,"Bearer")??throw new InvalidOperationException("Set the bearer token first.");if(bearer.Contains('\r')||bearer.Contains('\n'))throw new ArgumentException("Invalid token.");headers["Authorization"]="Bearer "+bearer;}
   var options=new HttpClientTransportOptions{Endpoint=new Uri(Definition.Address),Name=Definition.Name,TransportMode=HttpTransportMode.StreamableHttp,AdditionalHeaders=headers,EnableStandaloneGetStream=false,MaxReconnectionAttempts=0,ConnectionTimeout=TimeSpan.FromSeconds(30)};
   if(Definition.Authentication==McpAuthentication.BrowserOAuth){if(_authorize==null||redirectUri==null)throw new InvalidOperationException("Browser sign-in is unavailable.");options.OAuth=new(){RedirectUri=redirectUri,ClientId=string.IsNullOrWhiteSpace(Definition.ClientId)?null:Definition.ClientId,Scopes=Definition.Scopes??[],DynamicClientRegistration=new(){ClientName=AppBrand.Name},AuthorizationCallbackHandler=async(context,cancellation)=>await _authorize(context,cancellation)};}
   _http=testHttp==null?new HttpClientTransport(options,new HttpClient(new SocketsHttpHandler{AllowAutoRedirect=false}){MaxResponseContentBufferSize=4*1024*1024},ownsHttpClient:true):new HttpClientTransport(options,testHttp,ownsHttpClient:false);transport=_http;
  }
  try{_client=await McpClient.CreateAsync(transport,new(){ClientInfo=new(){Name=AppBrand.Name,Version="0.1"},ProtocolVersion=Definition.ProtocolVersion,InitializationTimeout=TimeSpan.FromMinutes(3),DiscoverProbeTimeout=TimeSpan.FromSeconds(2)},cancellationToken:token);await RefreshToolsAsync(token);}catch{await DisposeAsync();throw;}
 }
 public async Task RefreshToolsAsync(CancellationToken token){var client=_client??throw new InvalidOperationException("Connect first.");var tools=new List<McpToolInfo>();string? cursor=null;var seen=new HashSet<string>();for(int page=0;page<32;page++){var result=await client.ListToolsAsync(new ListToolsRequestParams{Cursor=cursor},token);foreach(var t in result.Tools){if(tools.Any(x=>x.Name==t.Name))throw new InvalidDataException("Server returned duplicate tool names.");if(t.InputSchema.GetRawText().Length>65536)throw new InvalidDataException("A tool schema exceeds 64 KiB.");tools.Add(new(t.Name,(t.Description??"")[..Math.Min(t.Description?.Length??0,4096)],t.InputSchema.Clone(),t.Annotations?.ReadOnlyHint));if(tools.Count>500)throw new InvalidDataException("This connector lists more than 500 tools.");}cursor=result.NextCursor;if(cursor==null){Tools=tools;return;}if(!seen.Add(cursor))throw new InvalidDataException("Server repeated a tool-list cursor.");}throw new InvalidDataException("Too many tool-list pages.");}
 public async Task<string> InvokeAsync(McpToolProposal proposal,Guid approval,McpInvocationGate gate,Func<string> revision,CancellationToken token){
  var tool=Tools.SingleOrDefault(t=>t.Name==proposal.ToolName)??throw new ArgumentException("Choose a discovered tool.");McpArgumentValidator.Validate(proposal.ArgumentsJson,tool.Schema);
  gate.DemandAndConsume(approval,proposal,Definition.Profile,ReviewRevision(revision()),true,Connected,new HashSet<string>{tool.Name});
  using var input=JsonDocument.Parse(proposal.ArgumentsJson);var args=input.RootElement.EnumerateObject().ToDictionary(p=>p.Name,p=>(object?)p.Value.Clone());
  var result=await _client!.CallToolAsync(tool.Name,args,cancellationToken:token);string json=JsonSerializer.Serialize(result,McpJsonUtilities.DefaultOptions);if(Encoding.UTF8.GetByteCount(json)>2*1024*1024)throw new InvalidDataException("The tool result exceeds the 2 MiB display/import limit.");return json;
 }
 public async ValueTask DisposeAsync(){var client=_client;_client=null;Tools=[];try{if(client!=null)await client.DisposeAsync();}finally{var local=_local;_local=null;var http=_http;_http=null;try{if(local!=null)await local.DisposeAsync();}finally{if(http!=null)await http.DisposeAsync();}}}
}
public static class McpArgumentValidator {
 public static void Validate(string json,JsonElement schema){using var input=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});var root=input.RootElement;if(root.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Arguments must be a JSON object.");Check(root,schema,"Arguments");}
 private static void Check(JsonElement value,JsonElement schema,string path){if(schema.ValueKind!=JsonValueKind.Object)return;if(schema.TryGetProperty("type",out var t)&&t.ValueKind==JsonValueKind.String){bool valid=t.GetString() switch{"object"=>value.ValueKind==JsonValueKind.Object,"array"=>value.ValueKind==JsonValueKind.Array,"string"=>value.ValueKind==JsonValueKind.String,"boolean"=>value.ValueKind is JsonValueKind.True or JsonValueKind.False,"integer"=>value.ValueKind==JsonValueKind.Number&&value.TryGetInt64(out _),"number"=>value.ValueKind==JsonValueKind.Number,"null"=>value.ValueKind==JsonValueKind.Null,_=>true};if(!valid)throw new ArgumentException(path+" must be "+t.GetString()+".");}
  if(schema.TryGetProperty("enum",out var choices)&&choices.ValueKind==JsonValueKind.Array&&!choices.EnumerateArray().Any(c=>c.GetRawText()==value.GetRawText()))throw new ArgumentException(path+" is not an allowed value.");
  if(value.ValueKind==JsonValueKind.Object){if(schema.TryGetProperty("required",out var required))foreach(var name in required.EnumerateArray())if(!value.TryGetProperty(name.GetString()!,out _))throw new ArgumentException(path+" needs "+name.GetString()+".");if(schema.TryGetProperty("properties",out var properties))foreach(var property in value.EnumerateObject()){if(properties.TryGetProperty(property.Name,out var child))Check(property.Value,child,path+"."+property.Name);else if(schema.TryGetProperty("additionalProperties",out var extra)&&extra.ValueKind==JsonValueKind.False)throw new ArgumentException(path+" has an unknown field: "+property.Name);}}
  if(value.ValueKind==JsonValueKind.Array&&schema.TryGetProperty("items",out var items))foreach(var item in value.EnumerateArray())Check(item,items,path+"[]");
 }
}

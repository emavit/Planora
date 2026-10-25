using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Avalonia.Threading;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
namespace ProjectDesk;

// Claude Code (or another local MCP client) works with the project open in Planora through a private per-user socket.
// Claude Code starts "Planora --mcp-connect", which relays its stdio to this socket. Reads are answered directly;
// every change is shown in Planora for approval first and becomes one Undo step (see PlannerWindow.Mcp.cs).
internal sealed class PlanoraMcpHost : IAsyncDisposable {
 public delegate Task<object> ToolCall(string client,string tool,JsonElement arguments);
 private readonly Socket _listener;private readonly ToolCall _call;private readonly CancellationTokenSource _stop=new();private readonly Task _accept;
 public string Path{get;}
 private PlanoraMcpHost(Socket listener,string path,ToolCall call){_listener=listener;Path=path;_call=call;_accept=Task.Run(Accept);}

 // macOS/Linux: a 0700 folder in the per-user temporary (or Linux runtime) folder; Windows: the per-user Planora settings folder.
 // PLANORA_MCP_SOCKET overrides the location for tests. Unix socket paths are limited to about 104 bytes.
 public static string SocketPath(){
  if(Environment.GetEnvironmentVariable("PLANORA_MCP_SOCKET") is {Length:>0} custom)return System.IO.Path.GetFullPath(custom);
  if(OperatingSystem.IsWindows())return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ProjectDesk","planora-mcp.sock");
  string root=!OperatingSystem.IsMacOS()&&Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is {Length:>0} runtime?runtime:System.IO.Path.GetTempPath();
  return System.IO.Path.Combine(root,"planora-"+Environment.UserName,"mcp.sock");
 }

 // Returns null when another running Planora already answers on this socket. A socket file left by a closed Planora is replaced.
 public static PlanoraMcpHost? Start(string path,ToolCall call){
  if(System.Text.Encoding.UTF8.GetByteCount(path)>100)throw new IOException("The Planora control socket path is too long: "+path);
  string folder=System.IO.Path.GetDirectoryName(path)!;
  if(OperatingSystem.IsWindows())Directory.CreateDirectory(folder);
  else{Directory.CreateDirectory(folder,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
   // Changing the mode also proves this user owns Planora's folder; another user's folder makes the host refuse to start.
   if(System.IO.Path.GetFileName(folder).StartsWith("planora-",StringComparison.Ordinal))File.SetUnixFileMode(folder,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);}
  if(File.Exists(path)){if(Answers(path))return null;File.Delete(path);}
  var listener=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified);
  try{listener.Bind(new UnixDomainSocketEndPoint(path));if(!OperatingSystem.IsWindows())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite);listener.Listen(8);}
  catch{listener.Dispose();throw;}
  return new PlanoraMcpHost(listener,path,call);
 }
 public static bool Answers(string path){
  if(!File.Exists(path))return false;
  using var probe=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified);
  try{probe.Connect(new UnixDomainSocketEndPoint(path));return true;}catch(SocketException){return false;}
 }
 private async Task Accept(){
  while(!_stop.IsCancellationRequested){
   Socket client;try{client=await _listener.AcceptAsync(_stop.Token);}catch(Exception e)when(e is OperationCanceledException or ObjectDisposedException or SocketException){return;}
   _=Task.Run(()=>Serve(client));
  }
 }
 private async Task Serve(Socket client){
  try{
   await using var stream=new NetworkStream(client,ownsSocket:true);
   await using var server=McpServer.Create(new StreamServerTransport(stream,stream,AppBrand.Name),new(){ServerInfo=PlanoraMcpTools.Server,Capabilities=new(){Tools=new()},ServerInstructions=PlanoraMcpTools.Instructions,Handlers=new(){
    ListToolsHandler=(_,_)=>ValueTask.FromResult(new ListToolsResult{Tools=PlanoraMcpTools.All}),
    CallToolHandler=async(request,token)=>{
     var arguments=JsonSerializer.SerializeToElement(request.Params?.Arguments??new Dictionary<string,JsonElement>());string client=request.Server.ClientInfo?.Name switch{"claude-code"=>"Claude Code",{Length:>0} name=>name,_=>"An MCP client"};
     try{var data=await Dispatcher.UIThread.InvokeAsync(()=>_call(client,request.Params?.Name??"",arguments));return PlanoraMcpTools.Result(data,false);}
     catch(OperationCanceledException){throw;}
     catch(Exception e){return PlanoraMcpTools.Result(new{error=e.Message},true);}
    }}});
   await server.RunAsync(_stop.Token);
  }catch(Exception e)when(e is OperationCanceledException or IOException or ObjectDisposedException or SocketException){}
  catch(Exception e){Debug.WriteLine("Planora MCP connection ended: "+e);}
 }
 public async ValueTask DisposeAsync(){
  _stop.Cancel();_listener.Dispose();try{await _accept.ConfigureAwait(false);}catch(Exception){}
  try{File.Delete(Path);}catch(IOException){}catch(UnauthorizedAccessException){}
 }
}

// "Planora --mcp-connect": the command Claude Code runs. Claude Code starts its MCP servers with every session, so this
// relay answers tool listing itself and reaches (or starts) Planora only when a Planora tool is actually called.
internal static class PlanoraMcpBridge {
 public static async Task<int> Run(bool launch){
  var relay=new Relay(launch);
  try{
   await using var server=McpServer.Create(new StdioServerTransport(AppBrand.Name),new(){ServerInfo=PlanoraMcpTools.Server,Capabilities=new(){Tools=new()},ServerInstructions=PlanoraMcpTools.Instructions,Handlers=new(){
    ListToolsHandler=(_,_)=>ValueTask.FromResult(new ListToolsResult{Tools=PlanoraMcpTools.All}),
    // A call cut off because Planora closed is reported, never repeated: repeating could start Planora again.
    CallToolHandler=async(request,token)=>{
     try{var client=await relay.Client(request.Server.ClientInfo,token);return await client.CallToolAsync(request.Params!,token);}
     catch(Exception e)when(e is not OperationCanceledException){await relay.Reset();return PlanoraMcpTools.Result(new{error=e is PlanoraUnavailableException?e.Message:"Planora closed before answering. Nothing was retried; read the project again once Planora is open."},true);}
    }}});
   await server.RunAsync();
  }finally{await relay.Reset();}
  return 0;
 }
 private sealed class Relay(bool launch){
  private McpClient? _client;private readonly SemaphoreSlim _gate=new(1,1);
  public async Task<McpClient> Client(Implementation? caller,CancellationToken token){
   await _gate.WaitAsync(token);
   try{
    // A Planora closed since the previous call ends that connection; the next call reaches (or starts) a new one.
    if(_client is {Completion.IsCompleted:true}){await _client.DisposeAsync();_client=null;}
    if(_client!=null)return _client;
    string path=PlanoraMcpHost.SocketPath();var socket=await Connect(path);
    if(socket==null&&launch){StartPlanora();var clock=Stopwatch.StartNew();while(socket==null&&clock.Elapsed<TimeSpan.FromSeconds(45)){await Task.Delay(250,token);socket=await Connect(path);}}
    if(socket==null)throw new PlanoraUnavailableException("Planora is not running, or Claude Code control is turned off in Planora (Connectors > Claude Code…). Open Planora and try again.");
    var stream=new NetworkStream(socket,ownsSocket:true);
    try{_client=await McpClient.CreateAsync(new StreamClientTransport(stream,stream),new(){ClientInfo=caller??new(){Name="MCP client",Version="1.0"}},cancellationToken:token);}
    catch{await stream.DisposeAsync();throw;}
    return _client;
   }finally{_gate.Release();}
  }
  public async Task Reset(){var client=Interlocked.Exchange(ref _client,null);if(client!=null)try{await client.DisposeAsync();}catch(Exception){}}
 }
 private sealed class PlanoraUnavailableException(string message):Exception(message);
 private static async Task<Socket?> Connect(string path){
  if(!File.Exists(path))return null;
  var socket=new Socket(AddressFamily.Unix,SocketType.Stream,ProtocolType.Unspecified);
  try{await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));return socket;}catch(SocketException){socket.Dispose();return null;}
 }
 // The started Planora must not inherit the MCP pipes, so it is launched detached from this relay's stdio.
 private static void StartPlanora(){
  string executable=Environment.ProcessPath??throw new InvalidOperationException("Cannot locate Planora.");
  string? bundle=OperatingSystem.IsMacOS()?BundleOf(executable):null;
  var command=Program.SelfCommand();
  ProcessStartInfo info;
  if(bundle!=null){info=new("/usr/bin/open",["-a",bundle]);}
  else if(OperatingSystem.IsWindows()){info=new(command.FileName,command.Arguments){UseShellExecute=true};}
  else{info=new("/bin/sh",["-c","exec \"$0\" \"$@\" </dev/null >/dev/null 2>&1 &",command.FileName,..command.Arguments]);}
  if(!info.UseShellExecute){info.RedirectStandardInput=info.RedirectStandardOutput=info.RedirectStandardError=true;}
  using var process=Process.Start(info)??throw new InvalidOperationException("Could not start Planora.");
  if(!info.UseShellExecute){process.StandardInput.Close();process.WaitForExit(10000);}
 }
 // ".../Planora.app/Contents/MacOS/Planora" belongs to Planora.app; LaunchServices then reuses a running copy.
 internal static string? BundleOf(string executable){var macos=System.IO.Path.GetDirectoryName(executable);var contents=System.IO.Path.GetDirectoryName(macos);var bundle=System.IO.Path.GetDirectoryName(contents);return System.IO.Path.GetFileName(macos)=="MacOS"&&System.IO.Path.GetFileName(contents)=="Contents"&&bundle!=null&&bundle.EndsWith(".app",StringComparison.OrdinalIgnoreCase)?bundle:null;}
}

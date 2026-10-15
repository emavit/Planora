using System.Diagnostics;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
namespace ProjectDesk;

// The SDK's Windows launcher routes through cmd.exe. Launch the selected executable directly
// so spaces, quotes, percent signs and shell metacharacters remain literal argument values.
internal sealed class McpLocalProcess : IAsyncDisposable {
 private readonly Process _process;private readonly CancellationTokenSource _stderrStop=new();private readonly Task _stderr;
 public IClientTransport Transport {get;}
 private McpLocalProcess(Process process){_process=process;Transport=new StreamClientTransport(process.StandardInput.BaseStream,process.StandardOutput.BaseStream);_stderr=DrainErrors();}
 public static McpLocalProcess Start(McpServerDefinition definition,IDictionary<string,string?> environment){var info=new ProcessStartInfo(definition.Address){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=definition.WorkingDirectory??Environment.CurrentDirectory};foreach(var argument in definition.Arguments)info.ArgumentList.Add(argument);info.Environment.Clear();foreach(var pair in environment)info.Environment[pair.Key]=pair.Value;var process=new Process{StartInfo=info};try{if(!process.Start())throw new IOException("Could not start the selected MCP executable.");return new(process);}catch{process.Dispose();throw;}}
 private async Task DrainErrors(){var buffer=new char[2048];try{while(await _process.StandardError.ReadAsync(buffer,_stderrStop.Token)>0){ }}catch(OperationCanceledException){}catch(ObjectDisposedException){}catch(IOException){}}
 public async ValueTask DisposeAsync(){try{try{_process.StandardInput.Close();}catch(IOException){}catch(ObjectDisposedException){}using var graceful=new CancellationTokenSource(TimeSpan.FromSeconds(2));try{await _process.WaitForExitAsync(graceful.Token);}catch(OperationCanceledException){if(!_process.HasExited)_process.Kill(entireProcessTree:true);using var exit=new CancellationTokenSource(TimeSpan.FromSeconds(3));await _process.WaitForExitAsync(exit.Token);}}finally{_stderrStop.Cancel();try{await _stderr.WaitAsync(TimeSpan.FromSeconds(1));}catch(TimeoutException){}finally{_process.Dispose();_stderrStop.Dispose();}}}
}

using ModelContextProtocol.Authentication;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace ProjectDesk;
// One IPv4 loopback callback for one browser sign-in; no desktop HTTP API is exposed.
public sealed class McpBrowserLogin : IDisposable {
 private readonly TcpListener _listener=new(IPAddress.Loopback,0);public Uri RedirectUri {get;}
 public McpBrowserLogin(){_listener.Start();RedirectUri=new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/callback");}
 public async Task<AuthorizationResult> Authorize(AuthorizationCallbackContext context,CancellationToken token){
  if(context.AuthorizationUri.Scheme!=Uri.UriSchemeHttps)throw new InvalidOperationException("Browser sign-in requires an HTTPS authorization server.");
  Process.Start(new ProcessStartInfo(context.AuthorizationUri.AbsoluteUri){UseShellExecute=true});
  while(true){using var client=await _listener.AcceptTcpClientAsync(token);await using var stream=client.GetStream();using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(10));var bytes=new List<byte>();var one=new byte[1];while(bytes.Count<8192&&await stream.ReadAsync(one,timeout.Token)>0){bytes.Add(one[0]);if(bytes.Count>=4&&bytes[^4]==13&&bytes[^3]==10&&bytes[^2]==13&&bytes[^1]==10)break;}
   string first=Encoding.ASCII.GetString(bytes.ToArray()).Split("\r\n")[0];var parts=first.Split(' ');bool valid=parts.Length==3&&parts[0]=="GET"&&Uri.TryCreate(RedirectUri,parts[1],out var callback)&&callback.AbsolutePath=="/callback"&&callback.Host=="127.0.0.1"&&callback.Port==RedirectUri.Port;
   string body=valid?"Sign-in received. You can return to Planora.":"Not found.";byte[] reply=Encoding.UTF8.GetBytes($"HTTP/1.1 {(valid?"200 OK":"404 Not Found")}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}");await stream.WriteAsync(reply,timeout.Token);if(!valid)continue;
   var values=ParseQuery(new Uri(RedirectUri,parts[1]).Query);if(values.TryGetValue("error",out _))throw new InvalidOperationException("Browser sign-in was declined.");if(!values.TryGetValue("code",out var code)||!values.TryGetValue("state",out var state))throw new InvalidOperationException("Sign-in callback did not include code and state.");return new AuthorizationResult{Code=code,State=state,Iss=values.GetValueOrDefault("iss")};
  }
 }
 public static Dictionary<string,string> ParseQuery(string query){var values=new Dictionary<string,string>();foreach(var part in query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries)){var pair=part.Split('=',2);string key=Uri.UnescapeDataString(pair[0].Replace('+',' '));if(!values.TryAdd(key,Uri.UnescapeDataString((pair.Length==2?pair[1]:"").Replace('+',' '))))throw new ArgumentException("Duplicate sign-in callback parameter.");}return values;}
 public void Dispose()=>_listener.Stop();
}

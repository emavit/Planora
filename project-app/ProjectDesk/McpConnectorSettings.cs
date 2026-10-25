using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ProjectDesk;

public enum McpAuthentication { None, BearerToken, BrowserOAuth }
public sealed record McpServerDefinition(string Id,string Name,McpConnectorTransport Transport,string Address,
    string[] Arguments,string? WorkingDirectory=null,McpAuthentication Authentication=McpAuthentication.None,
    string? ClientId=null,string[]? Scopes=null,string[]? SecretEnvironmentNames=null,string? ProtocolVersion=null) {
 public McpConnectorProfile Profile=>new(Id,Name,Transport,Address,true);
 // macOS/Linux counterpart of the .cmd/.bat rule: "#!/usr/bin/env node" launchers (npx and similar) depend on the
 // caller's PATH, which is minimal for apps started from Finder. Scripts naming an absolute interpreter stay allowed.
 private static bool FindsInterpreterOnPath(string path){try{using var stream=File.OpenRead(path);Span<byte> head=stackalloc byte[256];int read=stream.Read(head);string first=Encoding.ASCII.GetString(head[..read]).Split('\n')[0];return Regex.IsMatch(first,@"^#!\s*/usr/bin/env\s");}catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}}
 public void Validate(bool requireExecutable=true){
  Profile.Validate();if(!Guid.TryParse(Id,out _)||Name.Length>120)throw new ArgumentException("Invalid connector identity/name.");
  if(Arguments==null||Arguments.Length>100||Arguments.Any(a=>a==null||a.Length>4096))throw new ArgumentException("Use at most 100 individual arguments, each under 4 KiB.");
  if(Transport==McpConnectorTransport.LocalStdio){if(requireExecutable&&!File.Exists(Address))throw new ArgumentException("Choose an existing executable file.");if(Path.GetExtension(Address).ToLowerInvariant() is ".cmd" or ".bat")throw new ArgumentException("Choose the runtime executable (node.exe/python.exe), and pass the server script as an argument.");if(!OperatingSystem.IsWindows()&&requireExecutable&&FindsInterpreterOnPath(Address))throw new ArgumentException("Choose the runtime executable (for example /opt/homebrew/bin/node or /usr/bin/python3) and pass the server script as an argument. Scripts that look up their interpreter on PATH may not start when Planora is opened from Finder.");if(WorkingDirectory!=null&&(!Path.IsPathFullyQualified(WorkingDirectory)||(requireExecutable&&!Directory.Exists(WorkingDirectory))))throw new ArgumentException("Choose an existing absolute working directory.");}
  if(!Enum.IsDefined(Authentication)||Transport==McpConnectorTransport.LocalStdio&&Authentication!=McpAuthentication.None)throw new ArgumentException("HTTP authentication is only available for remote servers.");
  if(ProtocolVersion is not (null or "2025-11-25" or "2025-06-18" or "2026-07-28"))throw new ArgumentException("Choose Auto or a supported MCP version.");
  if(SecretEnvironmentNames?.Any(n=>!Regex.IsMatch(n,@"^[A-Za-z_][A-Za-z0-9_]{0,99}$"))==true)throw new ArgumentException("Use valid environment variable names.");
 }
 public string Fingerprint()=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this)));
 public override string ToString()=>Name;
}
public sealed class McpSettingsStore {
 public string Path {get;} public List<McpServerDefinition> Servers {get;}=[];
 public McpSettingsStore(string path){Path=path;if(File.Exists(path)){var items=JsonSerializer.Deserialize<List<McpServerDefinition>>(File.ReadAllText(path),new JsonSerializerOptions{MaxDepth=16})??[];foreach(var item in items){item.Validate(false);if(Servers.Any(s=>s.Id==item.Id))throw new InvalidDataException("Duplicate connector identity.");Servers.Add(item);}}}
 public void Save(){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path))!);string temp=Path+"."+Guid.NewGuid()+".tmp";try{File.WriteAllText(temp,JsonSerializer.Serialize(Servers,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,Path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
public interface IMcpSecrets {string? Read(string id,string name);void Write(string id,string name,string value);void Remove(string id,string name);}
// Tokens/env values never enter connector JSON, MPP files, logs or view preferences.
// Windows Credential Manager on Windows, the Keychain on macOS; other platforms keep secrets for this process only.
public sealed class McpCredentialStore : IMcpSecrets {
 private readonly Dictionary<string,string> _memory=[];
 public static bool Persistent=>OperatingSystem.IsWindows()||OperatingSystem.IsMacOS();
 private static string Target(string id,string name){if(!Guid.TryParse(id,out _)||!Regex.IsMatch(name,@"^[A-Za-z_][A-Za-z0-9_]{0,110}$"))throw new ArgumentException("Invalid credential key.");return "ProjectDesk/MCP/"+id+"/"+name;}
 public string? Read(string id,string name){string key=Target(id,name);if(OperatingSystem.IsMacOS())return MacKeychain.Read(key);if(!OperatingSystem.IsWindows())return _memory.GetValueOrDefault(key);if(!CredRead(key,1,0,out var ptr)){if(Marshal.GetLastWin32Error()==1168)return null;throw new IOException("Could not read connector credential.");}try{var c=Marshal.PtrToStructure<Credential>(ptr);return Marshal.PtrToStringUni(c.Blob,(int)c.Size/2);}finally{CredFree(ptr);}}
 public void Write(string id,string name,string value){string key=Target(id,name);if(Encoding.Unicode.GetByteCount(value)>2560)throw new ArgumentException("Credential exceeds the 2,560-byte limit shared by all platforms.");if(OperatingSystem.IsMacOS()){MacKeychain.Write(key,value);return;}if(!OperatingSystem.IsWindows()){_memory[key]=value;return;}var blob=Marshal.StringToCoTaskMemUni(value);try{var c=new Credential{Type=1,Target=key,Size=(uint)Encoding.Unicode.GetByteCount(value),Blob=blob,Persist=2,UserName="ProjectDesk"};if(!CredWrite(ref c,0))throw new IOException("Could not store connector credential in Windows Credential Manager.");}finally{Marshal.ZeroFreeCoTaskMemUnicode(blob);}}
 public void Remove(string id,string name){string key=Target(id,name);if(OperatingSystem.IsMacOS()){MacKeychain.Remove(key);return;}if(!OperatingSystem.IsWindows()){_memory.Remove(key);return;}if(!CredDelete(key,1,0)&&Marshal.GetLastWin32Error()!=1168)throw new IOException("Could not remove connector credential.");}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct Credential {public uint Flags,Type;public string? Target,Comment;public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;public uint Size;public IntPtr Blob;public uint Persist,AttributeCount;public IntPtr Attributes;public string? Alias,UserName;}
 [DllImport("advapi32.dll",EntryPoint="CredReadW",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool CredRead(string target,uint type,uint flags,out IntPtr result);
 [DllImport("advapi32.dll",EntryPoint="CredWriteW",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool CredWrite(ref Credential credential,uint flags);
 [DllImport("advapi32.dll",EntryPoint="CredDeleteW",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool CredDelete(string target,uint type,uint flags);
 [DllImport("advapi32.dll")]private static extern void CredFree(IntPtr pointer);
}

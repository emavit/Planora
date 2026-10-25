using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
namespace ProjectDesk;

// Registers Planora as an MCP server in the user's own Claude Code ("claude mcp add"). Claude Code keeps the Claude
// subscription sign-in; Planora never sees it and only answers the tools in PlanoraMcpTools.
internal static class ClaudeCodeSetup {
 public const string ServerName="planora";
 public static string ExecutableName=>OperatingSystem.IsWindows()?"claude.exe":"claude";
 public static string? FindCli(IEnumerable<string>? folders=null){foreach(var folder in folders??SearchFolders()){string file=Path.Combine(folder,ExecutableName);if(File.Exists(file))return Path.GetFullPath(file);}return null;}
 // Apps started from Finder or the Start menu get a minimal PATH, so also look where the Claude Code installers put claude.
 private static IEnumerable<string> SearchFolders(){string home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);return (Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator,StringSplitOptions.RemoveEmptyEntries).Concat([Path.Combine(home,".local","bin"),Path.Combine(home,".claude","local"),"/opt/homebrew/bin","/usr/local/bin"]).Distinct();}
 public static string[] ServerCommand(){var self=Program.SelfCommand("--mcp-connect");return [self.FileName,..self.Arguments];}
 public static string[] AddArguments(string[] command)=>["mcp","add","--scope","user",ServerName,"--",..command];
 public static string ManualCommand(string[] command)=>"claude "+string.Join(" ",AddArguments(command).Select(Quote));
 private static string Quote(string value)=>value.Length>0&&value.All(c=>char.IsAsciiLetterOrDigit(c)||"-_./:=@\\".Contains(c))?value:OperatingSystem.IsWindows()?"\""+value.Replace("\"","\\\"")+"\"":"'"+value.Replace("'","'\\''")+"'";
 // macOS runs a downloaded app from a temporary translocated copy until it is moved; that path disappears later.
 public static string? LocationProblem(string executable)=>executable.Contains("/AppTranslocation/",StringComparison.Ordinal)?"macOS is running Planora from a temporary copy. Move Planora.app to Applications, open it from there and connect again.":null;
 public static async Task Register(string cli,CancellationToken token){
  var command=ServerCommand();if(LocationProblem(command[0]) is string problem)throw new InvalidOperationException(problem);
  await Run(cli,["mcp","remove","--scope","user",ServerName],token);
  var added=await Run(cli,AddArguments(command),token);if(added.Code!=0)throw new InvalidOperationException("Claude Code did not add Planora: "+Summary(added));
 }
 public static async Task Unregister(string cli,CancellationToken token){var removed=await Run(cli,["mcp","remove","--scope","user",ServerName],token);if(removed.Code!=0)throw new InvalidOperationException("Claude Code did not remove Planora: "+Summary(removed));}
 public static async Task<bool> IsRegistered(string cli,CancellationToken token)=>(await Run(cli,["mcp","get",ServerName],token)).Code==0;
 private static string Summary((int Code,string Output,string Error) result)=>(result.Error+"\n"+result.Output).Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).FirstOrDefault()??"exit code "+result.Code;
 private static async Task<(int Code,string Output,string Error)> Run(string cli,string[] args,CancellationToken token){
  if(!Path.IsPathFullyQualified(cli)||!File.Exists(cli))throw new ArgumentException("Choose the claude executable installed by Claude Code.");
  var info=new ProcessStartInfo(cli){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
  foreach(var arg in args)info.ArgumentList.Add(arg);
  using var process=Process.Start(info)??throw new IOException("Could not start Claude Code.");process.StandardInput.Close();
  try{var output=process.StandardOutput.ReadToEndAsync(token);var error=process.StandardError.ReadToEndAsync(token);await process.WaitForExitAsync(token);return(process.ExitCode,await output,await error);}
  catch(OperationCanceledException){try{process.Kill(true);}catch(InvalidOperationException){}throw;}
 }
}

public sealed class ClaudeCodeWindow : Window {
 private CancellationTokenSource? _operation;
 // checkRegistration=false keeps validation from running the installed Claude Code.
 internal ClaudeCodeWindow(PlannerWindow planner,bool checkRegistration=true){Title="Claude Code";Width=660;Height=600;MinWidth=520;MinHeight=460;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var root=new DockPanel{Margin=new Thickness(22)};var body=new StackPanel{Spacing=14,Margin=new Thickness(0,0,16,0)};
  body.Children.Add(new TextBlock{Text="Claude Code",FontSize=24,FontWeight=FontWeight.SemiBold});
  body.Children.Add(new TextBlock{Text="Use your Claude subscription through Claude Code to work on the project open in Planora. Claude Code reads the project and proposes changes; Planora shows each change for your approval and applies it as one Undo step. Your Claude sign-in stays in Claude Code.",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary});
  var allow=new CheckBox{Name="ClaudeCodeAllow",Content="Allow Claude Code to read this project and propose changes",IsChecked=planner.ClaudeCodeControlEnabled};body.Children.Add(allow);
  var listening=new TextBlock{TextWrapping=TextWrapping.Wrap,Text=planner.ClaudeCodeStatus};body.Children.Add(listening);
  var cli=new TextBox{Name="ClaudeCodeExecutable",Text=ClaudeCodeSetup.FindCli(),Watermark="Absolute path to "+ClaudeCodeSetup.ExecutableName};
  var group=new StackPanel{Spacing=5};group.Children.Add(new TextBlock{Text="Claude Code executable",FontWeight=FontWeight.SemiBold});group.Children.Add(cli);Avalonia.Automation.AutomationProperties.SetName(cli,"Claude Code executable");body.Children.Add(group);
  var connect=new Button{Content="Connect Claude Code"};connect.Classes.Add("primary");var disconnect=new Button{Content="Disconnect"};
  body.Children.Add(new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,Children={connect,disconnect}});
  var status=new TextBlock{Name="ClaudeCodeResult",TextWrapping=TextWrapping.Wrap};body.Children.Add(status);
  string manual=ClaudeCodeSetup.ManualCommand(ClaudeCodeSetup.ServerCommand());
  body.Children.Add(new TextBlock{Text="Claude Code not found, or prefer Terminal? Run:",FontWeight=FontWeight.SemiBold});
  body.Children.Add(new TextBox{Text=manual,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,FontFamily=new FontFamily("Menlo, Consolas, monospace"),FontSize=11});
  var copy=new Button{Content="Copy Command"};body.Children.Add(copy);
  body.Children.Add(new TextBlock{Text="Then start a new Claude Code session and ask, for example: “List the tasks in my Planora project” or “Add a testing phase after design”. Planora starts automatically when Claude Code needs it.",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary});
  if(ClaudeCodeSetup.LocationProblem(Environment.ProcessPath??"") is string problem){status.Text=problem;connect.IsEnabled=false;}
  var close=new Button{Content="Close",HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Bottom);root.Children.Add(close);
  root.Children.Add(new ScrollViewer{Content=body});Content=root;UiIcons.ApplyTo(this);
  allow.IsCheckedChanged+=async(_,_)=>{await planner.SetClaudeCodeControl(allow.IsChecked==true);listening.Text=planner.ClaudeCodeStatus;};
  copy.Click+=async(_,_)=>{if(Clipboard!=null){await Clipboard.SetTextAsync(manual);status.Text="Command copied. Paste it into Terminal.";}};
  async Task Do(string working,Func<string,CancellationToken,Task<string>> action){
   if(_operation!=null)return;using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(60));_operation=stop;connect.IsEnabled=disconnect.IsEnabled=false;status.Text=working;
   try{status.Text=await action(cli.Text?.Trim()??"",stop.Token);}catch(OperationCanceledException){status.Text="Claude Code did not respond in time.";}catch(Exception e){status.Text=e.Message;}finally{_operation=null;connect.IsEnabled=disconnect.IsEnabled=true;}
  }
  connect.Click+=async(_,_)=>await Do("Adding Planora to Claude Code…",async(path,token)=>{await ClaudeCodeSetup.Register(path,token);if(allow.IsChecked!=true)allow.IsChecked=true;return "Connected. Start a new Claude Code session to use Planora; you approve each change here.";});
  disconnect.Click+=async(_,_)=>await Do("Removing Planora from Claude Code…",async(path,token)=>{await ClaudeCodeSetup.Unregister(path,token);return "Planora was removed from Claude Code.";});
  Opened+=async(_,_)=>{if(!checkRegistration)return;if(string.IsNullOrWhiteSpace(cli.Text)){status.Text="Claude Code was not found. Install it from claude.com/claude-code, or enter the path of the claude executable.";return;}await Do("Checking Claude Code…",async(path,token)=>await ClaudeCodeSetup.IsRegistered(path,token)?"Planora is already added to Claude Code.":"Planora is not yet added to Claude Code. Choose Connect Claude Code.");};
  Closing+=(_,_)=>_operation?.Cancel();
 }
}

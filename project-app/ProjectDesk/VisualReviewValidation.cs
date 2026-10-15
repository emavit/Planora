using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Security.Cryptography;
using System.Text.Json;
namespace ProjectDesk;

public static class VisualReviewValidation {
 public static int Run(string file,string output){Directory.CreateDirectory(output);byte[] hash=SHA256.HashData(File.ReadAllBytes(file));AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();using var session=new ProjectSession();session.Open(file);var window=new PlannerWindow(session,Path.Combine(output,"views.json"));window.Show();int checks=0;void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;}
  foreach(var mode in new[]{"Light","Dark"}){window.SetThemeMode(mode);foreach(var brush in new[]{DesignTokens.Text,DesignTokens.Secondary,DesignTokens.Accent,DesignTokens.Link})Check(Contrast(brush.Color,DesignTokens.Surface.Color)>=4.5,$"Insufficient {mode} text contrast: {brush.Color}");Check(Contrast(Colors.White,DesignTokens.AccentFill.Color)>=4.5,"Primary button label contrast");
   for(int page=0;page<13;page++){window.SelectPreviewTab(page);Pump();using var frame=window.CaptureRenderedFrame()??throw new Exception("Missing view frame");frame.Save(Path.Combine(output,$"{mode}-view-{page:00}.png"));Check(window.IsVisible,$"View {page} unavailable");}
   Check(window.CalendarView.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="2"),"Calendar shows day numbers without full-date expansion");window.SelectPreviewTab(1);window.Width=1050;window.Height=760;Pump();using(var frame=window.CaptureRenderedFrame()??throw new Exception("Missing compact frame"))frame.Save(Path.Combine(output,mode+"-compact.png"));Check(window.Sheet.Grid.Bounds.Width>180&&window.Gantt.Bounds.Width>150,"Compact task table and Gantt remain usable");window.Width=1550;window.Height=1000;
  }
  Check(!session.HasEdits,"Visual review changed project data");Check(hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(file))),"Original source file changed");window.CloseForValidation();File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(new{checks,views=26,compactViews=2,sourceUnchanged=true},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS: {checks} visual checks; 26 view captures and 2 compact captures.");return 0;
 }
 private static double Contrast(Color a,Color b){double L(Color c){double V(byte v){double n=v/255d;return n<=.04045?n/12.92:Math.Pow((n+.055)/1.055,2.4);}return .2126*V(c.R)+.7152*V(c.G)+.0722*V(c.B);}double x=L(a),y=L(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
 private static void Pump(){Dispatcher.UIThread.RunJobs();AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);Dispatcher.UIThread.RunJobs();}
}

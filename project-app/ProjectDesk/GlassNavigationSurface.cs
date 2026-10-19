using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace ProjectDesk;

// Windows approximation of the Liquid Glass navigation layer; native AppKit material is a later Mac gate.
public sealed class GlassNavigationSurface : UserControl {
 private readonly ExperimentalAcrylicBorder _acrylic;
 private readonly Border _frame;
 private readonly Grid _layers=new();
 public bool IsReduced {get;private set;}
 public GlassNavigationSurface(Control content){
  _acrylic=new(){CornerRadius=new CornerRadius(18)};
  _frame=new(){CornerRadius=new CornerRadius(18),BorderThickness=new Thickness(1),Padding=new Thickness(6,3),Child=content};
  _layers.Children.Add(_acrylic);_layers.Children.Add(_frame);Content=_layers;Margin=new Thickness(12,5,12,5);
 }
 public void SetAppearance(bool dark,bool reduce){
  IsReduced=reduce;_acrylic.IsVisible=!reduce;
  var tint=Color.Parse(dark?"#242426":"#F7F7F9");
  _acrylic.Material=new ExperimentalAcrylicMaterial{BackgroundSource=AcrylicBackgroundSource.Digger,TintColor=tint,TintOpacity=.88,MaterialOpacity=.72,FallbackColor=tint};
  _frame.Background=reduce?new SolidColorBrush(tint):new LinearGradientBrush{StartPoint=new RelativePoint(0,0,RelativeUnit.Relative),EndPoint=new RelativePoint(0,1,RelativeUnit.Relative),GradientStops={new GradientStop(Color.Parse(dark?"#E629292C":"#EBFFFFFF"),0),new GradientStop(Color.Parse(dark?"#EB222224":"#EBF5F5F7"),1)}};
  _frame.BorderBrush=new SolidColorBrush(Color.Parse(dark?"#454549":"#E2E2E7"));
  _frame.BoxShadow=reduce?default:BoxShadows.Parse(dark?"0 3 12 0 #50000000":"0 3 12 0 #160D2545");
 }
 public static bool SystemRequestsSolid(Window window){
  if(window.PlatformSettings?.GetColorValues().ContrastPreference.ToString() is "High" or "More")return true;
  if(OperatingSystem.IsWindows())try{return Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","EnableTransparency",1) is int enabled&&enabled==0;}catch{return true;}
  return false;
 }
}

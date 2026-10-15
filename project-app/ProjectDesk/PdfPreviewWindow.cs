using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
namespace ProjectDesk;
public sealed record PdfPreviewData(int Pages,List<byte[]> Images);
public static class PdfPreview {
 public static PdfPreviewData Create(PdfSnapshot data,PdfOptions options){string folder=Path.Combine(Path.GetTempPath(),"ProjectDesk");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"preview-"+Guid.NewGuid()+".pdf");var images=new List<byte[]>();try{var result=ProjectPdfExporter.Export(path,data,options,(_,bytes)=>images.Add(bytes));return new(result.Pages,images);}finally{if(File.Exists(path))File.Delete(path);}}
}
public sealed class PdfPreviewWindow : Window {
 private readonly List<Bitmap> _images;private readonly Image _page=new(){Stretch=Stretch.Uniform};private readonly TextBlock _count=new(){VerticalAlignment=VerticalAlignment.Center};private int _index;private double _zoom=1;
 public int PageCount {get;}
 public PdfPreviewWindow(PdfPreviewData data){
  PageCount=data.Pages;_images=data.Images.Select(bytes=>new Bitmap(new MemoryStream(bytes))).ToList();Title="Print Preview · PDF";Width=1120;Height=820;MinWidth=700;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=DesignTokens.Background;
  var root=new DockPanel{Margin=new Thickness(14)};var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,Margin=new Thickness(0,0,0,12)};
  Button Command(string title,Action action){var b=new Button{Content=title};b.Click+=(_,_)=>action();return b;}
  actions.Children.Add(Command("Previous",()=>{_index=Math.Max(0,_index-1);Refresh();}));actions.Children.Add(_count);actions.Children.Add(Command("Next",()=>{_index=Math.Min(_images.Count-1,_index+1);Refresh();}));actions.Children.Add(Command("−",()=>{_zoom=Math.Max(.4,_zoom/1.25);Refresh();}));actions.Children.Add(Command("Fit",()=>{_zoom=1;Refresh();}));actions.Children.Add(Command("+",()=>{_zoom=Math.Min(3,_zoom*1.25);Refresh();}));actions.Children.Add(Command("Close Preview",Close));DockPanel.SetDock(actions,Dock.Top);root.Children.Add(actions);root.Children.Add(new ScrollViewer{Content=new Border{Background=Brushes.White,Padding=new Thickness(8),Child=_page},HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto});Content=root;Closed+=(_,_)=>{foreach(var image in _images)image.Dispose();};Refresh();
 }
 private void Refresh(){if(_images.Count==0)return;_page.Source=_images[_index];_page.Width=1000*_zoom;_count.Text=$"Page {_index+1} of {PageCount}"+(PageCount>_images.Count?$" · first {_images.Count} pages previewed":"");}
}

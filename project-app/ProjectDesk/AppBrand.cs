using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;
namespace ProjectDesk;

public static class AppBrand {
 public const string Name="Planora";
 private static readonly Lazy<byte[]> _png=new(()=>RenderIcon(256));
 public static byte[] IconPng=>_png.Value;
 public static WindowIcon WindowIcon()=>new(new MemoryStream(IconPng));
 public static void Install(){Control.LoadedEvent.AddClassHandler<Window>((window,_)=>{window.Icon??=WindowIcon();if(window.Title?.Contains(Name,StringComparison.Ordinal)!=true)window.Title+=" · "+Name;});}
 public static byte[] RenderIcon(int size){using var surface=SKSurface.Create(new SKImageInfo(size,size,SKColorType.Rgba8888,SKAlphaType.Premul));var canvas=surface.Canvas;canvas.Clear(SKColors.Transparent);canvas.Scale(size/256f);using var fill=new SKPaint{IsAntialias=true,Shader=SKShader.CreateLinearGradient(new SKPoint(0,0),new SKPoint(256,256),[SKColor.Parse("#1759AD"),SKColor.Parse("#178B8A")],null,SKShaderTileMode.Clamp)};canvas.DrawRoundRect(new SKRect(4,4,252,252),56,56,fill);using var ink=new SKPaint{IsAntialias=true,Color=SKColors.White};canvas.DrawRoundRect(new SKRect(57,51,79,205),11,11,ink);canvas.DrawRoundRect(new SKRect(79,51,194,77),13,13,ink);canvas.DrawRoundRect(new SKRect(79,111,169,137),13,13,ink);using var lighter=new SKPaint{IsAntialias=true,Color=SKColor.Parse("#A5ECE8")};canvas.DrawRoundRect(new SKRect(107,170,205,196),13,13,lighter);using var image=surface.Snapshot();using var encoded=image.Encode(SKEncodedImageFormat.Png,100);return encoded.ToArray();}
 public static int WriteAssets(string folder){Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"Planora.png"),IconPng);int[] sizes=[16,24,32,48,64,128,256];var blocks=sizes.Select(RenderIcon).ToArray();using var stream=File.Create(Path.Combine(folder,"Planora.ico"));using var writer=new BinaryWriter(stream);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(blocks[i].Length);writer.Write(offset);offset+=blocks[i].Length;}foreach(var block in blocks)writer.Write(block);return 0;}
}
public sealed class BrandMark : Control {
 public BrandMark(){Width=Height=30;IsHitTestVisible=false;}
 public override void Render(DrawingContext context){base.Render(context);using var scale=context.PushTransform(Matrix.CreateScale(Bounds.Width/32,Bounds.Height/32));var blue=new SolidColorBrush(Color.Parse("#1759AD"));context.DrawRectangle(blue,null,new Rect(0,0,32,32),8,8);context.DrawRectangle(Brushes.White,null,new Rect(7,6,3,20),1.5,1.5);context.DrawRectangle(Brushes.White,null,new Rect(10,6,15,3.5),1.75,1.75);context.DrawRectangle(Brushes.White,null,new Rect(10,14,11,3.5),1.75,1.75);context.DrawRectangle(new SolidColorBrush(Color.Parse("#A5ECE8")),null,new Rect(14,22,12,3.5),1.75,1.75);}
}

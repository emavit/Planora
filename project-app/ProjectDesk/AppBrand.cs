using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;
namespace ProjectDesk;

public static class AppBrand {
 public const string Name="Planora";
 public const string Tagline="Clarity, from plan to progress.";
 private static readonly Lazy<byte[]> _png=new(()=>RenderIcon(256));
 public static byte[] IconPng=>_png.Value;
 public static WindowIcon WindowIcon()=>new(new MemoryStream(IconPng));
 public static void Install(){Control.LoadedEvent.AddClassHandler<Window>((window,_)=>{window.Icon??=WindowIcon();if(window.Title?.Contains(Name,StringComparison.Ordinal)!=true)window.Title+=" · "+Name;});}
 public static byte[] RenderIcon(int size){
  using var surface=SKSurface.Create(new SKImageInfo(size,size,SKColorType.Rgba8888,SKAlphaType.Premul));
  var canvas=surface.Canvas;canvas.Clear(SKColors.Transparent);canvas.Scale(size/256f);
  using var fill=new SKPaint{IsAntialias=true,Shader=SKShader.CreateLinearGradient(new SKPoint(0,0),new SKPoint(256,256),[SKColor.Parse("#272C68"),SKColor.Parse("#5264D9")],null,SKShaderTileMode.Clamp)};
  canvas.DrawRoundRect(new SKRect(4,4,252,252),58,58,fill);
  using var ink=new SKPaint{IsAntialias=true,Color=SKColors.White};
  canvas.DrawRoundRect(new SKRect(48,62,160,90),14,14,ink);
  canvas.DrawRoundRect(new SKRect(80,114,208,142),14,14,ink);
  using var aqua=new SKPaint{IsAntialias=true,Color=SKColor.Parse("#80E0D2")};
  canvas.DrawRoundRect(new SKRect(112,166,180,194),14,14,aqua);
  canvas.DrawCircle(62,180,14,aqua);
  using var image=surface.Snapshot();using var encoded=image.Encode(SKEncodedImageFormat.Png,100);return encoded.ToArray();
 }
 public static int WriteAssets(string folder){Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"Planora.png"),IconPng);int[] sizes=[16,24,32,48,64,128,256];var blocks=sizes.Select(RenderIcon).ToArray();using var stream=File.Create(Path.Combine(folder,"Planora.ico"));using var writer=new BinaryWriter(stream);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(blocks[i].Length);writer.Write(offset);offset+=blocks[i].Length;}foreach(var block in blocks)writer.Write(block);return 0;}
}
public sealed class BrandMark : Control {
 private static readonly Lazy<Avalonia.Media.Imaging.Bitmap> Image=new(()=>new(new MemoryStream(AppBrand.IconPng)));
 public BrandMark(){Width=Height=30;IsHitTestVisible=false;}
 public override void Render(DrawingContext context){base.Render(context);context.DrawImage(Image.Value,new Rect(0,0,Bounds.Width,Bounds.Height));}
}

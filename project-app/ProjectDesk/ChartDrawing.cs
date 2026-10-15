using Avalonia;
using Avalonia.Media;
using System.Globalization;

namespace ProjectDesk;

public static class ChartDrawing {
    public static readonly IBrush Bars=new SolidColorBrush(Color.Parse("#74C4D2"));
    public static readonly IBrush Progress=new SolidColorBrush(Color.Parse("#1596AC"));
    public static readonly IBrush Critical=new SolidColorBrush(Color.Parse("#C7463E"));
    public static void Text(DrawingContext ctx,string text,double x,double y,double width=250,IBrush? brush=null,double size=11,bool bold=false) {
        var typeface=new Typeface(DesignTokens.Font,FontStyle.Normal,bold?FontWeight.SemiBold:FontWeight.Normal);
        ctx.DrawText(new FormattedText(text,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,typeface,size,brush??DesignTokens.Text){MaxTextWidth=Math.Max(1,width),MaxLineCount=1,Trimming=TextTrimming.CharacterEllipsis},new Point(x,y));
    }
    public static void Diamond(DrawingContext ctx,Point center,double radius,IBrush brush) {
        var shape=new StreamGeometry();using(var g=shape.Open()){g.BeginFigure(new Point(center.X,center.Y-radius),true);g.LineTo(new Point(center.X+radius,center.Y));g.LineTo(new Point(center.X,center.Y+radius));g.LineTo(new Point(center.X-radius,center.Y));g.EndFigure(true);}
        ctx.DrawGeometry(brush,null,shape);
    }
    public static void Arrow(DrawingContext ctx,IReadOnlyList<Point> points,int direction,IBrush brush) {
        for(int i=1;i<points.Count;i++)ctx.DrawLine(new Pen(brush,1),points[i-1],points[i]);
        var p=points[^1];var head=new StreamGeometry();using(var g=head.Open()){g.BeginFigure(p,true);g.LineTo(new Point(p.X-direction*5,p.Y-3));g.LineTo(new Point(p.X-direction*5,p.Y+3));g.EndFigure(true);}ctx.DrawGeometry(brush,null,head);
    }
    public static void DateHeader(DrawingContext ctx,DateAxis axis,double plotWidth,double height,bool weekends,bool officeHeader=false,IBrush? weekendBrush=null) {
        if(officeHeader)ctx.DrawRectangle(RibbonModule.Chrome,null,new Rect(0,0,plotWidth+24,36));
        double pxDay=plotWidth/axis.Days;
        int step=pxDay>=30?1:pxDay>=10?7:pxDay>=4?14:pxDay>=2?28:56;
        for(int i=0;i<=axis.Days;i++) {
            var day=axis.Start.AddDays(i);double x=axis.X(day,plotWidth);
            if(weekends&&pxDay>=3&&(day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))ctx.DrawRectangle(weekendBrush??DesignTokens.NonWorking,null,new Rect(x,36,Math.Max(1,pxDay),Math.Max(0,height-36)));
            if(i%step==0){ctx.DrawLine(new Pen(DesignTokens.Separator,0.7),new Point(x,36),new Point(x,height));Text(ctx,day.ToString("dd MMM yy",System.Globalization.CultureInfo.InvariantCulture),x+3,8,Math.Max(55,step*pxDay-4),officeHeader?RibbonModule.Ink:null);}
        }
        ctx.DrawLine(new Pen(DesignTokens.Separator),new Point(0,35),new Point(plotWidth+24,35));
    }
}

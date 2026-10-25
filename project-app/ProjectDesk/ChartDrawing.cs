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
    private static string? _labelFont;private static double _labelTextWidth;
    // Widest "dd MMM yy" header label in the current UI font, measured once per font.
    private static double DateLabelTextWidth(){var font=DesignTokens.Font;if(_labelFont==font.Name)return _labelTextWidth;var typeface=new Typeface(font);_labelTextWidth=Enumerable.Range(1,12).Max(m=>new FormattedText(new DateTime(2026,m,28).ToString("dd MMM yy",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,typeface,11,Brushes.Black).Width);_labelFont=font.Name;return _labelTextWidth;}
    // Days between date labels and the width each label may use, so labels never overlap or truncate at this scale.
    public static (int Step,double LabelWidth) DateLabels(double pxDay){
        double needed=Math.Ceiling(DateLabelTextWidth())+6;int step=0;
        foreach(int candidate in new[]{1,7,14,28,56,91,182,364})if(candidate*pxDay>=needed){step=candidate;break;}
        if(step==0)step=364*(int)Math.Ceiling(needed/(364*Math.Max(pxDay,1e-6)));
        return(step,step*pxDay-4);
    }
    public static void DateHeader(DrawingContext ctx,DateAxis axis,double plotWidth,double height,bool weekends,bool officeHeader=false,IBrush? weekendBrush=null) {
        if(officeHeader)ctx.DrawRectangle(RibbonModule.Chrome,null,new Rect(0,0,plotWidth+24,36));
        double pxDay=plotWidth/axis.Days;
        var (step,labelWidth)=DateLabels(pxDay);
        for(int i=0;i<=axis.Days;i++) {
            var day=axis.Start.AddDays(i);double x=axis.X(day,plotWidth);
            if(weekends&&pxDay>=3&&(day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))ctx.DrawRectangle(weekendBrush??DesignTokens.NonWorking,null,new Rect(x,36,Math.Max(1,pxDay),Math.Max(0,height-36)));
            if(i%step==0){ctx.DrawLine(new Pen(DesignTokens.Separator,0.7),new Point(x,36),new Point(x,height));Text(ctx,day.ToString("dd MMM yy",System.Globalization.CultureInfo.InvariantCulture),x+3,8,labelWidth,officeHeader?RibbonModule.Ink:null);}
        }
        ctx.DrawLine(new Pen(DesignTokens.Separator),new Point(0,35),new Point(plotWidth+24,35));
    }
}

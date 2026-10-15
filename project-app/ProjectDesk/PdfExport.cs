using SkiaSharp;
using OfficeIMO.Project;
using System.Globalization;
namespace ProjectDesk;

public enum PdfContent { TableAndGantt, GanttOnly, Timelines }
public enum PdfPaper { A4, A3, Letter }
public sealed record PdfOptions(PdfContent Content=PdfContent.TableAndGantt,PdfPaper Paper=PdfPaper.A4,bool Landscape=true,int RowsPerPage=24,int PagesWide=1,DateAxisView? Dates=null,bool AllTasks=false,bool CompactColumns=true,bool Legend=true,bool Dependencies=true,bool Baselines=true,int Baseline=0,double MarginMm=12.7,bool FitOnePageTall=false,string? Header=null,string? Footer=null);
public sealed record PdfColumn(string Title,float Width);
public sealed record PdfTask(int Uid,int Id,string Name,DateTime? Start,DateTime? Finish,bool Summary,bool Milestone,int Percent,bool Critical,string[] Cells,string? Color,DateTime? BaselineStart,DateTime? BaselineFinish);
public sealed record PdfLink(int From,int To,ProjectDependencyType Type);
public sealed record PdfLane(string Name,int[] Uids,DateAxisView View,TimelineStyle? Style=null,Dictionary<int,TimelineItemStyle>? ItemStyles=null);
public sealed record PdfSnapshot(string Name,PdfTask[] Tasks,PdfColumn[] Columns,PdfLink[] Links,PdfLane[] Lanes,DateTime Start,DateTime Finish,bool ColorByProgress=false,bool ProgressLabels=false,bool ShowCritical=true,Dictionary<string,string>? Palette=null);
public sealed record PdfResult(int Pages,int Tasks,string Path);

// Renders immutable project data, not virtualized screen rows. No printer, browser or network required.
public static class ProjectPdfExporter {
 private static readonly SKColor Ink=SKColor.Parse("#20252B"),Muted=SKColor.Parse("#66717D"),Grid=SKColor.Parse("#DDE3E9"),Blue=SKColor.Parse("#4472C4"),Teal=SKColor.Parse("#74C4D2"),Critical=SKColor.Parse("#C7463E");
 public static PdfSnapshot Capture(ProjectSession session,TaskSheetModule sheet,TimelineModule timeline,DateAxis axis,FormattingStore formatting,PdfOptions options,GanttCanvas? chart=null){
  var doc=session.Document??throw new InvalidOperationException("Open or create a project first.");
  var ids=sheet.Grid.ItemsSource!.OfType<TaskTableRow>().Where(r=>r.Uid>0).Select(r=>r.Uid).ToArray();
  var tasks=options.AllTasks||options.Content==PdfContent.Timelines?session.Tasks:ids.Select(id=>session.Tasks.Single(t=>t.Uid==id)).ToList();
  string[] keys=options.CompactColumns?["DisplayId","Name","Duration","Start","Finish"]:sheet.VisibleKeys.ToArray();
  var columns=keys.Select(key=>sheet.Catalogue.First(c=>c.Key==key)).ToArray();
  var pdfColumns=columns.Select(c=>new PdfColumn(c.Title,c.Key=="Name"?170:c.Key=="DisplayId"?30:70)).ToArray();
  var items=tasks.Select(t=>{var baseline=t.Baselines.FirstOrDefault(b=>(b.Number??0)==options.Baseline);return new PdfTask(t.Uid,t.DisplayId??0,t.Name??"",t.Start,t.Finish,t.IsSummary,t.IsMilestone==true||t.Duration?.Value==0||t.Start==t.Finish,t.PercentComplete??0,t.IsCritical==true,columns.Select(c=>c.Read(t,doc)??"").ToArray(),formatting.Document.Bars.GetValueOrDefault(t.Uid),baseline?.Start,baseline?.Finish);}).ToArray();
  return new(doc.Name??Path.GetFileNameWithoutExtension(session.SourcePath)??"Project",items,pdfColumns,doc.Dependencies.Where(d=>d.Predecessor!=null&&d.Successor!=null).Select(d=>new PdfLink(d.Predecessor!.Uid,d.Successor!.Uid,d.Type??ProjectDependencyType.FinishToStart)).ToArray(),timeline.Lanes.Select(l=>new PdfLane(l.Name,l.TaskUids.ToArray(),l.View,l.Style,l.ItemStyles?.ToDictionary(p=>p.Key,p=>p.Value))).ToArray(),axis.Start,axis.Finish,chart?.ColorByProgress??false,chart?.ShowProgressLabels??false,chart?.ShowCritical??true,formatting.Document.ChartColors.ToDictionary(p=>p.Key,p=>p.Value));
 }
 public static PdfResult Export(string path,PdfSnapshot data,PdfOptions options,Action<int,byte[]>? previewPage=null){
  Validate(options);string full=Path.GetFullPath(path);if(File.Exists(full))throw new IOException("Choose a new PDF filename; an existing file will not be overwritten.");
  Directory.CreateDirectory(Path.GetDirectoryName(full)!);string temp=full+"."+Guid.NewGuid()+".tmp";int pages=0;
  try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var pdf=SKDocument.CreatePdf(stream,new SKDocumentPdfMetadata{Title=data.Name,Creator=AppBrand.Name,Subject=options.Content.ToString()})){
    if(pdf==null)throw new IOException("PDF renderer could not be initialized.");
    using var normal=SKTypeface.FromFamilyName(OperatingSystem.IsMacOS()?"Helvetica Neue":"Calibri");using var bold=SKTypeface.FromFamilyName(OperatingSystem.IsMacOS()?"Helvetica Neue":"Calibri",SKFontStyle.Bold);
    SKColor Color(string? value,SKColor fallback)=>value!=null&&Avalonia.Media.Color.TryParse(value,out var c)?new SKColor(c.R,c.G,c.B,c.A):fallback;SKColor Palette(string key,SKColor fallback)=>Color(data.Palette?.GetValueOrDefault(key),fallback);
    var size=Size(options);float margin=(float)(options.MarginMm*72/25.4),bodyWidth=size.Width-2*margin,top=margin+67,bottom=size.Height-margin-24-(options.Legend?44:0);int rowsPerPage=options.FitOnePageTall?Math.Max(1,data.Tasks.Length):options.RowsPerPage;float rowHeight=(bottom-top)/rowsPerPage;
    SKPictureRecorder? recorder=null;SKNWayCanvas? mirror=null;SKCanvas BeginPage(){var canvas=pdf.BeginPage(size.Width,size.Height);if(previewPage==null||pages>=32)return canvas;recorder=new SKPictureRecorder();mirror=new SKNWayCanvas((int)size.Width,(int)size.Height);mirror.AddCanvas(canvas);mirror.AddCanvas(recorder.BeginRecording(new SKRect(0,0,size.Width,size.Height)));return mirror;}void EndPage(){if(recorder!=null){using var picture=recorder.EndRecording();using var image=SKImage.FromPicture(picture,new SKSizeI((int)(size.Width*1.5f),(int)(size.Height*1.5f)),SKMatrix.CreateScale(1.5f,1.5f));using var encoded=image.Encode(SKEncodedImageFormat.Png,100);previewPage!(pages,encoded.ToArray());mirror!.Dispose();recorder.Dispose();mirror=null;recorder=null;}pdf.EndPage();}
    void Text(SKCanvas canvas,string value,float x,float y,float width,float font=8,bool strong=false,SKColor? color=null){using var paint=new SKPaint{Typeface=strong?bold:normal,TextSize=font,Color=color??Ink,IsAntialias=true};var text=value.Replace('\r',' ').Replace('\n',' ');if(width<=0)return;if(paint.MeasureText(text)>width){int lo=0,hi=text.Length;while(lo<hi){int mid=(lo+hi+1)/2;if(paint.MeasureText(text[..mid]+"…")<=width)lo=mid;else hi=mid-1;}text=text[..lo]+"…";}canvas.DrawText(text,x,y,paint);}
    void Rect(SKCanvas c,float x,float y,float w,float h,SKColor color,float radius=0){using var p=new SKPaint{Color=color,IsAntialias=true};if(radius>0)c.DrawRoundRect(new SKRect(x,y,x+w,y+h),radius,radius,p);else c.DrawRect(x,y,w,h,p);}
    void Line(SKCanvas c,float x,float y,float x2,float y2,SKColor color,float stroke=.6f){using var p=new SKPaint{Color=color,StrokeWidth=stroke,IsAntialias=true};c.DrawLine(x,y,x2,y2,p);}
    void SummaryCap(SKCanvas c,float x,float y,bool right){using var path=new SKPath();path.MoveTo(x,y-3);path.LineTo(x+(right?-5:5),y-3);path.LineTo(x,y+5);path.Close();using var p=new SKPaint{Color=Palette("Summary",Ink),IsAntialias=true};c.DrawPath(path,p);}
    void Diamond(SKCanvas c,float x,float y,SKColor color){using var path=new SKPath();path.MoveTo(x,y-4);path.LineTo(x+4,y);path.LineTo(x,y+4);path.LineTo(x-4,y);path.Close();using var p=new SKPaint{Color=color,IsAntialias=true};c.DrawPath(path,p);}
    (DateTime Start,DateTime Finish) Range(DateAxisView? view){var axis=new DateAxis();axis.SetRange(data.Start.Date,data.Finish.AddTicks(-1).Date);if(view?.Start is DateTime start&&view.Finish is DateTime finish)axis.SetRange(start,finish);return(axis.Start,axis.Finish);}
    void Header(SKCanvas c,string label,DateTime a,DateTime b,int across){c.Clear(SKColors.White);Text(c,options.Header??data.Name,margin,margin+10,bodyWidth,11,true);Text(c,$"{label} · {a:dd MMM yyyy} – {b.AddTicks(-1):dd MMM yyyy}",margin,margin+24,bodyWidth,8,color:Muted);}
    void Footer(SKCanvas c,string kind){
      float y=size.Height-margin;Text(c,options.Footer??data.Name,margin,y,bodyWidth*.4f,8,color:Muted);Text(c,$"Page {pages}",size.Width/2-22,y,80,8,color:Muted);
      if(!options.Legend)return;
      var entries=data.ColorByProgress?new List<(string Label,SKColor Color,string Shape)>{("Not started",SKColor.Parse("#8594A8"),"bar"),("In progress",Blue,"bar"),("Completed",SKColor.Parse("#2F956B"),"bar")}:new List<(string,SKColor,string)>{("Task",Palette("Task",Teal),"bar"),("Progress",Palette("Progress",SKColor.Parse("#1596AC")),"bar"),("Critical task",Palette("Critical",Critical),"bar")};
      entries.Add(("Milestone",Palette("Milestone",Teal),"diamond"));entries.Add(("Summary",Palette("Summary",Ink),"summary"));if(options.Baselines)entries.Add(($"Baseline {options.Baseline}",Palette("Baseline",Muted),"line"));
      for(int i=0;i<entries.Count;i++){var (name,color,shape)=entries[i];float x=margin+(i%3)*bodyWidth/3,ly=y-39+(i/3)*16;if(shape=="diamond")Diamond(c,x+10,ly-3,color);else if(shape=="summary"){Rect(c,x,ly-6,20,4,color);SummaryCap(c,x,ly-3,false);SummaryCap(c,x+20,ly-3,true);}else if(shape=="line")Line(c,x,ly-3,x+20,ly-3,color,2);else Rect(c,x,ly-7,20,7,color,1);Text(c,name,x+25,ly,bodyWidth/3-30,8);}
    }
    void DateHeader(SKCanvas c,DateTime start,DateTime finish,float x,float width){
      double days=(finish-start).TotalDays,pxDay=width/days;float upper=top-32;
      Rect(c,x,upper,width,32,SKColor.Parse("#F3F5F7"));int step=pxDay>=15?7:pxDay>=3?28:pxDay>=.7?91:365;
      var anchor=step==7?start.Date.AddDays(-((7+(int)start.DayOfWeek-1)%7)):new DateTime(start.Year,start.Month,1);
      for(var date=anchor;date<finish;){var next=step==7?date.AddDays(7):step==28?date.AddMonths(1):step==91?date.AddMonths(3):date.AddYears(1);float left=Math.Max(x,x+(float)((date-start).TotalDays/days)*width),right=Math.Min(x+width,x+(float)((next-start).TotalDays/days)*width);Line(c,left,upper,left,top,Grid);Text(c,step==7?date.ToString("dd MMM yy",CultureInfo.InvariantCulture):step==28?date.ToString("MMM yyyy",CultureInfo.InvariantCulture):step==91?$"Q{(date.Month-1)/3+1} {date.Year}":date.Year.ToString(),left+3,upper+11,right-left-5,7);date=next;}
      if(pxDay>=15){for(int i=0;i<Math.Ceiling(days);i++){var day=start.AddDays(i);float xx=x+(float)(i/days)*width;Text(c,day.ToString("ddd",CultureInfo.InvariantCulture)[..1],xx+3,top-5,(float)pxDay-4,7);Line(c,xx,top-15,xx,bottom,Grid);if(day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)Rect(c,xx,top,(float)pxDay,bottom-top,Palette("Weekend",SKColor.Parse("#F5F6F8")));}}
      else{int tick=step==28?7:step==91?28:91;for(int i=0;i<days;i+=tick){var date=start.AddDays(i);float xx=x+(float)(i/days)*width;Text(c,date.ToString("dd MMM",CultureInfo.InvariantCulture),xx+3,top-5,Math.Max(1,(float)(tick/days)*width-5),7);Line(c,xx,top-15,xx,bottom,Grid);}}
      Line(c,x,top-16,x+width,top-16,Grid);Line(c,x,top,x+width,top,Grid);
    }
    float X(DateTime date,DateTime start,DateTime finish,float x,float width)=>x+(float)((date-start).TotalDays/(finish-start).TotalDays)*width;
    void Gantt(PdfTask[] tasks,DateTime rangeStart,DateTime rangeFinish,string label){
      int vertical=Math.Max(1,(int)Math.Ceiling(tasks.Length/(double)rowsPerPage));
      for(int down=0;down<vertical;down++)for(int across=0;across<options.PagesWide;across++){
       var start=rangeStart.AddDays((rangeFinish-rangeStart).TotalDays*across/options.PagesWide);var finish=rangeStart.AddDays((rangeFinish-rangeStart).TotalDays*(across+1)/options.PagesWide);
       var c=BeginPage();pages++;Header(c,label,start,finish,across);float tableWidth=options.Content==PdfContent.TableAndGantt?bodyWidth*.48f:0,chartX=margin+tableWidth,chartWidth=bodyWidth-tableWidth;
       DateHeader(c,start,finish,chartX,chartWidth);var rows=tasks.Skip(down*rowsPerPage).Take(rowsPerPage).ToArray();
       if(tableWidth>0){Rect(c,margin,top-32,tableWidth,32,SKColor.Parse("#F3F5F7"));float x=margin,total=data.Columns.Sum(t=>t.Width);foreach(var col in data.Columns){float width=tableWidth*col.Width/total;Text(c,col.Title,x+3,top-6,width-6,7,true);Line(c,x,top-32,x,bottom,Grid);x+=width;}}
       var rowMap=rows.Select((t,i)=>(t.Uid,i)).ToDictionary(t=>t.Uid,t=>t.i);
       if(options.Dependencies){var linkColor=Palette("Links",SKColor.Parse("#1596AC"));c.Save();c.ClipRect(new(chartX,top,chartX+chartWidth,bottom));foreach(var edge in data.Links){if(!rowMap.TryGetValue(edge.To,out int target)||!rowMap.TryGetValue(edge.From,out int source))continue;var a=rows[source];var b=rows[target];bool fromStart=edge.Type is ProjectDependencyType.StartToStart or ProjectDependencyType.StartToFinish,toFinish=edge.Type is ProjectDependencyType.FinishToFinish or ProjectDependencyType.StartToFinish;var from=fromStart?a.Start:a.Finish;var to=toFinish?b.Finish:b.Start;if(from==null||to==null)continue;float x0=X(from.Value,start,finish,chartX,chartWidth),x1=X(to.Value,start,finish,chartX,chartWidth),y0=top+(source+.5f)*rowHeight,y1=top+(target+.5f)*rowHeight,dir=toFinish?-1:1,turn=x1-dir*7,sourceDir=fromStart?-1:1;
        Line(c,x0,y0,x0+sourceDir*8,y0,linkColor);Line(c,x0+sourceDir*8,y0,x0+sourceDir*8,y1,linkColor);Line(c,x0+sourceDir*8,y1,turn,y1,linkColor);Line(c,turn,y1,x1,y1,linkColor);Line(c,x1,y1,x1-dir*4,y1-2,linkColor);Line(c,x1,y1,x1-dir*4,y1+2,linkColor);
       }c.Restore();}
       for(int i=0;i<rows.Length;i++){var t=rows[i];float y=top+i*rowHeight,mid=y+rowHeight/2;Line(c,margin,y+rowHeight,margin+bodyWidth,y+rowHeight,Grid);if(tableWidth>0){float x=margin,total=data.Columns.Sum(col=>col.Width);for(int k=0;k<data.Columns.Length;k++){float width=tableWidth*data.Columns[k].Width/total;Text(c,t.Cells[k],x+3,mid+3,width-6,Math.Min(9,rowHeight*.6f),t.Summary);x+=width;}}
        c.Save();c.ClipRect(new(chartX,top,chartX+chartWidth,bottom));if(t.Start is DateTime a&&t.Finish is DateTime b&&b>=start&&a<finish){float x=X(a,start,finish,chartX,chartWidth),right=X(b,start,finish,chartX,chartWidth);var color=Color(t.Color,data.ColorByProgress?(t.Percent>=100?SKColor.Parse("#2F956B"):t.Percent>0?Blue:SKColor.Parse("#8594A8")):t.Critical&&data.ShowCritical?Palette("Critical",Critical):Palette("Task",Teal));
         if(t.Milestone)Diamond(c,x,mid,Color(t.Color,Palette("Milestone",color)));else if(t.Summary){Rect(c,x,mid-3,Math.Max(2,right-x),4,Palette("Summary",Ink));SummaryCap(c,x,mid,false);SummaryCap(c,right,mid,true);}else{Rect(c,x,mid-4,Math.Max(2,right-x),8,color,2);Rect(c,x,mid-4,Math.Max(0,right-x)*Math.Clamp(t.Percent/100f,0,1),8,data.ColorByProgress?new SKColor((byte)(color.Red*.78f),(byte)(color.Green*.78f),(byte)(color.Blue*.78f),color.Alpha):Palette("Progress",SKColor.Parse("#1596AC")),2);if(data.ProgressLabels)Text(c,$"{Math.Clamp(t.Percent,0,100)}%",right+3,mid+3,32,7,true);}
         if(options.Content==PdfContent.GanttOnly)Text(c,$"{t.Id} · {t.Name}",Math.Max(chartX+3,x+5),mid-6,Math.Max(60,Math.Min(240,chartX+chartWidth-Math.Max(chartX+3,x+5))),Math.Min(8,rowHeight*.35f),t.Summary);
         foreach(var e in data.Links.Where(e=>options.Dependencies&&(e.From==t.Uid||e.To==t.Uid))){int other=e.From==t.Uid?e.To:e.From;if(rowMap.ContainsKey(other))continue;var item=data.Tasks.FirstOrDefault(q=>q.Uid==other);if(item!=null)Text(c,$"{(e.From==t.Uid?"→":"←")} {item.Id}",Math.Clamp(right+4,chartX+3,chartX+chartWidth-40),mid+3,36,6,color:Blue);}
        }if(options.Baselines&&t.BaselineStart is DateTime bs&&t.BaselineFinish is DateTime bf){float x=X(bs,start,finish,chartX,chartWidth),right=X(bf,start,finish,chartX,chartWidth);Rect(c,x,mid+6,Math.Max(2,right-x),2,Palette("Baseline",Muted));}c.Restore();
       }
       if(DateTime.Today>=start&&DateTime.Today<finish){float today=X(DateTime.Today,start,finish,chartX,chartWidth);Line(c,today,top,today,bottom,Palette("Today",SKColor.Parse("#298A45")));}
       if(rows.Length==0)Text(c,"No tasks in this selection",margin+8,top+25,bodyWidth-16,11,color:Muted);Line(c,chartX,top-32,chartX,bottom,Grid);Footer(c,label);EndPage();
      }
    }
    if(options.Content!=PdfContent.Timelines){var range=Range(options.Dates);Gantt(data.Tasks,range.Start,range.Finish,options.Content==PdfContent.TableAndGantt?"Task table + Gantt":"Gantt chart");}
    else foreach(var lane in data.Lanes){
     var range=Range(options.Dates??lane.View);var style=lane.Style??new();var overrides=lane.ItemStyles??[];var selected=data.Tasks.Where(t=>lane.Uids.Contains(t.Uid)&&t.Start.HasValue&&t.Finish.HasValue&&t.Finish>=range.Start&&t.Start<range.Finish).OrderBy(t=>t.Start).ToArray();var ends=new List<DateTime>();var packed=new List<(PdfTask Task,int Row)>();
     foreach(var task in selected){int row=ends.FindIndex(end=>end<task.Start!.Value);var end=task.Milestone?task.Start!.Value.AddDays((range.Finish-range.Start).TotalDays/options.PagesWide*175/bodyWidth):task.Finish!.Value;if(row<0){row=ends.Count;ends.Add(end);}else ends[row]=end;packed.Add((task,row));}
     int rowCount=Math.Max(1,ends.Count),timelineRows=Math.Min(options.RowsPerPage,Math.Max(1,(int)((bottom-top)/44))),vertical=Math.Max(1,(int)Math.Ceiling(rowCount/(double)timelineRows));
     for(int down=0;down<vertical;down++)for(int across=0;across<options.PagesWide;across++){
      var start=range.Start.AddDays((range.Finish-range.Start).TotalDays*across/options.PagesWide);var finish=range.Start.AddDays((range.Finish-range.Start).TotalDays*(across+1)/options.PagesWide);var c=BeginPage();pages++;Header(c,"Timeline · "+lane.Name,start,finish,across);DateHeader(c,start,finish,margin,bodyWidth);
      float timelineRow=Math.Min(60,(bottom-top)/Math.Min(timelineRows,rowCount));c.Save();c.ClipRect(new(margin,top,margin+bodyWidth,bottom));
      foreach(var (t,row) in packed.Where(p=>p.Row>=down*timelineRows&&p.Row<(down+1)*timelineRows)){
       if(t.Start>=finish||t.Finish<start)continue;var item=overrides.GetValueOrDefault(t.Uid);string text=item?.Label??t.Name;string? colorString=item?.Color??(t.Milestone?style.MilestoneColor:style.BarColor);var color=colorString!=null&&SKColor.TryParse(colorString,out var custom)?custom:t.Milestone?Blue:SKColor.Parse("#E1EEF9");var ink=style.TextColor!=null&&SKColor.TryParse(style.TextColor,out var customInk)?customInk:Ink;
       float x=X(t.Start!.Value,start,finish,margin,bodyWidth),right=X(t.Finish!.Value,start,finish,margin,bodyWidth),y=top+12+(row-down*timelineRows)*timelineRow,font=(float)style.FontSize*.8f;
       if(t.Milestone){Line(c,margin,y+6,margin+bodyWidth,y+6,Grid);Diamond(c,x,y+6,color);float label=Math.Clamp(x+8,margin+3,margin+bodyWidth-165);if(ink==SKColors.White)Rect(c,label-3,y+13,166,style.ShowDates?29:16,color,3);Text(c,text,label,y+25,160,font,true,ink);if(style.ShowDates)Text(c,t.Start.Value.ToString("dd MMM yyyy",CultureInfo.InvariantCulture),label,y+37,160,7,color:ink);}
       else{float left=Math.Max(margin,x),end=Math.Min(margin+bodyWidth,right);Rect(c,left,y,Math.Max(4,end-left),style.ShowDates?30:20,color,style.RoundedBars?4:0);Text(c,text,left+5,y+12,Math.Max(1,end-left-10),font,true,ink);if(style.ShowDates)Text(c,$"{t.Start:dd MMM yy} – {t.Finish:dd MMM yy}",left+5,y+25,Math.Max(1,end-left-10),7,color:ink);}
      }c.Restore();if(selected.Length==0)Text(c,"No dated tasks in this timeline range",margin+8,top+25,bodyWidth-16,11,color:Muted);Footer(c,"Timeline · "+lane.Name);EndPage();
     }
    }
    pdf.Close();
   }File.Move(temp,full,false);return new(pages,data.Tasks.Length,full);
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public static void Validate(PdfOptions o){if(!Enum.IsDefined(o.Content)||!Enum.IsDefined(o.Paper)||o.RowsPerPage is <8 or >60||o.PagesWide is <1 or >8||o.Baseline is <0 or >10||!double.IsFinite(o.MarginMm)||o.MarginMm is <5 or >40)throw new ArgumentException("Check PDF page size, 8–60 rows per page, 1–8 time panels, and baseline number.");if(o.Dates!=null)new DateAxis().Restore(o.Dates);}
 public static (float Width,float Height) Size(PdfOptions o){(float a,float b)=o.Paper switch{PdfPaper.A3=>(842,1191),PdfPaper.Letter=>(612,792),_=>(595,842)};return o.Landscape?(b,a):(a,b);}
}

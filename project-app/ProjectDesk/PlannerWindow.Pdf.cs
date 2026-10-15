using Avalonia.Platform.Storage;
namespace ProjectDesk;
public sealed partial class PlannerWindow {
 private async Task ExportPdf(){
  if(_session.Document==null)throw new InvalidOperationException("Open or create a project first.");
  var options=await new PdfExportWindow(_axis,Gantt.BaselineNumber,_session.Document.Name,async previewOptions=>{if(previewOptions.Dates==null&&previewOptions.Content!=PdfContent.Timelines)previewOptions=previewOptions with{Dates=_axis.Capture()};var previewData=ProjectPdfExporter.Capture(_session,Sheet,_timeline,_axis,_formats,previewOptions,Gantt);return await Task.Run(()=>PdfPreview.Create(previewData,previewOptions));}).ShowDialog<PdfOptions?>(this);if(options==null)return;
  if(options.Dates==null&&options.Content!=PdfContent.Timelines)options=options with{Dates=_axis.Capture()};
  var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title="Export PDF",DefaultExtension="pdf",SuggestedFileName=System.IO.Path.GetFileNameWithoutExtension(_session.SourcePath??"Project")+"-"+options.Content+".pdf",FileTypeChoices=[new FilePickerFileType("PDF document"){Patterns=["*.pdf"]}]});
  if(file?.TryGetLocalPath() is not string path)return;
  var data=ProjectPdfExporter.Capture(_session,Sheet,_timeline,_axis,_formats,options,Gantt);_status.Text="Exporting PDF…";
  var result=await Task.Run(()=>ProjectPdfExporter.Export(path,data,options));_status.Text=$"Exported {result.Pages} PDF pages: {result.Path}";
 }
}

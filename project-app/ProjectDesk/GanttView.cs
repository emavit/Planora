using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OfficeIMO.Project;
using System.Globalization;

namespace ProjectDesk;

public sealed class GanttView : Control {
    private List<ProjectTask> _tasks = [];
    public void SetTasks(List<ProjectTask> tasks) { _tasks = tasks; Height = 60 + tasks.Count * 30; Width = 1400; InvalidateVisual(); }
    public override void Render(DrawingContext context) {
        base.Render(context);
        var dated = _tasks.Where(t => t.Start.HasValue && t.Finish.HasValue).ToList();
        if (dated.Count == 0) { Text(context, "No stored schedule dates", 20, 20); return; }
        var start = dated.Min(t => t.Start!.Value).Date;
        var finish = dated.Max(t => t.Finish!.Value).Date.AddDays(1);
        foreach (var baseline in dated.SelectMany(t => t.Baselines).Where(b => b.Number == 0)) {
            if (baseline.Start is DateTime a && a.Date < start) start = a.Date;
            if (baseline.Finish is DateTime b && b.Date >= finish) finish = b.Date.AddDays(1);
        }
        Text(context, "Stored schedule · Gray markers: baseline", 12, 15);
        double days = Math.Max(1, (finish - start).TotalDays), scale = 1000 / days;
        for (int i = 0; i <= 10; i++) {
            double x = 350 + i * 100;
        context.DrawLine(new Pen(DesignTokens.Separator), new Point(x, 45), new Point(x, Height));
            Text(context, start.AddDays(days * i / 10).ToString("dd MMM yy", CultureInfo.InvariantCulture), x, 15);
        }
        for (int i = 0; i < _tasks.Count; i++) {
            var task = _tasks[i]; double y = 55 + i * 30;
            Text(context, task.Name ?? "", 12, y);
            if (task.Start is not DateTime a || task.Finish is not DateTime b) continue;
            double x = 350 + (a - start).TotalDays * scale, width = Math.Max(4, (b - a).TotalDays * scale);
            context.DrawRectangle(task.IsCritical == true ? new SolidColorBrush(Color.Parse("#C7463E")) : DesignTokens.Accent, null, new Rect(x, y + 2, width, task.IsSummary ? 8 : 16), 3, 3);
            if (task.PercentComplete > 0) context.DrawRectangle(Brushes.DarkSlateBlue, null, new Rect(x, y + 2, width * (double)task.PercentComplete.Value / 100, 16));
            var baseline = task.Baselines.FirstOrDefault(v => v.Number == 0);
            if (baseline?.Start is DateTime ba && baseline.Finish is DateTime bb && bb >= ba)
                context.DrawRectangle(Brushes.Gray, null, new Rect(350 + (ba - start).TotalDays * scale, y + 20, Math.Max(3, (bb - ba).TotalDays * scale), 3));
        }
    }
    private static void Text(DrawingContext context, string text, double x, double y) => context.DrawText(
        new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(DesignTokens.Font), 12, DesignTokens.Text) { MaxTextWidth = 325, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis }, new Point(x, y));
}

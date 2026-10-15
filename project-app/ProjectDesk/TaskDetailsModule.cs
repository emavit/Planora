using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using OfficeIMO.Project;
namespace ProjectDesk;

public sealed class TaskDetailsModule : UserControl {
 private readonly StackPanel _body=new(){Spacing=18,Margin=new Thickness(22)};
 public TaskDetailsModule(){Content=new ScrollViewer{Content=_body};SetTask(null);}
 public void SetTask(ProjectTask? task){_body.Children.Clear();if(task==null){_body.Children.Add(new TextBlock{Text="Select a task to see its schedule, progress and baselines.",Foreground=DesignTokens.Secondary});return;}
  _body.Children.Add(new TextBlock{Text=task.Name,FontSize=24,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap});
  _body.Children.Add(new TextBlock{Text=$"Task {task.DisplayId??task.Uid} · {(task.IsManual==true?"Manually scheduled":"Automatically scheduled")}",Foreground=DesignTokens.Secondary});
  var cards=new WrapPanel{Orientation=Orientation.Horizontal};foreach(var (label,value) in new[]{("Duration",TaskColumns.Format(task.Duration)),("Start",TaskColumns.Format(task.Start)),("Finish",TaskColumns.Format(task.Finish)),("Work",TaskColumns.Format(task.Work)),("Cost",TaskColumns.Format(task.Cost)),("Complete",task.PercentComplete.HasValue?$"{task.PercentComplete}%":"—")}){var stack=new StackPanel{Spacing=8};stack.Children.Add(new TextBlock{Text=label,Foreground=DesignTokens.Secondary,FontSize=12});stack.Children.Add(new TextBlock{Text=value??"—",FontSize=18,TextWrapping=TextWrapping.Wrap});cards.Children.Add(new Border{Child=stack,Width=220,MinHeight=92,Padding=new Thickness(16),Margin=new Thickness(0,0,12,12),Background=DesignTokens.Background,BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(1),CornerRadius=DesignTokens.Radius});}_body.Children.Add(cards);
  _body.Children.Add(new TextBlock{Text="Baselines",FontSize=18,FontWeight=FontWeight.SemiBold});bool any=false;foreach(var baseline in task.Baselines){any=true;var row=new StackPanel{Spacing=6};row.Children.Add(new TextBlock{Text=$"Baseline {baseline.Number}",FontWeight=FontWeight.SemiBold});row.Children.Add(new TextBlock{Text=$"{TaskColumns.Format(baseline.Start)??"—"} → {TaskColumns.Format(baseline.Finish)??"—"} · Work {TaskColumns.Format(baseline.Work)??"—"} · Cost {TaskColumns.Format(baseline.Cost)??"—"}",TextWrapping=TextWrapping.Wrap,Foreground=DesignTokens.Secondary});_body.Children.Add(new Border{Child=row,Padding=new Thickness(14),BorderBrush=DesignTokens.Separator,BorderThickness=new Thickness(1),CornerRadius=DesignTokens.Radius});}if(!any)_body.Children.Add(new TextBlock{Text="No baseline stored for this task.",Foreground=DesignTokens.Secondary});
 }
}

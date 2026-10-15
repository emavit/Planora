using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ProjectDesk;

public static class IconValidation {
    public static int Run(string file,string output) {
        Directory.CreateDirectory(output);
        AppBuilder.Configure<DesktopApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).SetupWithoutStarting();
        int checks=0;void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
        using var session=new ProjectSession();session.Open(file);
        var window=new PlannerWindow(session,Path.Combine(output,"icon-views.json"));window.Show();Dispatcher.UIThread.RunJobs();window.RefreshCommandIcons();
        foreach(string key in UiIcons.Paths.Keys){var icon=new UiIcon(key);Check(icon.Width==18&&icon.Height==18,"Invalid icon geometry: "+key);}
        var app=Application.Current!;
        foreach(var theme in new[]{ThemeVariant.Light,ThemeVariant.Dark}) {
            app.RequestedThemeVariant=theme;Dispatcher.UIThread.RunJobs();
            var tabs=window.GetVisualDescendants().OfType<Button>().Where(b=>b.Classes.Contains("ribbonTab")).ToArray();
            var labels=new HashSet<string>();int mapped=0;
            foreach(var tab in tabs) {
                var before=tab.Content;tab.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
                Check(Equals(before,tab.Content),"Icon styling changed ribbon tab identity.");
                foreach(var button in window.GetVisualDescendants().OfType<Button>().Where(b=>b is not ToggleButton&&b.Content is string&&b.IsEffectivelyVisible&&b.Bounds.Width>0&&b.Bounds.Height>0)) {
                    string label=(string)button.Content!;
                    if(UiIcons.KeyFor(label)==null)continue;
                    labels.Add(label);mapped++;
                    Check(button.GetVisualDescendants().OfType<UiIcon>().Any(),"Command icon is missing: "+label);
                    Check(!string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)),"Command accessibility name missing: "+label);
                    Check(button.Content is string,"Command identity replaced: "+label);
                }
            }
            Check(mapped>50&&labels.Contains("Save a copy")&&labels.Contains("Gantt Chart"),"Major ribbon commands were not covered.");
            var menus=window.GetLogicalDescendants().OfType<MenuItem>().Where(m=>m.Header is string text&&UiIcons.KeyFor(text)!=null).ToArray();
            Check(menus.Length>20&&menus.All(m=>m.Icon is UiIcon),"Menu icons did not cover available commands.");
            Check(menus.Any(m=>Equals(m.Header,"Export PDF…")&&m.Icon is UiIcon icon&&icon.IconKey=="pdf"),"PDF menu icon missing.");
            var taskTab=tabs.Single(b=>Equals(b.Content,"Task"));taskTab.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.UIThread.RunJobs();
            var largeView=window.GetVisualDescendants().OfType<Button>().First(b=>b.Content is StackPanel);
            Check(largeView.GetVisualDescendants().OfType<UiIcon>().Any(),"Large ribbon control content was not preserved.");
            Check(!largeView.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text=="Avalonia.Controls.StackPanel"),"A custom command panel rendered as its type name.");
            using(var frame=window.CaptureRenderedFrame()??throw new Exception("Icon UI did not render"))frame.Save(Path.Combine(output,"icons-"+theme.Key+".png"));
            var dialog=new DateRangeWindow("Gantt",window.Axis);dialog.Show(window);Dispatcher.UIThread.RunJobs();
            foreach(string label in new[]{"Apply","Cancel"}) {
                var button=dialog.GetVisualDescendants().OfType<Button>().Single(b=>Equals(b.Content,label));
                Check(button.GetVisualDescendants().OfType<UiIcon>().Any(),"Dynamic dialog icon missing: "+label);
            }
            dialog.Close();
            var wrap=new WrapPanel{Margin=new Thickness(16),ItemWidth=145,ItemHeight=64};
            foreach(string key in UiIcons.Paths.Keys)wrap.Children.Add(new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,
                Children={new UiIcon(key,24){VerticalAlignment=VerticalAlignment.Center},new TextBlock{Text=key,VerticalAlignment=VerticalAlignment.Center}}});
            var gallery=new Window{Title="Original ProjectDesk vector icons",Width=950,Height=780,Content=wrap};gallery.Show();Dispatcher.UIThread.RunJobs();
            var brush=TextElement.GetForeground(gallery.GetVisualDescendants().OfType<UiIcon>().First());
            Check(brush is ISolidColorBrush solid&&solid.Color==DesignTokens.Text.Color,"Icon foreground does not follow theme.");
            using(var frame=gallery.CaptureRenderedFrame()??throw new Exception("Icon gallery did not render"))frame.Save(Path.Combine(output,"icon-catalogue-"+theme.Key+".png"));gallery.Close();
            File.WriteAllLines(Path.Combine(output,"commands-"+theme.Key+".txt"),labels.Order());
        }
        Check(!session.HasEdits,"Icon UI changed the project.");window.Close();
        Console.WriteLine($"PASS: {checks} icon checks; {UiIcons.Paths.Count} original vector symbols, menus, all ribbon tabs and dynamic dialogs in light/dark. Project unchanged.");return 0;
    }
}

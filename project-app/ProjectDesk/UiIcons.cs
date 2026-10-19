using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace ProjectDesk;

// Original 24-unit stroke drawings. No SF Symbols assets, icon font or platform-specific glyphs.
public sealed class UiIcon : Control {
    private readonly Geometry _geometry;
    public string IconKey { get; }
    public UiIcon(string key, double size = 18) {
        IconKey=key;_geometry=Geometry.Parse(UiIcons.Paths.GetValueOrDefault(key,UiIcons.Paths["command"]));
        Width=Height=size;IsHitTestVisible=false;Focusable=false;
        AutomationProperties.SetName(this, "");
    }
    internal IBrush IconBrush => !IsEffectivelyEnabled || this.GetVisualAncestors().OfType<Button>().Any(b=>b.Classes.Contains("primary"))
        ? TextElement.GetForeground(this)??DesignTokens.Text
        : UiIcons.ColorFor(IconKey,ActualThemeVariant==ThemeVariant.Dark);
    static UiIcon() => AffectsRender<UiIcon>(TextElement.ForegroundProperty);
    public override void Render(DrawingContext context) {
        base.Render(context);
        using var transform=context.PushTransform(Matrix.CreateScale(Bounds.Width/24,Bounds.Height/24));
        context.DrawGeometry(null,new Pen(IconBrush,1.65,
            lineCap:PenLineCap.Round,lineJoin:PenLineJoin.Round),_geometry);
    }
}

public static class UiIcons {
    private static readonly Dictionary<string,(IBrush Light,IBrush Dark)> Colors=new(){
        ["blue"]=(Brush("#2563B8"),Brush("#89BBFF")),
        ["teal"]=(Brush("#087E81"),Brush("#6AD8CB")),
        ["purple"]=(Brush("#7750B7"),Brush("#C5A5F5")),
        ["amber"]=(Brush("#A2640A"),Brush("#F2C16A")),
        ["red"]=(Brush("#BE4248"),Brush("#FF9DA4"))
    };
    private static IBrush Brush(string value)=>new SolidColorBrush(Color.Parse(value));
    internal static IBrush ColorFor(string key,bool dark){
        string family=key switch{
            "gantt" or "timeline" or "task" or "check" or "progress" or "today"=>"teal",
            "person" or "people" or "assign" or "network" or "board"=>"purple",
            "open" or "milestone" or "baseline" or "notes" or "highlight"=>"amber",
            "delete" or "pdf" or "unlink"=>"red",
            _=>"blue"
        };
        var pair=Colors[family];return dark?pair.Dark:pair.Light;
    }

    public static IReadOnlyDictionary<string,string> Paths { get; }=new Dictionary<string,string> {
        ["command"]="M5 4H19V20H5Z M8 8H16 M8 12H16 M8 16H13",
        ["file"]="M6 3H14L19 8V21H6Z M14 3V8H19",
        ["new"]="M6 3H14L19 8V21H6Z M14 3V8H19 M9 14H16 M12.5 10.5V17.5",
        ["open"]="M3 7V20H19L22 10H7L4 20 M3 7H10L12 10",
        ["save"]="M4 3H17L21 7V21H4Z M8 3V10H16V3 M8 21V14H17V21",
        ["pdf"]="M6 3H14L19 8V21H6Z M14 3V8H19 M8 15H16 M12 11V18 M9 15L12 18L15 15",
        ["close"]="M6 6L18 18 M18 6L6 18",
        ["undo"]="M8 5L3 10L8 15 M3 10H14C20 10 22 19 15 21",
        ["redo"]="M16 5L21 10L16 15 M21 10H10C4 10 2 19 9 21",
        ["copy"]="M9 7H20V21H9Z M5 17H3V3H14V5",
        ["paste"]="M8 5H4V21H20V5H16 M8 3H16V7H8Z M8 12H16 M8 16H14",
        ["cut"]="M8 9L20 3 M8 15L20 21 M7 7A3 3 0 1 1 1 7A3 3 0 1 1 7 7 M7 17A3 3 0 1 1 1 17A3 3 0 1 1 7 17",
        ["search"]="M16 10A6 6 0 1 1 4 10A6 6 0 1 1 16 10 M14.5 14.5L21 21",
        ["replace"]="M4 7H18L15 4 M18 7L15 10 M20 17H6L9 14 M6 17L9 20",
        ["task"]="M4 4H20V20H4Z M8 9L10 11L15 6 M8 15H16",
        ["milestone"]="M12 3L21 12L12 21L3 12Z",
        ["summary"]="M4 5H20 M4 19H20 M4 5V19 M8 9H19 M8 14H15",
        ["delete"]="M3 6H21 M9 6V3H15V6 M6 6L7 21H17L18 6 M10 10V17 M14 10V17",
        ["link"]="M9 15L15 9 M8 13L6 15C2 19 7 24 11 20L14 17 M16 11L18 9C22 5 17 0 13 4L10 7",
        ["unlink"]="M9 15L15 9 M6 15C2 19 7 24 11 20 M18 9C22 5 17 0 13 4 M3 3L21 21",
        ["person"]="M16 7A4 4 0 1 1 8 7A4 4 0 1 1 16 7 M4 21V18C4 11 20 11 20 18V21",
        ["people"]="M14 7A3 3 0 1 1 8 7A3 3 0 1 1 14 7 M3 20V17C3 11 19 11 19 17V20 M17 4C22 4 22 10 18 10 M21 14C24 16 23 20 23 20",
        ["assign"]="M10 7A3 3 0 1 1 4 7A3 3 0 1 1 10 7 M1 21V17C1 11 13 11 13 17V21 M15 12H23 M19 8V16",
        ["info"]="M21 12A9 9 0 1 1 3 12A9 9 0 1 1 21 12 M12 11V17 M12 7V7.2",
        ["calendar"]="M3 5H21V21H3Z M7 2V8 M17 2V8 M3 10H21 M7 14H10 M14 14H17 M7 18H10",
        ["calculate"]="M5 3H19V21H5Z M8 6H16V10H8Z M8 14H10 M14 14H16 M9 17V19 M15 17V19",
        ["baseline"]="M3 20H21 M5 4V17 M7 7H14 M7 12H20 M7 17H17",
        ["gantt"]="M3 3V21H22 M7 6H15V9H7Z M11 12H21V15H11Z M5 18H13",
        ["table"]="M3 4H21V20H3Z M3 10H21 M3 15H21 M9 4V20 M15 4V20",
        ["usage"]="M4 3V21H22 M7 16V11H10V16 M13 16V6H16V16 M19 16V9H22V16",
        ["network"]="M3 9H8V15H3Z M16 3H21V9H16Z M16 15H21V21H16Z M8 12H12V6H16 M12 12V18H16",
        ["chart"]="M4 3V21H22 M7 16L12 10L16 13L22 5",
        ["board"]="M3 4H21V20H3Z M9 4V20 M15 4V20 M5 7H7 M11 7H13 M17 7H19 M5 11H7 M11 11H13",
        ["timeline"]="M2 12H22 M5 9V15 M11 9V15 M19 9V15 M5 4H12V7H5Z M12 17H20V20H12Z",
        ["columns"]="M3 4H9V20H3Z M12 4H18V20H12Z M21 9V15 M18 12H24",
        ["zoom"]="M15 9A6 6 0 1 1 3 9A6 6 0 1 1 15 9 M13 13L21 21 M6 9H12 M9 6V12",
        ["plus"]="M5 12H19 M12 5V19",
        ["minus"]="M5 12H19",
        ["fit"]="M3 9V3H9 M15 3H21V9 M21 15V21H15 M9 21H3V15 M8 12H16",
        ["today"]="M3 5H21V21H3Z M7 2V8 M17 2V8 M3 10H21 M8 16L11 19L17 13",
        ["format"]="M3 4H21 M12 4V21 M7 21H17",
        ["color"]="M12 3C5 3 2 8 3 14C4 20 11 22 14 20C17 18 12 15 17 14C22 13 23 6 17 4Z M7 8H7.2 M12 6H12.2 M17 8H17.2 M6 13H6.2",
        ["highlight"]="M7 14L4 19L9 20L13 17 M7 14L15 3L21 7L13 17Z M3 23H17",
        ["painter"]="M4 3H18V9H4Z M18 6H21V13H12V21H9V13",
        ["clear"]="M8 4L21 12L13 22H7L1 17Z M6 11L17 18 M13 22H23",
        ["notes"]="M5 3H20V15L14 21H5Z M14 21V15H20 M8 8H16 M8 12H16",
        ["left"]="M3 5H21 M3 10H15 M3 15H21 M3 20H15",
        ["center"]="M3 5H21 M6 10H18 M3 15H21 M6 20H18",
        ["right"]="M3 5H21 M9 10H21 M3 15H21 M9 20H21",
        ["sort"]="M4 4V20L1 17 M4 20L7 17 M10 6H22 M10 12H18 M10 18H14",
        ["indent"]="M10 5H22 M10 10H22 M10 15H22 M10 20H22 M2 8L6 12L2 16",
        ["outdent"]="M10 5H22 M10 10H22 M10 15H22 M10 20H22 M6 8L2 12L6 16",
        ["expand"]="M4 8L12 3L20 8 M4 16L12 21L20 16",
        ["up"]="M5 15L12 8L19 15",
        ["down"]="M5 9L12 16L19 9",
        ["check"]="M4 12L9 17L20 6",
        ["progress"]="M21 12A9 9 0 1 1 12 3 M12 3V12H21",
        ["clock"]="M21 12A9 9 0 1 1 3 12A9 9 0 1 1 21 12 M12 6V12L16 15",
        ["settings"]="M3 6H21 M3 12H21 M3 18H21 M7 3V9 M16 9V15 M10 15V21",
        ["theme"]="M12 3A9 9 0 1 0 12 21Z M12 3A9 9 0 0 1 12 21",
        ["sun"]="M16 12A4 4 0 1 1 8 12A4 4 0 1 1 16 12 M12 1V4 M12 20V23 M1 12H4 M20 12H23 M4 4L6 6 M18 18L20 20 M4 20L6 18 M18 6L20 4",
        ["moon"]="M15 3C9 11 13 18 21 17C17 24 5 22 3 14C1 7 8 1 15 3Z",
        ["help"]="M21 12A9 9 0 1 1 3 12A9 9 0 1 1 21 12 M8 8C8 3 17 4 16 9C15 12 12 11 12 15 M12 18V18.2",
        ["back"]="M21 12H3 M9 6L3 12L9 18",
        ["arrow"]="M3 12H21 M15 6L21 12L15 18"
    };
    private static bool _installed;
    public static void Install(Application app) {
        app.Styles.Add(new Style(s=>s.OfType<Button>()) { Setters={
            new Setter(Button.ContentTemplateProperty,new FuncDataTemplate<object>((value,_)=>value is Control content?content:Label(value?.ToString()??""))) } });
        if(_installed)return;_installed=true;
        Button.ContentProperty.Changed.AddClassHandler<Button>((button,_)=>Decorate(button));
        MenuItem.HeaderProperty.Changed.AddClassHandler<MenuItem>((menu,_)=>Decorate(menu));
        Control.LoadedEvent.AddClassHandler<Control>((control,_)=> { if(control is Button button)Decorate(button);else if(control is MenuItem item)Decorate(item); });
    }
    public static string DisplayText(string label) {
        var text=label.Trim();
        foreach(var prefix in new[]{"▣  ","▱  ","⌕  ","＋ ","→ ","← ","◇ ","Aa ","▤ ","ⓘ "})if(text.StartsWith(prefix,StringComparison.Ordinal))text=text[prefix.Length..];
        return text;
    }
    public static string? KeyFor(string label) {
        var text=DisplayText(label).Replace("…","").Replace(" ▾","").Trim().ToLowerInvariant();
        if(text is "▣")return "save";if(text is "↶")return "undo";if(text is "↷")return "redo";
        if(text is "+" or "＋")return "plus";if(text is "−" or "-")return "minus";
        if(text is "▾")return "down";if(text is "⌃")return "up";
        if(text is "b" or "i" or "u" or "0%" or "25%" or "50%" or "75%" or "100%")return null;
        if(text is "previous")return "back";if(text is "next")return "arrow";
        if(text is "preview"||text.StartsWith("export"))return "pdf";if(text is "close preview")return "close";
        if(text is "save connection")return "save";if(text is "forget api key")return "delete";if(text is "new chat")return "new";if(text is "use answer")return "check";
        if(text.Contains("chatgpt")||text.Contains("claude")||text is "ai" or "planora ai")return "notes";
        if(text.Contains("copy answer"))return "copy";
        if(text.Contains("import task")||text.Contains("paste web"))return "paste";
        if(text.Contains("connection settings"))return "settings";
        if(text.Contains("review and send"))return "arrow";
        if(text.Contains("cancel request"))return "close";
        if(text.Contains("pdf"))return "pdf";
        if(text is "cancel operation" or "close")return "close";
        if(text.Contains("disconnect"))return "unlink";
        if(text.Contains("connect")||text.Contains("mcp"))return "link";
        if(text.Contains("remove server"))return "delete";
        if(text.Contains("edit server")||text.Contains("environment"))return "settings";
        if(text.Contains("refresh"))return "redo";
        if(text.Contains("review and run")||text is "run tool")return "arrow";
        if(text.Contains("import selected")||text is "import tasks")return "paste";
        if(text is "add server" or "add demo")return "plus";
        if(text.StartsWith("save"))return "save";if(text.StartsWith("open"))return "open";
        if(text is "new" or "new project")return "new";
        if(text.Contains("undo"))return "undo";if(text.Contains("redo"))return "redo";
        if(text.Contains("painter"))return "painter";
        if(text is "copy" or "cut" or "paste")return text;
        if(text.Contains("find")||text.Contains("search"))return "search";
        if(text.Contains("replace"))return "replace";
        if(text.Contains("information")||text.Contains("details"))return "info";
        if(text.Contains("assign"))return "assign";
        if(text.Contains("resource graph"))return "chart";
        if(text.Contains("resource"))return "people";
        if(text.Contains("unlink"))return "unlink";if(text.Contains("link"))return "link";
        if(text.Contains("milestone"))return "milestone";if(text.Contains("summary"))return "summary";
        if(text.Contains("baseline"))return "baseline";if(text.Contains("calculate"))return "calculate";
        if(text.Contains("today"))return "today";
        if(text.Contains("zoom")||text.Contains("timescale"))return "zoom";
        if(text.Contains("fit")||text.Contains("entire project"))return "fit";
        if(text.Contains("dates")||text.Contains("calendar")||text.Contains("working time"))return "calendar";
        if(text.Contains("outdent"))return "outdent";if(text.Contains("indent"))return "indent";
        if(text.Contains("sort"))return "sort";
        if(text.Contains("move up"))return "up";if(text.Contains("move down"))return "down";
        if(text.Contains("expand")||text.Contains("subtask"))return "expand";
        if(text.Contains("column"))return "columns";
        if(text.Contains("timeline"))return "timeline";
        if(text.Contains("network"))return "network";
        if(text.Contains("board"))return "board";
        if(text.Contains("gantt")||text.Contains("tracking"))return "gantt";
        if(text.Contains("task usage")||text is "work" or "cost")return "usage";
        if(text.Contains("sheet")||text is "entry" or "tables")return "table";
        if(text.Contains("notes"))return "notes";
        if(text.Contains("highlight"))return "highlight";
        if(text.Contains("color")||text.StartsWith("choose"))return "color";
        if(text.Contains("align left"))return "left";if(text is "center")return "center";if(text.Contains("align right"))return "right";
        if(text.Contains("theme")||text.Contains("appearance")||text.Contains("automatic"))return "theme";
        if(text is "light")return "sun";if(text is "dark")return "moon";
        if(text.Contains("format")||text.Contains("style")||text.Contains("case")||text is "bold" or "italic" or "underline")return "format";
        if(text.Contains("delete")||text is "remove")return "delete";
        if(text.Contains("clear")||text.Contains("reset"))return "clear";
        if(text.Contains("progress")||text.Contains("complete")||text.Contains("mark on track"))return "progress";
        if(text.Contains("manually")||text.Contains("auto schedule"))return "clock";
        if(text.Contains("insert")||text.Contains("add")||text is "task" or "tasks")return "task";
        if(text is "cancel" or "don't save" or "close project" or "exit")return "close";
        if(text.StartsWith("apply")||text is "continue" or "ok")return "check";
        if(text.Contains("help")||text.Contains("all project commands"))return "help";
        if(text.Contains("run command")||text.Contains("fill down")||text.Contains("scroll to task"))return "arrow";
        if(text.Contains("rename")||text.Contains("inspect")||text.Contains("move")||text.Contains("layout")||text.Contains("transparency"))return "settings";
        return text switch{"file"=>"file","edit"=>"format","project"=>"gantt","view"=>"table","report"=>"chart","schedule"=>"calendar",_=>null};
    }
    public static bool IsIconOnly(string label)=>label.Trim() is "▣" or "↶" or "↷" or "+" or "＋" or "−" or "-" or "▾" or "⌃";
    private static Control Label(string label) {
        var key=KeyFor(label);
        if(key==null)return new TextBlock{Text=DisplayText(label),VerticalAlignment=VerticalAlignment.Center};
        if(IsIconOnly(label))return new UiIcon(key);
        return new StackPanel{Orientation=Orientation.Horizontal,Spacing=6,VerticalAlignment=VerticalAlignment.Center,
            Children={new UiIcon(key){VerticalAlignment=VerticalAlignment.Center},new TextBlock{Text=DisplayText(label),VerticalAlignment=VerticalAlignment.Center}}};
    }
    public static void Decorate(Button button) {
        if(button.Content is string label) {
            if(string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)))AutomationProperties.SetName(button,
                IsIconOnly(label)?KeyFor(label)??label:DisplayText(label));
            if(IsIconOnly(label)&&ToolTip.GetTip(button)==null)ToolTip.SetTip(button,KeyFor(label));
        } else if(button.Content is StackPanel panel&&panel.Children.FirstOrDefault() is TextBlock glyph&&glyph.FontSize>=24) {
            var name=panel.Children.OfType<TextBlock>().LastOrDefault()?.Text;
            if(name!=null&&KeyFor(name) is string key){panel.Children.RemoveAt(0);panel.Children.Insert(0,new UiIcon(key,28){HorizontalAlignment=HorizontalAlignment.Center});AutomationProperties.SetName(button,DisplayText(name));}
        }
    }
    public static void Decorate(MenuItem item) {
        if(item.Icon==null&&item.Header is string text&&KeyFor(text) is string key)item.Icon=new UiIcon(key,16);
    }
    public static void ApplyTo(Control root) {
        foreach(var button in root.GetVisualDescendants().OfType<Button>())Decorate(button);
        foreach(var menu in root.GetLogicalDescendants().OfType<MenuItem>())Decorate(menu);
    }
}

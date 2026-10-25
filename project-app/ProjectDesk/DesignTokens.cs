using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ProjectDesk;

// Shared visual policy; platform fonts and native window chrome remain platform-specific.
public static class DesignTokens {
    public static readonly SolidColorBrush Background = Brush("#F5F5F7");
    public static readonly SolidColorBrush Sidebar = Brush("#ECECF0");
    public static readonly SolidColorBrush Surface = Brush("#FFFFFF");
    public static readonly SolidColorBrush Text = Brush("#1D1D1F");
    public static readonly SolidColorBrush Secondary = Brush("#626269");
    public static readonly SolidColorBrush Accent = Brush("#0066CC");
    public static readonly SolidColorBrush AccentFill = Brush("#0066CC");
    public static readonly SolidColorBrush Selected = Brush("#DCEAFF");
    public static readonly SolidColorBrush TimelineFill=Brush("#E1EEF9");
    public static readonly SolidColorBrush NonWorking=Brush("#F3F5F7");
    public static readonly SolidColorBrush Link=Brush("#0066CC");
    public static readonly SolidColorBrush Separator = Brush("#DCDCE1");
    public static readonly CornerRadius Radius = new(10);
    public static void ApplyAppearance(bool dark){
        Accent.Color=Color.Parse(dark?"#82B9FF":"#005CBF");
        AccentFill.Color=Color.Parse(dark?"#0066CC":"#0066CC");
        TimelineFill.Color=Color.Parse(dark?"#293D50":"#E6EFF8");
        NonWorking.Color=Color.Parse(dark?"#262628":"#F5F5F7");
        Link.Color=Accent.Color;
        Background.Color=Color.Parse(dark?"#1C1C1E":"#F5F5F7");
        Sidebar.Color=Color.Parse(dark?"#28282A":"#EEEEF0");
        Surface.Color=Color.Parse(dark?"#222224":"#FFFFFF");
        Text.Color=Color.Parse(dark?"#F5F5F7":"#1D1D1F");
        Secondary.Color=Color.Parse(dark?"#B0B0B8":"#626269");
        Separator.Color=Color.Parse(dark?"#3A3A3E":"#DEDEE3");
        Selected.Color=Color.Parse(dark?"#283D58":"#E7F0FC");
    }
    // macOS: the system UI font (San Francisco) like native apps, falling back to Helvetica Neue. PDF export embeds its own font.
    public static FontFamily Font => new(OperatingSystem.IsMacOS() ? ".AppleSystemUIFont, Helvetica Neue" : "Segoe UI");
    private static SolidColorBrush Brush(string value) => new(Color.Parse(value));
    public static void Apply(Application app) {
        app.Resources["SystemAccentColor"] = Accent.Color;
        app.Styles.Add(new Style(s=>s.OfType<Window>()){Setters={new Setter(Window.BackgroundProperty,Surface),new Setter(Window.ForegroundProperty,Text)}});
        app.Styles.Add(new Style(s => s.OfType<Button>()) { Setters = {
            new Setter(Button.CornerRadiusProperty, Radius), new Setter(Button.BackgroundProperty, Surface),
            new Setter(Button.BorderBrushProperty, Separator), new Setter(Button.BorderThicknessProperty, new Thickness(1)),
            new Setter(Button.FontSizeProperty, 13d), new Setter(Button.PaddingProperty, new Thickness(12, 6)),
            new Setter(Button.ForegroundProperty, Text), new Setter(Button.MinHeightProperty, 32d)
        }});
        app.Styles.Add(new Style(s => s.OfType<Button>().Class("primary")) { Setters = {
            new Setter(Button.BackgroundProperty, AccentFill), new Setter(Button.ForegroundProperty, Brushes.White),
            new Setter(Button.BorderThicknessProperty, new Thickness(0))
        }});
        app.Styles.Add(new Style(s => s.OfType<Button>().Class("navigation")) { Setters = {
            new Setter(Button.BackgroundProperty, Brushes.Transparent), new Setter(Button.BorderThicknessProperty, new Thickness(0)),
            new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Left),
            new Setter(Button.PaddingProperty, new Thickness(12, 10)), new Setter(Button.FontSizeProperty, 14d)
        }});
        app.Styles.Add(new Style(s => s.OfType<Button>().Class("navigation").Class("selected")) { Setters = {
            new Setter(Button.BackgroundProperty, Selected), new Setter(Button.ForegroundProperty, Text),
            new Setter(Button.FontWeightProperty, FontWeight.SemiBold)
        }});
        // Quiet toolbar actions and a compact segmented view control keep the chart dominant.
        app.Styles.Add(new Style(s=>s.OfType<Button>().Class("toolbar")){Setters={
            new Setter(Button.BackgroundProperty,Brushes.Transparent),new Setter(Button.BorderThicknessProperty,new Thickness(0)),
            new Setter(Button.CornerRadiusProperty,new CornerRadius(7)),new Setter(Button.PaddingProperty,new Thickness(10,6))
        }});
        app.Styles.Add(new Style(s=>s.OfType<Button>().Class("workspaceTab")){Setters={
            new Setter(Button.BackgroundProperty,Brushes.Transparent),new Setter(Button.BorderThicknessProperty,new Thickness(0)),
            new Setter(Button.CornerRadiusProperty,new CornerRadius(6)),new Setter(Button.FontSizeProperty,13d)
        }});
        app.Styles.Add(new Style(s=>s.OfType<Button>().Class("workspaceTab").Class("selected")){Setters={
            new Setter(Button.BackgroundProperty,Surface),new Setter(Button.ForegroundProperty,Text),new Setter(Button.FontWeightProperty,FontWeight.SemiBold)
        }});
        app.Styles.Add(new Style(s=>s.OfType<Button>().Class("toolbar").Class(":pointerover")){Setters={new Setter(Button.BackgroundProperty,Selected)}});
        app.Styles.Add(new Style(s => s.OfType<TextBox>()) { Setters = {
            new Setter(TextBox.ForegroundProperty,Text), new Setter(TextBox.CornerRadiusProperty, Radius), new Setter(TextBox.BackgroundProperty, Surface),
            new Setter(TextBox.BorderBrushProperty, Separator), new Setter(TextBox.FontSizeProperty, 13d),
            new Setter(TextBox.PaddingProperty, new Thickness(12, 8))
        }});
        app.Styles.Add(new Style(s=>s.OfType<Window>()){Setters={new Setter(Window.FontFamilyProperty,Font)}});
        app.Styles.Add(new Style(s=>s.OfType<ComboBox>()){Setters={new Setter(ComboBox.CornerRadiusProperty,new CornerRadius(7)),new Setter(ComboBox.BorderBrushProperty,Separator),new Setter(ComboBox.BackgroundProperty,Surface),new Setter(ComboBox.ForegroundProperty,Text),new Setter(ComboBox.FontSizeProperty,13d)}});
        // Shared popup treatment: opaque readable surfaces, quiet borders and rounded rows.
        app.Styles.Add(new Style(s=>s.OfType<ContextMenu>()){Setters={
            new Setter(ContextMenu.BackgroundProperty,Surface),new Setter(ContextMenu.ForegroundProperty,Text),
            new Setter(ContextMenu.BorderBrushProperty,Separator),new Setter(ContextMenu.BorderThicknessProperty,new Thickness(1)),
            new Setter(ContextMenu.CornerRadiusProperty,Radius),new Setter(ContextMenu.PaddingProperty,new Thickness(5)),
            new Setter(ContextMenu.FontFamilyProperty,Font),new Setter(ContextMenu.FontSizeProperty,13d)
        }});
        app.Styles.Add(new Style(s=>s.OfType<MenuItem>()){Setters={
            new Setter(MenuItem.CornerRadiusProperty,new CornerRadius(6)),new Setter(MenuItem.FontSizeProperty,13d),
            new Setter(MenuItem.ForegroundProperty,Text),new Setter(MenuItem.MinHeightProperty,30d)
        }});
        app.Styles.Add(new Style(s=>s.OfType<MenuItem>().Class(":pointerover")){Setters={new Setter(MenuItem.BackgroundProperty,Selected)}});
        app.Styles.Add(new Style(s=>s.OfType<MenuItem>().Class(":selected")){Setters={new Setter(MenuItem.BackgroundProperty,Selected)}});
        app.Styles.Add(new Style(s=>s.OfType<ComboBoxItem>()){Setters={
            new Setter(ComboBoxItem.CornerRadiusProperty,new CornerRadius(6)),new Setter(ComboBoxItem.PaddingProperty,new Thickness(10,6)),
            new Setter(ComboBoxItem.MinHeightProperty,32d),new Setter(ComboBoxItem.FontFamilyProperty,Font),
            new Setter(ComboBoxItem.FontSizeProperty,13d),new Setter(ComboBoxItem.ForegroundProperty,Text)
        }});
        app.Styles.Add(new Style(s=>s.OfType<ComboBoxItem>().Class(":pointerover")){Setters={new Setter(ComboBoxItem.BackgroundProperty,Selected)}});
        app.Styles.Add(new Style(s=>s.OfType<ComboBoxItem>().Class(":selected")){Setters={new Setter(ComboBoxItem.BackgroundProperty,Selected),new Setter(ComboBoxItem.FontWeightProperty,FontWeight.SemiBold)}});
        app.Styles.Add(new Style(s=>s.OfType<ListBox>()){Setters={new Setter(ListBox.BackgroundProperty,Background),new Setter(ListBox.ForegroundProperty,Text),new Setter(ListBox.BorderBrushProperty,Separator),new Setter(ListBox.BorderThicknessProperty,new Thickness(1)),new Setter(ListBox.CornerRadiusProperty,new CornerRadius(8))}});
        app.Styles.Add(new Style(s=>s.OfType<Button>().Class(":focus-visible")){Setters={new Setter(Button.BorderBrushProperty,Accent),new Setter(Button.BorderThicknessProperty,new Thickness(2))}});
        app.Styles.Add(new Style(s=>s.OfType<DataGrid>()){Setters={new Setter(DataGrid.BackgroundProperty,Surface),new Setter(DataGrid.ForegroundProperty,Text),new Setter(DataGrid.HorizontalGridLinesBrushProperty,Separator),new Setter(DataGrid.VerticalGridLinesBrushProperty,Separator)}});
        app.Styles.Add(new Style(s=>s.OfType<DataGridColumnHeader>()){Setters={new Setter(DataGridColumnHeader.BackgroundProperty,Background),new Setter(DataGridColumnHeader.ForegroundProperty,Text)}});
        app.Styles.Add(new Style(s=>s.OfType<TabItem>()){Setters={new Setter(TabItem.FontSizeProperty,14d),new Setter(TabItem.PaddingProperty,new Thickness(14,10))}});
    }
}

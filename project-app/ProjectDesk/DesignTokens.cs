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
    public static void ApplyAppearance(bool dark){Accent.Color=Color.Parse(dark?"#82B9FF":"#005CBF");TimelineFill.Color=Color.Parse(dark?"#304862":"#E1EEF9");NonWorking.Color=Color.Parse(dark?"#303740":"#F3F5F7");Link.Color=Color.Parse(dark?"#82B9FF":"#0066CC");Background.Color=Color.Parse(dark?"#17191D":"#F3F5F7");Sidebar.Color=Color.Parse(dark?"#20242A":"#EEF1F4");Surface.Color=Color.Parse(dark?"#22262C":"#FFFFFF");Text.Color=Color.Parse(dark?"#EFF1F4":"#20252B");Secondary.Color=Color.Parse(dark?"#A9B0BA":"#66717D");Separator.Color=Color.Parse(dark?"#39414B":"#DDE3E9");Selected.Color=Color.Parse(dark?"#263E59":"#E8F1FC");}
    public static FontFamily Font => new(OperatingSystem.IsMacOS() ? "Helvetica Neue" : "Segoe UI");
    private static SolidColorBrush Brush(string value) => new(Color.Parse(value));
    public static void Apply(Application app) {
        app.Resources["SystemAccentColor"] = Accent.Color;
        app.Styles.Add(new Style(s=>s.OfType<Window>()){Setters={new Setter(Window.BackgroundProperty,Surface),new Setter(Window.ForegroundProperty,Text)}});
        app.Styles.Add(new Style(s => s.OfType<Button>()) { Setters = {
            new Setter(Button.CornerRadiusProperty, Radius), new Setter(Button.BackgroundProperty, Surface),
            new Setter(Button.BorderBrushProperty, Separator), new Setter(Button.BorderThicknessProperty, new Thickness(1)),
            new Setter(Button.FontSizeProperty, 13d), new Setter(Button.PaddingProperty, new Thickness(14, 8)),
            new Setter(Button.ForegroundProperty, Text), new Setter(Button.MinHeightProperty, 34d)
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
        app.Styles.Add(new Style(s => s.OfType<TextBox>()) { Setters = {
            new Setter(TextBox.ForegroundProperty,Text), new Setter(TextBox.CornerRadiusProperty, Radius), new Setter(TextBox.BackgroundProperty, Surface),
            new Setter(TextBox.BorderBrushProperty, Separator), new Setter(TextBox.FontSizeProperty, 13d),
            new Setter(TextBox.PaddingProperty, new Thickness(12, 8))
        }});
        app.Styles.Add(new Style(s=>s.OfType<Window>()){Setters={new Setter(Window.FontFamilyProperty,Font)}});
        app.Styles.Add(new Style(s=>s.OfType<ComboBox>()){Setters={new Setter(ComboBox.CornerRadiusProperty,new CornerRadius(7)),new Setter(ComboBox.BorderBrushProperty,Separator),new Setter(ComboBox.BackgroundProperty,Surface),new Setter(ComboBox.ForegroundProperty,Text),new Setter(ComboBox.FontSizeProperty,13d)}});
        app.Styles.Add(new Style(s=>s.OfType<ListBox>()){Setters={new Setter(ListBox.BackgroundProperty,Background),new Setter(ListBox.ForegroundProperty,Text),new Setter(ListBox.BorderBrushProperty,Separator),new Setter(ListBox.BorderThicknessProperty,new Thickness(1)),new Setter(ListBox.CornerRadiusProperty,new CornerRadius(8))}});
        app.Styles.Add(new Style(s=>s.OfType<Button>().Class(":focus-visible")){Setters={new Setter(Button.BorderBrushProperty,Accent),new Setter(Button.BorderThicknessProperty,new Thickness(2))}});
        app.Styles.Add(new Style(s=>s.OfType<DataGrid>()){Setters={new Setter(DataGrid.BackgroundProperty,Surface),new Setter(DataGrid.ForegroundProperty,Text),new Setter(DataGrid.HorizontalGridLinesBrushProperty,Separator),new Setter(DataGrid.VerticalGridLinesBrushProperty,Separator)}});
        app.Styles.Add(new Style(s=>s.OfType<DataGridColumnHeader>()){Setters={new Setter(DataGridColumnHeader.BackgroundProperty,Sidebar),new Setter(DataGridColumnHeader.ForegroundProperty,Text)}});
        app.Styles.Add(new Style(s=>s.OfType<TabItem>()){Setters={new Setter(TabItem.FontSizeProperty,14d),new Setter(TabItem.PaddingProperty,new Thickness(14,10))}});
    }
}

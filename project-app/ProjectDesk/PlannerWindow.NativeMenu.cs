using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
namespace ProjectDesk;

// macOS shows application menus in the system menu bar. The in-window menu stays the single definition:
// each native item raises the matching in-window item, so commands, checks and enabled state never diverge.
public sealed partial class PlannerWindow {
    private void InstallNativeMenu(Menu menus){
        if(!OperatingSystem.IsMacOS())return;
        NativeMenu.SetMenu(this,MirrorMenu(menus));
        // Headless and unsupported hosts do not export native menus; keep the in-window menu there.
        Opened+=(_,_)=>menus.IsVisible=!NativeMenu.GetIsNativeMenuExported(this);
    }
    internal static NativeMenu MirrorMenu(Menu menus){var native=new NativeMenu();foreach(var item in menus.Items)native.Items.Add(Mirror(item));return native;}
    private static NativeMenuItemBase Mirror(object? item){
        if(item is not MenuItem source)return new NativeMenuItemSeparator();
        var native=new NativeMenuItem{Header=source.Header as string,IsChecked=source.IsChecked,IsEnabled=source.IsEnabled,ToggleType=source.ToggleType switch{MenuItemToggleType.CheckBox=>NativeMenuItemToggleType.CheckBox,MenuItemToggleType.Radio=>NativeMenuItemToggleType.Radio,_=>NativeMenuItemToggleType.None}};
        var children=source.Items.Cast<object?>().ToList();
        if(children.Count>0){native.Menu=new NativeMenu();foreach(var child in children)native.Menu.Items.Add(Mirror(child));}
        else native.Command=new MenuCommand(()=>source.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)));
        source.PropertyChanged+=(_,e)=>{if(e.Property==MenuItem.IsCheckedProperty)native.IsChecked=source.IsChecked;else if(e.Property==IsEnabledProperty)native.IsEnabled=source.IsEnabled;else if(e.Property==MenuItem.HeaderProperty)native.Header=source.Header as string;};
        return native;
    }
    private sealed class MenuCommand(Action run):ICommand{public event EventHandler? CanExecuteChanged{add{}remove{}}public bool CanExecute(object? parameter)=>true;public void Execute(object? parameter)=>run();}
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
namespace ProjectDesk;
public enum SaveChangesChoice { Cancel,Save,DontSave }
public sealed class SaveChangesWindow : Window {
 public SaveChangesWindow(string name){Title="Save changes";Width=510;SizeToContent=SizeToContent.Height;MinHeight=215;MaxHeight=400;CanResize=false;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=DesignTokens.Background;FontFamily=DesignTokens.Font;
  var root=new DockPanel{Margin=new Thickness(24)};var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,22,0,0)};
  var dontSave=new Button{Content="Don't Save"};dontSave.Click+=(_,_)=>Close(SaveChangesChoice.DontSave);var cancel=new Button{Content="Cancel",IsCancel=true};cancel.Click+=(_,_)=>Close(SaveChangesChoice.Cancel);var save=new Button{Content="Save",IsDefault=true};save.Classes.Add("primary");save.Click+=(_,_)=>Close(SaveChangesChoice.Save);actions.Children.Add(dontSave);actions.Children.Add(cancel);actions.Children.Add(save);DockPanel.SetDock(actions,Dock.Bottom);root.Children.Add(actions);
  root.Children.Add(new StackPanel{Spacing=12,Children={new TextBlock{Text=$"Save changes to “{name}”?",FontSize=18,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap,MaxLines=3,TextTrimming=TextTrimming.CharacterEllipsis},new TextBlock{Text="Save creates a project copy and keeps your original file intact. If you don’t save, changes from this session will be discarded.",Foreground=DesignTokens.Secondary,TextWrapping=TextWrapping.Wrap}}});Content=root;
  AddHandler(InputElement.KeyDownEvent,(_,e)=>{if(e.Key==Key.Escape){e.Handled=true;Close(SaveChangesChoice.Cancel);}},Avalonia.Interactivity.RoutingStrategies.Tunnel);
 }
}

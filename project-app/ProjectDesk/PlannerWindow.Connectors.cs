namespace ProjectDesk;
public sealed partial class PlannerWindow {
 private async Task ShowMcpConnectors(){var path=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(_settingsPath))!,"connectors.json");var panel=new McpConnectorsWindow(new McpSettingsStore(path),new McpCredentialStore(),_session,Refresh);await panel.ShowDialog(this);}
 private async Task ShowAi(){var path=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(_settingsPath))!,"ai-connections.json");await new AiChatWindow(new AiSettingsStore(path),new McpCredentialStore(),_session,SelectedTaskUids,Refresh).ShowDialog(this);}
}

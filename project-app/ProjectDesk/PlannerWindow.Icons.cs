namespace ProjectDesk;

public sealed partial class PlannerWindow {
    // Global templates cover dynamically created dialogs; this hook also decorates lazy menu items.
    public void RefreshCommandIcons()=>UiIcons.ApplyTo(this);
}

# Source inventory

Generated from this upload snapshot. Declarations are a navigation aid, not a semantic dependency or per-function audit. All application files remain in `project-app/ProjectDesk/`.

| File | Declared types (sample) |
|---|---|
| AiChatWindow.cs | AiChatWindow |
| AiCodexClient.cs | AiCodexClient |
| AiConnectionEditor.cs | AiConnectionEditor, InputSecret |
| AiConnections.cs | AiProvider, AiConnectionMode, AiConnectionProfile, AiSettingsStore, AiMessage, AiReply, AiProviderClient, AiTaskDraft |
| AiValidation.cs | AiValidation, TestSecrets, ProviderFixture |
| AppBrand.cs | AppBrand, BrandMark |
| AutomaticTaskDates.cs | ProjectSession |
| AutoSchedulingValidation.cs | AutoSchedulingValidation |
| CalendarViewModule.cs | CalendarViewModule, DayTask |
| ChartColorsWindow.cs | ChartColorsWindow |
| ChartDrawing.cs | ChartDrawing |
| CloseFlowValidation.cs | CloseFlowValidation |
| ColumnPickerWindow.cs | ColumnPickerWindow |
| ColumnTitleWindow.cs | ColumnTitleWindow |
| CoreValidation.cs | CoreValidation |
| DateAxis.cs | DateAxisView, DateAxis |
| DateRangeWindow.cs | DateRangeWindow, TimelineNameWindow |
| DesignTokens.cs | DesignTokens |
| DurableCommands.cs | DurableCommand, DateCell, DurableHistoryEntry, DurableProjectState, ProjectSession, SessionCommand |
| DurableJsonConverters.cs | DurationJsonConverter, WorkingTimeJsonConverter |
| DurableValidation.cs | DurableValidation, serialization |
| EditableSheetModule.cs | SheetRecord, EditableSheetModule |
| EditorForms.cs | EditorInput, Choice, EditorForm, TaskEditorWindow, DependencyEdit, DependencyEditorWindow, ResourceEditorWindow, AssignmentEdit, AssignmentEditorWindow, BaselineEdit, BaselineEditorWindow, CalendarEdit, CalendarEditorWindow, ProjectInformationEdit, CalendarExceptionEdit, CalendarExceptionEditorWindow, ProjectInformationWindow |
| FormattingValidation.cs | FormattingValidation |
| FormattingWindows.cs | OfficeColorWindow, ColorChoice, TextFormatWindow |
| GanttView.cs | GanttView |
| GlassNavigationSurface.cs | GlassNavigationSurface |
| GuiCommandForms.cs | FindReplaceEdit, FindReplaceWindow, GoToTaskWindow, GuiFormatEdit, ChooseGuiItemWindow, ZoomEditorWindow, GuiSortEdit, SortEditorWindow, GuiFormatWindow |
| IconValidation.cs | IconValidation |
| McpBrowserLogin.cs | McpBrowserLogin |
| McpConnections.cs | McpToolInfo, McpConnection, McpArgumentValidator |
| McpConnectorContracts.cs | McpConnectorTransport, McpConnectorProfile, McpToolProposal, McpInvocationGate, Approval |
| McpConnectorSettings.cs | McpAuthentication, McpServerDefinition, McpSettingsStore, IMcpSecrets, McpCredentialStore, Credential |
| McpConnectorsWindow.cs | McpConnectorsWindow, McpReviewWindow, McpTaskImportWindow |
| McpDemoServer.cs | McpDemoServer |
| McpLocalProcess.cs | McpLocalProcess |
| McpServerEditor.cs | McpServerEditor |
| McpTaskImport.cs | McpImportedTask, McpProjectRevision, McpTaskImport |
| McpValidation.cs | McpValidation, FixtureHttp |
| NativeStabilityCheck.cs | NativeStabilityCheck |
| NetworkDiagramModule.cs | NetworkDiagramModule, NetworkCanvas |
| PdfExport.cs | PdfContent, PdfPaper, PdfOptions, PdfColumn, PdfTask, PdfLink, PdfLane, PdfSnapshot, PdfResult, ProjectPdfExporter |
| PdfExportWindow.cs | PdfExportWindow, FormatChoice |
| PdfPreviewWindow.cs | PdfPreviewData, PdfPreview, PdfPreviewWindow |
| PdfValidation.cs | PdfValidation |
| PlannerWindow.Appearance.cs | PlannerWindow |
| PlannerWindow.Closing.cs | PlannerWindow |
| PlannerWindow.Connectors.cs | PlannerWindow |
| PlannerWindow.cs | PlannerWindow |
| PlannerWindow.DateViews.cs | PlannerWindow |
| PlannerWindow.Formatting.cs | PlannerWindow, StyleNameWindow, FormattedSheetColumn |
| PlannerWindow.Icons.cs | PlannerWindow |
| PlannerWindow.Pdf.cs | PlannerWindow |
| PlannerWindow.Recovery.cs | PlannerWindow, RecoveryChoice |
| PlanoraPackage.cs | PlanoraPackage, Manifest |
| Program.cs | Program, DesktopApp |
| ProgressBoardModule.cs | ProgressBoardModule |
| ProjectCommandCatalogue.cs | ProjectGuiCommand, ProjectCommandCatalogue, ProjectCommandsWindow, CommandRow |
| ProjectPresentation.cs | ProjectPresentation |
| ProjectReportsModule.cs | ProjectReportKind, ProjectReportsModule, ReportSnapshot, OverviewRow, CostRow, WorkRow, ResourceRow |
| ProjectSession.cs | ProjectSession, TextCellChange, TaskEdit, ResourceEdit |
| PublicMppValidation.cs | PublicMppValidation |
| RecoveryStore.cs | never, RecoveryStore |
| RecoveryUiValidation.cs | RecoveryUiValidation |
| ResourceGraphModule.cs | ResourceGraphModule, ResourceChoice, AllocationInterval, AllocationCanvas |
| RibbonModule.cs | RibbonModule |
| RichNotesWindow.cs | RichNotesWindow, ParagraphFormatWindow |
| SaveChangesWindow.cs | SaveChangesChoice, SaveChangesWindow |
| TaskColumns.cs | TaskColumn, TaskColumns, TaskTableRow, ColumnSetting, ViewPreferences |
| TaskDetailsModule.cs | TaskDetailsModule |
| TaskFieldEditing.cs | TaskFieldChange, TaskFieldEditing, TaskFieldEditorWindow, CalculationEdit, CalculationEditorWindow |
| TaskGanttModule.cs | TaskGanttModule, GanttNode, GanttCanvas |
| TaskPaste.cs | ProjectSession, TaskClipboard |
| TaskSheetModule.cs | TaskSheetModule |
| TaskUsageModule.cs | TaskUsageModule, UsageRow |
| TextFormatting.cs | TextFormat, ParagraphFormat, RichRun, RichNote, ProjectAppearance, FormattingStore, FormatVisuals, FormattedTaskColumn |
| ThemeValidation.cs | ThemeValidation |
| TimelineModule.cs | TimelineLane, TimelineLayout, TimelineModule, DragState, LaneChoice, LaneView, TimelineBlock, TimelineCanvas, TimelineTaskPicker |
| TimelineStyleWindow.cs | TimelineStyle, TimelineItemStyle, TimelineStyleWindow, TimelineItemStyleWindow |
| TimelineValidation.cs | TimelineValidation |
| UiIcons.cs | UiIcon, UiIcons |
| VisualReviewValidation.cs | VisualReviewValidation |

## Workspace and template additions (0.3.x)

| File | Responsibility |
|---|---|
| PlannerWindow.Workspace.cs | Compact navigation and optional inspector composition |
| PlannerWindow.NativeMenu.cs | macOS menu bar mirror of the in-window menu |
| MacKeychain.cs | macOS Keychain adapter behind McpCredentialStore |
| TaskInspectorModule.cs | Validated draft editing bound to task identity |
| SavedWorkspaceView.cs | Saved workspace preferences and dialogs |
| StarterTemplates.cs | Original schedule recipes and atomic creation state |
| TemplateLibraryWindow.cs | Search, preview and new-project choice |
| WorkspaceValidation.cs | Navigation, inspector, editing and MPP regressions |
| PlanningLibraryValidation.cs | Template and saved-view regressions |
| PerformanceValidation.cs | Large-plan timing measurement (paste, edit, reschedule, Undo, save, reopen) |

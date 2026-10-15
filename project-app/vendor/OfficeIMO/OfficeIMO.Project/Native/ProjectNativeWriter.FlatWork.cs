namespace OfficeIMO.Project;

internal sealed partial class ProjectNativeWriter {
    // Bounded MPP14 flat-work encoding, measured against independently produced
    // Project 2024 fixtures at 0/25/50/100%. Never replace imported opaque curves.
    private void WriteAuthoredFlatWork(IProjectNativeTableEditor editor, ProjectAssignment assignment) {
        if (!_new || _profile != ProjectNativeProfile.Mpp14 || assignment.Resource?.Type != ProjectResourceType.Work) return;
        var task = assignment.Task;
        if (task?.Duration is not ProjectDuration duration || duration.IsElapsed || Minutes(duration) <= 0 ||
            assignment.Work is not ProjectWork work || assignment.Units is not ProjectUnits units || units.Value <= 0 ||
            assignment.TimephasedData.Count != 0 || (assignment.OvertimeWork?.Minutes ?? 0) != 0) {
            if ((assignment.Work?.Minutes ?? 0) > 0) UnsupportedFlatWork(assignment);
            return;
        }
        decimal actual = assignment.ActualWork?.Minutes ?? 0;
        decimal remaining = assignment.RemainingWork?.Minutes ?? work.Minutes - actual;
        // This profile covers uniform allocation only. More complex curves must
        // be qualified separately rather than silently flattened.
        if (work.Minutes != Minutes(duration) * units.Value || actual < 0 || remaining < 0 || actual + remaining != work.Minutes || !assignment.Start.HasValue || !assignment.Finish.HasValue) {
            UnsupportedFlatWork(assignment); return;
        }
        editor.Set(assignment.Uid, 0x0f40000b, Number(work.ScaledMinutes(1000)));
        editor.Set(assignment.Uid, 0x0f400029, new byte[] { actual > 0 ? (byte)1 : (byte)0 });
        editor.Set(assignment.Uid, 0x0f40002a, new byte[] { remaining == 0 ? (byte)1 : (byte)0 });
        editor.Set(assignment.Uid, 0x0f400053, new byte[] { 1 });
        editor.Set(assignment.Uid, 0x0f400054, new byte[] { 1 });
        decimal actualMinutes = actual / units.Value, remainingMinutes = remaining / units.Value;
        var planned = new byte[actual > 0 && remaining > 0 ? 100 : 72];
        PutFlat(planned, 2, (ushort)32); PutFlat(planned, 4, 44);
        PutFlat(planned, 8, (double)(160000m * units.Value));
        PutFlat(planned, 16, (double)(remaining * 1000));
        PutFlat(planned, 24, Exact(remainingMinutes * 80)); PutFlat(planned, 32, 1);
        if (planned.Length == 100) {
            foreach (int offset in new[] { 44, 72 }) {
                PutFlat(planned, offset, (double)(remaining * 1000));
                PutFlat(planned, offset + 8, (double)(160000m * units.Value));
                PutFlat(planned, offset + 24, Exact(remainingMinutes * 80));
            }
        }
        editor.Set(assignment.Uid, 0x0f400031, planned);
        if (actual > 0) {
            var calendar = task.Calendar ?? assignment.Resource.Calendar ?? _document.Calendar;
            if (calendar != null && assignment.Start.HasValue) {
                var stop = calendar.AddWorkingMinutes(assignment.Start.Value, actualMinutes);
                var resume = remaining > 0 ? NextFlatWork(calendar, stop) : stop;
                editor.Set(assignment.Uid, 0x0f400018, BitConverter.GetBytes(ProjectNativeCreation.Date(resume)));
                editor.Set(assignment.Uid, 0x0f400108, BitConverter.GetBytes(ProjectNativeCreation.Date(stop)));
            }
            var completed = new byte[56];
            PutFlat(completed, 0, (ushort)1); PutFlat(completed, 2, (ushort)24); PutFlat(completed, 4, 36);
            PutFlat(completed, 8, (double)(10000m * units.Value));
            PutFlat(completed, 16, (double)(actual * 1000)); PutFlat(completed, 24, Exact(actualMinutes * 80));
            PutFlat(completed, 36, (double)(actual * 1000)); PutFlat(completed, 44, (double)(10000m * units.Value));
            PutFlat(completed, 52, Exact(actualMinutes * 80));
            editor.Set(assignment.Uid, 0x0f400032, completed);
        }
    }
    private static void PutFlat(byte[] target, int offset, ushort value) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, target, offset, 2);
    private static void PutFlat(byte[] target, int offset, int value) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, target, offset, 4);
    private static void PutFlat(byte[] target, int offset, double value) => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, target, offset, 8);
    private DateTime NextFlatWork(ProjectCalendar calendar, DateTime stop) {
        for (int day = 0; day < 36600; day++) {
            _token.ThrowIfCancellationRequested();
            var next = calendar.GetWorkingIntervals(stop.Date.AddDays(day)).FirstOrDefault(r => r.Finish > stop);
            if (next.Finish > stop) return next.Start > stop ? next.Start : stop;
        }
        throw new InvalidOperationException("No working interval found after actual work.");
    }
    private void UnsupportedFlatWork(ProjectAssignment assignment) => AddDiagnostic(new ProjectDiagnostic(
        "PROJECT_NATIVE_AUTHORED_WORK_PROFILE", ProjectDiagnosticSeverity.Error,
        "Native authoring currently supports calculated uniform work without overtime or elapsed duration. Calculate the project first; other work contours require a separately qualified encoder.", Path(assignment, "Assignment")));
}

using OfficeIMO.Project;
using System.Text;

namespace ProjectDesk;

public sealed partial class ProjectSession {
    /// <summary>
    /// Pastes into the displayed task columns and rows. A null anchor creates rows at
    /// the bottom, or before insertBeforeUid for an uncommitted inserted blank row.
    /// The supplied visible row order may differ from the project's stored order.
    /// Validation and row creation are committed together as one undoable command.
    /// </summary>
    public IReadOnlyList<int> PasteTasks(int? anchorUid, IReadOnlyList<string> visibleColumnKeys,
        string startKey, string clipboardText, int? insertBeforeUid = null,
        IReadOnlyList<int>? visibleTaskUids = null) {
        if(Document==null)throw new InvalidOperationException("Open or create a project first.");
        var rows=TaskClipboard.Parse(clipboardText);
        if(rows.Count==0)return Array.Empty<int>();
        var columns=visibleColumnKeys.ToArray();
        int firstColumn=Array.IndexOf(columns,startKey);
        if(firstColumn<0)throw new ArgumentException("Select a displayed task column before pasting.");
        if(anchorUid.HasValue&&insertBeforeUid.HasValue)
            throw new ArgumentException("Choose an existing row or an inserted blank row as the paste destination.");
        var available=Tasks.Select(t=>t.Uid).ToHashSet();
        var order=(visibleTaskUids??Tasks.Select(t=>t.Uid).ToArray()).ToArray();
        if(order.Distinct().Count()!=order.Length||order.Any(uid=>!available.Contains(uid)))
            throw new ArgumentException("The displayed task rows changed. Select the destination again.");
        int firstRow=anchorUid.HasValue?Array.IndexOf(order,anchorUid.Value):order.Length;
        if(anchorUid.HasValue&&firstRow<0)
            throw new ArgumentException("The selected task is no longer displayed.");
        if(insertBeforeUid.HasValue&&!available.Contains(insertBeforeUid.Value))
            throw new ArgumentException("The row selected for insertion no longer exists.");

        // Check the complete block before even creating staging rows. A ragged final
        // row is valid: it changes only the cells actually present in the clipboard.
        for(int row=0;row<rows.Count;row++) {
            if(firstColumn+rows[row].Count>columns.Length)
                throw new ArgumentException($"Paste row {row+1} has more cells than the displayed columns.");
            foreach(var key in columns.Skip(firstColumn).Take(rows[row].Count))
                if(!CanEditText(key))throw new ArgumentException($"The {key} column is read-only. Start in an editable column or hide calculated columns before pasting.");
            bool newRow=firstRow+row>=order.Length;
            int nameOffset=Array.IndexOf(columns,"Name")-firstColumn;
            if(newRow&&(nameOffset<0||nameOffset>=rows[row].Count||string.IsNullOrWhiteSpace(rows[row][nameOffset])))
                throw new ArgumentException($"Paste row {row+1} creates a task and needs a Task Name.");
        }

        using var staging=new ProjectSession {
            _baseBytes=_baseBytes,Document=Replay(_position),SourcePath=SourcePath,_position=_position,_deferAutoDates=true
        };
        staging._commands.AddRange(_commands.Take(_position));
        var targets=new int[rows.Count];
        var newTasks=new List<int>();
        // Create every new row first so predecessor IDs can also address other rows
        // created by this same paste, using their final displayed IDs.
        for(int row=0;row<rows.Count;row++) {
            if(firstRow+row<order.Length)targets[row]=order[firstRow+row];
            else {
                int nameOffset=Array.IndexOf(columns,"Name")-firstColumn;
                targets[row]=staging.AddTask(rows[row][nameOffset].Trim(),beforeUid:insertBeforeUid,calculateDates:false);
                newTasks.Add(targets[row]);
            }
        }
        var explicitDates=new HashSet<(int Uid,string Key)>();
        var later=new List<TextCellChange>();
        for(int row=0;row<rows.Count;row++) {
            for(int cell=0;cell<rows[row].Count;cell++) {
                string key=columns[firstColumn+cell], value=rows[row][cell];
                if(key is "Start" or "Finish"&&!string.IsNullOrWhiteSpace(value))explicitDates.Add((targets[row],key));
                try {
                    if(key is "Predecessors" or "PercentComplete")later.Add(new(targets[row],key,value));
                    else {
                        var propertyKey=TaskFieldEditing.PropertyKey(key);
                        if(key!="ResourceNames") {
                            var parsed=TaskFieldEditing.Parse(propertyKey,value);
                            if(parsed is ProjectDuration duration&&duration.Value<0)
                                throw new ArgumentException("Duration cannot be negative.");
                            if(parsed is Enum enumeration&&!Enum.IsDefined(enumeration.GetType(),enumeration))
                                throw new ArgumentException("Choose a valid value for this field.");
                        }
                        staging.EditText(targets[row],key,value);
                    }
                } catch(Exception error) when(error is ArgumentException or FormatException or OverflowException or InvalidOperationException) {
                    throw new ArgumentException($"Paste row {row+1}, {key}: {error.Message}",error);
                }
            }
        }
        // Remove replaced incoming links together before validating new ones. Otherwise
        // reversing a link in one paste could be rejected because of its old direction.
        var relinked=later.Where(c=>c.Key=="Predecessors").Select(c=>c.TaskUid).ToHashSet();
        if(relinked.Count>0)staging.Execute("Replace pasted predecessors",d=> {
            foreach(var link in d.Dependencies.Where(l=>relinked.Contains(l.Successor.Uid)).ToArray())d.Dependencies.Remove(link);
        }, DurableCommand.For("RemoveIncoming",(object)relinked.ToArray()));
        // Links resolve against the completed row set and retain cycle validation.
        foreach(var change in later.Where(c=>c.Key=="Predecessors")) {
            try {staging.EditText(change.TaskUid,change.Key,change.Text);}
            catch(Exception error) when(error is ArgumentException or FormatException or OverflowException or InvalidOperationException) {
                throw new ArgumentException($"Paste row {Array.IndexOf(targets,change.TaskUid)+1}, {change.Key}: {error.Message}",error);
            }
        }
        // Calculate once after all pasted inputs and links are complete, including imported new rows.
        var affected=newTasks.Concat(targets.Where(uid=>rows[Array.IndexOf(targets,uid)].Select((_,index)=>columns[firstColumn+index]).Any(DateInputs.Contains))).Distinct().ToArray();
        if(affected.Length>0)try{staging.Execute("Update pasted automatic dates",d=>ComputeAutoDates(d,affected,explicitDates), DurableCommand.Dates(affected,explicitDates));}catch(Exception error)when(error is ArgumentException or InvalidDataException or InvalidOperationException){throw new ArgumentException("Could not schedule pasted tasks: "+error.Message,error);}
        // Progress is applied last so actual start/finish use the final dates.
        foreach(var change in later.Where(c=>c.Key=="PercentComplete")) {
            try {staging.EditText(change.TaskUid,change.Key,change.Text);}
            catch(Exception error) when(error is ArgumentException or FormatException or OverflowException or InvalidOperationException) {
                throw new ArgumentException($"Paste row {Array.IndexOf(targets,change.TaskUid)+1}, {change.Key}: {error.Message}",error);
            }
        }
        foreach(int uid in targets) {
            var task=staging.Document!.Tasks.GetByUid(uid);
            if(task.Start.HasValue&&task.Finish.HasValue&&task.Finish.Value<task.Start.Value)
                throw new ArgumentException($"Paste row {Array.IndexOf(targets,uid)+1}: Finish must be on or after Start.");
        }
        var steps=staging._commands.Skip(_position).ToArray();
        if(steps.Length>0)Execute("Paste tasks",d=>{foreach(var step in steps)step.Apply(d);}, DurableCommand.Batch(steps.Select(s=>s.Command)));
        return targets;
    }
}

/// <summary>Excel-compatible tab-separated clipboard text, including quoted cells.</summary>
public static class TaskClipboard {
    public static IReadOnlyList<IReadOnlyList<string>> Parse(string text) {
        if(string.IsNullOrEmpty(text))return Array.Empty<IReadOnlyList<string>>();
        var rows=new List<IReadOnlyList<string>>();
        var cells=new List<string>();
        var value=new StringBuilder();
        bool quoted=false,closedQuote=false,atCellStart=true,rowEnded=false;
        for(int i=0;i<text.Length;i++) {
            char c=text[i];
            if(quoted) {
                if(c=='"') {
                    if(i+1<text.Length&&text[i+1]=='"'){value.Append('"');i++;}
                    else {quoted=false;closedQuote=true;}
                } else value.Append(c);
                rowEnded=false;
                continue;
            }
            if(atCellStart&&c=='"'){quoted=true;atCellStart=false;rowEnded=false;continue;}
            if(c=='\t'||c is '\r' or '\n') {
                cells.Add(value.ToString());value.Clear();atCellStart=true;closedQuote=false;
                if(c!='\t') {
                    rows.Add(cells.ToArray());cells.Clear();rowEnded=true;
                    if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;
                } else rowEnded=false;
                continue;
            }
            if(closedQuote)throw new ArgumentException("A quoted clipboard cell must end before the next tab or line break.");
            value.Append(c);atCellStart=false;rowEnded=false;
        }
        if(quoted)throw new ArgumentException("The clipboard contains an unfinished quoted cell.");
        if(!rowEnded){cells.Add(value.ToString());rows.Add(cells.ToArray());}
        return rows;
    }
}

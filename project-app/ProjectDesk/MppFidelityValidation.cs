using OfficeIMO.Project;

namespace ProjectDesk;

/// <summary>Native MPP fidelity regressions first exposed by the public corpus.</summary>
internal static class MppFidelityValidation {
    public static int Run(string folder) {
        int checks=0;
        void Check(bool condition,string label){if(!condition)throw new Exception("MPP fidelity check failed: "+label);checks++;}
        string corpus=FindCorpus();
        // Baseline fixed cost: Project stores it in a dedicated task field (MPXJ FieldMap9/12/14 BASELINE_FIXED_COST and BASELINE<N>_FIXED_COST).
        using(var s=new ProjectSession()){
            s.NewProject("Baseline fixed cost",new DateTime(2026,10,19,8,0,0));int t=s.AddTask("Fixed cost task");
            s.EditTask(t,TaskEdit.From(s.Document!.Tasks.GetByUid(t)) with {FixedCost=250m});
            s.CaptureBaseline(0);s.CaptureBaseline(10);
            string dest=Path.Combine(folder,"baseline-fixed-cost.mpp");s.SaveCopy(dest,true);
            using var reopened=ProjectDocument.Load(dest);var task=reopened.Tasks.GetByUid(t);
            Check(task.Baselines.Single(b=>b.Number==0).FixedCost==250m,"Baseline 0 fixed cost survives a native round trip");
            Check(task.Baselines.Single(b=>b.Number==10).FixedCost==250m,"Baseline 10 fixed cost survives a native round trip");
        }
        foreach(string name in new[]{"mpp14task.mpp","mpp12task.mpp","mpp9task.mpp"}){
            using var s=new ProjectSession();s.Open(Path.Combine(corpus,name));var generation=s.Document!.NativeInfo!.Generation;int number=generation=="MPP9"?0:10;
            var task=s.Document.Tasks.GetByUid(1);Check(task.FixedCost>0,name+": task 1 carries a nonzero fixed cost");
            s.CaptureBaseline(number,true);string dest=Path.Combine(folder,Path.GetFileNameWithoutExtension(name)+"-baseline.mpp");
            var report=s.AssessSave(dest);Check(!report.HasErrors,name+": captured baseline fixed cost has a native writer");
            s.SaveCopy(dest,true);using var reopened=ProjectDocument.Load(dest);
            Check(reopened.Tasks.GetByUid(1).Baselines.Single(b=>b.Number==number).FixedCost==task.FixedCost,name+": captured baseline fixed cost reloads from "+generation);
        }
        Console.WriteLine($"PASS: {checks} MPP fidelity checks (baseline fixed cost).");return checks;
    }
    private static string FindCorpus() {
        for(var directory=new DirectoryInfo(AppContext.BaseDirectory);directory!=null;directory=directory.Parent){
            string candidate=Path.Combine(directory.FullName,"examples","public-mpp");
            if(File.Exists(Path.Combine(candidate,"manifest.json")))return candidate;
        }
        throw new Exception("MPP fidelity check failed: examples/public-mpp corpus not found above "+AppContext.BaseDirectory);
    }
}

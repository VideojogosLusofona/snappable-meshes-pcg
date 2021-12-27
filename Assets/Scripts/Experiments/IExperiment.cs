using System.Collections.Generic;

namespace SnapMeshPCG.Experiments
{
    public interface IExperiment
    {
        IDictionary<string, IDictionary<string, IDictionary<string, object>>> Runs { get; }
    }
}
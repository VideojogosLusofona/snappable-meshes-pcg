using System.Collections.Generic;

namespace SnapMeshPCG.Experiments
{
    public interface IExperiment
    {
        IDictionary<string, IDictionary<string, object>> GenParamSet { get; }
        IDictionary<string, IDictionary<string, object>> NavParamSet { get; }
    }
}
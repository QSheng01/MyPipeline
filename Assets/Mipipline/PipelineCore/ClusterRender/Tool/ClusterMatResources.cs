using UnityEngine;

namespace MPipeline
{
    /// <summary>
    /// Optional shared settings for the URP cluster system.
    /// The old custom pipeline used this object for virtual material streaming.
    /// URP keeps this migration focused on geometry clusters.
    /// </summary>
    [CreateAssetMenu(menuName = "MPipeline/Cluster Settings")]
    public sealed class ClusterMatResources : ScriptableObject
    {
        public static ClusterMatResources current { get; private set; }

        [Min(1)]
        public int maximumClusterCount = 100000;

        public string outputFolder = "Assets/Mipipline/PipelineCore/ClusterRender/ClusterData";

        public void Activate()
        {
            current = this;
        }

        public void Deactivate()
        {
            if (current == this)
                current = null;
        }
    }
}

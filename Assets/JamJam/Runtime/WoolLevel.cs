using UnityEngine;

namespace JamJam
{
    [CreateAssetMenu(menuName = "JamJam/Wool Level")]
    public sealed class WoolLevel : ScriptableObject
    {
        public string Title;
        public LevelDefinition Data = new LevelDefinition();
        public Vector3[] Path = System.Array.Empty<Vector3>();
    }
}

#region

using UnityEngine;

#endregion

namespace HyenaQuest.nav
{
    /// <summary>
    ///     Mimics A* Pathfinding project NavmeshCut
    /// </summary>
    public class entity_sdk_nav_cut : MonoBehaviour
    {
        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
        
        public void OnValidate() {
            if (Application.isPlaying) return;
            this.name = "entity_sdk_nav_cut";
        }
        #endif
    }
}
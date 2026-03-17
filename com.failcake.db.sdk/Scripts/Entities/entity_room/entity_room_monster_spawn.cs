#region

using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_room_monster_spawn : MonoBehaviour
    {
        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(this.transform.position + Vector3.up, new Vector3(1, 2, 1)); // Safe monster size-ish
            Gizmos.DrawIcon(this.transform.position, "npc_maker");
        }
        #endif
    }
}
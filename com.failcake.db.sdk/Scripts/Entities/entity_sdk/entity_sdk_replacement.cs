using SaintsField;
using UnityEngine;

namespace HyenaQuest
{
    public class entity_sdk_replacement : MonoBehaviour
    {
        #if UNITY_EDITOR
        [InfoBox("This entity template should be placed inside a entity_network_spawn")]
        [InfoBox("DO NOT EDIT, USED TO MAP TEMPLATES ON THE MAIN GAME", EMessageType.Error)]
        public Mesh preview;

        public void OnDrawGizmos() {
            if (!this.preview) return;
            
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.DrawMesh(this.preview);
        }

        public void OnValidate() {
            if (Application.isPlaying || !this.preview) return;
            
            string newName = $"SDK-{this.preview.name}";
            if (this.name == newName) return;
            this.name = newName;
        }
        #endif
    }
}
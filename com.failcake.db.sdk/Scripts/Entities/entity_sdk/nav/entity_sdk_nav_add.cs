#region

using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_sdk_nav_add : MonoBehaviour
    {
        public void Awake() {
            SDK_SETUP.PatchSDKEntity?.Invoke(this.gameObject);
        }

        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            Gizmos.color = Color.blueViolet;
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.localScale);
            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 0.001F, 1));
        }

        public void OnValidate() {
            if (Application.isPlaying) return;
            string newName = "entity_sdk_nav_add";
            if (this.name == newName) return;
            
            this.name = newName;
        }
        #endif
    }
}
using System;
using SaintsField.Playa;
using UnityEngine;

namespace HyenaQuest.nav
{
    public class entity_sdk_nav_add : MonoBehaviour
    {
        #if UNITY_EDITOR
        public void OnDrawGizmos() {
            Gizmos.color = Color.blueViolet;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.DrawCube(Vector3.zero,  new Vector3(1, 0.001F, 1));
        }

        public void OnValidate() {
            if (Application.isPlaying) return;
            this.name = "entity_sdk_nav_add";
        }
        #endif
    }
}
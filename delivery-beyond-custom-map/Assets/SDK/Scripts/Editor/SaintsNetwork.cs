#region

using SaintsField.Editor;
using Unity.Netcode;
using UnityEditor;

#endregion

namespace HyenaQuest
{
    [CustomEditor(typeof(NetworkBehaviour), true)]
    public class ApplySaintsNetworkBehaviourEditor : SaintsEditor { }
}
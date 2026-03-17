using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest
{
    public class entity_network_template_auto: entity_network_template_base
    {
        #region PRIVATE
        private bool IsServer => NetworkManager.Singleton && NetworkManager.Singleton.IsServer;
        
        private Coroutine _coroutine;
        #endregion
        
        public new void Awake() {
            base.Awake();
            this._coroutine = this.StartCoroutine(this.Spawn());
        }

        public new void OnDestroy() {
            base.OnDestroy();
            if(this._coroutine != null) this.StopCoroutine(this._coroutine);
        }
        
        public override bool CanSpawn() {
            return true;
        }
        
        #region PRIVATE

        private IEnumerator Spawn() {
            yield return new WaitForSecondsRealtime(1);
            if (!this.IsServer) yield break;
            
            (GameObject, NetworkObject) items = this.NetworkSpawn();
            if (!items.Item1 || !items.Item2) yield break;
            
            items.Item2.Spawn();

        }
        #endregion
    }
}
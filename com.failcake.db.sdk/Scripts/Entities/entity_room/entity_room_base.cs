#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FailCake.VIS;
using FailCake.VMF;

using SaintsField;
using SaintsField.Playa;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZLinq;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

namespace HyenaQuest
{
    [Serializable]
    public struct RoomLayer
    {
        public GameObject layer;

        [Range(0, 1)]
        public float weight;

        [Range(1, byte.MaxValue)]
        public int round;
    }

    [Serializable]
    public struct RoomTexture
    {
        public VMFMaterial material;

        [MaxValue(16)]
        public List<Texture2D> texture;
    }

    [RequireComponent(typeof(NetworkObject))]
    public class entity_room_base : NetworkBehaviour
    {
        #region STATIC

        private static readonly int Blend = Shader.PropertyToID("_Blend");
        private static readonly int BlendTexture = Shader.PropertyToID("_BlendTexture");
        private static readonly int MainTexture = Shader.PropertyToID("_MainTexture");
        private static readonly int Color = Shader.PropertyToID("_MainColor");
        private static readonly int Layer = Shader.PropertyToID("_Layer");

        #endregion

        [LayoutStart("BOUNDS", ELayout.Background | ELayout.TitleOut), SaintsField.ReadOnly, SerializeField, NoLabel]
        private Bounds _BOUNDS_;

        [LayoutStart("Settings", ELayout.Background | ELayout.TitleOut)]
        public List<RoomLayer> layers = new List<RoomLayer>();

        [SaintsDictionary]
        public SaintsDictionary<VMFLayer, RoomTexture> modifierTextures = new SaintsDictionary<VMFLayer, RoomTexture>();
        
        [Range(0, 100)]
        public int minSpawnRounds;

        #if UNITY_EDITOR
        [Button("SETUP")]
        public void __SetupRoom__() {
            if (Application.isPlaying) return;

            // Calculate all bounds ----
            (Bounds, Bounds) bounds = this.CalculateRoomBounds();
            this._BOUNDS_ = bounds.Item1;
            // ------------------------

            // Setup navcut on ceiling ----
            /*NavmeshCut ceilingCollider = this.GetComponentsInChildren<NavmeshCut>(true).AsValueEnumerable()
                .Where(c => !this.HasOccluderIgnoreParent(c.transform))
                .FirstOrDefault();
            if (ceilingCollider)
            {
                ceilingCollider.type = NavmeshCut.MeshType.Box;
                ceilingCollider.useRotationAndScale = true;

                ceilingCollider.height = 0.8F;
                ceilingCollider.center = bounds.Item1.center + new Vector3(0, bounds.Item1.extents.y, 0);
                ceilingCollider.rectangleSize = new Vector2(bounds.Item1.size.x, bounds.Item1.size.z);

                ceilingCollider.radiusExpansionMode = NavmeshCut.RadiusExpansionMode.DontExpand;
            }*/
            // ----------------------------

            // Setup mesh renderers ---
            MeshRenderer[] meshRenderers = this.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (!meshRenderer) continue;
                meshRenderer.staticShadowCaster = true;
                meshRenderer.renderingLayerMask = RenderingLayerMask.GetMask("RecieveDecals");
            }
            // ---------------

            // Setup room
            entity_vis_room VISRoom = this.GetComponent<entity_vis_room>();
            if (VISRoom)
            {
                // Setup portals
                entity_vis_portal[] VISPortals = this.GetComponentsInChildren<entity_vis_portal>();
                foreach (entity_vis_portal portal in VISPortals)
                {
                    if (!portal) continue;

                    switch (portal)
                    {
                        case entity_vis_portal_3d portal3D:
                            portal3D.transform.localPosition = bounds.Item2.center;
                            portal3D.size = bounds.Item2.size;

                            portal3D.SetRoom(VISRoom);
                            break;
                        case entity_vis_portal_2d portal2D:
                            portal2D.SetRoomB(VISRoom);
                            break;
                    }
                }
                // -----------------
            }
            else
                Debug.LogWarning("Missing entity_vis_room component on room prefab: " + this.name);
            // -----------------

            EditorUtility.SetDirty(this);
        }
        #endif

        #region PRIVATE

        protected readonly List<entity_room_base> _adjacentRooms = new List<entity_room_base>();

        #region VIS

        protected entity_vis_room _VISRoom;

        protected Renderer[] _renderers;
        protected Light[] _lights;
        protected DecalProjector[] _decals;

        protected bool _isVIS = true;
        protected float _lightTick;

        #endregion

        #region NET SPAWN

        private readonly List<NetworkObject> _spawnedNetworkTemplates = new List<NetworkObject>();

        #endregion

        #region NET

        protected readonly NetVar<ulong> _combinedLayerData = new NetVar<ulong>(ulong.MaxValue);

        #endregion

        #endregion

        public void Awake() {
            this._VISRoom = this.GetComponent<entity_vis_room>();
            if (!this._VISRoom) return;

            // SETUP ----
            this._VISRoom.IsInside = point => this._BOUNDS_.Contains(point);
            this._VISRoom.OnVisibilityChanged = active => {
                this._isVIS = active;

                if (this._renderers != null)
                    foreach (Renderer r in this._renderers)
                        if (r)
                            r.enabled = active;

                if (this._decals != null)
                    foreach (DecalProjector d in this._decals)
                        if (d)
                            d.enabled = active;

                if (active) this._lightTick = Time.time; // Force light check
            };
            // -----------
        }

        public void Update() {
            if (!Camera.main) return;

            if (Time.time < this._lightTick || this._lights == null) return;
            this._lightTick = Time.time + 0.2F;

            foreach (Light l in this._lights)
                if (l)
                    l.gameObject.SetActive(this._isVIS || Vector3.Distance(l.transform.position, Camera.main.transform.position) <= 18);
        }

        public bool IsRoomVisibile() { return this._isVIS; }
        
        public override void OnNetworkSpawn() {
            this._BOUNDS_ = util_bounds.GetWorldBounds(this._BOUNDS_, this.transform); // Update bounds
            if (!this.IsServer) return;
            this.PickLayerData();
        }

        public override void OnNetworkDespawn() {
            if (!this.IsServer) return;
            this.DespawnTemplates();
        }

        #region HOOKS

        // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/3186
        protected override void OnNetworkPostSpawn() {
            if (this.IsServer) this.InternalSpawnTemplates();
            
            base.OnNetworkPostSpawn();
            if (!this.IsClient) return;
            
            SDK_SETUP.OnRoomSpawn?.Invoke(this);  // Tell the map controller we're done spawning this room
            
            // HOOKS
            this._combinedLayerData.RegisterOnValueChanged((_, newValue) => {
                if (newValue == ulong.MaxValue) return;
                this.ApplyLayerData(newValue);
            });
            // ---------
        }

        public override void OnNetworkPreDespawn() {
            base.OnNetworkPreDespawn();
            if (!this.IsClient) return;

            // HOOKS ---
            this._combinedLayerData.OnValueChanged = null;
            // ---------
        }

        #endregion

        public void UpdateBounds() {
            this._BOUNDS_ = util_bounds.GetWorldBounds(this._BOUNDS_, this.transform); // Update bounds
        }

        public Bounds GetBounds() {
            return this._BOUNDS_;
        }

        public entity_room_exit[] GetExits() {
            return this.GetComponentsInChildren<entity_room_exit>(false).AsValueEnumerable().Where(a => a is not entity_interior_exit)
                .OrderBy(a => a.order != -1 ? a.order : int.MaxValue).ToArray(); // Cannot be cached since we remove the exits once we are done with them
        }

        public void SetVolumeColor(Color color) {
            entity_volume_light[] windowVolumes = this.GetComponentsInChildren<entity_volume_light>(true);
            foreach (entity_volume_light window in windowVolumes)
            {
                if (!window) continue;
                window.SetColor(color);
            }
        }

        public void SetWindowColor(Color color, float outsideIntensity) {
            entity_window_light[] windows = this.GetComponentsInChildren<entity_window_light>(true);
            foreach (entity_window_light window in windows)
            {
                if (!window) continue;
                window.SetColor(color, outsideIntensity);
            }
        }

        public void SetWindowStatus(string seed, bool forceClosed = false) {
            entity_room_window[] windows = this.GetComponentsInChildren<entity_room_window>(true);
            foreach (entity_room_window window in windows)
            {
                if (!window) continue;
                window.SetStatus(seed, forceClosed);
            }
        }

        public void SetSkyboxColor(Color color) {
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true).AsValueEnumerable().Where(r => r.transform.parent == this.transform).ToArray();
            if (renderers.Length == 0) return;

            foreach (Renderer r in renderers)
            {
                if (!r) continue;

                Material[] mats = r.materials;
                if (mats.Length == 0) continue;

                foreach (Material mat in mats)
                {
                    if (!mat || mat.shader.name != "Shader Graph/FakeSkyboxFX") continue;
                    if (!mat.HasColor(entity_room_base.Color)) continue;

                    mat.SetColor(entity_room_base.Color, color);
                }
            }
        }

        public void SetModifierTexture(Texture2D modifier, float blend) {
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true).AsValueEnumerable().Where(r => r.transform.parent == this.transform).ToArray();
            if (renderers.Length == 0) return;

            Shader shader = Shader.Find("FailCake/VMF/VMFLit");
            if (!shader) throw new UnityException("Shader 'FailCake/VMF/VMFLit' not found!");

            Shader shaderSingle = Shader.Find("FailCake/VMF/VMFLitLayer");
            if (!shaderSingle) throw new UnityException("Shader 'FailCake/VMF/VMFLitLayer' not found!");

            foreach (Renderer r in renderers)
            {
                if (!r) continue;

                Material[] mats = r.materials;
                if (mats.Length == 0) continue;

                foreach (Material mat in mats)
                {
                    if (!mat || (mat.shader != shader && mat.shader != shaderSingle)) continue;
                    if (!mat.HasTexture(entity_room_base.BlendTexture) || !mat.HasFloat(entity_room_base.Blend)) continue;

                    mat.SetTexture(entity_room_base.BlendTexture, modifier);
                    mat.SetFloat(entity_room_base.Blend, blend);
                }
            }
        }

        #region TEMPLATES
        protected virtual void DespawnTemplates() {
            if (!this.IsServer) throw new UnityException("DespawnTemplates can only be called on the server");
            if (this._spawnedNetworkTemplates.Count == 0) return;

            foreach (NetworkObject net in this._spawnedNetworkTemplates)
            {
                if (!net || !net.IsSpawned) continue;
                net.Despawn();
            }

            this._spawnedNetworkTemplates.Clear();
        }

        protected virtual void InternalSpawnTemplates() {
            if (!this.IsServer) throw new UnityException("SpawnTemplates can only be called on the server");
            if (this._spawnedNetworkTemplates.Count > 0) throw new UnityException("Templates already spawned");

            entity_network_template_chance[] netTemplates = this.GetComponentsInChildren<entity_network_template_chance>(false);
            foreach (entity_network_template_chance net in netTemplates)
            {
                if (!net.CanSpawn()) continue;
                
                (GameObject, NetworkObject) obj = net.NetworkSpawn();
                if (!obj.Item1 || !obj.Item2)
                {
                    #if UNITY_EDITOR
                    Debug.Log($"Invalid template, missing network object / valid template: {net.template.name}");
                    #endif

                    continue;
                }

                obj.Item2.Spawn();
                this._spawnedNetworkTemplates.Add(obj.Item2);
            }
        }

        #endregion

        #region NEIGHBOURHOOD
        public void AddAdjacentRoom(entity_room_base room) {
            if (this._adjacentRooms.Contains(room)) return;
            this._adjacentRooms.Add(room);
        }
        
        public List<entity_room_base> GetAdjacentRooms() {
            return this._adjacentRooms;
        }

        #endregion

        #region PRIVATE

        #region PORTALS

        #if UNITY_EDITOR
        private bool HasOccluderIgnoreParent(Transform trn) {
            Transform current = trn.parent;
            while (current)
            {
                if (current.CompareTag("OCCLUDER/IGNORE")) return true;
                current = current.parent;
            }

            return false;
        }

        private (Bounds, Bounds) CalculateRoomBounds() {
            Collider[] colliders = this.GetComponentsInChildren<Collider>(false)
                .AsValueEnumerable()
                .Where(c => !this.HasOccluderIgnoreParent(c.transform))
                .ToArray();

            if (colliders.Length == 0) throw new UnityException("No colliders found!");
            int groundLayer = LayerMask.NameToLayer("entity_ground");

            Bounds? total = null;
            Bounds? totalNoTriggers = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i] || colliders[i].gameObject.layer != groundLayer) continue;
                if (colliders[i].CompareTag("OCCLUDER/IGNORE")) continue;

                Bounds b = colliders[i].bounds;

                if (total.HasValue)
                {
                    Bounds t = total.Value;
                    t.Encapsulate(b);
                    total = t;
                }
                else
                    total = b;

                if (!colliders[i].isTrigger)
                {
                    if (totalNoTriggers.HasValue)
                    {
                        Bounds t = totalNoTriggers.Value;
                        t.Encapsulate(b);
                        totalNoTriggers = t;
                    }
                    else
                        totalNoTriggers = b;
                }

                if (!colliders[i].CompareTag("OCCLUDER/VIEW-IGNORE")) colliders[i].tag = "Untagged";
            }

            if (!total.HasValue) throw new UnityException("No valid ground colliders found!");

            return (total.Value, totalNoTriggers ?? total.Value);
        }
        #endif

        #endregion

        #region LAYER-PICKING
        protected virtual void PickLayerData() {
            if (!this.IsServer) throw new UnityException("PickCombinedLayerData can only be called on the server");
            byte currentRound = SDK_SETUP.GetCurrentRound?.Invoke() ?? 0;

            byte selectedLayer = 0;
            uint textureData = 0;

            // ----------- ROOM LAYER ---------------------
            if (this.layers is { Count: > 0 })
            {
                float totalWeight = 0f;
                foreach (RoomLayer layer in this.layers)
                    if (layer.round <= currentRound)
                        totalWeight += layer.weight;

                float chance = Random.value * totalWeight;
                for (int i = 0; i < this.layers.Count; i++)
                {
                    RoomLayer layer = this.layers[i];
                    if (layer.round > currentRound) continue;

                    if (chance < layer.weight)
                    {
                        selectedLayer = (byte)i;
                        break;
                    }

                    chance -= layer.weight;
                }
            }
            // -----------

            // ----------- TEXTURE LAYER ---------------------
            if (this.modifierTextures is { Count: > 0 })
            {
                int layerIndex = 0;
                int seed = this.TextureLayerSeed();
                if (seed != -1) Random.InitState(seed);

                foreach (KeyValuePair<VMFLayer, RoomTexture> kvp in this.modifierTextures)
                {
                    if (layerIndex >= 16) break;

                    List<Texture2D> tex = kvp.Value.texture;
                    byte selection = 0;
                    if (tex is { Count: > 0 }) selection = (byte)Random.Range(0, tex.Count);

                    textureData |= (uint)(selection & 0xFF) << (layerIndex * 8);
                    layerIndex++;
                }

                Random.InitState((int)DateTime.Now.Ticks);
            }
            // -----------

            this._combinedLayerData.Value = ((ulong)textureData << 8) | selectedLayer;
            this.ApplyLayerData(this._combinedLayerData.Value);
        }

        protected virtual void CleanUnselectedLayers(byte selectedLayer) {
            if (this.layers is not { Count: > 0 }) return;

            for (int i = 0; i < this.layers.Count; i++)
            {
                RoomLayer layer = this.layers[i];
                if (!layer.layer) continue;

                if (i == selectedLayer)
                {
                    layer.layer.SetActive(true);
                    continue;
                }

                entity_room_base.DestroyImmediate(layer.layer);
            }

            // SETUP VIS ----
            this._renderers = this.GetComponentsInChildren<Renderer>(false).AsValueEnumerable().Where(r => !string.Equals(r.name, "world", StringComparison.InvariantCultureIgnoreCase) && !r.CompareTag("OCCLUDER/IGNORE")).ToArray();
            
            this._lights = this.GetComponentsInChildren<Light>(false);
            this._decals = this.GetComponentsInChildren<DecalProjector>(false);
            // ---------------
        }

        protected virtual int TextureLayerSeed() { return -1; }

        protected virtual void ApplyLayerData(ulong combinedData) {
            byte selectedLayer = (byte)(combinedData & 0xFF);
            uint textureData = (uint)(combinedData >> 8);

            this.CleanUnselectedLayers(selectedLayer);
            this.ApplyTextureLayer(textureData);
        }

        protected byte GetSelectedLayerModel() {
            return (byte)(this._combinedLayerData.Value & 0xFF);
        }

        protected virtual void ApplyTextureLayer(uint encodedTextures) {
            if (this.modifierTextures is not { Count: > 0 }) return;

            Debug.Log("ApplyTextureLayer");
            
            Renderer[] renderers = this.GetComponentsInChildren<Renderer>(true)
                .AsValueEnumerable()
                .Where(r => r.name == "func_wall_layer" || r.transform.parent == this.transform)
                .ToArray();

            VMFLayerMaterial[] vmfMaterials = this.GetComponentsInChildren<VMFLayerMaterial>(true);
            if (renderers.Length == 0 && vmfMaterials.Length == 0) return;

            Shader shaderSingle = Shader.Find("FailCake/VMF/VMFLitLayer");
            if (!shaderSingle) throw new UnityException("Shader 'FailCake/VMF/VMFLitLayer' not found!");

            List<Material> validMaterials = new List<Material>();
            foreach (Renderer r in renderers)
            {
                if (!r) continue;

                foreach (Material mat in r.materials)
                    if (mat && mat.shader == shaderSingle &&
                        mat.HasTexture(entity_room_base.MainTexture) && mat.HasFloat(entity_room_base.Layer))
                        validMaterials.Add(mat);
            }

            Dictionary<VMFLayer, IGrouping<VMFLayer, VMFLayerMaterial>> vmfMaterialsByLayer = vmfMaterials.AsValueEnumerable()
                .Where(v => v)
                .GroupBy(v => v.layer).ToDictionary(g => g.Key, g => g);

            int layerIndex = 0;
            foreach (KeyValuePair<VMFLayer, RoomTexture> kvp in this.modifierTextures)
            {
                if (layerIndex >= 4) break; // Max 4 layers

                VMFLayer layer = kvp.Key;
                List<Texture2D> textures = kvp.Value.texture;

                byte textureIndex = (byte)((encodedTextures >> (layerIndex * 8)) & 0xFF);
                if (textures is { Count: > 0 } && textureIndex < textures.Count)
                {
                    Texture2D selectedTexture = textures[textureIndex];
                    if (selectedTexture)
                    {
                        float targetLayer = (float)layer;
                        foreach (Material mat in validMaterials)
                            if (Mathf.Approximately(mat.GetFloat(entity_room_base.Layer), targetLayer))
                                mat.SetTexture(entity_room_base.MainTexture, selectedTexture);
                    }
                }
                // -------

                // Material ---
                if (vmfMaterialsByLayer.TryGetValue(layer, out IGrouping<VMFLayer, VMFLayerMaterial> layerMaterials))
                    foreach (VMFLayerMaterial vmfLayerMaterial in layerMaterials)
                        vmfLayerMaterial.materialType = kvp.Value.material;
                // ---------

                layerIndex++;
            }
        }

        #endregion

        #if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) return;

            NetworkObject net = this.NetworkObject;
            if (!net) return;

            PropertyInfo ownershipProperty = typeof(NetworkObject).GetProperty("Ownership", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ownershipProperty != null) ownershipProperty.SetValue(net, NetworkObject.OwnershipStatus.SessionOwner);

            net.SynchronizeTransform = true;
            net.SpawnWithObservers = true;
            net.ActiveSceneSynchronization = false;
            net.SceneMigrationSynchronization = false;
            net.DontDestroyWithOwner = false;
            net.DestroyWithScene = true;
            net.SyncOwnerTransformWhenParented = false;
            net.AutoObjectParentSync = false;
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = UnityEngine.Color.blueViolet;
            Gizmos.DrawWireCube(this._BOUNDS_.center, this._BOUNDS_.size);
        }
        #endif

        #endregion
    }
}
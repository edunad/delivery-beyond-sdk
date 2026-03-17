namespace HyenaQuest
{
    public class entity_room : entity_room_base
    {
        #region PRIVATE

        private int _roomDistance;

        #endregion

        protected override int TextureLayerSeed() { return SDK_SETUP.GetSeed?.Invoke() ?? -1; }

        public entity_interior_exit[] GetInteriorExits() {
            return this.GetComponentsInChildren<entity_interior_exit>(true); // Cannot be cached
        }
    }
}
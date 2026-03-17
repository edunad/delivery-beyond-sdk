#region

using SaintsField;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public class entity_room_interior : entity_room_base
    {
        #region PRIVATE
        private bool _isFlipped;
        #endregion

        public void SetFlip(bool flip) {
            this._isFlipped = flip;
        }

        public bool IsRoomFlipped() { return this._isFlipped; }
    }
}
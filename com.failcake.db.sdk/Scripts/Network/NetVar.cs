#region

using System;
using System.Reflection;
using Unity.Netcode;

#endregion

namespace HyenaQuest
{
    [Serializable, GenerateSerializationForGenericParameter(0)]
    public class NetVar<T> : NetworkVariable<T>
    {
        #region PRIVATE

        private readonly T _initialValue;
        private static FieldInfo _previousValueField;

        #endregion

        public void RegisterOnValueChanged(OnValueChangedDelegate action) {
            this.OnValueChanged += action;
            this.OnValueChanged?.Invoke(this._initialValue, this.Value);
        }

        public void SetSpawnValue(T value) {
            this.Value = value;
        }

        public override void SetDirty(bool isDirty) {
            if (this.GetBehaviour()) base.SetDirty(isDirty);
        }

        public virtual T PrevValue {
            get {
                if (NetVar<T>._previousValueField == null)
                    NetVar<T>._previousValueField = typeof(NetworkVariable<T>).GetField(
                        "m_PreviousValue",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );

                return (T)NetVar<T>._previousValueField.GetValue(this);
            }
        }


        public NetVar(T value = default(T),
            NetworkVariableReadPermission readPerm = NetVar<T>.DefaultReadPerm,
            NetworkVariableWritePermission writePerm = NetVar<T>.DefaultWritePerm)
            : base(value, readPerm, writePerm) {
            this._initialValue = value;
        }
    }
}
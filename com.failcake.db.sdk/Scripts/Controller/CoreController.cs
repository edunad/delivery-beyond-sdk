#region

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

#endregion

namespace HyenaQuest
{
    public interface IController { }

    public abstract class MonoController<T> : MonoBehaviour, IController where T : MonoController<T>
    {
        public static T Instance { get; private set; }

        public void Awake() {
            MonoController<T>.Instance = (T)this;
            CoreController.Register(this);
        }

        public void OnDestroy() {
            if (MonoController<T>.Instance != this) return;
            CoreController.Unregister<T>();
            MonoController<T>.Instance = null;
        }
    }

    public abstract class NetController<T> : NetworkBehaviour, IController where T : NetController<T>
    {
        public static T Instance { get; private set; }

        public void Awake() {
            NetController<T>.Instance = (T)this;
            CoreController.Register(this);
        }

        public override void OnDestroy() {
            if (NetController<T>.Instance == this)
            {
                CoreController.Unregister<T>();
                NetController<T>.Instance = null;
            }

            base.OnDestroy();
        }
    }

    [DisallowMultipleComponent, DefaultExecutionOrder((int)ScriptOrder.MAIN - 100)]
    public class CoreController : MonoBehaviour
    {
        #region PRIVATE STATIC

        private static readonly Dictionary<Type, IController> _controllers = new Dictionary<Type, IController>();
        private static readonly Dictionary<Type, List<Delegate>> _pendingCallbacks = new Dictionary<Type, List<Delegate>>();

        #endregion

        public void Awake() {
            CoreController.DontDestroyOnLoad(this.gameObject);
        }

        public static void Register<T>(T controller) where T : IController {
            Type type = controller.GetType();

            if (!CoreController._controllers.TryAdd(type, controller)) throw new UnityException($"Controller of type {type.Name} is already registered");
            if (!CoreController._pendingCallbacks.TryGetValue(type, out List<Delegate> callbacks)) return;
            foreach (Delegate callback in callbacks) callback.DynamicInvoke(controller);

            CoreController._pendingCallbacks.Remove(type);
        }

        public static void Unregister<T>() where T : IController {
            Type type = typeof(T);
            if (!CoreController._controllers.Remove(type, out IController _)) throw new UnityException($"Tried to unregister {type.Name} but it wasn't registered");
        }

        public static void WaitFor<T>(Action<T> onComplete) where T : IController {
            Type type = typeof(T);

            if (CoreController._controllers.TryGetValue(type, out IController controller))
            {
                onComplete((T)controller);
                return;
            }

            if (!CoreController._pendingCallbacks.ContainsKey(type)) CoreController._pendingCallbacks[type] = new List<Delegate>();
            CoreController._pendingCallbacks[type].Add(onComplete);
        }

        #region PRIVATE

        private void OnDestroy() {
            CoreController._controllers.Clear();
            CoreController._pendingCallbacks.Clear();
        }

        #endregion
    }
}
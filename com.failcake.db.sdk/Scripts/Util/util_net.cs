#region

using System;

#endregion

namespace HyenaQuest
{
    /*[Aspect(Scope.PerInstance), Injection(typeof(Server))]
    public class Server : Attribute
    {
        [Advice(Kind.Before, Targets = Target.Method)]
        public void OnMethodExecuting() {
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) throw new UnityException("Server only");
        }
    }

    [Aspect(Scope.PerInstance), Injection(typeof(Client))]
    public class Client : Attribute
    {
        [Advice(Kind.Before, Targets = Target.Method)]
        public void OnMethodExecuting() {
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsClient) throw new UnityException("Client only");
        }
    }*/

    [AttributeUsage(AttributeTargets.Method)]
    public class ServerAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class SharedAttribute : Attribute { }
}
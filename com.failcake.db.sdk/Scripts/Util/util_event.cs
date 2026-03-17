#region

using System;

#endregion

namespace HyenaQuest
{
    public abstract class GameEventBase
    {
        protected bool _hasLastValues;
    }

    public class GameEvent : GameEventBase
    {
        private event Action OnEvent;

        public void Invoke() {
            this._hasLastValues = true;
            this.OnEvent?.Invoke();
        }

        public static GameEvent operator +(GameEvent gameEvent, Action listener) {
            if (gameEvent._hasLastValues) listener();
            gameEvent.OnEvent += listener;
            return gameEvent;
        }

        public static GameEvent operator -(GameEvent gameEvent, Action listener) {
            gameEvent.OnEvent -= listener;
            return gameEvent;
        }
    }

    public class GameEvent<T1> : GameEventBase
    {
        private event Action<T1> OnEvent;
        private T1 _lastParam;

        public void Invoke(T1 param1) {
            this._lastParam = param1;
            this._hasLastValues = true;
            this.OnEvent?.Invoke(param1);
        }

        public static GameEvent<T1> operator +(GameEvent<T1> gameEvent, Action<T1> listener) {
            if (gameEvent._hasLastValues) listener(gameEvent._lastParam);
            gameEvent.OnEvent += listener;
            return gameEvent;
        }

        public static GameEvent<T1> operator -(GameEvent<T1> gameEvent, Action<T1> listener) {
            gameEvent.OnEvent -= listener;
            return gameEvent;
        }
    }

    public class GameEvent<T1, T2> : GameEventBase
    {
        private event Action<T1, T2> OnEvent;
        private (T1, T2) _lastParams;

        public void Invoke(T1 param1, T2 param2) {
            this._lastParams = (param1, param2);
            this._hasLastValues = true;
            this.OnEvent?.Invoke(param1, param2);
        }

        public static GameEvent<T1, T2> operator +(GameEvent<T1, T2> gameEvent, Action<T1, T2> listener) {
            if (gameEvent._hasLastValues) listener(gameEvent._lastParams.Item1, gameEvent._lastParams.Item2);
            gameEvent.OnEvent += listener;
            return gameEvent;
        }

        public static GameEvent<T1, T2> operator -(GameEvent<T1, T2> gameEvent, Action<T1, T2> listener) {
            gameEvent.OnEvent -= listener;
            return gameEvent;
        }
    }

    public class GameEvent<T1, T2, T3> : GameEventBase
    {
        private event Action<T1, T2, T3> OnEvent;
        private (T1, T2, T3) _lastParams;

        public void Invoke(T1 param1, T2 param2, T3 param3) {
            this._lastParams = (param1, param2, param3);
            this._hasLastValues = true;
            this.OnEvent?.Invoke(param1, param2, param3);
        }

        public static GameEvent<T1, T2, T3> operator +(GameEvent<T1, T2, T3> gameEvent, Action<T1, T2, T3> listener) {
            if (gameEvent._hasLastValues) listener(gameEvent._lastParams.Item1, gameEvent._lastParams.Item2, gameEvent._lastParams.Item3);
            gameEvent.OnEvent += listener;
            return gameEvent;
        }

        public static GameEvent<T1, T2, T3> operator -(GameEvent<T1, T2, T3> gameEvent, Action<T1, T2, T3> listener) {
            gameEvent.OnEvent -= listener;
            return gameEvent;
        }
    }

    public class GameEvent<T1, T2, T3, T4> : GameEventBase
    {
        private event Action<T1, T2, T3, T4> OnEvent;
        private (T1, T2, T3, T4) _lastParams;

        public void Invoke(T1 param1, T2 param2, T3 param3, T4 param4) {
            this._lastParams = (param1, param2, param3, param4);
            this._hasLastValues = true;
            this.OnEvent?.Invoke(param1, param2, param3, param4);
        }

        public static GameEvent<T1, T2, T3, T4> operator +(GameEvent<T1, T2, T3, T4> gameEvent, Action<T1, T2, T3, T4> listener) {
            if (gameEvent._hasLastValues) listener(gameEvent._lastParams.Item1, gameEvent._lastParams.Item2, gameEvent._lastParams.Item3, gameEvent._lastParams.Item4);
            gameEvent.OnEvent += listener;
            return gameEvent;
        }

        public static GameEvent<T1, T2, T3, T4> operator -(GameEvent<T1, T2, T3, T4> gameEvent, Action<T1, T2, T3, T4> listener) {
            gameEvent.OnEvent -= listener;
            return gameEvent;
        }
    }
}
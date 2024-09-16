using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

namespace GameManagement
{
    public abstract class GameRule : NetworkBehaviour
    {
        public event Action OnGameStarted;
        public virtual void OnGameStart()
        {
            OnGameStarted?.Invoke();
        }

        public event Action OnGameUpdated;
        public virtual void OnGameUpdate()
        {
            OnGameUpdated?.Invoke();
        }

        public event Action OnGameEnded;
        public virtual void OnGameEnd()
        {
            OnGameEnded?.Invoke();
        }
    }
}
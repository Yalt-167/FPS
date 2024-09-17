using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

namespace GameManagement
{
    public abstract class GameRule : NetworkBehaviour
    {
        #region Game Start Handling

        public event Action OnGameStartedServerSide;
        public event Action OnGameStartedClientSide;

        [Rpc(SendTo.Server)]
        public virtual void OnGameStartServerRpc()
        {
            OnGameStartedServerSide?.Invoke();

            OnGameStartClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        protected virtual void OnGameStartClientRpc()
        {
            OnGameStartedClientSide?.Invoke();
        }

        #endregion

        #region Game Update Handling

        public event Action OnGameUpdatedServerSide;
        public event Action OnGameUpdatedClientSide;

        [Rpc(SendTo.Server)]
        public virtual void OnGameUpdateServerRpc()
        {
            OnGameUpdatedServerSide?.Invoke();

            OnGameUpdateClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        protected virtual void OnGameUpdateClientRpc()
        {
            OnGameUpdatedClientSide?.Invoke();
        }

        #endregion

        #region Game End Handling

        public event Action OnGameEndedServerSide;
        public event Action OnGameEndedClientSide;

        [Rpc(SendTo.Server)]
        public virtual void OnGameEndServerRpc()
        {
            OnGameEndedServerSide?.Invoke();

            OnGameEndClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        protected virtual void OnGameEndClientRpc()
        {
            OnGameEndedClientSide?.Invoke();
        } 

        #endregion
    }
}
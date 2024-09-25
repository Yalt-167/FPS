//#define DEV_BUILD


using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

namespace GameManagement
{
    public interface IGameRule
    {
        public IGameRuleState GameRuleState { get; }
#if DEV_BUILD
        public bool DebugGameRuleUpdateCalls { get; }
#endif

        #region Game Start Handling

        public event Action OnGameStartedServerSide;
        public event Action OnGameStartedClientSide;

        public void StartGameServerSide();

        public void StartGameClientSide();

        #endregion

        #region Game Update Handling

        public event Action OnGameUpdatedServerSide;
        public event Action<IClientSideGameRuleUpdateParam> OnGameUpdatedClientSide;

        public IClientSideGameRuleUpdateParam UpdateGameServerSide();

        public WinInfos UpdateGameClientSide(IClientSideGameRuleUpdateParam param);

        #endregion

        #region Game End Handling

        public event Action OnGameEndedServerSide;
        public event Action OnGameEndedClientSide;

        public void EndGameServerSide(WinInfos winInfos);

        public void EndGameClientSide(WinInfos winInfos);

        #endregion
    }
}
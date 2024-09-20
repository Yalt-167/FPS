using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace GameManagement
{
    public sealed class TeamFightGameRule : MonoBehaviour, IGameRule // might become a network behaviour so some values** cannot be tampered with
    {
        public IGameRuleState GameRuleState => TeamFightGameRuleState;
        private TeamFightGameRuleState TeamFightGameRuleState; // **

        #region Start

        public event Action OnGameStartedServerSide;
        public event Action OnGameStartedClientSide;

        public void StartGameServerSide()
        {
            OnGameStartedServerSide?.Invoke();

            Debug.Log("TeamFight was started ServerSide");
        }

        public void StartGameClientSide()
        {
            OnGameStartedClientSide?.Invoke();

            Debug.Log("TeamFight was started ClientSide");
        }

        #endregion

        #region Update

        public event Action OnGameUpdatedServerSide;
        public event Action<IClientSideGameRuleUpdateParam> OnGameUpdatedClientSide;

        public IClientSideGameRuleUpdateParam UpdateGameServerSide()
        {
            OnGameUpdatedServerSide?.Invoke();
            Debug.Log("TeamFight was updated ServerSide");

            //TeamFightGameRuleState // edit value as needed

            return null; // new TeamFightClientSideUpdateParam(null);
        }

        public WinInfos UpdateGameClientSide(IClientSideGameRuleUpdateParam param) // client should not tamper with win info
        {
            OnGameUpdatedClientSide?.Invoke(param);
            Debug.Log("TeamFight was updated ClientSide");

            var winInfo = TeamFightGameRuleState.WinInfos;
            winInfo.WinningTeamNumber = Input.GetKeyDown(KeyCode.Return) ? 1 : 0;
            TeamFightGameRuleState.WinInfos = winInfo;

            return TeamFightGameRuleState.WinInfos;
        }

        #endregion

        #region End

        public event Action OnGameEndedServerSide;
        public event Action OnGameEndedClientSide;

        public void EndGameServerSide(WinInfos winInfos)
        {
            OnGameEndedServerSide?.Invoke();
            Debug.Log("Teamfight was ended ServerSide");
        }

        public void EndGameClientSide(WinInfos winInfos)
        {
            OnGameEndedClientSide?.Invoke();
            Debug.Log("Teamfight was ended ClientSide");
        }

        #endregion
    }
}
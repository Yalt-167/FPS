#define DEV_BUILD

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

namespace GameManagement
{
    public sealed class TeamFightGameRule : MonoBehaviour, IGameRule // might become a network behaviour so some values** cannot be tampered with
    {
#if DEV_BUILD
        private static TeamFightGameRule Instance { get; set; }

#endif
        public IGameRuleState GameRuleState => TeamFightGameRuleState;
        private TeamFightGameRuleState TeamFightGameRuleState; // **

#if DEV_BUILD

        public bool DebugGameRuleUpdateCalls { get; private set; }

        [UnityEditor.MenuItem("Developer/Debug/ToggleGameRuleUpdateCalls")]
        public static void ToggleGameRuleUpdateCalls()
        {
            Instance.DebugGameRuleUpdateCalls = !Instance.DebugGameRuleUpdateCalls;
        }

        private void Awake()
        {
            Instance = this;
        }

#endif
        #region Start

        public event Action OnGameStartedServerSide;
        public event Action OnGameStartedClientSide;

        public void StartGameServerSide()
        {
            OnGameStartedServerSide?.Invoke();

#if DEV_BUILD
            if (Instance.DebugGameRuleUpdateCalls)
            { 
                Debug.Log("TeamFight was started ServerSide");
            }
#endif
        }

        public void StartGameClientSide()
        {
            OnGameStartedClientSide?.Invoke();
            
#if DEV_BUILD
            if (Instance.DebugGameRuleUpdateCalls)
            {
                Debug.Log("TeamFight was started ClientSide");
            }
#endif
        }

        #endregion

        #region Update

        public event Action OnGameUpdatedServerSide;
        public event Action<IClientSideGameRuleUpdateParam> OnGameUpdatedClientSide;

        public IClientSideGameRuleUpdateParam UpdateGameServerSide()
        {
            OnGameUpdatedServerSide?.Invoke();

#if DEV_BUILD
            if (Instance.DebugGameRuleUpdateCalls)
            {
                Debug.Log("TeamFight was updated ServerSide");
            }
#endif
            

            //TeamFightGameRuleState // edit value as needed

            return null; // new TeamFightClientSideUpdateParam(null);
        }

        public WinInfos UpdateGameClientSide(IClientSideGameRuleUpdateParam param) // client should not tamper with win info
        {
            OnGameUpdatedClientSide?.Invoke(param);

#if DEV_BUILD
            if (Instance.DebugGameRuleUpdateCalls)
            {
                Debug.Log("TeamFight was updated ClientSide");
            }
#endif
            
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

#if DEV_BUILD
            if (Instance.DebugGameRuleUpdateCalls)
            {
                Debug.Log("Teamfight was ended ServerSide");
            }
#endif
        }

        public void EndGameClientSide(WinInfos winInfos)
        {
            OnGameEndedClientSide?.Invoke();

#if DEV_BUILD
            if (Instance.DebugGameRuleUpdateCalls)
            {
                Debug.Log("Teamfight was ended ClientSide");
            }
#endif
        }

        #endregion
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace GameManagement
{
    public sealed class TeamFightGameRule : IGameRule
    {
        public IGameRuleState GameRuleState => TeamFightGameRuleState;
        private readonly TeamFightGameRuleStat TeamFightGameRuleState;

        #region Start

        public event Action OnGameStartedServerSide;
        public event Action OnGameStartedClientSide;

        public void StartGameServerSide()
        {
            OnGameStartedServerSide?.Invoke();

            Debug.Log("TeamFight was started serverside");
        }

        public void StartGameClientSide()
        {
            OnGameStartedClientSide?.Invoke();

            Debug.Log("TeamFight was started clientside");
        }

        #endregion

        #region Update

        public event Action OnGameUpdatedServerSide;
        public event Action<IClientSideGameRuleUpdateParam> OnGameUpdatedClientSide;

        public IClientSideGameRuleUpdateParam UpdateGameServerSide()
        {
            OnGameUpdatedServerSide?.Invoke();
            Debug.Log("TeamFight was updated serverside");

            return null;
        }

        public WinInfos UpdateGameClientSide(IClientSideGameRuleUpdateParam param)
        {
            OnGameUpdatedClientSide?.Invoke(param);
            Debug.Log("TeamFight was updated clientside");

            return TeamFightGameRuleState.WinInfos;
        }

        #endregion

        #region End

        public event Action OnGameEndedServerSide;
        public event Action OnGameEndedClientSide;

        public void EndGameServerSide(WinInfos winInfos)
        {
            OnGameEndedServerSide?.Invoke();
            Debug.Log("Teamfight was ended serverside");
        }

        public void EndGameClientSide(WinInfos winInfos)
        {
            OnGameEndedClientSide?.Invoke();
            Debug.Log("Teamfight was ended clientside");
        }

        #endregion
    }

    [Serializable]
    public struct TeamFightClientSideUpdateParam : IClientSideGameRuleUpdateParam
    {
        //public void Deserialize(ref FastBufferReader reader)
        //{
        //    //reader.ReadValueSafe(out value)
        //}

        //public void Serialize(ref FastBufferWriter writer)
        //{
        //    //writer.WriteValueSafe();
        //}
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            //serializer.SerializeValue(ref Value);
        }
    }

    public struct TeamFightGameRuleStat : IGameRuleState
    {
        public WinInfos WinInfos => throw new NotImplementedException();
    }
}
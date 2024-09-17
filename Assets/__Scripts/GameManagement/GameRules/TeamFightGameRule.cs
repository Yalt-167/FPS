using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameManagement
{
    public sealed class TeamFightGameRule : GameRule
    {
        [Rpc(SendTo.Server)]
        public override void OnGameStartServerRpc()
        {
            base.OnGameStartServerRpc();

            Debug.Log("TeamFight was started serverside");
        }

        //[Rpc(SendTo.ClientsAndHost)]
        //protected override void OnGameStartClientRpc()
        //{
        //    base.OnGameStartClientRpc();
        //    Debug.Log("TeamFight was started clientside");
        //}


        [Rpc(SendTo.Server)]
        public override void OnGameUpdateServerRpc()
        {
            base.OnGameUpdateServerRpc();
            Debug.Log("TeamFight was updated serverside");
        }

        //[Rpc(SendTo.ClientsAndHost)]
        //protected override void OnGameUpdateClientRpc()
        //{
        //    base.OnGameUpdateClientRpc();
        //    Debug.Log("TeamFight was updated clientside");
        //}

        [Rpc(SendTo.Server)]
        public override void OnGameEndServerRpc()
        {
            base.OnGameEndServerRpc();
            Debug.Log("Teamfight was ended serverside");
        }

        //[Rpc(SendTo.ClientsAndHost)]
        //protected override void OnGameEndClientRpc()
        //{
        //    base.OnGameEndClientRpc();
        //    Debug.Log("Teamfight was ended clientside");
        //}
    }
}
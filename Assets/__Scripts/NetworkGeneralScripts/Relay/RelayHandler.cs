using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Video;


namespace RelayHandling
{

    public class RelayHandler : MonoBehaviour
    {
        public async void CreateRelay(int slots)
        {
			Allocation allocation;
            string joinCode;
			try
			{
				allocation = await RelayService.Instance.CreateAllocationAsync(slots);
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			}
			catch (RelayServiceException exception)
			{
				Debug.Log(exception.Message);
				return;
			}

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
				allocation.RelayServer.IpV4,
				(ushort) allocation.RelayServer.Port,
				allocation.AllocationIdBytes,
				allocation.Key,
				allocation.ConnectionData
			);

			NetworkManager.Singleton.StartServer();

			Debug.Log("Successfully created relay");
        }


		public async void JoinRelay(string joinCode)
		{
			JoinAllocation joinAllocation;
			try
			{
				joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
			}
			catch (RelayServiceException exception)
			{
                Debug.Log(exception.Message);
                return ;
			}



			NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
				joinAllocation.RelayServer.IpV4,
				(ushort) joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData
			);

			NetworkManager.Singleton.StartClient();

            Debug.Log("Successfully joined relay");
        }
    }

}
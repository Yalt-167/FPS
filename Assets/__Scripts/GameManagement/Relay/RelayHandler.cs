#define HEADLESS_ARCHITECTURE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;



namespace RelayHandling
{
	public class RelayHandler : MonoBehaviour
	{
		public int Slots;
		public string JoinCode;
		public bool LaunchAsHost;
		public async Task<string> CreateRelay(int slots, bool lauchAsHost)
		{
			Allocation allocation;
			try
			{
				allocation = await RelayService.Instance.CreateAllocationAsync(slots);
				JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			}
			catch (RelayServiceException exception)
			{
				Debug.Log(exception.Message);
				return string.Empty;
			}

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
				allocation.RelayServer.IpV4,
				(ushort)allocation.RelayServer.Port,
				allocation.AllocationIdBytes,
				allocation.Key,
				allocation.ConnectionData
			);

			_ = lauchAsHost ? NetworkManager.Singleton.StartHost() : NetworkManager.Singleton.StartServer();

			Debug.Log("Successfully created relay");
			return JoinCode;
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
				return;
			}



			NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
				joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData
			);

			NetworkManager.Singleton.StartClient();

			Debug.Log("Successfully joined relay");
		}

		public async void GetRegions()
		{
			List<Region> regions;

			try
			{
				regions = await RelayService.Instance.ListRegionsAsync();
			}
			catch (RelayServiceException exception)
			{
				Debug.Log(exception.Message);
				return;
			}

			foreach (Region region in regions)
			{
				Debug.Log(region.Description);
			}
		}
	}
}
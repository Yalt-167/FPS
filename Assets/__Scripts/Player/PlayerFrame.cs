using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;



/// <summary>
/// Class that links all the player s classes for better communication between scripts and ease of access
/// </summary>
[DefaultExecutionOrder(-98)]
public class PlayerFrame : NetworkBehaviour
{
    private bool nameSelected = false;
    [field: SerializeField] public ChampionStats ChampionStats { get; set; }

    private PlayerCombat playerCombat;
    private WeaponHandler weaponHandler;

    private PlayerHealthNetworked playerHealth;

    private PlayerMovement playerMovement;

    private HandlePlayerNetworkBehaviour handlePlayerNetworkBehaviour;


    public ushort PlayerID;
    public ushort TeamID => playerHealth.TeamID;

    public bool Alive => playerHealth.Alive;

    public void SetPlayerID(ushort playerID)
    {
        PlayerID = playerID;
    }


    public override void OnNetworkSpawn()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerCombat.InitPlayerFrame(this);

        weaponHandler = GetComponent<WeaponHandler>();
        weaponHandler.InitPlayerFrame(this);

        playerHealth = GetComponent<PlayerHealthNetworked>();
        playerHealth.InitPlayerFrame(this);

        playerMovement = GetComponent<PlayerMovement>();
        playerMovement.InitPlayerFrame(this);

        handlePlayerNetworkBehaviour = GetComponent<HandlePlayerNetworkBehaviour>();
        handlePlayerNetworkBehaviour.InitPlayerFrame(this);
        handlePlayerNetworkBehaviour.ManageFiles();

        StartCoroutine(SelectNickname());
    }

    private IEnumerator SelectNickname()
    {
        var name = gameObject.AddComponent<PlayerNameSelector>();

        yield return new WaitUntil(() => nameSelected);
    }

    public void NicknameSelected()
    {
        nameSelected = true;
    }
}

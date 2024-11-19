#define USING_RUNTIME_WEAPON_DATA

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

using Projectiles;
using GameManagement;
using WeaponHandling;




[Serializable]
public sealed class WeaponHandler : NetworkBehaviour
{
    #region References

    //[SerializeField] private NetworkVariable<WeaponScriptableObject> currentWeapon = new NetworkVariable<WeaponScriptableObject>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public WeaponScriptableObject CurrentWeapon =>
#if USING_RUNTIME_WEAPON_DATA
        weaponsGatherer[currentWeaponIndex.Value].Weapon;
#else
        CurrentWeapon;

#endif
    private NetworkVariable<int> currentWeaponIndex = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private PlayerWeaponsGatherer weaponsGatherer;

    private bool IsInitialized ;



    private CrosshairRecoil crosshairRecoil;
    public WeaponBehaviourGatherer<BarrelEnd> BarrelEnds;
    private WeaponBehaviourGatherer<WeaponADSGunMovement> weaponsADSGunMovements;
    private WeaponBehaviourGatherer<WeaponKickback> weaponsKickbacks;
    private WeaponBehaviourGatherer<WeaponSpread> weaponsSpreads;
    private WeaponADSFOV weaponADSFOV;



    [SerializeField] private LayerMask layersToHit;
    [SerializeField] private GameObject bulletTrailPrefab;

    private PlayerSettings playerSettings;
    private DamageLogSettings DamageLogsSettings => playerSettings.DamageLogSettings;
    [SerializeField] private DamageLogManager damageLogManager;
    private PlayerFrame ownerFrame;

#endregion

    #region Sound Setup

    private readonly List<AudioSource> audioSources = new();
    private int audioSourcePoolSize;
    private int currentAudioSourceIndex;
    private WeaponSounds currentWeaponSounds;

    #endregion

    #region Global Setup

    // eventually make this a NetworkVariable
    private bool switchedThisFrame;

    private NetworkVariable<float> timeLastShotFired = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> shotThisFrame = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<ushort> ammos = new NetworkVariable<ushort>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> canShoot = new NetworkVariable<bool>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);


    private Action shootingStyleMethod;
    private Action shootingRythmMethod;
    private Action<ShotInfos, INetworkSerializable> onHitWallMethod;

    public bool IsAiming { get; private set; }

    private static readonly float shootBuffer = .1f;
    private float lastShootPressed = float.NegativeInfinity;
    private bool HasBufferedShoot => lastShootPressed + shootBuffer > Time.time;

    #endregion


    #region Burst Setup

    private ushort bulletFiredthisBurst;

    #endregion

    #region RampUp Setup


    private float currentCooldownBetweenRampUpShots;
    private float CurrentCooldownBetweenRampUpShots
    {
        get
        {
            return currentCooldownBetweenRampUpShots;
        }
        set
        {
            currentCooldownBetweenRampUpShots = Mathf.Clamp(value, CurrentWeapon.RampUpStats.RampUpMinCooldownBetweenShots, CurrentWeapon.RampUpStats.RampUpMaxCooldownBetweenShots);
        }
    }


    #endregion

    #region Charge Setup

    private float timeStartedCharging;
    private bool holdingAttackKey;

    #endregion

    // methods

    #region Unity Handled


    private void Awake()
    {
        MyUtilities.NetworkUtility.CallWhenNetworkSpawned(this, Init);
    }

    private void FixedUpdate()
    {
        if (!IsInitialized) { return; }

        if (!IsServer) { return; }

        //_ = weaponsSpreads ?? throw new Exception("Nope");
        foreach (var weaponSpread in weaponsSpreads)
        {
            weaponSpread.HandleSpreadServerRpc();
        }

        crosshairRecoil.HandleRecoilServerRpc();

        foreach (var weaponKickback in weaponsKickbacks)
        {
            weaponKickback.HandleKickbackServerRpc();
        }


        if (shotThisFrame.Value) { return; }

        // ?do a delegate PostShotLogic() to handle such behaviour? (and juste do plain increment instead of this)
        CurrentCooldownBetweenRampUpShots *= CurrentWeapon.RampUpStats.RampUpCooldownRegulationMultiplier;
    }

    private void LateUpdate()
    {
        switchedThisFrame = false;

        if(!IsServer) { return; }

        LateUpdateServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void LateUpdateServerRpc()
    {
        shotThisFrame.Value = false;
    }

    #endregion

    #region Init
    private void Init()
    {
        InitPlayerLoadoutServerRpc();

        playerSettings = GetComponent<PlayerSettings>();
        crosshairRecoil = GetComponent<CrosshairRecoil>();
        weaponADSFOV = transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<WeaponADSFOV>();

        //SetWeapon(weaponsGatherer[0].Weapon);
        SetWeapon(0);
        audioSourcePoolSize = 10; // (int)(Mathf.Max(currentWeaponSounds.ShootingSound.length, currentWeaponSounds.NearEmptyMagazineShootSound.length) / currentWeapon.CooldownBetweenShots);
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            audioSources.Add(gameObject.AddComponent<AudioSource>());
        }

        ownerFrame = GetComponent<PlayerFrame>();

        damageLogManager.UpdatePlayerSettings(DamageLogsSettings);

        IsInitialized = true;
    }

    #region Init Player Loadout

    [Rpc(SendTo.Server)]
    private void InitPlayerLoadoutServerRpc()
    {
        Debug.Log("I was");
        InitPlayerLoadoutClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InitPlayerLoadoutClientRpc()
    {
        Debug.Log("Called");
        weaponsGatherer = new PlayerWeaponsGatherer(
            new string[]
            {
                "Weapons/Shotguns/TestShotgun",
                "Weapons/SMGs/TestSMG",
                "Weapons/Rifles/TestRifle",
            }
        );
    }

    #endregion

    public void SetWeapon(int index)
    {
        SetWeaponServerRpc(index);
    }

    [Rpc(SendTo.Server)]
    private void SetWeaponServerRpc(int index)
    {
        currentWeaponIndex.Value = index;
        InitWeaponClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InitWeaponClientRpc()
    {
        InitWeapon();
    }

    private void InitWeapon()
    {
        BarrelEnds = new(GetComponentsInChildren<BarrelEnd>());

        weaponsADSGunMovements = new(GetComponentsInChildren<WeaponADSGunMovement>());
        foreach (var weaponADSGunMovement in weaponsADSGunMovements)
        {
            weaponADSGunMovement.SetupData(this);
        }

        weaponADSFOV.SetupData(this);

        crosshairRecoil.SetupData(this);

        weaponsKickbacks = new(GetComponentsInChildren<WeaponKickback>());
        foreach (var weaponKickback in weaponsKickbacks)
        {
            weaponKickback.SetupData(this);
        }


        weaponsSpreads = new(GetComponentsInChildren<WeaponSpread>());
        foreach (var weaponSpread in weaponsSpreads)
        {
            weaponSpread.SetupData(this);
        }

        currentWeaponSounds = CurrentWeapon.Sounds;

        InitWeaponServerRpc();
        switchedThisFrame = true;

        SetShootingStyle(); // the actual shooting logic
        SetShootingRythm(); // the rythm logic of the shots
        SetOnHitWallMethod();
    }

    [Rpc(SendTo.Server)]
    private void InitWeaponServerRpc()
    {
        timeLastShotFired.Value = float.MinValue;
        ammos.Value = CurrentWeapon.MagazineSize;
        canShoot.Value = true;
    }

    private void SetShootingStyle()
    {
        shootingStyleMethod = CurrentWeapon.ShootingStyle switch
        {
            // HERE
            //ShootingStyle.Single => currentWeapon.IsHitscan ? ExecuteSimpleHitscanShotClientRpc : ExecuteSimpleTravelTimeShotClientRpc,
            ShootingStyle.Single => CurrentWeapon.IsHitscan ? ExecuteSimpleHitscanShot : ExecuteSimpleTravelTimeShotClientRpc,

            //ShootingStyle.Shotgun => currentWeapon.IsHitscan ? ExecuteShotgunHitscanShotClientRpc :  ExecuteShotgunTravelTimeShotClientRpc,
            ShootingStyle.Shotgun => CurrentWeapon.IsHitscan ? ExecuteShotgunHitscanShot :  ExecuteShotgunTravelTimeShotClientRpc,

            _ => throw new Exception("This Shooting Style does not exist")
            ,
        };
    }

    private void SetShootingRythm()
    {
        shootingRythmMethod = CurrentWeapon.ShootingRythm switch
        {
            ShootingRythm.Single => RequestShotServerRpc,

            ShootingRythm.Burst => () =>
                {
                    StartCoroutine(ShootBurst(CurrentWeapon.BurstStats.BulletsPerBurst));
                }
            ,

            ShootingRythm.RampUp => () =>
                {
                    RequestShotServerRpc();
                    CurrentCooldownBetweenRampUpShots *= CurrentWeapon.RampUpStats.RampUpCooldownMultiplierPerShot;
                }
            ,

            ShootingRythm.Charge => () => { }
            ,

            _ => throw new Exception("This Shooting Rhythm does not exist")
            ,
        };
    }

    private void SetOnHitWallMethod()
    {
        onHitWallMethod = CurrentWeapon.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Classic => (_, _) => { },
            HitscanBulletActionOnHitWall.ThroughWalls => (_, _) => { },
            HitscanBulletActionOnHitWall.Explosive => ExplodeUponWallHit,
            HitscanBulletActionOnHitWall.BounceOnWalls => BounceUponWallHit,
            _ => (_, _) => { }
            ,
        };
    }

    #endregion

    #region Server Request & Validation

    public void UpdateState(bool holdingAttackKey_)
    {
        if (holdingAttackKey_ != holdingAttackKey)
        {
            holdingAttackKey = holdingAttackKey_;
            if (holdingAttackKey_) // just started pressing the attack key
            {
                lastShootPressed = Time.time;
                if (CurrentWeapon.ShootingRythm == ShootingRythm.Charge)
                {
                    StartCoroutine(ChargeShot());
                    return;
                }
            }
            //else  // just released the attack key
            //{

            //}

        }

        if (!(canShoot.Value && holdingAttackKey))
        {
            return;
        }

        shootingRythmMethod();
    }

    public void UpdateAimingState(bool shouldBeAiming)
    {
        IsAiming = shouldBeAiming; 
    }

    [Rpc(SendTo.Server)]
    public void _RequestShotServerRpc()
    {
        CheckCooldownsClientRpc();
    }
    [Rpc(SendTo.Server)]
    public void RequestShotServerRpc()
    {
        MyDebug.DebugUtility.LogMethodCall();

        if (timeLastShotFired.Value + GetRelevantCooldown() > Time.time) { return; }
        RequestShotCallbackClientRpc(false);

        if (ammos.Value <= 0) { return; }
        RequestShotCallbackClientRpc(true);
        shootingStyleMethod();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void RequestShotCallbackClientRpc(bool full)
    {
        Debug.Log($"Gone through {(full ? "" : "half")}");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CheckCooldownsClientRpc()
    {
        if (!IsOwner) { return; }

        if (timeLastShotFired.Value + GetRelevantCooldown() > Time.time) { return; }

        if (ammos.Value <= 0) { return; }

        shootingStyleMethod();
    }

    [Rpc(SendTo.Server)]
    public void RequestChargedShotServerRpc(float chargeRatio)
    {
        CheckChargedShotCooldownsClientRpc(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CheckChargedShotCooldownsClientRpc(float chargeRatio)
    {
        if (timeLastShotFired.Value + GetRelevantCooldown() > Time.time) { return; }

        if (CurrentWeapon.IsHitscan)
        {
            if (CurrentWeapon.ShootingStyle == ShootingStyle.Shotgun)
            {
                ExecuteChargedShotgunHitscanShotClientRpc(chargeRatio);
            }
            else
            {
                //ExecuteChargedHitscanShotClientRpc(chargeRatio);
                ExecuteChargedHitscanShot(chargeRatio);
            }
        }
        else
        {
            if (CurrentWeapon.ShootingStyle == ShootingStyle.Shotgun)
            {
                ExecuteChargedShotgunTravelTimeShotClientRpc(chargeRatio);
            }
            else
            {
                ExecuteChargedTravelTimeShotClientRpc(chargeRatio);
            }
        }
    }

    private float GetRelevantCooldown()
    {
        return CurrentWeapon.ShootingRythm switch
        {
            ShootingRythm.Single => CurrentWeapon.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == CurrentWeapon.BurstStats.BulletsPerBurst ? CurrentWeapon.CooldownBetweenShots : CurrentWeapon.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => CurrentWeapon.CooldownBetweenShots,
            _ => 0f,
        };
    }

    private IEnumerator ShootBurst(int bullets) // ANCHOR
    {
        if (ammos.Value <= 0) { yield break; }

        if (timeLastShotFired.Value + GetRelevantCooldown() > Time.time) { yield break; }

        bulletFiredthisBurst = 0;

        //canShoot = false;

        for (int i = 0; i < bullets; i++)
        {
            var timerStart = Time.time;

            yield return new WaitUntil(() => timerStart + CurrentWeapon.BurstStats.CooldownBetweenShotsOfBurst < Time.time || switchedThisFrame);

            if (switchedThisFrame) { yield break; }

            RequestShotServerRpc();
        }

        //canShoot = true;
    }

    private IEnumerator ChargeShot()
    {
        if (ammos.Value <= 0) { yield break; }

        if (timeLastShotFired.Value + GetRelevantCooldown() > Time.time) { yield break; }

        timeStartedCharging = Time.time;

        yield return new WaitWhile(() => holdingAttackKey);

        var timeCharged = Time.time - timeStartedCharging;
        var chargeRatio = timeCharged / CurrentWeapon.ChargeStats.ChargeDuration;
        if (chargeRatio >= CurrentWeapon.ChargeStats.MinChargeRatioToShoot)
        {
            var ammoConsumedByThisShot = (ushort)(CurrentWeapon.ChargeStats.AmmoConsumedByFullyChargedShot * chargeRatio);
            if (ammos.Value < ammoConsumedByThisShot)
            {
                RequestChargedShotServerRpc(ammos.Value / CurrentWeapon.ChargeStats.AmmoConsumedByFullyChargedShot);
            }
            else
            {
                RequestChargedShotServerRpc(chargeRatio);
            }
        }
    }

    #endregion

    [Rpc(SendTo.ClientsAndHost)]
    private void EmulateHitscanShotClientRpc(int barrelEndIndex, Vector3 endPoint)
    {
        var bulletTrail = Instantiate(bulletTrailPrefab, BarrelEnds[barrelEndIndex].transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        bulletTrail.Set(BarrelEnds[barrelEndIndex].transform.position, endPoint);

        PlayShootingSound(CurrentWeapon.AmmoLeftInMagazineToWarn > ammos.Value);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void EmulateHitscanShotsClientRpc(int barrelEndIndex, Vector3[] endPoints)
    {   
        for (int i = 0; i < endPoints.Length; i++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, BarrelEnds[barrelEndIndex].transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            bulletTrail.Set(BarrelEnds[barrelEndIndex].transform.position, endPoints[i]);
        }

        PlayShootingSound(CurrentWeapon.AmmoLeftInMagazineToWarn > ammos.Value);
    }

    #region Execute Shot

    [Obsolete("Should be replaced by the non-ClientRpc version")]
    private void UpdateOwnerSettingsUponShot()
    {
        Debug.Log("[UpdateOwnerSettingsUponShot]I was called tho I shouldn t");

        if (!IsOwner) { return; }

        PlayShootingSoundServerRpc(CurrentWeapon.AmmoLeftInMagazineToWarn >= ammos.Value);

        timeLastShotFired.Value = Time.time;
        //shotThisFrame = true;
        ammos.Value--;
        bulletFiredthisBurst++;
    }
    private void UpdateOwnerSettingsUponShotFromServer()
    {
        timeLastShotFired.Value = Time.time;
        shotThisFrame.Value = true;
        ammos.Value--;
        bulletFiredthisBurst++;
    }

    #region Execute Shot Hitscan

    private INetworkSerializable GetRelevantHitscanBulletSettings()
    {
        return CurrentWeapon.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Explosive => CurrentWeapon.HitscanBulletSettings.ExplodingBulletsSettings,
            HitscanBulletActionOnHitWall.BounceOnWalls => CurrentWeapon.HitscanBulletSettings.BouncingBulletsSettings,
            HitscanBulletActionOnHitWall.Classic => null,
            HitscanBulletActionOnHitWall.ThroughWalls => null,
            _ => null
        };
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleHitscanShotClientRpc()
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = weaponsSpreads[index].GetDirectionWithSpread(index);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    shootableComponent.ReactShot(
                        CurrentWeapon.Damage,
                        hit.point,
                        barrelEnd.transform.forward,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        CurrentWeapon.CanBreakThings
                    );
                }

                if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                onHitWallMethod(
                    new(
                        directionWithSpread,
                        hit,
                        NetworkObjectId,
                        new(CurrentWeapon)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(BarrelEnds.Current.transform.position, endPoint);

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        
        ApplyCrosshairRecoil();
        ApplySpread(index);
        ApplyKickback(index);
    }
    private void ExecuteSimpleHitscanShot()
    {
        //Debug.Log($"IsServer: {IsServer}");
        MyDebug.DebugUtility.LogMethodCall();
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);

        var directionWithSpread = weaponsSpreads[index].GetDirectionWithSpreadFromServer(index);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(
                    CurrentWeapon.Damage,
                    hit.point,
                    barrelEnd.transform.forward,
                    NetworkObjectId,
                    PlayerFrame.LocalPlayer.TeamNumber, // cannot work anymore
                    CurrentWeapon.CanBreakThings
                );
                

                if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                onHitWallMethod(
                    new(
                        directionWithSpread,
                        hit,
                        NetworkObjectId,
                        new(CurrentWeapon)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        EmulateHitscanShotClientRpc(index, endPoint);

        BarrelEnds.GoNext();

        ApplyCrosshairRecoilFromServer();
        ApplySpreadFromServer(index);
        ApplyKickbackFromServer(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunHitscanShotClientRpc() // do all that server side instead
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);

        //foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        foreach (var direction in weaponsSpreads[index].ComputeShotgunSpreadEnumerable(index))
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + direction * 100;
            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, CurrentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(
                            CurrentWeapon.Damage / CurrentWeapon.ShotgunStats.PelletsCount,
                            direction,
                            hit.point,
                            NetworkObjectId,
                            PlayerFrame.LocalPlayer.TeamNumber,
                            CurrentWeapon.CanBreakThings
                        );
                    }

                    if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hit.point;
                        break;
                    }
                }
                else
                {

                    onHitWallMethod(
                        new(
                            direction,
                            hit,
                            NetworkObjectId,
                            new(CurrentWeapon)
                        ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }

                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil();
        ApplyKickback(index);
    }
    private void ExecuteShotgunHitscanShot() // do all that server side instead
    {
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);
        var pelletIdx = 0;
        var endPoints = new Vector3[CurrentWeapon.ShotgunStats.PelletsCount];
        //foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpreadFromServer(index))
        foreach (var direction in weaponsSpreads[index].ComputeShotgunSpreadEnumerable(index))
        {
            endPoints[pelletIdx] = barrelEnd.transform.position + direction * 100;

            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, CurrentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot(
                        CurrentWeapon.Damage / CurrentWeapon.ShotgunStats.PelletsCount,
                        direction,
                        hit.point,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        CurrentWeapon.CanBreakThings
                    );
                    

                    if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoints[pelletIdx] = hit.point;
                        break;
                    }
                }
                else
                {
                    onHitWallMethod(
                        new(
                            direction,
                            hit,
                            NetworkObjectId,
                            new(CurrentWeapon)
                        ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoints[pelletIdx] = hit.point;
                        break;
                    }

                }

                pelletIdx++;
            }
        }
        
        EmulateHitscanShotsClientRpc(index, endPoints);

        BarrelEnds.GoNext();

        ApplyCrosshairRecoil();
        ApplyKickback(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedHitscanShotClientRpc(float chargeRatio)
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);
        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = weaponsSpreads[index].GetDirectionWithSpread(index);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    shootableComponent.ReactShot(
                        CurrentWeapon.Damage * chargeRatio,
                        hit.point,
                        barrelEnd.transform.forward,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        CurrentWeapon.CanBreakThings
                    );
                }

                if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                onHitWallMethod(
                    new(
                        directionWithSpread,
                        hit,
                        NetworkObjectId,
                        new(CurrentWeapon, chargeRatio)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(barrelEnd.transform.position, endPoint);

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplySpread(index, chargeRatio);
        ApplyKickback(index, chargeRatio);
    }
    private void ExecuteChargedHitscanShot(float chargeRatio)
    {
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);
        var directionWithSpread = weaponsSpreads[index].GetDirectionWithSpreadFromServer(index);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(
                    CurrentWeapon.Damage * chargeRatio,
                    hit.point,
                    barrelEnd.transform.forward,
                    NetworkObjectId,
                    PlayerFrame.LocalPlayer.TeamNumber,
                    CurrentWeapon.CanBreakThings
                );
             

                if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                onHitWallMethod(
                    new(
                        directionWithSpread,
                        hit,
                        NetworkObjectId,
                        new(CurrentWeapon, chargeRatio)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        EmulateHitscanShotClientRpc(index, endPoint);

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplySpread(index, chargeRatio);
        ApplyKickback(index, chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore
        

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);
        //foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        foreach (var direction in weaponsSpreads[index].ComputeShotgunSpreadEnumerable(index))
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + direction * 100;


            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, CurrentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(
                            CurrentWeapon.Damage / CurrentWeapon.ShotgunStats.PelletsCount * chargeRatio,
                            direction,
                            hit.point,
                            NetworkObjectId,
                            PlayerFrame.LocalPlayer.TeamNumber,
                            CurrentWeapon.CanBreakThings
                        );
                    }

                    if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hit.point;
                        break;
                    }
                }
                else
                {
                    onHitWallMethod(
                        new(
                            direction,
                            hit,
                            NetworkObjectId,
                            new(CurrentWeapon, chargeRatio)
                            ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }

                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplyKickback(index, chargeRatio);
    }
    private void ExecuteChargedShotgunHitscanShot(float chargeRatio)
    {
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);
        //foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpreadFromServer(index))
        foreach (var direction in weaponsSpreads[index].ComputeShotgunSpreadEnumerable(index))
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + direction * 100;


            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, CurrentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(
                            CurrentWeapon.Damage / CurrentWeapon.ShotgunStats.PelletsCount * chargeRatio,
                            direction,
                            hit.point,
                            NetworkObjectId,
                            PlayerFrame.LocalPlayer.TeamNumber,
                            CurrentWeapon.CanBreakThings
                        );
                    }

                    if (!CurrentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hit.point;
                        break;
                    }
                }
                else
                {
                    onHitWallMethod(
                        new(
                            direction,
                            hit,
                            NetworkObjectId,
                            new(CurrentWeapon, chargeRatio)
                            ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (CurrentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }

                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        BarrelEnds.GoNext();

        ApplyCrosshairRecoil(chargeRatio);
        ApplyKickback(index, chargeRatio);
    }

    #endregion

    #region Execute Shot TravelTime

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleTravelTimeShotClientRpc()
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);

        var projectile = Instantiate(
            CurrentWeapon.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.transform.position,
            Quaternion.LookRotation(weaponsSpreads[index].GetDirectionWithSpread(index))
            ).GetComponent<Projectile>();

        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        projectile.Init(
            CurrentWeapon.Damage,
            CurrentWeapon.TravelTimeBulletSettings.BulletSpeed,
            CurrentWeapon.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            CurrentWeapon.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            ownerFrame.TeamNumber
        );

        BarrelEnds.GoNext();

        if (IsOwner) { return; }

        ApplyCrosshairRecoil();
        ApplySpread(index);
        ApplyKickback(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunTravelTimeShotClientRpc()
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);

        //for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        //foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        foreach (var direction in weaponsSpreads[index].ComputeShotgunSpreadEnumerable(index))
        {

            var projectile = Instantiate(
                CurrentWeapon.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.transform.position,
                Quaternion.LookRotation(/*shotgunPelletsDirections[i]*/ direction)
            ).GetComponent<Projectile>();

            if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

            projectile.Init(
                CurrentWeapon.Damage / CurrentWeapon.ShotgunStats.PelletsCount,
                CurrentWeapon.TravelTimeBulletSettings.BulletSpeed,
                CurrentWeapon.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                CurrentWeapon.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                ownerFrame.TeamNumber
            );

        }

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil();
        ApplyKickback(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedTravelTimeShotClientRpc(float chargeRatio)
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out int index);

        var projectile = Instantiate(
            CurrentWeapon.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.transform.position,
            Quaternion.LookRotation(weaponsSpreads[index].GetDirectionWithSpread(index))
        ).GetComponent<Projectile>();

        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        projectile.Init(
            CurrentWeapon.Damage * chargeRatio,
            CurrentWeapon.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
            CurrentWeapon.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            CurrentWeapon.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            ownerFrame.TeamNumber
        );

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplySpread(index, chargeRatio);
        ApplyKickback(index, chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunTravelTimeShotClientRpc(float chargeRatio)
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = BarrelEnds.GetCurrentAndIndex(out var index);

        //for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        //foreach(var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        foreach(var direction in weaponsSpreads[index].ComputeShotgunSpreadEnumerable(index))
        {
            var projectile = Instantiate(
                CurrentWeapon.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.transform.position,
                Quaternion.LookRotation(/*shotgunPelletsDirections[i]*/direction)
            ).GetComponent<Projectile>();

            if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

            projectile.Init(
                CurrentWeapon.Damage / CurrentWeapon.ShotgunStats.PelletsCount * chargeRatio,
                CurrentWeapon.TravelTimeBulletSettings.BulletSpeed,
                CurrentWeapon.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                CurrentWeapon.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                ownerFrame.TeamNumber
            );
        }

        BarrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplyKickback(index, chargeRatio);
    }

    #endregion

    #endregion

    #region Hitscan Wall Hit Effects

    private void ExplodeUponWallHit(ShotInfos shotInfos, INetworkSerializable hitscanBulletEffectSettings_)
    {
        var hitscanBulletEffectSettings = (ExplodingHitscanBulletsSettings)hitscanBulletEffectSettings_;

        var inRange = Physics.OverlapSphere(shotInfos.Hit.point, hitscanBulletEffectSettings.ExplosionRadius);

        for (int i = 0; i < inRange.Length; i++)
        {
            if (inRange[i].TryGetComponent<IExplodable>(out var explodableComponent))
            {
                explodableComponent.ReactExplosion(
                    hitscanBulletEffectSettings.ExplosionDamage,
                    shotInfos.Hit.point,
                    shotInfos.AttackerNetworkID,
                    shotInfos.WeaponInfos.CanBreakThings
                );
            }
        }
    }

    private void BounceUponWallHit(ShotInfos shotInfos, INetworkSerializable hitscanBulletEffectSettings_)
    {
        var hitscanBulletEffectSettings = (BouncingHitscanBulletsSettings)hitscanBulletEffectSettings_;

        if (hitscanBulletEffectSettings.BouncesAmount == 0) { return; }

        var newShotDirection = MyUtilities.Utility.ReflectVector(shotInfos.ShotDirection, shotInfos.Hit.normal);

        var bulletTrail = Instantiate(bulletTrailPrefab, shotInfos.Hit.point, Quaternion.identity).GetComponent<BulletTrail>();
        var endPoint = shotInfos.Hit.point + newShotDirection * 100;

        var hits = Physics.RaycastAll(shotInfos.Hit.point, newShotDirection, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    shootableComponent.ReactShot(
                        shotInfos.WeaponInfos.Damage,
                        hit.point,
                        newShotDirection,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        shotInfos.WeaponInfos.CanBreakThings
                    );
                }

                if (!shotInfos.WeaponInfos.PierceThroughPlayers)
                {
                    endPoint = hit.point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                BounceUponWallHit(
                        new(
                            newShotDirection,
                            hit,
                            shotInfos.AttackerNetworkID,
                            new(CurrentWeapon)
                            ),
                        --hitscanBulletEffectSettings
                        );

                if (shotInfos.WeaponInfos.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(shotInfos.Hit.point, endPoint);
    }

    #endregion

    #region Travel Time Hit Effects

    private ProjectileOnHitWallBehaviour GetRelevantHitWallBehaviour()
    {
        return CurrentWeapon.TravelTimeBulletSettings.OnHitWallBehaviour switch
        {
            ProjectileBehaviourOnHitWall.Stop => new ProjectileStopOnHitWall(null),
            ProjectileBehaviourOnHitWall.Pierce => new ProjectilePierceOnHitWall(CurrentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallPierceParams),
            ProjectileBehaviourOnHitWall.Bounce => new ProjectileBounceOnHitWall(CurrentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallBounceParams),
            ProjectileBehaviourOnHitWall.Explode => new ProjectileExplodeOnHitWall(CurrentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }


    private ProjectileOnHitPlayerBehaviour GetRelevantHitPlayerBehaviour()
    {
        return CurrentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviour switch
        {
            ProjectileBehaviourOnHitPlayer.Stop => new ProjectileStopOnHitPlayer(),
            ProjectileBehaviourOnHitPlayer.Pierce => new ProjectilePierceOnHitPlayer(CurrentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerPierceParams),
            ProjectileBehaviourOnHitPlayer.Explode => new ProjectileExplodeOnHitPlayer(CurrentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }

    #endregion

    #region Reload

    public void Reload()
    {
        if (!CurrentWeapon.NeedReload) { return; }

        if (ammos.Value == CurrentWeapon.MagazineSize) { return; } // already fully loaded

        if (CurrentWeapon.TimeToReloadOneRound == 0f)
        {
            ExecuteReloadServerRpc();
        }
        else
        {
            ExecuteReloadRoundPerRoundServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void ExecuteReloadServerRpc()
    {
        StartCoroutine(ExecuteReload());
    }

    private IEnumerator ExecuteReload()
    {
        canShoot.Value = false;

        // PlayReloadAnimCientRpc();
        var timerStart = Time.time;
        yield return new WaitUntil(() => timerStart + CurrentWeapon.ReloadSpeed < Time.time || switchedThisFrame);
        
        canShoot.Value = true;

        if (switchedThisFrame) { yield break; }

        ammos.Value = CurrentWeapon.MagazineSize;
    }

    [Rpc(SendTo.Server)]
    private void ExecuteReloadRoundPerRoundServerRpc()
    {
        StartCoroutine(ExecuteReloadRoundPerRound());
    }

    private IEnumerator ExecuteReloadRoundPerRound()
    {
        var ammosToReload = CurrentWeapon.MagazineSize - ammos.Value;

        for (int i = 0; i < ammosToReload; i++)
        {
            var timerStart = Time.time;
            yield return new WaitUntil(
                () => timerStart + CurrentWeapon.TimeToReloadOneRound < Time.time ||
                switchedThisFrame || // pass that server side then
                HasBufferedShoot // pass that server side too
                ); // + buffer a shoot input that would interrupt the reload

            if (switchedThisFrame) { yield break; }

            if (HasBufferedShoot)
            {
                if (CurrentWeapon.ShootingRythm == ShootingRythm.Charge)
                {
                    StartCoroutine(ChargeShot());
                }
                else
                {
                    shootingRythmMethod();
                }

                yield break;
            }

            ammos.Value++;
        }
    }

    #endregion

    #region Handle Gun Behaviour

    private void ApplyCrosshairRecoil(float chargeRatio = 1)
    {
        crosshairRecoil.ApplyRecoilServerRpc(chargeRatio);
    }

    private void ApplyCrosshairRecoilFromServer(float chargeRatio = 1)
    {
        crosshairRecoil.ApplyRecoilFromServer(chargeRatio);
    }

    private void ApplyKickback(int idx, float chargeRatio = 1f)
    {
        weaponsKickbacks[idx].ApplyKickbackServerRpc(chargeRatio);
    }

    private void ApplyKickbackFromServer(int idx, float chargeRatio = 1f)
    {
        weaponsKickbacks[idx].ApplyKickbackFromServer(chargeRatio);
    }

    private void ApplySpread(int idx, float chargeRatio = 1f)
    {
        weaponsSpreads[idx].ApplySpreadServerRpc(chargeRatio);
    }

    private void ApplySpreadFromServer(int idx, float chargeRatio = 1f)
    {
        weaponsSpreads[idx].ApplySpreadFromServer(chargeRatio);
    }

    private Vector3 GetDirectionWithSpread(float spreadAngle, Transform directionTransform)
    {
        var spreadStrength = spreadAngle / 45f;
        /*Most fucked explanantion to ever cross the realm of reality
         / 45f -> to get value which we can use in a vector instead of an angle
        ex in 2D:  a vector that has a 45 angle above X has a (1, 1) direction
        while the X has a (1, 0)
        so we essentially brought the 45 to a value we could use as a direction in the vector
         */
        // perhaps do directionTransform.forward * 45 instead of other / 45 (for performances purposes)
        return (
                directionTransform.forward + directionTransform.TransformDirection(
                    new Vector3(
                        UnityEngine.Random.Range(-spreadStrength, spreadStrength),
                        UnityEngine.Random.Range(-spreadStrength, spreadStrength),
                        0
                    )
                )
            ).normalized;
    }

    #endregion

    #region Handle Sound

    [Rpc(SendTo.Server)]
    private void PlayShootingSoundServerRpc(bool shouldWarn)
    {
        PlayShootingSoundClientRpc(shouldWarn);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayShootingSoundClientRpc(bool shouldWarn)
    {
        PlayShootingSound(shouldWarn);
    }

    private void PlayShootingSound(bool shouldWarn)
    {
        for (int iteration = 0; iteration < audioSourcePoolSize; iteration++) // this loop ensures that we only do <audioSourcePoolSize> loops
        {
            currentAudioSourceIndex = ++currentAudioSourceIndex % audioSourcePoolSize; // while this index ensures that we continue where we left of and remain within the boundaries of the pool

            if (!audioSources[currentAudioSourceIndex].isPlaying)
            {
                audioSources[currentAudioSourceIndex].clip = shouldWarn ? currentWeaponSounds.NearEmptyMagazineShootSound : currentWeaponSounds.ShootingSound;
                audioSources[currentAudioSourceIndex].Play();
                return;
            }
        }

        // if reached this point there wasn t enough channels to play all the sounds concurrently so we allocate another one
        audioSources.Add(gameObject.AddComponent<AudioSource>());
        audioSourcePoolSize++;
        audioSources[^1].clip = shouldWarn ? currentWeaponSounds.NearEmptyMagazineShootSound : currentWeaponSounds.ShootingSound;
        audioSources[^1].Play();
    }

    [Rpc(SendTo.Server)]
    private void PlayReloadSoundServerRpc()
    {
        PlayReloadSoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayReloadSoundClientRpc()
    {
        // make the reload duration rely on the audio for its duration so no need to "eardrum it" (check for audioSource.isPlaying in the coroutine)
        foreach (var audioSource in audioSources)
        {
            audioSource.Stop();
        }

        audioSources[0].clip = currentWeaponSounds.ReloadSound;
        audioSources[0].Play();
    }

    #endregion

    #region Handle Damage Logs

    public void SpawnDamageLog(TargetType targetType, ushort damage)
    {
        //if (!IsOwner) { return; }
        damageLogManager.SummonDamageLog(Vector3.zero, targetType, damage);
    }

    #endregion
}

public struct ShotgunHitData
{
    public IShootable Victim;
    public Vector3 HitPoint;
    public Vector3 HitDirection;

    public ShotgunHitData(IShootable _victim, Vector3 _hitPoint, Vector3 _hitDirection)
    {
        Victim = _victim;
        HitPoint = _hitPoint;
        HitDirection = _hitDirection;
    }
}


public struct ShotInfos
{
    public Vector3 ShotDirection;
    public RaycastHit Hit;
    public WeaponInfos WeaponInfos;
    public ulong AttackerNetworkID;

    public ShotInfos(Vector3 shotDirection, RaycastHit hit, ulong attackerNetworkID, WeaponInfos weaponInfos)
    {
        ShotDirection = shotDirection;
        Hit = hit;
        WeaponInfos = weaponInfos;
        AttackerNetworkID = attackerNetworkID;
    }
}

public struct WeaponInfos
{
    public DamageDealt Damage;
    public bool CanBreakThings;
    public Effects Effects;
    public bool PierceThroughPlayers;
    public HitscanBulletActionOnHitWall ActionOnHitWall;

    public WeaponInfos(WeaponScriptableObject weaponStats, float chargeRatio = 1f)
    {
        Damage = (weaponStats.ShootingStyle == ShootingStyle.Single ? weaponStats.Damage : weaponStats.Damage / weaponStats.ShotgunStats.PelletsCount) * chargeRatio;
        CanBreakThings = weaponStats.CanBreakThings;
        Effects = weaponStats.EffectsInflicted;
        PierceThroughPlayers = weaponStats.HitscanBulletSettings.PierceThroughPlayers;
        ActionOnHitWall = weaponStats.HitscanBulletSettings.ActionOnHitWall;
    }
}

// create a spread increase on shotguns in case I add a somewhat auto shotgun

// create a weapon abstract clas instead? with OnAttackDown OnAttackUp OnReload OnAimDown OnAimUp etc bc that could be more flexible

// double/three fire rifle kinda like in thsi game where you can chop limbs off using your gun (horizontally would help for crowds of opponent while vertical would shred their health by allowing hitting headshot + 2 bodyshots at once
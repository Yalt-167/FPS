using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

using Random = UnityEngine.Random;

using Projectiles;
using GameManagement;
using WeaponHandling;




[Serializable]
public sealed class WeaponHandler : NetworkBehaviour
{
    #region References

    [SerializeField] private NetworkVariable<WeaponScriptableObject> currentWeapon = new NetworkVariable<WeaponScriptableObject>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    public WeaponScriptableObject CurrentWeapon => currentWeapon.Value;

    private bool isInitialized;


    private float cameraTransformInitialZ;
    private Transform cameraTransform;
    private new Camera camera;
    private Transform recoilHandlerTransform;
    private CrosshairRecoil crosshairRecoil;
    private WeaponBehaviourGatherer<BarrelEnd> barrelEnds;
    public WeaponBehaviourGatherer<BarrelEnd> BarrelEnds => barrelEnds;
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

    private List<AudioSource> audioSources = new();
    private int audioSourcePoolSize;
    private int currentAudioSourceIndex;
    private WeaponSounds currentWeaponSounds;

    #endregion

    #region Global Setup

    // eventually make this a NetworkVariable
    private NetworkVariable<float> timeLastShotFired = new NetworkVariable<float>(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
    private bool switchedThisFrame;
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

    #region Recoil Setup

    private Vector3 currentRecoilHandlerRotation;
    private Vector3 targetRecoilHandlerRotation;

    #endregion

    #region Spread Setup

    private float currentSpreadAngle = 0f;

    #endregion

    #region Shotgun Setup

    private Vector3[] shotgunPelletsDirections;

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
            currentCooldownBetweenRampUpShots = Mathf.Clamp(value, currentWeapon.Value.RampUpStats.RampUpMinCooldownBetweenShots, currentWeapon.Value.RampUpStats.RampUpMaxCooldownBetweenShots);
        }
    }


    #endregion

    #region Charge Setup

    private float timeStartedCharging;
    private bool holdingAttackKey = false;



    #endregion

    // methods

    #region Unity Handled

    private void FixedUpdate()
    {
        crosshairRecoil.HandleRecoilServerRpc();

        foreach (var weaponKickback in weaponsKickbacks)
        {
            weaponKickback.HandleKickbackServerRpc();
        }


        if (shotThisFrame.Value) { return; }

        // ?do a delegate PostShotLogic() to handle such behaviour? (and juste do plain increment instead of this)
        CurrentCooldownBetweenRampUpShots *= currentWeapon.Value.RampUpStats.RampUpCooldownRegulationMultiplier;
    }

    private void Update()
    {
        HandleSpread();
    }

    private void LateUpdate()
    {
        switchedThisFrame = false;
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
        if (isInitialized) { return; }

        playerSettings = GetComponent<PlayerSettings>();
        recoilHandlerTransform = transform.GetChild(0).GetChild(0);
        crosshairRecoil = GetComponent<CrosshairRecoil>();
        cameraTransform = recoilHandlerTransform.GetChild(0);
        camera = cameraTransform.GetComponent<Camera>();
        weaponADSFOV = cameraTransform.GetComponent<WeaponADSFOV>();

        InitWeapon();
        audioSourcePoolSize = 10; // (int)(Mathf.Max(currentWeaponSounds.ShootingSound.length, currentWeaponSounds.NearEmptyMagazineShootSound.length) / currentWeapon.CooldownBetweenShots);
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            audioSources.Add(gameObject.AddComponent<AudioSource>());
        }

        ownerFrame = GetComponent<PlayerFrame>();

        damageLogManager.UpdatePlayerSettings(DamageLogsSettings);

        isInitialized = true;
    }

    public void SetWeapon(WeaponScriptableObject weapon)
    {
        Init();
        SetWeaponServerRpc(weapon);
        InitWeapon();
    }

    [Rpc(SendTo.Server)]
    private void SetWeaponServerRpc(WeaponScriptableObject weapon)
    { 
        currentWeapon.Value = weapon;
    }

    public void InitWeapon()
    {
        barrelEnds = new(GetComponentsInChildren<BarrelEnd>());

        //weaponSockets = new(GetComponentsInChildren<WeaponSocket>());

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

        currentWeaponSounds = currentWeapon.Value.Sounds;

        StartCoroutine(InitWeaponFromServer());
        switchedThisFrame = true;

        SetShootingStyle(); // the actual shooting logic
        SetShootingRythm(); // the rythm logic of the shots
        SetOnHitWallMethod();
    }

    private IEnumerator InitWeaponFromServer()
    {
        yield return new WaitUntil(() => IsSpawned);

        InitWeaponServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void InitWeaponServerRpc()
    {
        timeLastShotFired.Value = float.MinValue;
        ammos.Value = currentWeapon.Value.MagazineSize;
        canShoot.Value = true;
    }

    private void SetShootingStyle()
    {
        shootingStyleMethod = currentWeapon.Value.ShootingStyle switch
        {
            // ANCHOR
            //ShootingStyle.Single => currentWeapon.IsHitscan ? ExecuteSimpleHitscanShotClientRpc : ExecuteSimpleTravelTimeShotClientRpc,
            ShootingStyle.Single => currentWeapon.Value.IsHitscan ? ExecuteSimpleHitscanShot : ExecuteSimpleTravelTimeShotClientRpc,

            //ShootingStyle.Shotgun => currentWeapon.IsHitscan ? ExecuteShotgunHitscanShotClientRpc :  ExecuteShotgunTravelTimeShotClientRpc,
            ShootingStyle.Shotgun => currentWeapon.Value.IsHitscan ? ExecuteShotgunHitscanShot :  ExecuteShotgunTravelTimeShotClientRpc,

            _ => throw new Exception("This Shooting Style does not exist")
            ,
        };
    }

    private void SetShootingRythm()
    {
        shootingRythmMethod = currentWeapon.Value.ShootingRythm switch
        {
            ShootingRythm.Single => RequestShotServerRpc,

            ShootingRythm.Burst => () =>
                {
                    StartCoroutine(ShootBurst(currentWeapon.Value.BurstStats.BulletsPerBurst));
                }
            ,

            ShootingRythm.RampUp => () =>
                {
                    RequestShotServerRpc();
                    CurrentCooldownBetweenRampUpShots *= currentWeapon.Value.RampUpStats.RampUpCooldownMultiplierPerShot;
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
        onHitWallMethod = currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall switch
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
                if (currentWeapon.Value.ShootingRythm == ShootingRythm.Charge)
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

        if (currentWeapon.Value.IsHitscan)
        {
            if (currentWeapon.Value.ShootingStyle == ShootingStyle.Shotgun)
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
            if (currentWeapon.Value.ShootingStyle == ShootingStyle.Shotgun)
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
        return currentWeapon.Value.ShootingRythm switch
        {
            ShootingRythm.Single => currentWeapon.Value.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == currentWeapon.Value.BurstStats.BulletsPerBurst ? currentWeapon.Value.CooldownBetweenShots : currentWeapon.Value.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => currentWeapon.Value.CooldownBetweenShots,
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

            yield return new WaitUntil(() => timerStart + currentWeapon.Value.BurstStats.CooldownBetweenShotsOfBurst < Time.time || switchedThisFrame);

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
        var chargeRatio = timeCharged / currentWeapon.Value.ChargeStats.ChargeDuration;
        if (chargeRatio >= currentWeapon.Value.ChargeStats.MinChargeRatioToShoot)
        {
            var ammoConsumedByThisShot = (ushort)(currentWeapon.Value.ChargeStats.AmmoConsumedByFullyChargedShot * chargeRatio);
            if (ammos.Value < ammoConsumedByThisShot)
            {
                RequestChargedShotServerRpc(ammos.Value / currentWeapon.Value.ChargeStats.AmmoConsumedByFullyChargedShot);
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

        PlayShootingSound(currentWeapon.Value.AmmoLeftInMagazineToWarn > ammos.Value);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void EmulateHitscanShotsClientRpc(int barrelEndIndex, Vector3[] endPoints)
    {   
        for (int i = 0; i < endPoints.Length; i++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, BarrelEnds[barrelEndIndex].transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            bulletTrail.Set(BarrelEnds[barrelEndIndex].transform.position, endPoints[i]);
        }

        PlayShootingSound(currentWeapon.Value.AmmoLeftInMagazineToWarn > ammos.Value);
    }

    #region Execute Shot

    [Obsolete("Should be replaced by the non-ClientRpc version")]
    private void UpdateOwnerSettingsUponShot()
    {
        Debug.Log("[UpdateOwnerSettingsUponShot]I was called tho I shouldn t");

        if (!IsOwner) { return; }

        PlayShootingSoundServerRpc(currentWeapon.Value.AmmoLeftInMagazineToWarn >= ammos.Value);

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
        return currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Explosive => currentWeapon.Value.HitscanBulletSettings.ExplodingBulletsSettings,
            HitscanBulletActionOnHitWall.BounceOnWalls => currentWeapon.Value.HitscanBulletSettings.BouncingBulletsSettings,
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

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

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
                        currentWeapon.Value.Damage,
                        hit.point,
                        barrelEnd.transform.forward,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        currentWeapon.Value.CanBreakThings
                    );
                }

                if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeapon.Value)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(barrelEnds.Current.transform.position, endPoint);

        barrelEnds.GoNext();

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

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        var directionWithSpread = weaponsSpreads[index].GetDirectionWithSpreadFromServer(index);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(
                    currentWeapon.Value.Damage,
                    hit.point,
                    barrelEnd.transform.forward,
                    NetworkObjectId,
                    PlayerFrame.LocalPlayer.TeamNumber,
                    currentWeapon.Value.CanBreakThings
                );
                

                if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeapon.Value)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        EmulateHitscanShotClientRpc(index, endPoint);

        barrelEnds.GoNext();

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

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + direction * 100;
            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, currentWeapon.Value.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(
                            currentWeapon.Value.Damage / currentWeapon.Value.ShotgunStats.PelletsCount,
                            direction,
                            hit.point,
                            NetworkObjectId,
                            PlayerFrame.LocalPlayer.TeamNumber,
                            currentWeapon.Value.CanBreakThings
                        );
                    }

                    if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                            new(currentWeapon.Value)
                        ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }

                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil();
        ApplyKickback(index);
    }
    private void ExecuteShotgunHitscanShot() // do all that server side instead
    {
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
        var pelletIdx = 0;
        var endPoints = new Vector3[currentWeapon.Value.ShotgunStats.PelletsCount];
        foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpreadFromServer(index))
        {
            endPoints[pelletIdx] = barrelEnd.transform.position + direction * 100;

            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, currentWeapon.Value.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot(
                        currentWeapon.Value.Damage / currentWeapon.Value.ShotgunStats.PelletsCount,
                        direction,
                        hit.point,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        currentWeapon.Value.CanBreakThings
                    );
                    

                    if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                            new(currentWeapon.Value)
                        ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoints[pelletIdx] = hit.point;
                        break;
                    }

                }

                pelletIdx++;
            }
        }
        
        EmulateHitscanShotsClientRpc(index, endPoints);

        barrelEnds.GoNext();

        ApplyCrosshairRecoil();
        ApplyKickback(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedHitscanShotClientRpc(float chargeRatio)
    {
#pragma warning disable
        UpdateOwnerSettingsUponShot();
#pragma warning restore

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
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
                        currentWeapon.Value.Damage * chargeRatio,
                        hit.point,
                        barrelEnd.transform.forward,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        currentWeapon.Value.CanBreakThings
                    );
                }

                if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeapon.Value, chargeRatio)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(barrelEnd.transform.position, endPoint);

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplySpread(index, chargeRatio);
        ApplyKickback(index, chargeRatio);
    }
    private void ExecuteChargedHitscanShot(float chargeRatio)
    {
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
        var directionWithSpread = weaponsSpreads[index].GetDirectionWithSpreadFromServer(index);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(
                    currentWeapon.Value.Damage * chargeRatio,
                    hit.point,
                    barrelEnd.transform.forward,
                    NetworkObjectId,
                    PlayerFrame.LocalPlayer.TeamNumber,
                    currentWeapon.Value.CanBreakThings
                );
             

                if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeapon.Value, chargeRatio)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        EmulateHitscanShotClientRpc(index, endPoint);

        barrelEnds.GoNext();

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
        

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
        foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + direction * 100;


            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, currentWeapon.Value.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(
                            currentWeapon.Value.Damage / currentWeapon.Value.ShotgunStats.PelletsCount * chargeRatio,
                            direction,
                            hit.point,
                            NetworkObjectId,
                            PlayerFrame.LocalPlayer.TeamNumber,
                            currentWeapon.Value.CanBreakThings
                        );
                    }

                    if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                            new(currentWeapon.Value, chargeRatio)
                            ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }

                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplyKickback(index, chargeRatio);
    }
    private void ExecuteChargedShotgunHitscanShot(float chargeRatio)
    {
        UpdateOwnerSettingsUponShotFromServer();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
        foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpreadFromServer(index))
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + direction * 100;


            var hits = Physics.RaycastAll(barrelEnd.transform.position, direction, currentWeapon.Value.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(
                            currentWeapon.Value.Damage / currentWeapon.Value.ShotgunStats.PelletsCount * chargeRatio,
                            direction,
                            hit.point,
                            NetworkObjectId,
                            PlayerFrame.LocalPlayer.TeamNumber,
                            currentWeapon.Value.CanBreakThings
                        );
                    }

                    if (!currentWeapon.Value.HitscanBulletSettings.PierceThroughPlayers)
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
                            new(currentWeapon.Value, chargeRatio)
                            ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (currentWeapon.Value.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }

                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        barrelEnds.GoNext();

        ApplyCrosshairRecoil(chargeRatio);
        ApplyKickback(index, chargeRatio);
    }

    #endregion

    #region Execute Shot TravelTime

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleTravelTimeShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        var projectile = Instantiate(
            currentWeapon.Value.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.transform.position,
            Quaternion.LookRotation(weaponsSpreads[index].GetDirectionWithSpread(index))
            ).GetComponent<Projectile>();

        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        projectile.Init(
            currentWeapon.Value.Damage,
            currentWeapon.Value.TravelTimeBulletSettings.BulletSpeed,
            currentWeapon.Value.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeapon.Value.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            ownerFrame.TeamNumber
        );

        barrelEnds.GoNext();

        if (IsOwner) { return; }

        ApplyCrosshairRecoil();
        ApplySpread(index);
        ApplyKickback(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunTravelTimeShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        //for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        foreach (var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        {

            var projectile = Instantiate(
                currentWeapon.Value.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.transform.position,
                Quaternion.LookRotation(/*shotgunPelletsDirections[i]*/ direction)
            ).GetComponent<Projectile>();

            if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

            projectile.Init(
                currentWeapon.Value.Damage / currentWeapon.Value.ShotgunStats.PelletsCount,
                currentWeapon.Value.TravelTimeBulletSettings.BulletSpeed,
                currentWeapon.Value.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeapon.Value.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                ownerFrame.TeamNumber
            );

        }

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil();
        ApplyKickback(index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedTravelTimeShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out int index);

        var projectile = Instantiate(
            currentWeapon.Value.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.transform.position,
            Quaternion.LookRotation(weaponsSpreads[index].GetDirectionWithSpread(index))
        ).GetComponent<Projectile>();

        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        projectile.Init(
            currentWeapon.Value.Damage * chargeRatio,
            currentWeapon.Value.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
            currentWeapon.Value.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeapon.Value.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            ownerFrame.TeamNumber
        );

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyCrosshairRecoil(chargeRatio);
        ApplySpread(index, chargeRatio);
        ApplyKickback(index, chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunTravelTimeShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        //for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        foreach(var direction in weaponsSpreads[index].GetShotgunDirectionsWithSpread(index))
        {
            var projectile = Instantiate(
                currentWeapon.Value.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.transform.position,
                Quaternion.LookRotation(/*shotgunPelletsDirections[i]*/direction)
            ).GetComponent<Projectile>();

            if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

            projectile.Init(
                currentWeapon.Value.Damage / currentWeapon.Value.ShotgunStats.PelletsCount * chargeRatio,
                currentWeapon.Value.TravelTimeBulletSettings.BulletSpeed,
                currentWeapon.Value.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeapon.Value.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                ownerFrame.TeamNumber
            );
        }

        barrelEnds.GoNext();

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

        var newShotDirection = MyUtility.Utility.ReflectVector(shotInfos.ShotDirection, shotInfos.Hit.normal);

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
                            new(currentWeapon.Value)
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
        return currentWeapon.Value.TravelTimeBulletSettings.OnHitWallBehaviour switch
        {
            ProjectileBehaviourOnHitWall.Stop => new ProjectileStopOnHitWall(null),
            ProjectileBehaviourOnHitWall.Pierce => new ProjectilePierceOnHitWall(currentWeapon.Value.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallPierceParams),
            ProjectileBehaviourOnHitWall.Bounce => new ProjectileBounceOnHitWall(currentWeapon.Value.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallBounceParams),
            ProjectileBehaviourOnHitWall.Explode => new ProjectileExplodeOnHitWall(currentWeapon.Value.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }


    private ProjectileOnHitPlayerBehaviour GetRelevantHitPlayerBehaviour()
    {
        return currentWeapon.Value.TravelTimeBulletSettings.OnHitPlayerBehaviour switch
        {
            ProjectileBehaviourOnHitPlayer.Stop => new ProjectileStopOnHitPlayer(null),
            ProjectileBehaviourOnHitPlayer.Pierce => new ProjectilePierceOnHitPlayer(currentWeapon.Value.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerPierceParams),
            ProjectileBehaviourOnHitPlayer.Explode => new ProjectileExplodeOnHitPlayer(currentWeapon.Value.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }

    #endregion

    #region Reload

    public void Reload()
    {
        if (!currentWeapon.Value.NeedReload) { return; }

        if (ammos.Value == currentWeapon.Value.MagazineSize) { return; } // already fully loaded

        if (currentWeapon.Value.TimeToReloadOneRound == 0f)
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
        yield return new WaitUntil(() => timerStart + currentWeapon.Value.ReloadSpeed < Time.time || switchedThisFrame);
        
        canShoot.Value = true;

        if (switchedThisFrame) { yield break; }

        ammos.Value = currentWeapon.Value.MagazineSize;
    }

    [Rpc(SendTo.Server)]
    private void ExecuteReloadRoundPerRoundServerRpc()
    {
        StartCoroutine(ExecuteReloadRoundPerRound());
    }

    private IEnumerator ExecuteReloadRoundPerRound()
    {
        var ammosToReload = currentWeapon.Value.MagazineSize - ammos.Value;

        for (int i = 0; i < ammosToReload; i++)
        {
            var timerStart = Time.time;
            yield return new WaitUntil(
                () => timerStart + currentWeapon.Value.TimeToReloadOneRound < Time.time ||
                switchedThisFrame || // pass that server side then
                HasBufferedShoot // pass that server side too
                ); // + buffer a shoot input that would interrupt the reload

            if (switchedThisFrame) { yield break; }

            if (HasBufferedShoot)
            {
                if (currentWeapon.Value.ShootingRythm == ShootingRythm.Charge)
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

    private void HandleSpread()
    {
        foreach (var weaponSpread in weaponsSpreads)
        {
            weaponSpread.HandleSpreadServerRpc();
        }
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
                        Random.Range(-spreadStrength, spreadStrength),
                        Random.Range(-spreadStrength, spreadStrength),
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
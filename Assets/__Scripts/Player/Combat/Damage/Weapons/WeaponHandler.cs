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

    [SerializeField] private WeaponScriptableObject currentWeapon;
    public WeaponScriptableObject CurrentWeapon => currentWeapon;

    private WeaponRecoilStats CurrentWeaponHipfireRecoilStats => currentWeapon.HipfireRecoilStats;
    private WeaponRecoilStats CurrentWeaponADSRecoilStats => currentWeapon.AimingRecoilStats;

    private KickbackStats CurrentWeaponKickbackStats => currentWeapon.KickbackStats;
    private bool isInitialized;


    private float cameraTransformInitialZ;
    private Transform cameraTransform;
    private new Camera camera;
    private Transform recoilHandlerTransform;
    private CrosshairRecoil crosshairRecoil;
    private WeaponBehaviourGatherer<BarrelEnd> barrelEnds;
    private WeaponBehaviourGatherer<WeaponADSGunMovement> weaponsADSGunMovements;
    private WeaponBehaviourGatherer<WeaponKickback> weaponsKickbacks;
    private WeaponBehaviourGatherer<WeaponSpread> weaponsSpreads;
    private WeaponADSFOV weaponADSFOV;

    //private WeaponBehaviourGatherer<WeaponSocket> weaponSockets;

    [SerializeField] private Transform weaponTransform;
    [SerializeField] private Transform weaponSocketTransform;
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

    private float timeLastShotFired = float.NegativeInfinity;
    private bool switchedThisFrame;
    private bool shotThisFrame;
    private ushort ammos;

    private bool canShoot;


    private Action shootingStyleMethod;
    private Action shootingRythmMethod;
    private Action<ShotInfos, IHitscanBulletEffectSettings> onHitWallMethod;

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
            currentCooldownBetweenRampUpShots = Mathf.Clamp(value, currentWeapon.RampUpStats.RampUpMinCooldownBetweenShots, currentWeapon.RampUpStats.RampUpMaxCooldownBetweenShots);
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


        if (shotThisFrame) { return; }

        // ?do a delegate PostShotLogic() to handle such behaviour? (and juste do plain increment instead of this)
        CurrentCooldownBetweenRampUpShots *= currentWeapon.RampUpStats.RampUpCooldownRegulationMultiplier;
    }

    private void Update()
    {
        HandleSpead();
    }

    private void LateUpdate()
    {
        switchedThisFrame = false;
        shotThisFrame = false;
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

        isInitialized = true;
    }

    public void SetWeapon(WeaponScriptableObject weapon)
    {
        Init();
        currentWeapon = weapon;
        InitWeapon();
    }

    public void InitWeapon()
    {
        barrelEnds = new(GetComponentsInChildren<BarrelEnd>());

        //weaponSockets = new(GetComponentsInChildren<WeaponSocket>());

        weaponsADSGunMovements = new(GetComponentsInChildren<WeaponADSGunMovement>());
        foreach (var weaponADSGunMovement in weaponsADSGunMovements)
        {
            weaponADSGunMovement.SetupData(currentWeapon.AimingAndScopeStats);
        }

        weaponADSFOV.SetupData(currentWeapon.AimingAndScopeStats);

        StartCoroutine(crosshairRecoil.SetupData(currentWeapon.AimingRecoilStats, currentWeapon.HipfireRecoilStats));

        weaponsKickbacks = new(GetComponentsInChildren<WeaponKickback>());
        foreach (var weaponKickback in weaponsKickbacks)
        {
            StartCoroutine(weaponKickback.SetupData(currentWeapon.KickbackStats));
        }


        weaponsSpreads = new(GetComponentsInChildren<WeaponSpread>());
        foreach (var weaponSpread in weaponsSpreads)
        {
            StartCoroutine(weaponSpread.SetupData(currentWeapon.SimpleShotStats, currentWeapon.AimingSimpleShotStats));
        }

        currentWeaponSounds = currentWeapon.Sounds;
        ammos = currentWeapon.MagazineSize;

        canShoot = true;
        timeLastShotFired = float.MinValue;
        switchedThisFrame = true;

        SetShootingStyle(); // the actual shooting logic
        SetShootingRequestRythm(); // the rythm logic of the shots
        SetOnHitWallMethod();
        
    }

    private void SetShootingStyle()
    {
        shootingStyleMethod = currentWeapon.ShootingStyle switch
        {
            ShootingStyle.Single => currentWeapon.IsHitscan ? ExecuteSimpleHitscanShotClientRpc : ExecuteSimpleTravelTimeShotClientRpc,

            ShootingStyle.Shotgun => currentWeapon.IsHitscan ?
                () =>
                    {
                        SetShotgunPelletsDirections(barrelEnds.Current.transform);
                        ExecuteShotgunHitscanShotClientRpc();
                    }
            :
                () =>
                    {
                        SetShotgunPelletsDirections(barrelEnds.Current.transform);
                        ExecuteShotgunTravelTimeShotClientRpc();
                    }
            ,

            _ => throw new Exception("This Shooting Style does not exist")
            ,
        };
    }

    private void SetShootingRequestRythm()
    {
        shootingRythmMethod = currentWeapon.ShootingRythm switch
        {
            ShootingRythm.Single => RequestShotServerRpc,

            ShootingRythm.Burst => () =>
                {
                    StartCoroutine(ShootBurst(currentWeapon.BurstStats.BulletsPerBurst));
                }
            ,

            ShootingRythm.RampUp => () =>
                {
                    RequestShotServerRpc();
                    CurrentCooldownBetweenRampUpShots *= currentWeapon.RampUpStats.RampUpCooldownMultiplierPerShot;
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
        onHitWallMethod = currentWeapon.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Classic => (_, _) => { }
            ,
            HitscanBulletActionOnHitWall.ThroughWalls => (_, _) => { }
            ,
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
                if (currentWeapon.ShootingRythm == ShootingRythm.Charge)
                {
                    StartCoroutine(ChargeShot());
                    return;
                }
            }
            else  // just released the attack key
            {

            }

        }

        if (!(canShoot && holdingAttackKey))
        {
            return;
        }

        shootingRythmMethod();
    }

    public void UpdateAimingState(bool shouldBeAiming)
    {
        if (IsAiming == shouldBeAiming) { return; }

        IsAiming = shouldBeAiming;
        foreach (var weaponsADSGunMovement in weaponsADSGunMovements)
        {
            weaponsADSGunMovement.ToggleADS(shouldBeAiming);
        }
        weaponADSFOV.ToggleADS(shouldBeAiming);
    }

    [Rpc(SendTo.Server)]
    public void RequestShotServerRpc()
    {
        CheckCooldownsClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CheckCooldownsClientRpc()
    {
        if (!IsOwner) { return; }

        if (timeLastShotFired + GetRelevantCooldown() > Time.time) { return; }

        if (ammos <= 0) { return; }

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
        if (currentWeapon.ShootingStyle == ShootingStyle.Shotgun)
        {
            SetShotgunPelletsDirections(barrelEnds.Current.transform); // different on all clients
        }

        if (timeLastShotFired + GetRelevantCooldown() > Time.time) { return; }

        if (currentWeapon.IsHitscan)
        {
            if (currentWeapon.ShootingStyle == ShootingStyle.Shotgun)
            {
                ExecuteChargedShotgunHitscanShotClientRpc(chargeRatio);
            }
            else
            {
                ExecuteChargedHitscanShotClientRpc(chargeRatio);
            }
        }
        else
        {
            if (currentWeapon.ShootingStyle == ShootingStyle.Shotgun)
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
        return currentWeapon.ShootingRythm switch
        {
            ShootingRythm.Single => currentWeapon.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == currentWeapon.BurstStats.BulletsPerBurst ? currentWeapon.CooldownBetweenShots : currentWeapon.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => currentWeapon.CooldownBetweenShots,
            _ => 0f,
        };
    }

    private IEnumerator ShootBurst(int bullets)
    {
        if (ammos <= 0) { yield break; }

        if (timeLastShotFired + GetRelevantCooldown() > Time.time) { yield break; }

        bulletFiredthisBurst = 0;

        canShoot = false;

        for (int i = 0; i < bullets; i++)
        {
            var timerStart = Time.time;

            yield return new WaitUntil(() => timerStart + currentWeapon.BurstStats.CooldownBetweenShotsOfBurst < Time.time || switchedThisFrame);

            if (switchedThisFrame) { yield break; }

            RequestShotServerRpc();
        }

        canShoot = true;
    }

    private IEnumerator ChargeShot()
    {
        if (ammos <= 0) { yield break; }

        if (timeLastShotFired + GetRelevantCooldown() > Time.time) { yield break; }

        timeStartedCharging = Time.time;

        yield return new WaitWhile(() => holdingAttackKey);

        var timeCharged = Time.time - timeStartedCharging;
        var chargeRatio = timeCharged / currentWeapon.ChargeStats.ChargeDuration;
        if (chargeRatio >= currentWeapon.ChargeStats.MinChargeRatioToShoot)
        {
            var ammoConsumedByThisShot = (ushort)(currentWeapon.ChargeStats.AmmoConsumedByFullyChargedShot * chargeRatio);
            if (ammos < ammoConsumedByThisShot)
            {
                RequestChargedShotServerRpc(ammos / currentWeapon.ChargeStats.AmmoConsumedByFullyChargedShot);
            }
            else
            {
                RequestChargedShotServerRpc(chargeRatio);
            }
        }
    }

    #endregion

    #region Execute Shot

    private void UpdateOwnerSettingsUponShot()
    {
        //if (!IsOwner) { return; }

        PlayShootingSoundServerRpc(currentWeapon.AmmoLeftInMagazineToWarn >= ammos);

        damageLogManager.UpdatePlayerSettings(DamageLogsSettings);
        timeLastShotFired = Time.time;
        shotThisFrame = true;
        ammos--;
        bulletFiredthisBurst++;
    }

    #region Execute Shot Hitscan

    private IHitscanBulletEffectSettings GetRelevantHitscanBulletSettings()
    {
        return currentWeapon.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Explosive => currentWeapon.HitscanBulletSettings.ExplodingBulletsSettings,
            HitscanBulletActionOnHitWall.BounceOnWalls => currentWeapon.HitscanBulletSettings.BouncingBulletsSettings,
            HitscanBulletActionOnHitWall.Classic => null,
            HitscanBulletActionOnHitWall.ThroughWalls => null,
            _ => null
        };
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //private void _ExecuteSimpleHitscanShotClientRpc()
    //{
    //    UpdateOwnerSettingsUponShot();

    //    var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
    //    var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);

    //    if (currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
    //    {
    //        var endPoint = barrelEnd.position + directionWithSpread * 100;

    //        var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

    //        Array.Sort(hits, new RaycastHitComparer());

    //        foreach (var hit in hits)
    //        {
    //            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
    //            {
    //                if (IsOwner)
    //                {
    //                    shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnd.forward, NetworkObjectId, _____placeHolderTeamID, currentWeapon.CanBreakThings);
    //                }
    //            }
    //            else // so far else is the wall but do proper checks later on
    //            {
    //                endPoint = hit.point;
    //                break;
    //            }
    //        }

    //        bulletTrail.Set(barrelEnd.position, endPoint);

    //    }
    //    else
    //    {
    //        if (Physics.Raycast(barrelEnd.position, directionWithSpread, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
    //        {
    //            bulletTrail.Set(barrelEnd.position, hit.point);
    //            if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
    //            {
    //                shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnd.forward, NetworkObjectId, _____placeHolderTeamID, currentWeapon.CanBreakThings);
    //            }
    //        }
    //        else
    //        {
    //            bulletTrail.Set(barrelEnd.position, barrelEnd.position + directionWithSpread * 100);
    //        }
    //    }

    //    if (!IsOwner) { return; }

    //    ApplyRecoil();
    //    ApplySpread();
    //    ApplyKickback();
    //}

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleHitscanShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd.transform);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    //shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
                    shootableComponent.ReactShot(
                        currentWeapon.Damage,
                        hit.point,
                        barrelEnd.transform.forward,
                        NetworkObjectId,
                        PlayerFrame.LocalPlayer.TeamNumber,
                        currentWeapon.CanBreakThings
                    );
                }

                if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeapon)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(barrelEnds.Current.transform.position, endPoint);

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        
        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunHitscanShotClientRpc() // do all that server side instead
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
        for (int pelletIndex = 0; pelletIndex < currentWeapon.ShotgunStats.PelletsCount; pelletIndex++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + shotgunPelletsDirections[pelletIndex] * 100;

            var hits = Physics.RaycastAll(barrelEnd.transform.position, shotgunPelletsDirections[pelletIndex], currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        //shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
                        shootableComponent.ReactShot(currentWeapon.Damage / currentWeapon.ShotgunStats.PelletsCount, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
                    }

                    if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hit.point;
                        break;
                    }

                    else
                    {

                        onHitWallMethod(
                            new(
                                shotgunPelletsDirections[pelletIndex],
                                hit,
                                NetworkObjectId,
                                new(currentWeapon)
                            ),
                            GetRelevantHitscanBulletSettings()
                        );

                        if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                        {
                            endPoint = hit.point;
                            break;
                        }

                    }
                }
            }

            bulletTrail.Set(barrelEnd.transform.position, endPoint);
        }

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);
        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd.transform);
        var endPoint = barrelEnd.transform.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    //shootableComponent.ReactShot(currentWeapon.Damage * chargeRatio, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
                    shootableComponent.ReactShot(currentWeapon.Damage * chargeRatio, hit.point, barrelEnd.transform.forward, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
                }

                if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeapon, chargeRatio)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }

            }
        }

        bulletTrail.Set(barrelEnd.transform.position, endPoint);

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var inex);
        for (int pelletIndex = 0; pelletIndex < currentWeapon.ShotgunStats.PelletsCount; pelletIndex++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.transform.position + shotgunPelletsDirections[pelletIndex] * 100;


            var hits = Physics.RaycastAll(barrelEnd.transform.position, shotgunPelletsDirections[pelletIndex], currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        //shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
                        shootableComponent.ReactShot(currentWeapon.Damage / currentWeapon.ShotgunStats.PelletsCount * chargeRatio, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
                    }

                    if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hit.point;
                        break;
                    }
                }
                else
                {
                    onHitWallMethod(
                        new(
                            shotgunPelletsDirections[pelletIndex],
                            hit,
                            NetworkObjectId,
                            new(currentWeapon, chargeRatio)
                            ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
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

        ApplyRecoil();
        ApplyKickback();
    }

    #endregion

    #region Execute Shot TravelTime

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleTravelTimeShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        var projectile = Instantiate(
            currentWeapon.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.transform.position,
            Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnd.transform))
            ).GetComponent<Projectile>();

        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        projectile.Init(
            currentWeapon.Damage,
            currentWeapon.TravelTimeBulletSettings.BulletSpeed,
            currentWeapon.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeapon.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            ownerFrame.TeamNumber
        );

        barrelEnds.GoNext();

        if (IsOwner) { return; }

        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunTravelTimeShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {

            var projectile = Instantiate(
                currentWeapon.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.transform.position,
                Quaternion.LookRotation(shotgunPelletsDirections[i])
            ).GetComponent<Projectile>();

            if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

            projectile.Init(
                currentWeapon.Damage / currentWeapon.ShotgunStats.PelletsCount,
                currentWeapon.TravelTimeBulletSettings.BulletSpeed,
                currentWeapon.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeapon.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                ownerFrame.TeamNumber
            );

        }

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedTravelTimeShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out int index);

        var projectile = Instantiate(
            currentWeapon.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.transform.position,
            Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnd.transform))
        ).GetComponent<Projectile>();

        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        projectile.Init(
            currentWeapon.Damage * chargeRatio,
            currentWeapon.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
            currentWeapon.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeapon.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            ownerFrame.TeamNumber
        );

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunTravelTimeShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var barrelEnd = barrelEnds.GetCurrentAndIndex(out var index);

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            var projectile = Instantiate(
                currentWeapon.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.transform.position,
                Quaternion.LookRotation(shotgunPelletsDirections[i])
            ).GetComponent<Projectile>();

            if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

            projectile.Init(
                currentWeapon.Damage / currentWeapon.ShotgunStats.PelletsCount * chargeRatio,
                currentWeapon.TravelTimeBulletSettings.BulletSpeed,
                currentWeapon.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeapon.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                ownerFrame.TeamNumber
            );
        }

        barrelEnds.GoNext();

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    #endregion

    #endregion

    #region Hitscan Wall Hit Effects

    private void ExplodeUponWallHit(ShotInfos shotInfos, IHitscanBulletEffectSettings hitscanBulletEffectSettings_)
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

    private void BounceUponWallHit(ShotInfos shotInfos, IHitscanBulletEffectSettings hitscanBulletEffectSettings_)
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
                    //shootableComponent.ReactShot(shotInfos.WeaponInfos.Damage, hit.point, newShotDirection, NetworkObjectId, PlayerFrame.TeamID, shotInfos.WeaponInfos.CanBreakThings);
                    shootableComponent.ReactShot(shotInfos.WeaponInfos.Damage, hit.point, newShotDirection, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, shotInfos.WeaponInfos.CanBreakThings);
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
                            new(currentWeapon)
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
        return currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviour switch
        {
            ProjectileBehaviourOnHitWall.Stop => new ProjectileStopOnHitWall(null),
            ProjectileBehaviourOnHitWall.Pierce => new ProjectilePierceOnHitWall(currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallPierceParams),
            ProjectileBehaviourOnHitWall.Bounce => new ProjectileBounceOnHitWall(currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallBounceParams),
            ProjectileBehaviourOnHitWall.Explode => new ProjectileExplodeOnHitWall(currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }


    private ProjectileOnHitPlayerBehaviour GetRelevantHitPlayerBehaviour()
    {
        return currentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviour switch
        {
            ProjectileBehaviourOnHitPlayer.Stop => new ProjectileStopOnHitPlayer(null),
            ProjectileBehaviourOnHitPlayer.Pierce => new ProjectilePierceOnHitPlayer(currentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerPierceParams),
            ProjectileBehaviourOnHitPlayer.Explode => new ProjectileExplodeOnHitPlayer(currentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }

    #endregion

    #region Setting up Shotgun Shot

    private void SetShotgunPelletsDirections(Transform directionTranform)
    {
        shotgunPelletsDirections = new Vector3[currentWeapon.ShotgunStats.PelletsCount];

        var relevantSpread = IsAiming ? currentWeapon.ShotgunStats.AimingPelletsSpreadAngle : currentWeapon.ShotgunStats.PelletsSpreadAngle;

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            shotgunPelletsDirections[i] = GetDirectionWithSpread(relevantSpread, directionTranform);
        }

        RequestUpdateShotgunPelletsDirectionServerRpc(shotgunPelletsDirections);
    }

    [Rpc(SendTo.Server)]
    private void RequestUpdateShotgunPelletsDirectionServerRpc(Vector3[] directions)
    {
        UpdateShotgunPelletDirectionClientRpc(directions);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateShotgunPelletDirectionClientRpc(Vector3[] directions)
    {
        shotgunPelletsDirections = directions;
    }

    #endregion

    #region Reload

    public void Reload()
    {
        if (!currentWeapon.NeedReload) { return; }

        if (ammos == currentWeapon.MagazineSize) { return; } // already fully loaded

        StartCoroutine(currentWeapon.TimeToReloadOneRound == 0f ? ExecuteReload() : ExecuteReloadRoundPerRound());
    }

    private IEnumerator ExecuteReload()
    {
        canShoot = false;

        var timerStart = Time.time;
        yield return new WaitUntil(() => timerStart + currentWeapon.ReloadSpeed < Time.time || switchedThisFrame);

        canShoot = true;

        if (switchedThisFrame) { yield break; }

        ammos = currentWeapon.MagazineSize;
    }

    private IEnumerator ExecuteReloadRoundPerRound()
    {
        var ammosToReload = currentWeapon.MagazineSize - ammos;

        for (int i = 0; i < ammosToReload; i++)
        {
            var timerStart = Time.time;
            yield return new WaitUntil(
                () => timerStart + currentWeapon.TimeToReloadOneRound < Time.time ||
                switchedThisFrame ||
                HasBufferedShoot
                ); // + buffer a shoot input that would interrupt the reload

            if (switchedThisFrame) { yield break; }

            if (HasBufferedShoot)
            {
                if (currentWeapon.ShootingRythm == ShootingRythm.Charge)
                {
                    StartCoroutine(ChargeShot());
                }
                else
                {
                    shootingRythmMethod();
                }

                yield break;
            }

            ammos++;
        }
    }

    #endregion

    #region Handle Gun Movements

    private void ApplyRecoil(float chargeRatio = 1)
    {
        crosshairRecoil.ApplyRecoilServerRpc(chargeRatio);
    }

    private void ApplyKickback(float chargeRatio = 1f)
    {
        foreach (var weaponsKickback in weaponsKickbacks)
        {
            weaponsKickback.ApplyKickbackServerRpc(chargeRatio);
        }
    }


    #endregion

    #region Handle Spread

    private void ApplySpread(int idx)
    {
        weaponsSpreads[idx].ApplySpreadServerRpc();
    }

    private void HandleSpead()
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


// create a weapon abstract clas instead? with OnAttackDown OnAttackUp OnReload OnAimDown OnAimUp etc bc that could be more flexible

// double/three fire rifle kinda like in thsi game where you can chop limbs off using your gun (horizontally would help for crowds of opponent while vertical would shred their health by allowing hitting headshot + 2 bodyshots at once
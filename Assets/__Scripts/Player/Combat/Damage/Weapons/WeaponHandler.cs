using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

using Random = UnityEngine.Random;

using Projectiles;
using GameManagement;

[Serializable]
public sealed class WeaponHandler : NetworkBehaviour
    //, IPlayerFrameMember
{
    #region References

    [SerializeField] private Weapon currentWeapon;
    private WeaponStats currentWeaponStats;
    private float cameraTransformInitialZ;
    private Transform cameraTransform;
    private new Camera camera;
    private readonly int baseFOV = 60;
    private Transform recoilHandlerTransform;

    [SerializeField] private Transform barrelEnd;
    [SerializeField] private Transform weaponTransform;
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

    private bool isAiming;

    private static readonly float shootBuffer = .1f;
    private float lastShootPressed = float.NegativeInfinity;
    private bool HasBufferedShoot => lastShootPressed + shootBuffer > Time.time;

    #endregion

    #region Recoil Setup

    private Vector3 currentRecoilHandlerRotation;
    private Vector3 targetRecoilHandlerRotation;
    [SerializeField] private float recoilMovementSnappiness;

    private float RecoilRegulationSpeed => isAiming ? currentWeaponStats.AimingRecoilStats.RecoilRegulationSpeed : currentWeaponStats.HipfireRecoilStats.RecoilRegulationSpeed;

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
            currentCooldownBetweenRampUpShots = Mathf.Clamp(value, currentWeaponStats.RampUpStats.RampUpMinCooldownBetweenShots, currentWeaponStats.RampUpStats.RampUpMaxCooldownBetweenShots);
        }
    }


    #endregion

    //public PlayerFrame PlayerFrame { get; set; }

    //public void InitPlayerFrame(PlayerFrame playerFrame)
    //{
    //    PlayerFrame = playerFrame;
    //}

    #region Charge Setup

    private float timeStartedCharging;
    private bool holdingAttackKey = false;



    #endregion

    // methods

    #region Unity Handled

    private void Awake()
    {
        playerSettings = GetComponent<PlayerSettings>();
        recoilHandlerTransform = transform.GetChild(0).GetChild(0);
        cameraTransform = recoilHandlerTransform.GetChild(0);
        camera = cameraTransform.GetComponent<Camera>();

        InitWeapon();
        audioSourcePoolSize = 10;// (int)(Mathf.Max(currentWeaponSounds.ShootingSound.length, currentWeaponSounds.NearEmptyMagazineShootSound.length) / currentWeaponStats.CooldownBetweenShots);
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            audioSources.Add(gameObject.AddComponent<AudioSource>());
        }

        ownerFrame = GetComponent<PlayerFrame>();
    }

    private void FixedUpdate()
    {
        if (shotThisFrame) { return; }

        CurrentCooldownBetweenRampUpShots *= currentWeaponStats.RampUpStats.RampUpCooldownRegulationMultiplier;
    }

    private void Update()
    {
        HandleRecoil();
        HandleSpead();
        HandleKickback();
    }

    private void LateUpdate()
    {
        switchedThisFrame = false;
        shotThisFrame = false;
    }

    private void OnValidate()
    {
        InitWeapon();
    }

    #endregion

    #region Init

    public void SetWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
        InitWeapon();
    }

    public void InitWeapon()
    {
        currentWeaponStats = currentWeapon.Stats;
        currentWeaponSounds = currentWeapon.Sounds;
        ammos = currentWeaponStats.MagazineSize;

        canShoot = true;
        timeLastShotFired = float.MinValue;
        switchedThisFrame = true;

        SetShootingStyle(); // the actual shooting logic
        SetShootingRequestRythm(); // the rythm logic of the shots
        SetOnHitWallMethod();
    }

    private void SetShootingStyle()
    {
        shootingStyleMethod = currentWeaponStats.ShootingStyle switch
        {
            ShootingStyle.Single => currentWeaponStats.IsHitscan ? ExecuteSimpleHitscanShotClientRpc : ExecuteSimpleTravelTimeShotClientRpc,

            ShootingStyle.Shotgun => currentWeaponStats.IsHitscan ?
                () =>
                    {
                        SetShotgunPelletsDirections(barrelEnd);
                        ExecuteShotgunHitscanShotClientRpc();
                    }
                    :
                () =>
                    {
                        SetShotgunPelletsDirections(barrelEnd);
                        ExecuteShotgunTravelTimeShotClientRpc();
                    }
            ,

            _ => () => { },
        };
    }

    private void SetShootingRequestRythm()
    {
        shootingRythmMethod = currentWeaponStats.ShootingRythm switch
        {
            ShootingRythm.Single => RequestShotServerRpc,

            ShootingRythm.Burst => () =>
                {
                    StartCoroutine(ShootBurst(currentWeaponStats.BurstStats.BulletsPerBurst));
                }
            ,

            ShootingRythm.RampUp => () =>
                {
                    RequestShotServerRpc();
                    CurrentCooldownBetweenRampUpShots *= currentWeaponStats.RampUpStats.RampUpCooldownMultiplierPerShot;
                }
            ,

            ShootingRythm.Charge => () => { }
            ,

            _ => () => { },
        };
    }

    private void SetOnHitWallMethod()
    {
        onHitWallMethod = currentWeaponStats.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Classic => (_, _) => { },
            HitscanBulletActionOnHitWall.ThroughWalls => (_, _) => { },
            HitscanBulletActionOnHitWall.Explosive => ExplodeUponWallHit,
            HitscanBulletActionOnHitWall.BounceOnWalls => BounceUponWallHit,
            _ => (_, _) => { },
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
                if (currentWeaponStats.ShootingRythm == ShootingRythm.Charge)
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
        if (currentWeaponStats.ShootingStyle == ShootingStyle.Shotgun)
        {
            SetShotgunPelletsDirections(barrelEnd);
        }

        if (!IsOwner) { return; }

        if (timeLastShotFired + GetRelevantCooldown() > Time.time) { return; }

        if (currentWeaponStats.IsHitscan)
        {
            if (currentWeaponStats.ShootingStyle == ShootingStyle.Shotgun)
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
            if (currentWeaponStats.ShootingStyle == ShootingStyle.Shotgun)
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
        return currentWeaponStats.ShootingRythm switch
        {
            ShootingRythm.Single => currentWeaponStats.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == currentWeaponStats.BurstStats.BulletsPerBurst ? currentWeaponStats.CooldownBetweenShots : currentWeaponStats.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => currentWeaponStats.CooldownBetweenShots,
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

            yield return new WaitUntil(() => timerStart + currentWeaponStats.BurstStats.CooldownBetweenShotsOfBurst < Time.time || switchedThisFrame);

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
        var chargeRatio = timeCharged / currentWeaponStats.ChargeStats.ChargeDuration;
        if (chargeRatio >= currentWeaponStats.ChargeStats.MinChargeRatioToShoot)
        {
            var ammoConsumedByThisShot = (ushort)(currentWeaponStats.ChargeStats.AmmoConsumedByFullyChargedShot * chargeRatio);
            if (ammos < ammoConsumedByThisShot)
            {
                 RequestChargedShotServerRpc(ammos / currentWeaponStats.ChargeStats.AmmoConsumedByFullyChargedShot);
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
        if (!IsOwner) { return; }

        PlayShootingSound(currentWeaponStats.AmmoLeftInMagazineToWarn >= ammos);

        damageLogManager.UpdatePlayerSettings(DamageLogsSettings);
        timeLastShotFired = Time.time;
        shotThisFrame = true;
        ammos--;
        bulletFiredthisBurst++;
    }

    #region Execute Shot Hitscan

    private IHitscanBulletEffectSettings GetRelevantHitscanBulletSettings()
    {
        return currentWeaponStats.HitscanBulletSettings.ActionOnHitWall switch
        {
            HitscanBulletActionOnHitWall.Explosive => currentWeaponStats.HitscanBulletSettings.ExplodingBulletsSettings,
            HitscanBulletActionOnHitWall.BounceOnWalls => currentWeaponStats.HitscanBulletSettings.BouncingBulletsSettings,
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

    //    if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
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
    //                    shootableComponent.ReactShot(currentWeaponStats.Damage, hit.point, barrelEnd.forward, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
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
    //                shootableComponent.ReactShot(currentWeaponStats.Damage, hit.point, barrelEnd.forward, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
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

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);
        var endPoint = barrelEnd.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    //shootableComponent.ReactShot(currentWeaponStats.Damage, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.TeamID, currentWeaponStats.CanBreakThings);
                    shootableComponent.ReactShot(currentWeaponStats.Damage, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeaponStats.CanBreakThings);
                }

                if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeaponStats)
                    ),
                    GetRelevantHitscanBulletSettings()
                );
                
                if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }
                
            }
        }

        bulletTrail.Set(barrelEnd.position, endPoint);


        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //private void _ExecuteShotgunHitscanShotClientRpc()
    //{
    //    UpdateOwnerSettingsUponShot();

    //    if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
    //    {
    //        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
    //        {
    //            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
    //            var endPoint = barrelEnd.position + shotgunPelletsDirections[i] * 100;

    //            var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[i], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
    //            Array.Sort(hits, new RaycastHitComparer());

    //            foreach (var hit in hits)
    //            {
    //                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
    //                {
    //                    if (IsOwner)
    //                    {
    //                        shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[i], hit.point, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
    //                    }
    //                    else
    //                    {
    //                        endPoint = hit.point;
    //                        break;
    //                    }
    //                }
    //            }

    //            bulletTrail.Set(barrelEnd.position, endPoint);
    //        }
    //    }
    //    else
    //    {
    //        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
    //        {
    //            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
    //            if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
    //            {
    //                bulletTrail.Set(barrelEnd.position, hit.point);
    //                if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
    //                {
    //                    shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[i], hit.point, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
    //                }
    //            }
    //            else
    //            {
    //                bulletTrail.Set(barrelEnd.position, barrelEnd.position + shotgunPelletsDirections[i] * currentWeaponStats.ShotgunStats.PelletsRange);
    //            }
    //        }
    //    }

    //    if (!IsOwner) { return; }

    //    ApplyRecoil();
    //    ApplyKickback();
    //}

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunHitscanShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        for (int pelletIndex = 0; pelletIndex < currentWeaponStats.ShotgunStats.PelletsCount; pelletIndex++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.position + shotgunPelletsDirections[pelletIndex] * 100;

            var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[pelletIndex], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        //shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.TeamID, currentWeaponStats.CanBreakThings);
                        shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeaponStats.CanBreakThings);
                    }

                    if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
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
                                new(currentWeaponStats)
                            ),
                            GetRelevantHitscanBulletSettings()
                        );
                        
                        if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                        {
                            endPoint = hit.point;
                            break;
                        }
                        
                    }
                }
            }

            bulletTrail.Set(barrelEnd.position, endPoint);
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //private void _ExecuteChargedHitscanShotClientRpc(float chargeRatio)
    //{
    //    UpdateOwnerSettingsUponShot();

    //    var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
    //    var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);

    //    if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
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
    //                    shootableComponent.ReactShot(currentWeaponStats.Damage * chargeRatio, hit.point, barrelEnd.forward, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
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
    //                shootableComponent.ReactShot(currentWeaponStats.Damage * chargeRatio, hit.point, barrelEnd.forward, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
    //            }

    //        }
    //        else
    //        {
    //            bulletTrail.Set(barrelEnd.position, barrelEnd.position + directionWithSpread * 100);
    //        }
    //    }

    //    if (!IsOwner) { return; }

    //    ApplyRecoil(chargeRatio);
    //    ApplySpread();
    //    ApplyKickback(chargeRatio);
    //}

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);
        var endPoint = barrelEnd.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    //shootableComponent.ReactShot(currentWeaponStats.Damage * chargeRatio, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.TeamID, currentWeaponStats.CanBreakThings);
                    shootableComponent.ReactShot(currentWeaponStats.Damage * chargeRatio, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeaponStats.CanBreakThings);
                }

                if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
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
                        new(currentWeaponStats, chargeRatio)
                    ),
                    GetRelevantHitscanBulletSettings()
                );

                if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hit.point;
                    break;
                }
                
            }
        }

        bulletTrail.Set(barrelEnd.position, endPoint);


        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //private void _ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
    //{
    //    UpdateOwnerSettingsUponShot();

    //    if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
    //    {
    //        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
    //        {
    //            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
    //            var endPoint = barrelEnd.position + shotgunPelletsDirections[i] * 100;


    //            var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[i], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
    //            Array.Sort(hits, new RaycastHitComparer());

    //            foreach (var hit in hits)
    //            {
    //                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
    //                {
    //                    if (IsOwner)
    //                    {
    //                        shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[i], hit.point, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
    //                    } 
    //                }
    //                else
    //                {
    //                    endPoint = hit.point;
    //                    break;
    //                }
    //            }

    //            bulletTrail.Set(barrelEnd.position, endPoint);
    //        }
    //    }
    //    else
    //    {
    //        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
    //        {
    //            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
    //            if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
    //            {
    //                bulletTrail.Set(barrelEnd.position, hit.point);
    //                if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
    //                {
    //                    shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[i], hit.point, NetworkObjectId, _____placeHolderTeamID, currentWeaponStats.CanBreakThings);
    //                }
    //            }
    //            else
    //            {
    //                bulletTrail.Set(barrelEnd.position, barrelEnd.position + shotgunPelletsDirections[i] * currentWeaponStats.ShotgunStats.PelletsRange);
    //            }
    //        }
    //    }

    //    if (!IsOwner) { return; }

    //    ApplyRecoil();
    //    ApplyKickback();
    //}

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        for (int pelletIndex = 0; pelletIndex < currentWeaponStats.ShotgunStats.PelletsCount; pelletIndex++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
            var endPoint = barrelEnd.position + shotgunPelletsDirections[pelletIndex] * 100;


            var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[pelletIndex], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            foreach (var hit in hits)   
            {
                if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        //shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.TeamID, currentWeaponStats.CanBreakThings);
                        shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeaponStats.CanBreakThings);
                    }

                    if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
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
                            new(currentWeaponStats, chargeRatio)
                            ),
                        GetRelevantHitscanBulletSettings()
                    );

                    if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hit.point;
                        break;
                    }
                    
                }
            }

            bulletTrail.Set(barrelEnd.position, endPoint);
        }

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

        var projectile = Instantiate(
            currentWeaponStats.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.position,
            Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnd))
            ).GetComponent<Projectile>();
            
        if (projectile == null)
        {
            throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");
        }

        projectile.Init(
            currentWeaponStats.Damage,
            currentWeaponStats.TravelTimeBulletSettings.BulletSpeed,
            currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeaponStats.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            //PlayerFrame.TeamID
            PlayerFrame.LocalPlayer.TeamNumber
        );

        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunTravelTimeShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
        {
            
            var projectile = Instantiate(
                currentWeaponStats.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.position,
                Quaternion.LookRotation(shotgunPelletsDirections[i])
            ).GetComponent<Projectile>();

            if (projectile == null)
            {
                throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");
            }

            projectile.Init(
                currentWeaponStats.ShotgunStats.PelletsDamage,
                currentWeaponStats.TravelTimeBulletSettings.BulletSpeed,
                currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeaponStats.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                //PlayerFrame.TeamID
                PlayerFrame.LocalPlayer.TeamNumber
            );
            
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedTravelTimeShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var projectile = Instantiate(
            currentWeaponStats.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.position,
            Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnd))
        ).GetComponent<Projectile>();

        if (projectile == null)
        {
            throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");
        }

        projectile.Init(
            currentWeaponStats.Damage * chargeRatio,
            currentWeaponStats.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
            currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeaponStats.CanBreakThings,
            layersToHit,
            GetRelevantHitWallBehaviour(),
            GetRelevantHitPlayerBehaviour(),
            //PlayerFrame.TeamID
            PlayerFrame.LocalPlayer.TeamNumber
        );
        

        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunTravelTimeShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
        {
            var projectile = Instantiate(
                currentWeaponStats.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.position,
                Quaternion.LookRotation(shotgunPelletsDirections[i])
            ).GetComponent<Projectile>();

            if (projectile == null)
            {
                throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");
            }

            projectile.Init(
                currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio,
                currentWeaponStats.TravelTimeBulletSettings.BulletSpeed,
                currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeaponStats.CanBreakThings,
                layersToHit,
                GetRelevantHitWallBehaviour(),
                GetRelevantHitPlayerBehaviour(),
                //PlayerFrame.TeamID
                PlayerFrame.LocalPlayer.TeamNumber
            );
        }

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

        var newShotDirection = Utility.ReflectVector(shotInfos.ShotDirection, shotInfos.Hit.normal);

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
                            new(currentWeaponStats)
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
        return currentWeaponStats.TravelTimeBulletSettings.OnHitWallBehaviour switch
        {
            ProjectileBehaviourOnHitWall.Stop => new ProjectileStopOnHitWall(null),
            ProjectileBehaviourOnHitWall.Pierce => new ProjectilePierceOnHitWall(currentWeaponStats.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallPierceParams),
            ProjectileBehaviourOnHitWall.Bounce => new ProjectileBounceOnHitWall(currentWeaponStats.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallBounceParams),
            ProjectileBehaviourOnHitWall.Explode => new ProjectileExplodeOnHitWall(currentWeaponStats.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }


    private ProjectileOnHitPlayerBehaviour GetRelevantHitPlayerBehaviour()
    {
        return currentWeaponStats.TravelTimeBulletSettings.OnHitPlayerBehaviour switch
        {
            ProjectileBehaviourOnHitPlayer.Stop => new ProjectileStopOnHitPlayer(null),
            ProjectileBehaviourOnHitPlayer.Pierce => new ProjectilePierceOnHitPlayer(currentWeaponStats.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerPierceParams),
            ProjectileBehaviourOnHitPlayer.Explode => new ProjectileExplodeOnHitPlayer(currentWeaponStats.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerExplodeParams),
            _ => throw new NotImplementedException(),
        };
    }

    #endregion

    #region Setting up Shotgun Shot

    private void SetShotgunPelletsDirections(Transform directionTranform)
    {
        shotgunPelletsDirections = new Vector3[currentWeaponStats.ShotgunStats.PelletsCount];

        var relevantSpread = isAiming ? currentWeaponStats.ShotgunStats.AimingPelletsSpreadAngle : currentWeaponStats.ShotgunStats.PelletsSpreadAngle;

        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
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
        if (!currentWeaponStats.NeedReload) { return; }

        if (ammos == currentWeaponStats.MagazineSize) { return; } // already fully loaded

        StartCoroutine(currentWeaponStats.TimeToReloadOneRound == 0f ? ExecuteReload() : ExecuteReloadRoundPerRound());
    }

    private IEnumerator ExecuteReload()
    {
        canShoot = false;

        var timerStart = Time.time;
        yield return new WaitUntil(() => timerStart + currentWeaponStats.ReloadSpeed < Time.time || switchedThisFrame);

        canShoot = true;

        if (switchedThisFrame) { yield break; }

        ammos = currentWeaponStats.MagazineSize;
    }

    private IEnumerator ExecuteReloadRoundPerRound()
    {
        var ammosToReload = currentWeaponStats.MagazineSize - ammos;

        for (int i = 0; i < ammosToReload; i++)
        {
            var timerStart = Time.time;
            yield return new WaitUntil(
                () => timerStart + currentWeaponStats.TimeToReloadOneRound < Time.time ||
                switchedThisFrame ||
                HasBufferedShoot
                ); // + buffer a shoot input that would interrupt the reload

            if (switchedThisFrame) { yield break; }

            if (HasBufferedShoot)
            {
                if (currentWeaponStats.ShootingRythm == ShootingRythm.Charge)
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

    #region Aiming

    public void UpdateAimingState(bool shoulbBeAiming)
    {
        if (isAiming == shoulbBeAiming) { return; }

        isAiming = shoulbBeAiming;
        //StartCoroutine(ToggleAim(shoulbBeAiming));
        StartCoroutine(ToggleAimFOV(shoulbBeAiming));
    }

    private IEnumerator ToggleAimMovement(bool shouldBeAiming)
    {
        var startingPointZ = cameraTransform.localPosition.z;
        var elapsedTime = 0f;
        var (targetZ, targetDuration) = shouldBeAiming ?
            (currentWeaponStats.AimingAndScopeStats.AimingFOV, currentWeaponStats.AimingAndScopeStats.TimeToADS)
            :
            (cameraTransformInitialZ, currentWeaponStats.AimingAndScopeStats.TimeToUnADS);
        while (elapsedTime < targetDuration)
        {
            cameraTransform.localPosition = new(0f, 0f, Mathf.Lerp(startingPointZ, targetZ, elapsedTime / targetDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ToggleAimFOV(bool shouldBeAiming)
    {
        var startingPoint = camera.fieldOfView;
        var elapsedTime = 0f;
        var (target, targetDuration) = shouldBeAiming ?
            (GetRelevantFOV(), currentWeaponStats.AimingAndScopeStats.TimeToADS)
            :
            (baseFOV, currentWeaponStats.AimingAndScopeStats.TimeToUnADS);
        while (elapsedTime < targetDuration)
        {
            camera.fieldOfView = Mathf.Lerp(startingPoint, target, elapsedTime / targetDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

    }

    private float GetRelevantFOV()
    {
        return currentWeaponStats.AimingAndScopeStats.ScopeMagnification == 1f ?
            currentWeaponStats.AimingAndScopeStats.AimingFOV
            :
            baseFOV / currentWeaponStats.AimingAndScopeStats.ScopeMagnification
            ;
    }

    #endregion

    #region Handle Recoil

    private void ApplyRecoil()
    {
        if (isAiming)
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeaponStats.AimingRecoilStats.RecoilForceX,
                Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceY, currentWeaponStats.AimingRecoilStats.RecoilForceY),
                Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceZ, currentWeaponStats.AimingRecoilStats.RecoilForceZ)
            );
        }
        else
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeaponStats.HipfireRecoilStats.RecoilForceX,
                Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceY, currentWeaponStats.HipfireRecoilStats.RecoilForceY),
                Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceZ, currentWeaponStats.HipfireRecoilStats.RecoilForceZ)
            );
        }
    }
    private void ApplyRecoil(float chargeRatio)
    {
        if (isAiming)
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeaponStats.AimingRecoilStats.RecoilForceX * chargeRatio,
                Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceY * chargeRatio, currentWeaponStats.AimingRecoilStats.RecoilForceY * chargeRatio),
                Random.Range(-currentWeaponStats.AimingRecoilStats.RecoilForceZ * chargeRatio, currentWeaponStats.AimingRecoilStats.RecoilForceZ * chargeRatio)
            );
        }
        else
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeaponStats.HipfireRecoilStats.RecoilForceX * chargeRatio,
                Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceY * chargeRatio, currentWeaponStats.HipfireRecoilStats.RecoilForceY * chargeRatio),
                Random.Range(-currentWeaponStats.HipfireRecoilStats.RecoilForceZ * chargeRatio, currentWeaponStats.HipfireRecoilStats.RecoilForceZ * chargeRatio)
            );
        }
    }

    private void HandleRecoil()
    {
        targetRecoilHandlerRotation = Vector3.Lerp(targetRecoilHandlerRotation, Vector3.zero, RecoilRegulationSpeed * Time.deltaTime);
        currentRecoilHandlerRotation = Vector3.Slerp(currentRecoilHandlerRotation, targetRecoilHandlerRotation, recoilMovementSnappiness * Time.deltaTime);
        recoilHandlerTransform.localRotation = Quaternion.Euler(currentRecoilHandlerRotation);
    }

    #endregion

    #region Handle Kickback

    private void ApplyKickback()
    {
        weaponTransform.localPosition -= new Vector3(0f, 0f, currentWeaponStats.KickbackStats.WeaponKickBackPerShot);
    }
    private void ApplyKickback(float chargeRatio)
    {
        weaponTransform.localPosition -= new Vector3(0f, 0f, currentWeaponStats.KickbackStats.WeaponKickBackPerShot * chargeRatio);
    }

    private void HandleKickback()
    {
        weaponTransform.localPosition = Vector3.Slerp(weaponTransform.localPosition, Vector3.zero, currentWeaponStats.KickbackStats.WeaponKickBackRegulationTime * Time.time);
    }

    #endregion

    #region Handle Spread

    private void ApplySpread()
    {
        currentSpreadAngle += isAiming ? currentWeaponStats.AimingSimpleShotStats.SpreadAngleAddedPerShot : currentWeaponStats.SimpleShotStats.SpreadAngleAddedPerShot;
    }

    private void HandleSpead()
    {
        currentSpreadAngle = Mathf.Lerp(currentSpreadAngle, 0f, (isAiming ? currentWeaponStats.AimingSimpleShotStats.SpreadRegulationSpeed : currentWeaponStats.SimpleShotStats.SpreadRegulationSpeed) * Time.time);
    }

    private Vector3 GetDirectionWithSpread(float spreadAngle, Transform directionTransform)
    {
        var spreadStrength = spreadAngle / 45f;
        /*Most fucked explanantion to ever cross the frontier of reality
         / 45f -> to get value which we can use in a vector instead of an angle
        ex in 2D:  a vector that has a 45� angle above X has a (1, 1) direction
        while the X has a (1, 0)
        so we essentially brought the 45� to a value we could use as a direction in the vector
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

    //private Vector3 GetDirectionWithSpread(float spreadAngle, Vector3 direction)
    //{
    //    var spreadStrength = spreadAngle / 45f;
    //    /*Most fucked explanantion to ever cross the frontier of reality
    //     / 45f -> to get value which we can use iun a vector instead of an angle
    //    ex in 2D:  a vector that has a 45� angle above X has a (1, 1) direction
    //    while the X has a (1, 0)
    //    so we essentially brought the 45� to a value we could use as a direction in the vector
    //     */
    //    // perhaps do directionTransform.forward * 45 instead of other / 45 (for performances purposes)
    //    return (
    //            direction + direction.TransformDirection(
    //                new Vector3(
    //                    Random.Range(-spreadStrength, spreadStrength),
    //                    Random.Range(-spreadStrength, spreadStrength),
    //                    0
    //                )
    //            )
    //        ).normalized;
    //}

    #endregion

    #region Handle Sound

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

    private void PlayReloadSound()
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

    public WeaponInfos(WeaponStats weaponStats, float chargeRatio = 1f)
    {
        Damage = (weaponStats.ShootingStyle == ShootingStyle.Single ? weaponStats.Damage : weaponStats.ShotgunStats.PelletsDamage) * chargeRatio;
        CanBreakThings = weaponStats.CanBreakThings;
        Effects = weaponStats.EffectsInflicted;
        PierceThroughPlayers = weaponStats.HitscanBulletSettings.PierceThroughPlayers;
        ActionOnHitWall = weaponStats.HitscanBulletSettings.ActionOnHitWall;
    }
}


// create a weapon abstract clas instead? with OnAttackDown OnAttackUp OnReload OnAimDown OnAimUp etc bc that could be more flexible
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class WeaponHandler : NetworkBehaviour
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
    private Action<Vector3, RaycastHit, IHitscanBulletEffectSettings, ulong> onHitWallMethod; // to avoid throwing an error

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

    #region Charge Setup

    private float timeStartedCharging;
    private bool holdingAttackKey = false;



    #endregion

    // methods

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
            HitscanBulletActionOnHitWall.Classic => (_, _, _, _) => { },
            HitscanBulletActionOnHitWall.ThroughWalls => (_, _, _, _) => { },
            HitscanBulletActionOnHitWall.Explosive => (_, _, _, _) => { },
            HitscanBulletActionOnHitWall.BounceOnWalls => (_, _, _, _) => { },
            _ => (_, _, _, _) => { },
        };
    }

    #endregion

    public void UpdateState(bool holdingAttackKey_)
    {
        if (holdingAttackKey_ != holdingAttackKey)
        {
            holdingAttackKey = holdingAttackKey_;
            if (holdingAttackKey_) // just started pressing the attack key
            {
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

        if (timeLastShotFired + GetRelevantCooldown(false) > Time.time) { return; }

        _ = GetRelevantCooldown(true);

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

        if (timeLastShotFired + GetRelevantCooldown(false) > Time.time) { return; }

        _ = GetRelevantCooldown(true);

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

    private float GetRelevantCooldown(bool doDebug)
    {
        var cd = currentWeaponStats.ShootingRythm switch
        {
            ShootingRythm.Single => currentWeaponStats.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == currentWeaponStats.BurstStats.BulletsPerBurst ? currentWeaponStats.CooldownBetweenShots : currentWeaponStats.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => currentWeaponStats.CooldownBetweenShots,
            _ => 0f,
        };
        if (doDebug) print(cd);
        return cd;
    }

    private IEnumerator ShootBurst(int bullets)
    {
        if (ammos <= 0) { yield break; }

        if (timeLastShotFired + GetRelevantCooldown(false) > Time.time) { yield break; }

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

        if (timeLastShotFired + GetRelevantCooldown(false) > Time.time) { yield break; }

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

    #region Execute Shot

    private void UpdateOwnerSettingsUponShot()
    {
        if (!IsOwner) {  return; }

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

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleHitscanShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);

        if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
        {
            var endPoint = barrelEnd.position + directionWithSpread * 100;
            
            var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

            Array.Sort(hits, new RaycastHitComparer());

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(currentWeaponStats.Damage, hits[i].point, barrelEnd.forward, NetworkObjectId, currentWeaponStats.CanBreakThings);
                    }
                }
                else // so far else is the wall but do proper checks later on
                {
                    endPoint = hits[i].point;
                    break;
                }
            }

            bulletTrail.Set(barrelEnd.position, endPoint);

        }
        else
        {
            if (Physics.Raycast(barrelEnd.position, directionWithSpread, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
            {
                bulletTrail.Set(barrelEnd.position, hit.point);
                if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot(currentWeaponStats.Damage, hit.point, barrelEnd.forward, NetworkObjectId, currentWeaponStats.CanBreakThings);
                }
            }
            else
            {
                bulletTrail.Set(barrelEnd.position, barrelEnd.position + directionWithSpread * 100);
            }
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleHitscanShotClientRpc__()
    {
        UpdateOwnerSettingsUponShot();

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);


        var endPoint = barrelEnd.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    shootableComponent.ReactShot(currentWeaponStats.Damage, hits[i].point, barrelEnd.forward, NetworkObjectId, currentWeaponStats.CanBreakThings);
                }

                if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
                {
                    endPoint = hits[i].point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hits[i].point;
                    break;
                }
                else
                {
                    onHitWallMethod(directionWithSpread, hits[i], GetRelevantHitscanBulletSettings(), NetworkObjectId);
                }
            }
        }

        bulletTrail.Set(barrelEnd.position, endPoint);


        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunHitscanShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
        {
            for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
            {
                var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();

                var endPoint = barrelEnd.position + shotgunPelletsDirections[i];

                var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[i], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, new RaycastHitComparer());

                for (int j = 0; j < hits.Length; j++)
                {
                    if (hits[j].collider.TryGetComponent<IShootable>(out var shootableComponent))
                    {
                        if (IsOwner)
                        {
                            shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[i], hits[j].point, NetworkObjectId, currentWeaponStats.CanBreakThings);
                        }
                        else
                        {
                            endPoint = hits[j].point;
                            break;
                        }
                    }
                }

                bulletTrail.Set(barrelEnd.position, endPoint);
            }
        }
        else
        {
            for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
            {
                var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
                if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
                {
                    bulletTrail.Set(barrelEnd.position, hit.point);
                    if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                    {
                        shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[i], hit.point, NetworkObjectId, currentWeaponStats.CanBreakThings);
                    }
                }
                else
                {
                    bulletTrail.Set(barrelEnd.position, barrelEnd.position + shotgunPelletsDirections[i] * currentWeaponStats.ShotgunStats.PelletsRange);
                }
            }
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunHitscanShotClientRpc__()
    {
        UpdateOwnerSettingsUponShot();

        
        for (int pelletIndex = 0; pelletIndex < currentWeaponStats.ShotgunStats.PelletsCount; pelletIndex++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();

            var endPoint = barrelEnd.position + shotgunPelletsDirections[pelletIndex];

            var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[pelletIndex], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot(currentWeaponStats.ShotgunStats.PelletsDamage, shotgunPelletsDirections[pelletIndex], hits[i].point, NetworkObjectId, currentWeaponStats.CanBreakThings);
                    }

                    if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hits[i].point;
                        break;
                    }

                    else
                    {
                        if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                        {
                            endPoint = hits[i].point;
                            break;
                        }
                        else
                        {
                            onHitWallMethod(shotgunPelletsDirections[pelletIndex], hits[i], GetRelevantHitscanBulletSettings(), NetworkObjectId);
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

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);

        if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
        {
            var endPoint = barrelEnd.position + directionWithSpread * 100;

            var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

            Array.Sort(hits, new RaycastHitComparer());

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot((ushort)(currentWeaponStats.Damage * chargeRatio), hits[i].point, barrelEnd.forward, NetworkObjectId, currentWeaponStats.CanBreakThings);
                    }
                }
                else // so far else is the wall but do proper checks later on
                {
                    endPoint = hits[i].point;
                    break;
                }
            }

            bulletTrail.Set(barrelEnd.position, endPoint);

        }
        else
        {
            if (Physics.Raycast(barrelEnd.position, directionWithSpread, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
            {
                bulletTrail.Set(barrelEnd.position, hit.point);
                if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot((ushort)(currentWeaponStats.Damage * chargeRatio), hit.point, barrelEnd.forward, NetworkObjectId, currentWeaponStats.CanBreakThings);
                }

            }
            else
            {
                bulletTrail.Set(barrelEnd.position, barrelEnd.position + directionWithSpread * 100);
            }
        }

        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedHitscanShotClientRpc__(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);

        
        var endPoint = barrelEnd.position + directionWithSpread * 100;

        var hits = Physics.RaycastAll(barrelEnd.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        Array.Sort(hits, new RaycastHitComparer());

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.TryGetComponent<IShootable>(out var shootableComponent))
            {
                if (IsOwner)
                {
                    shootableComponent.ReactShot((ushort)(currentWeaponStats.Damage * chargeRatio), hits[i].point, barrelEnd.forward, NetworkObjectId, currentWeaponStats.CanBreakThings);
                }

                if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
                {
                    endPoint = hits[i].point;
                    break;
                }
            }
            else // so far else is the wall but do proper checks later on
            {
                if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                {
                    endPoint = hits[i].point;
                    break;
                }
                else
                {
                    onHitWallMethod(directionWithSpread, hits[i], GetRelevantHitscanBulletSettings(), NetworkObjectId);
                }
            }
        }

        bulletTrail.Set(barrelEnd.position, endPoint);


        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        if (currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
        {
            for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
            {
                var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();

                var endPoint = barrelEnd.position + shotgunPelletsDirections[i];

                var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[i], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, new RaycastHitComparer());

                for (int j = 0; j < hits.Length; j++)
                {
                    if (hits[j].collider.TryGetComponent<IShootable>(out var shootableComponent))
                    {
                        if (IsOwner)
                        {
                            shootableComponent.ReactShot((ushort)(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio), shotgunPelletsDirections[i], hits[j].point, NetworkObjectId, currentWeaponStats.CanBreakThings);
                        } 
                    }
                    else
                    {
                        endPoint = hits[j].point;
                        break;
                    }
                }

                bulletTrail.Set(barrelEnd.position, endPoint);
            }
        }
        else
        {
            for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
            {
                var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
                if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
                {
                    bulletTrail.Set(barrelEnd.position, hit.point);
                    if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                    {
                        shootableComponent.ReactShot((ushort)(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio), shotgunPelletsDirections[i], hit.point, NetworkObjectId, currentWeaponStats.CanBreakThings);
                    }
                }
                else
                {
                    bulletTrail.Set(barrelEnd.position, barrelEnd.position + shotgunPelletsDirections[i] * currentWeaponStats.ShotgunStats.PelletsRange);
                }
            }
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunHitscanShotClientRpc__(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        
        for (int pelletIndex = 0; pelletIndex < currentWeaponStats.ShotgunStats.PelletsCount; pelletIndex++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();

            var endPoint = barrelEnd.position + shotgunPelletsDirections[pelletIndex];

            var hits = Physics.RaycastAll(barrelEnd.position, shotgunPelletsDirections[pelletIndex], currentWeaponStats.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, new RaycastHitComparer());

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    if (IsOwner)
                    {
                        shootableComponent.ReactShot((ushort)(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio), shotgunPelletsDirections[pelletIndex], hits[i].point, NetworkObjectId, currentWeaponStats.CanBreakThings);
                    }

                    if (!currentWeaponStats.HitscanBulletSettings.PierceThroughPlayers)
                    {
                        endPoint = hits[i].point;
                        break;
                    }
                }
                else
                {
                    if (currentWeaponStats.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
                    {
                        endPoint = hits[i].point;
                        break;
                    }
                    else
                    {
                        onHitWallMethod(shotgunPelletsDirections[pelletIndex], hits[i], GetRelevantHitscanBulletSettings(), NetworkObjectId);
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
            ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");
        
        projectile.Init(
            currentWeaponStats.Damage,
            currentWeaponStats.TravelTimeBulletSettings.BulletSpeed,
            currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeaponStats.CanBreakThings,
            layersToHit
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
            ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");

            projectile.Init(
                currentWeaponStats.ShotgunStats.PelletsDamage,
                currentWeaponStats.TravelTimeBulletSettings.BulletSpeed,
                currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeaponStats.CanBreakThings,
                layersToHit
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
        ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");

        projectile.Init(
            (ushort)(currentWeaponStats.Damage * chargeRatio),
            currentWeaponStats.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
            currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeaponStats.CanBreakThings,
            layersToHit
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
            ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");

            projectile.Init(
                (ushort)(currentWeaponStats.ShotgunStats.PelletsDamage * chargeRatio),
                currentWeaponStats.TravelTimeBulletSettings.BulletSpeed,
                currentWeaponStats.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeaponStats.CanBreakThings,
                layersToHit
            );
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    #endregion

    #endregion


    private void ExplodeUponWallHit(Vector3 shotDirection, RaycastHit hit, int bounceLeft, ulong attackerNetworkID)
    {

    }


    private void BounceUponWallHit(Vector3 shotDirection, RaycastHit hit, int bounceLeft, ulong attackerNetworkID)
    {
        var normal = hit.normal;
        var newDirection = Vector3.zero;
    }

    private Vector3 ReflectVector(Vector3 vectorToReflect, Vector3 normalVector)
    {
        var normalizedNormal = normalVector.normalized;

        var dotProduct = Vector3.Dot(vectorToReflect, normalizedNormal);

        return vectorToReflect - 2 * dotProduct * normalizedNormal;
    }





    private void SetShotgunPelletsDirections(Transform directionTranform)
    {
        shotgunPelletsDirections = new Vector3[currentWeaponStats.ShotgunStats.PelletsCount];
        for (int i = 0; i < currentWeaponStats.ShotgunStats.PelletsCount; i++)
        {
            shotgunPelletsDirections[i] = GetDirectionWithSpread(isAiming ? currentWeaponStats.ShotgunStats.AimingPelletsSpreadAngle : currentWeaponStats.ShotgunStats.PelletsSpreadAngle, directionTranform);
        }
    }

    # region Reload

    public void Reload()
    {
        if (!currentWeaponStats.NeedReload) { return; }

        StartCoroutine(ExecuteReload());
    }

    private IEnumerator ExecuteReload()
    {
        if (ammos == currentWeaponStats.MagazineSize) { yield break; } // already fully loaded

        canShoot = false;

        var timerStart = Time.time;
        yield return new WaitUntil(() => timerStart + currentWeaponStats.ReloadSpeed < Time.time || switchedThisFrame);

        canShoot = true;

        if (switchedThisFrame) { yield break; }

        ammos = currentWeaponStats.MagazineSize;
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
        //print($"called with value: {}");
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
         / 45f -> to get value which we can use iun a vector instead of an angle
        ex in 2D:  a vector that has a 45° angle above X has a (1, 1) direction
        while the X has a (1, 0)
        so we essentially brought the 45° to a value we could use as a direction in the vector
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
    //    ex in 2D:  a vector that has a 45° angle above X has a (1, 1) direction
    //    while the X has a (1, 0)
    //    so we essentially brought the 45° to a value we could use as a direction in the vector
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
        for (int i = 0; i < audioSources.Count; i++)
        {
            audioSources[i].Stop();
        }

        audioSources[0].clip = currentWeaponSounds.ReloadSound;
        audioSources[0].Play();
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

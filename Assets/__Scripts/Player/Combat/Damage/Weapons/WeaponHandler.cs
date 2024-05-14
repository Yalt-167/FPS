using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class WeaponHandler : NetworkBehaviour
{
    #region References

    [SerializeField] private WeaponStats currentWeapon;
    private float cameraTransformInitialZ;
    private Transform cameraTransform;
    private new Camera camera;
    private int baseFOV = 60;
    private Transform recoilHandlerTransform;

    [SerializeField] private Transform barrelEnd;
    [SerializeField] private Transform weaponTransform;
    [SerializeField] private LayerMask layersToHit;
    [SerializeField] private GameObject bulletTrailPrefab;

    private PlayerSettings playerSettings;
    private DamageLogSettings DamageLogsSettings => playerSettings.DamageLogSettings;
    [SerializeField] private DamageLogManager damageLogManager;

    #endregion

    #region Global Setup

    private float timeLastShotFired = float.NegativeInfinity;
    private bool switchedThisFrame;
    private bool shotThisFrame;
    private ushort ammos;

    private bool canShoot;


    private Action shootingStyleMethod;
    private Action shootingRythmMethod;
    private Action updateMethod;

    private bool isAiming;

    private static readonly float shootBuffer = .1f;
    private float lastShootPressed = float.NegativeInfinity;
    private bool HasBufferedShoot => lastShootPressed + shootBuffer > Time.time;

    #endregion

    #region Recoil Setup

    private Vector3 currentRecoilHandlerRotation;
    private Vector3 targetRecoilHandlerRotation;
    [SerializeField] private float recoilMovementSnappiness;

    private float RecoilRegulationSpeed => isAiming ? currentWeapon.AimingRecoilStats.RecoilRegulationSpeed : currentWeapon.HipfireRecoilStats.RecoilRegulationSpeed;

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

    private void Awake()
    {
        playerSettings = GetComponent<PlayerSettings>();
        recoilHandlerTransform = transform.GetChild(0).GetChild(0);
        cameraTransform = recoilHandlerTransform.GetChild(0);
        camera = cameraTransform.GetComponent<Camera>();
        switchedThisFrame = false;
        InitGun();
    }

    private void FixedUpdate()
    {
        if (shotThisFrame) { return; }

        CurrentCooldownBetweenRampUpShots *= currentWeapon.RampUpStats.RampUpCooldownRegulationMultiplier;
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
        InitGun();
    }

    #region Init

    public void InitGun()
    {
        ammos = currentWeapon.MagazineSize;

        canShoot = true;
        timeLastShotFired = float.MinValue;

        SetShootingStyle(); // the actual shooting logic
        SetShootingRequestRythm(); // the rythm logic of the shots

    }

    private void SetShootingStyle()
    {
        shootingStyleMethod = currentWeapon.ShootingStyle switch
        {
            ShootingStyle.Single => currentWeapon.IsHitscan ? ExecuteSimpleHitscanShotClientRpc : ExecuteSimpleTravelTimeShotClientRpc,

            ShootingStyle.Shotgun => currentWeapon.IsHitscan ?
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

            _ => () => { },
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
        if (currentWeapon.ShootingStyle == ShootingStyle.Shotgun)
        {
            SetShotgunPelletsDirections(barrelEnd);
        }

        if (!IsOwner) { return; }

        if (timeLastShotFired + GetRelevantCooldown(false) > Time.time) { return; }

        _ = GetRelevantCooldown(true);

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

    private float GetRelevantCooldown(bool doDebug)
    {
        var cd = currentWeapon.ShootingRythm switch
        {
            ShootingRythm.Single => currentWeapon.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == currentWeapon.BurstStats.BulletsPerBurst ? currentWeapon.CooldownBetweenShots : currentWeapon.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => currentWeapon.CooldownBetweenShots,
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

            yield return new WaitUntil(() => timerStart + currentWeapon.BurstStats.CooldownBetweenShotsOfBurst < Time.time || switchedThisFrame);

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

    #region Execute Shot

    private void UpdateOwnerSettingsUponShot()
    {
        if (!IsOwner) {  return; }

        if (currentWeapon.AmmoLeftInMagazineToWarn >= ammos)
        {
            WarnThatMagazineIsRunningOut();
        }

        damageLogManager.UpdatePlayerSettings(DamageLogsSettings);
        timeLastShotFired = Time.time;
        shotThisFrame = true;
        ammos--;
        bulletFiredthisBurst++;


    }

    #region Execute Shot Hitscan

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteSimpleHitscanShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnd);
        if (Physics.Raycast(barrelEnd.position, directionWithSpread, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
        {
            bulletTrail.Set(barrelEnd.position, hit.point);
            if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnd.forward, NetworkObjectId, currentWeapon.CanBreakThings);
            }

            //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);

        }
        else
        {
            bulletTrail.Set(barrelEnd.position, barrelEnd.position + directionWithSpread * 100);
        }



        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplySpread();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteShotgunHitscanShotClientRpc()
    {
        UpdateOwnerSettingsUponShot();

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
            if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
            {
                bulletTrail.Set(barrelEnd.position, hit.point);
                if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage, shotgunPelletsDirections[i], hit.point, NetworkObjectId, currentWeapon.CanBreakThings);
                }

                //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);
            }
            else
            {
                bulletTrail.Set(barrelEnd.position, barrelEnd.position + shotgunPelletsDirections[i] * currentWeapon.ShotgunStats.PelletsRange);
            }
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
        if (Physics.Raycast(barrelEnd.position, directionWithSpread, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
        {
            bulletTrail.Set(barrelEnd.position, hit.point);
            if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot((ushort)(currentWeapon.Damage * chargeRatio), hit.point, barrelEnd.forward, NetworkObjectId, currentWeapon.CanBreakThings);
            }

            //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);

        }
        else
        {
            bulletTrail.Set(barrelEnd.position, barrelEnd.position + directionWithSpread * 100);
        }

        if (!IsOwner) { return; }

        ApplyRecoil(chargeRatio);
        ApplySpread();
        ApplyKickback(chargeRatio);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
    {
        UpdateOwnerSettingsUponShot();

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
            if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
            {
                bulletTrail.Set(barrelEnd.position, hit.point);
                if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot((ushort)(currentWeapon.ShotgunStats.PelletsDamage * chargeRatio), shotgunPelletsDirections[i], hit.point, NetworkObjectId, currentWeapon.CanBreakThings);
                }

                //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);
            }
            else
            {
                bulletTrail.Set(barrelEnd.position, barrelEnd.position + shotgunPelletsDirections[i] * currentWeapon.ShotgunStats.PelletsRange);
            }
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
            currentWeapon.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.position,
            Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnd))
            ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");
        
        projectile.Init(
            currentWeapon.Damage,
            currentWeapon.TravelTimeBulletSettings.BulletSpeed,
            currentWeapon.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeapon.CanBreakThings,
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

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            var projectile = Instantiate(
                currentWeapon.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.position,
                Quaternion.LookRotation(shotgunPelletsDirections[i])
            ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");

            projectile.Init(
                currentWeapon.ShotgunStats.PelletsDamage,
                currentWeapon.TravelTimeBulletSettings.BulletSpeed,
                currentWeapon.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeapon.CanBreakThings,
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
            currentWeapon.TravelTimeBulletSettings.BulletPrefab,
            barrelEnd.position,
            Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnd))
        ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");

        projectile.Init(
            (ushort)(currentWeapon.Damage * chargeRatio),
            currentWeapon.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
            currentWeapon.TravelTimeBulletSettings.BulletDrop,
            NetworkObjectId,
            currentWeapon.CanBreakThings,
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

        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            var projectile = Instantiate(
                currentWeapon.TravelTimeBulletSettings.BulletPrefab,
                barrelEnd.position,
                Quaternion.LookRotation(shotgunPelletsDirections[i])
            ).GetComponent<Projectile>() ?? throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it");

            projectile.Init(
                (ushort)(currentWeapon.ShotgunStats.PelletsDamage * chargeRatio),
                currentWeapon.TravelTimeBulletSettings.BulletSpeed,
                currentWeapon.TravelTimeBulletSettings.BulletDrop,
                NetworkObjectId,
                currentWeapon.CanBreakThings,
                layersToHit
            );
        }

        if (!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    #endregion

    #endregion

    private void SetShotgunPelletsDirections(Transform directionTranform)
    {
        shotgunPelletsDirections = new Vector3[currentWeapon.ShotgunStats.PelletsCount];
        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            shotgunPelletsDirections[i] = GetDirectionWithSpread(isAiming ? currentWeapon.AimingShotgunStats.PelletsSpreadAngle : currentWeapon.ShotgunStats.PelletsSpreadAngle, directionTranform);
        }
    }

    # region Reload

    public void Reload()
    {
        if (!currentWeapon.NeedReload) { return; }

        StartCoroutine(ExecuteReload());
    }

    private IEnumerator ExecuteReload()
    {
        if (ammos == currentWeapon.MagazineSize) { yield break; } // already fully loaded

        canShoot = false;

        var timerStart = Time.time;
        yield return new WaitUntil(() => timerStart + currentWeapon.ReloadSpeed < Time.time || switchedThisFrame);

        canShoot = true;

        if (switchedThisFrame) { yield break; }

        ammos = currentWeapon.MagazineSize;
    }

    private void WarnThatMagazineIsRunningOut()
    {
        print("You ve been warned that ur magazine is near empty");
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
            (currentWeapon.AimingAndScopeStats.AimingFOV, currentWeapon.AimingAndScopeStats.TimeToADS)
            :
            (cameraTransformInitialZ, currentWeapon.AimingAndScopeStats.TimeToUnADS);
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
            (GetRelevantFOV(), currentWeapon.AimingAndScopeStats.TimeToADS)
            :
            (baseFOV, currentWeapon.AimingAndScopeStats.TimeToUnADS);
        while (elapsedTime < targetDuration)
        {
            camera.fieldOfView = Mathf.Lerp(startingPoint, target, elapsedTime / targetDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

    }

    private float GetRelevantFOV()
    {
        return currentWeapon.AimingAndScopeStats.ScopeMagnification == 1f ?
            currentWeapon.AimingAndScopeStats.AimingFOV
            :
            baseFOV / currentWeapon.AimingAndScopeStats.ScopeMagnification
            ;
    }

    #endregion

    #region Handle Recoil

    private void ApplyRecoil()
    {
        if (isAiming)
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeapon.AimingRecoilStats.RecoilForceX,
                Random.Range(-currentWeapon.AimingRecoilStats.RecoilForceY, currentWeapon.AimingRecoilStats.RecoilForceY),
                Random.Range(-currentWeapon.AimingRecoilStats.RecoilForceZ, currentWeapon.AimingRecoilStats.RecoilForceZ)
            );
        }
        else
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeapon.HipfireRecoilStats.RecoilForceX,
                Random.Range(-currentWeapon.HipfireRecoilStats.RecoilForceY, currentWeapon.HipfireRecoilStats.RecoilForceY),
                Random.Range(-currentWeapon.HipfireRecoilStats.RecoilForceZ, currentWeapon.HipfireRecoilStats.RecoilForceZ)
            );
        }
    }
    private void ApplyRecoil(float chargeRatio)
    {
        if (isAiming)
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeapon.AimingRecoilStats.RecoilForceX * chargeRatio,
                Random.Range(-currentWeapon.AimingRecoilStats.RecoilForceY * chargeRatio, currentWeapon.AimingRecoilStats.RecoilForceY * chargeRatio),
                Random.Range(-currentWeapon.AimingRecoilStats.RecoilForceZ * chargeRatio, currentWeapon.AimingRecoilStats.RecoilForceZ * chargeRatio)
            );
        }
        else
        {
            targetRecoilHandlerRotation += new Vector3(
                -currentWeapon.HipfireRecoilStats.RecoilForceX * chargeRatio,
                Random.Range(-currentWeapon.HipfireRecoilStats.RecoilForceY * chargeRatio, currentWeapon.HipfireRecoilStats.RecoilForceY * chargeRatio),
                Random.Range(-currentWeapon.HipfireRecoilStats.RecoilForceZ * chargeRatio, currentWeapon.HipfireRecoilStats.RecoilForceZ * chargeRatio)
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
        weaponTransform.localPosition -= new Vector3(0f, 0f, currentWeapon.KickbackStats.WeaponKickBackPerShot);
    }
    private void ApplyKickback(float chargeRatio)
    {
        weaponTransform.localPosition -= new Vector3(0f, 0f, currentWeapon.KickbackStats.WeaponKickBackPerShot * chargeRatio);
    }

    private void HandleKickback()
    {
        weaponTransform.localPosition = Vector3.Slerp(weaponTransform.localPosition, Vector3.zero, currentWeapon.KickbackStats.WeaponKickBackRegulationTime * Time.time);
    }

    #endregion

    #region Handle Spread

    private void ApplySpread()
    {
        currentSpreadAngle += isAiming ? currentWeapon.AimingSimpleShotStats.SpreadAngleAddedPerShot : currentWeapon.SimpleShotStats.SpreadAngleAddedPerShot;
    }

    private void HandleSpead()
    {
        currentSpreadAngle = Mathf.Lerp(currentSpreadAngle, 0f, (isAiming ? currentWeapon.AimingSimpleShotStats.SpreadRegulationSpeed : currentWeapon.SimpleShotStats.SpreadRegulationSpeed) * Time.time);
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

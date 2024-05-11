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
    private float cameraTransformInitialZ = 0f;
    private Transform cameraTransform;
    private new FollowRotationCamera camera;
    private Transform recoilHandlerTransform;

    [SerializeField] private Transform barrelEnd; // should have the same rotation as the camera
    [SerializeField] private Transform weaponTransform;
    [SerializeField] private LayerMask layersToHit;
    [SerializeField] private GameObject bulletTrailPrefab;

    private PlayerSettings playerSettings;
    private DamageLogSettings DamageLogsSettings => playerSettings.DamageLogSettings;
    [SerializeField] private DamageLogManager damageLogManager;

    #endregion

    #region Global Setup

    private float timeLastShotFired;
    private bool switchedThisFrame;
    private bool shotThisFrame;
    private ushort ammos;

    private bool canShoot;


    private Action shootingStyleMethod;
    private Action shootingRythmMethod;
    private Action updateMethod;

    private bool isAiming = false;

    #endregion

    #region Recoil Setup

    private Vector3 currentRecoilHandlerRotation = Vector3.zero;
    private Vector3 targetRecoilHandlerRotation = Vector3.zero;
    [SerializeField] private float recoilMovementSnappiness;
    private float RecoilRegulationSpeed => isAiming ? currentWeapon.AimingRecoilStats.RecoilRegulationSpeed : currentWeapon.HipfireRecoilStats.RecoilRegulationSpeed;

    private float recoilX = 0f;
    private float recoilY = 0f;
    private float recoilZ = 0f;

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

    // methods

    private void Awake()
    {
        playerSettings = GetComponent<PlayerSettings>();
        camera = transform.GetChild(0).GetComponent<FollowRotationCamera>();
        recoilHandlerTransform = transform.GetChild(0).GetChild(0);
        cameraTransform = recoilHandlerTransform.GetChild(0);
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
            ShootingStyle.Single => ExecuteSimpleShotClientRpc,

            ShootingStyle.Shotgun => () =>
                {
                    SetShotgunPelletsDirections();
                    ExecuteShotgunShotClientRpc();
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

            ShootingRythm.Charge => throw new NotImplementedException(),

            _ => () => { },
        };
    }

    #endregion

    public void Shoot()
    {
        if (!canShoot)
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


    private float GetRelevantCooldown(bool doDebug)
    {
        var cd = currentWeapon.ShootingRythm switch
        {
            ShootingRythm.Single => currentWeapon.CooldownBetweenShots,
            ShootingRythm.Burst => bulletFiredthisBurst == currentWeapon.BurstStats.BulletsPerBurst ? currentWeapon.CooldownBetweenShots : currentWeapon.BurstStats.CooldownBetweenShotsOfBurst,
            ShootingRythm.RampUp => CurrentCooldownBetweenRampUpShots,
            ShootingRythm.Charge => throw new NotImplementedException(),
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

    #region ExecuteShot

    [Rpc(SendTo.ClientsAndHost)] // called by the server to execute on all clients
    private void ExecuteSimpleShotClientRpc()
    {
        if (IsOwner)
        {
            damageLogManager.UpdatePlayerSettings(DamageLogsSettings);
            timeLastShotFired = Time.time;
            shotThisFrame = true;
            ammos--;
            bulletFiredthisBurst++;
        }

        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
        if (Physics.Raycast(barrelEnd.position, barrelEnd.forward, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
        {
            bulletTrail.Set(barrelEnd.position, hit.point);
            if (IsOwner && hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnd.forward, NetworkObjectId);
            }

            //Destroy(Instantiate(landingShotEffect, hit.point - shootingDir * .1f, Quaternion.identity), hitEffectLifetime);

        }
        else
        {
            bulletTrail.Set(barrelEnd.position, barrelEnd.position + barrelEnd.forward * 100);
        }

        

        if(!IsOwner) { return; }

        ApplyRecoil();
        ApplyKickback();
    }

    [Rpc(SendTo.ClientsAndHost)] // called by the server to execute on all clients
    private void ExecuteShotgunShotClientRpc()
    {
        if (IsOwner)
        {
            damageLogManager.UpdatePlayerSettings(DamageLogsSettings);
            timeLastShotFired = Time.time;
            shotThisFrame = true;
            ammos--;
            bulletFiredthisBurst++;
        }

        //var hits = new List<ShotgunHitData>();
        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnd.position, Quaternion.identity).GetComponent<BulletTrail>();
            if (Physics.Raycast(barrelEnd.position, shotgunPelletsDirections[i], out RaycastHit hit, currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore))
            {
                bulletTrail.Set(barrelEnd.position, hit.point);
                if (hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
                {
                    shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage, shotgunPelletsDirections[i], hit.point, NetworkObjectId);
                    //hits.Add(new(shootableComponent, hit.point, shotgunPelletsDirections[i]));
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

        //for (int i = 0; i < hits.Count; i++)
        //{
        //    hits[i].Victim.ReactShot(currentWeapon.ShotgunStats.PelletsDamage, hits[i].HitPoint, hits[i].HitDirection, NetworkObjectId);
        //}
    }

    #endregion

    private void SetShotgunPelletsDirections()
    {
        shotgunPelletsDirections = new Vector3[currentWeapon.ShotgunStats.PelletsCount];
        /*Most fucked explanantion to ever cross the frontier of reality
         / 45f -> to get value which we can use iun a vector instead of an angle
        ex in 2D:  a vector that has a 45° angle above X has a (1, 1) direction
        while the X has a (1, 0)
        so we essentially brought the 45° to a value we could use as a direction in the vector
         */
        var spreadStrength = currentWeapon.ShotgunStats.PelletsSpreadAngle / 45f;
        // perhaps do barrelEnd.forward * 45 instead for performances purposes
        for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        {
            shotgunPelletsDirections[i] = (
                barrelEnd.forward + barrelEnd.TransformDirection(
                    new Vector3(
                        Random.Range(-spreadStrength, spreadStrength),
                        Random.Range(-spreadStrength, spreadStrength),
                        0
                    )
                )
            ).normalized;
        }
    }

    # region Reload

    public void Reload()
    {
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

    #endregion

    #region Aiming

    public void UpdateAimingState(bool shoulbBeAiming)
    {
        if (isAiming != shoulbBeAiming)
        {
            isAiming = shoulbBeAiming;
            //StartCoroutine(ToggleAim(shoulbBeAiming));
            StartCoroutine(ToggleAimFOV(shoulbBeAiming));

        }
    }

    private IEnumerator ToggleAim(bool shouldBeAiming)
    {
        var startingPointZ = cameraTransform.localPosition.z;
        var elapsedTime = 0f;
        var (targetZ, targetDuration) = shouldBeAiming ?
            (currentWeapon.AimingAndScopeStats.AimingCameraMovement, currentWeapon.AimingAndScopeStats.TimeToADS)
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
        var cam = cameraTransform.GetComponent<Camera>();
        var startingPoint = cam.fieldOfView;
        var elapsedTime = 0f;
        var (target, targetDuration) = shouldBeAiming ?
            (currentWeapon.AimingAndScopeStats.AimingCameraMovement, currentWeapon.AimingAndScopeStats.TimeToADS)
            :
            (60, currentWeapon.AimingAndScopeStats.TimeToUnADS);
        while (elapsedTime < targetDuration)
        {
            cam.fieldOfView = Mathf.Lerp(startingPoint, target, elapsedTime / targetDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

    }

    #endregion

    #region Handle Recoil

    private void ApplyRecoil()
    {
        SetRelevantRecoil();
        targetRecoilHandlerRotation += new Vector3(-recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }

    private void HandleRecoil()
    {
        targetRecoilHandlerRotation = Vector3.Lerp(targetRecoilHandlerRotation, Vector3.zero, RecoilRegulationSpeed * Time.deltaTime);
        currentRecoilHandlerRotation = Vector3.Slerp(currentRecoilHandlerRotation, targetRecoilHandlerRotation, recoilMovementSnappiness * Time.deltaTime);
        recoilHandlerTransform.localRotation = Quaternion.Euler(currentRecoilHandlerRotation);
    }

    private void SetRelevantRecoil()
    {
        (recoilX, recoilY, recoilZ) = isAiming ?
            (
                currentWeapon.AimingRecoilStats.RecoilForceX,
                currentWeapon.AimingRecoilStats.RecoilForceY,
                currentWeapon.AimingRecoilStats.RecoilForceZ
            )
            :
            (
                currentWeapon.HipfireRecoilStats.RecoilForceX,
                currentWeapon.HipfireRecoilStats.RecoilForceY,
                currentWeapon.HipfireRecoilStats.RecoilForceZ
            )
            ;
    }

    #endregion


    #region Handle Kickback

    private void ApplyKickback()
    {
        weaponTransform.localPosition -= new Vector3(0f, 0f, currentWeapon.KickbackStats.WeaponKickBackPerShot);
    }

    private void HandleKickback()
    {
        weaponTransform.localPosition = Vector3.Slerp(weaponTransform.localPosition, Vector3.zero, currentWeapon.KickbackStats.WeaponKickBackRegulationTime * Time.time);
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

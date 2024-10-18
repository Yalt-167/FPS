using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Netcode;
using GameManagement;
using Projectiles;

namespace WeaponHandling
{
    public abstract class Weapon : Holdable
    {
        [SerializeField] protected WeaponScriptableObject weaponData;

        protected WeaponRecoil weaponRecoil;
        protected WeaponBehaviourGatherer<WeaponADSGunMovement> weaponADSGatherer;
        protected WeaponSpread weaponSpread;
        protected WeaponBehaviourGatherer<WeaponKickback> weaponKickbackGatherer;
        protected WeaponBehaviourGatherer<BarrelEnd> barrelEndsGatherer;
        [SerializeField] protected LayerMask layersToHit;
        [SerializeField] protected GameObject bulletTrailPrefab;

        //#region ShootingMethods


        //private void UpdateOwnerSettingsUponShot()
        //{
        //    if (!IsOwner) { return; }

        //    //PlayShootingSoundServerRpc(currentWeapon.AmmoLeftInMagazineToWarn >= ammos);

        //    //damageLogManager.UpdatePlayerSettings(DamageLogsSettings);
        //    //timeLastShotFired = Time.time;
        //    //shotThisFrame = true;
        //    //ammos--;
        //    //bulletFiredthisBurst++;
        //}

        //private async void CreateBulletTrail()
        //{
        //    var bulletTrail = Instantiate(bulletTrailPrefab, barrelEndsGatherer.Current.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        //    var directionWithSpread = await weaponSpreadGatherer.Current.GetDirectionWithSpread(currentSpreadAngle, barrelEnds.Current.transform);
        //    var endPoint = barrelEndsGatherer.Current.transform.position + directionWithSpread * 100;
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteSimpleHitscanShotClientRpc()
        //{
        //    UpdateOwnerSettingsUponShot();

        //    var bulletTrail = Instantiate(bulletTrailPrefab, barrelEndsGatherer.Current.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        //    var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnds.Current.transform);
        //    var endPoint = barrelEndsGatherer.Current.transform.position + directionWithSpread * 100;

        //    var hits = Physics.RaycastAll(barrelEndsGatherer.Current.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        //    Array.Sort(hits, new RaycastHitComparer());

        //    foreach (var hit in hits)
        //    {
        //        if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
        //        {
        //            if (IsOwner)
        //            {
        //                //shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
        //                shootableComponent.ReactShot(currentWeapon.Damage, hit.point, barrelEnds.Current.transform.forward, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
        //            }

        //            if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
        //            {
        //                endPoint = hit.point;
        //                break;
        //            }
        //        }
        //        else // so far else is the wall but do proper checks later on
        //        {
        //            onHitWallMethod(
        //                new(
        //                    directionWithSpread,
        //                    hit,
        //                    NetworkObjectId,
        //                    new(currentWeapon)
        //                ),
        //                GetRelevantHitscanBulletSettings()
        //            );

        //            if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
        //            {
        //                endPoint = hit.point;
        //                break;
        //            }

        //        }
        //    }

        //    bulletTrail.Set(barrelEnds.Current.transform.position, endPoint);


        //    if (!IsOwner) { return; }

        //    ApplyRecoil();
        //    ApplySpread();
        //    ApplyKickback();
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteShotgunHitscanShotClientRpc()
        //{
        //    UpdateOwnerSettingsUponShot();

        //    for (int pelletIndex = 0; pelletIndex < currentWeapon.ShotgunStats.PelletsCount; pelletIndex++)
        //    {
        //        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnds.Current.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        //        var endPoint = barrelEnds.Current.transform.position + shotgunPelletsDirections[pelletIndex] * 100;

        //        var hits = Physics.RaycastAll(barrelEnds.Current.transform.position, shotgunPelletsDirections[pelletIndex], currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
        //        Array.Sort(hits, new RaycastHitComparer());

        //        foreach (var hit in hits)
        //        {
        //            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
        //            {
        //                if (IsOwner)
        //                {
        //                    //shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
        //                    shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
        //                }

        //                if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
        //                {
        //                    endPoint = hit.point;
        //                    break;
        //                }

        //                else
        //                {

        //                    onHitWallMethod(
        //                        new(
        //                            shotgunPelletsDirections[pelletIndex],
        //                            hit,
        //                            NetworkObjectId,
        //                            new(currentWeapon)
        //                        ),
        //                        GetRelevantHitscanBulletSettings()
        //                    );

        //                    if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
        //                    {
        //                        endPoint = hit.point;
        //                        break;
        //                    }

        //                }
        //            }
        //        }

        //        bulletTrail.Set(barrelEnds.Current.transform.position, endPoint);
        //    }

        //    if (!IsOwner) { return; }

        //    ApplyRecoil();
        //    ApplyKickback();
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteChargedHitscanShotClientRpc(float chargeRatio)
        //{
        //    UpdateOwnerSettingsUponShot();

        //    var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnds.Current.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        //    var directionWithSpread = GetDirectionWithSpread(currentSpreadAngle, barrelEnds.Current.transform);
        //    var endPoint = barrelEnds.Current.transform.position + directionWithSpread * 100;

        //    var hits = Physics.RaycastAll(barrelEnds.Current.transform.position, directionWithSpread, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        //    Array.Sort(hits, new RaycastHitComparer());

        //    foreach (var hit in hits)
        //    {
        //        if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
        //        {
        //            if (IsOwner)
        //            {
        //                //shootableComponent.ReactShot(currentWeapon.Damage * chargeRatio, hit.point, barrelEnd.forward, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
        //                shootableComponent.ReactShot(currentWeapon.Damage * chargeRatio, hit.point, barrelEnds.Current.transform.forward, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
        //            }

        //            if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
        //            {
        //                endPoint = hit.point;
        //                break;
        //            }
        //        }
        //        else // so far else is the wall but do proper checks later on
        //        {
        //            onHitWallMethod(
        //                new(
        //                    directionWithSpread,
        //                    hit,
        //                    NetworkObjectId,
        //                    new(currentWeapon, chargeRatio)
        //                ),
        //                GetRelevantHitscanBulletSettings()
        //            );

        //            if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
        //            {
        //                endPoint = hit.point;
        //                break;
        //            }

        //        }
        //    }

        //    bulletTrail.Set(barrelEnds.Current.transform.position, endPoint);


        //    if (!IsOwner) { return; }

        //    ApplyRecoil(chargeRatio);
        //    ApplySpread();
        //    ApplyKickback(chargeRatio);
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteChargedShotgunHitscanShotClientRpc(float chargeRatio)
        //{
        //    UpdateOwnerSettingsUponShot();

        //    for (int pelletIndex = 0; pelletIndex < currentWeapon.ShotgunStats.PelletsCount; pelletIndex++)
        //    {
        //        var bulletTrail = Instantiate(bulletTrailPrefab, barrelEnds.Current.transform.position, Quaternion.identity).GetComponent<BulletTrail>();
        //        var endPoint = barrelEnds.Current.transform.position + shotgunPelletsDirections[pelletIndex] * 100;


        //        var hits = Physics.RaycastAll(barrelEnds.Current.transform.position, shotgunPelletsDirections[pelletIndex], currentWeapon.ShotgunStats.PelletsRange, layersToHit, QueryTriggerInteraction.Ignore);
        //        Array.Sort(hits, new RaycastHitComparer());

        //        foreach (var hit in hits)
        //        {
        //            if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
        //            {
        //                if (IsOwner)
        //                {
        //                    //shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.TeamID, currentWeapon.CanBreakThings);
        //                    shootableComponent.ReactShot(currentWeapon.ShotgunStats.PelletsDamage * chargeRatio, shotgunPelletsDirections[pelletIndex], hit.point, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, currentWeapon.CanBreakThings);
        //                }

        //                if (!currentWeapon.HitscanBulletSettings.PierceThroughPlayers)
        //                {
        //                    endPoint = hit.point;
        //                    break;
        //                }
        //            }
        //            else
        //            {
        //                onHitWallMethod(
        //                    new(
        //                        shotgunPelletsDirections[pelletIndex],
        //                        hit,
        //                        NetworkObjectId,
        //                        new(currentWeapon, chargeRatio)
        //                        ),
        //                    GetRelevantHitscanBulletSettings()
        //                );

        //                if (currentWeapon.HitscanBulletSettings.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
        //                {
        //                    endPoint = hit.point;
        //                    break;
        //                }

        //            }
        //        }

        //        bulletTrail.Set(barrelEnds.Current.transform.position, endPoint);
        //    }

        //    if (!IsOwner) { return; }

        //    ApplyRecoil();
        //    ApplyKickback();
        //}

        //#endregion

        //#region Execute Shot TravelTime

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteSimpleTravelTimeShotClientRpc()
        //{
        //    UpdateOwnerSettingsUponShot();

        //    var projectile = Instantiate(
        //        currentWeapon.TravelTimeBulletSettings.BulletPrefab,
        //        barrelEnds.Current.transform.position,
        //        Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnds.Current.transform))
        //        ).GetComponent<Projectile>();

        //    if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        //    projectile.Init(
        //        currentWeapon.Damage,
        //        currentWeapon.TravelTimeBulletSettings.BulletSpeed,
        //        currentWeapon.TravelTimeBulletSettings.BulletDrop,
        //        NetworkObjectId,
        //        currentWeapon.CanBreakThings,
        //        layersToHit,
        //        GetRelevantHitWallBehaviour(),
        //        GetRelevantHitPlayerBehaviour(),
        //        PlayerFrame.LocalPlayer.TeamNumber
        //    );

        //    ApplyRecoil();
        //    ApplySpread();
        //    ApplyKickback();
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteShotgunTravelTimeShotClientRpc()
        //{
        //    UpdateOwnerSettingsUponShot();

        //    for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        //    {

        //        var projectile = Instantiate(
        //            currentWeapon.TravelTimeBulletSettings.BulletPrefab,
        //            barrelEnds.Current.transform.position,
        //            Quaternion.LookRotation(shotgunPelletsDirections[i])
        //        ).GetComponent<Projectile>();

        //        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        //        projectile.Init(
        //            currentWeapon.ShotgunStats.PelletsDamage,
        //            currentWeapon.TravelTimeBulletSettings.BulletSpeed,
        //            currentWeapon.TravelTimeBulletSettings.BulletDrop,
        //            NetworkObjectId,
        //            currentWeapon.CanBreakThings,
        //            layersToHit,
        //            GetRelevantHitWallBehaviour(),
        //            GetRelevantHitPlayerBehaviour(),
        //            PlayerFrame.LocalPlayer.TeamNumber
        //        );

        //    }

        //    if (!IsOwner) { return; }

        //    ApplyRecoil();
        //    ApplyKickback();
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteChargedTravelTimeShotClientRpc(float chargeRatio)
        //{
        //    UpdateOwnerSettingsUponShot();

        //    var projectile = Instantiate(
        //        currentWeapon.TravelTimeBulletSettings.BulletPrefab,
        //        barrelEnds.Current.transform.position,
        //        Quaternion.LookRotation(GetDirectionWithSpread(currentSpreadAngle, barrelEnds.Current.transform))
        //    ).GetComponent<Projectile>();

        //    if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        //    projectile.Init(
        //        currentWeapon.Damage * chargeRatio,
        //        currentWeapon.TravelTimeBulletSettings.BulletSpeed * chargeRatio,
        //        currentWeapon.TravelTimeBulletSettings.BulletDrop,
        //        NetworkObjectId,
        //        currentWeapon.CanBreakThings,
        //        layersToHit,
        //        GetRelevantHitWallBehaviour(),
        //        GetRelevantHitPlayerBehaviour(),
        //        PlayerFrame.LocalPlayer.TeamNumber
        //    );


        //    if (!IsOwner) { return; }

        //    ApplyRecoil(chargeRatio);
        //    ApplySpread();
        //    ApplyKickback(chargeRatio);
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void ExecuteChargedShotgunTravelTimeShotClientRpc(float chargeRatio)
        //{
        //    UpdateOwnerSettingsUponShot();

        //    for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        //    {
        //        var projectile = Instantiate(
        //            currentWeapon.TravelTimeBulletSettings.BulletPrefab,
        //            barrelEnds.Current.transform.position,
        //            Quaternion.LookRotation(shotgunPelletsDirections[i])
        //        ).GetComponent<Projectile>();

        //        if (projectile == null) { throw new Exception("The prefab used for this projectile doesn t have a projectile script attached to it"); }

        //        projectile.Init(
        //            currentWeapon.ShotgunStats.PelletsDamage * chargeRatio,
        //            currentWeapon.TravelTimeBulletSettings.BulletSpeed,
        //            currentWeapon.TravelTimeBulletSettings.BulletDrop,
        //            NetworkObjectId,
        //            currentWeapon.CanBreakThings,
        //            layersToHit,
        //            GetRelevantHitWallBehaviour(),
        //            GetRelevantHitPlayerBehaviour(),
        //            PlayerFrame.LocalPlayer.TeamNumber
        //        );
        //    }

        //    if (!IsOwner) { return; }

        //    ApplyRecoil();
        //    ApplyKickback();
        //}

        //#endregion



        //#region Hitscan Wall Hit Effects

        //private void ExplodeUponWallHit(ShotInfos shotInfos, IHitscanBulletEffectSettings hitscanBulletEffectSettings_)
        //{
        //    var hitscanBulletEffectSettings = (ExplodingHitscanBulletsSettings)hitscanBulletEffectSettings_;

        //    var inRange = Physics.OverlapSphere(shotInfos.Hit.point, hitscanBulletEffectSettings.ExplosionRadius);

        //    for (int i = 0; i < inRange.Length; i++)
        //    {
        //        if (inRange[i].TryGetComponent<IExplodable>(out var explodableComponent))
        //        {
        //            explodableComponent.ReactExplosion(
        //                hitscanBulletEffectSettings.ExplosionDamage,
        //                shotInfos.Hit.point,
        //                shotInfos.AttackerNetworkID,
        //                shotInfos.WeaponInfos.CanBreakThings
        //                );
        //        }
        //    }
        //}

        //private void BounceUponWallHit(ShotInfos shotInfos, IHitscanBulletEffectSettings hitscanBulletEffectSettings_)
        //{
        //    var hitscanBulletEffectSettings = (BouncingHitscanBulletsSettings)hitscanBulletEffectSettings_;

        //    if (hitscanBulletEffectSettings.BouncesAmount == 0) { return; }

        //    var newShotDirection = MyUtility.Utility.ReflectVector(shotInfos.ShotDirection, shotInfos.Hit.normal);

        //    var bulletTrail = Instantiate(bulletTrailPrefab, shotInfos.Hit.point, Quaternion.identity).GetComponent<BulletTrail>();
        //    var endPoint = shotInfos.Hit.point + newShotDirection * 100;

        //    var hits = Physics.RaycastAll(shotInfos.Hit.point, newShotDirection, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore);

        //    Array.Sort(hits, new RaycastHitComparer());

        //    foreach (var hit in hits)
        //    {
        //        if (hit.collider.TryGetComponent<IShootable>(out var shootableComponent))
        //        {
        //            if (IsOwner)
        //            {
        //                //shootableComponent.ReactShot(shotInfos.WeaponInfos.Damage, hit.point, newShotDirection, NetworkObjectId, PlayerFrame.TeamID, shotInfos.WeaponInfos.CanBreakThings);
        //                shootableComponent.ReactShot(shotInfos.WeaponInfos.Damage, hit.point, newShotDirection, NetworkObjectId, PlayerFrame.LocalPlayer.TeamNumber, shotInfos.WeaponInfos.CanBreakThings);
        //            }

        //            if (!shotInfos.WeaponInfos.PierceThroughPlayers)
        //            {
        //                endPoint = hit.point;
        //                break;
        //            }
        //        }
        //        else // so far else is the wall but do proper checks later on
        //        {
        //            BounceUponWallHit(
        //                    new(
        //                        newShotDirection,
        //                        hit,
        //                        shotInfos.AttackerNetworkID,
        //                        new(currentWeapon)
        //                        ),
        //                    --hitscanBulletEffectSettings
        //                    );

        //            if (shotInfos.WeaponInfos.ActionOnHitWall != HitscanBulletActionOnHitWall.ThroughWalls)
        //            {
        //                endPoint = hit.point;
        //                break;
        //            }

        //        }
        //    }

        //    bulletTrail.Set(shotInfos.Hit.point, endPoint);
        //}

        //#endregion

        //#region Travel Time Hit Effects

        //private ProjectileOnHitWallBehaviour GetRelevantHitWallBehaviour()
        //{
        //    return currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviour switch
        //    {
        //        ProjectileBehaviourOnHitWall.Stop => new ProjectileStopOnHitWall(null),
        //        ProjectileBehaviourOnHitWall.Pierce => new ProjectilePierceOnHitWall(currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallPierceParams),
        //        ProjectileBehaviourOnHitWall.Bounce => new ProjectileBounceOnHitWall(currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallBounceParams),
        //        ProjectileBehaviourOnHitWall.Explode => new ProjectileExplodeOnHitWall(currentWeapon.TravelTimeBulletSettings.OnHitWallBehaviourParams.ProjectileWallExplodeParams),
        //        _ => throw new NotImplementedException(),
        //    };
        //}


        //private ProjectileOnHitPlayerBehaviour GetRelevantHitPlayerBehaviour()
        //{
        //    return currentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviour switch
        //    {
        //        ProjectileBehaviourOnHitPlayer.Stop => new ProjectileStopOnHitPlayer(null),
        //        ProjectileBehaviourOnHitPlayer.Pierce => new ProjectilePierceOnHitPlayer(currentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerPierceParams),
        //        ProjectileBehaviourOnHitPlayer.Explode => new ProjectileExplodeOnHitPlayer(currentWeapon.TravelTimeBulletSettings.OnHitPlayerBehaviourParams.ProjectilePlayerExplodeParams),
        //        _ => throw new NotImplementedException(),
        //    };
        //}

        //#endregion

        //#region Setting up Shotgun Shot

        //private void SetShotgunPelletsDirections(Transform directionTranform)
        //{
        //    shotgunPelletsDirections = new Vector3[currentWeapon.ShotgunStats.PelletsCount];

        //    var relevantSpread = isAiming ? currentWeapon.ShotgunStats.AimingPelletsSpreadAngle : currentWeapon.ShotgunStats.PelletsSpreadAngle;

        //    for (int i = 0; i < currentWeapon.ShotgunStats.PelletsCount; i++)
        //    {
        //        shotgunPelletsDirections[i] = GetDirectionWithSpread(relevantSpread, directionTranform);
        //    }

        //    RequestUpdateShotgunPelletsDirectionServerRpc(shotgunPelletsDirections);
        //}

        //[Rpc(SendTo.Server)]
        //private void RequestUpdateShotgunPelletsDirectionServerRpc(Vector3[] directions)
        //{
        //    UpdateShotgunPelletDirectionClientRpc(directions);
        //}

        //[Rpc(SendTo.ClientsAndHost)]
        //private void UpdateShotgunPelletDirectionClientRpc(Vector3[] directions)
        //{
        //    shotgunPelletsDirections = directions;
        //}




        //#endregion

        protected virtual void Awake()
        {
            weaponRecoil = GetComponentInChildren<WeaponRecoil>();
            weaponADSGatherer = new(GetComponentsInChildren<WeaponADSGunMovement>());
            weaponSpread = GetComponentInChildren<WeaponSpread>();
            weaponKickbackGatherer = new(GetComponentsInChildren<WeaponKickback>());
            barrelEndsGatherer = new(GetComponentsInChildren<BarrelEnd>());
        }

        protected void SetShootingStrategy()
        {

        }


        //public override void OnPullOut()
        //{
        //    base.OnPullOut();
        //}

        //public override void OnPutAway()
        //{
        //    base.OnPutAway();
        //}


        //public override void OnPrimaryUseKeyDown()
        //{
        //    base.OnPrimaryUseKeyDown();
        //}


        //public override void OnPrimaryUseKeyUp()
        //{
        //    base.OnPrimaryUseKeyUp();
        //}


    }
}
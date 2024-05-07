using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] private WeaponStats currentWeapon;

    private readonly float shootBuffer = .1f;
    private float timeLastShotFired;

    private ushort ammoCountInCurrentMagazine;
    private ushort spareAmoCount;

    private bool canShoot;

    

    public void InitGun()
    {
        canShoot = true;
        ammoCountInCurrentMagazine = currentWeapon.MagazineSize;
        spareAmoCount = currentWeapon.MaxAmmoCount;
        timeLastShotFired = 0;
    }

    public void Shoot()
    {
        if (!canShoot) { return; }


    }

    public IEnumerator Reload()
    {
        if (spareAmoCount < 1) { yield break; }

        if (ammoCountInCurrentMagazine == currentWeapon.MagazineSize) { yield break; }

        canShoot = false;

        yield return new WaitForSeconds(currentWeapon.ReloadSpeed);

        canShoot = true;

        spareAmoCount = (ushort)(spareAmoCount + ammoCountInCurrentMagazine - currentWeapon.MagazineSize);
        ammoCountInCurrentMagazine = currentWeapon.MagazineSize;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SurvivalPlayerWeaponSystem : MonoBehaviour
{
    [Serializable]
    public class WeaponRuntimeConfig
    {
        public string weaponName;
        public bool canFire = true;
        public bool useProjectile;
        public GameObject projectilePrefab;
        public Transform muzzle;
        public float fireRate = 8f;
        public float damage = 18f;
        public float range = 180f;
        public int magazineSize = 12;
        public int reserveSize = 48;
        public float reloadTime = 1.5f;
    }

    [Header("References")]
    public Camera playerCamera;
    public SurvivalController survivalController;
    public TMP_Text ammoText;

    [Header("Weapons")]
    public List<WeaponRuntimeConfig> weaponConfigs = new List<WeaponRuntimeConfig>();

    private readonly Dictionary<string, int> currentMag = new Dictionary<string, int>();
    private readonly Dictionary<string, int> currentReserve = new Dictionary<string, int>();
    private WeaponRuntimeConfig equipped;
    private bool reloading;
    private float nextFireTime;

    void Start()
    {
        if (survivalController == null)
            survivalController = FindObjectOfType<SurvivalController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        foreach (WeaponRuntimeConfig config in weaponConfigs)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.weaponName))
                continue;

            currentMag[config.weaponName] = Mathf.Max(0, config.magazineSize);
            currentReserve[config.weaponName] = Mathf.Max(0, config.reserveSize);
        }

        UpdateAmmoUI();
    }

    void Update()
    {
        if (equipped == null || !equipped.canFire)
            return;

        if (Input.GetKeyDown(KeyCode.R))
            StartReload();

        if (reloading)
            return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            Fire();
    }

    public void OnWeaponEquipped(string weaponName, SurvivalController.WeaponOffer offer)
    {
        equipped = weaponConfigs.Find(w => w.weaponName == weaponName);
        if (equipped == null)
        {
            UpdateAmmoUI();
            return;
        }

        if (!currentMag.ContainsKey(weaponName))
            currentMag[weaponName] = Mathf.Max(0, equipped.magazineSize);

        if (!currentReserve.ContainsKey(weaponName))
            currentReserve[weaponName] = Mathf.Max(0, equipped.reserveSize);

        if (offer != null)
        {
            if (equipped.magazineSize <= 0)
                equipped.magazineSize = Mathf.Max(0, offer.magazineSize);

            if (equipped.reserveSize <= 0)
                equipped.reserveSize = Mathf.Max(0, offer.reserveSize);
        }

        UpdateAmmoUI();
    }

    public void BuyAmmoForEquippedWeapon()
    {
        if (equipped == null || survivalController == null)
            return;

        int missingMag = Mathf.Max(0, equipped.magazineSize - GetMag(equipped.weaponName));
        int missingReserve = Mathf.Max(0, equipped.reserveSize - GetReserve(equipped.weaponName));

        if (!survivalController.TryBuyAmmo(equipped.weaponName, missingMag, missingReserve))
            return;

        currentMag[equipped.weaponName] = equipped.magazineSize;
        currentReserve[equipped.weaponName] = equipped.reserveSize;
        UpdateAmmoUI();
    }

    void Fire()
    {
        string weaponName = equipped.weaponName;
        int mag = GetMag(weaponName);
        if (mag <= 0)
        {
            StartReload();
            return;
        }

        currentMag[weaponName] = mag - 1;
        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, equipped.fireRate));

        if (equipped.useProjectile)
            FireProjectile();
        else
            FireHitscan();

        UpdateAmmoUI();
    }

    void FireHitscan()
    {
        if (playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, equipped.range))
            return;

        SurvivalEnemyHurtbox hurtbox = hit.collider.GetComponent<SurvivalEnemyHurtbox>();
        if (hurtbox != null)
            hurtbox.ApplyHit(equipped.damage);
    }

    void FireProjectile()
    {
        if (equipped.projectilePrefab == null)
            return;

        Transform origin = equipped.muzzle != null ? equipped.muzzle : transform;
        GameObject projectile = Instantiate(equipped.projectilePrefab, origin.position, origin.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 forward = playerCamera != null ? playerCamera.transform.forward : origin.forward;
            rb.velocity = forward * 35f;
        }
    }

    void StartReload()
    {
        if (reloading || equipped == null)
            return;

        int mag = GetMag(equipped.weaponName);
        int reserve = GetReserve(equipped.weaponName);
        if (mag >= equipped.magazineSize || reserve <= 0)
            return;

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        reloading = true;
        yield return new WaitForSeconds(equipped.reloadTime);

        string weaponName = equipped.weaponName;
        int mag = GetMag(weaponName);
        int reserve = GetReserve(weaponName);
        int needed = Mathf.Max(0, equipped.magazineSize - mag);
        int toLoad = Mathf.Min(needed, reserve);

        currentMag[weaponName] = mag + toLoad;
        currentReserve[weaponName] = reserve - toLoad;

        reloading = false;
        UpdateAmmoUI();
    }

    int GetMag(string weaponName)
    {
        return currentMag.TryGetValue(weaponName, out int value) ? value : 0;
    }

    int GetReserve(string weaponName)
    {
        return currentReserve.TryGetValue(weaponName, out int value) ? value : 0;
    }

    void UpdateAmmoUI()
    {
        if (ammoText == null)
            return;

        if (equipped == null || !equipped.canFire)
        {
            ammoText.text = "-/-";
            return;
        }

        ammoText.text = $"{GetMag(equipped.weaponName)}/{GetReserve(equipped.weaponName)}";
    }
}

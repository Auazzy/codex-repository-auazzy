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

    [Header("Hitscan")]
    public bool useMuzzleAsHitscanOrigin;
    public LayerMask hitscanMask = ~0;
    public bool raycastHitsTriggers = true;
    public List<string> ignoredHitscanTags = new List<string> { "Player", "EnemyHitbox", "Weapon" };

    [Header("Debug")]
    public bool debugHitscan;
    public float debugRayDuration = 1.5f;
    public Color debugHitColor = Color.green;
    public Color debugMissColor = Color.red;

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

        Vector3 origin = playerCamera.transform.position;
        if (useMuzzleAsHitscanOrigin && equipped != null && equipped.muzzle != null)
            origin = equipped.muzzle.position;

        Vector3 direction = playerCamera.transform.forward;
        Ray ray = new Ray(origin, direction);

        QueryTriggerInteraction triggerMode = raycastHitsTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            equipped.range,
            hitscanMask,
            triggerMode);

        if (hits.Length == 0)
        {
            if (debugHitscan)
            {
                Debug.DrawRay(origin, direction * equipped.range, debugMissColor, debugRayDuration);
                Debug.Log($"Hitscan miss. Origin={origin} Dir={direction} Range={equipped.range}");
            }

            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit? chosenHit = null;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null)
                continue;

            if (ShouldIgnoreHit(collider))
            {
                if (debugHitscan)
                    Debug.Log($"Ignoring hitscan target due to tag: {collider.name} [{collider.tag}]");

                continue;
            }

            chosenHit = hits[i];
            break;
        }

        if (!chosenHit.HasValue)
        {
            if (debugHitscan)
            {
                Debug.DrawRay(origin, direction * equipped.range, debugMissColor, debugRayDuration);
                Debug.Log("Hitscan only hit ignored tags.");
            }

            return;
        }

        RaycastHit hit = chosenHit.Value;

        if (debugHitscan)
        {
            Debug.DrawLine(origin, hit.point, debugHitColor, debugRayDuration);
            Debug.Log($"Hitscan hit: {hit.collider.name} at {hit.point}");
        }

        if (TryResolveEnemyTarget(hit.collider, out SurvivalEnemyHurtbox hurtbox, out SurvivalEnemyAI enemyAI))
        {
            if (hurtbox != null)
            {
                hurtbox.ApplyHit(equipped.damage);
                return;
            }

            enemyAI.TakeDamage(equipped.damage, false);
            return;
        }

        if (debugHitscan)
            Debug.Log($"No SurvivalEnemyHurtbox/SurvivalEnemyAI found on hit target hierarchy: {hit.collider.name}");
    }

    bool ShouldIgnoreHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return true;

        if (ignoredHitscanTags == null || ignoredHitscanTags.Count == 0)
            return false;

        for (int i = 0; i < ignoredHitscanTags.Count; i++)
        {
            string ignoredTag = ignoredHitscanTags[i];
            if (string.IsNullOrWhiteSpace(ignoredTag))
                continue;

            if (hitCollider.CompareTag(ignoredTag))
                return true;
        }

        return false;
    }

    bool TryResolveEnemyTarget(
        Collider hitCollider,
        out SurvivalEnemyHurtbox hurtbox,
        out SurvivalEnemyAI enemyAI)
    {
        hurtbox = null;
        enemyAI = null;

        if (hitCollider == null)
            return false;

        hurtbox = hitCollider.GetComponent<SurvivalEnemyHurtbox>();
        if (hurtbox == null)
            hurtbox = hitCollider.GetComponentInParent<SurvivalEnemyHurtbox>();
        if (hurtbox == null)
            hurtbox = hitCollider.transform.root.GetComponentInChildren<SurvivalEnemyHurtbox>();

        if (hurtbox != null)
        {
            enemyAI = hurtbox.enemy != null
                ? hurtbox.enemy
                : hurtbox.GetComponentInParent<SurvivalEnemyAI>();

            return true;
        }

        enemyAI = hitCollider.GetComponent<SurvivalEnemyAI>();
        if (enemyAI == null)
            enemyAI = hitCollider.GetComponentInParent<SurvivalEnemyAI>();
        if (enemyAI == null && hitCollider.attachedRigidbody != null)
            enemyAI = hitCollider.attachedRigidbody.GetComponentInParent<SurvivalEnemyAI>();
        if (enemyAI == null)
            enemyAI = hitCollider.transform.root.GetComponentInChildren<SurvivalEnemyAI>();

        return enemyAI != null;
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

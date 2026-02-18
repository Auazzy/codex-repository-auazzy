using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SurvivalExplosionProjectile : MonoBehaviour
{
    public float directDamage = 20f;
    public float radius = 4f;
    public float lifetime = 4f;
    public bool explodeOnImpact = true;

    private bool exploded;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnImpact)
            return;

        Explode(collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
    }

    void OnTriggerEnter(Collider other)
    {
        if (explodeOnImpact)
            Explode(transform.position);
    }

    void Explode(Vector3 position)
    {
        if (exploded)
            return;

        exploded = true;

        Collider[] hits = Physics.OverlapSphere(position, radius);
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            SurvivalController controller = FindObjectOfType<SurvivalController>();
            if (controller == null)
                continue;

            float dist = Vector3.Distance(position, hit.transform.position);
            float t = Mathf.Clamp01(dist / Mathf.Max(0.01f, radius));
            float damage = Mathf.Lerp(directDamage, 0f, t);
            controller.DamagePlayer(damage);
        }

        Destroy(gameObject);
    }
}

using UnityEngine;

public class A : MonoBehaviour
{
    public Rigidbody projectile;

    private void LaunchProjectile()
    {
        Rigidbody instance = Instantiate(projectile);
        instance.velocity = UnityEngine.Random.insideUnitSphere * 5;
    }

    private void Example()
    {
        InvokeRepeating("LaunchProjectile", 2, 0.3F);
    }
}

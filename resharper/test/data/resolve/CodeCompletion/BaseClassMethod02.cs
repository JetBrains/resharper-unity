using UnityEngine;

public class A : MonoBehaviour
{
    public Rigidbody projectile;

    private void LaunchProjectile()
    {
        Rigidbody instance = Instantiate(projectile);
        instance.velocity = UnityEngine.Random.insideUnitSphere * 5;
    }
}

public class B : A
{
    private void Example()
    {
        Invoke("{caret}", 2);
    }
}

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

public class B : MonoBehaviour
{
    private void Example()
    {
        GetComponent<A>().Invoke("{caret}", 2);
    }
}

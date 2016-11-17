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
    private void DoSomething()
    {
    }

    private void Example()
    {
        GetComponent<A>().Invoke("LaunchProjectile", 2);
        var a = GetComponent<A>();
        a.Invoke("LaunchProjectile", 2);
        Invoke("DoSomething", 3);
        this.Invoke("DoSomething", 3);
    }
}

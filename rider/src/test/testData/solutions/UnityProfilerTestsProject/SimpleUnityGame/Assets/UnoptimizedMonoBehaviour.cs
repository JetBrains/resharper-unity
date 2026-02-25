using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Profiling;
using System.Linq;

public partial class UnoptimizedMonoBehaviour : MonoBehaviour
{
    private List<GameObject> temporaryObjects = new List<GameObject>();
    private StringBuilder stringBuilder = new StringBuilder();

    void Update()
    {
        Profiler.BeginSample("1");
        InefficientStringOperations();
        Profiler.EndSample();
        Profiler.BeginSample("2");
        UnnecessaryGameObjectOperations();
        ExpensivePhysicsCalculations();
        Profiler.EndSample();
        Profiler.BeginSample("3");
        FrequentMemoryAllocations();
        InefficientFindOperations();
        HeavyCalculations();
        ExcessiveGetComponentCalls();
        Profiler.EndSample();
        Profiler.BeginSample("4");
        InefficientCollectionOperations();
        BoxingAndUnboxing();
        InefficientLinqQueries();
        Profiler.EndSample();
        Profiler.BeginSample("5");
        ExcessiveInstantiation();
        // InefficientCoroutineUsage();
        RedundantNullChecks();
        Profiler.EndSample();
    }

    private void InefficientStringOperations()
    {
        // Profiler.BeginSample();
        string result = "";
        for (int i = 0; i < 1000; i++)
        {
            result += "Adding string " + i.ToString() + " to the result. ";
        }
    }

    private void UnnecessaryGameObjectOperations()
    {
        GameObject temp = new GameObject("Temporary");
        temp.AddComponent<Rigidbody>();
        temp.AddComponent<BoxCollider>();
        temporaryObjects.Add(temp);
        if (temporaryObjects.Count > 100)
        {
            foreach (var obj in temporaryObjects)
            {
                Destroy(obj);
            }

            temporaryObjects.Clear();
        }
    }

    private void ExpensivePhysicsCalculations()
    {
        for (int i = 0; i < 1000; i++)
        {
            Physics.Raycast(transform.position, Random.onUnitSphere, out RaycastHit hit);
            if (hit.collider != null)
            {
                // Debug.Log("Hit point: " + hit.point.ToString());
            }
        }
    }

    private void FrequentMemoryAllocations()
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < 1000; i++)
        {
            positions.Add(Random.onUnitSphere * 10f);
        }
    }

    private void InefficientFindOperations()
    {
        for (int i = 0; i < 100; i++)
        {
            GameObject obj = GameObject.Find("SomeObject");
            GameObject.FindGameObjectWithTag("Player");
            GameObject.FindObjectsOfType<MeshRenderer>();
        }
    }

    private void ExcessiveGetComponentCalls()
    {
        for (int i = 0; i < 10000; i++)
        {
            GetComponent<Rigidbody>();
            GetComponent<Collider>();
            GetComponent<MeshRenderer>();
        }
    }

    private void HeavyCalculations()
    {
        const int MATRIX_SIZE = 100;
        const int CALCULATION_ITERATIONS = 10;
        float[,] matrix = new float[MATRIX_SIZE, MATRIX_SIZE];
        for (int i = 0; i < MATRIX_SIZE; i++)
        {
            for (int j = 0; j < MATRIX_SIZE; j++)
            {
                float sum = 0;
                for (int k = 0; k < CALCULATION_ITERATIONS; k++)
                {
                    sum += Mathf.Sin(k) * Mathf.Cos(k) * Mathf.Sqrt(k);
                }

                matrix[i, j] = sum;
            }
        }
    }

    private void InefficientCollectionOperations()
    {
        // Creating new collections every frame
        List<int> numbers = new List<int>();
        Dictionary<string, int> dict = new Dictionary<string, int>();

        for (int i = 0; i < 500; i++)
        {
            numbers.Add(i);
            dict.Add("key" + i, i);
        }

        // Inefficient removal from list
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            if (numbers[i] % 2 == 0)
            {
                numbers.RemoveAt(i); // Forces array shift
            }
        }

        // Checking if contains before adding
        if (!dict.ContainsKey("test"))
        {
            dict["test"] = 1; // Double lookup
        }
    }

    private void BoxingAndUnboxing()
    {
        // Boxing value types into objects
        List<object> mixedList = new List<object>();
        for (int i = 0; i < 1000; i++)
        {
            mixedList.Add(i); // Boxing int to object
            mixedList.Add(i * 0.5f); // Boxing float to object
            mixedList.Add(true); // Boxing bool to object
        }

        // Unboxing
        int sum = 0;
        foreach (object item in mixedList)
        {
            if (item is int)
            {
                sum += (int)item; // Unboxing
            }
        }
    }

    private void InefficientLinqQueries()
    {
        List<int> largeList = new List<int>();
        for (int i = 0; i < 1000; i++)
        {
            largeList.Add(i);
        }

        // Multiple enumerations of the same collection
        var result1 = largeList.Where(x => x > 500).ToList();
        var result2 = largeList.Where(x => x > 500).Count();
        var result3 = largeList.Where(x => x > 500).First();

        // Inefficient nested LINQ
        var nested = largeList.SelectMany(x =>
            largeList.Where(y => y != x)).ToList();
    }

    private void ExcessiveInstantiation()
    {
        // Creating new objects repeatedly
        for (int i = 0; i < 100; i++)
        {
            Vector3 position = new Vector3(i, i, i);
            Quaternion rotation = new Quaternion(0, 0, 0, 1);
            Color color = new Color(1, 1, 1, 1);

            // Creating temporary arrays
            int[] tempArray = new int[100];
            float[] floatArray = new float[50];
        }

        // Allocating large objects
        byte[] hugeArray = new byte[1024 * 1024]; // 1MB allocation every frame
    }

    // private void InefficientCoroutineUsage()
    // {
    //     // Starting coroutines every frame without stopping them
    //     StartCoroutine(WasteCoroutine());
    //     StartCoroutine(WasteCoroutine());
    //     StartCoroutine(WasteCoroutine());
    // }
    //
    // private System.Collections.IEnumerator WasteCoroutine()
    // {
    //     yield return new WaitForSeconds(0.1f); // New WaitForSeconds allocation
    //     yield return new WaitForSeconds(0.2f); // Another allocation
    // }

    private void RedundantNullChecks()
    {
        // Excessive null checking
        for (int i = 0; i < 500; i++)
        {
            if (gameObject != null)
            {
                if (transform != null)
                {
                    if (transform.position != null)
                    {
                        Vector3 pos = transform.position;
                        if (pos != null)
                        {
                            // Debug.Log("Position: " + pos.ToString());
                        }
                    }
                }
            }
        }

        // Using string comparison incorrectly
        if (gameObject.tag == "Player") // Allocates string
        {
            Debug.Log("Found player");
        }
    }
}

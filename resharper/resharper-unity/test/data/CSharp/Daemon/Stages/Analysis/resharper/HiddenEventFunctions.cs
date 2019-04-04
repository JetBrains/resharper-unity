// ReSharper disable Unity.RedundantEventFunction
using UnityEditor;
using UnityEngine;

public class Base : MonoBehaviour
{
    public void Start()
    {
    }

    protected void OnEnable()
    {
    }

    protected virtual void OnDestroy()
    {
    }

    private void Awake()
    {
    }

    // Not an event function
	private void OnAudioFilterRead()
	{
	}
}

public class Derived : Base
{
    // Requires "new" - inspection comes from R# core
    public void Start()
    {
    }

    // Requires "new" - inspection comes from R# core
    public void OnEnable()
    {
    }

    // Requires "new" or "override" - inspection comes from R# core
    public void OnDestroy()
    {
    }

    // Valid code, but show that it's hiding an event function
    private void Awake()
    {
    }

    // Perfectly valid
	private void OnAudioFilterRead(float[] data, int channels)
	{
	}
}


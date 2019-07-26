using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Foo : MonoBehaviour
{
    public int my{off:Initialize:in:'Start'}Value;
    public int my{off:Add:'RequireComponent'}Value_;

    private int my{off:Initialize:in:'Start'}Value2;
    private int my{off:Add:'RequireComponent'}Value2_;

    [SerializeField] private int my{off:Initialize:in:'Start'}Value3;
    [SerializeField] private int my{off:Add:'RequireComponent'}Value3_;

    [NonSerialized] public int my{off:Initialize:in:'Start'}Value4;
    [NonSerialized] public int my{off:Add:'RequireComponent'}Value4_;

    public Collider2D Colli{on:Initialize:in:'Start'}{on:Add:'RequireComponent'}der2D;
    public Collider2D Collid{off:Initialize:in:'Start'}{on:Add:'RequireComponent'}er2D_;
    public Camera Cam{on:Initialize:in:'Start'}{off:Add:'RequireComponent'}era;
    public Camera Cam{off:Initialize:in:'Start'}{off:Add:'RequireComponent'}era_;

    public void Awake() {
        Camera_ = GetComponent<Camera>();
        Collider2D_ = GetComponent<Collider2D>();
    }
}

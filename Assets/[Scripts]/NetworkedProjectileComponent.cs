using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkedProjectileComponent : MonoBehaviour
{
    //public int id = 0;
    private Rigidbody2D rb;
    public Rigidbody2D RB => rb;
    public Vector3 Vel => rb.velocity;

    public NetManager netMan;
    public int NetID;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetImpulse(Vector3 vel)
    {
        rb.velocity = vel;
    }
}

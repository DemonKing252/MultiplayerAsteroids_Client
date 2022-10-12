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

    public bool clientOwner = false;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetImpulse(Vector3 vel)
    {
        rb.velocity = vel;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If owned by this client, delete the bullet on the Network.
        if (clientOwner && collision.gameObject.CompareTag("Asteroid"))
        {
            netMan.DeleteAsteroidOnNetwork(collision.GetComponent<NetAsteroid>());
        }
    }
}

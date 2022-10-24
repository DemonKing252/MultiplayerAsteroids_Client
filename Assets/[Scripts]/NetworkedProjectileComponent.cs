using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkedProjectileComponent : MonoBehaviour
{
    public bool OwnedByThisClient = false;
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
    private void FixedUpdate()
    {
        if (!OwnedByThisClient)
            return;

        if (transform.position.y > 10f) {

            var deleteNetBullet = new ClientToServer.DeleteNetProjectile();
            deleteNetBullet.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DELETE_BULLET;
            deleteNetBullet.playerId = netMan.PlayerID;
            deleteNetBullet.netId = netMan.NetID;
            deleteNetBullet.bullet = new NetUtils.SProjectile();
            deleteNetBullet.bullet.id = NetID;
            deleteNetBullet.bullet.pos = new NetUtils.SVector2(transform.position.x, transform.position.y);
            deleteNetBullet.bullet.vel = new NetUtils.SVector2(0f, 10f);
            netMan.SendToServer(deleteNetBullet);
        }
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetAsteroid : MonoBehaviour
{
    public bool OwnedByThisClient = false;
    private Rigidbody2D rb;
    private Vector2 vel;
    public Vector2 Vel { get { return vel; } set { vel = value; rb.velocity = value; } }
    public int NetID = -1;
    public NetManager netMan;
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!OwnedByThisClient)
            return;

        if (transform.position.y < -10f)
        {
            var delNetAsteroid = new ClientToServer.DeleteNetAsteroid();
            delNetAsteroid.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DEL_ASTEROID;
            delNetAsteroid.playerId = netMan.PlayerID;
            delNetAsteroid.netId = netMan.NetID;
            delNetAsteroid.asteroid = new NetUtils.SAsteroid();
            delNetAsteroid.asteroid.id = NetID;
            delNetAsteroid.asteroid.pos = new NetUtils.SVector2(transform.position.x, transform.position.y);
            delNetAsteroid.asteroid.vel = new NetUtils.SVector2(Vel.x, Vel.y);
            netMan.SendToServer(delNetAsteroid);
        }
    }
}

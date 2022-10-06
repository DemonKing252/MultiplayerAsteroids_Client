using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetClient : MonoBehaviour
{
    public int NetID = -1;
    private NetManager netManager;
    private float speed = 5f;

    private Vector3 posPrev;
    private Vector3 posNow;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        netManager = FindObjectOfType<NetManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (NetID == netManager.NetID)
        {
            rb.velocity = new Vector2(speed * Input.GetAxis("Horizontal"), speed * Input.GetAxis("Vertical"));

            posPrev = posNow;
            posNow = transform.position;
            float magnitude = (posNow - posPrev).magnitude;
            if (magnitude >= Mathf.Epsilon/* || netManager.netProjectiles.Count > 0*/)
            {
                netManager.UpdatePositionOnNetwork(transform.position);
            }
            
            if (Input.GetKeyUp(KeyCode.Space))
            {
                Vector3 worldP = transform.position;
                var bulletGO = Instantiate(netManager.bulletPrefab, worldP, Quaternion.identity);
                var projComp = bulletGO.GetComponent<NetworkedProjectileComponent>();
                projComp.SetImpulse(new Vector2(0f, 10f));
                netManager.netProjectiles.Add(projComp);

                var networkedBullet = new ClientToServer.SpawnNetProjectile();
                networkedBullet.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_SPAWN_BULLET;
                networkedBullet.netId = netManager.NetID;
                networkedBullet.playerId = netManager.PlayerID;
                networkedBullet.pos = new NetUtils.SVector2(worldP.x, worldP.y);
                networkedBullet.vel = new NetUtils.SVector2(0f, 10f);
                netManager.SendToServer(networkedBullet);
            }
            
            //{
            //    var bullet = Instantiate(netManager.bulletPrefab, transform.position, Quaternion.identity);
            //    var projComp = bullet.GetComponent<NetworkedProjectileComponent>();
            //    projComp.SetImpulse(new Vector2(0f, 10f));
            //
            //    var networkedBullet = new ClientToServer.SpawnProjectile();
            //
            //    networkedBullet.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_SPAWN_BULLET;
            //    Vector3 myPos = transform.position;
            //    networkedBullet.pos = new NetUtils.SVector2(myPos.x, myPos.y);
            //    networkedBullet.vel = new NetUtils.SVector2(0f, 10f);
            //    netManager.SendToServer(networkedBullet);
            //
            //    netManager.LatestProjectile = new NetProjectile();
            //    netManager.LatestProjectile.projComp = projComp;
            //
            //}

        }
    }
}

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
        netManager.NetworkPlayTime = 0f;    // Re roll match, this time gets reset
    }

    void FixedUpdate()
    {
        if (NetID == netManager.NetID)
            netManager.NetworkPlayTime += Time.fixedDeltaTime;
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
            if (magnitude >= Mathf.Epsilon)
            {
                netManager.UpdatePositionOnNetwork(transform.position);
            }
            
            if (Input.GetKeyUp(KeyCode.Space))
            {
                Vector3 worldP = transform.position;
                var networkedBullet = new ClientToServer.SpawnNetProjectile();
                networkedBullet.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_SPAWN_BULLET;
                networkedBullet.netId = netManager.NetID;
                networkedBullet.playerId = netManager.PlayerID;
                networkedBullet.pos = new NetUtils.SVector2(worldP.x, worldP.y);
                networkedBullet.vel = new NetUtils.SVector2(0f, 10f);
                netManager.SendToServer(networkedBullet);
            }
            
        }
    }
}

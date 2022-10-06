using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
namespace NetUtils
{
    public enum CommandSignifiers
    {
        TOSERVER_HANDSHAKE = 0,     // Tell the server that we joined, add our netid to the clients dictionary
        TOCLIENT_HANDSHAKE = 1,     // Recieve a copy of all clients currently in the session, spawn them all in
        START_MATCHMAKING = 2,
        //ADD_NEW_CLIENT = 3,          // Client that just joined

        TO_SERVER_POS_UPDATE = 4,
        TO_CLIENT_POS_UPDATE = 5,

        TO_SERVER_DROP_CLIENT = 6,
        TO_CLIENT_DROP_CLIENT = 7,

        TO_SERVER_SPAWN_BULLET = 8,
        TO_CLIENT_SPAWN_BULLET = 9,

        TO_SERVER_DELETE_BULLET = 10,
        TO_CLIENT_DELETE_BULLET = 11,
        //BULLET_SPAWN_RESPONSE = 10,
        //UPDATE_BULLET_ON_SERVER = 11,

        MATCH_MADE = 100,
        REROLL_MATCH = 101
    }

    [Serializable]
    public class NetworkHeader
    {
        public CommandSignifiers commandSignifier;
    }

    [Serializable]
    public class SVector2
    {
        [SerializeField]
        private string x;
        [SerializeField]
        private string y;

        public float X { get { return float.Parse(x); } set { x = value.ToString("F2"); } }
        public float Y { get { return float.Parse(y); } set { y = value.ToString("F2"); } }

        public SVector2(float x, float y)
        {
            this.x = x.ToString("F2");
            this.y = y.ToString("F2");
        }
        public SVector2() { x = y = "0.00"; }

    }

    [Serializable]
    public class SPlayer
    {
        public int id;
        public SVector2 pos;
    }

    [Serializable]
    public class SProjectile
    {
        public int id = -1;
        public SVector2 pos;
        public SVector2 vel;

    }
    
}


namespace ClientToServer
{
    [Serializable]
    public class Handshake : NetUtils.NetworkHeader
    {
        public string message;
    }
    [Serializable]
    public class RequestMatch : NetUtils.NetworkHeader
    {
        public int clientId;
    }
    [Serializable]
    public class PositionUpdate : NetUtils.NetworkHeader
    {
        public NetUtils.SVector2 pos = new NetUtils.SVector2();
        public int clientId;    // Client id
        public int playerId;    // Player id (ie: the id in the match that this player is in)
        //public List<NetUtils.SProjectile> projectiles = new List<NetUtils.SProjectile>();
    }

    [Serializable]
    public class DropClient : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
    }
    [Serializable]
    public class SpawnNetProjectile : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.SVector2 pos;
        public NetUtils.SVector2 vel;
    }
    [Serializable]
    public class DeleteNetProjectile : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.SProjectile bullet;
    }

    //[Serializable]
    //public class ReRequestMatch : NetUtils.NetworkHeader
    //{
    //    public int clientId;
    //}

}
namespace ServerToClient
{
    [Serializable]
    public class Handshake : NetUtils.NetworkHeader
    {
        public int id;  // Our ID
        //public NetUtils.SPlayer[] players;   // Other player ID's
        //public NetUtils.SProjectile[] projectiles;
    }
    [Serializable]
    public class AddNewClient : NetUtils.NetworkHeader
    {
        public int newPlayerID;
    }
    [Serializable]
    public class PositionUpdate : NetUtils.NetworkHeader
    {
        public NetUtils.SVector2 pos;
        public int playerId;
    }
    [Serializable]
    public class DropClient : NetUtils.NetworkHeader
    {
        public int id;
    }

    [Serializable]
    public class SpawnNetProjectile : NetUtils.NetworkHeader
    {
        public int playerId;
        public NetUtils.SProjectile bullet;
    }
    [Serializable]
    public class DeleteNetProjectile : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.SProjectile bullet;
    }

    //[Serializable]
    //public class BulletSpawnResponse : NetUtils.NetworkHeader
    //{
    //    public NetUtils.SProjectile proj;    
    //}
    //
    //[Serializable]
    //public class DeleteBullet : NetUtils.NetworkHeader
    //{
    //    public NetUtils.SProjectile proj;
    //}


    [Serializable]
    public class Matchmade : NetUtils.NetworkHeader
    {
        public string message;
        public int id1;
        public int id2;
        public int player;
    }


}
[Serializable]
public class NetProjectile
{
    public NetUtils.SProjectile sProj;
    public NetworkedProjectileComponent projComp;
}


public class NetManager : MonoBehaviour
{
    // Projectiles that are owned by this client
    public List<NetworkedProjectileComponent> netProjectiles = new List<NetworkedProjectileComponent>();

    // Projectiles that belong to other clients.
    public List<NetworkedProjectileComponent> otherProjectiles = new List<NetworkedProjectileComponent>();
    
    public GameObject bulletPrefab;
    public GameObject playerPrefab;
    public UdpClient client;
    
    public ServerToClient.Handshake clientHandShake;
    public ServerToClient.AddNewClient addClients;
    public ServerToClient.PositionUpdate posUpdate;
    public ServerToClient.DropClient dropClient;
    public ServerToClient.SpawnNetProjectile spawnNetProj;
    public ServerToClient.DeleteNetProjectile delProj;
    public ServerToClient.Matchmade match;

    public int NetID = -1;
    public int PlayerID = -1;

    [SerializeField] private NetClient player1 = null;
    [SerializeField] private NetClient player2 = null;

    // Async task handling
    private bool match_made = false;
    private bool update_pos = false;
    private bool drop_client = false;
    private bool spawn_bullet = false;
    private bool delete_bullet = false;

    private string ec2_ip = "52.14.46.199";
    private int port = 12345;
    
    public bool ec2_connect = true;
    
    private NetProjectile latestProjectile = null;
    public NetProjectile LatestProjectile { get { return latestProjectile; } set { latestProjectile = value; } }

    public ClientToServer.PositionUpdate positionUpdate;
    private bool connected = false;
    private bool alreadyConnected = true;
    // Start is called before the first frame update
    void Start()
    {
        MenuController.Instance.onConnectToHost += InitUDPClient;
        MenuController.Instance.onHostIPChanged += OnIPChanged;
        MenuController.Instance.onHostPortChanged += OnPortChanged;
        MenuController.Instance.onStartMatchmaking += OnStartMatchmaking;
    }
    public void InitUDPClient()
    {
        client = new UdpClient();
        client.Connect(ec2_ip, port);

        ClientToServer.Handshake message = new ClientToServer.Handshake();
        message.commandSignifier = NetUtils.CommandSignifiers.TOSERVER_HANDSHAKE;
        message.message = "Hello from client";

        SendToServer(message);

        client.BeginReceive(new AsyncCallback(OnMessageRecv), client);


        Time.timeScale = 1f;
        Invoke(nameof(OnConnectionTimedOut), 2f);
    }
    private void OnConnectionTimedOut()
    {
        if (!connected)
        {
            Debug.Log("Could not connect to host!");
            MenuController.Instance.OnConnectionTimedOut();
        }
    }

    public void OnIPChanged(string ip)
    {
        ec2_ip = ip;
    }
    public void OnPortChanged(string port)
    {
        this.port = int.Parse(port);
    }

    public void OnStartMatchmaking()
    {
        var startMatchmaking = new ClientToServer.RequestMatch();
        startMatchmaking.commandSignifier = NetUtils.CommandSignifiers.START_MATCHMAKING;
        startMatchmaking.clientId = NetID;
        SendToServer(startMatchmaking);
        MenuController.Instance.ChangeGameState(GameStates.WaitingForMatch);
    }


    private void OnMessageRecv(IAsyncResult result)
    {
        UdpClient sock = result.AsyncState as UdpClient;
        IPEndPoint endPoint = new IPEndPoint(0, 0);

        byte[] bytes = sock.EndReceive(result, ref endPoint);

        NetUtils.NetworkHeader clientmsg = LoadJson<NetUtils.NetworkHeader>(bytes);

        switch (clientmsg.commandSignifier) {

            case NetUtils.CommandSignifiers.TOCLIENT_HANDSHAKE:
                Debug.Log("Connected.");
                clientHandShake = LoadJson<ServerToClient.Handshake>(bytes);
                NetID = clientHandShake.id;
                connected = true;
                alreadyConnected = false;
                break;
            case NetUtils.CommandSignifiers.MATCH_MADE:
                match = LoadJson<ServerToClient.Matchmade>(bytes);
                PlayerID = match.player;
                Debug.Log(match.message);
                match_made = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_POS_UPDATE:
                posUpdate = LoadJson<ServerToClient.PositionUpdate>(bytes);
                update_pos = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_DROP_CLIENT:
                dropClient = LoadJson<ServerToClient.DropClient>(bytes);
                drop_client = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_SPAWN_BULLET:
                spawnNetProj = LoadJson<ServerToClient.SpawnNetProjectile>(bytes);
                spawn_bullet = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_DELETE_BULLET:
                delProj = LoadJson<ServerToClient.DeleteNetProjectile>(bytes);
                delete_bullet = true;
                break;
        }

        sock.BeginReceive(new AsyncCallback(OnMessageRecv), sock);
    }

    public string BytesToJsonString(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }

    public T LoadJson<T>(byte[] bytes)
    {
        string s = Encoding.ASCII.GetString(bytes);
        return JsonUtility.FromJson<T>(s);
    }

    public string ObjToJson(object msg)
    {
        string s = JsonUtility.ToJson(msg);
        return s;
    }
    public byte[] ObjToBytes(object msg)
    {
        string s = JsonUtility.ToJson(msg);
        byte[] bytes = Encoding.ASCII.GetBytes(s);

        return bytes;
    }

    public void SendToServer(object message)
    {
        int _size = JsonUtility.ToJson(message).Length;
        client.Send(ObjToBytes(message), _size);

        client.BeginReceive(new AsyncCallback(OnMessageRecv), client);
    }

    public string ByteToString(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }

    public void UpdatePositionOnNetwork(Vector3 pos)
    {
        positionUpdate = new ClientToServer.PositionUpdate();
        positionUpdate.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_POS_UPDATE;
        positionUpdate.pos = new NetUtils.SVector2(pos.x, pos.y);
        positionUpdate.clientId = NetID;
        positionUpdate.playerId = PlayerID;
        SendToServer(positionUpdate);
    }

    public void DeleteBulletsOutOfBounds()
    {
        // Only if it exists in our netProjectiles array, then delete it.
        foreach(var b in netProjectiles)
        {
            Vector3 absolutePos = b.transform.position;
            absolutePos.x = Mathf.Abs(absolutePos.x);
            absolutePos.y = Mathf.Abs(absolutePos.y);

            if (absolutePos.x > 10f || absolutePos.y > 10f)
            {
                Debug.Log("Deleting bullet");
                // This bullet is out of bounds
                // Delete it on the network.
                var deleteNetBullet = new ClientToServer.DeleteNetProjectile();
                deleteNetBullet.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DELETE_BULLET;
                deleteNetBullet.playerId = PlayerID;
                deleteNetBullet.netId = NetID;
                
                deleteNetBullet.bullet = new NetUtils.SProjectile();
                deleteNetBullet.bullet.id = b.NetID;
                deleteNetBullet.bullet.pos = new NetUtils.SVector2(b.transform.position.x, b.transform.position.y);
                deleteNetBullet.bullet.vel = new NetUtils.SVector2(0f, 10f);
                SendToServer(deleteNetBullet);
            }
        }
    }

    void OnApplicationQuit()
    {
        MenuController.Instance.onConnectToHost -= InitUDPClient;
        MenuController.Instance.onHostIPChanged -= OnIPChanged;
        MenuController.Instance.onHostPortChanged -= OnPortChanged;
        MenuController.Instance.onStartMatchmaking -= OnStartMatchmaking;

        // Need a fix for this too
        if (!connected)
            return;

        ClientToServer.DropClient drop = new ClientToServer.DropClient();
        drop.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DROP_CLIENT;
        drop.netId = NetID;
        drop.playerId = PlayerID;
        SendToServer(drop);
    }
    // This also handles adding clients
    void HandleMatchmaking()
    {
        if (!alreadyConnected)
        {
            MenuController.Instance.ChangeGameState(GameStates.Matchmaking);
            alreadyConnected = true;
        }
        if (match_made)
        {
            Time.timeScale = 1f;
            MenuController.Instance.ChangeGameState(GameStates.MatchmakingGame);
            for (int i = 0; i < 2; i++)
            {
                GameObject go = Instantiate(playerPrefab, new Vector3(i == 0 ? -3f : +3f, -3f, 0f), Quaternion.identity);
                var client = go.GetComponent<NetClient>();
                client.NetID = i == 0 ? match.id1 : match.id2;
                if (i == 0)
                    player1 = client;
                else
                    player2 = client;
            }

            match_made = false;
        }
    }
    void UpdateClients()
    {
        if (update_pos)
        {
            if (posUpdate.playerId == 1)
                player1.transform.position = new Vector3(posUpdate.pos.X, posUpdate.pos.Y);
            else if (posUpdate.playerId == 2)
                player2.transform.position = new Vector3(posUpdate.pos.X, posUpdate.pos.Y);
            else
                Debug.LogWarning("Player ID unknown.");

            update_pos = false;
        }
    }
    void DropClients()
    {
        if (drop_client)
        {
            Destroy(player1.gameObject);
            Destroy(player2.gameObject);
            player1 = player2 = null;

            PlayerID = -1;
            Debug.Log("Client left the session, returning to matchmaking menu...");

            var requestMatch = new ClientToServer.RequestMatch();
            requestMatch.commandSignifier = NetUtils.CommandSignifiers.REROLL_MATCH;
            requestMatch.clientId = NetID;
            SendToServer(requestMatch);
            MenuController.Instance.ChangeGameState(GameStates.Matchmaking);
            MenuController.Instance.ClientLeftMatchWarningText.gameObject.SetActive(true);
            drop_client = false;
        }
    }
    void SpawnAndDeleteNetBullets()
    {
        if (spawn_bullet)
        {
            // This is our bullet, just assign the id to it
            if (PlayerID == spawnNetProj.playerId)
            {
                netProjectiles[netProjectiles.Count - 1].NetID = spawnNetProj.bullet.id;
            }
            // This is a bullet that the other client shot, spawn it in a seperate container so that we can keep track of it
            else
            {
                Vector3 worldP = new Vector3(spawnNetProj.bullet.pos.X, spawnNetProj.bullet.pos.Y);
                var bulletGO = Instantiate(bulletPrefab, worldP, Quaternion.identity);
                var projComp = bulletGO.GetComponent<NetworkedProjectileComponent>();
                projComp.SetImpulse(new Vector2(0f, 10f));
                projComp.NetID = spawnNetProj.bullet.id;
                otherProjectiles.Add(projComp);
            }

            spawn_bullet = false;
        }
        if (delete_bullet)
        {
            // Search through our bullet list and delete it.
            if (PlayerID == delProj.playerId)
            {
                foreach (var b in netProjectiles)
                {
                    if (b.NetID == delProj.bullet.id)
                    {
                        Destroy(b.gameObject);
                        netProjectiles.Remove(b);
                        break;
                    }
                }
            }
            // Search through the other bullet list and delete it.
            else
            {
                foreach (var b in otherProjectiles)
                {
                    if (b.NetID == delProj.bullet.id)
                    {
                        Destroy(b.gameObject);
                        otherProjectiles.Remove(b);
                        break;
                    }
                }
            }

            delete_bullet = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleMatchmaking();
        DropClients();    
        UpdateClients();
        SpawnAndDeleteNetBullets();
        DeleteBulletsOutOfBounds();
    }
}

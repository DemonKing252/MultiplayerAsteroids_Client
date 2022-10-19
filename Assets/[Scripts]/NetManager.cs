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

        TO_SERVER_POS_UPDATE = 4,
        TO_CLIENT_POS_UPDATE = 5,

        TO_SERVER_DROP_CLIENT = 6,
        TO_CLIENT_DROP_CLIENT = 7,

        TO_SERVER_SPAWN_BULLET = 8,
        TO_CLIENT_SPAWN_BULLET = 9,

        TO_SERVER_DELETE_BULLET = 10,
        TO_CLIENT_DELETE_BULLET = 11,

        TO_SERVER_SPAWN_ASTEROID = 12,
        TO_CLIENT_SPAWN_ASTEROID = 13,

        TO_SERVER_DEL_ASTEROID = 14,
        TO_CLIENT_DEL_ASTEROID = 15,

        TO_SERVER_USER_AUTHENTICATE = 50,
        TO_SERVER_LOGIN          = 51,
        TO_CLIENT_CREATE_ACCOUNT_RESPONSE = 52,


        MATCH_MADE = 100,
        REROLL_MATCH = 101,
    }
    public enum DeletionFlags
    {
        BY_PLAYER = 0,
        BY_CLIENT = 1
    }
    public enum AccountResponse
    {
        CREATE_ACCOUNT_USER_TAKEN = 0,
        CREATE_ACCOUNT_SUCCESS = 1,
        
        WRONG_USER = 2,
        WRONG_PASS = 3,
        LOGIN_SUCCESS = 4,
        USER_ACTIVE_ON_SERVER = 5
    
    }
    public enum UserAuthenticate
    {
        CREATE_ACCOUNT = 0,
        LOGIN = 1,
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

    [Serializable]
    public class SAsteroid
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
    }

    [Serializable]
    public class DropClient : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public string user;
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
    
    // Player 1 spawns asteroids, they get send to player 2 over the server
    [Serializable]
    public class SpawnNetAsteroid : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.SAsteroid asteroid;
    }
    [Serializable]
    public class DeleteNetAsteroid : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.DeletionFlags deletionFlags;
        public NetUtils.SAsteroid asteroid;
    }

    [Serializable]
    public class UserAuthenticationCommand : NetUtils.NetworkHeader
    {
        public NetUtils.UserAuthenticate authen;
        public string user;
        public string pass;
    }


}
namespace ServerToClient
{
    [Serializable]
    public class Handshake : NetUtils.NetworkHeader
    {
        public int id;  // Our ID
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

    [Serializable]
    public class Matchmade : NetUtils.NetworkHeader
    {
        public string message;
        public int id1;
        public int id2;
        public int player;
    }
    // Player 1 spawns asteroids, they get send to player 2 over the server
    [Serializable]
    public class SpawnNetAsteroid : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.SAsteroid asteroid;
    }
    [Serializable]
    public class DeleteNetAsteroid : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.DeletionFlags deletionFlags;
        public NetUtils.SAsteroid asteroid;
    }
    [Serializable]
    public class CreateAccountResponse : NetUtils.NetworkHeader
    {
        public NetUtils.AccountResponse response;
        public string user;
        public string pass;
    }
}


public class NetManager : MonoBehaviour
{
    // Networked gameobjects
    private NetClient player1 = null;
    private NetClient player2 = null;

    // int represents Network ID
    private Dictionary<int, NetworkedProjectileComponent> ourProjectiles = new Dictionary<int, NetworkedProjectileComponent>();
    private Dictionary<int, NetworkedProjectileComponent> otherProjectiles = new Dictionary<int, NetworkedProjectileComponent>();
    private Dictionary<int, NetAsteroid> ourAsteroids = new Dictionary<int, NetAsteroid>();
    private Dictionary<int, NetAsteroid> otherAsteroids = new Dictionary<int, NetAsteroid>();

    // Prefabs
    public GameObject bulletPrefab;
    public GameObject playerPrefab;
    public GameObject asteroidPrefab;
    public UdpClient client;
    
    // Server Responses as objects (from json)
    public ServerToClient.Handshake clientHandShake;
    public ServerToClient.AddNewClient addClients;
    public ServerToClient.PositionUpdate posUpdate;
    public ServerToClient.DropClient dropClient;
    public ServerToClient.SpawnNetProjectile spawnNetProj;
    public ServerToClient.DeleteNetProjectile delProj;
    public ServerToClient.Matchmade match;
    public ServerToClient.SpawnNetAsteroid spawnNetAsteroid;
    public ServerToClient.DeleteNetAsteroid delNetAsteroid;
    public ServerToClient.CreateAccountResponse createAccountResponse;
    public ClientToServer.PositionUpdate positionUpdate;

    // Matchmaking and Client Id's
    public int NetID = -1;
    public int PlayerID = -1;

    // Async task handling
    private bool match_made = false;
    private bool update_pos = false;
    private bool drop_client = false;
    private bool spawn_bullet = false;
    private bool delete_bullet = false;
    private bool spawn_asteroid = false;
    private bool delete_asteroid = false;
    private bool user_authen_resp = false;
    private bool login_Response = false;

    // Server ip and port
    public string ec2_ip = "52.14.46.199";
    public string port = "12345";
    
    public bool ec2_connect = true; // for debugging, easier to test locally when debugging code
    public string user_name = "";   // waiting on server response
    private bool connected = false;
    private bool alreadyConnected = true;
    // Start is called before the first frame update
    void Start()
    {
        MenuController.Instance.onConnectToHost += InitUDPClient;
        MenuController.Instance.onHostIPChanged += OnIPChanged;
        MenuController.Instance.onHostPortChanged += OnPortChanged;
        MenuController.Instance.onStartMatchmaking += OnStartMatchmaking;
        MenuController.Instance.onCreateAccount += OnCreateAccount;
        MenuController.Instance.onLogin += OnLogin;
    }
    public void InitUDPClient()
    {
        client = new UdpClient();
        //client.Connect(ec2_ip, int.Parse(port));
        client.Connect("127.0.0.1", 5491);

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
        this.port = port;
    }

    public void OnStartMatchmaking()
    {
        var startMatchmaking = new ClientToServer.RequestMatch();
        startMatchmaking.commandSignifier = NetUtils.CommandSignifiers.START_MATCHMAKING;
        startMatchmaking.clientId = NetID;
        SendToServer(startMatchmaking);
        MenuController.Instance.ChangeGameState(GameStates.WaitingForMatch);
    }
    public void OnCreateAccount(string user, string pass)
    {
        Debug.Log("Created account with credentials, user: " + user + ", pass: " + pass);
        var createAccount = new ClientToServer.UserAuthenticationCommand();
        createAccount.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_USER_AUTHENTICATE;
        createAccount.authen = NetUtils.UserAuthenticate.CREATE_ACCOUNT;
        createAccount.user = user;
        createAccount.pass = pass;
        SendToServer(createAccount);

    }
    public void OnLogin(string user, string pass)
    {
        Debug.Log("Login with credentials, user: " + user + ", pass: " + pass);
        var logintoAccount = new ClientToServer.UserAuthenticationCommand();
        logintoAccount.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_USER_AUTHENTICATE;
        logintoAccount.authen = NetUtils.UserAuthenticate.LOGIN;
        logintoAccount.user = user;
        logintoAccount.pass = pass;
        SendToServer(logintoAccount);
    }


    private void OnMessageRecv(IAsyncResult result)
    {
        UdpClient sock = result.AsyncState as UdpClient;
        IPEndPoint endPoint = new IPEndPoint(0, 0);

        byte[] bytes = sock.EndReceive(result, ref endPoint);

        NetUtils.NetworkHeader clientmsg = LoadJson<NetUtils.NetworkHeader>(bytes);

        switch (clientmsg.commandSignifier) {

            case NetUtils.CommandSignifiers.TOCLIENT_HANDSHAKE:
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
            case NetUtils.CommandSignifiers.TO_CLIENT_SPAWN_ASTEROID:
                spawnNetAsteroid = LoadJson<ServerToClient.SpawnNetAsteroid>(bytes);
                spawn_asteroid = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_DEL_ASTEROID:
                delNetAsteroid = LoadJson<ServerToClient.DeleteNetAsteroid>(bytes);
                delete_asteroid = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_CREATE_ACCOUNT_RESPONSE:
                createAccountResponse = LoadJson<ServerToClient.CreateAccountResponse>(bytes);                
                user_authen_resp = true;
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

    public void DeleteAsteroidOnNetwork(NetAsteroid netAsteroidComp)
    {
        var delNetAsteroid = new ClientToServer.DeleteNetAsteroid();
        delNetAsteroid.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DEL_ASTEROID;
        delNetAsteroid.playerId = PlayerID;
        delNetAsteroid.netId = NetID;
        delNetAsteroid.deletionFlags = NetUtils.DeletionFlags.BY_CLIENT;
        delNetAsteroid.asteroid = new NetUtils.SAsteroid();
        delNetAsteroid.asteroid.id = netAsteroidComp.NetID;
        delNetAsteroid.asteroid.pos = new NetUtils.SVector2(netAsteroidComp.transform.position.x, netAsteroidComp.transform.position.y);
        delNetAsteroid.asteroid.vel = new NetUtils.SVector2(netAsteroidComp.Vel.x, netAsteroidComp.Vel.y);
        SendToServer(delNetAsteroid);
    }

    public void DeleteBulletsAndAsteroidsOutOfBounds()
    {
        // Only if it exists in our netProjectiles array, then delete it.
        foreach(var b in ourProjectiles.Values)
        {
            Vector3 absolutePos = b.transform.position;
            absolutePos.x = Mathf.Abs(absolutePos.x);
            absolutePos.y = Mathf.Abs(absolutePos.y);

            if (absolutePos.x > 10f || absolutePos.y > 10f)
            {
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
        foreach (var a in ourAsteroids.Values)
        {

            Vector3 absolutePos = a.transform.position;
            absolutePos.x = Mathf.Abs(absolutePos.x);
            absolutePos.y = Mathf.Abs(absolutePos.y);
            if (absolutePos.x > 11f || absolutePos.y > 11f)
            {
                var delNetAsteroid = new ClientToServer.DeleteNetAsteroid();
                delNetAsteroid.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DEL_ASTEROID;
                delNetAsteroid.playerId = PlayerID;
                delNetAsteroid.netId = NetID;
                delNetAsteroid.deletionFlags = NetUtils.DeletionFlags.BY_PLAYER;
                delNetAsteroid.asteroid = new NetUtils.SAsteroid();
                delNetAsteroid.asteroid.id = a.NetID;
                delNetAsteroid.asteroid.pos = new NetUtils.SVector2(a.transform.position.x, a.transform.position.y);
                delNetAsteroid.asteroid.vel = new NetUtils.SVector2(a.Vel.x, a.Vel.y);
                SendToServer(delNetAsteroid);
            }
        }
    }

    void OnApplicationQuit()
    {
        MenuController.Instance.onConnectToHost -= InitUDPClient;
        MenuController.Instance.onHostIPChanged -= OnIPChanged;
        MenuController.Instance.onHostPortChanged -= OnPortChanged;
        MenuController.Instance.onStartMatchmaking -= OnStartMatchmaking;
        MenuController.Instance.onCreateAccount -= OnCreateAccount;
        MenuController.Instance.onLogin -= OnLogin;

        // If playing singleplayer, were not shutting down the network
        if (!connected)
            return;

        ClientToServer.DropClient drop = new ClientToServer.DropClient();
        drop.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DROP_CLIENT;
        drop.netId = NetID;
        drop.playerId = PlayerID;
        drop.user = user_name;
        SendToServer(drop);
    }

    // This also handles adding clients
    void HandleMatchmaking()
    {
        if (!alreadyConnected)
        {
            MenuController.Instance.ChangeGameState(GameStates.UserAuthentication);
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

            // Only the host will spawn the asteroids
            if (PlayerID == 1)
                InvokeRepeating(nameof(SpawnAsteroid), 0f, 2f);

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
            CancelInvoke(nameof(SpawnAsteroid));
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
                Vector3 worldP = new Vector3(spawnNetProj.bullet.pos.X, spawnNetProj.bullet.pos.Y);
                var bulletGO = Instantiate(bulletPrefab, worldP, Quaternion.identity);
                var projComp = bulletGO.GetComponent<NetworkedProjectileComponent>();
                projComp.SetImpulse(new Vector2(0f, 10f));
                projComp.NetID = spawnNetProj.bullet.id;
                projComp.clientOwner = true;    // This bullet is owned by THIS client
                projComp.netMan = this;
                ourProjectiles.Add(spawnNetProj.bullet.id, projComp);
            }
            // This is a bullet that the other client shot, spawn it in a seperate container so that we can keep track of it
            else
            {
                Vector3 worldP = new Vector3(spawnNetProj.bullet.pos.X, spawnNetProj.bullet.pos.Y);
                var bulletGO = Instantiate(bulletPrefab, worldP, Quaternion.identity);
                var projComp = bulletGO.GetComponent<NetworkedProjectileComponent>();
                projComp.SetImpulse(new Vector2(0f, 10f));
                projComp.NetID = spawnNetProj.bullet.id;
                projComp.clientOwner = false;    // This bullet is owned by a DIFFERENT client
                projComp.netMan = this;
                otherProjectiles.Add(spawnNetProj.bullet.id, projComp);
            }

            spawn_bullet = false;
        }
        if (delete_bullet)
        {
            if (PlayerID == delProj.playerId)
            {
                if (ourProjectiles.ContainsKey(delProj.bullet.id)) {    // Always good practice
                    Destroy(ourProjectiles[delProj.bullet.id].gameObject);
                    ourProjectiles.Remove(delProj.bullet.id);
                }
            }
            else
            {
                if (otherProjectiles.ContainsKey(delProj.bullet.id)) {  // Always good practice
                    Destroy(otherProjectiles[delProj.bullet.id].gameObject);
                    otherProjectiles.Remove(delProj.bullet.id);
                }
            }

            delete_bullet = false;
        }
    }
    void SpawnAsteroid()
    {
        NetUtils.SVector2 worldP = new NetUtils.SVector2();
        worldP.X = UnityEngine.Random.Range(-5f, +5f);
        worldP.Y = +10f;

        var netAsteroid = new ClientToServer.SpawnNetAsteroid();
        netAsteroid.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_SPAWN_ASTEROID;
        netAsteroid.asteroid = new NetUtils.SAsteroid();
        netAsteroid.asteroid.pos = worldP;
        netAsteroid.asteroid.vel = new NetUtils.SVector2(0f, -5f);
        netAsteroid.netId = NetID;
        netAsteroid.playerId = PlayerID;

        SendToServer(netAsteroid);
    }
    void SpawnAndDeleteNetAsteroids()
    {
        if (spawn_asteroid)
        {
            Vector3 worldP = new Vector3(spawnNetAsteroid.asteroid.pos.X, spawnNetAsteroid.asteroid.pos.Y);
            var asteroid = Instantiate(asteroidPrefab, worldP, Quaternion.identity);
            var netAsteroid = asteroid.GetComponent<NetAsteroid>();
            netAsteroid.NetID = spawnNetAsteroid.asteroid.id;
            netAsteroid.Vel = new Vector3(spawnNetAsteroid.asteroid.vel.X, spawnNetAsteroid.asteroid.vel.Y);

            if (spawnNetAsteroid.playerId == PlayerID)
            {
                ourAsteroids.Add(spawnNetAsteroid.asteroid.id, netAsteroid);
            }
            else
            {
                otherAsteroids.Add(spawnNetAsteroid.asteroid.id, netAsteroid);
            }
            spawn_asteroid = false;
        }
        if (delete_asteroid)
        {
            // Delete our projectile
            if (delNetAsteroid.deletionFlags == NetUtils.DeletionFlags.BY_PLAYER)
            {
                if (PlayerID == delNetAsteroid.playerId)
                {
                    if (ourAsteroids.ContainsKey(delNetAsteroid.netId)) {
                        Debug.Log("(our asteroids) Deleting at key: " + delNetAsteroid.netId);
                        Destroy(ourAsteroids[delNetAsteroid.netId].gameObject);
                        ourAsteroids.Remove(delNetAsteroid.netId);
                    }
                }
                // Delete other projectile
                else
                {
                    if (otherAsteroids.ContainsKey(delNetAsteroid.netId)) {
                        Debug.Log("(other asteroids) Deleting at key: " + delNetAsteroid.netId);
                        Destroy(otherAsteroids[delNetAsteroid.netId].gameObject);
                        otherAsteroids.Remove(delNetAsteroid.netId);
                    }
                }
            }
            else if (delNetAsteroid.deletionFlags == NetUtils.DeletionFlags.BY_CLIENT)
            {
                if (ourAsteroids.ContainsKey(delNetAsteroid.netId)) {
                    Destroy(ourAsteroids[delNetAsteroid.netId].gameObject);
                    ourAsteroids.Remove(delNetAsteroid.netId);
                }
                else if (otherAsteroids.ContainsKey(delNetAsteroid.netId)) {
                    Destroy(otherAsteroids[delNetAsteroid.netId].gameObject);
                    otherAsteroids.Remove(delNetAsteroid.netId);
                }

            }
            
            
        
            delete_asteroid = false;
        }
    }
    private void AccountHandling()
    {
        if (user_authen_resp)
        {
            Debug.Log("Server authentication status: " + createAccountResponse.response);
            if (createAccountResponse.response == NetUtils.AccountResponse.CREATE_ACCOUNT_SUCCESS)
                MenuController.Instance.SetServerResponseStatus("Server response: Created account successfully!", Color.green);
            else if (createAccountResponse.response == NetUtils.AccountResponse.CREATE_ACCOUNT_USER_TAKEN)
                MenuController.Instance.SetServerResponseStatus("Server response: That username is already taken!", Color.red);
            else if (createAccountResponse.response == NetUtils.AccountResponse.WRONG_USER)
                MenuController.Instance.SetServerResponseStatus("Server response: Wrong username!", Color.red);
            else if (createAccountResponse.response == NetUtils.AccountResponse.WRONG_PASS)
                MenuController.Instance.SetServerResponseStatus("Server response: Wrong password!", Color.red);
            else if (createAccountResponse.response == NetUtils.AccountResponse.USER_ACTIVE_ON_SERVER)
                MenuController.Instance.SetServerResponseStatus("Server response: User already logged in!", Color.red);
            else if (createAccountResponse.response == NetUtils.AccountResponse.LOGIN_SUCCESS)
            {
                user_name = createAccountResponse.user;
                MenuController.Instance.ChangeGameState(GameStates.Matchmaking);          
            }
            user_authen_resp = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Majority tasks that involve modifying Mono Behaviour Objects can't be modified in OnMsgRecv()
        // Because OnMsgRecv() is a Asynchronious function
        // This may look inefficient at first, until you realize that these events only happen under the condition of
        // If match_found, if del_bullet, if del_asteroid, etc. They're only called once.
        // Majority of OnMsgRecv() tasks have to be done here since they modify Mono Behaviour objects
        HandleMatchmaking();
        DropClients();   
        UpdateClients();
        SpawnAndDeleteNetBullets();
        DeleteBulletsAndAsteroidsOutOfBounds();
        SpawnAndDeleteNetAsteroids();
        AccountHandling();
    }
}

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

        TO_CLIENT_SERVER_OFFLINE = 16,

        TO_SERVER_USER_AUTHENTICATE       = 50,
        TO_SERVER_LOGIN                   = 51,
        TO_CLIENT_CREATE_ACCOUNT_RESPONSE = 52,

        TO_CLIENT_MATCH_MADE = 100,
        TO_CLIENT_REROLL_MATCH = 101,
        TO_CLIENT_START_MATCH = 102,

        TO_SERVER_READY_UP = 150
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
        public string networkUpTime;
        public int highScore;
        public string user;
    }
    [Serializable]
    public class PlayerReadyUp : NetUtils.NetworkHeader
    {
        public int NetID;
        public int PlayerID;
        public bool isReady;
        public string user;
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
        public string networkUpTime;
        public int highScore;
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
        public bool ownedByThisClient;
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
        public bool ownedByThisClient;
        public NetUtils.SAsteroid asteroid;
    }
    [Serializable]
    public class DeleteNetAsteroid : NetUtils.NetworkHeader
    {
        public int netId;
        public int playerId;
        public NetUtils.SAsteroid asteroid;
    }
    [Serializable]
    public class CreateAccountResponse : NetUtils.NetworkHeader
    {
        public NetUtils.AccountResponse response;
        public string user;
        public string pass;
        public int highScore;
        public float timePlayed;
    }
}

public class NetManager : MonoBehaviour
{
    // Networked gameobjects
    private NetClient player1 = null;
    private NetClient player2 = null;

    // int represents Network ID
    private Dictionary<int, NetworkedProjectileComponent> netProjectiles = new Dictionary<int, NetworkedProjectileComponent>();
    private Dictionary<int, NetAsteroid> netAsteroids = new Dictionary<int, NetAsteroid>();

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
    private bool server_offline = false;
    private bool start_up_match = false;
    private float networkPlayTime = 0f;
    public float NetworkPlayTime { get { return networkPlayTime; } set { networkPlayTime = value; } }

    // Server ip and port
    public string ec2_ip = "52.14.46.199";
    public string port = "12345";
    
    public bool connect_locally = true; // for debugging, easier to test locally when debugging code
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
        MenuController.Instance.onShutdownNetwork += OnDisconnectFromServer;
        MenuController.Instance.onPlayerReadyUp += OnPlayerReadyUp;
    }
    public void InitUDPClient()
    {
        client = new UdpClient();
        
        if ( connect_locally )
            client.Connect("127.0.0.1", 5491);
        else
            client.Connect(ec2_ip, int.Parse(port));

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
            MenuController.Instance.OnConnectionTimedOut("Connection timed out.");
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
        startMatchmaking.user = "unknown";

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

    public void OnPlayerReadyUp(bool ready)
    {
        Debug.Log("player selected ready up");

        var readyUp = new ClientToServer.PlayerReadyUp();
        readyUp.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_READY_UP;
        readyUp.NetID = NetID;
        readyUp.PlayerID = PlayerID;
        readyUp.isReady = ready;
        readyUp.user = user_name;
        SendToServer(readyUp);
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
            case NetUtils.CommandSignifiers.TO_CLIENT_MATCH_MADE:
                match = LoadJson<ServerToClient.Matchmade>(bytes);
                PlayerID = match.player;
                //Debug.Log(match.message);
                match_made = true;
                break;
            case NetUtils.CommandSignifiers.TO_CLIENT_START_MATCH:
                match = LoadJson<ServerToClient.Matchmade>(bytes);

                start_up_match = true;
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
            case NetUtils.CommandSignifiers.TO_CLIENT_SERVER_OFFLINE:
                server_offline = true;
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
        delNetAsteroid.asteroid = new NetUtils.SAsteroid();
        delNetAsteroid.asteroid.id = netAsteroidComp.NetID;
        delNetAsteroid.asteroid.pos = new NetUtils.SVector2(netAsteroidComp.transform.position.x, netAsteroidComp.transform.position.y);
        delNetAsteroid.asteroid.vel = new NetUtils.SVector2(netAsteroidComp.Vel.x, netAsteroidComp.Vel.y);
        SendToServer(delNetAsteroid);
    }

    void OnApplicationQuit()
    {
        MenuController.Instance.onConnectToHost -= InitUDPClient;
        MenuController.Instance.onHostIPChanged -= OnIPChanged;
        MenuController.Instance.onHostPortChanged -= OnPortChanged;
        MenuController.Instance.onStartMatchmaking -= OnStartMatchmaking;
        MenuController.Instance.onCreateAccount -= OnCreateAccount;
        MenuController.Instance.onLogin -= OnLogin;
        MenuController.Instance.onShutdownNetwork -= OnDisconnectFromServer;

        // If playing singleplayer, were not shutting down the network
        if (!connected)
            return;

        ClientToServer.DropClient drop = new ClientToServer.DropClient();
        drop.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DROP_CLIENT;
        drop.netId = NetID;
        drop.playerId = PlayerID;
        drop.user = user_name;
        drop.networkUpTime = networkPlayTime.ToString("F1");
        drop.highScore = MenuController.Instance.HighScore;

        SendToServer(drop);
    }

    private void OnDisconnectFromServer()
    {
        ClientToServer.DropClient drop = new ClientToServer.DropClient();
        drop.commandSignifier = NetUtils.CommandSignifiers.TO_SERVER_DROP_CLIENT;
        drop.netId = NetID;
        drop.playerId = PlayerID;
        drop.user = user_name;
        SendToServer(drop);

        ShutdownNetwork();
        MenuController.Instance.SetServerResponseStatus("", Color.green);
        MenuController.Instance.ChangeGameState(GameStates.Multiplayer);    
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

            Debug.Log("Match has been made");

            MenuController.Instance.match_found_text.gameObject.SetActive(true);

            
            match_made = false;
        }
        if (start_up_match)
        {
            Time.timeScale = 1f;
            MenuController.Instance.match_found_text.gameObject.SetActive(false);
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
            
            start_up_match = false;
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

            foreach(var b in netProjectiles) {
                Destroy(b.Value.gameObject);
            }            
            foreach(var a in netAsteroids) {
                Destroy(a.Value.gameObject);
            }
            netProjectiles.Clear();
            netAsteroids.Clear();

            Debug.Log("Client left the session, returning to matchmaking menu...");

            var requestMatch = new ClientToServer.RequestMatch();
            requestMatch.commandSignifier = NetUtils.CommandSignifiers.TO_CLIENT_REROLL_MATCH;
            requestMatch.clientId = NetID;
            requestMatch.networkUpTime = networkPlayTime.ToString("F1");
            requestMatch.user = user_name;
            requestMatch.highScore = MenuController.Instance.HighScore;
            SendToServer(requestMatch);


            MenuController.Instance.ChangeGameState(GameStates.Matchmaking);
            MenuController.Instance.readyUp.isOn = false;

            MenuController.Instance.ClientLeftMatchWarningText.gameObject.SetActive(true);
            drop_client = false;
        }
    }
    void SpawnAndDeleteNetBullets()
    {
        if (spawn_bullet)
        {
            // This is our bullet, just assign the id to it
            //if (PlayerID == spawnNetProj.playerId)
            //{

            Vector3 worldP = new Vector3(spawnNetProj.bullet.pos.X, spawnNetProj.bullet.pos.Y);
            var bulletGO = Instantiate(bulletPrefab, worldP, Quaternion.identity);
            var projComp = bulletGO.GetComponent<NetworkedProjectileComponent>();
            projComp.SetImpulse(new Vector2(0f, 10f));
            projComp.NetID = spawnNetProj.bullet.id;
            projComp.clientOwner = true;    // This bullet is owned by THIS client
            projComp.netMan = this;
            projComp.OwnedByThisClient = spawnNetProj.ownedByThisClient;
            netProjectiles.Add(spawnNetProj.bullet.id, projComp);

            spawn_bullet = false;
        }
        if (delete_bullet)
        {
            if (netProjectiles.ContainsKey(delProj.bullet.id)) {    // Always good practice
                Destroy(netProjectiles[delProj.bullet.id].gameObject);
                netProjectiles.Remove(delProj.bullet.id);
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
            netAsteroid.OwnedByThisClient = spawnNetAsteroid.ownedByThisClient;
            netAsteroid.netMan = this;
            netAsteroids.Add(spawnNetAsteroid.asteroid.id, netAsteroid);

            spawn_asteroid = false;
        }
        if (delete_asteroid)
        {
            if (netAsteroids.ContainsKey(delNetAsteroid.netId)) {
                Debug.Log("(our asteroids) Deleting at key: " + delNetAsteroid.netId);
                Destroy(netAsteroids[delNetAsteroid.netId].gameObject);
                netAsteroids.Remove(delNetAsteroid.netId);
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
                MenuController.Instance.HighScore = createAccountResponse.highScore;
            }
            user_authen_resp = false;
        }
    }
    void ShutdownNetwork() {

        // All Async tasks will be turned off
        match_made       =
        update_pos       =
        drop_client      =
        spawn_bullet     =
        delete_bullet    =
        spawn_asteroid   =
        delete_asteroid  =
        user_authen_resp =
        login_Response   =
        server_offline   =
        connected        = false;

        // Destroy players, all projectiles and asteroids
        if (player1 != null && player2 != null)
        {
            Destroy(player1.gameObject);
            Destroy(player2.gameObject);
            player1 = player2 = null;
        }
        foreach (var b in netProjectiles)
        {
            Destroy(b.Value.gameObject);
        }
        foreach (var a in netAsteroids)
        {
            Destroy(a.Value.gameObject);
        }
        netProjectiles.Clear();
        netAsteroids.Clear();

        // Set Network ID's back to their default value
        NetID = -1;
        PlayerID = -1;

        // Close down the UdpClient
        client.Dispose();
    }

    void HandleConnectionLost() {
        if (server_offline) {
            ShutdownNetwork();
            MenuController.Instance.OnConnectionTimedOut("Lost connection to host.");
            MenuController.Instance.ChangeGameState(GameStates.Multiplayer);
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
        SpawnAndDeleteNetAsteroids();
        AccountHandling();
        HandleConnectionLost();
    }
}

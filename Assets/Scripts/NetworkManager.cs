using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    public string ipAddress = "127.0.0.1";
    public int portNo = 12345;

    private UdpClient clientSocket;

    private byte[] recvBuffer = new byte[1024];

    public int NetworkID = 0;

    [Serializable]
    public enum NetworkCommand
    {
        // Sending commands
        CONNECTION_REQUEST = 1,

        // Recieving commands
        RECV_NET_ID = 2,
        RECV_EXISTING_CLIENTS_IN_MATCH = 3,
        RECV_NEW_CLIENT = 4,

        // These events happen in run time because the clients are always moving, 
        // you need to update the server alot to get smooth player movement over a network!
        POSITION_UPDATE = 5,
        RECV_POSITION_UPDATE = 6,
        
        // This client wants to disconnect
        DISCONNECT_REQUEST = 7,

        // Other client requested to disconnect
        PLAYER_DISCONNECTED = 8
        
    }

    [Serializable]
    public class Command
    {
        public NetworkCommand command;
    }

    [Serializable]
    public class InitialRecv : Command
    {
        public int netID = 0;
    }

    [Serializable]
    public class ConnectionReq : Command
    {
        public string msg;
        public Pos pos;
    }

    [Serializable]
    public class Pos
    {
        public float X;
        public float Y;
    }


    [Serializable]
    public class SubClient
    {
        public int netID = 0;
        public Pos pos;
    }
    [Serializable]
    public class RecvExistingClients : Command
    {
        public SubClient[] subclients;
    }
    [Serializable]
    public class RecvNewClient : Command
    {
        public SubClient subclient;
    } 

    [Serializable]
    public class PositionUpdate : Command
    {
        public SubClient client;
    }
    [Serializable]
    public class OtherClientPositionUpdate : Command
    {
        public SubClient[] subclients;
    }
    [SerializeField]
    public class DropRequest : Command
    {
        public int netID;  
    }

    // Note the different wording here:
    [Serializable]
    public class DisconnectedPlayers : Command 
    {
        public int netID;
    }

    public List<GameObject> players;
 
    public GameObject playerPrefab;

    private ConnectionReq m_connRequest;
    private InitialRecv m_initialRecv;
    private RecvExistingClients m_recvExistingClients;
    private RecvNewClient m_recvNewClient;
    private PositionUpdate m_positionUpdate;
    private OtherClientPositionUpdate m_clientPosUpdate;
    private DropRequest m_dropReq;
    private DisconnectedPlayers m_disconnected;

    // Start is called before the first frame update
    void Start()
    {
        m_dropReq = new DropRequest();
        m_disconnected = new DisconnectedPlayers();
        m_clientPosUpdate = new OtherClientPositionUpdate();
        m_positionUpdate = new PositionUpdate();
        m_positionUpdate.command = NetworkCommand.POSITION_UPDATE;
        m_positionUpdate.client = new SubClient();

        m_connRequest = new ConnectionReq();
        
        m_initialRecv = new InitialRecv();
        m_recvNewClient = new RecvNewClient();
        m_positionUpdate = new PositionUpdate();

        clientSocket = new UdpClient();
        clientSocket.Connect(ipAddress, portNo);


        m_connRequest.command = NetworkCommand.CONNECTION_REQUEST;
        m_connRequest.pos = new Pos();
        m_connRequest.pos.X = 0f;
        m_connRequest.pos.Y = 0f;
        
        m_connRequest.msg = "Hello from client!";
        

        clientSocket.Client.Send(ASCIIEncoding.ASCII.GetBytes(JsonUtility.ToJson(m_connRequest)));

        clientSocket.Client.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnServerMessageRecv), clientSocket);


        InvokeRepeating("_Refresh", 0.5f, 3.6f);
    }
    public Text netMsg;

    public void OnServerMessageRecv(IAsyncResult AP)
    {
        print("New message from server . . .");
        int _size = clientSocket.Client.EndReceive(AP);
        if (_size <= 0)
            return;

        byte[] recData = new byte[_size];
        Buffer.BlockCopy(recvBuffer, 0, recData, 0, _size);

        string str_data = ASCIIEncoding.ASCII.GetString(recData);

        print("Message from server: " + str_data);

        // Recieving network ID for this client:
        Command netMessage = JsonUtility.FromJson<Command>(str_data);
        if (netMessage.command == NetworkCommand.RECV_NET_ID)
        {
            m_initialRecv = JsonUtility.FromJson<InitialRecv>(str_data);

            NetworkID = m_initialRecv.netID;

            print("Net ID: " + NetworkID.ToString());
        }
        else if (netMessage.command == NetworkCommand.RECV_EXISTING_CLIENTS_IN_MATCH)
        {
            //print("Here 1");
            m_recvExistingClients = JsonUtility.FromJson<RecvExistingClients>(str_data);

            connected1 = true;

            //print("Here 4");
        }
        else if (netMessage.command == NetworkCommand.RECV_NEW_CLIENT)
        {
            m_recvNewClient = JsonUtility.FromJson<RecvNewClient>(str_data);

            connected2 = true;
        }
        else if (netMessage.command == NetworkCommand.RECV_POSITION_UPDATE)
        {
            m_clientPosUpdate = JsonUtility.FromJson<OtherClientPositionUpdate>(str_data);

            for (int i = 0; i < m_clientPosUpdate.subclients.Length; i++)
            {
                // We dont want to override our own clients position (were doing that ourselves)!
                if (players[i].GetComponent<PlayerController>().NetIndex != NetworkID)
                    players[i].transform.position = new Vector3(m_clientPosUpdate.subclients[i].pos.X, m_clientPosUpdate.subclients[i].pos.Y, 0f);
            }

        }
        else if (netMessage.command == NetworkCommand.PLAYER_DISCONNECTED)
        {
            m_disconnected = JsonUtility.FromJson<DisconnectedPlayers>(str_data);

            disconnect = true;
            
        }

        clientSocket.Client.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnServerMessageRecv), clientSocket);

    }
    void _Refresh()
    {
        clientSocket.Client.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnServerMessageRecv), clientSocket);
    }
    private bool disconnect = false;
    private bool connected1 = false;
    private bool connected2 = false;
    // Update is called once per frame
    
    public void UpdateClient(Vector2 pos)
    {
        m_positionUpdate.command = NetworkCommand.POSITION_UPDATE;
        m_positionUpdate.client = new SubClient();
        m_positionUpdate.client.netID = NetworkID;
        m_positionUpdate.client.pos = new Pos();
        m_positionUpdate.client.pos.X = pos.x;
        m_positionUpdate.client.pos.Y = pos.y;
        
        string serializedData = JsonUtility.ToJson(m_positionUpdate);
        byte[] msg = ASCIIEncoding.ASCII.GetBytes(serializedData);
        clientSocket.Client.Send(msg);
        clientSocket.Client.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnServerMessageRecv), clientSocket);
    }

    private void OnApplicationQuit()
    {
        m_dropReq.command = NetworkCommand.DISCONNECT_REQUEST;
        m_dropReq.netID = NetworkID;

        string serializedData = JsonUtility.ToJson(m_dropReq);
        byte[] msg = ASCIIEncoding.ASCII.GetBytes(serializedData);
        clientSocket.Client.Send(msg);
        clientSocket.Client.BeginReceive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnServerMessageRecv), clientSocket);
    }

    void Update()
    {
        if (disconnect)
        {
            for (int i = 0; i < players.Count; i++)
            {
                // This is how we identify which client wanted to drop the server
                if (players[i].GetComponent<PlayerController>().NetIndex == m_disconnected.netID)
                {
                    Destroy(players[i]);
                    players.RemoveAt(i);
                    break;
                }
            }
            disconnect = false;
        }

        if (connected1)
        {
            print("Spawning new client . . .");
            foreach (SubClient client in m_recvExistingClients.subclients)
            {
                float randX = UnityEngine.Random.Range(-3f, +3f);
                float randY = UnityEngine.Random.Range(-3f, +3f);

                Vector3 randomPoint = new Vector3(randX, randY, 0f);

                GameObject go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                players.Add(go);
                players[players.Count - 1].GetComponent<PlayerController>().NetIndex = client.netID;

            }
            for (int i = 0; i < players.Count; i++)
            {
                print("Player net ID: " + players[i].GetComponent<PlayerController>().NetIndex + ", position -> " + players[i].transform.position.ToString());
            }

            connected1 = false;
        }
        if (connected2)
        {
            float randX = UnityEngine.Random.Range(-3f, +3f);
            float randY = UnityEngine.Random.Range(-3f, +3f);

            Vector3 randomPoint = new Vector3(randX, randY, 0f);
            
            if (m_recvNewClient != null)
            {

                GameObject go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                players.Add(go);
                players[players.Count - 1].GetComponent<PlayerController>().NetIndex = m_recvNewClient.subclient.netID;
            }
            for (int i = 0; i < players.Count; i++)
            {
                print("Player net ID: " + players[i].GetComponent<PlayerController>().NetIndex + ", position -> " + players[i].transform.position.ToString());
            }

            connected2 = false;
        }
    }
}

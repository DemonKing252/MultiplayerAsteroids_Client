using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameStates
{
    MainMenu,
    SinglePlayer,
    Multiplayer,
    UserAuthentication,
    Matchmaking,
    WaitingForMatch,
    MatchmakingGame

}


public class MenuController : MonoBehaviour
{
    [SerializeField] private Button singleplayer;
    [SerializeField] private Button multiplayer;
    [SerializeField] private Button quit;

    [SerializeField] private Button connect;

    [SerializeField] private Button start_matchmaking;
    [SerializeField] private Button disconnectBtn;

    [SerializeField] private Toggle readyUp;    // maybe


    [SerializeField] private Button createAccountBtn;
    [SerializeField] private Button loginBtn;

    // Canvases
    [SerializeField] private Canvas inGameUI;
    [SerializeField] private Canvas mainMenu;
    [SerializeField] private Canvas connectToHost;
    [SerializeField] private Canvas matchmaking;
    [SerializeField] private Canvas waitingForMatch;
    [SerializeField] private Canvas userAuthenticationMenu;


    // Extras: text boxes for connect to host
    [SerializeField] private TMPro.TMP_InputField hostIPField;
    [SerializeField] private TMPro.TMP_InputField hostPortField;
    [SerializeField] private TMPro.TMP_InputField userNameField;
    [SerializeField] private TMPro.TMP_InputField passwordField;
    [SerializeField] private TMPro.TMP_Text connectionTimedOutText;
    [SerializeField] private TMPro.TMP_Text clientLeftMatchWarningText;
    [SerializeField] private TMPro.TMP_Text server_response_status_text;
    public TMPro.TMP_Text ClientLeftMatchWarningText => clientLeftMatchWarningText;

    private GameStates gameState = GameStates.MainMenu;
    public GameStates GameState => gameState;

    private string user, password;
    private static MenuController instance = null;
    public static MenuController Instance => instance;

    // Delegates go here.
    public delegate void OnReadyUpEvent();    
    public delegate void OnHostIPChangedEvent(string ip);
    public delegate void OnHostPortChangedEvent(string port);
    public delegate void OnStartmatchmakingEvent();
    public delegate void OnCreateAccountEvent(string user, string pass);
    public delegate void OnLoginEvent(string user, string pass);
    public delegate void OnShutdownNetwork();

    public event OnReadyUpEvent onConnectToHost;
    public event OnHostIPChangedEvent onHostIPChanged;
    public event OnHostPortChangedEvent onHostPortChanged;
    public event OnStartmatchmakingEvent onStartMatchmaking;
    public event OnCreateAccountEvent onCreateAccount;
    public event OnLoginEvent onLogin;
    public event OnShutdownNetwork onShutdownNetwork;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Debug.LogError("ERROR: you have two or more menu controllers in the scene!");
    }

    // Start is called before the first frame update
    void Start()
    {
        singleplayer.onClick.AddListener( () => OnSinglePlayer() );
        multiplayer.onClick.AddListener( () => OnMultiplayer() );
        quit.onClick.AddListener( () => OnQuit() );
        connect.onClick.AddListener( () => OnConnect() );
        start_matchmaking.onClick.AddListener( () => OnStartMatchmaking() );
        readyUp.onValueChanged.AddListener( (value) => OnReadyUp(value) );
        hostIPField.onValueChanged.AddListener( (ip) => OnHostIPChanged(ip) );
        hostPortField.onValueChanged.AddListener( (port) => OnHostPortChanged(port) );

        userNameField.onValueChanged.AddListener( (user) => OnUsernameChanged(user) );
        passwordField.onValueChanged.AddListener( (pass) => OnPasswordChanged(pass) );

        createAccountBtn.onClick.AddListener(() => OnCreateAccount());
        loginBtn.onClick.AddListener(() => OnLogin());
        disconnectBtn.onClick.AddListener(() => onShutdownNetwork?.Invoke());

        ChangeGameState(GameStates.MainMenu);
    }
    // ------------------------------------------------------------------
    // Event handlers
    public void OnSinglePlayer()
    {
        // Nothing yet
    }
    public void OnMultiplayer()
    {
        ChangeGameState(GameStates.Multiplayer);
    }
    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }
    public void OnConnect()
    {
        Time.timeScale = 0f;
        connectionTimedOutText.gameObject.SetActive(false);
        onConnectToHost?.Invoke();
    }
    public void OnReadyUp(bool ready)
    {
        if (ready)
        {
            // Code here.
            Time.timeScale = 1f;
            ChangeGameState(GameStates.MatchmakingGame);
        }
    }
    public void OnHostIPChanged(string ip)
    {
        onHostIPChanged?.Invoke(ip);
    }
    public void OnHostPortChanged(string port)
    {
        onHostPortChanged?.Invoke(port);
    }

    public void OnUsernameChanged(string user)
    {
        this.user = user;
    }
    public void OnPasswordChanged(string pass)
    {
        this.password = pass;
    }
    public void OnCreateAccount()
    {
        onCreateAccount?.Invoke(user, password);
    }
    public void OnLogin()
    {
        onLogin?.Invoke(user, password);
    }
    public void SetServerResponseStatus(string status, Color col)
    {
        server_response_status_text.text = "<color=" + (col == Color.red ? "red" : "green") + ">" + status + "</color>";

    }

    public void OnStartMatchmaking()
    {
        clientLeftMatchWarningText.gameObject.SetActive(false);
        onStartMatchmaking?.Invoke();
    }

    // ------------------------------------------------------------------

    public void ChangeGameState(GameStates state)
    {
        gameState = state;

        inGameUI.gameObject.SetActive(false);
        mainMenu.gameObject.SetActive(false);
        connectToHost.gameObject.SetActive(false);
        matchmaking.gameObject.SetActive(false);
        waitingForMatch.gameObject.SetActive(false);
        userAuthenticationMenu.gameObject.SetActive(false);

        Canvas turnOn = state switch
        {
            GameStates.MainMenu        => mainMenu,
            GameStates.SinglePlayer    => inGameUI,
            GameStates.Multiplayer     => connectToHost,
            GameStates.UserAuthentication => userAuthenticationMenu,
            GameStates.Matchmaking     => matchmaking,
            GameStates.WaitingForMatch => waitingForMatch,
            GameStates.MatchmakingGame => inGameUI,
            _ => throw new System.Exception("Error: Unknown game state!")
        };

        turnOn.gameObject.SetActive(true);

    }
    public void OnConnectionTimedOut(string text)
    {
        connectionTimedOutText.gameObject.SetActive(true);
    }
}

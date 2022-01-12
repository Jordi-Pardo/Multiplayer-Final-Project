using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PUNManager : MonoBehaviourPunCallbacks
{

    private string gameVersion = "game1";
    private bool isConnecting;
    private bool checkActivation = false;
    private System.DateTime exitTime=System.DateTime.Now;
    private bool exit = false;

    public GameObject player1;
    public GameObject player2;
    public GameObject connecting;

    void Awake()
    {
            DontDestroyOnLoad(this);
            PhotonNetwork.AutomaticallySyncScene = true;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (checkActivation)
        {
            if (player1 == null || player2 == null)
            {
                checkActivation = false;
                return;
            }
            if (!PhotonNetwork.InRoom)
                return;

            if(PhotonNetwork.CurrentRoom.PlayerCount>1)
            {
                player2.SetActive(true);
            }
            connecting.SetActive(false);
            player1.SetActive(true);
        }
        if (exit && exitTime <= System.DateTime.Now)
        {
            ExitRoomAndGoToLobby();
        }
    }

    public void Connect()
    {
        checkActivation=true;
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
            Debug.Log("Connected");
        }
        else
        {
            isConnecting = PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
            Debug.Log("Not Connected");
        }
    }

    public override void OnConnectedToMaster()
    {
        if (isConnecting)
        {
            PhotonNetwork.JoinRandomRoom();
            Debug.Log("ConnectedToMaster");
        }
        isConnecting = false;
    }


    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        isConnecting = false;
        Debug.LogWarning(cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Failed");

        // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
        PhotonNetwork.CreateRoom(null, new Photon.Realtime.RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Entered a room");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient: new player");
            PhotonNetwork.LoadLevel(1);
        }
        else
        {
            Debug.Log("New player");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount);
        //PhotonNetwork.SetMasterClient(photonView)
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient: one less player");
            if (player1 == null && player2 == null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                GameObject winImage = GameObject.FindGameObjectWithTag("Holder").GetComponent<GameObjectHolder>().holder;
                winImage.SetActive(true);
                exitTime = System.DateTime.Now.AddSeconds(3);
                exit = true;
            }
            else
            {
                ExitRoomAndGoToLobby();
            }
        }
        else
        {
            Debug.Log("One less player");
        }
    }

    public void ExitRoomAndGoToLobby()
    {
        PhotonNetwork.LoadLevel(0);
        PhotonNetwork.LeaveRoom();
        Destroy(this.gameObject);
    }

    public override void OnLeftRoom()
    {
        Debug.LogWarning("Room left");
    }

}

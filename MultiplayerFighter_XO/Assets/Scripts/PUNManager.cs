using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PUNManager : MonoBehaviourPunCallbacks
{

    private string gameVersion = "game1";
    private bool isConnecting;
    private bool notDuplicated = true;

    void Awake()
    {
        if (FindObjectsOfType<PUNManager>().Length > 1)
        {
            Destroy(this.gameObject);
            notDuplicated = false;
        }
        else
        {
            DontDestroyOnLoad(this);
            PhotonNetwork.AutomaticallySyncScene = true;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Connect();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Connect()
    {
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
            PhotonNetwork.LoadLevel(0);
        }
        else
        {
            Debug.Log("One less player");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.LogWarning("Room left");
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading;

public class NewServer : MonoBehaviour
{

    [Header("UI Manager")]
    public GameObject playername1;
    public GameObject playername2;


    private List<EndPoint> guests; //keeps track of the connections
    private List<int> unconfirmedGuests;
    private List<int> waitingGuests;
    private List<int> guestsDisconnecting;
    private List<Action> actions = new List<Action>();
    private List<TextWithID> textsToSend = new List<TextWithID>();
    private List<TextWithID> backupTexts = new List<TextWithID>();
    private Dictionary<int, uint> listOfMessagesReceived = new Dictionary<int, uint>();
    private Dictionary<int, List<uint>> listOfMessagesNeeded = new Dictionary<int, List<uint>>();
    private Dictionary<InfoOfBackupMessages, string> backupOfMessagesSent = new Dictionary<InfoOfBackupMessages, string>();
    private object actionLock;
    private object guestLock;
    private object backupLock;
    private object textLock;
    private object losesJitterLock;
    private object maxPlayersLock;
    private object confirmedGuestsLock;
    private Thread serverListenThread;
    private Thread serverSendThread;
    private Socket server;
    private bool startPlayed = false;
    private bool disconnectedItself = true;
    private DateTime timerDisconnection;
    public int maxTimeout = 5000;

    [Space(10)]
    public int maxPlayers;
    private bool morePlayersAllowed = true;
    public int port = 6162; //default port
    public GameObject packetLoss;
    public GameObject jitter;
    public TMP_InputField lossThreshold;
    public TMP_InputField minJitt;
    public TMP_InputField maxJitt;
    private bool _packetLoss = false;
    private bool _jitter = false;
    private int _lossThreshold = 0;
    private int _minJit = 0;
    private int _maxJit = 0;
    public int capMessagesNeeded = 20;

    private bool serverconnected; //server started or not

    public class TextWithID
    {
        public string text;
        public int id;
        public DateTime timeToSendMessage;
        public bool jitterApplied;
        public TextWithID(string t, int i)
        {
            text = t;
            id = i;
            timeToSendMessage = DateTime.Now;
            jitterApplied = false;
        }
    }

    public class InfoOfBackupMessages
    {
        public uint messageID;
        public int senderID;
        public int getterID;

        public InfoOfBackupMessages(uint messageId, int senderId, int getterId)
        {
            messageID = messageId;
            senderID = senderId;
            getterID = getterId;
        }
    }

    private void Start()
    {
        actionLock = new object();
        guestLock = new object();
        textLock = new object();
        backupLock = new object();
        losesJitterLock = new object();
        confirmedGuestsLock = new object();
        maxPlayersLock = new object();

        guests = new List<EndPoint>();
        unconfirmedGuests = new List<int>();
        waitingGuests = new List<int>();
        waitingGuests.Add(-2);
        guestsDisconnecting = new List<int>();

        server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverListenThread = new Thread(ServerListenThread);
        serverSendThread = new Thread(ServerSendThread);

        serverconnected = true;

        serverListenThread.Start();
        serverSendThread.Start();

    }
    private void Update()
    {
        if (!serverconnected)
        {
            return;
        }

        lock (losesJitterLock)
        {
            _packetLoss = packetLoss.GetComponent<Toggle>().isOn;
            _jitter = jitter.GetComponent<Toggle>().isOn;
            _minJit = Int32.Parse(minJitt.text);
            _maxJit = Int32.Parse(maxJitt.text);
            _lossThreshold = Int32.Parse(lossThreshold.text);
        }

        lock (actionLock)
        {
            //we clean the list of ongoing actions 
            while (actions.Count > 0)
            {
                Action action = actions[0];
                actions.RemoveAt(0);
                action();
            }
        }
        lock (maxPlayersLock)
        {
            if (morePlayersAllowed || unconfirmedGuests.Count != 0)
            {
                lock (confirmedGuestsLock)
                {
                    for(int i = 0; i < unconfirmedGuests.Count; i++) {
                        SendConnectionMessage(unconfirmedGuests[i], false);
                    }
                }
            }
            else if(!startPlayed)
            {
                lock (confirmedGuestsLock)
                {
                    for(int i = 0; i < maxPlayers; i++)
                    {
                        if (waitingGuests.Contains(i))
                        {
                            SendConnectionMessage(i, true);
                        }
                    }
                }
            }
        }

        
    }


    void ServerListenThread()
    {
        IPEndPoint ipServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        server.Bind(ipServer);

        byte[] buffer = new byte[1000];
        EndPoint clientPoint = new IPEndPoint(IPAddress.Any, 0);


        Debug.Log("Server has started on port " + port.ToString());

        while (true)
        {
            //we make the connection with the client and it sends a message, we decode it and if its "ping" the message we proceed to make an output
            int size = server.ReceiveFrom(buffer, ref clientPoint);
            MessageClass messageReceived = new MessageClass(Encoding.ASCII.GetString(buffer));
            int id;
            lock (guestLock)
            {
                id = guests.FindIndex(client => client.Equals(clientPoint));
            }
            bool checkIfThereAreMessagesLost = true;
            switch (messageReceived.typeOfMessage)
            {
                case MessageClass.TYPEOFMESSAGE.Connection:
                    {
                        if (morePlayersAllowed && id == -1)
                        {
                            List<EndPoint> localClients;
                            lock (guestLock)
                            {
                                guests.Add(clientPoint);
                                id = guests.Count;
                                localClients = new List<EndPoint>(guests);
                            }
                            lock (unconfirmedGuests)
                            {
                                unconfirmedGuests.Add(id-1);
                            }
                            if (id-- >= maxPlayers)
                            {
                                lock (maxPlayersLock)
                                {
                                    morePlayersAllowed = false;
                                }
                            }

                            if (id== 0)
                            {
                                lock (actionLock)
                                {
                                    actions.Add(() => playername1.SetActive(true));
                                }
                            }
                            else if(id == 1)
                            {
                                lock (actionLock)
                                {
                                    actions.Add(() => playername2.SetActive(true));
                                }
                            }
                            Debug.Log("NEwwW CLIENT");
                            SendConnectionMessage(id, false);
                            
                        }
                        checkIfThereAreMessagesLost = false;


                        break;
                    }
                case MessageClass.TYPEOFMESSAGE.Input:
                    {
                        List<EndPoint> localClients;
                        lock (guestLock)
                        {
                            localClients = new List<EndPoint>(guests);
                        }
                        for (int i = 0; i < localClients.Count; i++)
                        {
                            if (i == id)
                            {
                                continue;
                            }
                            Vector3 move= new Vector3(0,0,0);
                            if (messageReceived.input == MessageClass.INPUT.Move)
                            {
                                move = messageReceived.position;
                            }
                            lock (textLock)
                            {
                                MessageClass message = new MessageClass(messageReceived.id, id, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now, messageReceived.input, move);
                                textsToSend.Add(new TextWithID(message.Serialize(), i));
                            }
                        }
                        localClients.Clear();
                        break;
                    }
                case MessageClass.TYPEOFMESSAGE.Disconnection:
                    List<EndPoint> localsClients;
                    lock (guestLock)
                    {
                        localsClients = guests;
                    }
                    if (id > -1 && !guestsDisconnecting.Contains(id))
                    {
                        guestsDisconnecting.Add(id);
                        if (id == 0)
                        {
                            lock (actionLock)
                            {
                                actions.Add(() => playername1.SetActive(false));
                            }
                        }
                        else if (id == 1)
                        {
                            lock (actionLock)
                            {
                                actions.Add(() => playername2.SetActive(false));
                            }
                        }
                    }
                    for (int i = 0; i < localsClients.Count; i++)
                    {
                        lock (textLock)
                        {
                            MessageClass message = new MessageClass(messageReceived.id, messageReceived.playerID, MessageClass.TYPEOFMESSAGE.Disconnection, DateTime.Now);
                            textsToSend.Add(new TextWithID(message.Serialize(), i));
                        }
                    }
                    localsClients.Clear();
                    checkIfThereAreMessagesLost = false;
                    break;
                case MessageClass.TYPEOFMESSAGE.Acknowledgment:
                    {
                        checkIfThereAreMessagesLost = false;
                        lock (confirmedGuestsLock)
                        {
                            if (waitingGuests.Contains(id) && id != messageReceived.playerID)
                            {
                                waitingGuests.Remove(id);
                                if (waitingGuests.Count == 0)
                                {
                                    startPlayed = true;
                                }
                            }
                            if (unconfirmedGuests.Contains(id))
                            {
                                unconfirmedGuests.Remove(id);
                                if (unconfirmedGuests.Count == 0 && !morePlayersAllowed)
                                {
                                    waitingGuests.Remove(-2);
                                    for (int i = 0; i < maxPlayers; i++)
                                    {
                                        waitingGuests.Add(i);
                                    }
                                }
                            }
                            
                        }
                        Dictionary<InfoOfBackupMessages, string> backupOfTheBackup;
                        lock (backupLock)
                        {
                            backupOfTheBackup = new Dictionary<InfoOfBackupMessages, string>(backupOfMessagesSent);
                        }
                        List<InfoOfBackupMessages> messagesToDelete = new List<InfoOfBackupMessages>();
                        foreach (var backMessage in backupOfTheBackup)
                        {
                            if (backMessage.Key.getterID != id)
                                continue;
                            if (backMessage.Key.senderID != messageReceived.playerID)
                                continue;
                            if (backMessage.Key.messageID > messageReceived.id)
                                continue;

                            if (messageReceived.messagesLostInBetween == false || messageReceived.id == backMessage.Key.messageID)
                            {
                                messagesToDelete.Add(backMessage.Key);
                            }
                            else
                            {
                                lock (textLock)
                                {
                                    textsToSend.Add(new TextWithID(backMessage.Value, id));
                                }
                            }
                        }
                        foreach (var messageDeleting in messagesToDelete)
                        {
                            lock (backupLock)
                            {
                                backupOfMessagesSent.Remove(messageDeleting);
                            }
                        }
                        break;
                    }
                case MessageClass.TYPEOFMESSAGE.MessagesNeeded:
                    {
                        checkIfThereAreMessagesLost = false;
                        for(int i = 0; i < maxPlayers; i++)
                        {
                            if (messageReceived.messagesNeeded.ContainsKey(i) && messageReceived.messagesNeeded[i].Count > capMessagesNeeded)
                            {
                                SendDisconnectionMessage(id);
                            }
                        }
                        Dictionary<InfoOfBackupMessages, string> backupOfTheBackup;
                        lock (backupLock)
                        {
                            backupOfTheBackup = new Dictionary<InfoOfBackupMessages, string>(backupOfMessagesSent);
                        }
                        foreach (var backMessage in backupOfTheBackup)
                        {
                            if (backMessage.Key.getterID != id)
                                continue;
                            if (!messageReceived.messagesNeeded.ContainsKey(backMessage.Key.senderID))
                                continue;
                            if (messageReceived.messagesNeeded[backMessage.Key.senderID].Contains(backMessage.Key.messageID))
                            {
                                lock (textLock)
                                {
                                    textsToSend.Add(new TextWithID(backMessage.Value,id));
                                }
                            }
                        }
                    break;
                    }
            }
            int index = messageReceived.playerID;
            List<MessageClass> newMessages = MessageClass.CheckIfThereAreMessagesLost(ref listOfMessagesReceived, ref listOfMessagesNeeded, messageReceived, index, checkIfThereAreMessagesLost);
            for (int i = 0; newMessages!=null && i < newMessages.Count; i++)
            {
                lock (textLock)
                {
                    textsToSend.Add(new TextWithID(newMessages[i].Serialize(),id));
                }
            }
                Debug.Log("Server from Player "+id+": " + Encoding.ASCII.GetString(buffer));


        }
    }

    void ServerSendThread()
    {
        List<TextWithID> localTexts;
        List<EndPoint> localClients;
        System.Random r = new System.Random();
        byte[] buffer;

        while (true)
        {
            lock (textLock)
            {
                localTexts = new List<TextWithID>(textsToSend);
                textsToSend.Clear();
            }
            if (backupTexts.Count > 0)
            {
                localTexts.AddRange(backupTexts);
                backupTexts.Clear();
            }
            lock (guestLock)
            {
                localClients = new List<EndPoint>(guests);                
            }


            for (int i = 0; i < localTexts.Count; i++)
            {
                //HERE WE WILL WORK WITH PACKET LOSS AND JITTER
                if (!localTexts[i].jitterApplied)
                {
                    MessageClass.TYPEOFMESSAGE type = (MessageClass.TYPEOFMESSAGE)int.Parse(localTexts[i].text.Split('#')[2]);
                    if (type != MessageClass.TYPEOFMESSAGE.Acknowledgment && type != MessageClass.TYPEOFMESSAGE.MessagesNeeded)
                    {
                        uint id = uint.Parse(localTexts[i].text.Split('#')[0]);
                        int senderID = int.Parse(localTexts[i].text.Split('#')[1]);
                        InfoOfBackupMessages info= new InfoOfBackupMessages(id,senderID,localTexts[i].id);
                        lock (backupLock)
                        {
                            if (!backupOfMessagesSent.ContainsKey(info))
                            {
                                backupOfMessagesSent.Add(info, localTexts[i].text);
                            }
                        }
                    }

                    bool pL, j;
                    int plT, mJ, mxJ;

                    lock(losesJitterLock)
                    {
                        pL = _packetLoss;
                        j = _jitter;
                        mJ = _minJit;
                        mxJ = _maxJit;
                        plT = _lossThreshold;
                    }

                    //FIRST PACKET LOSS
                    if (pL && r.Next(0, 100) <= plT)
                    {
                        if(type!=MessageClass.TYPEOFMESSAGE.Acknowledgment)
                            Debug.LogWarning("Message Lost by server: " + localTexts[i].text);
                        continue;
                    }
                    //THEN JITTER
                    if (j)
                    {
                        localTexts[i].timeToSendMessage = DateTime.Now.AddMilliseconds(r.Next(mJ,mxJ));
                    }
                    localTexts[i].jitterApplied = true;
                }
                if (localTexts[i].timeToSendMessage > DateTime.Now)
                {
                    backupTexts.Add(localTexts[i]);
                    continue;
                }
                buffer = new byte[1000];
                buffer = Encoding.ASCII.GetBytes(localTexts[i].text);
                server.SendTo(buffer, buffer.Length, SocketFlags.None, localClients[localTexts[i].id]);
            }
            localTexts.Clear();
            localClients.Clear();
        }

    }

    public void SendConnectionMessage(int playerID, bool multipleMessages)
    {
        Vector3 pos;
        if (playerID == 0)
        {
            pos = new Vector3(7, 0, -19);

        }
        else
        {
            pos = new Vector3(17, 0, -19);
        }

        MessageClass message = new MessageClass(0, playerID, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now, pos);
        lock (textLock)
        {
            textsToSend.Add(new TextWithID(message.Serialize(), playerID));
        }

        if (!multipleMessages)
            return;
        lock (guestLock)
        {
            for (int i = 0; i < guests.Count; i++)
            {
                if (i == 0)
                {
                    pos = new Vector3(17, 0, -19);

                }
                else
                {
                    pos = new Vector3(7, 0, -19);
                }

                message = new MessageClass(0, i == 0 ? 1 : 0, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now, pos);
                lock (textLock)
                {
                    textsToSend.Add(new TextWithID(message.Serialize(), i));
                }
            }
        }
    }

    public void SendDisconnectionMessage(int clientID)
    {
        MessageClass message = new MessageClass(0, -2, MessageClass.TYPEOFMESSAGE.Disconnection, DateTime.Now);
        lock (textLock)
        {
            textsToSend.Add(new TextWithID(message.Serialize(), clientID));
        }
        Thread.Sleep(50);
    }

    private void OnDestroy()
    {
        if (disconnectedItself)
        {
            int initialClients;
            int localClientsToDisconnect;
            DateTime disconnectionTime = DateTime.Now.AddMilliseconds(maxTimeout);

            lock (guestLock)
            {
                initialClients = guests.Count;
            }

            localClientsToDisconnect = initialClients-guestsDisconnecting.Count;
            
            while (localClientsToDisconnect > 0 && disconnectionTime >= DateTime.Now)
            {
                for(int i = 0; i < initialClients; i++)
                {
                    SendDisconnectionMessage(i);
                }

                localClientsToDisconnect = initialClients - guestsDisconnecting.Count;
                
            }
            
        }
        serverListenThread.Abort();
        serverSendThread.Abort();
        server.Close();
    }
}
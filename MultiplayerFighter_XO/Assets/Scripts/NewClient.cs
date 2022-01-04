using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Text;
using System.Threading;
using UnityEngine.UI;

public class NewClient : MonoBehaviour
{
    private IPEndPoint ipDestination;
    private EndPoint serverPoint;
    private Socket socket;
    private List<MessageWithPossibleJitter> textsToSend = new List<MessageWithPossibleJitter>();
    private List<MessageWithPossibleJitter> backupTexts = new List<MessageWithPossibleJitter>();
    private Dictionary<int, uint> listOfMessagesReceived = new Dictionary<int, uint>();
    private Dictionary<int, List<uint>> listOfMessagesNeeded = new Dictionary<int, List<uint>>();
    private Dictionary<uint, string> backupOfMessagesSent = new Dictionary<uint, string>();
    private List<Action> actions = new List<Action>();
    private object actionLock;
    private object backupLock;
    private object textLock;
    private object disconnectionLock;
    private object messageReceivedLock;
    private Thread clientListenThread;
    private Thread clientSendThread;
    private StreamWriter writter;
    private StreamReader reader;
    private bool firstMessageSent = false;
    private bool firstMessageReceived = false;
    private bool resentMessage = false;
    private bool connected = false;
    private uint messageID = 0;
    private DateTime connectionTimer;
    private bool disconnectItself = true;
    private bool disconnectionAcknowledged = false;
    public int clientID = -1;
    public int maxTimeout = 5000;

    public bool packetLoss = false;
    public bool jitter = false;
    public int lossThreshold = 90;
    public int minJitt = 0;
    public int maxJitt = 800;
    public Dictionary<int, Vector3> positionsDic = new Dictionary<int, Vector3>();

    public static Action<int> onConnectionReceived;

    public List<CharacterScript> characterScripts;


    public class MessageWithPossibleJitter
    {
        public string text;
        public DateTime timeToSendMessage;
        public bool jitterApplied;
        public MessageWithPossibleJitter(string t)
        {
            text = t;
            timeToSendMessage = DateTime.Now;
            jitterApplied = false;
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        actionLock = new object();
        textLock = new object();
        backupLock = new object();
        messageReceivedLock = new object();
        disconnectionLock = new object();
        MessageClass message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now, new Vector3(0,0,0));
        textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
    }
    public void ConnectToServer()
    {
        if (connected)
        {
            return;
        }
        connected = true;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        ipDestination = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6162);
        serverPoint = new IPEndPoint(IPAddress.Any, 0);
        clientListenThread = new Thread(ClientListenThread);
        clientSendThread = new Thread(ClientSendThread);

        clientSendThread.Start();
        clientListenThread.Start();
        connectionTimer = DateTime.Now.AddMilliseconds(100);
    }

    private void Update()
    {
        lock (actionLock)
        {
            while (actions.Count > 0)
            {
                Action action = actions[0];
                actions.RemoveAt(0);
                action();
            }
        }
        lock (messageReceivedLock)
        {
            if(!firstMessageReceived && DateTime.Now > connectionTimer)
            {
                MessageClass message = new MessageClass(0, clientID, MessageClass.TYPEOFMESSAGE.Connection, DateTime.Now, new Vector3(0, 0, 0));
                lock (textLock)
                {
                    textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
                }
                connectionTimer = DateTime.Now.AddMilliseconds(100);
            }
        }
    }

    void ClientSendThread()
    {
        byte[] buffer;
        List<MessageWithPossibleJitter> localTexts;
        System.Random r = new System.Random();

        while (true)
        {
            lock (textLock)
            {
                localTexts = new List<MessageWithPossibleJitter>(textsToSend);
                textsToSend.Clear();
            }
            if (backupTexts.Count > 0)
            {
                localTexts.AddRange(backupTexts);
                backupTexts.Clear();
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
                        lock (backupLock)
                        {
                            if (!backupOfMessagesSent.ContainsKey(id))
                            {
                                backupOfMessagesSent.Add(id, localTexts[i].text);
                            }
                        }
                    }
                    //FIRST PACKET LOSS
                    if (packetLoss && r.Next(0, 100) <= lossThreshold)
                    {
                        Debug.LogWarning("Message Lost by client: " + clientID);
                        continue;
                    }
                    //THEN JITTER
                    if (jitter)
                    {
                        localTexts[i].timeToSendMessage = DateTime.Now.AddMilliseconds(r.Next(minJitt, maxJitt));

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
                socket.SendTo(buffer, buffer.Length, SocketFlags.None, ipDestination);
                firstMessageSent = true;
                Debug.LogWarning("Message sent");
            }
            localTexts.Clear();
        }
    }

    void ClientListenThread()
    {
        byte[] buffer = new byte[1000];
        while (true)
        {
            if (firstMessageSent)
            {
                socket.ReceiveFrom(buffer, ref serverPoint);
                lock (messageReceivedLock)
                {
                    firstMessageReceived = true;
                }
                MessageClass messageReceived = new MessageClass(Encoding.ASCII.GetString(buffer));
                bool checkIfThereAreMessagesLost = true;
                switch (messageReceived.typeOfMessage)
                {
                    case MessageClass.TYPEOFMESSAGE.Input:
                        foreach (var character in characterScripts)
                        {
                            if (character.ID != messageReceived.playerID)
                                continue;

                            if (messageReceived.input == MessageClass.INPUT.Attack)
                            {
                                character.ToAttacK();
                            }

                            if (messageReceived.input == MessageClass.INPUT.Block)
                            {
                                character.ToBlock();
                            }

                            if (messageReceived.input == MessageClass.INPUT.Move)
                            {
                                character.ToWalk(messageReceived.position);
                            }

                            if(messageReceived.input == MessageClass.INPUT.Idle)
                            {
                                character.ToIdle();
                            }

                            if(messageReceived.input == MessageClass.INPUT.KnockBack)
                            {
                                character.ToKnockBack();
                            }
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.Connection:
                        if (clientID == -1)
                        {
                            lock (backupLock)
                            {
                                backupOfMessagesSent.Remove(0);
                            }
                            clientID = messageReceived.playerID;
                        }
                        lock (actionLock)
                        {
                            actions.Add(() => onConnectionReceived?.Invoke(messageReceived.playerID));
                        }
                        if (!positionsDic.ContainsKey(messageReceived.playerID))
                        {
                            positionsDic.Add(messageReceived.playerID, messageReceived.position);
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.Acknowledgment:
                        checkIfThereAreMessagesLost = false;
                        Dictionary<uint, string> backupOfTheBackup;
                        lock (backupLock)
                        {
                            backupOfTheBackup = new Dictionary<uint, string>(backupOfMessagesSent);
                        }
                        List<uint> messagesToDelete = new List<uint>();
                        foreach(var backMessage in backupOfTheBackup)
                        {
                            if (backMessage.Key > messageReceived.id)
                                break;
                            if (messageReceived.messagesLostInBetween == false || messageReceived.id==backMessage.Key)
                            {
                                messagesToDelete.Add(backMessage.Key);
                            }
                            else
                            {
                                lock (textLock)
                                {
                                    textsToSend.Add(new MessageWithPossibleJitter(backMessage.Value));
                                }
                            }
                        }
                        foreach(var messageDeleting in messagesToDelete)
                        {
                            lock (backupLock)
                            {
                                backupOfMessagesSent.Remove(messageDeleting);
                            }
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.MessagesNeeded:
                        checkIfThereAreMessagesLost = false;
                        lock (backupLock)
                        {
                            foreach (var backMessage in backupOfMessagesSent)
                            {
                                if (messageReceived.messagesNeeded[clientID].Contains(backMessage.Key))
                                {
                                    lock (textLock)
                                    {
                                        textsToSend.Add(new MessageWithPossibleJitter(backMessage.Value));
                                    }
                                }
                            }
                        }
                        break;
                    case MessageClass.TYPEOFMESSAGE.Disconnection:
                        if (messageReceived.playerID == -2)
                        {
                            disconnectItself = false;
                            lock (actionLock)
                            {
                                actions.Add(() => Quit());
                            }
                        }
                        else if (messageReceived.playerID == clientID)
                        {
                            lock (disconnectionLock)
                            {
                                disconnectionAcknowledged = true;
                            }
                        }
                        else
                        {
                            lock (actionLock)
                            {
                                actions.Add(() => CharacterScript.onFinishGame?.Invoke(true));
                            }
                        }
                        break;
                }
                Debug.LogWarning(messageReceived.typeOfMessage + ";   ");

                int index = messageReceived.playerID;
                List<MessageClass> newMessages= MessageClass.CheckIfThereAreMessagesLost(ref listOfMessagesReceived, ref listOfMessagesNeeded, messageReceived, index,checkIfThereAreMessagesLost, clientID);
                for(int i = 0; newMessages != null && i < newMessages.Count; i++)
                {
                    lock (textLock)
                    {
                        textsToSend.Add(new MessageWithPossibleJitter(newMessages[i].Serialize()));
                    }
                }
                if (messageReceived.typeOfMessage != MessageClass.TYPEOFMESSAGE.Acknowledgment)

                    Debug.Log("Player " + clientID + ": " + Encoding.ASCII.GetString(buffer));
            }
        }
    }


    public void SendInputMessageToServer(MessageClass.INPUT messageInput, bool sendPos = false, float x = 0, float y = 0, float z = 0)
    {
        MessageClass message;
        if (sendPos)
        {
            message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now, messageInput,new Vector3(x,y,z));
        }
        else
        {

            message = new MessageClass(messageID++, clientID, MessageClass.TYPEOFMESSAGE.Input, DateTime.Now, messageInput);
        }
        lock (textLock)
        {
            textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
        }
    }


    public void Quit()
    {
        Application.Quit();
    }

    public void SendDisconnectionMessage()
    {
        MessageClass message = new MessageClass(0, clientID, MessageClass.TYPEOFMESSAGE.Disconnection, DateTime.Now);
        lock (textLock)
        {
            textsToSend.Add(new MessageWithPossibleJitter(message.Serialize()));
        }
        Thread.Sleep(50);
    }

    private void OnDestroy()
    {
        if (disconnectItself && clientID > -1)
        {
            DateTime disconnectionTime = DateTime.Now.AddMilliseconds(maxTimeout);
            bool acknowledgment;
            lock (disconnectionLock)
            {
                acknowledgment = disconnectionAcknowledged;
            }
            while (!acknowledgment && disconnectionTime>=DateTime.Now)
            {
                lock (disconnectionLock)
                {
                    acknowledgment = disconnectionAcknowledged;
                }
                SendDisconnectionMessage();
            }
        }
        clientSendThread.Abort();
        clientListenThread.Abort();
        socket.Close();
    }

}

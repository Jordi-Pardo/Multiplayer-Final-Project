using System.Collections.Generic;
using UnityEngine;
public class MessageClass
{
    public enum TYPEOFMESSAGE
    {
        Acknowledgment,
        Input,
        Connection,
        Disconnection,
        CharacterUpdate,
        WorldUpdate,
        MessagesNeeded
    }

    public enum INPUT
    {
        Move,
        Attack,
        Block,
        Idle,
        KnockBack
    }

    //public enum OBJECTUPDATE
    //{
    //    Appeared,
    //    Destroyed
    //}

    public uint id;
    public int playerID;
    public TYPEOFMESSAGE typeOfMessage;
    public INPUT input;
    public int objectID;
    public System.DateTime time;
    public Dictionary<int, List<uint>> messagesNeeded;
    public bool messagesLostInBetween;
    public Vector3 position;
    //public OBJECTUPDATE objectUpdate;

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time=t;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, INPUT inp)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        input = inp;
    }
    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, INPUT inp, Vector3 pos)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        input = inp;
        position = pos;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, bool lost)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        messagesLostInBetween = lost;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, int oi/*, OBJECTUPDATE ou*/)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        objectID = oi;
        //objectUpdate = ou;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, Dictionary<int, List<uint>> needed)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        messagesNeeded = needed;
    }

    public MessageClass(uint i, int pi, TYPEOFMESSAGE type, System.DateTime t, Vector3 pos)
    {
        id = i;
        playerID = pi;
        typeOfMessage = type;
        time = t;
        position = pos;
    }

    public MessageClass(string str)
    {
        str = str.TrimEnd('\0');
        string[] info= str.Split('#');
        id = uint.Parse(info[0]);
        playerID = int.Parse(info[1]);
        typeOfMessage = (TYPEOFMESSAGE)int.Parse(info[2]);
        System.DateTime.Parse(info[3]);
        switch (typeOfMessage)
        {
            case TYPEOFMESSAGE.Input:
                input = (INPUT)int.Parse(info[4]);
                if(input == INPUT.Move)
                {
                    string[] move = info[5].Split(';');
                    position = new Vector3(float.Parse(move[0]), float.Parse(move[1]), float.Parse(move[2]));
                }
                break;
            case TYPEOFMESSAGE.Connection:
                string[] pos = info[4].Split(';');
                position = new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
                break;
            case TYPEOFMESSAGE.WorldUpdate:
                objectID = int.Parse(info[4]);
                break;
            case TYPEOFMESSAGE.Acknowledgment:
                messagesLostInBetween = bool.Parse(info[4]);
                break;
            case TYPEOFMESSAGE.MessagesNeeded:
                string[] numbers = info[4].Split(';');
                string[] specificNumbers;
                messagesNeeded = new Dictionary<int, List<uint>>();
                for(int i = 0; i < numbers.Length; i++)
                {
                    specificNumbers = numbers[i].Split(',');
                    List<uint> ids = new List<uint>();
                    for(int j = 1;  j < specificNumbers.Length; j++)
                    {
                        ids.Add(uint.Parse(specificNumbers[j]));
                    }
                    messagesNeeded.Add(int.Parse(specificNumbers[0]), ids);
                }
                break;
            default:
                break;
        }
    }

    public string Serialize()
    {
        string info;
        switch (typeOfMessage)
        {
            case TYPEOFMESSAGE.Input:
                info = '#' + input.ToString("d");
                if(input == INPUT.Move)
                {
                    info += '#' + position.x.ToString() + ';' + position.y.ToString() + ';' + position.z.ToString();
                }
                break;
            case TYPEOFMESSAGE.WorldUpdate:
                info = '#' + objectID.ToString();
                break;
            case TYPEOFMESSAGE.Connection:
                info = '#' + position.x.ToString() + ';' + position.y.ToString() + ';' + position.z.ToString();
                break;
            case TYPEOFMESSAGE.Acknowledgment:
                info = '#' + messagesLostInBetween.ToString();
                break;
            case TYPEOFMESSAGE.MessagesNeeded:
                bool firstNumber = true;
                string numbers="";
                foreach(var number in messagesNeeded)
                {
                    if (number.Value.Count <= 0)
                    {
                        continue;
                    }
                    if (!firstNumber)
                    {
                        numbers += ';';
                    }
                    numbers += number.Key;
                    foreach(var ids in number.Value)
                    {
                        numbers += ',';
                        numbers += ids;
                    }
                    firstNumber = false;
                }
                info = '#' + numbers;
                break;
            default:
                info = "";
                break;
        }
        return id.ToString() + '#' + playerID.ToString() + '#' + typeOfMessage.ToString("d") + '#' + time.ToString() + info + '#';
    }

    public static List<MessageClass> CheckIfThereAreMessagesLost(ref Dictionary<int, uint> listOfMessages, ref Dictionary<int, List<uint>> fullListOfMessagesLost, MessageClass message, int index, bool sendMessage, int clientID=0)
    {
        uint lastMessageID;
        if (!fullListOfMessagesLost.ContainsKey(index))
        {
            fullListOfMessagesLost.Add(index, new List<uint>());
        }
        List<uint> listOfMessagesLost = fullListOfMessagesLost[index];
        uint idMessage = message.id;
        bool thereAreMessagesLost = false;
        List<MessageClass> messagesToSend = new List<MessageClass>();


        if (!listOfMessages.ContainsKey(index))
        {
            listOfMessages.Add(index, idMessage);

            uint firstMessageExpected = 0;

            if (clientID > index)
            {
                firstMessageExpected = 1;
            }

            if (idMessage > firstMessageExpected)
            {
                bool enteredFor = false;
                for (uint i = firstMessageExpected; i < idMessage; i++)
                {
                    listOfMessagesLost.Add(i);
                    enteredFor = true;
                }
                if (enteredFor)
                {
                    fullListOfMessagesLost[index] = listOfMessagesLost;
                }
            }
        }
        else if (clientID !=index)
        {
            lastMessageID = listOfMessages[index];
            if (idMessage < lastMessageID && listOfMessagesLost.Contains(idMessage))
            {
                listOfMessagesLost.Remove(idMessage);
                fullListOfMessagesLost[index] = listOfMessagesLost;
            }
            else if (idMessage > lastMessageID)
            {
                listOfMessages[index] = idMessage;
                bool enteredFor = false;
                for(uint i = lastMessageID + 1; i < idMessage; i++)
                {
                    if (!listOfMessagesLost.Contains(i))
                    {
                        listOfMessagesLost.Add(i);
                        enteredFor = true;
                    }
                }
                if (enteredFor)
                {
                    fullListOfMessagesLost[index] = listOfMessagesLost;
                }
            }
        }

        foreach (var lists in fullListOfMessagesLost)
        {
            if (lists.Value.Count > 0)
            {
                thereAreMessagesLost = true;

            }
        }  
        

        if (!sendMessage)
            return null;
        MessageClass messageToSend;
        if (thereAreMessagesLost)
        {
            messageToSend = new MessageClass(idMessage, message.playerID, MessageClass.TYPEOFMESSAGE.MessagesNeeded, System.DateTime.Now, fullListOfMessagesLost);
            messagesToSend.Add(messageToSend);
            Debug.LogWarning(listOfMessagesLost.ToString());
        }
        messageToSend = new MessageClass(idMessage, message.playerID, MessageClass.TYPEOFMESSAGE.Acknowledgment, System.DateTime.Now, thereAreMessagesLost);
        messagesToSend.Add(messageToSend);

        return messagesToSend;
    }
}

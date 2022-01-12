using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public interface IDamageable
{
    public void RecieveDamage();
}

[RequireComponent(typeof(PhotonView))]
public class Destructible : MonoBehaviour,IDamageable
{
    public void RecieveDamage()
    {
        PhotonView photonView = PhotonView.Get(this);
        photonView.RPC("DestroyObject", RpcTarget.All);
    }

    [PunRPC]
    public void DestroyObject()
    {
        Destroy(gameObject,0.2f);
    }
}

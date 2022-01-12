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
    public float dissolveSpeed = 1;
    public void RecieveDamage()
    {
        PhotonView photonView = PhotonView.Get(this);
        photonView.RPC("DestroyObject", RpcTarget.All);
    }

    [PunRPC]
    public void DestroyObject()
    {
        StartCoroutine(Dissolve());
    }

    public IEnumerator Dissolve()
    {
        float value = 0;

        Material material = GetComponent<MeshRenderer>().material;

        while (value < 1)
        {
            value = Mathf.MoveTowards(value, 1, Time.deltaTime * dissolveSpeed);
            material.SetFloat("_Float", value);
            yield return null;
        }

        Destroy(gameObject);


    }

        
}

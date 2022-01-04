using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    public void RecieveDamage();
}

public class Destructible : MonoBehaviour,IDamageable
{
    public void RecieveDamage()
    {

            Destroy(gameObject);
       
    }

}

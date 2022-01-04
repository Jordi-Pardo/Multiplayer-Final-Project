using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{
    public int ID;
    public static Action<CharacterScript> onReceiveDamage;
    public static Action<bool> onFinishGame;
    public Animator animator;
    public CharacterController controller;
    public float speed;
    private Vector3 dir;
    public int health;
    private bool beingHit = false;
    private bool attack = false;
    private bool blocking = false;
    private bool toAttack = false;
    private bool toWalk = false;
    private bool toBlock = false;
    private bool toKnockBack = false;
    private bool idle = true;
    private Vector3 toWalkVector;
    private Vector3 lastVectorRecieved;

    [Header("Attack Info")]
    public Transform castDamagePoint;
    public float hitRadius;
    public NewClient client;
    public bool canMove = true;
    public bool startBlocking = false;
    private object attackLock;
    private object walkLock;
    private object idleLock; 
    private object blockLock;
    private object knockBackLock;
    enum STATE
    {
        SEARCH_STATE,
        IDLE,
        WALK,
        HIT,
        ATTACK
    }
    enum INPUT_STATE
    {
        IN_IDLE,
        IN_IDLE_END,
        IN_WALK,
        IN_WALK_END,
        IN_HIT,
        IN_HIT_END,
        IN_ATTACK
    }

    private STATE currentState = STATE.IDLE;
    private List<INPUT_STATE> inputList = new List<INPUT_STATE>();



    // Start is called before the first frame update
    void Start()
    {
        attackLock = new object();
        walkLock = new object();
        idleLock = new object();
        blockLock = new object();
        knockBackLock = new object();
    }

    // Update is called once per frame
    void Update()
    {

        ProcessInternalInput();
        ProcessExternalInput();

        ProcessState();
        UpdateState();

        Debug.Log(currentState.ToString());
        Debug.Log(beingHit.ToString());

    }

    void ProcessInternalInput()
    {

        //Send messages to client
        //A = Input.GetAxisRaw("Horizontal") < 0 ? true : false;
        //D = Input.GetAxisRaw("Horizontal") > 0 ? true : false;

        //Debug.Log($"Pressed A: {A} Pressed D: {D}");
        //if (!A && !D)
        //{

        //    //client.SendInputMessageToServer(MessageClass.INPUT.Idle);

        //    inputList.Add(INPUT_STATE.IN_IDLE);
        //}
        //else if(!attack)
        //{
        //    if (A)
        //    {
        //        //client.SendInputMessageToServer(MessageClass.INPUT.A);
        //        //Walk(MessageClass.INPUT.A);
        //    }
        //    else if (D)
        //    {
        //        //Walk(MessageClass.INPUT.D);
        //        //client.SendInputMessageToServer(MessageClass.INPUT.D);

        //    }
        //    //inputList.Add(INPUT_STATE.IN_WALK);
        //}
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (client.clientID == ID)
            {
                Attack();
                client.SendInputMessageToServer(MessageClass.INPUT.Attack);
            }
        }
        if (client.clientID == ID)
        {
            bool nowBlocking = blocking;
            blocking = Input.GetKey(KeyCode.B);
            if (nowBlocking != blocking)
            {
                if(blocking)
                    client.SendInputMessageToServer(MessageClass.INPUT.Block);
                else
                    client.SendInputMessageToServer(MessageClass.INPUT.Idle);
            }
        }
        //animator.SetBool("Blocking", blocking);
        if (client.clientID == ID)
        {
            //Local
            dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, 0f);
            dir = dir.normalized;
            if (dir == Vector3.zero) inputList.Add(INPUT_STATE.IN_IDLE);
            else inputList.Add(INPUT_STATE.IN_WALK);
        }
        //Send position
        lock (attackLock)
        {
            if (toAttack)
            {
                blocking = false;
                toAttack = false;
                Attack();
            }
        }

        lock (blockLock)
        {
            if (toBlock)
            {
                toBlock = false;
                blocking = true;
            }
        }
        lock (walkLock)
        {
            if (toWalk)
            {
                blocking = false;
                toWalk = false;
                toWalkVector = Vector3.Lerp(toWalkVector, lastVectorRecieved, Time.deltaTime * (lastVectorRecieved.magnitude/toWalkVector.magnitude)*5);
                Walk(toWalkVector);
                Debug.Log("To Walk");
            }
        }
        lock (idleLock)
        {
            if (idle)
            {
                blocking = false;
                idle = false;
                animator.SetInteger("DIR", 0);
                Debug.Log("TO IDLE");
            }
        }

        lock (knockBackLock)
        {
            if (toKnockBack)
            {
                toKnockBack = false;
                //StartCoroutine(PushedBack(this));
                this.ReceiveDamage();
            }
        }

        animator.SetBool("Blocking", blocking);

    }

    void ProcessExternalInput()
    {
        if (Input.GetKeyDown(KeyCode.Y)/* || attack*/)
        {
            //inputList.Add(INPUT_STATE.IN_HIT);
            //beingHit = true;
            //if (!attack)
            //{
            //    client.SendInputMessageToServer(MessageClass.INPUT.Attack);
            //}
            //attack = false;
            //client.SendInputMessageToServer(MessageClass.INPUT.Attack);
        }
        //else if (attack)
        //{
        //    inputList.Add(INPUT_STATE.IN_HIT);
        //    beingHit = true;
        //    attack = false;
        //    Debug.Log("ATACK YES!!!!!!!!!!!!!!!!!!");
        //}
        if (beingHit && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.8f)
        {
            //inputList.Add(INPUT_STATE.IN_IDLE);
            beingHit = false;
        }
        if(attack &&animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9)
        {
            attack = false;
        }
    }

    void ProcessState()
    {
        while (inputList.Count > 0)
        {
            INPUT_STATE _input = inputList[0];

            switch (currentState)
            {
                case STATE.IDLE:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_WALK:
                            currentState = STATE.WALK;
                            break;
                        case INPUT_STATE.IN_HIT:
                            currentState = STATE.HIT;
                            break;
                        case INPUT_STATE.IN_ATTACK:
                            currentState = STATE.ATTACK;
                            break;
                    }
                    break;
                case STATE.WALK:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            client.SendInputMessageToServer(MessageClass.INPUT.Idle);
                            break;
                        case INPUT_STATE.IN_HIT:
                            currentState = STATE.HIT;
                            break;
                        case INPUT_STATE.IN_ATTACK:
                            currentState = STATE.ATTACK;
                            break;
                    }
                    break;
                case STATE.HIT:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_IDLE:
                            currentState = STATE.IDLE;
                            client.SendInputMessageToServer(MessageClass.INPUT.Idle);
                            break;
                    }
                    break;
                case STATE.ATTACK:
                    switch (_input)
                    {
                        case INPUT_STATE.IN_IDLE:
                            if (attack)
                            {
                                return;
                            }
                            currentState = STATE.IDLE;
                            client.SendInputMessageToServer(MessageClass.INPUT.Idle);
                            break;
                    }
                    break;
            }
            inputList.RemoveAt(0);
        }
    }

    void UpdateState()
    {
        switch (currentState)
        {
            
            case STATE.IDLE:
                animator.SetInteger("DIR", 0);
                //animator.Play("Fighting Idle");
                //animator.SetBool("A", false);
                //animator.SetBool("D", false);
                break;
            case STATE.WALK:
                if (client.clientID == ID)
                {
                    Vector3 desiredPos = transform.position + (dir.normalized * speed * Time.deltaTime);
                    Walk(desiredPos);
                    client.SendInputMessageToServer(MessageClass.INPUT.Move, true, desiredPos.x, desiredPos.y, desiredPos.z);
                    
                }

                break;
            case STATE.HIT:
                animator.Play("Hit Reaction");
                break;
            case STATE.ATTACK:

                inputList.Add(INPUT_STATE.IN_IDLE);
                break;
        }
    }

    public void Attack()
    {
        attack = true;
        Debug.LogWarning(System.DateTime.Now.Millisecond);
        //animator.SetTrigger("Punch");
        inputList.Add(INPUT_STATE.IN_ATTACK);
        animator.SetTrigger("Punch");
    }

    public void Walk(Vector3 desiredPos)
    {
        if (controller.transform.position.x > desiredPos.x)
        {
            animator.SetInteger("DIR", 1);
        }
        else
        {
            animator.SetInteger("DIR", -1);

        }
        controller.transform.position = desiredPos;
        //if(input == MessageClass.INPUT.A)
        //{
        //    dir = new Vector3(-1, 0, 0);
        //}
        //else if (input == MessageClass.INPUT.D)
        //{
        //    dir = new Vector3(1, 0, 0);
        //}
    }

    public void ToAttacK()
    {
        lock (attackLock)
        {
            toAttack = true;
        }
    }

    public void ToWalk(Vector3 vector)
    {
        lock (walkLock)
        {
            toWalk = true;
            inputList.Add(INPUT_STATE.IN_WALK);
            toWalkVector = lastVectorRecieved = vector;
        }
    }
    public void ToBlock()
    {
        lock (blockLock)
        {
            toBlock = true;
        }
    }
    public void ToIdle()
    {
        lock (idleLock)
        {
            idle = true;
        }
    }

    public void ToKnockBack()
    {
        lock (knockBackLock)
        {
            toKnockBack = true;
        }
    }

    public void ReceiveDamage()
    {
        int damage = 50;
        if (blocking)
        {
            animator.SetTrigger("Blocked");
            damage = 5;
        }
        health -= damage;
        if(health <= 0)
        {
            health = 0;
            Debug.Log("Died");
            animator.SetTrigger("Die");
            animator.applyRootMotion = true;

            //Finish game
            if (ID == client.clientID)
            {
                onFinishGame?.Invoke(false);
            }
            else
            {
                onFinishGame?.Invoke(true);
            }


        }
        animator.SetTrigger("HeadHit");

        onReceiveDamage?.Invoke(this);
    }

    public void CheckDamage()
    {
        Collider[] colliders = Physics.OverlapSphere(castDamagePoint.position, hitRadius);

        foreach (Collider c in colliders)
        {
            if(c.gameObject.TryGetComponent(out CharacterScript character))
            {
                //if (client.clientID == ID)
                //{
                    character.ReceiveDamage();
                    Debug.Log("Hitted");
                    //StartCoroutine(PushedBack(character));
                    //client.SendInputMessageToServer(MessageClass.INPUT.KnockBack);
                //}
            }

            if(c.gameObject.TryGetComponent(out IDamageable damageable))
            {
                damageable.RecieveDamage();
            }
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(castDamagePoint.position,hitRadius);
    }

    IEnumerator PushedBack(CharacterScript character)
    {
        float time = 0.05f;
        float impulse = 0.1f;
        while(time > 0)
        {
            time -= Time.deltaTime;
            character.controller.Move(transform.forward * impulse);
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out IDamageable damageable))
        {
            damageable.RecieveDamage();
        }
    }
}

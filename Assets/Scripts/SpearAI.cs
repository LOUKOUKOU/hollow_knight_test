using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpearAI : MonoBehaviour
{
    private delegate void postTurnEventCallbackType();
    private postTurnEventCallbackType postTurnEventCallback = null;
    private Animator _animator;
    private Rigidbody _rb;

    // Magic numbers
    private const int MAX_PATROL = 10;
    private const int MAX_WAIT = 5;
    private const int MAX_ATTACK = 5;
    private const int MAX_ATTACKED = 0;
    private const int MAX_BLOCK = 5;

    private int _direction = 1;

    private int patrolCount = 0; // How long it patrols
    private int waitCount = 0; // How long it waits between patrolling
    private int blockCount = 0; // How long it blocks before resuming patrol
    private int attackedCount = 0; // How many times it can be attacked before before blocking

    private int attackCooldown = 0; // How long before it can attack again

    private bool suspendUpdater = false; // Suspends updating while animations are playing out.

    public bool walking { get; private set; } = false;
    public bool blocking { get; private set; } = false;

    private float update;

    private void Awake()
    {

        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        patrolCount = MAX_PATROL;
        waitCount = MAX_WAIT;
        attackedCount = MAX_ATTACKED;
        toggleWalk();
    }

    private void Update()
    {
        update += Time.deltaTime;
        if (update > 0.24f)
        {
            update = 0.0f;
            if (!suspendUpdater)
            {
                if (blockCount <= 0)
                {
                    if (patrolCount <= 0)
                    {
                        if (walking)
                        {
                            toggleWalk();
                        }
                        if (blocking)
                        {
                            toggleBlock();
                        }

                        if (waitCount <= 0)
                        {
                            suspendUpdater = true;
                            turn();
                            postTurnEventCallback = () =>
                            {
                                patrolCount = MAX_PATROL;
                                waitCount = MAX_WAIT;
                                if (!walking)
                                {
                                    toggleWalk();
                                }

                                suspendUpdater = false;
                            };
                        }
                        else
                        {
                            waitCount--;
                        }
                    }
                    else
                    {
                        patrolCount--;
                        Vector3 temp = transform.localPosition;
                    }

                    if (attackCooldown > 0)
                    {
                        attackCooldown--;
                    }
                } else
                {
                    blockCount--;
                }
            } 
        }
    }

    private bool isAttackAnimating
    {
        get
        {
            return _animator.GetCurrentAnimatorStateInfo(0).IsTag("Attacking");
        }
    }

    public void attack()
    {
        if (attackCooldown <= 0)
        {
            if (walking)
            {
                toggleWalk();
            }
            suspendUpdater = true;
            _animator.SetTrigger("Attack");
            attackCooldown = MAX_ATTACK;
            _rb.velocity = new Vector3(0, 0, 0);
        }
    }

    public void turn()
    {
        _direction = _direction > 0 ? -1 : 1;
        _animator.SetTrigger("Turn");
    }

    public void attacked() {
        _animator.SetTrigger("Attacked");
        if (!blocking)
        {
            if (attackedCount <= 0)
            {
                if (walking)
                {
                    toggleWalk();
                }
                toggleBlock();
                attackedCount = MAX_ATTACKED;
            } else
            {
                attackedCount--;
            }
        } else
        {
            blockCount = MAX_BLOCK;
        }
    }

    public void postTurnEvent()
    {
        Vector3 temp = transform.localPosition;
        transform.localScale = new Vector3(_direction, transform.localScale.y, transform.localScale.z);
        transform.localPosition = new Vector3(temp.x+((float)(1.279)*_direction), temp.y, temp.z);
        if(postTurnEventCallback != null)
        {
            postTurnEventCallback();
            postTurnEventCallback = null;
        }
    }

    public void preAttackEvent()
    {
        _rb.velocity = new Vector3(15*_direction, 0, 0);
    }

    public void postAttackEvent()
    {
        _rb.velocity = new Vector3(0, 0, 0);
    }

    public void postAttackWinddownEvent()
    {
        suspendUpdater = false;
    }

    public void toggleWalk()
    {
        if (!blocking)
        {
            walking = !walking;
            _rb.velocity = new Vector3(walking?5*_direction:0, 0, 0);
            _animator.SetBool("Walking", walking);
        }
    }

    public void toggleBlock()
    {
        if (!walking && !isAttackAnimating)
        {
            blocking = !blocking;
            blockCount = blocking?MAX_BLOCK:0;
            _animator.SetBool("Blocking", blocking);
        }
    }
}

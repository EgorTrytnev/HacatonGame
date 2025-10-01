using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private float _hor;
    private float _ver;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator animator;
    private SpawnDetector spawnDetector;
    [SerializeField]private CollorTeam collorTeam;
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnDetector = GetComponent<SpawnDetector>();

    }

    void Update()
    {
        PlayerMove();
        PlayerActions();
        
    }

    void PlayerMove()
    {
        _hor = Input.GetAxisRaw("Horizontal");
        _ver = Input.GetAxisRaw("Vertical");

        if (_ver != 0 || _hor != 0)
        {
            if (_hor == -1)
                _spriteRenderer.flipX = true;
            else if (_hor == 1)
                _spriteRenderer.flipX = false;

            animator.SetBool("isRun", true);
        }
        else
            animator.SetBool("isRun", false);

        Vector2 newDir = new Vector2(_hor, _ver).normalized;

        _rb.velocity = newDir * speed;
    }

    void PlayerActions()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (spawnDetector.getSpawnAllowed())
            {
                spawnDetector.SpawnMob(collorTeam, "Zomby");
            }
            else
            {
                Debug.Log("Out of Spawner");
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            spawnDetector.FollowMe("Zomby");
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            spawnDetector.StopFollowMe("Zomby");
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            spawnDetector.AtackEnemy("Zomby");
        }

    }


}
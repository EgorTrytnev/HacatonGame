using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] private float speed = 5f;
    private float _hor;
    private float _ver;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator animator;
    private SpawnDetector spawnDetector;
    [SerializeField] private CollorTeam collorTeam;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnDetector = GetComponent<SpawnDetector>();
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
        PlayerMove();
        PlayerActions();
    }

    void PlayerMove()
    {
        _hor = Input.GetAxisRaw("Horizontal");
        _ver = Input.GetAxisRaw("Vertical");

        bool moving = _ver != 0 || _hor != 0;
        if (moving)
        {
            if (_hor == -1) _spriteRenderer.flipX = true;
            else if (_hor == 1) _spriteRenderer.flipX = false;
        }
        animator.SetBool("isRun", moving);

        Vector2 newDir = new Vector2(_hor, _ver).normalized;
        _rb.linearVelocity = newDir * speed; // Rigidbody2D sync делаем через PhotonRigidbody2DView
    }

    void PlayerActions()
    {
        if (!photonView.IsMine) return;
        var sdPv = spawnDetector ? spawnDetector.GetComponent<PhotonView>() : null;
        if (sdPv == null) return;

        if (Input.GetKeyDown(KeyCode.E) && spawnDetector.getSpawnAllowed())
        {
            int teamId = (int)collorTeam;
            sdPv.RPC("RPC_SpawnMob", RpcTarget.MasterClient, teamId, "Zomby", transform.position);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            spawnDetector.CmdFollowMe("Zomby", photonView.ViewID);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            spawnDetector.CmdStopFollow("Zomby");
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            spawnDetector.CmdAttackEnemy("Zomby");
        }
    }
}

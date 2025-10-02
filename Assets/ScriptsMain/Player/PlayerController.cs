using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Модифицированный PlayerController с интегрированной голосовой системой
/// Управляет персонажем и его юнитами через голос и клавиатуру
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(VoiceSystemManager))]
public class PlayerController : MonoBehaviourPun
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private CollorTeam collorTeam;

    private float _hor;
    private float _ver;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator animator;
    private SpawnDetector spawnDetector;
    private VoiceSystemManager voiceManager;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnDetector = GetComponent<SpawnDetector>();
        voiceManager = GetComponent<VoiceSystemManager>();

        // Подписываемся на события спавна юнитов для голосовой системы
        if (photonView.IsMine && PhotonNetwork.IsConnected)
        {
            spawnDetector.OnUnitSpawned += OnUnitSpawned;
            spawnDetector.OnUnitDestroyed += OnUnitDestroyed;
        }
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
        _rb.linearVelocity = newDir * speed;
    }

    void PlayerActions()
    {
        if (!photonView.IsMine) return;

        var sdPv = spawnDetector?.GetComponent<PhotonView>();
        if (sdPv == null) return;

        // Спавн юнита (клавиша E)
        if (Input.GetKeyDown(KeyCode.E) && spawnDetector.getSpawnAllowed())
        {
            int teamId = (int)collorTeam;
            sdPv.RPC("RPC_SpawnMob", RpcTarget.MasterClient, teamId, "Zomby", transform.position);
        }

        // Команды через клавиатуру (альтернатива голосу)
        if (Input.GetKeyDown(KeyCode.F))
        {
            spawnDetector.CmdFollowMe("Zomby", photonView.ViewID);
            Debug.Log("🎮 Команда клавиатурой: Следовать");
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            spawnDetector.CmdStopFollow("Zomby");
            Debug.Log("🎮 Команда клавиатурой: Стоп");
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            spawnDetector.CmdAttackEnemy("Zomby");
            Debug.Log("🎮 Команда клавиатурой: Атака");
        }

        // Голосовая активация (клавиша V)
        if (Input.GetKeyDown(KeyCode.V))
        {
            ActivateVoiceListening();
        }
    }

    /// <summary>
    /// Активация прослушивания голосовых команд
    /// </summary>
    void ActivateVoiceListening()
    {
        if (voiceManager != null)
        {
            Debug.Log("🎙️ Активирован голосовой режим");
            // Можно добавить визуальную индикацию активного режима
        }
    }

    /// <summary>
    /// Обработка события спавна нового юнита
    /// </summary>
    void OnUnitSpawned(GameObject unit)
    {
        if (!photonView.IsMine) return;

        var unitFSM = unit.GetComponent<UnitFSM>();
        var unitPhotonView = unit.GetComponent<PhotonView>();
        
        if (unitFSM != null && unitPhotonView != null && voiceManager != null)
        {
            // Регистрируем юнит в голосовой системе
            voiceManager.RegisterUnit(unitFSM.unitId, unitPhotonView.ViewID);
            Debug.Log($"🎤 Юнит {unitFSM.unitId} зарегистрирован в голосовой системе");
        }
    }

    /// <summary>
    /// Обработка события уничтожения юнита
    /// </summary>
    void OnUnitDestroyed(GameObject unit)
    {
        if (!photonView.IsMine) return;

        var unitFSM = unit.GetComponent<UnitFSM>();
        
        if (unitFSM != null && voiceManager != null)
        {
            // Удаляем юнит из голосовой системы
            voiceManager.UnregisterUnit(unitFSM.unitId);
            Debug.Log($"🎤 Юнит {unitFSM.unitId} удален из голосовой системы");
        }
    }

    void OnDestroy()
    {
        if (spawnDetector != null)
        {
            spawnDetector.OnUnitSpawned -= OnUnitSpawned;
            spawnDetector.OnUnitDestroyed -= OnUnitDestroyed;
        }
    }
}
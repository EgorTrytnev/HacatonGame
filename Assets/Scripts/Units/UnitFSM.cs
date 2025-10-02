using UnityEngine;

public class UnitFSM : MonoBehaviour
{
    [Header("Аудио ответы")]
    public AudioClip[] responseClips;

    private AudioSource _audioSource;
    [HideInInspector] public string unitId;
    private BaseState currentState;
    private static int _nextId = 1;
    private bool _nameLogged = false;

    void Awake()
    {
        unitId = "Unit_" + _nextId++;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        // Защита от null
        if (VoiceSystemManager.Instance == null)
        {
            Debug.LogError("VoiceSystemManager не найден! Юнит не зарегистрирован.");
            return;
        }

        VoiceSystemManager.Instance.RegisterUnit(unitId);
        VoiceCommandBroadcaster.OnCommandReceived += OnVoiceCommand;
        TransitionTo(new IdleState(this));
    }

    void Update()
    {
        currentState?.OnUpdate();

        // Логируем имя ОДИН РАЗ, когда оно становится доступно
        if (!_nameLogged)
        {
            string myName = VoiceSystemManager.Instance.GetUnitName(unitId);
            if (!string.IsNullOrEmpty(myName))
            {
                Debug.Log($"✅ Я — {myName} (ID: {unitId})");
                _nameLogged = true;
            }
        }

        // Проверка ожидания
        bool isAwaiting = VoiceSystemManager.Instance.GetAwaitingUnits().Contains(unitId);
        if (isAwaiting && !(currentState is AwaitingCommandState))
        {
            TransitionTo(new AwaitingCommandState(this));
        }
    }

    void OnDisable()
    {
        VoiceCommandBroadcaster.OnCommandReceived -= OnVoiceCommand;
        currentState?.OnExit();
    }

    public void TransitionTo(BaseState newState)
    {
        currentState?.OnExit();
        currentState = newState;
        currentState?.OnEnter();
    }

    void OnVoiceCommand(string targetId, string[] actions)
    {
        if (targetId != unitId) return;

        // Сбрасываем ожидание при получении команды
        VoiceSystemManager.Instance.SetAwaitingCommand(unitId, false);

        foreach (string action in actions)
        {
            switch (action)
            {
                case "GoToBase":
                    GoToPoint("Point_Base");
                    break;
                case "GoToMid":
                    GoToPoint("Point_Mid");
                    break;
                case "GoToLair":
                    GoToPoint("Point_Lair");
                    break;
            }
        }
    }

    void GoToPoint(string pointName)
    {
        GameObject point = GameObject.Find(pointName);
        if (point == null)
        {
            Debug.LogError($"❌ Точка '{pointName}' не найдена!");
            return;
        }
        TransitionTo(new MoveToPointState(this, point.transform.position, pointName));
    }

    public void PlayRandomResponse()
    {
        if (responseClips == null || responseClips.Length == 0)
        {
            Debug.LogWarning("Нет аудио для ответа у юнита " + unitId);
            return;
        }

        AudioClip clip = responseClips[Random.Range(0, responseClips.Length)];
        _audioSource.PlayOneShot(clip);
    }
}
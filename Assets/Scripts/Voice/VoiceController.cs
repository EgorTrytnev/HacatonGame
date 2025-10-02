using System;
using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Локальный VoiceController для каждого игрока
/// Заменяет глобальный Singleton на локальный экземпляр
/// </summary>
public class VoiceController : MonoBehaviour
{
    [DllImport("trr_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void StartTranscription(string vadModelPath, string asrModelPath);

    [DllImport("trr_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void StopTranscription();

    [DllImport("trr_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool HasTranscribedText();

    [DllImport("trr_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetNextTranscribedText();

    [DllImport("trr_lib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FreeText(IntPtr text);

    public event Action<string> OnCommandRecognized;

    [Header("Настройки распознавания")]
    public bool autoStartListening = true;
    public bool debugMode = true;
    public float minSilenceDuration = 1.0f;

    private bool isListening = false;
    private string vadModelPath;
    private string asrModelPath;
    private float lastRecognitionTime;
    private int playerId;

    // Статический счетчик для уникальных ID контроллеров
    private static int _controllerIdCounter = 0;

    void Awake()
    {
        playerId = _controllerIdCounter++;
        
        // Добавляем ID игрока к имени объекта для отладки
        gameObject.name = $"LocalVoiceController_Player{playerId}";
    }

    void Start()
    {
        vadModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "silero_vad.onnx");
        asrModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "asr_model.onnx");

        if (autoStartListening)
        {
            StartListening();
        }
    }

    void Update()
    {
        if (!isListening) return;

        ProcessTranscribedText();
    }

    /// <summary>
    /// Начать прослушивание голоса
    /// </summary>
    public void StartListening()
    {
        if (isListening) return;

        try
        {
            StartTranscription(vadModelPath, asrModelPath);
            isListening = true;
            
            if (debugMode)
                Debug.Log($"🎙️ [Player{playerId}] Начинаю слушать...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [Player{playerId}] Ошибка запуска распознавания: {e.Message}");
        }
    }

    /// <summary>
    /// Остановить прослушивание голоса
    /// </summary>
    public void StopListening()
    {
        if (!isListening) return;

        try
        {
            StopTranscription();
            isListening = false;
            
            if (debugMode)
                Debug.Log($"⏹️ [Player{playerId}] Перестаю слушать.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [Player{playerId}] Ошибка остановки распознавания: {e.Message}");
        }
    }

    /// <summary>
    /// Переключение режима прослушивания
    /// </summary>
    public void ToggleListening()
    {
        if (isListening)
            StopListening();
        else
            StartListening();
    }

    /// <summary>
    /// Обработка распознанного текста
    /// </summary>
    void ProcessTranscribedText()
    {
        while (HasTranscribedText())
        {
            IntPtr ptr = GetNextTranscribedText();
            if (ptr != IntPtr.Zero)
            {
                try
                {
                    string text = Marshal.PtrToStringAnsi(ptr);
                    FreeText(ptr);

                    if (!string.IsNullOrEmpty(text))
                    {
                        // Фильтруем повторяющиеся команды
                        if (Time.time - lastRecognitionTime > minSilenceDuration)
                        {
                            ProcessRecognizedText(text.Trim());
                            lastRecognitionTime = Time.time;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ [Player{playerId}] Ошибка обработки текста: {e.Message}");
                    FreeText(ptr); // Освобождаем память в случае ошибки
                }
            }
        }
    }

    /// <summary>
    /// Обработка распознанного текста с фильтрацией
    /// </summary>
    void ProcessRecognizedText(string text)
    {
        // Простая фильтрация шума и коротких фраз
        if (text.Length < 2) return;
        
        // Фильтрация частых ложных срабатываний
        string lowerText = text.ToLower();
        if (lowerText.Contains("хм") || lowerText.Contains("эм") || 
            lowerText.Contains("ага") || lowerText.Contains("угу"))
        {
            return;
        }

        if (debugMode)
            Debug.Log($"🗣️ [Player{playerId}] Распознано: \"{text}\"");

        // Уведомляем подписчиков
        OnCommandRecognized?.Invoke(text);
    }

    /// <summary>
    /// Проверка, активно ли прослушивание
    /// </summary>
    public bool IsListening => isListening;

    /// <summary>
    /// Получение ID игрока
    /// </summary>
    public int GetPlayerId() => playerId;

    /// <summary>
    /// Установка режима отладки
    /// </summary>
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
        Debug.Log($"🐛 [Player{playerId}] Режим отладки: {(enabled ? "включен" : "отключен")}");
    }

    /// <summary>
    /// Принудительная обработка команды (для тестирования)
    /// </summary>
    public void SimulateCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) return;
        
        Debug.Log($"🧪 [Player{playerId}] Симуляция команды: \"{command}\"");
        OnCommandRecognized?.Invoke(command);
    }

    void OnDestroy()
    {
        if (isListening)
        {
            StopListening();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isListening)
        {
            StopListening();
        }
        else if (!pauseStatus && autoStartListening)
        {
            StartListening();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isListening)
        {
            StopListening();
        }
        else if (hasFocus && autoStartListening)
        {
            StartListening();
        }
    }
}
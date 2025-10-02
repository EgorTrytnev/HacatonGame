using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class VoiceController : MonoBehaviour
{
    public static VoiceController Instance;

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

    private bool isListening = false;
    private string vadModelPath;
    private string asrModelPath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        vadModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "silero_vad.onnx");
        asrModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "asr_model.onnx");

        StartTranscription(vadModelPath, asrModelPath);
        isListening = true;
        Debug.Log("🎙️ Начинаю слушать...");
    }

    void Update()
    {
        if (!isListening) return;

        while (HasTranscribedText())
        {
            IntPtr ptr = GetNextTranscribedText();
            if (ptr != IntPtr.Zero)
            {
                string text = Marshal.PtrToStringAnsi(ptr);
                FreeText(ptr);

                if (!string.IsNullOrEmpty(text))
                {
                    Debug.Log($"🗣️ Распознано: \"{text}\"");
                    OnCommandRecognized?.Invoke(text.Trim());
                }
            }
        }
    }

    void OnDestroy()
    {
        if (isListening)
        {
            StopTranscription();
            Debug.Log("⏹️ Перестаю слушать.");
        }
    }
}
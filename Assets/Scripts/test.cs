// VoiceCommandManager.cs
using System;
using UnityEngine;

public class VoiceCommandManager : MonoBehaviour
{
    public bool enable = true;
    

    void Start()
    {
        if (enable)
        {
            Initialize();
        }
    }
    
    void Initialize()
    {
        try
        {
            //инициализация и запуск записи
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка {e.Message}");
        }
    }
    
    void Update()
    {
        //таймер с вызовом CheckCommands()
    }
    
    void CheckCommands()
    {
        if (true) // проверка на наличие комманд у транскребатора
        {
            //получаем последнюю комманду и обрабатываем
        }
    }
    

    void OnDestroy()
    {
     //выключаем
    }
}

using System;
using UnityEngine;

public static class VoiceCommandBroadcaster
{
    public static event Action<string, string[]> OnCommandReceived;

    public static void Broadcast(string unitId, string[] actions)
    {
        OnCommandReceived?.Invoke(unitId, actions);
    }
}
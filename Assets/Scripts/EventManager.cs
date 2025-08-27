using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    // Parametresiz event dictionary'si
    private static Dictionary<string, Action> _events = new();

    // Parametreli event dictionary'si
    private static Dictionary<string, Delegate> _paramEvents = new();

    // Abone ol (parametresiz)
    public static void Subscribe(string eventName, Action callback)
    {
        if (!_events.ContainsKey(eventName))
            _events[eventName] = null;

        _events[eventName] += callback;
    }

    // Abonelikten çýk (parametresiz)
    public static void Unsubscribe(string eventName, Action callback)
    {
        if (_events.ContainsKey(eventName))
            _events[eventName] -= callback;
    }

    // Tetikle (parametresiz)
    public static void Invoke(string eventName)
    {
        if (_events.ContainsKey(eventName))
            _events[eventName]?.Invoke();
    }

    // Abone ol (parametreli)
    public static void Subscribe<T>(string eventName, Action<T> callback)
    {
        if (!_paramEvents.ContainsKey(eventName))
            _paramEvents[eventName] = null;

        _paramEvents[eventName] = Delegate.Combine(_paramEvents[eventName], callback);
    }

    // Abonelikten çýk (parametreli)
    public static void Unsubscribe<T>(string eventName, Action<T> callback)
    {
        if (_paramEvents.ContainsKey(eventName))
            _paramEvents[eventName] = Delegate.Remove(_paramEvents[eventName], callback);
    }

    // Tetikle (parametreli)
    public static void Invoke<T>(string eventName, T param)
    {
        if (_paramEvents.TryGetValue(eventName, out var del))
        {
            if (del is Action<T> action)
                action.Invoke(param);
        }
    }

    // (Opsiyonel) Temizle
    public static void ClearAll()
    {
        _events.Clear();
        _paramEvents.Clear();
    }
}

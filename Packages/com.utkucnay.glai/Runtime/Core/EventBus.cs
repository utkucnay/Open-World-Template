using System;
using System.Collections.Generic;

namespace Glai.Core
{
    public static class EventBus
    {
        static Dictionary<string, Action<Object>> events;

        static EventBus()
        {
            events = new Dictionary<string, Action<Object>>();
        }

        static public void Subscribe(string eventName, Action<Object> callback)
        {
            if (events.ContainsKey(eventName))
            {
                events[eventName] += callback;
            }
            else
            {
                events.Add(eventName, callback);
            }
        }

        static public void Unsubscribe(string eventName, Action<Object> callback)
        {
            if (events.ContainsKey(eventName))
            {
                events[eventName] -= callback;
            }
        }

        static public void Publish(string eventName, Object data = null)
        {
            if (events.ContainsKey(eventName))
            {
                events[eventName]?.Invoke(data);
            }
        }
    }
}
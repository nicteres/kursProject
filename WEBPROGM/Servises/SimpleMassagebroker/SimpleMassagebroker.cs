using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public class SimpleMessageBroker : IMessageBroker
{
    private readonly Dictionary<string, List<Action<string>>> _subscribers = new();

    public void Publish(string topic, string message)
    {

        if (_subscribers.ContainsKey(topic))
        {
            foreach (var handler in _subscribers[topic])
            {
                handler(message);
            }
        }
    }

    public void Subscribe(string topic, Action<string> handler)
    {
        if (!_subscribers.ContainsKey(topic))
        {
            _subscribers[topic] = new List<Action<string>>();
        }
        _subscribers[topic].Add(handler);
    }
}
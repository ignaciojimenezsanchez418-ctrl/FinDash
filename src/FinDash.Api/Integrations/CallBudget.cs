namespace FinDash.Api.Integrations;

/// <summary>La API externa no se llamó para no pasar su límite. Quien llama sirve la caché.</summary>
public sealed class RateLimitedException(string message) : Exception(message);

/// <summary>
/// Cuenta las llamadas a una API externa en una ventana de 60 s (RNF-02). Si se llega al
/// máximo o la API respondió 429, no espera: lanza RateLimitedException para que se use la caché.
/// </summary>
public sealed class CallBudget(int maxCallsPerMinute, TimeProvider time)
{
    readonly Queue<DateTimeOffset> _calls = new();
    readonly Lock _lock = new();
    DateTimeOffset _blockedUntil = DateTimeOffset.MinValue;

    public int CallsInLastMinute
    {
        get { lock (_lock) { Trim(time.GetUtcNow()); return _calls.Count; } }
    }

    public void Take(string api)
    {
        lock (_lock)
        {
            var now = time.GetUtcNow();
            if (now < _blockedUntil)
                throw new RateLimitedException($"{api} pidió esperar hasta {_blockedUntil:HH:mm:ss}.");
            Trim(now);
            if (_calls.Count >= maxCallsPerMinute)
                throw new RateLimitedException($"Se llegó al máximo de {maxCallsPerMinute} llamadas por minuto a {api}.");
            _calls.Enqueue(now);
        }
    }

    /// <summary>Se llama cuando la API respondió 429.</summary>
    public void BlockFor(TimeSpan wait)
    {
        lock (_lock) _blockedUntil = time.GetUtcNow() + wait;
    }

    void Trim(DateTimeOffset now)
    {
        while (_calls.Count > 0 && now - _calls.Peek() >= TimeSpan.FromMinutes(1))
            _calls.Dequeue();
    }
}

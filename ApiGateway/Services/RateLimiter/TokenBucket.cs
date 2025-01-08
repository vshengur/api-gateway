namespace ApiGateway.Services.RateLimiter;

public class TokenBucket(int bucketCapacity, double refillRate) : IRateLimiter
{
    private double _currentTokens = bucketCapacity;
    private long _lastRefillTimeTicks = DateTime.UtcNow.Ticks; // Храним время в формате DateTime.Ticks

    public bool AllowRequest()
    {
        RefillTokens(); // Пополняем токены
        double currentTokensSnapshot;

        do
        {
            currentTokensSnapshot = _currentTokens;
            if (currentTokensSnapshot < 1)
            {
                return false; // Недостаточно токенов, отклоняем запрос
            }
        }
        while (Interlocked.CompareExchange(ref _currentTokens, currentTokensSnapshot - 1, currentTokensSnapshot) != currentTokensSnapshot);

        return true; // Запрос разрешён
    }

    private void RefillTokens()
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        var lastRefillTimeSnapshot = Interlocked.Read(ref _lastRefillTimeTicks);

        var elapsedTimeSeconds = (nowTicks - lastRefillTimeSnapshot) / (double)TimeSpan.TicksPerSecond;
        if (elapsedTimeSeconds <= 0) return; // Нет необходимости в пополнении

        var tokensToAdd = elapsedTimeSeconds * refillRate;

        double currentTokensSnapshot;
        do
        {
            currentTokensSnapshot = _currentTokens;
            var newTokenCount = Math.Min(bucketCapacity, currentTokensSnapshot + tokensToAdd);

            if (Interlocked.CompareExchange(ref _currentTokens, newTokenCount, currentTokensSnapshot) == currentTokensSnapshot)
            {
                Interlocked.Exchange(ref _lastRefillTimeTicks, nowTicks); // Обновляем время пополнения
                break;
            }
        } while (true);
    }
}

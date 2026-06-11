using System.Collections.Concurrent;

namespace AuthService.Shared.Sessions;

/// <summary>
/// Maps OpenIddict session IDs (sid) to the set of ticket store keys
/// associated with that SSO session.
/// 
/// This is the critical link for back-channel logout:
/// When the IdP dispatches a logout_token with a sid,
/// we look up all session keys tied to that sid and revoke them.
/// 
/// Thread-safe via ConcurrentDictionary.
/// </summary>
public class SidIndex
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _index = new();

    /// <summary>
    /// Register a session key under a given sid.
    /// Called when a user completes OIDC login and tokens are stored.
    /// </summary>
    public void AddSession(string sid, string sessionKey)
    {
        var bag = _index.GetOrAdd(sid, _ => new ConcurrentBag<string>());
        bag.Add(sessionKey);
    }

    /// <summary>
    /// Get all session keys associated with a given sid.
    /// Used during back-channel logout to find which tickets to revoke.
    /// </summary>
    public IReadOnlyList<string> GetSessions(string sid)
    {
        if (_index.TryGetValue(sid, out var bag))
        {
            return bag.ToArray();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Remove a specific session key from a sid's set.
    /// </summary>
    public void RemoveSession(string sid, string sessionKey)
    {
        if (_index.TryGetValue(sid, out var bag))
        {
            // ConcurrentBag doesn't support removal, so rebuild
            var remaining = new ConcurrentBag<string>(bag.Where(k => k != sessionKey));
            _index.TryUpdate(sid, remaining, bag);
        }
    }

    /// <summary>
    /// Clear all session keys for a sid.
    /// Called after back-channel logout has revoked all sessions.
    /// </summary>
    public void ClearSid(string sid)
    {
        _index.TryRemove(sid, out _);
    }
}

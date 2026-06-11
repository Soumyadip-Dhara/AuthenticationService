using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AuthService.Shared.Sessions;

/// <summary>
/// Server-side ticket store that keeps authentication tickets in memory.
/// Cookies only contain the session key (opaque), never the tokens.
/// 
/// In production, replace with a distributed store (Redis, database, etc.).
/// </summary>
public class InMemoryTicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, AuthenticationTicket> _store = new();

    /// <summary>
    /// Store a new ticket and return a unique session key.
    /// The key (not the ticket) is what goes into the browser cookie.
    /// </summary>
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");
        _store[key] = ticket;
        return Task.FromResult(key);
    }

    /// <summary>
    /// Renew (update) an existing ticket by its key.
    /// Called when the ticket is refreshed (e.g., sliding expiration, token refresh).
    /// </summary>
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        _store[key] = ticket;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieve a ticket by session key.
    /// Returns null if the session has been revoked or doesn't exist.
    /// </summary>
    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _store.TryGetValue(key, out var ticket);
        return Task.FromResult(ticket);
    }

    /// <summary>
    /// Remove a ticket by session key — effectively logging the user out of this app.
    /// Called during logout or back-channel logout.
    /// </summary>
    public Task RemoveAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if a session key exists in the store.
    /// </summary>
    public bool ContainsKey(string key) => _store.ContainsKey(key);
}

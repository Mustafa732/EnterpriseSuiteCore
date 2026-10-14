using System.Collections.Concurrent;

namespace Common.Globalization
{
    public class Sessions
    {
        // In-Memory active sessions tracker -.NET 8 me HttpRuntime nahi hai isliye ye use hoga
        private static readonly ConcurrentDictionary<string, Dictionary<string, object>> _activeSessions = new();

        // Session add/update - Program.cs ya middleware se call karna
        public static void TrackSession(string sessionId, Dictionary<string, object> items)
        {
            _activeSessions[sessionId] = items;
        }

        public static void RemoveSession(string sessionId)
        {
            _activeSessions.TryRemove(sessionId, out _);
        }

        // OLD method ka replacement - Same name rakha hai taake tumhara purana code na toote
        public static IEnumerable<Dictionary<string, object>> GetActiveSessions()
        {
            return _activeSessions.Values;
        }

        // Helper - Session count
        public static int GetActiveSessionCount()
        {
            return _activeSessions.Count;
        }
    }
}
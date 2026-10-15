using System.Security.Cryptography;
using System.Text.Json;

namespace ProjectDesk;

// Integration boundary only: this file never opens a socket, starts a process or executes a tool.
public enum McpConnectorTransport { LocalStdio, RemoteHttps }

public sealed record McpConnectorProfile(string Id, string Name, McpConnectorTransport Transport,
    string Address, bool Enabled = false) {
    public void Validate() {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("A connector needs an ID and a name.");
        if (!Enum.IsDefined(Transport)) throw new ArgumentException("Unknown connector transport.");
        if (Transport == McpConnectorTransport.LocalStdio) {
            if (!Path.IsPathFullyQualified(Address))
                throw new ArgumentException("Choose an absolute executable path for a local connector.");
        } else if (!Uri.TryCreate(Address, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
            throw new ArgumentException("A remote connector needs an HTTPS URL without embedded credentials or a fragment.");
    }
}

public sealed record McpToolProposal(string ConnectorId, string ToolName, string ArgumentsJson,
    string ProjectRevision, bool ChangesProject, bool SendsProjectData) {
    public void Validate() {
        if (string.IsNullOrWhiteSpace(ConnectorId) || string.IsNullOrWhiteSpace(ToolName) ||
            string.IsNullOrWhiteSpace(ProjectRevision)) throw new ArgumentException("Incomplete tool proposal.");
        if (ArgumentsJson is null || System.Text.Encoding.UTF8.GetByteCount(ArgumentsJson) > 262144)
            throw new ArgumentException("Tool arguments exceed the 256 KiB limit.");
        using var json = JsonDocument.Parse(ArgumentsJson, new JsonDocumentOptions { MaxDepth = 32 });
        if (json.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Tool arguments must be a JSON object.");
    }

    internal string Fingerprint() => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(this)));
}

// Approve is called only after the connector UI shows the exact proposal to the user.
// The revision must include document identity and a monotonically increasing generation; history
// position alone is insufficient because Undo can reuse it for a different project state.
public sealed class McpInvocationGate {
    private sealed record Approval(string Fingerprint, DateTimeOffset ExpiresAt);
    private readonly Dictionary<Guid, Approval> _approvals = [];
    private readonly object _sync = new();
    private readonly TimeProvider _clock;
    public McpInvocationGate(TimeProvider? clock = null) => _clock = clock ?? TimeProvider.System;

    public Guid Approve(McpToolProposal proposal) {
        proposal.Validate();
        var token = Guid.NewGuid();
        lock (_sync) {
            var now = _clock.GetUtcNow();
            foreach (var expired in _approvals.Where(p => p.Value.ExpiresAt <= now).Select(p => p.Key).ToArray())
                _approvals.Remove(expired);
            _approvals.Add(token, new(proposal.Fingerprint(), now.AddMinutes(5)));
        }
        return token;
    }

    public void DemandAndConsume(Guid approvalToken, McpToolProposal proposal,
        McpConnectorProfile connector, string currentProjectRevision, bool networkAvailable,
        bool connected, IReadOnlySet<string> locallyAllowedTools) {
        proposal.Validate(); connector.Validate();
        lock (_sync) {
            // A failed attempt also consumes its approval: the UI must present the proposal again.
            if (!_approvals.Remove(approvalToken, out var approval))
                throw new InvalidOperationException("Review and approve this connector action first.");
            if (approval.ExpiresAt <= _clock.GetUtcNow() || approval.Fingerprint != proposal.Fingerprint())
                throw new InvalidOperationException("This approval expired or the connector action changed.");
            if (currentProjectRevision != proposal.ProjectRevision)
                throw new InvalidOperationException("The project changed. Review the action again.");
            if (!connector.Enabled || !connected || connector.Id != proposal.ConnectorId)
                throw new InvalidOperationException("The selected connector is not connected and enabled.");
            if (connector.Transport == McpConnectorTransport.RemoteHttps && !networkAvailable)
                throw new InvalidOperationException("This connector is unavailable offline.");
            if (!locallyAllowedTools.Contains(proposal.ToolName))
                throw new InvalidOperationException("This tool is not permitted by the local connector policy.");
        }
    }

    public void Clear() { lock (_sync) _approvals.Clear(); }
}

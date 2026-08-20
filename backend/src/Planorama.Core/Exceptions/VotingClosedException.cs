using System.Net;

namespace Planorama.Core.Exceptions;

/// <summary>409 — the voting window has closed, so the vote can no longer be cast or changed.</summary>
public class VotingClosedException()
    : AuthProblemException(HttpStatusCode.Conflict, "Voting has closed", "Voting on this suggestion has already closed.");

public enum TowerSubmissionOutcome
{
    Rejected,
    Committed,
    CommittedWithTechnicalFailure
}

public readonly struct TowerSubmissionResult
{
    internal TowerSubmissionResult(TowerSubmissionOutcome outcome, string reason = "")
    { Outcome = outcome; FailureReason = reason; }
    public TowerSubmissionOutcome Outcome { get; }
    public string FailureReason { get; }
    public bool IsCommitted => Outcome != TowerSubmissionOutcome.Rejected;
    internal static TowerSubmissionResult Reject(string reason) =>
        new TowerSubmissionResult(TowerSubmissionOutcome.Rejected, reason);
}

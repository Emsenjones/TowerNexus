using System;

public readonly struct DraftAttemptToken : IEquatable<DraftAttemptToken>
{
    public DraftAttemptToken(ulong battleGeneration, ulong attemptIdentity)
    {
        BattleGeneration = battleGeneration;
        AttemptIdentity = attemptIdentity;
    }

    public ulong BattleGeneration { get; }
    public ulong AttemptIdentity { get; }
    public bool IsValid => BattleGeneration != 0 && AttemptIdentity != 0;

    public bool Equals(DraftAttemptToken other)
    {
        return BattleGeneration == other.BattleGeneration &&
               AttemptIdentity == other.AttemptIdentity;
    }

    public override bool Equals(object obj)
    {
        return obj is DraftAttemptToken other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (BattleGeneration.GetHashCode() * 397) ^
                   AttemptIdentity.GetHashCode();
        }
    }

    public override string ToString()
    {
        return IsValid
            ? $"{BattleGeneration}:{AttemptIdentity}"
            : "<invalid>";
    }

    public static bool operator ==(
        DraftAttemptToken left,
        DraftAttemptToken right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        DraftAttemptToken left,
        DraftAttemptToken right)
    {
        return !left.Equals(right);
    }
}

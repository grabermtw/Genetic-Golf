public readonly struct GolferSettings
{
    public enum Fitness
    {
        drivingDist,
        accuracy
    }

    public enum MoveableJointsExtent
    {
        fullBody,
        armsTorso
    }

    public enum ClubGrip
    {
        oneHand,
        twoHands
    }

    public Fitness fitnessFunc { get; }
    public MoveableJointsExtent moveableJoints { get; }
    public ClubGrip clubGrip { get; }

    public GolferSettings(Fitness fitnessFunc, MoveableJointsExtent moveableJoints, ClubGrip clubGrip)
    {
        this.fitnessFunc = fitnessFunc;
        this.moveableJoints = moveableJoints;
        this.clubGrip = clubGrip;
    }
}
public readonly struct GolferSettings
{
    public enum Fitness
    {
        accuracy,
        drivingDist
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
    public float holeDist { get; }

    public GolferSettings(Fitness fitnessFunc, MoveableJointsExtent moveableJoints, ClubGrip clubGrip, float holeDist)
    {
        this.fitnessFunc = fitnessFunc;
        this.moveableJoints = moveableJoints;
        this.clubGrip = clubGrip;
        this.holeDist = holeDist;
    }
}
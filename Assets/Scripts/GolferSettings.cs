public readonly struct GolferSettings
{
    public enum Fitness
    {
        accuracy,
        drivingDist
    }

    public enum MoveableJointsExtent
    {
        armsTorso,
        fullBody
    }

    public enum ClubGrip
    {
        twoHands,
        oneHand        
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
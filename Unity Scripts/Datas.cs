public class RelativeAngles
{
    public double R0 { get; set; }
    public double R1 { get; set; }
    public double R2 { get; set; }
    public double R3 { get; set; }
    public double R4 { get; set; }
    public double R5 { get; set; }
    public double R6 { get; set; }
    public double R7 { get; set; }
    public double R8 { get; set; }
    public double R9 { get; set; }
}

public class RelativeAnglesData
{
    public RelativeAngles Relative_angles { get; set; }
}

public class FrameData
{
    public int Frame_number { get; set; }
    public double Timestamp { get; set; }
    public RelativeAngles Relative_angles { get; set; }
}

public class Song
{
    public string Song_name { get; set; }
    public string Artist { get; set; }
    public int Start_frame { get; set; }
    public int End_frame { get; set; }
}

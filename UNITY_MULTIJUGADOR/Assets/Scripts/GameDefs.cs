using System;

public enum GameMode
{
    FreeForAll = 0,
    Teams = 1,
    CaptureTheFlag = 2
}

[Serializable]
public class GameModePlayerRequirement
{
    public GameMode gameMode;
    public int requiredPlayers;
}

[Serializable]
public class GameModeSceneConfig
{
    public GameMode gameMode;
    public int[] sceneBuildIndices;
}
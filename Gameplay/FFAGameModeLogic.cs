using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class FFAGameModeLogic : BaseGameModeLogic
{
    public const int TIME_UNTIL_GAME_RESTART = 20;
    public const float DEATH_TIMER = 2.5f;

    // We can use the existing MatchSessionManager's data to track kills, 
    // but the Logic class should be the one to "decide" if that data means a win.
}

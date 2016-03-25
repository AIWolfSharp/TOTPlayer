using AIWolf.Common.Data;
using AIWolf.Common.Net;

namespace TOT
{
    /// <summary>
    /// Managing player which assign special player according to its role.
    /// </summary>
    /// <remarks></remarks>
    public class TOTPlayer : IPlayer
    {
        IPlayer villagerPlayer = new TOTVillager();
        IPlayer seerPlayer = new TOTSeer();
        IPlayer mediumPlayer = new TOTMedium();
        IPlayer bodyguardPlayer = new TOTBodyguard();
        IPlayer possessedPlayer = new TOTPossessed();
        IPlayer werewolfPlayer = new TOTWerewolf();

        IPlayer player;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <remarks></remarks>
        public TOTPlayer() { }

        /// <summary>
        ///  This player's name.
        /// </summary>
        /// <value>This player's name.</value>
        /// <remarks></remarks>
        public string Name
        {
            get
            {
                return "TOT";
            }
        }

        /// <summary>
        /// Returns the agent this werewolf wants to attack. 
        /// </summary>
        /// <returns>The agent this werewolf wants to attack.</returns>
        /// <remarks></remarks>
        public Agent Attack()
        {
            return player.Attack();
        }

        /// <summary>
        /// Called when the day started.
        /// </summary>
        /// <remarks></remarks>
        public void DayStart()
        {
            player.DayStart();
        }

        /// <summary>
        /// Returns the agent this seer wants to divine.
        /// </summary>
        /// <returns>The agent this seer wants to divine.</returns>
        /// <remarks></remarks>
        public Agent Divine()
        {
            return player.Divine();
        }

        /// <summary>
        /// Called when the game finishes.
        /// </summary>
        /// <remarks>Before this method is called, the game information is updated with all information.</remarks>
        public void Finish()
        {
            player.Finish();
        }

        /// <summary>
        /// Returns the agent this bodyguard wants to guard. 
        /// </summary>
        /// <returns>The agent this bodyguard wants to guard.</returns>
        /// <remarks></remarks>
        public Agent Guard()
        {
            return player.Guard();
        }

        /// <summary>
        /// Called when the game started.
        /// </summary>
        /// <param name="gameInfo">The current information of this game.</param>
        /// <param name="gameSetting">The setting of this game.</param>
        /// <remarks></remarks>
        public void Initialize(GameInfo gameInfo, GameSetting gameSetting)
        {
            switch (gameInfo.Role)
            {
                case Role.BODYGUARD:
                    player = bodyguardPlayer;
                    break;
                case Role.MEDIUM:
                    player = mediumPlayer;
                    break;
                case Role.POSSESSED:
                    player = possessedPlayer;
                    break;
                case Role.SEER:
                    player = seerPlayer;
                    break;
                case Role.WEREWOLF:
                    player = werewolfPlayer;
                    break;
                default:
                    player = villagerPlayer;
                    break;
            }
            player.Initialize(gameInfo, gameSetting);
        }

        /// <summary>
        /// Returns this player's talk. 
        /// </summary>
        /// <returns>The string representing this player's talk.</returns>
        /// <remarks>The returned string must be written in aiwolf protocol. Null means SKIP.</remarks>
        public string Talk()
        {
            return player.Talk();
        }

        /// <summary>
        /// Called when the game information is updated. 
        /// </summary>
        /// <param name="gameInfo">The current information of this game.</param>
        /// <remarks></remarks>
        public void Update(GameInfo gameInfo)
        {
            player.Update(gameInfo);
        }

        /// <summary>
        /// Returns the agent this player wants to execute. 
        /// </summary>
        /// <returns>The agent this player wants to execute.</returns>
        /// <remarks></remarks>
        public Agent Vote()
        {
            return player.Vote();
        }

        /// <summary>
        /// Returns this werewolf's whisper.
        /// </summary>
        /// <returns>The string representing this werewolf's whisper.</returns>
        /// <remarks>The returned string must be written in aiwolf protocol. Null means SKIP.</remarks>
        public string Whisper()
        {
            return player.Whisper();
        }
    }
}

using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using System.Collections.Generic;

namespace TOT
{
    /// <summary>
    /// Player's state.
    /// </summary>
    /// <remarks></remarks>
    public class TOTPlayerState
    {
        /// <summary>
        /// Me.
        /// </summary>
        /// <value>The player having this state.</value>
        /// <remarks></remarks>
        public Agent Me { get; }

        /// <summary>
        /// Whether I am alive or dead.
        /// </summary>
        /// <value>ALIVE or DEAD.</value>
        /// <remarks></remarks>
        public Status Status { get; set; } = Status.ALIVE;

        /// <summary>
        /// My role.
        /// </summary>
        /// <value>Role.</value>
        /// <remarks></remarks>
        public Role? Role { get; set; } = null;

        /// <summary>
        /// The role I have come out with.
        /// </summary>
        /// <value>Role.</value>
        /// <remarks></remarks>
        public Role? CoRole { get; set; } = null;

        /// <summary>
        /// My species.
        /// </summary>
        /// <value>Species.</value>
        /// <remarks></remarks>
        public Species? Species { get; set; } = null;

        /// <summary>
        /// My team.
        /// </summary>
        /// <value>TOTeam.</value>
        /// <remarks>UNC if uncertain.</remarks>
        public TOTeam Team { get; set; } = TOTeam.UNC;

        /// <summary>
        /// Whether or not I was attacked.
        /// </summary>
        /// <value>True or false.</value>
        /// <remarks></remarks>
        public bool Attacked { get; set; } = false;

        /// <summary>
        /// The latest topic I talked about.
        /// </summary>
        /// <value>The latest topic I talked about.</value>
        /// <remarks></remarks>
        public Topic? LatestTopic { get; set; } = null;

        /// <summary>
        /// My latest whisper.
        /// </summary>
        /// <value>My latest whisper.</value>
        /// <remarks></remarks>
        public Talk LatestWhisper { get; set; } = null;

        /// <summary>
        /// The player I want to vote.
        /// </summary>
        /// <value>The player I want to vote.</value>
        /// <remarks></remarks>
        public Agent VoteTarget { get; set; } = null;

        /// <summary>
        /// The number of votes for execution I obtain today.
        /// </summary>
        /// <value>The number of votes for execution I obtain today.</value>
        /// <remarks></remarks>
        public int TodaysVotes { get; set; } = 0;

        /// <summary>
        /// The player I want to attack.
        /// </summary>
        /// <value>The player I want to attack.</value>
        /// <remarks></remarks>
        public Agent AttackTarget { get; set; } = null;

        /// <summary>
        /// Whether or not I may be executed.
        /// </summary>
        /// <value>True or false.</value>
        /// <remarks></remarks>
        public bool MayBeExecuted { get; set; } = false;

        List<Judge> divinations = new List<Judge>();
        List<Judge> inquests = new List<Judge>();

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="me">The owner of this state.</param>
        /// <remarks></remarks>
        public TOTPlayerState(Agent me)
        {
            Me = me;
        }

        /// <summary>
        /// Resisters a new divination.
        /// </summary>
        /// <param name="judge">The divination to be registered.</param>
        /// <remarks></remarks>
        public void AddDivination(Judge judge)
        {
            divinations.Add(judge);
        }

        /// <summary>
        /// Registers a new inquest.
        /// </summary>
        /// <param name="judge">The inquest to be registered.</param>
        /// <remarks></remarks>
        public void AddInquest(Judge judge)
        {
            inquests.Add(judge);
        }

        /// <summary>
        /// Increments my vote for execution.
        /// </summary>
        /// <remarks></remarks>
        public void IncTodaysVotes()
        {
            TodaysVotes++;
        }

        /// <summary>
        /// Decrements my vote for execution.
        /// </summary>
        /// <remarks></remarks>
        public void DecTodaysVotes()
        {
            TodaysVotes--;
        }
    }
}

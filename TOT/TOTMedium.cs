using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Net;
using AIWolf.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    /// <summary>
    /// Medium player.
    /// </summary>
    class TOTMedium : TOTRuleBasePlayer
    {
        int coDay; // The day of CO.
        bool isAfterCO = false;
        Queue<Judge> inquestQueue = new Queue<Judge>(); // Queue of inquests.

        public override string Name
        {
            get
            {
                return "TOTMedium";
            }
        }

        public override void DayStart()
        {
            base.DayStart();

            // Introduce inquest.
            Judge judge = currentGameInfo.MediumResult;
            if (judge != null)
            {
                gameState.SetSpecies(judge.Target, judge.Result);
                inquestQueue.Enqueue(judge);
            }
        }

        public override void Initialize(GameInfo gameInfo, GameSetting gameSetting)
        {
            base.Initialize(gameInfo, gameSetting);

            coDay = 2;
            isAfterCO = false;
            inquestQueue.Clear();
        }

        public override string Talk()
        {
            if (day == 0)
            {
                // Do not talk on day 0.
                return TemplateTalkFactory.Over();
            }

            if (!isAfterCO && OughtToCO())
            {
                // Come out with role.
                isAfterCO = true;
                return TemplateTalkFactory.Comingout(me, Role.MEDIUM);
            }

            if (isAfterCO)
            {
                // Once you come out, talk about all known inquests. 
                if (inquestQueue.Count != 0)
                {
                    Judge j = inquestQueue.Dequeue();
                    return TemplateTalkFactory.Inquested(j.Target, j.Result);
                }
            }

            MakeVoteCandidate();

            // Talk about estimation.
            if (estimateQueue.Count != 0)
            {
                // Team villager only : Talk about real estimation.
                Agent a = estimateQueue.Dequeue();
                // Team villager only : Talk about estimation of team werewolf.
                if (teamWerewolf.Contains(a))
                {
                    if (humanPlayers.Contains(a))
                    {
                        // Possessed.
                        return TemplateTalkFactory.Estimate(a, Role.POSSESSED);
                    }
                    else
                    {
                        return TemplateTalkFactory.Estimate(a, Role.WEREWOLF);
                    }
                }
                else if (teamVillager.Contains(a))
                {
                    // Team villager only : Support company's role.
                    Role? role = gameState.GetCoRole(a);
                    if (role != null)
                    {
                        switch (role)
                        {
                            case Role.SEER:
                                return TemplateTalkFactory.Estimate(a, Role.SEER);
                            case Role.MEDIUM:
                                return TemplateTalkFactory.Estimate(a, Role.POSSESSED);
                            case Role.BODYGUARD:
                                return TemplateTalkFactory.Estimate(a, Role.BODYGUARD);
                            default:
                                return TemplateTalkFactory.Estimate(a, Role.VILLAGER);
                        }
                    }
                }
            }
            else if (voteCandidateToDeclare != null)
            {
                // Declare whom you want to vote if you have not done yet.
                voteCandidateToDeclare = null;
                return TemplateTalkFactory.Vote(voteCandidate);
            }
            return TemplateTalkFactory.Over();
        }

        /// <summary>
        /// To come out, or not to com out, that is the question.
        /// </summary>
        /// <returns>True or false.</returns>
        bool OughtToCO()
        {
            // When you are about to be executed, come out with role.
            if (gameState.IsMayBeExecuted(me))
            {
                return true;
            }
            // Come out on the coDay-th day.
            if (day == coDay)
            {
                return true;
            }
            // When a werewolf is found, come out with role.
            Judge j = currentGameInfo.MediumResult;
            if (!isAfterCO && j != null && j.Result == Species.WEREWOLF)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Decide the player to be voted for execution.
        /// </summary>
        void MakeVoteCandidate()
        {
            List<Agent> candidates = new List<Agent>();
            if (teamWerewolf.Count > 0)
            {
                candidates.AddRange(teamWerewolf);
            }
            else if (teamUncertain.Count > 0)
            {
                candidates.AddRange(teamUncertain);
            }
            if (!candidates.Contains(voteCandidate))
            {
                // Change candidate.
                voteCandidate = candidates.Shuffle().First();
                voteCandidateToDeclare = voteCandidate;
            }
        }
    }
}

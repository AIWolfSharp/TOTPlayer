using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    /// <summary>
    /// Villager player.
    /// </summary>
    class TOTVillager : TOTRuleBasePlayer
    {
        public override string Name
        {
            get
            {
                return "TOTVillager";
            }
        }

        public override string Talk()
        {
            if (day == 0)
            {
                // Do not talk on day 0.
                return TemplateTalkFactory.Over();
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
                                return TemplateTalkFactory.Estimate(a, Role.MEDIUM);
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

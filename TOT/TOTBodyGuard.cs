﻿using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    /// <summary>
    /// Bodyguard player.
    /// </summary>
    class TOTBodyguard : TOTRuleBasePlayer
    {
        public override string Name
        {
            get
            {
                return "TOTBodyguard";
            }
        }

        public override Agent Guard()
        {
            foreach (Agent a in teamVillager)
            {
                // If you find a real seer or a villager coming out with seer, guard him.
                if (gameState.GetRole(a) == Role.SEER || gameState.GetCoRole(a) == Role.SEER)
                {
                    return a;
                }
            }
            // If you find a real medium or a villager coming out with medium, guard him.
            foreach (Agent a in teamVillager)
            {
                if (gameState.GetRole(a) == Role.MEDIUM || gameState.GetCoRole(a) == Role.MEDIUM)
                {
                    return a;
                }
            }

            // Guard the player who doesn't belong to any side and comes out with seer. 
            List<Agent> guardCandidates = new List<Agent>();
            foreach (Agent a in teamUncertain)
            {
                if (gameState.GetCoRole(a) == Role.SEER)
                {
                    guardCandidates.Add(a);
                }
            }
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            // Guard the player who doesn't belong to any side and comes out with medium. 
            guardCandidates.Clear();
            foreach (Agent a in teamUncertain)
            {
                if (gameState.GetCoRole(a) == Role.MEDIUM)
                {
                    guardCandidates.Add(a);
                }
            }
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            // Guard one of the villager players. 
            guardCandidates = new List<Agent>(teamVillager);
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            // Guard one of the players not belonging to any side. 
            guardCandidates = new List<Agent>(teamUncertain);
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            return null;
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
                                return TemplateTalkFactory.Estimate(a, Role.POSSESSED);
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

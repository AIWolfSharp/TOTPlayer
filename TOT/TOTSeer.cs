using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Net;
using AIWolf.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    /// <summary>
    /// Seer player.
    /// </summary>
    class TOTSeer : TOTRuleBasePlayer
    {
        int coDay; // The day of CO.
        bool isAfterCO = false;
        Queue<Judge> divinationQueue = new Queue<Judge>(); // Queue of divinations.
        List<Agent> judgeTargetList = new List<Agent>(); // Players divined already.

        public override string Name
        {
            get
            {
                return "TOTSeer";
            }
        }

        public override void DayStart()
        {
            base.DayStart();

            // Introduce divination.
            Judge judge = currentGameInfo.DivineResult;
            if (judge != null)
            {
                judgeTargetList.Add(judge.Target);
                gameState.SetSpecies(judge.Target, judge.Result);
                divinationQueue.Enqueue(judge);
            }
        }

        public override Agent Divine()
        {
            // Divine one of uncertain-side players.
            List<Agent> divineCandidates = new List<Agent>(teamUncertain);
            foreach (Agent a in teamUncertain)
            {
                if (judgeTargetList.Contains(a))
                {
                    divineCandidates.Remove(a);
                }
            }
            if (divineCandidates.Count > 0)
            {
                return divineCandidates.Shuffle().First();
            }
            return null;
        }

        public override void Initialize(GameInfo gameInfo, GameSetting gameSetting)
        {
            base.Initialize(gameInfo, gameSetting);

            coDay = new Random((int)gameSetting.RandomSeed).Next(3) + 1;
            isAfterCO = false;
            divinationQueue.Clear();
            judgeTargetList.Clear();
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
                return TemplateTalkFactory.Comingout(me, Role.SEER);
            }

            if (isAfterCO)
            {
                // Once you come out, talk about all known divinations. 
                if (divinationQueue.Count != 0)
                {
                    Judge j = divinationQueue.Dequeue();
                    return TemplateTalkFactory.Divined(j.Target, j.Result);
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
                                return TemplateTalkFactory.Estimate(a, Role.POSSESSED);
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
        /// To come out, or not to com out, that is the question.
        /// </summary>
        /// <returns></returns>
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

            // Come out when fake seer appeared.
            foreach (var a in others)
            {
                if (gameState.GetCoRole(a) == Role.SEER)
                {
                    return true;
                }
            }

            // When a werewolf is found, come out with role.
            Judge j = currentGameInfo.DivineResult;
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

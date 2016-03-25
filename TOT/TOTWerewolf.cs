using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    /// <summary>
    /// Werewolf player.
    /// </summary>
    class TOTWerewolf : TOTRuleBasePlayer
    {
        Queue<string> whisperQueue = new Queue<string>(); // Queue of whispers;

        public override string Name
        {
            get
            {
                return "TOTWerewolf";
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

            // Do not talk about estimation for now.
            if (estimateQueue.Count != 0 && false)
            {
                Agent a = estimateQueue.Dequeue();
                if (teamVillager.Contains(a))
                {
                    return TemplateTalkFactory.Estimate(a, Role.WEREWOLF);
                }
                else if (teamWerewolf.Contains(a))
                {
                    Role? coRole = gameState.GetCoRole(a);
                    if (coRole != null)
                    {
                        switch (coRole)
                        {
                            case Role.SEER:
                                return TemplateTalkFactory.Estimate(a, Role.SEER);
                            case Role.MEDIUM:
                                return TemplateTalkFactory.Estimate(a, Role.MEDIUM);
                            default:
                                break;
                        }
                    }
                    return TemplateTalkFactory.Estimate(a, Role.VILLAGER);
                }
            }
            else if (voteCandidateToDeclare != null)
            {
                //Declare whom you want to vote if you have not done yet.
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
            List<Agent> villagers = new List<Agent>(teamVillager);
            // Do not vote player to be attacked.
            if (attackCandidate != null)
            {
                villagers.Remove(attackCandidate);
            }

            // Vote villager coming out with seer.
            foreach (Agent a in villagers)
            {
                if (gameState.GetCoRole(a) == Role.SEER)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(voteCandidate))
                {
                    // Change candidate.
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
                return;
            }

            // Vote villager coming out with medium.
            candidates.Clear();
            foreach (Agent a in villagers)
            {
                if (gameState.GetCoRole(a) == Role.MEDIUM)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(voteCandidate))
                {
                    // Change candidate.
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
                return;
            }

            // Vote villager at ramdom.
            candidates = new List<Agent>(villagers);
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(voteCandidate))
                {
                    // Change candidate.
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
                return;
            }

            List<Agent> uncertains = new List<Agent>(teamUncertain);
            if (attackCandidate != null)
            {
                uncertains.Remove(attackCandidate);
            }
            // Vote player not belonging to any side and coming out with seer.
            candidates.Clear();
            foreach (Agent a in uncertains)
            {
                if (gameState.GetCoRole(a) == Role.SEER)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(voteCandidate))
                {
                    // Change candidate.
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
                return;
            }

            // Vote player not belonging to any side and coming out with medium.
            candidates.Clear();
            foreach (Agent a in uncertains)
            {
                if (gameState.GetCoRole(a) == Role.MEDIUM)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(voteCandidate))
                {
                    // Change candidate.
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
                return;
            }

            // Vote player not belonging to any side at random.
            candidates = new List<Agent>(uncertains);
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(voteCandidate))
                {
                    // Change candidate.
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
                return;
            }
        }

        public override string Whisper()
        {
            if (day == 0)
            {
                // Do not whisper on day 0.
                return TemplateWhisperFactory.Over();
            }

            MakeAttackCandidate();

            if (attackCandidateToDeclare != null)
            {
                attackCandidateToDeclare = null;
                List<Agent> otherWerewolfPlayers = new List<Agent>(werewolfPlayers);
                otherWerewolfPlayers.Remove(me);
                foreach (Agent a in otherWerewolfPlayers)
                {
                    Agent tgt = gameState.GetAttackTarget(a);
                    if (tgt != null && !werewolfPlayers.Contains(tgt)) // Do not attack werewolf.
                    {
                        // Agree with other werewolf declaring early.
                        attackCandidate = tgt;
                        whisperQueue.Enqueue(TemplateWhisperFactory.Agree(TalkType.WHISPER, gameState.GetLatestWhisper(a).Day, gameState.GetLatestWhisper(a).Idx));
                        gameState.SetAttackTarget(a, null);
                    }
                }
                // Declare whom you want to attack if you have not done yet.
                whisperQueue.Enqueue(TemplateWhisperFactory.Attack(attackCandidate));
            }
            if (whisperQueue.Count != 0)
            {
                // Whisper if you have something to do.
                return whisperQueue.Dequeue();
            }
            // Nothing to do.
            return TemplateWhisperFactory.Over();
        }

        /// <summary>
        /// Decide the player to be attacked.
        /// </summary>
        void MakeAttackCandidate()
        {
            // Attack the real seer/medium.
            List<Agent> candidates = new List<Agent>();
            foreach (Agent a in teamVillager)
            {
                if (gameState.GetRole(a) == Role.SEER || gameState.GetRole(a) == Role.MEDIUM)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(attackCandidate))
                {
                    // Change candidate.
                    attackCandidate = candidates.Shuffle().First();
                    attackCandidateToDeclare = attackCandidate;
                }
                // No change
                return;
            }

            // Attack the player coming out with seer/medium.
            candidates.Clear();
            foreach (Agent a in teamUncertain)
            {
                if (gameState.GetCoRole(a) == Role.SEER || gameState.GetCoRole(a) == Role.MEDIUM)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(attackCandidate))
                {
                    // Change candidate.
                    attackCandidate = candidates.Shuffle().First();
                    attackCandidateToDeclare = attackCandidate;
                }
                // No change
                return;
            }

            // Attack non-werewolf side player at random.
            candidates = new List<Agent>(aliveOthers);
            candidates.RemoveAll(role => teamWerewolf.Contains(role));
            if (candidates.Count > 0)
            {
                if (!candidates.Contains(attackCandidate))
                {
                    // Change candidate.
                    attackCandidate = candidates.Shuffle().First();
                    attackCandidateToDeclare = attackCandidate;
                }
                // No change
                return;
            }
        }
    }
}

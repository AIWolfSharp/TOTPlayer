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
    /// Possessed player.
    /// </summary>
    class TOTPossessed : TOTRuleBasePlayer
    {
        Role fakeRole = Role.VILLAGER;
        int coDay; // The day of CO.

        // Fake seer specific.
        bool isAfterCO = false;
        Queue<Judge> divinationQueue = new Queue<Judge>(); // Queue of fake divinations.
        List<Agent> judgeTargetList = new List<Agent>(); // Players divined already.

        // Fake medium specific.
        Queue<Judge> inquestQueue = new Queue<Judge>(); // Queue of fake inquests.

        public override string Name
        {
            get
            {
                return "TOTPossessed";
            }
        }

        public override void DayStart()
        {
            base.DayStart();

            MakeFakeJudge();
        }

        public override void Initialize(GameInfo gameInfo, GameSetting gameSetting)
        {
            base.Initialize(gameInfo, gameSetting);

            fakeRole = Role.VILLAGER;
            coDay = new Random((int)gameSetting.RandomSeed).Next(3) + 1;
            isAfterCO = false;
            divinationQueue.Clear();
            judgeTargetList.Clear();
            inquestQueue.Clear();
        }

        /// <summary>
        /// Generate fake judge.
        /// </summary>
        void MakeFakeJudge()
        {
            switch (fakeRole)
            {
                case Role.SEER:
                    // Divine that werewolf player is human.
                    List<Agent> candidates = new List<Agent>(werewolfPlayers);
                    candidates.Remove(me);
                    foreach (Agent a in candidates.Shuffle())
                    {
                        if (!judgeTargetList.Contains(a))
                        {
                            Judge j = new Judge(day, me, a, Species.HUMAN);
                            judgeTargetList.Add(a);
                            divinationQueue.Enqueue(j);
                            return;
                        }
                    }
#if false
                    // Divine that real seer/medium is werewolf.
                    foreach (Agent a in teamVillager.Shuffle())
                    {
                        if (!judgeTargetList.Contains(a))
                        {
                            Role? role = gs.GetRole(a);
                            if (role == Role.SEER || role == Role.MEDIUM)
                            {
                                Judge j = new Judge(day, me, a, Species.WEREWOLF);
                                judgeTargetList.Add(a);
                                divinationQueue.Enqueue(j);
                                return;
                            }
                        }
                    }
#endif
                    // Divine that villager is humam at random.
                    foreach (Agent a in teamVillager.Shuffle())
                    {
                        if (!judgeTargetList.Contains(a))
                        {
                            Judge j = new Judge(day, me, a, Species.HUMAN);
                            judgeTargetList.Add(a);
                            divinationQueue.Enqueue(j);
                            return;
                        }
                    }
                    break;
                case Role.MEDIUM:
                    Agent agent = currentGameInfo.ExecutedAgent;
                    if (!judgeTargetList.Contains(agent))
                    {
                        judgeTargetList.Add(agent);
                        Role? role = gameState.GetRole(agent);
                        Species? species = gameState.GetSpecies(agent);
                        // Claim always that executed player is human. 
                        if (species == Species.WEREWOLF || role == Role.POSSESSED)
                        {
                            inquestQueue.Enqueue(new Judge(day, me, agent, Species.HUMAN));
                        }
                        else {
                            inquestQueue.Enqueue(new Judge(day, me, agent, Species.HUMAN));

                        }
                    }
                    break;
                default:
                    break;
            }
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
                // Decide fake role taking into account the number of COs, and come out with fake role.
                isAfterCO = true;
                int coSeer = 0;
                int coMedium = 0;
                foreach (Agent a in others)
                {
                    if (gameState.GetCoRole(a) == Role.SEER)
                    {
                        coSeer++;
                    }
                    else if (gameState.GetCoRole(a) == Role.MEDIUM)
                    {
                        coMedium++;
                    }
                }
                if (coSeer < 2 && coMedium < 2)
                {
                    // At random.
                    fakeRole = new Role[] { Role.SEER, Role.MEDIUM }.Shuffle().First();
                    return TemplateTalkFactory.Comingout(me, fakeRole);
                }
                else if (coSeer < 2 && coMedium >= 2)
                {
                    fakeRole = Role.SEER;
                    return TemplateTalkFactory.Comingout(me, fakeRole);
                }
                else if (coSeer >= 2 && coMedium < 2)
                {
                    fakeRole = Role.MEDIUM;
                    return TemplateTalkFactory.Comingout(me, fakeRole);
                }
                else {
                    fakeRole = Role.VILLAGER; // Nothing to come out with.
                }
            }

            if (isAfterCO)
            {
                switch (fakeRole)
                {
                    case Role.SEER:
                        // Once you come out, talk about all your fake divinations. 
                        if (divinationQueue.Count != 0)
                        {
                            Judge j = divinationQueue.Dequeue();
                            if (!attackedPlayers.Contains(j.Target))
                            {
                                return TemplateTalkFactory.Divined(j.Target, j.Result);
                            }
                        }
                        break;
                    case Role.MEDIUM:
                        // Once you come out, talk about all your fake inquests.
                        if (inquestQueue.Count != 0)
                        {
                            Judge j = inquestQueue.Dequeue();
                            return TemplateTalkFactory.Inquested(j.Target, j.Result);
                        }
                        break;
                    default:
                        break;
                }
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
#if false
            int maxVotes = -1;
            Agent maxVotesPlayer = null;
            foreach (Agent a in others)
            {
                int nVotes = gs.GetTodaysVotes(a);
                if (nVotes > maxVotes)
                {
                    maxVotes = nVotes;
                    maxVotesPlayer = a;
                }
            }
            int myVotesNum = gs.GetTodaysVotes(me);
            if (myVotesNum > maxVotes)
            { // I will be executed if I do nothing.
                gs.MayBeExecuted(me, true);
                if (voteCandidate != maxVotesPlayer)
                {
                    voteCandidate = maxVotesPlayer;
                    voteCandidateToDeclare = maxVotesPlayer;
                    return;
                }
            }
#endif

            List<Agent> candidates = new List<Agent>();
            foreach (Agent a in teamVillager)
            {
                // If you are fake seer, vote villager coming out with seer. 
                if (isAfterCO && fakeRole == Role.SEER && gameState.GetCoRole(a) == Role.SEER)
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

            // If you are fake medium, vote villager coming out with medium. 
            candidates.Clear();
            foreach (Agent a in teamVillager)
            {
                if (isAfterCO && fakeRole == Role.MEDIUM && gameState.GetCoRole(a) == Role.MEDIUM)
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

            // Vote villager at random. 
            candidates = new List<Agent>(teamVillager);
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

            // If you are fake seer, vote player not belonging to any side and coming out with seer. 
            candidates.Clear();
            foreach (Agent a in teamUncertain)
            {
                if (isAfterCO && fakeRole == Role.SEER && gameState.GetCoRole(a) == Role.SEER)
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

            // If you are fake medium, vote player not belonging to any side and coming out with medium. 
            candidates.Clear();
            foreach (Agent a in teamUncertain)
            {
                if (isAfterCO && fakeRole == Role.MEDIUM && gameState.GetCoRole(a) == Role.MEDIUM)
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
            candidates = new List<Agent>(teamUncertain);
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

        /// <summary>
        /// To come out, or not to com out, that is the question.
        /// </summary>
        /// <returns>True or false.</returns>
        bool OughtToCO()
        {
            // When you are about to be executed, come out with fake role.
            if (gameState.IsMayBeExecuted(me))
            {
                return true;
            }
            // Come out on the coDay-th day.
            if (day == coDay)
            {
                return true;
            }
            return false;
        }
    }
}

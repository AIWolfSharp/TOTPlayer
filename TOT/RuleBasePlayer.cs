using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Net;
using AIWolf.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    class RuleBasePlayer : IPlayer
    {
        Agent me;
        Role myRole;
        TOTGameState gs;
        int day = -1;
        int currentTalkIdx = 0; // The index of current talk.
        Queue<Agent> todaysEstimateQueue = new Queue<Agent>(); // FIFO for estimating talk.
        Agent voteCandidate;
        Agent voteCandidateToDeclare;
        GameInfo currentGameInfo;

        // These fields are determined by facts.
        List<Agent> players;
        List<Agent> others;
        List<Agent> aliveOthers;
        List<Agent> deadOthers = new List<Agent>();
        List<Agent> attackedPlayers = new List<Agent>();
        List<Agent> executedPlayers = new List<Agent>();

        // These fields are determined by inference.
        List<Agent> humanPlayers = new List<Agent>();
        List<Agent> werewolfPlayers = new List<Agent>();
        List<Agent> teamVillager = new List<Agent>();
        List<Agent> teamWerewolf = new List<Agent>();
        List<Agent> teamUncertain = new List<Agent>();
        List<Agent> lastTeamVillager;
        List<Agent> lastTeamWerewolf;

        // These fields are for seer.
        bool isAfterCO = false;
        Queue<Judge> divinationQueue = new Queue<Judge>(); // FIFO for talks about divination.
        List<Agent> judgeTargetList = new List<Agent>();

        public string Name
        {
            get
            {
                return "TOT";
            }
        }

        public Agent Attack()
        {
            // This method is called only in werewolf player.
            // First, find the real seer/medium and attack him.
            List<Agent> candidates = new List<Agent>();
            foreach (Agent a in teamVillager)
            {
                if (gs.GetRole(a) == Role.SEER || gs.GetRole(a) == Role.MEDIUM)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                return candidates.Shuffle().First();
            }

            // Next, find the player coming out with seer/medium and attack him.
            candidates.Clear();
            foreach (Agent a in teamUncertain)
            {
                if (gs.GetCoRole(a) == Role.SEER || gs.GetCoRole(a) == Role.MEDIUM)
                {
                    candidates.Add(a);
                }
            }
            if (candidates.Count > 0)
            {
                return candidates.Shuffle().First();
            }

            // Last, decide a candidate from non-werewolf side players.
            candidates = new List<Agent>(aliveOthers);
            candidates.RemoveAll(role => teamWerewolf.Contains(role));
            if (candidates.Count > 0)
            {
                return candidates.Shuffle().First();
            }
            return null;
        }

        public void DayStart()
        {
            if (day == 0)
            {
                return;
            }

            // Reset agent's state.
            foreach (Agent a in players)
            {
                gs.SetLatestTopic(a, null);
                gs.SetLatestWhisper(a, null);
                gs.SetTodaysVotes(a, 0);
                gs.SetVoteTarget(a, null);
                gs.SetAttackTarget(a, null);
                gs.MayBeExecuted(a, false);
            }

            // Update fields and state based on the attack of last night.
            Agent latestAttackedPlayer = currentGameInfo.AttackedAgent;
            if (latestAttackedPlayer != null)
            {
                aliveOthers.Remove(latestAttackedPlayer);
                deadOthers.Add(latestAttackedPlayer);
                attackedPlayers.Add(latestAttackedPlayer);
                gs.Attacked(latestAttackedPlayer);
            }

            // Update fields based on the execution.
            Agent latestExecutedPlayer = currentGameInfo.ExecutedAgent;
            if (latestExecutedPlayer != null)
            {
                aliveOthers.Remove(latestExecutedPlayer);
                deadOthers.Add(latestExecutedPlayer);
                executedPlayers.Add(latestExecutedPlayer);
            }

            // (Seer only) Update fields and state based on the result of the divination.
            if (myRole == Role.SEER)
            {
                Judge judge = currentGameInfo.DivineResult;
                if (judge != null)
                {
                    judgeTargetList.Add(judge.Target);
                    gs.SetSpecies(judge.Target, judge.Result);
                    divinationQueue.Enqueue(judge);
                }
            }
        }

        public Agent Divine()
        {
            // This method is called only in seer player.
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

        public void Finish()
        {
            return;
        }

        public Agent Guard()
        {
            // This method is called only in bodyguard player.
            foreach (Agent a in teamVillager)
            {
                if (gs.GetRole(a) == Role.SEER || gs.GetCoRole(a) == Role.SEER)
                {
                    return a;
                }
            }
            foreach (Agent a in teamVillager)
            {
                if (gs.GetRole(a) == Role.MEDIUM || gs.GetCoRole(a) == Role.MEDIUM)
                {
                    return a;
                }
            }

            List<Agent> guardCandidates = new List<Agent>();
            foreach (Agent a in teamUncertain)
            {
                if (gs.GetCoRole(a) == Role.SEER)
                {
                    guardCandidates.Add(a);
                }
            }
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            guardCandidates.Clear();
            foreach (Agent a in teamUncertain)
            {
                if (gs.GetCoRole(a) == Role.MEDIUM)
                {
                    guardCandidates.Add(a);
                }
            }
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            guardCandidates = new List<Agent>(teamVillager);
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            guardCandidates = new List<Agent>(teamUncertain);
            if (guardCandidates.Count > 0)
            {
                return guardCandidates.Shuffle().First();
            }

            return null;
        }

        public void Initialize(GameInfo gameInfo, GameSetting gameSetting)
        {
            currentGameInfo = gameInfo;
            me = currentGameInfo.Agent;
            myRole = (Role)currentGameInfo.Role;
            gs = new TOTGameState(currentGameInfo);
            players = currentGameInfo.AgentList;
            others = new List<Agent>(players);
            others.Remove(me);
            aliveOthers = new List<Agent>(others);

            // At first, the candidate to be voted for execution is decided at random.
            voteCandidate = others.Shuffle().First();
            voteCandidateToDeclare = voteCandidate;
        }

        public string Talk()
        {
            if (day == 0)
            {
                return TemplateTalkFactory.Over();
            }

            if (myRole == Role.SEER)
            {
                // Seer only : Come out when the time is ripe.
                if (!isAfterCO && OughtToCO())
                {
                    isAfterCO = true;
                    return TemplateTalkFactory.Comingout(me, Role.SEER);
                }

                // Seer only : Talk about divination if already come out.
                if (isAfterCO)
                {
                    if (divinationQueue.Count != 0)
                    {
                        Judge j = divinationQueue.Dequeue();
                        return TemplateTalkFactory.Divined(j.Target, j.Result);
                    }
                }
            }

            MakeVoteCandidate(); // Decide the candidate to be voted for execution.

            // First, talk about estimation.
            if (todaysEstimateQueue.Count != 0)
            {
                // Team werewolf only : Talk about fake estimation.
                if (myRole == Role.WEREWOLF || myRole == Role.POSSESSED)
                {
                    Agent a = todaysEstimateQueue.Dequeue();
                    if (teamVillager.Contains(a))
                    {
                        //return TemplateTalkFactory.Estimate(a, Role.WEREWOLF);
                    }
                    else if (teamWerewolf.Contains(a))
                    {
                        // Team werewolf only : Support company's fake role.
                        Role? coRole = gs.GetCoRole(a);
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
                else
                {
                    // Team villager only : Talk about real estimation.
                    Agent a = todaysEstimateQueue.Dequeue();
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
                        Role? role = gs.GetRole(a);
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
            }
            else if (voteCandidateToDeclare != null)
            {
                // Next, talk about will to vote if it has not be declared.
                voteCandidateToDeclare = null;
                return TemplateTalkFactory.Vote(voteCandidate);
            }
            return TemplateTalkFactory.Over();
        }

        /// <summary>
        /// Decide the candidate to be voted for execution.
        /// </summary>
        void MakeVoteCandidate()
        {
            if (myRole == Role.WEREWOLF || myRole == Role.POSSESSED)
            {
                // Team werewolf only.
                List<Agent> candidates = new List<Agent>();
                // First, find a candidate from players of team villager who came out about being seer.
                foreach (Agent a in teamVillager)
                {
                    if (gs.GetCoRole(a) == Role.SEER)
                    {
                        candidates.Add(a);
                    }
                }
                if (candidates.Count > 0 && !candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                    return;
                }

                // Find a candidate from players of team villager who came out about being medium.
                candidates.Clear();
                foreach (Agent a in teamVillager)
                {
                    if (gs.GetCoRole(a) == Role.MEDIUM)
                    {
                        candidates.Add(a);
                    }
                }
                if (candidates.Count > 0 && !candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                    return;
                }

                // Find a candidate from players of team villager.
                candidates = new List<Agent>(teamVillager);
                if (candidates.Count > 0 && !candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                    return;
                }

                // Find a candidate from players of team uncertain who came out about being seer.
                candidates.Clear();
                foreach (Agent a in teamUncertain)
                {
                    if (gs.GetCoRole(a) == Role.SEER)
                    {
                        candidates.Add(a);
                    }
                }
                if (candidates.Count > 0 && !candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                    return;
                }

                // Find a candidate from players of team uncertain who came out about being medium.
                candidates.Clear();
                foreach (Agent a in teamUncertain)
                {
                    if (gs.GetCoRole(a) == Role.MEDIUM)
                    {
                        candidates.Add(a);
                    }
                }
                if (candidates.Count > 0 && !candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                    return;
                }

                // Find a candidate from players of team uncertain.
                candidates = new List<Agent>(teamUncertain);
                if (candidates.Count > 0 && !candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                    return;
                }
            }
            else
            {
                // Team villager only.
                List<Agent> candidates;
                if (teamWerewolf.Count > 0)
                {
                    candidates = new List<Agent>(teamWerewolf);
                }
                else if (teamUncertain.Count > 0)
                {
                    candidates = new List<Agent>(teamUncertain);
                }
                else
                {
                    candidates = new List<Agent>(teamVillager);
                }
                if (!candidates.Contains(voteCandidate))
                {
                    voteCandidate = candidates.Shuffle().First();
                    voteCandidateToDeclare = voteCandidate;
                }
            }
        }

        /// <summary>
        /// To come out, or not to com out, that is the question.
        /// </summary>
        /// <returns></returns>
        bool OughtToCO()
        {
            // If the seer is about to be executed, he ought to come out about his role.
            if (gs.IsMayBeExecuted(me))
            {
                return true;
            }
            // When the seer find a werewolf, he ought to com out about his role.
            Judge j = currentGameInfo.DivineResult;
            if (!isAfterCO && j != null && j.Result == Species.WEREWOLF)
            {
                return true;
            }
            return false;
        }


        public void Update(GameInfo gameInfo)
        {
            currentGameInfo = gameInfo;
            if (day != currentGameInfo.Day)
            {
                // New day has just come.
                currentTalkIdx = 0;
                day = currentGameInfo.Day;
                voteCandidateToDeclare = voteCandidate; // Talk about vote at least once a day.
                return;
            }

            // Analysis of recent talks.
            List<Talk> talkList = currentGameInfo.TalkList.GetRange(currentTalkIdx, currentGameInfo.TalkList.Count - currentTalkIdx);
            currentTalkIdx = currentGameInfo.TalkList.Count;
            foreach (Talk talk in talkList)
            {
                Agent talker = talk.Agent;
                Utterance utterance = new Utterance(talk.Content);
                Topic? topic = utterance.Topic;
                gs.SetLatestTopic(talker, topic);
                switch (topic)
                {
                    case Topic.COMINGOUT:
                        gs.SetCoRole(talker, utterance.Role);
                        break;
                    case Topic.DIVINED:
                        gs.AddDivination(talker, new Judge(day, talker, utterance.Target, (Species)utterance.Result));
                        break;
                    case Topic.INQUESTED:
                        gs.AddNecromancy(talker, new Judge(day, talker, utterance.Target, (Species)utterance.Result));
                        break;
                    case Topic.VOTE:
                        // Update the state of voting.
                        if (gs.GetVoteTarget(talker) != null)
                        {
                            gs.DecTodaysVotes(gs.GetVoteTarget(talker));
                        }
                        gs.SetVoteTarget(talker, utterance.Target);
                        gs.IncTodaysVotes(utterance.Target);
                        break;
                    default:
                        break;
                }
            }

            // Update agent's species and team based on the latest inference.
            lastTeamVillager = new List<Agent>(teamVillager);
            lastTeamWerewolf = new List<Agent>(teamWerewolf);
            humanPlayers.Clear();
            werewolfPlayers.Clear();
            teamVillager.Clear();
            teamWerewolf.Clear();
            teamUncertain.Clear();
            foreach (Agent a in aliveOthers)
            {
                if (gs.GetSpecies(a) == Species.HUMAN)
                {
                    humanPlayers.Add(a);
                }
                else if (gs.GetSpecies(a) == Species.WEREWOLF)
                {
                    werewolfPlayers.Add(a);
                }
                // If newly inferred belonging to team villager, put the agent at the tail of the queue.
                if (gs.GetTeam(a) == TOTeam.VILLAGER)
                {
                    teamVillager.Add(a);
                    if (!lastTeamVillager.Contains(a) && !todaysEstimateQueue.Contains(a))
                    {
                        todaysEstimateQueue.Enqueue(a);
                    }
                }
                // If newly inferred belonging to team werewolf, put the agent at the tail of the queue.
                if (gs.GetTeam(a) == TOTeam.WEREWOLF)
                {
                    teamWerewolf.Add(a);
                    if (!lastTeamWerewolf.Contains(a) && !todaysEstimateQueue.Contains(a))
                    {
                        todaysEstimateQueue.Enqueue(a);
                    }
                }
                else
                {
                    teamUncertain.Add(a);
                }
            }
        }

        public Agent Vote()
        {
            return voteCandidate;
        }

        public string Whisper()
        {
            return TemplateWhisperFactory.Over();
        }
    }
}

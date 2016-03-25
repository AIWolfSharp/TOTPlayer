using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Net;
using AIWolf.Common.Util;
using System.Collections.Generic;
using System.Linq;

namespace TOT
{
    /// <summary>
    /// Player who infers using rule base system.
    /// </summary>
    class TOTRuleBasePlayer : IPlayer
    {
        protected Agent me;
        protected Role myRole;
        protected TOTGameState gameState;
        protected int day = -1;
        protected int currentTalkIdx = 0; // The index of current talk.
        protected int currentWhisperIdx = 0; // The index of current whisper.
        protected Queue<Agent> estimateQueue = new Queue<Agent>(); // Queue of player whose role is estimated.
        protected Agent voteCandidate;
        protected Agent voteCandidateToDeclare;
        protected Agent attackCandidate;
        protected Agent attackCandidateToDeclare;
        protected GameInfo currentGameInfo;

        // These fields are determined by facts.
        protected List<Agent> players;
        protected List<Agent> others;
        protected List<Agent> aliveOthers;
        protected List<Agent> deadOthers = new List<Agent>();
        protected List<Agent> attackedPlayers = new List<Agent>();
        protected List<Agent> executedPlayers = new List<Agent>();

        // These fields are determined by inference.
        protected List<Agent> humanPlayers = new List<Agent>();
        protected List<Agent> werewolfPlayers = new List<Agent>();
        protected List<Agent> teamVillager = new List<Agent>();
        protected List<Agent> teamWerewolf = new List<Agent>();
        protected List<Agent> teamUncertain = new List<Agent>();
        protected List<Agent> lastTeamVillager;
        protected List<Agent> lastTeamWerewolf;

        public virtual string Name
        {
            get
            {
                return "TOTRuleBasePlayer";
            }
        }

        public virtual Agent Attack()
        {
            return attackCandidate;
        }

        public virtual void DayStart()
        {
            if (day == 0)
            {
                // Do nothin on day 0.
                return;
            }

            // Reset players' states.
            foreach (Agent a in players)
            {
                gameState.SetLatestTopic(a, null);
                gameState.SetLatestWhisper(a, null);
                gameState.SetTodaysVotes(a, 0);
                gameState.SetVoteTarget(a, null);
                gameState.SetAttackTarget(a, null);
                gameState.MayBeExecuted(a, false);
            }

            // Update fields and states based on the attack of last night.
            Agent latestAttackedPlayer = currentGameInfo.AttackedAgent;
            if (latestAttackedPlayer != null)
            {
                aliveOthers.Remove(latestAttackedPlayer);
                deadOthers.Add(latestAttackedPlayer);
                attackedPlayers.Add(latestAttackedPlayer);
                gameState.Attacked(latestAttackedPlayer);
            }

            // Update fields based on the execution.
            Agent latestExecutedPlayer = currentGameInfo.ExecutedAgent;
            if (latestExecutedPlayer != null)
            {
                aliveOthers.Remove(latestExecutedPlayer);
                deadOthers.Add(latestExecutedPlayer);
                executedPlayers.Add(latestExecutedPlayer);
            }
        }

        public virtual Agent Divine()
        {
            return null;
        }

        public virtual void Finish()
        {
            return;
        }

        public virtual Agent Guard()
        {
            return null;
        }

        public virtual void Initialize(GameInfo gameInfo, GameSetting gameSetting)
        {
            currentGameInfo = gameInfo;
            me = currentGameInfo.Agent;
            myRole = (Role)currentGameInfo.Role;
            gameState = new TOTGameState(currentGameInfo);
            day = -1;
            currentTalkIdx = 0;
            currentWhisperIdx = 0;
            estimateQueue.Clear();

            players = currentGameInfo.AgentList;
            others = new List<Agent>(players);
            others.Remove(me);
            aliveOthers = new List<Agent>(others);
            deadOthers.Clear();
            attackedPlayers.Clear();
            executedPlayers.Clear();

            // At first, vote other player at random.
            voteCandidate = others.Shuffle().First();
            voteCandidateToDeclare = voteCandidate;

            attackCandidate = null;
            attackCandidateToDeclare = null;
        }

        public virtual string Talk()
        {
            return TemplateTalkFactory.Over();
        }

        public virtual void Update(GameInfo gameInfo)
        {
            currentGameInfo = gameInfo;
            if (day != currentGameInfo.Day)
            {
                // New day has just come.
                currentTalkIdx = 0;
                currentWhisperIdx = 0;
                day = currentGameInfo.Day;
                voteCandidateToDeclare = voteCandidate; // Declare whom you want to vote at least once a day.
                attackCandidateToDeclare = attackCandidate; // Declare whom you want to attack at least once a day.
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
                gameState.SetLatestTopic(talker, topic);
                switch (topic)
                {
                    case Topic.COMINGOUT:
                        gameState.SetCoRole(talker, utterance.Role);
                        break;
                    case Topic.DIVINED:
                        gameState.AddDivination(talker, new Judge(day, talker, utterance.Target, (Species)utterance.Result));
                        break;
                    case Topic.INQUESTED:
                        gameState.AddInquest(talker, new Judge(day, talker, utterance.Target, (Species)utterance.Result));
                        break;
                    case Topic.VOTE:
                        // Update the state of voting.
                        if (gameState.GetVoteTarget(talker) != null)
                        {
                            gameState.DecTodaysVotes(gameState.GetVoteTarget(talker));
                        }
                        gameState.SetVoteTarget(talker, utterance.Target);
                        gameState.IncTodaysVotes(utterance.Target);
                        break;
                    default:
                        break;
                }
            }

            // Analysis of recent whispers
            List<Talk> whisperList = currentGameInfo.WhisperList.GetRange(currentWhisperIdx, currentGameInfo.WhisperList.Count - currentWhisperIdx);
            currentWhisperIdx = gameInfo.WhisperList.Count;
            foreach (Talk whisper in whisperList)
            {
                Agent whisperer = whisper.Agent;
                Utterance utterance = new Utterance(whisper.Content);
                Topic? topic = utterance.Topic;
                switch (topic)
                {
                    case Topic.ATTACK:
                        gameState.SetLatestWhisper(whisperer, whisper);
                        gameState.SetAttackTarget(whisperer, utterance.Target);
                        break;
                    default:
                        break;
                }
            }

            // Update players' species and team based on the latest inference.
            lastTeamVillager = new List<Agent>(teamVillager);
            lastTeamWerewolf = new List<Agent>(teamWerewolf);
            humanPlayers.Clear();
            werewolfPlayers.Clear();
            teamVillager.Clear();
            teamWerewolf.Clear();
            teamUncertain.Clear();
            foreach (Agent a in aliveOthers)
            {
                if (gameState.GetSpecies(a) == Species.HUMAN)
                {
                    humanPlayers.Add(a);
                }
                else if (gameState.GetSpecies(a) == Species.WEREWOLF)
                {
                    werewolfPlayers.Add(a);
                }
                // Put the newly inferred player at the tail of the queue.
                if (gameState.GetTeam(a) == TOTeam.VILLAGER)
                {
                    teamVillager.Add(a);
                    if (!lastTeamVillager.Contains(a) && !estimateQueue.Contains(a))
                    {
                        estimateQueue.Enqueue(a);
                    }
                }
                // Put the newly inferred player at the tail of the queue.
                if (gameState.GetTeam(a) == TOTeam.WEREWOLF)
                {
                    teamWerewolf.Add(a);
                    if (!lastTeamWerewolf.Contains(a) && !estimateQueue.Contains(a))
                    {
                        estimateQueue.Enqueue(a);
                    }
                }
                else
                {
                    teamUncertain.Add(a);
                }
            }
        }

        public virtual Agent Vote()
        {
            return voteCandidate;
        }

        public virtual string Whisper()
        {
            return TemplateWhisperFactory.Over();
        }
    }
}

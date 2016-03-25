using AIWolf.Client.Lib;
using AIWolf.Common.Data;
using AIWolf.Common.Net;
using System;
using System.Collections.Generic;

namespace TOT
{
    /// <summary>
    /// State of game.
    /// </summary>
    /// <remarks></remarks>
    public class TOTGameState
    {
        Dictionary<Agent, TOTPlayerState> agentStateMap = new Dictionary<Agent, TOTPlayerState>();
        Agent me;
        List<Agent> players;
        Role? myRole;
        TOTRuleBase rb;

        /// <summary>
        /// Initializes a new instance of this class with game information.
        /// </summary>
        /// <param name="gameInfo">Game information.</param>
        /// <remarks></remarks>
        public TOTGameState(GameInfo gameInfo)
        {
            me = gameInfo.Agent;
            myRole = gameInfo.Role;
            rb = new TOTRuleBase(me.ToString(), gameInfo);
            players = new List<Agent>(gameInfo.AgentList);
            foreach (Agent a in players)
            {
                TOTPlayerState ps = new TOTPlayerState(a);
                if (gameInfo.RoleMap.ContainsKey(a))
                {
                    // Role of the agent is known.
                    Role role = gameInfo.RoleMap[a];
                    ps.Role = role;
                    rb.NewFact("roleOf" + a, role.ToString());
                    ps.Species = role.GetSpecies();
                    rb.NewFact("speciesOf" + a, role.GetSpecies().ToString());
                    ps.Team = role.GetTOTeam();
                    rb.NewFact("teamOf" + a, role.GetTOTeam().ToString());
                }
                else
                {
                    ps.Role = null;
                    rb.SetVariableValue("roleOf" + a, "UNC");
                    rb.SetVariableValue("speciesOf" + a, "UNC");
                    rb.SetVariableValue("teamOf" + a, "UNC");
                }
                rb.SetVariableValue("coOf" + a, "UNC");
                rb.SetVariableValue("is" + a + "Attacked", "FALSE");
                agentStateMap[a] = ps;
            }
            rb.DeclareFacts();
            rb.ForwardChain();
            UpdateGameState();
        }

        /// <summary>
        /// Updates the state of game.
        /// </summary>
        /// <remarks></remarks>
        public void UpdateGameState()
        {
            foreach (Agent a in players)
            {
                try
                {
                    agentStateMap[a].Role = (Role)Enum.Parse(typeof(Role), rb.GetVariable("roleOf" + a).Value);
                }
                catch (ArgumentException)
                {
                    // UNC
                    agentStateMap[a].Role = null;
                }
                try
                {
                    agentStateMap[a].Species = (Species)Enum.Parse(typeof(Species), rb.GetVariable("speciesOf" + a).Value);
                }
                catch (ArgumentException)
                {
                    // UNC
                    agentStateMap[a].Species = null;
                }
                try
                {
                    agentStateMap[a].Team = (TOTeam)TOTeam.Parse(typeof(TOTeam), rb.GetVariable("teamOf" + a).Value);
                }
                catch
                {
                    // UNC
                    agentStateMap[a].Team = TOTeam.UNC;
                }
            }
        }

        /// <summary>
        /// Returns the role of the agent given. 
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The role of the agent given.</returns>
        /// <remarks>Null if uncertain.</remarks>
        public Role? GetRole(Agent agent)
        {
            return agentStateMap[agent].Role;
        }

        /// <summary>
        /// Sets the role of the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="role">The role of the agent given.</param>
        /// <remarks>That role is null, means uncertain.</remarks>
        public void SetRole(Agent agent, Role? role)
        {
            agentStateMap[agent].Role = role;
            if (role == null)
            {
                rb.SetVariableValue("roleOf" + agent, "UNC");
            }
            else {
                rb.SetVariableValue("roleOf" + agent, role.ToString());
            }
            rb.ForwardChain();
            UpdateGameState();
        }

        /// <summary>
        /// Returns the species of agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The species of agent given.</returns>
        /// <remarks>Null if uncertain.</remarks>
        public Species? GetSpecies(Agent agent)
        {
            return agentStateMap[agent].Species;
        }

        /// <summary>
        /// Sets the species of the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="species">The species of the agent given.</param>
        /// <remarks>That species is null, means uncertain.</remarks>
        public void SetSpecies(Agent agent, Species? species)
        {
            agentStateMap[agent].Species = species;
            if (species == null)
            {
                rb.SetVariableValue("speciesOf" + agent, "UNC");
            }
            else
            {
                rb.SetVariableValue("speciesOf" + agent, species.ToString());
            }
            rb.ForwardChain();
            UpdateGameState();
        }

        /// <summary>
        /// Returns the role which the given agent has come out with.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>the role which the given agent has come out with.</returns>
        /// <remarks>Null if the agent has not come out with his role.</remarks>
        public Role? GetCoRole(Agent agent)
        {
            return agentStateMap[agent].CoRole;
        }

        /// <summary>
        /// Sets the role which the given agent has come out with.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="role">The role which the given agent has come out with.</param>
        /// <remarks>That role is null, means uncertain.</remarks>
        public void SetCoRole(Agent agent, Role? role)
        {
            agentStateMap[agent].CoRole = role;
            if (role == null)
            {
                rb.SetVariableValue("coOf" + agent, "UNC");
            }
            else {
                rb.SetVariableValue("coOf" + agent, role.ToString());
            }
            rb.ForwardChain();
            UpdateGameState();
        }

        /// <summary>
        /// Sets the latest topic of the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="topic">The latest topic of the agent given.</param>
        /// <remarks></remarks>
        public void SetLatestTopic(Agent agent, Topic? topic)
        {
            agentStateMap[agent].LatestTopic = topic;
        }

        /// <summary>
        /// Returns the latest whisper of the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The latest whisper of the agent given.</returns>
        /// <remarks></remarks>
        public Talk GetLatestWhisper(Agent agent)
        {
            return agentStateMap[agent].LatestWhisper;
        }

        /// <summary>
        /// Sets the latest whisper of the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="whisper">The latest whisper of the agent given.</param>
        /// <remarks></remarks>
        public void SetLatestWhisper(Agent agent, Talk whisper)
        {
            agentStateMap[agent].LatestWhisper = whisper;
        }

        /// <summary>
        /// Returns the agent which the given agent wants to attack.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The agent which the given agent wants to attack.</returns>
        /// <remarks></remarks>
        public Agent GetAttackTarget(Agent agent)
        {
            return agentStateMap[agent].AttackTarget;
        }

        /// <summary>
        /// Sets the agent which the given agent wants to attack.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="target">The agent which the given agent wants to attack.</param>
        /// <remarks></remarks>
        public void SetAttackTarget(Agent agent, Agent target)
        {
            agentStateMap[agent].AttackTarget = target;
        }

        /// <summary>
        /// Registers a new divination with the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="judge">The divination to be registered.</param>
        /// <remarks></remarks>
        public void AddDivination(Agent agent, Judge judge)
        {
            agentStateMap[agent].AddDivination(judge);
            rb.SetVariableValue(agent + "Div" + judge.Target, judge.Result.ToString());
            rb.ForwardChain();
            UpdateGameState();
        }

        /// <summary>
        /// Register a new inquest with the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="judge">The inquest to be registered.</param>
        /// <remarks></remarks>
        public void AddInquest(Agent agent, Judge judge)
        {
            agentStateMap[agent].AddInquest(judge);
            rb.SetVariableValue(agent + "Inq" + judge.Target, judge.Result.ToString());
            rb.ForwardChain();
            UpdateGameState();
        }

        /// <summary>
        /// Returns the agent which the given agent wants to vote.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The agent which the given agent wants to vote.</returns>
        /// <remarks></remarks>
        public Agent GetVoteTarget(Agent agent)
        {
            return agentStateMap[agent].VoteTarget;
        }

        /// <summary>
        /// Sets the agent which the given agent wants to vote.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="target">The agent which the given agent wants to vote.</param>
        /// <remarks></remarks>
        public void SetVoteTarget(Agent agent, Agent target)
        {
            agentStateMap[agent].VoteTarget = target;
        }

        /// <summary>
        /// Returns the number of votes the given agent obtains today.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The number of votes for execution the given agent obtains today.</returns>
        /// <remarks></remarks>
        public int GetTodaysVotes(Agent agent)
        {
            return agentStateMap[agent].TodaysVotes;
        }

        /// <summary>
        /// Sets the number of votes the given agent obtains today.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="todaysVotes">The number of votes for execution the given agent obtains today.</param>
        /// <remarks></remarks>
        public void SetTodaysVotes(Agent agent, int todaysVotes)
        {
            agentStateMap[agent].TodaysVotes = todaysVotes;
        }

        /// <summary>
        /// Increments the given agent's vote for execution.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <remarks></remarks>
        public void IncTodaysVotes(Agent agent)
        {
            agentStateMap[agent].IncTodaysVotes();
        }

        /// <summary>
        /// Decrements the given agent's vote for execution.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <remarks></remarks>
        public void DecTodaysVotes(Agent agent)
        {
            agentStateMap[agent].DecTodaysVotes();
        }

        /// <summary>
        /// Returns the team of the agent given.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>The team of the agent given.</returns>
        /// <remarks></remarks>
        public TOTeam GetTeam(Agent agent)
        {
            return agentStateMap[agent].Team;
        }

        /// <summary>
        /// Whether or not the given agent may be executed.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <returns>True if the given agent may be executed, otherwise, false.</returns>
        /// <remarks></remarks>
        public bool IsMayBeExecuted(Agent agent)
        {
            return agentStateMap[agent].MayBeExecuted;
        }

        /// <summary>
        /// Sets whether or not the given agent may be executed.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <param name="mayBeExecuted">True if the given agent may be executed, otherwise, false.</param>
        /// <remarks></remarks>
        public void MayBeExecuted(Agent agent, bool mayBeExecuted)
        {
            agentStateMap[agent].MayBeExecuted = mayBeExecuted;
        }

        /// <summary>
        /// Sets whether or not the given agent was attacked to true.
        /// </summary>
        /// <param name="agent">Agent.</param>
        /// <remarks></remarks>
        public void Attacked(Agent agent)
        {
            agentStateMap[agent].Attacked = true;
            rb.SetVariableValue("is" + agent + "Attacked", "TRUE");
            rb.ForwardChain();
            UpdateGameState();
        }
    }
}

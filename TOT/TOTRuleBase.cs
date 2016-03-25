using AIWolf.Common.Data;
using AIWolf.Common.Net;
using RuleBaseSystem;
using System;
using System.Collections.Generic;

namespace TOT
{
    /// <summary>
    /// Rule base system for AIWolf.
    /// </summary>
    /// <remarks></remarks>
    public class TOTRuleBase : RuleBase
    {
        /// <summary>
        /// Initializes a new instance of this class with name and game information
        /// </summary>
        /// <param name="name">Name of this rule base.</param>
        /// <param name="gameInfo">Game information.</param>
        /// <remarks></remarks>
        public TOTRuleBase(string name, GameInfo gameInfo) : base(name)
        {
            SetupRuleBase(gameInfo);
        }

        void SetupRuleBase(GameInfo gameInfo)
        {
            Agent me = gameInfo.Agent;
            Role? myRole = gameInfo.Role;

            // Build rule base.
            NewFact("Iam", me.ToString());
            List<Agent> players = gameInfo.AgentList;
            List<Agent> others = new List<Agent>(players);
            others.Remove(me);
            foreach (Agent a1 in others)
            {
                // Permanent rules.
                foreach (Role role in Enum.GetValues(typeof(Role)))
                {
                    if (role != Role.FREEMASON)
                    {
                        NewRule(new Clause(GetVariable("roleOf" + a1), "EQ", role.ToString()), new Consequent(GetVariable("speciesOf" + a1), role.GetSpecies().ToString()));
                        NewRule(new Clause(GetVariable("roleOf" + a1), "EQ", role.ToString()), new Consequent(GetVariable("teamOf" + a1), role.GetTOTeam().ToString()));
                    }
                }
                // Team villager -> human.
                NewRule(new Clause(GetVariable("teamOf" + a1), "EQ", TOTeam.VILLAGER.ToString()), new Consequent(GetVariable("speciesOf" + a1),
                        Species.HUMAN.ToString()));
                // Species werewolf -> role werewolf.
                NewRule(new Clause(GetVariable("speciesOf" + a1), "EQ", Species.WEREWOLF.ToString()), new Consequent(GetVariable("roleOf" + a1),
                        Role.WEREWOLF.ToString()));
                // Attacked player -> human.
                NewRule(new Clause(GetVariable("is" + a1 + "Attacked"), "EQ", "TRUE"), new Consequent(GetVariable("speciesOf" + a1), Species.HUMAN.ToString()));
                // Werewolf side human -> possessed.
                NewRule(new Clause[] { new Clause(GetVariable("teamOf" + a1), "EQ", TOTeam.WEREWOLF.ToString()),
                    new Clause(GetVariable("speciesOf" + a1), "EQ", Species.HUMAN.ToString()) }, new Consequent(GetVariable("roleOf" + a1), Role.POSSESSED.ToString()));

                // Rules that we hope so.
                // Team villager's talk is always true.
                NewRule(new Clause(GetVariable("teamOf" + a1), "EQ", TOTeam.VILLAGER.ToString()), new Consequent(GetVariable("roleOf" + a1), GetVariable("coOf" + a1)));

                // Role specific rules.
                switch (myRole)
                {
                    case Role.BODYGUARD:
                        NewRule(new Clause(GetVariable("coOf" + a1), "EQ", Role.BODYGUARD.ToString()), new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));
                        break;
                    case Role.MEDIUM:
                        NewRule(new Clause(GetVariable("coOf" + a1), "EQ", Role.MEDIUM.ToString()), new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));
                        break;
                    case Role.POSSESSED:
                        NewRule(new Clause(GetVariable("is" + a1 + "Attacked"), "EQ", "TRUE"), new Consequent(GetVariable("teamOf" + a1), TOTeam.VILLAGER.ToString()));
                        break;
                    case Role.SEER:
                        NewRule(new Clause(GetVariable("coOf" + a1), "EQ", Role.SEER.ToString()), new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));
                        break;
                    default:
                        break;
                }

                List<Agent> players2 = new List<Agent>(players);
                players2.Remove(a1);
                foreach (Agent a2 in players2)
                {
                    // Initialize variables of divination/inquest.
                    SetVariableValue(a1 + "Div" + a2, "UNC");
                    SetVariableValue(a1 + "Inq" + a2, "UNC");
                    // (We hope) seer whose divination is true, is in villager side.
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.SEER.ToString()),
                        new Clause(GetVariable(a1 + "Div" + a2), "EQ", Species.HUMAN.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.HUMAN.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.VILLAGER.ToString()));
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.SEER.ToString()),
                        new Clause(GetVariable(a1 + "Div" + a2), "EQ", Species.WEREWOLF.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.WEREWOLF.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.VILLAGER.ToString()));

                    // (We hope) seer whose divination is false, is in werewolf side.
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.SEER.ToString()),
                        new Clause(GetVariable(a1 + "Div" + a2), "EQ", Species.WEREWOLF.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.HUMAN.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.SEER.ToString()),
                        new Clause(GetVariable(a1 + "Div" + a2), "EQ", Species.HUMAN.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.WEREWOLF.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));

                    // (We hope) medium whose inquest is true, is in villager side.
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.MEDIUM.ToString()),
                        new Clause(GetVariable(a1 + "Inq" + a2), "EQ", Species.HUMAN.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.HUMAN.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.VILLAGER.ToString()));
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.MEDIUM.ToString()),
                        new Clause(GetVariable(a1 + "Inq" + a2), "EQ", Species.WEREWOLF.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.WEREWOLF.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.VILLAGER.ToString()));

                    // (We hope) medium whose inquest is false, is in werewolf side.
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.MEDIUM.ToString()),
                        new Clause(GetVariable(a1 + "Inq" + a2), "EQ", Species.WEREWOLF.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.HUMAN.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));
                    NewRule(new Clause[] { new Clause(GetVariable("coOf" + a1), "EQ", Role.MEDIUM.ToString()),
                        new Clause(GetVariable(a1 + "Inq" + a2), "EQ", Species.HUMAN.ToString()),
                        new Clause(GetVariable("speciesOf" + a2), "EQ", Species.WEREWOLF.ToString()) },
                        new Consequent(GetVariable("teamOf" + a1), TOTeam.WEREWOLF.ToString()));
                }

                List<Agent> others2 = new List<Agent>(others);
                others2.Remove(a1);
                foreach (Agent a2 in others2)
                {
                    // (We hope) team villager's divination is true.
                    NewRule(new Clause(GetVariable("teamOf" + a1), "EQ", TOTeam.VILLAGER.ToString()),
                        new Consequent(GetVariable("speciesOf" + a2), GetVariable(a1 + "Div" + a2)));
                    // (We hope) team villager's inquest is true.
                    NewRule(new Clause(GetVariable("teamOf" + a1), "EQ", TOTeam.VILLAGER.ToString()),
                        new Consequent(GetVariable("speciesOf" + a2), GetVariable(a1 + "Inq" + a2)));
                }
            }
            DeclareFacts();
            ForwardChain();
        }

        /// <summary>
        /// Adds a new fact to this rule base.
        /// </summary>
        /// <param name="varName">Name of variable.</param>
        /// <param name="value">Value of variable.</param>
        /// <remarks></remarks>
        public void NewFact(String varName, String value)
        {
            NewFact(GetVariable(varName), (Object)value);
        }
    }
}

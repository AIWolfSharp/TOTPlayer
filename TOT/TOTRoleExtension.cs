using AIWolf.Common.Data;
using System.Collections.Generic;

namespace TOT
{
    /// <summary>
    /// Defines a extension method of enum Role.
    /// </summary>
    /// <remarks></remarks>
    public static class TOTRoleExtension
    {
        static Dictionary<Role, TOTeam> roleTOTeamMap = new Dictionary<Role, TOTeam>();

        static TOTRoleExtension()
        {
            roleTOTeamMap[Role.BODYGUARD] = TOTeam.VILLAGER;
            roleTOTeamMap[Role.FREEMASON] = TOTeam.VILLAGER;
            roleTOTeamMap[Role.MEDIUM] = TOTeam.VILLAGER;
            roleTOTeamMap[Role.POSSESSED] = TOTeam.WEREWOLF;
            roleTOTeamMap[Role.SEER] = TOTeam.VILLAGER;
            roleTOTeamMap[Role.VILLAGER] = TOTeam.VILLAGER;
            roleTOTeamMap[Role.WEREWOLF] = TOTeam.WEREWOLF;
        }

        /// <summary>
        /// Returns the team of the role given.
        /// </summary>
        /// <param name="role">Role.</param>
        /// <returns>The team of the role given.</returns>
        /// <remarks>UNC if uncertain.</remarks>
        public static TOTeam GetTOTeam(this Role role)
        {
            return roleTOTeamMap[role];
        }
    }
}

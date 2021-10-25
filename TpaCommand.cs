using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Commands;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Users;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Command = OpenMod.Core.Commands.Command;

namespace SimpleTpa
{
    [Command("teleportask")]
    [CommandAlias("tpa")]
    [CommandAlias("tpask")]
    [CommandSyntax("<player>")]
    [CommandDescription("Ask teleport to a player.")]
    public class TpaCommand : Command
    {
        private readonly IUserManager m_UserManager;
        private readonly IStringLocalizer m_StringLocalizer;

        public TpaCommand(IServiceProvider serviceProvider, IUserManager userManager, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_UserManager = userManager;
            m_StringLocalizer = stringLocalizer;
        } 

        protected override async Task OnExecuteAsync()
        {
            UnturnedUser user = (UnturnedUser)Context.Actor;
            if (Context.Parameters.Count != 1)
            {
                throw new CommandWrongUsageException("Error! Correct usage: /tpa <player>");
            }

            string tname = await Context.Parameters.GetAsync<string>(0);

            Player target = PlayerTool.getPlayer(tname); 

            if (target == null)
            {
                throw new UserFriendlyException(m_StringLocalizer["plugin_translation:tpa_error_player_not_found"]);
            }
            else if (target == user.Player.Player)
            {
                throw new UserFriendlyException(m_StringLocalizer["plugin_translation:tpa_error_yourself"]);
            }
            else
            {
                UnturnedUser targetUser = (UnturnedUser)await m_UserManager.FindUserAsync(KnownActorTypes.Player, target.channel.owner.playerID.steamID.ToString(), UserSearchMode.FindById);

                await targetUser.PrintMessageAsync(m_StringLocalizer["plugin_translation:tpa_received", new { USER = user.DisplayName }]);
                SimpleTpa.PendingTeleports.Add(user, targetUser);
            }
        }
    }
}

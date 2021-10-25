using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Cysharp.Threading.Tasks;
using OpenMod.Unturned.Plugins;
using OpenMod.API.Plugins;
using SDG.Unturned;
using System.Linq;
using System.Collections.Generic;
using OpenMod.Unturned.Users;
using OpenMod.API.Users;
using OpenMod.Core.Helpers;
using OpenMod.Core.Users;
using System.Threading.Tasks;

[assembly: PluginMetadata("SS.SimpleTpa", DisplayName = "SimpleTpa")]
namespace SimpleTpa
{
    public class SimpleTpa : OpenModUnturnedPlugin
    {
        private readonly IConfiguration m_Configuration;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;
        private readonly ILogger<SimpleTpa> m_Logger;

        public SimpleTpa(
            IConfiguration configuration, 
            IStringLocalizer stringLocalizer,
            ILogger<SimpleTpa> logger, 
            IUserManager userManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Configuration = configuration;
            m_StringLocalizer = stringLocalizer;
            m_UserManager = userManager;
            m_Logger = logger;
        }

        public async UniTask TelportPlayer(UnturnedUser target, UnturnedUser player) // player TO target
        {
            int interval = m_Configuration.GetSection("plugin_configuration:tpainterval").Get<int>() - 3;
            await Task.Delay(TimeSpan.FromSeconds(interval));
            await player.PrintMessageAsync("3...");
            await Task.Delay(TimeSpan.FromSeconds(1));
            await player.PrintMessageAsync("2...");
            await Task.Delay(TimeSpan.FromSeconds(1));
            await player.PrintMessageAsync("1...");
            await player.PrintMessageAsync(m_StringLocalizer["plugin_translation:tpa_now"]);
            await UniTask.SwitchToMainThread();

            if (!target.Player.IsAlive)
            {
                await player.PrintMessageAsync(m_StringLocalizer["plugin_translation:tpa_error_target_dead"]);
            }
            else if (!player.Player.IsAlive)
            {
                await player.PrintMessageAsync(m_StringLocalizer["plugin_translation:tpa_error_user_dead"]);
            }
            else
            {
                player.Player.Player.teleportToLocationUnsafe(target.Player.Player.transform.position, target.Player.Player.look.yaw);
            }
        }

        protected override async UniTask OnLoadAsync()
        {
            m_Logger.LogInformation("Plugin loaded correctly!");
            m_Logger.LogInformation("<<SSPlugins>>");

            await UniTask.CompletedTask;

            PlayerAnimator.OnLeanChanged_Global += PlayerAnimator_OnLeanChanged_Global;
        }

        private void PlayerAnimator_OnLeanChanged_Global(PlayerAnimator obj)
        {
            AsyncHelper.RunSync(async () => {
                UnturnedUser user = (UnturnedUser)await m_UserManager.FindUserAsync(KnownActorTypes.Player, obj.player.channel.owner.playerID.steamID.ToString(), UserSearchMode.FindById);
                if (PendingTeleports.Any(k => k.Value == user))
                {
                    if (obj.leanLeft)
                    {
                        var toRemove = PendingTeleports.Where(k => k.Value == user).First();
                        var sender = toRemove.Key;
                        PendingTeleports.Remove(toRemove.Key);
                        await sender.PrintMessageAsync(m_StringLocalizer["plugin_translation:tpa_cancelled"]);
                    }
                    else if (obj.leanRight)
                    {
                        var toRemove = PendingTeleports.Where(k => k.Value == user).First();
                        var sender = toRemove.Key;
                        PendingTeleports.Remove(toRemove.Key);
                        await sender.PrintMessageAsync(m_StringLocalizer["plugin_translation:tpa_accepted", new { USER = user.DisplayName, INTERVAL = m_Configuration.GetSection("plugin_configuration:tpainterval").Get<int>() }]);
                        AsyncHelper.Schedule("Start Teleport", () => TelportPlayer(user, sender).AsTask());
                    }
                }
            });
        }

        protected override async UniTask OnUnloadAsync()
        {
            PlayerAnimator.OnLeanChanged_Global -= PlayerAnimator_OnLeanChanged_Global;

            m_Logger.LogInformation("Plugin unloaded!");
            m_Logger.LogInformation("<<SSPlugins>>");

            await UniTask.CompletedTask;
        }

        internal static Dictionary<UnturnedUser, UnturnedUser> PendingTeleports = new Dictionary<UnturnedUser, UnturnedUser>();
        internal static Dictionary<UnturnedUser, DateTime> TpaCooldown = new Dictionary<UnturnedUser, DateTime>();
    }
}

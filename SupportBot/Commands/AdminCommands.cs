﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SupportBot.Commands
{
	[Group("admin")]
	[Description("Admin commands.")]
	[Hidden]
	public class AdminCommands
	{
		[Command("setlogchannel")]
		[RequirePermissions(Permissions.Administrator)]
		public async Task SetLogChannel(CommandContext command)
		{
			await command.RespondAsync("Received setlogchannel command");
		}

		[Command("addrole")]
		[RequirePermissions(Permissions.Administrator)]
		public async Task AddRole(CommandContext command)
		{
			await command.RespondAsync("Received addrole command");
		}

		[Command("removerole")]
		[RequirePermissions(Permissions.Administrator)]
		public async Task RemoveRole(CommandContext command)
		{
			await command.RespondAsync("Received removerole command");
		}

		[Command("addcategory")]
		public async Task AddCategory(CommandContext command)
		{
			await command.RespondAsync("Received addcategory command");
		}
		[Command("removecategory")]
		public async Task RemoveCategory(CommandContext command)
		{
			await command.RespondAsync("Received removecategory command");
		}
	}
}

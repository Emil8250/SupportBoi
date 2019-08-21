﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using SupportBoi.Commands;

namespace SupportBoi
{
	internal class SupportBoi
	{
		internal static SupportBoi instance;

		private DiscordClient discordClient = null;
		private CommandsNextModule commands = null;

		static void Main(string[] args)
		{
			new SupportBoi().MainAsync().GetAwaiter().GetResult();
		}

		private async Task MainAsync()
		{
			instance = this;
			
			Console.WriteLine("Starting SupportBoi version " + this.GetVersion() + "...");
			try
			{
				this.Reload();

				// Block this task until the program is closed.
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Console.WriteLine("Fatal error:");
				Console.WriteLine(e);
				Console.ReadLine();
			}
		}

		private string GetVersion()
		{
			Version version = Assembly.GetEntryAssembly()?.GetName().Version;
			return version?.Major + "." + version?.Minor + "." + version?.Build + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0));
		}

		public async void Reload()
		{
			if (this.discordClient != null)
			{
				await this.discordClient.DisconnectAsync();
				this.discordClient.Dispose();
				Console.WriteLine("Discord client disconnected.");
			}

			Console.WriteLine("Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();

			// Check if token is unset
			if (Config.token == "<add-token-here>" || Config.token == "")
			{
				Console.WriteLine("You need to set your bot token in the config and start the bot again.");
				Console.WriteLine("Press enter to close application.");
				Console.ReadLine();
				return;
			}

			// Database connection and setup
			Console.WriteLine("Connecting to database...");
			Database.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
			try
			{
				Database.SetupTables();
			}
			catch (Exception e)
			{
				Console.WriteLine("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e.Message);
				Console.WriteLine("Press enter to close application.");
				Console.ReadLine();
				return;
			}

			Console.WriteLine("Setting up Discord client...");

			// Checking log level
			if (!Enum.TryParse(Config.logLevel, true, out LogLevel logLevel))
			{
				Console.WriteLine("Log level " + Config.logLevel + " invalid, using 'Info' instead.");
				logLevel = LogLevel.Info;
			}

			// Setting up client configuration
			DiscordConfiguration cfg = new DiscordConfiguration
			{
				Token = Config.token,
				TokenType = TokenType.Bot,

				AutoReconnect = true,
				LogLevel = logLevel,
				UseInternalLogHandler = true
			};

			this.discordClient = new DiscordClient(cfg);

			Console.WriteLine("Hooking events...");
			this.discordClient.Ready += this.OnReady;
			this.discordClient.GuildAvailable += this.OnGuildAvailable;
			this.discordClient.ClientErrored += this.OnClientError;

			Console.WriteLine("Registering commands...");
			commands = discordClient.UseCommandsNext(new CommandsNextConfiguration
			{
				StringPrefix = Config.prefix
			});

			this.commands.RegisterCommands<TicketCommands>();
			this.commands.RegisterCommands<ModeratorCommands>();
			this.commands.RegisterCommands<AdminCommands>();

			Console.WriteLine("Hooking command events...");
			this.commands.CommandExecuted += this.OnCommandExecuted;
			this.commands.CommandErrored += this.OnCommandError;

			Console.WriteLine("Connecting to Discord...");
			await this.discordClient.ConnectAsync();
		}

		private Task OnReady(ReadyEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "Client is ready to process events.", DateTime.Now);
			discordClient.UpdateStatusAsync(new DiscordGame(Config.prefix + "new"), UserStatus.Online);
			return Task.CompletedTask;
		}

		private Task OnGuildAvailable(GuildCreateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", $"Guild available: {e.Guild.Name}", DateTime.Now);

			IReadOnlyList<DiscordRole> roles = e.Guild.Roles;

			foreach (DiscordRole role in roles)
			{
				e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", role.Name.PadRight(40, '.') + role.Id, DateTime.Now);
			}
			return Task.CompletedTask;
		}

		private Task OnClientError(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBoi", $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.Now);

			return Task.CompletedTask;
		}

		private Task OnCommandExecuted(CommandExecutionEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", $"User {e.Context.User.Username} used command '{e.Command.Name}' successfully.", DateTime.Now);

			return Task.CompletedTask;
		}

		private Task OnCommandError(CommandErrorEventArgs e)
		{
			switch (e.Exception)
			{
				case CommandNotFoundException _:
					return Task.CompletedTask;
				case ChecksFailedException _:
				{
					foreach (CheckBaseAttribute attr in ((ChecksFailedException)e.Exception).FailedChecks)
					{
						DiscordEmbed error = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = this.ParseFailedCheck(attr)
						};
						e.Context?.Channel?.SendMessageAsync("", false, error);
					}
					return Task.CompletedTask;
				}

				default:
				{
					e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBoi", $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.Now);
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Internal error occured, please report this to the developer."
					};
					e.Context?.Channel?.SendMessageAsync("", false, error);
					return Task.CompletedTask;
				}
			}
		}

		private string ParseFailedCheck(CheckBaseAttribute attr)
		{
			switch (attr)
			{
				case CooldownAttribute _:
					return "You cannot use do that so often!";
				case RequireOwnerAttribute _:
					return "Only the server owner can use that command!";
				case RequirePermissionsAttribute _:
					return "You don't have permission to do that!";
				case RequireRolesAttributeAttribute _:
					return "You do not have a required role!";
				case RequireUserPermissionsAttribute _:
					return "You don't have permission to do that!";
				case RequireNsfwAttribute _:
					return "This command can only be used in an NSFW channel!";
				default:
					return "Unknown Discord API error occured, please try again later.";
			}
		}
	}
}

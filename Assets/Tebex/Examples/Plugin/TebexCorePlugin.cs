using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tebex.Common;
using Tebex.Plugin;
using Category = Tebex.Plugin.Category;
using Package = Tebex.Plugin.Package;

#nullable enable

namespace Tebex.Examples.Plugin
{
    /// <summary>
    /// TebexCorePlugin defines the main logic and flows for a typical Tebex plugin. It can serve as both an example for
    /// implementing Tebex and a working plugin that processes command-based deliverables for Tebex packages on a game server.
    /// </summary>
    public class TebexExamplePlugin
    {
        protected PluginApi PluginApi;
        protected PluginAdapter PluginAdapter;

        /// <summary>
        /// Queue of player joins sent periodically to Tebex
        /// </summary>
        protected readonly List<JoinEvent> JoinEvents = new List<JoinEvent>();

        /// <summary>
        /// Queue of commands that ran successfully, periodically sent to Tebex.
        /// </summary>
        protected readonly List<Command> CompletedCommands = new List<Command>();

        /// <summary>
        /// Cached instance of the current store based on the secret key
        /// </summary>
        protected Store ConnectedStore;

        // Cached store components
        protected List<Package> Packages = new List<Package>();
        protected List<Category> Categories = new List<Category>();
        protected List<Coupon> Coupons = new List<Coupon>();
        protected List<CommunityGoal> Goals = new List<CommunityGoal>();
        protected List<Sale> Sales = new List<Sale>();

        // Recurring tasks to perform and their cancellation tokens
        private Task? _refreshStoreDataTask;
        private Task? _syncServerActivitiesTask;
        private Task? _queueCheckTask;
        private CancellationTokenSource _syncStoreDataCancellationToken = new CancellationTokenSource();
        private CancellationTokenSource _syncServerActivitiesCancellationToken = new CancellationTokenSource();
        private CancellationTokenSource _queueCheckCancellationToken = new CancellationTokenSource();

#pragma warning disable CS8618, CS9264
        private TebexExamplePlugin() {} // Force using Initialize(), where non-nullable fields are set based on key
#pragma warning restore CS8618, CS9264

        public static Task<TebexExamplePlugin> Initialize(PluginAdapter adapter, string key)
        {
            var plugin = new TebexExamplePlugin
            {
                PluginAdapter = adapter,
                PluginApi = PluginApi.Initialize(adapter, key)
            };

            var successfulStoreConnection = new TaskCompletionSource<bool>();
            plugin.PluginAdapter.LogDebug("Connecting to Tebex store...");
            plugin.PluginApi.GetInformation(store =>
            {
                plugin.PluginAdapter.LogDebug($"Successfully connected to {store.account.domain} as {store.server.name}");
                plugin.Start(store);
                successfulStoreConnection.SetResult(true);
            }, onPluginApiError => {
                successfulStoreConnection.SetResult(false);
            }, onServerError => {
                successfulStoreConnection.SetResult(false);
            }).Wait();

            if (successfulStoreConnection.Task.Result == false)
            {
                return Task.FromException<TebexExamplePlugin>(new Exception("Did not successfully connect to the Tebex store. Is your secret key correct?"));
            }

            return Task.FromResult(plugin);
        }

        public void Start(Store storeInfo)
        {
            PluginAdapter.LogDebug("Tebex is starting...");
            ConnectedStore = storeInfo;
            SyncStoreData();
            _scheduleRecurringTasks();
        }

        public void Stop()
        {
            PluginAdapter.LogDebug("Tebex is stopping...");
            _syncStoreDataCancellationToken.Cancel();
            _syncServerActivitiesCancellationToken.Cancel();
            _queueCheckCancellationToken.Cancel();

            _queueCheckTask?.Wait();
            _refreshStoreDataTask?.Wait();
            _syncServerActivitiesTask?.Wait();

            PluginAdapter.LogDebug("Tebex has stopped.");
        }

        private void _scheduleRecurringTasks()
        {
            _syncStoreDataCancellationToken = new CancellationTokenSource();
            _syncServerActivitiesCancellationToken = new CancellationTokenSource();
            _queueCheckCancellationToken = new CancellationTokenSource();

            _syncServerActivitiesTask = Scheduler.ExecuteEvery(PluginAdapter, TimeSpan.FromSeconds(60), () =>
            {
                SendPlayerJoinEvents();
                DeleteCompletedCommands();
            }, _syncServerActivitiesCancellationToken.Token);

            _refreshStoreDataTask = Scheduler.ExecuteEvery(PluginAdapter, TimeSpan.FromMinutes(30), SyncStoreData,
                _syncStoreDataCancellationToken.Token);

            _queueCheckTask = Scheduler.ExecuteEvery(PluginAdapter, TimeSpan.FromSeconds(120), CheckCommandQueue, _queueCheckCancellationToken.Token);
        }

        public void SendPlayerJoinEvents()
        {
            if (JoinEvents.Count == 0) return;

            PluginAdapter.LogDebug($"Sending {JoinEvents.Count} join events...");
            PluginApi.SendJoinEvents(JoinEvents, success =>
            {
                JoinEvents.Clear();
                PluginAdapter.LogDebug("Successfully cleared join events.");
            }, onPluginApiError =>
            {
                PluginAdapter.DefaultPluginError(onPluginApiError);
            }, onServerError =>
            {
                PluginAdapter.DefaultServerError(onServerError);
            });
        }

        public void CheckCommandQueue()
        {
            TaskCompletionSource<int> nextCheck = new TaskCompletionSource<int>();

            PluginAdapter.LogDebug("Checking command queue...");
            PluginApi.GetCommandQueue(queue =>
            {
                _handleCommandQueue(queue);
                nextCheck.SetResult(queue.meta.next_check);
            }, onPluginApiError =>
            {
                nextCheck.SetException(new Exception(onPluginApiError.error_message));
            }, onServerError =>
            {
                nextCheck.SetException(new Exception(onServerError.Body));
            }).Wait();

            if (nextCheck.Task.IsFaulted) return;

            PluginAdapter.LogDebug("Next check received was " + nextCheck.Task.Result + " seconds.");
        }

        public void DeleteCompletedCommands()
        {
            var commandIdsToDelete = new int[CompletedCommands.Count];
            for (var i = 0; i < CompletedCommands.Count; i++)
            {
                commandIdsToDelete[i] = CompletedCommands[i].id;
            }

            if (commandIdsToDelete.Length == 0)
            {
                PluginAdapter.LogDebug("No completed commands to delete.");
                return;
            }

            PluginAdapter.LogDebug($"Deleting {CompletedCommands.Count} completed commands...");
            PluginApi.DeleteCommands(commandIdsToDelete, response => CompletedCommands.Clear(), PluginAdapter.DefaultPluginError, PluginAdapter.DefaultServerError);
        }

        public void SyncStoreData()
        {
            PluginApi.GetAllCoupons(coupons =>
                {
                    Coupons = coupons.data;
                    PluginAdapter.LogDebug($"Fetched {coupons.data.Count} coupons");
                },
                onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                onServerError => PluginAdapter.DefaultServerError(onServerError));

            PluginApi.GetAllSales(sales =>
                {
                    Sales = sales.data;
                    PluginAdapter.LogDebug($"Fetched {sales.data.Count} sales");
                },
                onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                onServerError => PluginAdapter.DefaultServerError(onServerError));

            PluginApi.GetAllPackages(true, packages =>
                {
                    Packages = packages;
                    PluginAdapter.LogDebug($"Fetched {packages.Count} packages");
                },
                onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                onServerError => PluginAdapter.DefaultServerError(onServerError));

            PluginApi.GetListings(response =>
                {
                    Categories = response.categories;
                    PluginAdapter.LogDebug($"Fetched {response.categories.Count} categories");
                },
                onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                onServerError => PluginAdapter.DefaultServerError(onServerError));

            PluginApi.GetAllCommunityGoals(goals =>
                {
                    Goals = goals;
                    PluginAdapter.LogDebug($"Fetched {goals.Count} community goals");
                },
                onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                onServerError => PluginAdapter.DefaultServerError(onServerError));
        }

        public void OnPlayerJoin(string userId, string ipAddress)
        {
            var joinEvent = new JoinEvent(userId, "server.join", ipAddress);
            JoinEvents.Add(joinEvent);
            PluginAdapter.LogDebug($"Added join event for {userId}. Join event queue size: {JoinEvents.Count}");
        }

        public void Checkout(Package package, string username)
        {
            PluginAdapter.LogDebug("Generating checkout URL for " + username + " for package " + package.id + "...");
            PluginApi.CreateCheckoutUrl(package.id, username,
                checkout =>
                {
                    PluginAdapter.TellPlayer(username,
                        $"Please visit the following link to complete your purchase: {checkout.url}");
                }, onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                onServerError => PluginAdapter.DefaultServerError(onServerError));
        }

        private void _handleCommandQueue(CommandQueueResponse queue)
        {
            if (queue.meta.execute_offline)
            {
                PluginAdapter.LogDebug("Checking for offline commands...");
                _handleOfflineCommands();
            }
            else
            {
                PluginAdapter.LogDebug("No offline commands are queued.");
            }

            if (queue.players.Count == 0)
            {
                PluginAdapter.LogDebug("No online commands are queued.");
                return;
            }

            PluginAdapter.LogDebug($"{queue.players.Count} players have commands queued.");
            _handleOnlineCommands(queue);
        }

        private void _handleOfflineCommands()
        {
            PluginApi.GetOfflineCommands(response =>
            {
                foreach (var command in response.commands)
                {
                    PluginAdapter.LogDebug($"Executing offline command {command.id}: '{command.command_to_run}' | delay: {command.conditions.delay}");

                    Scheduler.ExecuteAfter(command.conditions.delay, () =>
                    {
                        var success = PluginAdapter.ExecuteCommand(command);
                        if (success)
                        {
                            CompletedCommands.Add(command);
                            PluginAdapter.LogDebug($"Offline command {command.id} executed successfully. Completed commands: {CompletedCommands.Count}");
                        }
                        else
                        {
                            PluginAdapter.LogError($"Offline command {command.id} failed to execute successfully.");
                        }
                    }).Wait();
                }
            }, onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError), onServerError => PluginAdapter.DefaultServerError(onServerError)).Wait();
        }

        private void _handleOnlineCommands(CommandQueueResponse queue)
        {
            foreach (var duePlayer in queue.players)
            {
                if (!PluginAdapter.IsPlayerOnline(duePlayer))
                {
                    PluginAdapter.LogDebug($"Player {duePlayer.name} has commands due, but is not online. Skipping.");
                    continue;
                }

                PluginAdapter.LogDebug($"Retrieving commands for player {duePlayer.name}...");
                PluginApi.GetOnlineCommands(duePlayer.id, onlineCommandsForThisPlayer =>
                    {
                        PluginAdapter.LogDebug($"Player {duePlayer.name} has {onlineCommandsForThisPlayer.commands.Count} commands due.");
                        foreach (var command in onlineCommandsForThisPlayer.commands)
                        {
                            PluginAdapter.LogDebug($"Executing command {command.id} for player {duePlayer.name}: '{command.command_to_run}' | delay: {command.conditions.delay} | slots: {command.conditions.slots}...");

                            Scheduler.ExecuteAfter(command.conditions.delay, () =>
                            {
                                if (command.conditions.slots > 0 &&
                                    !PluginAdapter.PlayerHasInventorySlotsAvailable(duePlayer, command.conditions.slots))
                                {
                                    PluginAdapter.LogWarning(
                                        $"Player {command.player.username} does not have enough slots to execute command {command.id}: '{command.command_to_run}'",
                                        "We will try again at the next commands check.");
                                    return;
                                }

                                var success = PluginAdapter.ExecuteCommand(command);
                                if (success)
                                {
                                    CompletedCommands.Add(command);
                                    PluginAdapter.LogDebug($"Command {command.id} executed successfully. Completed commands: {CompletedCommands.Count}");
                                }
                                else
                                {
                                    PluginAdapter.LogError($"Command {command.id} for player {duePlayer.name} failed to execute successfully.");
                                }
                            }).Wait();
                        }
                    }, onPluginApiError => PluginAdapter.DefaultPluginError(onPluginApiError),
                    onServerError => PluginAdapter.DefaultServerError(onServerError)).Wait();
            }
        }
    }
}

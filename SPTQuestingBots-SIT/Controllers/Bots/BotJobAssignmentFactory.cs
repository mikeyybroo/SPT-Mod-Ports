﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Models;
using Comfort.Common;
using UnityEngine;

namespace SPTQuestingBots.Controllers.Bots
{
    public static class BotJobAssignmentFactory
    {
        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static List<QuestQB> allQuests = new List<QuestQB>();
        private static Dictionary<string, List<BotJobAssignment>> botJobAssignments = new Dictionary<string, List<BotJobAssignment>>();

        public static int QuestCount => allQuests.Count;

        public static QuestQB[] FindQuestsWithZone(string zoneId) => allQuests.Where(q => q.GetObjectiveForZoneID(zoneId) != null).ToArray();
        public static bool CanMoreBotsDoQuest(this QuestQB quest) => quest.NumberOfActiveBots() < quest.MaxBots;
        
        public static void Clear()
        {
            // Only remove quests that are not based on an EFT quest template
            allQuests.RemoveAll(q => q.Template == null);

            // Remove all objectives for remaining quests. New objectives will be generated after loading the map.
            foreach (QuestQB quest in allQuests)
            {
                quest.Clear();
            }

            botJobAssignments.Clear();
        }

        public static void AddQuest(QuestQB quest)
        {
            foreach(QuestObjective objective in quest.AllObjectives)
            {
                objective.UpdateQuestObjectiveStepNumbers();
            }

            allQuests.Add(quest);
        }

        public static QuestQB FindQuest(string questID)
        {
            IEnumerable<QuestQB> matchingQuests = allQuests.Where(q => q.TemplateId == questID);
            if (matchingQuests.Count() == 0)
            {
                return null;
            }

            return matchingQuests.First();
        }

        public static void RemoveBlacklistedQuestObjectives(string locationId)
        {
            foreach (QuestQB quest in allQuests.ToArray())
            {
                foreach (QuestObjective objective in quest.AllObjectives)
                {
                    Vector3? firstPosition = objective.GetFirstStepPosition();
                    if (!firstPosition.HasValue)
                    {
                        continue;
                    }

                    // Remove quests on Lightkeeper island. Otherwise, PMC's will engage you there when they normally wouldn't on live. 
                    if ((locationId == "Lighthouse") && (firstPosition.Value.x > 120) && (firstPosition.Value.z > 325))
                    {
                        if (quest.TryRemoveObjective(objective))
                        {
                            LoggingController.LogInfo("Removing quest objective on Lightkeeper island: " + objective.ToString() + " for quest " + quest.ToString());
                        }
                        else
                        {
                            LoggingController.LogError("Could not remove quest objective on Lightkeeper island: " + objective.ToString() + " for quest " + quest.ToString());
                        }

                        // If there are no remaining objectives, remove the quest too
                        if (quest.NumberOfObjectives == 0)
                        {
                            LoggingController.LogInfo("Removing quest on Lightkeeper island: " + quest.ToString() + "...");
                            allQuests.Remove(quest);
                        }
                    }
                }
            }
        }

        public static void FailAllJobAssignmentsForBot(string botID)
        {
            if (!botJobAssignments.ContainsKey(botID))
            {
                return;
            }

            foreach (BotJobAssignment assignment in botJobAssignments[botID].Where(a => a.IsActive))
            {
                assignment.Fail();
            }
        }

        public static void InactivateAllJobAssignmentsForBot(string botID)
        {
            if (!botJobAssignments.ContainsKey(botID))
            {
                return;
            }

            foreach (BotJobAssignment assignment in botJobAssignments[botID])
            {
                assignment.Inactivate();
            }
        }

        public static int NumberOfConsecutiveFailedAssignments(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return 0;
            }

            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Reverse<BotJobAssignment>()
                .TakeWhile(a => a.Status == JobAssignmentStatus.Failed);

            return matchingAssignments.Count();
        }

        public static int NumberOfActiveBots(this QuestQB quest)
        {
            int num = 0;
            foreach (string id in botJobAssignments.Keys)
            {
                num += botJobAssignments[id]
                    .Where(a => a.Status == JobAssignmentStatus.Active)
                    .Where(a => a.QuestAssignment == quest)
                    .Count();
            }

            //LoggingController.LogInfo("Bots doing " + quest.ToString() + ": " + num);

            return num;
        }

        public static IEnumerable<QuestObjective> RemainingObjectivesForBot(this QuestQB quest, BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Bot is null", nameof(bot));
            }

            if (quest == null)
            {
                throw new ArgumentNullException("Quest is null", nameof(quest));
            }

            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return quest.AllObjectives;
            }

            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Where(a => a.QuestAssignment == quest)
                .Where(a => a.Status != JobAssignmentStatus.Archived);
            
            return quest.AllObjectives.Where(o => !matchingAssignments.Any(a => a.QuestObjectiveAssignment == o));
        }

        public static QuestObjective NearestToBot(this IEnumerable<QuestObjective> objectives, BotOwner bot)
        {
            Dictionary<QuestObjective, float> objectiveDistances = new Dictionary<QuestObjective, float>();
            foreach (QuestObjective objective in objectives)
            {
                Vector3? firstStepPosition = objective.GetFirstStepPosition();
                if (!firstStepPosition.HasValue)
                {
                    continue;
                }

                objectiveDistances.Add(objective, Vector3.Distance(bot.Position, firstStepPosition.Value));
            }

            if (objectiveDistances.Count == 0)
            {
                return null;
            }

            return objectiveDistances.OrderBy(i => i.Value).First().Key;
        }

        public static DateTime? TimeWhenLastEndedForBot(this QuestQB quest, BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return null;
            }

            // Find all of the bot's assignments with this quest that have not been archived yet
            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Where(a => a.QuestAssignment == quest)
                .Where(a => a.Status != JobAssignmentStatus.Archived)
                .Reverse<BotJobAssignment>()
                .SkipWhile(a => !a.EndTime.HasValue);

            if (!matchingAssignments.Any())
            {
                return null;
            }

            return matchingAssignments.First().EndTime;
        }

        public static double? ElapsedTimeWhenLastEndedForBot(this QuestQB quest, BotOwner bot)
        {
            DateTime? lastObjectiveEndingTime = quest.TimeWhenLastEndedForBot(bot);
            if (!lastObjectiveEndingTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - lastObjectiveEndingTime.Value).TotalSeconds;
        }

        public static DateTime? TimeWhenBotStarted(this QuestQB quest, BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return null;
            }

            // If the bot is currently doing this quest, find the time it first started
            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Reverse<BotJobAssignment>()
                .TakeWhile(a => a.QuestAssignment == quest);

            if (!matchingAssignments.Any())
            {
                return null;
            }

            return matchingAssignments.Last().EndTime;
        }

        public static double? ElapsedTimeSinceBotStarted(this QuestQB quest, BotOwner bot)
        {
            DateTime? firstObjectiveEndingTime = quest.TimeWhenBotStarted(bot);
            if (!firstObjectiveEndingTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - firstObjectiveEndingTime.Value).TotalSeconds;
        }

        public static bool CanAssignToBot(this QuestQB quest, BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Bot is null", nameof(bot));
            }

            if (quest == null)
            {
                throw new ArgumentNullException("Quest is null", nameof(quest));
            }

            // Check if the bot is eligible to do the quest
            if (!quest.CanAssignBot(bot))
            {
                //LoggingController.LogInfo("Cannot assign " + bot.GetText() + " to quest " + quest.ToString());
                return false;
            }

            // If the bot has never been assigned a job, it should be able to do the quest
            // TO DO: Could this return a false positive? 
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return true;
            }

            // Ensure the bot can do at least one of the objectives
            if (!quest.AllObjectives.Any(o => o.CanAssignBot(bot)))
            {
                //LoggingController.LogInfo("Cannot assign " + bot.GetText() + " to any objectives in quest " + quest.ToString());
                return false;
            }

            if (quest.HasBotBeingDoingQuestTooLong(bot, out double? timeDoingQuest))
            {
                return false;
            }

            // Check if at least one of the quest objectives has not been assigned to the bot
            if (quest.RemainingObjectivesForBot(bot).Count() > 0)
            {
                return true;
            }

            // Check if enough time has elasped from the bot's last assignment in the quest
            if (quest.TryArchiveIfBotCanRepeat(bot))
            {
                return true;
            }

            return false;
        }

        public static bool TryArchiveIfBotCanRepeat(this QuestQB quest, BotOwner bot)
        {
            if (!quest.IsRepeatable)
            {
                return false;
            }

            double? timeSinceQuestEnded = quest.ElapsedTimeWhenLastEndedForBot(bot);
            if (timeSinceQuestEnded.HasValue && (timeSinceQuestEnded >= ConfigController.Config.Questing.BotQuestingRequirements.RepeatQuestDelay))
            {
                LoggingController.LogInfo(bot.GetText() + " is now allowed to repeat quest " + quest.ToString());

                IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                    .Where(a => a.QuestAssignment == quest);

                foreach (BotJobAssignment assignment in matchingAssignments)
                {
                    assignment.Archive();
                }

                return true;
            }

            return false;
        }

        public static bool CanBotRepeatQuestObjective(this QuestObjective objective, BotOwner bot)
        {
            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Where(a => a.QuestObjectiveAssignment == objective);

            if (!matchingAssignments.Any())
            {
                return true;
            }
            
            // If the assignment hasn't been archived yet, not enough time has elapsed to repeat it
            if (!objective.IsRepeatable && matchingAssignments.Any(a => a.Status == JobAssignmentStatus.Completed))
            {
                return false;
            }

            return objective.IsRepeatable && matchingAssignments.All(a => a.Status == JobAssignmentStatus.Archived);
        }

        public static bool HasBotBeingDoingQuestTooLong(this QuestQB quest, BotOwner bot, out double? time)
        {
            time = quest.ElapsedTimeSinceBotStarted(bot);
            if (time.HasValue && (time >= ConfigController.Config.Questing.BotQuestingRequirements.MaxTimePerQuest))
            {
                return true;
            }

            return false;
        }

        public static BotJobAssignment GetCurrentJobAssignment(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                botJobAssignments.Add(bot.Profile.Id, new List<BotJobAssignment>());
            }

            if (DoesBotHaveNewJobAssignment(bot))
            {
                LoggingController.LogInfo("Bot " + bot.GetText() + " is now doing " + botJobAssignments[bot.Profile.Id].Last().ToString());

                if (botJobAssignments[bot.Profile.Id].Count > 1)
                {
                    BotJobAssignment lastAssignment = botJobAssignments[bot.Profile.Id].TakeLast(2).First();

                    LoggingController.LogInfo("Bot " + bot.GetText() + " was previously doing " + lastAssignment.ToString());

                    double? timeSinceBotStartedQuest = lastAssignment.QuestAssignment.ElapsedTimeSinceBotStarted(bot);
                    double? timeSinceBotLastFinishedQuest = lastAssignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(bot);

                    string startedTimeText = timeSinceBotStartedQuest.HasValue ? timeSinceBotStartedQuest.Value.ToString() : "N/A";
                    string lastFinishedTimeText = timeSinceBotLastFinishedQuest.HasValue ? timeSinceBotLastFinishedQuest.Value.ToString() : "N/A";
                    LoggingController.LogInfo("Time since first objective ended: " + startedTimeText + ", Time since last objective ended: " + lastFinishedTimeText);
                }
            }

            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                return botJobAssignments[bot.Profile.Id].Last();
            }

            LoggingController.LogWarning("Could not get a job assignment for bot " + bot.GetText());
            return null;
        }

        public static bool DoesBotHaveNewJobAssignment(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                botJobAssignments.Add(bot.Profile.Id, new List<BotJobAssignment>());
            }

            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                BotJobAssignment currentAssignment = botJobAssignments[bot.Profile.Id].Last();
                
                // Check if the bot is currently doing an assignment
                if (currentAssignment.IsActive)
                {
                    return false;
                }

                // Check if more steps are available for the bot's current assignment
                if (currentAssignment.TrySetNextObjectiveStep(false))
                {
                    return true;
                }

                //LoggingController.LogInfo("There are no more steps available for " + bot.GetText() + " in " + (currentAssignment.QuestObjectiveAssignment?.ToString() ?? "???"));
            }

            if (bot.GetNewBotJobAssignment() != null)
            {
                return true;
            }

            return false;
        }

        public static BotJobAssignment GetNewBotJobAssignment(this BotOwner bot)
        {
            // Do not select another quest objective if the bot wants to extract
            if (BotObjectiveManager.GetObjectiveManagerForBot(bot)?.DoesBotWantToExtract() == true)
            {
                return null;
            }

            // Get the bot's most recent assingment if applicable
            QuestQB quest = null;
            QuestObjective objective = null;
            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                quest = botJobAssignments[bot.Profile.Id].Last().QuestAssignment;
                objective = botJobAssignments[bot.Profile.Id].Last().QuestObjectiveAssignment;
            }

            if (quest?.HasBotBeingDoingQuestTooLong(bot, out double? timeDoingQuest) == true)
            {
                LoggingController.LogInfo(bot.GetText() + " has been performing quest " + quest.ToString() + " for " + timeDoingQuest.Value + "s and will get a new one.");
                quest = null;
                objective = null;
            }

            // Try to find a quest that has at least one objective that can be assigned to the bot
            List<QuestQB> invalidQuests = new List<QuestQB>();
            Stopwatch timeoutMonitor = Stopwatch.StartNew();
            do
            {
                // Find the nearest objective for the bot's currently assigned quest (if any)
                objective = quest?
                    .RemainingObjectivesForBot(bot)?
                    .Where(o => o.CanAssignBot(bot))?
                    .Where(o => o.CanBotRepeatQuestObjective(bot))?
                    .NearestToBot(bot);
                
                // Exit the loop if an objective was found for the bot
                if (objective != null)
                {
                    break;
                }
                if (quest != null)
                {
                    invalidQuests.Add(quest);
                }

                // If no objectives were found, select another quest
                quest = bot.GetRandomQuest(invalidQuests);

                // If a quest hasn't been found within a certain amount of time, something is wrong
                if (timeoutMonitor.ElapsedMilliseconds > ConfigController.Config.Questing.QuestSelectionTimeout)
                {
                    throw new TimeoutException("Finding a quest for " + bot.GetText() + " took too long");
                }

            } while (objective == null);

            // Once a valid assignment is selected, assign it to the bot
            BotJobAssignment assignment = new BotJobAssignment(bot, quest, objective);
            botJobAssignments[bot.Profile.Id].Add(assignment);
            return assignment;
        }

        public static IEnumerator ProcessAllQuests(Action<QuestQB> action)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action);
        }

        public static IEnumerator ProcessAllQuests<T1>(Action<QuestQB, T1> action, T1 param1)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1);
        }

        public static IEnumerator ProcessAllQuests<T1, T2>(Action<QuestQB, T1, T2> action, T1 param1, T2 param2)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1, param2);
        }

        public static QuestQB GetRandomQuest(this BotOwner bot, IEnumerable<QuestQB> invalidQuests)
        {
            // Group all valid quests by their priority number in ascending order
            var groupedQuests = allQuests
                .Where(q => !invalidQuests.Contains(q))
                .Where(q => q.NumberOfValidObjectives > 0)
                .Where(q => q.CanMoreBotsDoQuest())
                // .Where(q => q.CanAssignToBot(bot)) // TODO: Fixme -> Workaround, if there are no quests available for bots, it may start throwing exceptions like no tomorrow.
                .GroupBy
                (
                    q => q.Priority,
                    q => q,
                    (key, q) => new { Priority = key, Quests = q.ToList() }
                )
                .OrderBy(g => g.Priority);

            if (!groupedQuests.Any())
            {
                return null;
            }

            foreach (var priorityGroup in groupedQuests)
            {
                // Get the distances to the nearest and furthest objectives for each quest in the group
                Dictionary<QuestQB, Configuration.MinMaxConfig> questObjectiveDistances = new Dictionary<QuestQB, Configuration.MinMaxConfig>();
                foreach(QuestQB quest in priorityGroup.Quests)
                {
                    IEnumerable<Vector3?> objectivePositions = quest.ValidObjectives.Select(o => o.GetFirstStepPosition());
                    IEnumerable<Vector3> validObjectivePositions = objectivePositions.Where(p => p.HasValue).Select(p => p.Value);
                    IEnumerable<float> distancesToObjectives = validObjectivePositions.Select(p => Vector3.Distance(bot.Position, p));

                    questObjectiveDistances.Add(quest, new Configuration.MinMaxConfig(distancesToObjectives.Min(), distancesToObjectives.Max()));
                }

                if (questObjectiveDistances.Count == 0)
                {
                    continue;
                }

                // Calculate the maximum amount of "randomness" to apply to each quest
                double distanceRange = questObjectiveDistances.Max(q => q.Value.Max) - questObjectiveDistances.Min(q => q.Value.Min);
                int maxRandomDistance = (int)Math.Ceiling(distanceRange * ConfigController.Config.Questing.BotQuests.DistanceRandomness / 100.0);

                //string timestampText = "[" + DateTime.Now.ToLongTimeString() + "] ";
                //LoggingController.LogInfo(timestampText + "Possible quests for priority " + priorityGroup.Priority + ": " + questObjectiveDistances.Count + ", Distance Range: " + distanceRange);
                //LoggingController.LogInfo(timestampText + "Possible quests for priority " + priorityGroup.Priority + ": " + string.Join(", ", questObjectiveDistances.Select(o => o.Key.Name)));

                // Sort the quests in the group by their distance to you, with some randomness applied, in ascending order
                System.Random random = new System.Random();
                IEnumerable<QuestQB> randomizedQuests = questObjectiveDistances
                    .OrderBy(q => q.Value.Min + random.Next(-1 * maxRandomDistance, maxRandomDistance))
                    .Select(q => q.Key);

                // Use a random number to determine if the bot should be assigned to the first quest in the list
                QuestQB firstRandomQuest = randomizedQuests.First();
                if (random.Next(1, 100) <= firstRandomQuest.ChanceForSelecting)
                {
                    return firstRandomQuest;
                }
            }

            // If no quest was assigned to the bot, randomly assign a quest in the first priority group as a fallback method
            return groupedQuests.First().Quests.Random();
        }
        
        public static IEnumerable<BotJobAssignment> GetCompletedOrAchivedQuests(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return Enumerable.Empty<BotJobAssignment>();
            }

            return botJobAssignments[bot.Profile.Id].Where(a => a.IsCompletedOrArchived);
        }

        public static int NumberOfCompletedOrAchivedQuests(this BotOwner bot)
        {
            IEnumerable<BotJobAssignment> assignments = bot.GetCompletedOrAchivedQuests();

            return assignments
                .Distinct(a => a.QuestAssignment)
                .Count();
        }

        public static int NumberOfCompletedOrAchivedEFTQuests(this BotOwner bot)
        {
            IEnumerable<BotJobAssignment> assignments = bot.GetCompletedOrAchivedQuests();

            return assignments
                .Distinct(a => a.QuestAssignment)
                .Where(a => a.QuestAssignment.IsEFTQuest)
                .Count();
        }

        public static void WriteQuestLogFile(long timestamp)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Writing quest log file...");

            if (allQuests.Count == 0)
            {
                LoggingController.LogWarning("No quests to log.");
                return;
            }

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Quest Name,Objective,Steps,Min Level,Max Level,First Step Position");

            // Write a row for every objective in every quest
            foreach(QuestQB quest in allQuests)
            {
                foreach (QuestObjective objective in quest.AllObjectives)
                {
                    Vector3? firstPosition = objective.GetFirstStepPosition();
                    if (!firstPosition.HasValue)
                    {
                        continue;
                    }

                    sb.Append(quest.Name.Replace(",", "") + ",");
                    sb.Append("\"" + objective.ToString().Replace(",", "") + "\",");
                    sb.Append(objective.StepCount + ",");
                    sb.Append(quest.MinLevel + ",");
                    sb.Append(quest.MaxLevel + ",");
                    sb.AppendLine((firstPosition.HasValue ? "\"" + firstPosition.Value.ToString() + "\"" : "N/A"));
                }
            }

            string filename = ConfigController.GetLoggingPath()
                + BotQuestBuilder.PreviousLocationID.Replace(" ", "")
                + "_"
                + timestamp
                + "_quests.csv";
            
            LoggingController.CreateLogFile("quest", filename, sb.ToString());
        }

        public static void WriteBotJobAssignmentLogFile(long timestamp)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Writing bot job assignment log file...");

            if (botJobAssignments.Count == 0)
            {
                LoggingController.LogWarning("No bot job assignments to log.");
                return;
            }

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bot Name,Bot Nickname,Bot Level,Assignment Status,Quest Name,Objective Name,Step Number,Start Time,End Time");

            // Write a row for every quest, objective, and step that each bot was assigned to perform
            foreach (string botID in botJobAssignments.Keys)
            {
                foreach (BotJobAssignment assignment in botJobAssignments[botID])
                {
                    sb.Append(assignment.BotName + ",");
                    sb.Append("\"" + assignment.BotNickname.Replace(",", "") + "\",");
                    sb.Append(assignment.BotLevel + ",");
                    sb.Append(assignment.Status.ToString() + ",");
                    sb.Append("\"" + (assignment.QuestAssignment?.ToString()?.Replace(",", "") ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.QuestObjectiveAssignment?.ToString()?.Replace(",", "") ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.QuestObjectiveStepAssignment?.StepNumber?.ToString() ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.StartTime?.ToLongTimeString() ?? "N/A") + "\",");
                    sb.AppendLine("\"" + (assignment.EndTime?.ToLongTimeString() ?? "N/A") + "\",");
                }
            }

            string filename = ConfigController.GetLoggingPath()
                + BotQuestBuilder.PreviousLocationID.Replace(" ", "")
                + "_"
                + timestamp
                + "_assignments.csv";

            LoggingController.CreateLogFile("bot job assignment", filename, sb.ToString());
        }
    }
}

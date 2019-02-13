﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Npgsql;
using NpgsqlTypes;
using WB.Services.Export.Infrastructure;

namespace WB.Services.Export.InterviewDataStorage.InterviewDataExport
{
    public class InterviewDataExportCommandBuilder : IInterviewDataExportCommandBuilder
    {
        public DbCommand CreateUpdateValueForTable(string tableName, RosterTableKey rosterTableKey, IEnumerable<UpdateValueInfo> updateValueInfos)
        {
            if (!updateValueInfos.Any())
                throw new ArgumentException("don't have any update value info to create command");

            bool isTopLevel = rosterTableKey.RosterVector == null || rosterTableKey.RosterVector.Length == 0;

            NpgsqlCommand updateCommand = new NpgsqlCommand();

            var setValues = string.Empty;

            int index = 0;
            foreach (var updateValueInfo in updateValueInfos)
            {
                index++;
                setValues += $"   \"{updateValueInfo.ColumnName}\" = @answer{index},";

                if (updateValueInfo.Value == null)
                    updateCommand.Parameters.AddWithValue($"@answer{index}", DBNull.Value);
                else
                    updateCommand.Parameters.AddWithValue($"@answer{index}", updateValueInfo.ValueType, updateValueInfo.Value);
            }
            setValues = setValues.TrimEnd(',');

            var text = $"UPDATE \"{tableName}\" " +
                       $"   SET {setValues}" +
                       $" WHERE {InterviewDatabaseConstants.InterviewId} = @interviewId";

            updateCommand.Parameters.AddWithValue("@interviewId", NpgsqlDbType.Uuid, rosterTableKey.InterviewId);

            if (!isTopLevel)
            {
                text += $"   AND {InterviewDatabaseConstants.RosterVector} = @rosterVector;";
                updateCommand.Parameters.AddWithValue("@rosterVector", NpgsqlDbType.Array | NpgsqlDbType.Integer, rosterTableKey.RosterVector.Coordinates.ToArray());
            }

            updateCommand.CommandText = text;
            return updateCommand;
        }


        public DbCommand CreateInsertInterviewCommandForTable(string tableName, IEnumerable<Guid> interviewIds)
        {
            if (!interviewIds.Any())
                throw new ArgumentException("don't have any interview id to create command");

            var text = $"INSERT INTO \"{tableName}\" ({InterviewDatabaseConstants.InterviewId})" +
                       $"           VALUES ";
            NpgsqlCommand insertCommand = new NpgsqlCommand();

            int index = 0;
            foreach (var interviewId in interviewIds)
            {
                index++;
                text += $" (@interviewId{index}),";
                insertCommand.Parameters.AddWithValue($"@interviewId{index}", NpgsqlDbType.Uuid, interviewId);
            }

            text = text.TrimEnd(',');
            text += ";";

            insertCommand.CommandText = text;
            return insertCommand;
        }

        public DbCommand CreateDeleteInterviewCommandForTable(string tableName, IEnumerable<Guid> interviewIds)
        {
            if (!interviewIds.Any())
                throw new ArgumentException("don't have any interview id to create command");

            var text = $"DELETE FROM \"{tableName}\" " +
                       $"      WHERE {InterviewDatabaseConstants.InterviewId} = ANY(@interviewIds);";
            NpgsqlCommand deleteCommand = new NpgsqlCommand(text);
            deleteCommand.Parameters.AddWithValue("@interviewIds", NpgsqlDbType.Array | NpgsqlDbType.Uuid, interviewIds.ToArray());
            return deleteCommand;
        }

        public DbCommand CreateAddRosterInstanceForTable(string tableName, IEnumerable<RosterTableKey> rosterInfos)
        {
            if (!rosterInfos.Any())
                throw new ArgumentException("don't have any roster info to create command");

            var text = $"INSERT INTO \"{tableName}\" ({InterviewDatabaseConstants.InterviewId}, {InterviewDatabaseConstants.RosterVector})" +
                       $"           VALUES";

            NpgsqlCommand insertCommand = new NpgsqlCommand();
            int index = 0;
            foreach (var rosterInfo in rosterInfos)
            {
                index++;
                text += $"       (@interviewId{index}, @rosterVector{index}),";
                insertCommand.Parameters.AddWithValue($"@interviewId{index}", NpgsqlDbType.Uuid, rosterInfo.InterviewId);
                insertCommand.Parameters.AddWithValue($"@rosterVector{index}", NpgsqlDbType.Array | NpgsqlDbType.Integer, rosterInfo.RosterVector.Coordinates.ToArray());
            }

            text = text.TrimEnd(',');
            text += ";";

            insertCommand.CommandText = text;
            return insertCommand;
        }

        public DbCommand CreateRemoveRosterInstanceForTable(string tableName, IEnumerable<RosterTableKey> rosterInfos)
        {
            if (!rosterInfos.Any())
                throw new ArgumentException("don't have any roster info to create command");

            var text = $"DELETE FROM \"{tableName}\" " +
                       $"      WHERE ";
            NpgsqlCommand deleteCommand = new NpgsqlCommand();

            int index = 0;
            foreach (var rosterInfo in rosterInfos)
            {
                index++;
                text += $" (" +
                        $"   {InterviewDatabaseConstants.InterviewId} = @interviewId{index}" +
                        $"   AND {InterviewDatabaseConstants.RosterVector} = @rosterVector{index}" +
                        $" ) " +
                        $" OR";
                deleteCommand.Parameters.AddWithValue($"@interviewId{index}", NpgsqlDbType.Uuid, rosterInfo.InterviewId);
                deleteCommand.Parameters.AddWithValue($"@rosterVector{index}", NpgsqlDbType.Array | NpgsqlDbType.Integer, rosterInfo.RosterVector.Coordinates.ToArray());
            }

            text = text.TrimEnd('O', 'R');
            text += ";";

            deleteCommand.CommandText = text;

            return deleteCommand;
        }

        public IEnumerable<DbCommand> BuildCommandsInExecuteOrderFromState(InterviewDataState state)
        {
            var commands = new List<DbCommand>();

            foreach (var tableWithAddInterviews in state.GetInsertInterviewsData())
                commands.Add(CreateInsertInterviewCommandForTable(tableWithAddInterviews.TableName, tableWithAddInterviews.InterviewIds));

            foreach (var tableWithAddRosters in state.GetRemoveRostersBeforeInsertNewInstancesData())
                commands.Add(CreateRemoveRosterInstanceForTable(tableWithAddRosters.TableName, tableWithAddRosters.RosterLevelInfo));

            foreach (var tableWithAddRosters in state.GetInsertRostersData())
                commands.Add(CreateAddRosterInstanceForTable(tableWithAddRosters.TableName, tableWithAddRosters.RosterLevelInfo));

            foreach (var updateValueInfo in state.GetUpdateValuesData())
            {
                var updateValueCommand = CreateUpdateValueForTable(updateValueInfo.TableName,
                    updateValueInfo.RosterLevelTableKey,
                    updateValueInfo.UpdateValuesInfo);
                commands.Add(updateValueCommand);
            }

            foreach (var tableWithRemoveRosters in state.GetRemoveRostersData())
                commands.Add(CreateRemoveRosterInstanceForTable(tableWithRemoveRosters.TableName, tableWithRemoveRosters.RosterLevelInfo));

            foreach (var tableWithRemoveInterviews in state.GetRemoveInterviewsData())
                commands.Add(CreateDeleteInterviewCommandForTable(tableWithRemoveInterviews.TableName, tableWithRemoveInterviews.InterviewIds));

            return commands;
        }
    }
}

using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Engine
{
    public static class PlayerDataMapper
    {
        private static readonly string _connectionString = "Data Source=(local);Initial Catalog=SuperAdventure;Integrated Security=True;";

        public static Player CreateFromDatabase()
        {
            try
            {
                //Connect to database
                using(SqlConnection connection = new SqlConnection(_connectionString))
                {
                    //Open connection
                    connection.Open();

                    Player player;

                    using(SqlCommand savedGameCommand = connection.CreateCommand())
                    {
                        savedGameCommand.CommandType = CommandType.Text;

                        savedGameCommand.CommandText = "SELECT TOP 1 * FROM SavedGame";

                        SqlDataReader reader = savedGameCommand.ExecuteReader();

                        //Check if the query did not return a row/record of data
                        if(!reader.HasRows)
                        {
                            //There is no data in SavedGame table. so return null
                            return null;
                        }
                        //Get record from data reader
                        reader.Read();

                        //Get column values for the record
                        int currentHitPoints = (int)reader["CurrentHitPoints"];
                        int maximumHitPoints = (int)reader["MaximumHitPoints"];
                        int gold = (int)reader["Gold"];
                        int experiencePoints = (int)reader["ExperiencePoints"];
                        int currentLocationID = (int)reader["CurrentLocationID"];

                        //Create the Player object, with the saved games values
                        player = Player.CreatePlayerFromDatabase(currentHitPoints, maximumHitPoints, gold, experiencePoints, currentLocationID);
                    }

                    //Read records from Quest table, and add them to player
                    using(SqlCommand questCommand = connection.CreateCommand())
                    {
                        questCommand.CommandType = CommandType.Text;
                        questCommand.CommandText = "SELECT * FROM Quest";

                        SqlDataReader reader = questCommand.ExecuteReader();

                        if(reader.HasRows)
                        {
                            while(reader.Read())
                            {
                                int questID = (int)reader["QuestID"];
                                bool isCompleted = (bool)reader["IsCompleted"];

                                //Build the PlayerQuest item, for this row
                                PlayerQuest playerQuest = new PlayerQuest(World.QuestByID(questID));
                                playerQuest.IsCompleted = isCompleted;

                                //Add the PlayerQuest to inventory
                                player.Quests.Add(playerQuest);
                            }
                        }
                    }

                    using(SqlCommand inventoryCommand = connection.CreateCommand())
                    {
                        inventoryCommand.CommandType = CommandType.Text;
                        inventoryCommand.CommandText = "SELECT * FROM Inventory";

                        SqlDataReader reader = inventoryCommand.ExecuteReader();

                        if(reader.HasRows)
                        {
                            while(reader.Read())
                            {
                                int inventoryItemID = (int)reader["InventoryItemID"];
                                int quantity = (int)reader["Quantity"];

                                //Add item to inventory
                                player.AddItemToInventory(World.ItemByID(inventoryItemID), quantity);
                            }
                        }
                    }

                    //Now the player has been built from the database, return it.
                    return player;
                }
            }
            catch(Exception ex)
            {
                //Ignore error. If there is an error, it will return "null" player.
            }

            return null;
        }

        public static void SavaToDatabase(Player player)
        {
            try
            {
                using(SqlConnection connection = new SqlConnection(_connectionString))
                {
                    //Open the connection
                    connection.Open();

                    using(SqlCommand existingRowCountCommand = connection.CreateCommand())
                    {
                        existingRowCountCommand.CommandType = CommandType.Text;
                        existingRowCountCommand.CommandText = "SELECT count(*) FROM SavedGame";

                        //Use ExcuteScalar when your query will return one value
                        int existingRowCount = (int)existingRowCountCommand.ExecuteScalar();
                    
                        if(existingRowCount == 0)
                        {
                            //There is no existing row, so do an INSERT
                            using(SqlCommand insertSavedGame = connection.CreateCommand())
                            {
                                insertSavedGame.CommandType = CommandType.Text;
                                insertSavedGame.CommandText = "INSERT INTO SavedGame " + "(CurrentHitPoints, MaximumHitPoints, Gold, ExperiencePoints, CurrentLocationID) " +
                                    "VALUES " + "(@CurrentHitPoints, @MaximumHitPoints, @Gold, @ExperiencePoints, @CurrentLocationID)";

                                //Pass the values from the player object, to the SQL query
                                insertSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitpoints;
                                insertSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                insertSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                insertSavedGame.Parameters["@Gold"].Value = player.Gold;
                                insertSavedGame.Parameters.Add("@ExperinecePoints", SqlDbType.Int);
                                insertSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                insertSavedGame.Parameters.Add("@CurrentLocationID", SqlDbType.Int);
                                insertSavedGame.Parameters["@CurrentLocationID"].Value = player.CurrentLocation.ID;

                                //Perform SQL Command
                                //Use ExcuteNonQuery, because this query does not return any results.
                                insertSavedGame.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            //There is an existing row, so do an UPDATE
                            using (SqlCommand updateSavedGane = connection.CreateCommand())
                            {
                                updateSavedGane.CommandType = CommandType.Text;
                                updateSavedGane.CommandText = "UPDATE SavedGame" + "SET CurrentHitPoints = @CurrentHitPoints, " + "MaximumHitPoints = @MaximumHitPoints, " + "Gold = @Gold, " + "ExperiencePoints = @ExperiencePoints, " + "CurrentLocationID = @CurrentLocationID";

                                // Pass the values from the player object, to the SQL query, using parameters
                                // Using parameters helps make your program more secure.
                                // It will prevent SQL injection attacks.
                                updateSavedGane.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                updateSavedGane.Parameters["@CurrentHitPoints"].Value = player.CurrentHitpoints;
                                updateSavedGane.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                updateSavedGane.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                updateSavedGane.Parameters.Add("@Gold", SqlDbType.Int);
                                updateSavedGane.Parameters["@Gold"].Value = player.Gold;
                                updateSavedGane.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                updateSavedGane.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                updateSavedGane.Parameters.Add("@CurrentLocationID", SqlDbType.Int);
                                updateSavedGane.Parameters["@CurrentLocationID"].Value = player.CurrentLocation.ID;

                                //Perform SQL command.
                                //Use ExecuteNonQuery, because this query does not return any results
                                updateSavedGane.ExecuteNonQuery();
                            }
                        }
                    }

                    // The Quest and Inventory tables might have more, or less, rows in the database
                    // than what the player has in their properties.
                    // So, when we save the player's game, we will delete all the old rows
                    // and add in all new rows.
                    // This is easier than trying to add/delete/update each individual rows

                    // Delete existing Quest rows
                    using(SqlCommand deleteQuestsCommand = connection.CreateCommand())
                    {
                        deleteQuestsCommand.CommandType = CommandType.Text;
                        deleteQuestsCommand.CommandText = "DELETE FROM Quest";

                        deleteQuestsCommand.ExecuteNonQuery();
                    }

                    //Insert Quest rows, from the player object
                    foreach(PlayerQuest playerQuest in player.Quests)
                    {
                        using(SqlCommand insertQuestCommand = connection.CreateCommand())
                        {
                            insertQuestCommand.CommandType = CommandType.Text;
                            insertQuestCommand.CommandText = "INSERT INTO Quest(QuestID, IsCompleted) VALUES (@QuestID, @IsCompleted)";

                            insertQuestCommand.Parameters.Add("@QuestID", SqlDbType.Int);
                            insertQuestCommand.Parameters["@QuestID"].Value = playerQuest.Details.ID;
                            insertQuestCommand.Parameters.Add("@IsCompleted", SqlDbType.Bit);
                            insertQuestCommand.Parameters["@IsCompleted"].Value = playerQuest.IsCompleted;

                            insertQuestCommand.ExecuteNonQuery();
                        }
                    }

                    //Delete existing Inventory rows
                    using(SqlCommand deleteInventoryCommand = connection.CreateCommand())
                    {
                        deleteInventoryCommand.CommandType = CommandType.Text;
                        deleteInventoryCommand.CommandText = "DELETE FROM Inventory";

                        deleteInventoryCommand.ExecuteNonQuery();
                    }

                    //Insert Inventory rows, from the player object
                    foreach(InventoryItem inventoryItem in player.Inventory)
                    {
                        using(SqlCommand insertInventoryCommand = connection.CreateCommand())
                        {
                            insertInventoryCommand.CommandType = CommandType.Text;
                            insertInventoryCommand.CommandText = "INSERT INTO Inventory(InventoryItemID, Quantity) VALUES (@InventoryItemID, @Quantity)";

                            insertInventoryCommand.Parameters.Add("@InventoryItemID", SqlDbType.Int);
                            insertInventoryCommand.Parameters["@InventoryItemID"].Value = inventoryItem.Details.ID;
                            insertInventoryCommand.Parameters.Add("@Quantity", SqlDbType.Int);
                            insertInventoryCommand.Parameters["Quantity"].Value = inventoryItem.Quantity;

                            insertInventoryCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }
    }
}
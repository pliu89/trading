using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyHubs
{
    using UV.Lib.IO.Xml;            // IStringifiable
    using UV.Lib.Hubs;
    using UV.Lib.DatabaseReaderWriters.Queries;
    using UV.Lib.DatabaseReaderWriters;

    /// <summary>
    /// </summary>
    public static class StrategyMaker
    {

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //string filePath = string.Format("{0}{1}", this.Info.UserConfigPath, configFileName);
        //
        //
        public static bool TryCreateFromFile(string filePath, LogHub aLog, out List<Strategy> strategyList)
        {
            strategyList = new List<Strategy>();             // place to put new strategies
            try
            {
                List<IStringifiable> iStringObjects;
                using (StringifiableReader reader = new StringifiableReader(filePath))
                {
                    iStringObjects = reader.ReadToEnd();
                }
                if (aLog != null)
                {
                    aLog.NewEntry(LogLevel.Major, "StrategyMaker: Created {0} iStringifiable objects from {1}", iStringObjects.Count, filePath.Substring(filePath.LastIndexOf("\\") + 1));
                }
                foreach (IStringifiable iStrObj in iStringObjects)
                    if (iStrObj is Strategy)
                        strategyList.Add((Strategy)iStrObj);
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Exception: {0}\r\nContinue? \r\n{1}", e.Message,e.StackTrace);
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(msg.ToString(), "StrategyMaker.TryCreateFromFile", System.Windows.Forms.MessageBoxButtons.OKCancel);
                if (aLog != null)
                {
                    aLog.NewEntry(LogLevel.Major, "StrategyMaker: Exception {0}", e.Message);
                }
                return result == System.Windows.Forms.DialogResult.OK;
            }
            return true;
        }// TryCreateFromFile()
        //
        //
        //
        // ********************************************************
        // ****              TryCreateFromDatabase             ****
        // ********************************************************
        /// <summary>
        /// Called by the strategy hub thread to create strategies for a given group id.
        /// </summary>
        /// <param name="groupId">List of groups from which strategies are to be created. If null, no groups are loaded.</param>
        /// <param name="strategyIds">List of strategy ids to be created. If null, no strategyIds added.</param>
        /// <param name="aLog"></param>
        /// <param name="dataBaseReader"></param>
        /// <param name="strategyList"></param>
        /// <returns></returns>
        public static bool TryCreateFromDatabase(List<int> groupId, List<int> strategyIds, LogHub aLog, DatabaseReaderWriter dataBaseReader, out List<Strategy> strategyList)
        {
            strategyList = new List<Strategy>();                                    // place to put new strategies
            List<int> strategyIdList = new List<int>();                             // List of strategy ids we are going to create.            


            try
            {
                //
                // Request strategy Ids from groupIds provided by user.
                //
                StrategiesQuery strategyQuery = new StrategiesQuery(groupId,strategyIds);
                if (!dataBaseReader.SubmitSync(strategyQuery))
                {
                    aLog.NewEntry(LogLevel.Error, "TryCreateFromDatabase: StrategiesQuery failed, cannot create strategies.");
                    return false;
                }
                foreach (StrategyQueryItem aStrategyResult in strategyQuery.Results)
                    strategyIdList.Add(aStrategyResult.StrategyId);                 // create list of all id's we care about.
                //
                // Request all engines for those strategy ids.
                //
                StrategyEnginesQuery engineQuery = new StrategyEnginesQuery(strategyIdList);
                if (!dataBaseReader.SubmitSync(engineQuery))
                {
                    aLog.NewEntry(LogLevel.Error, "TryCreateFromDatabase: query failed try to find engines for {0} strategies, cannot create strategy", strategyIdList.Count);
                    return false;
                }

                // Workspace
                Dictionary<int, IStringifiable> enginesWithIds = new Dictionary<int, IStringifiable>();
                List<IStringifiable> enginesWithOutIds = new List<IStringifiable>();
                Dictionary<IStringifiable,StrategyEnginesQueryItem> itemList = new Dictionary<IStringifiable,StrategyEnginesQueryItem>();
                Dictionary<string, string> attributes = new Dictionary<string, string>();

                //
                // Now create each strategy
                //
                foreach (StrategyQueryItem strategyItem in strategyQuery.Results)
                {
                    int strategyId = strategyItem.StrategyId;
                    
                    //
                    // Search for its engines, create them.
                    //
                    foreach (StrategyEnginesQueryItem item in engineQuery.Results)// Now create all its engines.
                    {
                        if (item.StrategyId == strategyId)
                        {   // This is an engine for this strategy.                            
                            // Validate that we can create this object.
                            Type type;
                            if ( ! Stringifiable.TryGetType( item.EngineType, out type))
                            {
                                aLog.NewEntry(LogLevel.Error, "TryCreateFromDatabase: {0} unknown type in strategy {1}.",item.EngineType,strategyId);
                                return false;
                            }
                            if ( ! typeof(IStringifiable).IsAssignableFrom(type) )
                            {
                                aLog.NewEntry(LogLevel.Error, "TryCreateFromDatabase: Type {0} does not implement IStringifiable!", type.FullName);
                                return false;
                            }
                            // Create the new engine now.
                            IStringifiable newObj = (IStringifiable)Activator.CreateInstance(type); 
                            attributes.Clear();                             // empty this for next use.
                            StringifiableReader.TryAddAllAttributes(item.AttributeString, ref attributes);
                            newObj.SetAttributes(attributes);               // load its attributes.
                            

                            // Store this engine in appropriate list.
                            if ( item.EngineId >= 0)
                            {   // This engine has Id#
                                if (enginesWithIds.ContainsKey(item.EngineId))
                                {
                                    aLog.NewEntry(LogLevel.Error, "TryCreateFromDatabase: EngineIds must be unique. Found dupe engineIds in strategy #{0}.", strategyId);
                                    return false;
                                }
                                else
                                {
                                    enginesWithIds.Add(item.EngineId, newObj);
                                    itemList.Add(newObj,item);              // store the info about this new object
                                }
                            }
                            else
                            {   // This engine has NO Id#.  (Which only means it can't be someone's parent.)
                                enginesWithOutIds.Add(newObj);
                                itemList.Add(newObj,item);                  // store the info about this new object
                            }
                        }// strategyId
                    }// next engine in list

                    //
                    // Now create the strategy.
                    //
                    Strategy newStrategy = new Strategy();
                    attributes.Clear();                                     // empty this now
                    StringifiableReader.TryAddAllAttributes(strategyItem.AttributeString, ref attributes);  // extract attributes from string.
                    ((IStringifiable) newStrategy).SetAttributes(attributes);// load its attributes.
                    newStrategy.QueryItem = strategyItem;                   // give the strategy his database row, rather than giving him its contents.
                    //newStrategy.SqlId = strategyItem.StrategyId;
                    //newStrategy.StartTime = strategyItem.StartTime;
                    //newStrategy.EndTime = strategyItem.EndTime;
                    newStrategy.Name = strategyItem.Name;
                    
                    //
                    // Add engines to their parents.
                    //
                    // Now that we have the strategy AND collected all of its engines, 
                    // add engines to their parents, and then finally add engines without parents to the strategy.
                    // Notes:
                    //      1) Engines without a parent, have strategy as its parent.
                    //      2) Parent engines NEED to have an id, otherwise we couldn't know who their children are!
                    //      3) Children can have Id numbers or not... who knows.
                    // Search both lists for children.
                    foreach (IStringifiable engine in enginesWithOutIds)
                    {
                        StrategyEnginesQueryItem item;
                        IStringifiable parentEngine;
                        if ( itemList.TryGetValue(engine,out item) && item.ParentEngineId >= 0)
                        {   // This engine has a parent.
                            if ( enginesWithIds.TryGetValue(item.ParentEngineId,out parentEngine) )
                                parentEngine.AddSubElement( engine );       // added child to parent!
                        }
                        else
                        {   // This engine has NO parent!  It's parent is therefore the strategy!
                            ((IStringifiable) newStrategy).AddSubElement(engine);
                        }
                    }// next engine
                    foreach (IStringifiable engine in enginesWithIds.Values)
                    {
                        StrategyEnginesQueryItem item;
                        IStringifiable parentEngine;
                        if ( itemList.TryGetValue(engine,out item) && item.ParentEngineId >= 0)
                        {   // This engine has a parent.
                            if ( enginesWithIds.TryGetValue(item.ParentEngineId,out parentEngine) )
                                parentEngine.AddSubElement( engine );       // added child to parent!
                        }
                        else
                        {
                            ((IStringifiable) newStrategy).AddSubElement(engine);
                        }
                    }// next engine
                    
                    strategyList.Add(newStrategy);                          // Store new strategy for output.

                    // Clean up
                    enginesWithIds.Clear();
                    enginesWithOutIds.Clear();
                    itemList.Clear();
                }//next strategyId
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Exception: {0}\r\nContinue?", e.Message);
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(msg.ToString(),
                    "StrategyMaker.TryCreateFromFile", System.Windows.Forms.MessageBoxButtons.OKCancel);
                if (aLog != null)
                {
                    aLog.NewEntry(LogLevel.Major, "StrategyMaker: Exception {0}", e.Message);
                }
                return result == System.Windows.Forms.DialogResult.OK;
            }
            return true;
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }//end class
}

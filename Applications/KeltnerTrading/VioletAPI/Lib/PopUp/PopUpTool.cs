using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace VioletAPI.Lib.PopUp
{
    using TradingHelper;

    public class PopUpTool
    {

        #region Members
        private Dictionary<string, string> m_VariablesChanged = null;
        private Dictionary<int, string> m_PopUpDictionary = null;
        public event EventHandler DialogUserComplete;
        #endregion


        #region Constructor
        public PopUpTool()
        {
            m_VariablesChanged = new Dictionary<string, string>();
            m_PopUpDictionary = new Dictionary<int, string>();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Show dialog by dialog type, dialog ID, topic and text.
        /// </summary>
        /// <param name="dialogType"></param>
        /// <param name="dialogID"></param>
        /// <param name="topic"></param>
        /// <param name="text"></param>
        public void ShowDialog(DialogType dialogType, int dialogID, string topic, string text)
        {
            if (!m_PopUpDictionary.ContainsKey(dialogID))
                m_PopUpDictionary.Add(dialogID, text);
            else
            {
                if (dialogID >= 0)
                    return;
            }

            object[] objects = new object[4];
            objects[0] = dialogType;
            objects[1] = dialogID;
            objects[2] = topic;
            objects[3] = text;
            ThreadPool.QueueUserWorkItem(new WaitCallback(WaitUserInteraction), objects);
        }
        //
        //
        /// <summary>
        /// Record trading variables method.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryRecordTradingVariablesChange(string name, string value)
        {
            if (m_VariablesChanged.ContainsKey(name))
                m_VariablesChanged[name] = value;
            else
                m_VariablesChanged.Add(name, value);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Variables changed by user are listed below:");
            foreach (string variableName in m_VariablesChanged.Keys)
            {
                stringBuilder.AppendFormat("{0} is changed to {1}.\r\n", variableName, m_VariablesChanged[variableName]);
            }
            string text = stringBuilder.ToString();
            ShowDialog(DialogType.VariablesChangedNotice, DialogIDMap.VariablesChangedNotice, "Variables Changed", text);
            return true;
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// Interact with user by dialog.
        /// </summary>
        /// <param name="state"></param>
        private void WaitUserInteraction(object state)
        {
            // The schema for the data object is 1:dialogType,2:dialog ID,3:topic and 4:text.
            object[] objects = (object[])state;
            DialogType dialogType = (DialogType)objects[0];
            int dialogID = (int)objects[1];
            string topic = (string)objects[2];
            string text = (string)objects[3];
            System.Windows.Forms.DialogResult dialogResult;
            object[] outputObjects = null;
            switch (dialogType)
            {
                case DialogType.InitialPukeConfirm:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.OK);
                    outputObjects = new object[1];
                    outputObjects[0] = dialogType;
                    break;
                case DialogType.InitialFadeLevelReached:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.YesNo);
                    outputObjects = new object[3];
                    outputObjects[0] = dialogType;
                    if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                        outputObjects[1] = true;
                    else
                        outputObjects[1] = false;
                    if (text.Contains("long"))
                    {
                        outputObjects[2] = TradeSide.Buy;
                    }
                    else if (text.Contains("short"))
                    {
                        outputObjects[2] = TradeSide.Sell;
                    }
                    else
                    {
                        outputObjects[2] = TradeSide.Unknown;
                    }
                    break;
                case DialogType.InitialEntryLevelReached:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.YesNo);
                    outputObjects = new object[3];
                    outputObjects[0] = dialogType;
                    if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                        outputObjects[1] = true;
                    else
                        outputObjects[1] = false;
                    if (text.Contains("long"))
                    {
                        outputObjects[2] = TradeSide.Buy;
                    }
                    else if (text.Contains("short"))
                    {
                        outputObjects[2] = TradeSide.Sell;
                    }
                    else
                    {
                        outputObjects[2] = TradeSide.Unknown;
                    }
                    break;
                case DialogType.PositionValidationYesNo:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.YesNo);
                    outputObjects = new object[2];
                    outputObjects[0] = dialogType;
                    if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                        outputObjects[1] = true;
                    else
                        outputObjects[1] = false;
                    break;
                case DialogType.PositionValidationInput:
                    int correctPosition = 0;
                    string inputPosition = null;
                    while (string.IsNullOrEmpty(inputPosition))
                    {
                        inputPosition = Microsoft.VisualBasic.Interaction.InputBox(text, topic, "0");
                    }
                    outputObjects = new object[2];
                    outputObjects[0] = dialogType;
                    if (Int32.TryParse(inputPosition, out correctPosition))
                    {
                        outputObjects[1] = correctPosition;
                    }
                    else
                    {
                        outputObjects[1] = 0;
                    }
                    break;
                case DialogType.VariablesChangedNotice:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.OK);
                    outputObjects = new object[1];
                    outputObjects[0] = dialogType;
                    break;
                case DialogType.StopOrderTriggered:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.OK);
                    outputObjects = new object[1];
                    outputObjects[0] = dialogType;
                    break;
                case DialogType.OverFillsStopLoss:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.OK);
                    outputObjects = new object[1];
                    outputObjects[0] = dialogType;
                    break;
                case DialogType.ExchangeOpenCloseNotice:
                    dialogResult = System.Windows.Forms.MessageBox.Show(text, topic, System.Windows.Forms.MessageBoxButtons.OK);
                    outputObjects = new object[1];
                    outputObjects[0] = dialogType;
                    break;
            }
            
            if (m_PopUpDictionary.ContainsKey(dialogID))
                m_PopUpDictionary.Remove(dialogID);

            PopUpEventArgs eventArgs = new PopUpEventArgs();
            eventArgs.Data = outputObjects;
            if (DialogUserComplete != null)
                DialogUserComplete(this, eventArgs);
        }
        #endregion

    }
}

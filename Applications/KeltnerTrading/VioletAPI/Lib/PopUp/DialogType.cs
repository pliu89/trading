using System;

namespace VioletAPI.Lib.PopUp
{
    /// <summary>
    /// Enumeration of dialog type.
    /// </summary>
    public enum DialogType
    {
        InitialEntryLevelReached,
        InitialFadeLevelReached,
        InitialPukeConfirm,
        PositionValidationYesNo,
        PositionValidationInput,
        VariablesChangedNotice,
        StopOrderTriggered,
        OverFillsStopLoss,
        ExchangeOpenCloseNotice
    }
}

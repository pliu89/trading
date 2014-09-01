using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;
using System.Drawing;

namespace UV.Lib.FrontEnds.Utilities
{
    public static class ControlTools
    {

        #region Async Control Functions
        // *****************************************************************
        // ****              Asynch Control Functions                   ****
        // *****************************************************************
        //
        //
        //
        //
        // ****             Set Check()             ****
        //
        /// <summary>
        /// Allows called to asynchronously check a control.
        /// </summary>
        /// <param name="parentForm"></param>
        /// <param name="menuItem"></param>
        /// <param name="isCheck">Whether to set/reset check mark.</param>
        public static void SetCheck(Control parentForm, ToolStripMenuItem menuItem, bool isCheck)
        {
            if (parentForm == null || parentForm.Disposing)
                return;
            if (parentForm.InvokeRequired)
                parentForm.Invoke(new Action<ToolStripMenuItem>((c) => c.Checked = isCheck), menuItem);
            else
                menuItem.Checked = isCheck;
        }//SetCheck()
        //
        //
        //
        // ****             Set Text()              ****
        //
        /// <summary>
        /// Caller can set Text property of control asynchronously.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="text"></param>
        public static void SetText(Control control, string text)
        {
            if (control == null || control.Disposing)
                return;
            if (control.InvokeRequired)
                control.Invoke(new Action<Control>((c) => c.Text = text), control);
            else
                control.Text = text;
        }//SetText()
        //
        //
        // ****         Set BG Color()              ****
        //
        public static void SetBGColor(Control control, Color color)
        {
            if (control == null || control.Disposing)
                return;
            if (control.InvokeRequired)
                control.Invoke(new Action<Control>((c) => c.BackColor = color), control);
            else
                control.BackColor = color;
        }//SetBackGroundColor
        //
        //
        // ****         Try Swap BG Color()         ****
        //
        /// <summary>
        /// Used for making the BG Color of a control flash.  If control.BackColor equals
        /// either of the two colors provided, then it will toggle it to the other color.
        /// TODO: Generalize this to an array of colors?
        /// </summary>
        /// <param name="control"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        public static bool TrySwapBGColor(Control control, Color color1, Color color2)
        {
            Color currentColor = control.BackColor;
            if (currentColor == color1)
            {
                SetBGColor(control, color2);
                return true;
            }
            else if (currentColor == color2)
            {
                SetBGColor(control, color1);
                return true;
            }
            else
                return false;
        }//TryFlashBGColor()
        //
        //
        //
        //
        //
        #endregion//Async Control Functions


        #region NotifyIcon Functions
        // *****************************************************************
        // ****              NotifyIcon Functions                       ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// </summary>
        /// <param name="parentForm"></param>
        /// <param name="control"></param>
        /// <param name="message"></param>
        /// <param name="tipShowDuration"></param>
        public static void SetBalloonTip(Form parentForm, NotifyIcon control, string message, int tipShowDuration = 1000)
        {
            //Action<NotifyIcon> setBalloonTip = (c) => 
            if (parentForm == null || parentForm.Disposing)
                return;
            if (parentForm.InvokeRequired)
                parentForm.Invoke(new Action<NotifyIcon>((c) => { c.BalloonTipText = message; c.ShowBalloonTip(tipShowDuration); }), control);
            else
            {
                control.BalloonTipText = message;
                control.ShowBalloonTip(1000);
            }
        }//SetBalloonTip()
        //
        //
        #endregion//NotifyIcon Functions

    }
}

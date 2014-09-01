using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Talker
{
    using Misty.Lib.Products;

    using Ambre.Lib.ExcelRTD;

    public class Topic : TopicBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public string HubName = string.Empty;                   // hub to which we are subscribing.
        public InstrumentName Instrument;
        public SubscriptionType Type;

        //
        //
        #endregion// members


        #region Constructors & Creators
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private Topic(int topicID, string[] args, string currentValue) : base(topicID, args, currentValue)
        {
            
        }
        //
        //
        /// <summary>
        /// Creator provides for differnt Topic types, created dynamically based on the
        /// request arguments provided by the user.
        /// </summary>
        /// <returns>A new object that inherits from Topic and TopicBase.</returns>
        public static bool TryCreate(int topicID, string[] args, string currentValue, out Topic newTopic)
        {
            newTopic = null;
            if ( args.Length < 2 )
                return false;

            // Determine instrument
            InstrumentName instrument;
            if ( ! InstrumentName.TryDeserialize(args[0].Trim(), out instrument) )
                return false;


            // Determine subscription type.
            SubscriptionType type;
            if (!Enum.TryParse<SubscriptionType>(args[1].Trim(), out type))
                return false;

            // Create the object now, and exit.
            newTopic = new Topic(topicID, args, currentValue);
            newTopic.Instrument = instrument;
            newTopic.Type = type;

            // Add optional arguments
            if (args.Length > 2)
                newTopic.HubName = args[2];

            return true;
        }// Create()
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {            
            return string.Format("{0} {1} {2} {3}",base.ToString(),this.Instrument, this.Type, this.HubName);
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods

    }
}

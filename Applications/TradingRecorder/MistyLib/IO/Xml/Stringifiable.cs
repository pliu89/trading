using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;


namespace Misty.Lib.IO.Xml
{
    /// <summary>
    /// Tools to convert classes into XML strings.
    /// </summary>
    public static class Stringifiable
    {

        #region GetType() extension
        //
        //
        //
        // *************************************************************
        // ****                     Try GetType()                   ****
        // *************************************************************
        // Storage:
        private static Dictionary<string, Type> m_TypeCache = new Dictionary<string, Type>();       // storage of discovered type.
        //
        /// <summary>
        /// Searches all currently loaded assemblies looking for a type with the matching FullName.
        /// There is a peculiarity that not all assemblies are loaded until they are needed, so 
        /// by the time this is called, one class from each assembly better had been loaded.
        /// To ensure assembly is loaded try: 
        ///     typeof( AssemblyName.ClassName ).ToString();
        /// For example: typeof( Ambre.TTServices.MarketHubs.MarketHub ).ToString(); will load entire assembly "Ambre.TTServices"
        /// Once a new type is discovered, it is store in the above table.
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <param name="type"></param>
        /// <returns>true if type was determined.</returns>
        public static bool TryGetType(string typeFullName, out Type type)
        {
            lock (m_TypeCache)
            {
                if (!m_TypeCache.TryGetValue(typeFullName, out type))
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(typeFullName);
                        if (type != null)
                            break;
                    }
                    m_TypeCache.Add(typeFullName, type);            // this could be null if not in any assembly.
                }
            }//lock
            return (type != null);
        }// TryGetType()
        //
        //
        #endregion // TryGetType()


        #region Convert Node -> IStringifiable Object
        //
        //
        // *************************************************
        // ****             Create()               ****
        // *************************************************
        /// <summary>
        /// This is the new March 2013 approach.  It converts a node into a IStringifiable object.
        /// Nodes are created by the StringifiableReader object.
        /// </summary>
        /// <param name="node">a node to Create</param>
        /// <returns>the newly created object</returns>
        public static IStringifiable Create(Node node)
        {
            IStringifiable newObject = null;
            Type t;
            if (TryGetType(node.Name, out t))
            {
                IStringifiable obj = Activator.CreateInstance(t) as IStringifiable;
                obj.SetAttributes(node.Attributes);
                // sub elements
                foreach (IStringifiable subNode in node.SubElements)
                {
                    if (subNode is Node)
                    {
                        IStringifiable subIStringifiable = Create((Node)subNode);
                        obj.AddSubElement(subIStringifiable);
                    }
                    else
                        obj.AddSubElement(subNode);
                }
                newObject = obj;
            }
            else
                return null;
            return newObject;
        }// Create()
        //
        //
        //
        //
        public static List<IStringifiable> Create(string strigifiedObjects, bool createNodes = false)
        {
            List<IStringifiable> nodeList = null;
            byte[] byteBuffer = ASCIIEncoding.ASCII.GetBytes(strigifiedObjects);
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(byteBuffer))
            {
                using (StringifiableReader reader = new StringifiableReader(stream))
                {
                    nodeList = reader.ReadToEnd(createNodes);
                    reader.Close();
                }
                stream.Close();
            }
            return nodeList;
        }
        public static List<IStringifiable> Create(string[] stringifiedLines, bool createNodes = false)
        {
            StringBuilder msg = new StringBuilder();
            foreach (string aLine in stringifiedLines)
                msg.Append(aLine);
            return Create(msg.ToString(), createNodes);
        }
        //
        //
        //
        //
        #endregion Convert Node -> IStringifiable Object


        #region Convert Object -> XML string
        //
        //
        //
        //
        // *************************************************
        // ****             Stringify()                 ****
        // *************************************************
        /// <summary>
        /// Takes IStringifiable object and encodes it into an XML string.
        /// Attributes are obtained using IStringifiable.GetAttributes(), and sub-elements obtained
        /// using IStringifiable.GetElements().
        /// Use of Override Table:
        ///     Optionally, these functions can be overridden for specific object Types by including a string[3]
        ///     for the object Type key.  The string[3]{"typeFullName","GetAttributeFuncName","GetElementFuncName"}.
        ///     The name of the type allows one object to pretend to be another.  And the latter two functions must be 
        ///     found in the instance of the true object being called.
        /// Example of Usage:
        ///     Create the override table:
        ///         overrideTable = new Dictionary<Type, string[]>();
        ///     Add the object to override, and the names of the new functions to call.
        ///         overrideTable.Add(m_FillHub.GetType(), new string[] { string.Empty, "GetAttributesDrop", "GetElementsDrop" });
        ///     Then call this function:
        ///         string s = Stringify(m_FillHub,overrideTable);
        /// Improvements:
        ///     Can this be generalized to serialize objects generally? (At least some of them?)        
        /// </summary>
        /// <param name="target">IStringifiable object to convert.</param>
        /// <param name="overrideFunctionTable">dictionary of functions use instead of default Stringifiable interface for given Type. </param>
        /// <returns></returns>
        public static string Stringify(IStringifiable target, Dictionary<Type, string[]> overrideFunctionTable = null)
        {
            return Stringify(0, target, overrideFunctionTable);
        }
        //
        //
        //
        //
        //
        private static string Stringify(int level, IStringifiable target, Dictionary<Type, string[]> overrideFunctionTable)
        {
            if (target is IStringifiable)
            {
                StringBuilder msg = new StringBuilder();

                //
                // Get the information about this target object.                
                //
                Type targetType = target.GetType();
                string targetTypeFullName = string.Empty;
                string attributes = string.Empty;                               // default attributes is empty.
                List<IStringifiable> elements = null;                           // no sub elements.
                if (overrideFunctionTable != null && overrideFunctionTable.ContainsKey(targetType))
                {   // User has told us that this object has been overridden by other functions.
                    string[] funcNames;
                    if (overrideFunctionTable.TryGetValue(targetType, out funcNames))
                    {
                        if (funcNames.Length != 3)
                            throw new Exception("Invalid override table. Array must have length = 3.");
                        // Object type name
                        if (string.IsNullOrEmpty(funcNames[0]))
                            targetTypeFullName = targetType.FullName;           // use default name
                        else
                        {
                            targetTypeFullName = funcNames[0];                  // override with provided name.
                        }
                        // Attribute
                        if (string.IsNullOrEmpty(funcNames[1]))
                            attributes = target.GetAttributes();                // default
                        else
                        {   // User wants to override GetAttributes
                            MethodInfo mInfo = targetType.GetMethod(funcNames[1]);
                            if (mInfo.IsPublic && mInfo.GetParameters().Length == 0 && mInfo.ReturnType == typeof(string))
                                attributes = (string)mInfo.Invoke(target, null);
                            else
                                throw new Exception(string.Format("Invalid GetAttributes function {0} in {1}.", funcNames[1], targetType.Name));
                        }
                        // Elements
                        if (string.IsNullOrEmpty(funcNames[2]))
                            elements = target.GetElements();                    // default
                        else
                        {   // User wants to override GetElements()
                            MethodInfo mInfo = targetType.GetMethod(funcNames[2]);
                            if (mInfo.IsPublic && mInfo.GetParameters().Length == 0 && (mInfo.ReturnType == typeof(List<IStringifiable>)))
                                elements = (List<IStringifiable>)mInfo.Invoke(target, null);
                            else
                                throw new Exception(string.Format("Invalid GetElements function {0} in {1}.", funcNames[2], targetType.Name));
                        }
                    }
                }
                else if (target is IStringifiable)
                {   // This is the default case to use IStringifiable functions.
                    targetTypeFullName = targetType.FullName;
                    attributes = target.GetAttributes();                 // Get attributes from the target object.
                    elements = target.GetElements();
                }
                else
                {   // Object is unknown.
                    throw new Exception("Unknown object not handled by Stringify.");
                }

                //
                // Create xml string for this object.
                //
                string space = string.Empty;                                // create a nice indentation based on the depth of the object.
                if (level > 0)
                    space = "\r\n";
                for (int i = 0; i < level; ++i)
                    space = string.Format("{0}    ", space);
                msg.AppendFormat("{1}<{0}", targetTypeFullName, space);     // Create starting tag
                if (!String.IsNullOrEmpty(attributes))                      // It may have no attributes...
                    msg.AppendFormat(" {0}", attributes);
                // Now, add sub elements.
                if (elements != null && elements.Count > 0)
                {
                    msg.Append(">");                                        // Close the starting tag.
                    foreach (IStringifiable element in elements)            // add sub elements
                        msg.AppendFormat("{0}", Stringify(level + 1, element, overrideFunctionTable));
                    msg.AppendFormat("</{0}>", targetTypeFullName);         // Close this composite object with the full closing tag.
                }
                else
                    msg.Append("/>");                                       // This is the new "complete tag" for objects without sub-objects.
                return msg.ToString();
            }
            else
                return string.Empty;
        }// Stringify()
        //
        //
        #endregion// Convert objects -> strings


        #region Convert XML -> Objects - DEFUNCT
        //
        //
        //
        /*
        // *************************************************
        // ****           Destringify()                 ****
        // *************************************************
        /// <summary>
        /// Becoming defunct.   See StringifiableReader class for new approach.
        /// It can be used with a TextStream to override these functions here.
        /// </summary>
        /// <param name="serialStr"></param>
        /// <returns></returns>
        public static List<IStringifiable> DestringifyOld(string serialStr)
        {
            List<IStringifiable> objectsToReturn = new List<IStringifiable>();
            int ptr = 0;
            while (ptr < serialStr.Length)
            {
                IStringifiable newObject;
                if (TryCreateStringableObject(ref ptr, ref serialStr, out newObject))
                    objectsToReturn.Add(newObject);
            }
            return objectsToReturn;
        }//Destringify();
        //
        //
        private static bool TryCreateStringableObject(ref int ptr, ref string serialStr, out IStringifiable newObject)
        {
            newObject = null;                       // object we are creating.
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            while (ptr < serialStr.Length)
            {
                //Console.WriteLine("*" + serialStr.Substring(ptr, serialStr.Length - ptr) + "*");
                string nextTagName = PeekAtNextTag(serialStr, ref ptr);
                if (string.IsNullOrEmpty(nextTagName))
                {
                    break;
                }
                else if (nextTagName.StartsWith("/"))
                {
                    string objectName = ConsumeNextTag(serialStr, ref ptr, ref attributes);
                    break;                          // object is complete.  Break out of the while loop.
                }
                else if (newObject == null)
                {   // Create this object!
                    string objectName = ConsumeNextTag(serialStr, ref ptr, ref attributes);
                    try
                    {
                        Type t;
                        if (TryGetType(objectName, out t))
                        {
                            IStringifiable obj = Activator.CreateInstance(t) as IStringifiable;
                            obj.SetAttributes(attributes);
                            newObject = obj;
                        }
                        else
                            return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Stingifiable failed to create object. Exception {0}", ex.Message);
                        return false;
                    }
                }
                else if (newObject != null)
                {   // This must be a new sub object.
                    IStringifiable subObject = null;
                    if (TryCreateStringableObject(ref ptr, ref serialStr, out subObject))
                        newObject.AddSubElement(subObject);
                }
                else
                {   // Error!

                }
            }// Wend
            return (newObject != null);
        }
        //
        //
        private static string PeekAtNextTag(string serialStr, ref int startPtr)
        {
            string nextTagName = string.Empty;
            int ptr1 = serialStr.IndexOf('<', startPtr);    // find leading '<'
            if (ptr1 < 0)
            {   // Failed to find any tag!   
                startPtr = serialStr.Length;
                return string.Empty;
            }
            ptr1++;                                        // point to char following '<'
            while (ptr1 < serialStr.Length && Char.IsWhiteSpace(serialStr[ptr1]))
                ptr1++;                                     // now ptr1 points to non-white char after '<'
            int ptr2 = serialStr.IndexOf(' ', ptr1 + 1);       // tag name can have attributes after a space.
            int ptr3 = serialStr.IndexOf('>', ptr1 + 1);
            if (ptr2 < 0)
            {   // no following space found; no attributes in this tag.
                // Look for the ending ">" char.
                if (ptr3 < 0)
                {   // no ending char either! Error!
                    startPtr = serialStr.Length;
                    return string.Empty;
                }
                ptr2 = ptr3;                                // use ending char position.
            }
            else if (ptr3 > 0)
                ptr2 = Math.Min(ptr2, ptr3);                // use closest delimiter.
            // Found tag name.
            return serialStr.Substring(ptr1, ptr2 - ptr1).Trim();
        }//PeekAtNextTag()
        //
        //
        public static string ConsumeNextTag(string serialStr, ref int startPtr, ref Dictionary<string, string> attributes)
        {
            //
            // Read the object
            //
            string ObjectName;
            int ptr1 = serialStr.IndexOf('<', startPtr);    // find leading '<'
            if (ptr1 < 0)
            {   // Failed to locate starting tag.
                startPtr = serialStr.Length;
                return string.Empty;
            }
            int endTag = serialStr.IndexOf('>', ptr1);      // next closing tag

            ptr1++;
            int ptr2 = serialStr.IndexOf(' ', ptr1);
            if (ptr2 < endTag && ptr2 > 0)
            {
                ObjectName = serialStr.Substring(ptr1, ptr2 - ptr1).Trim();
                ptr1 = ptr2;
                // Read attributes            
                ptr2 = serialStr.IndexOf('=', ptr1);                            // find next "="
                while (ptr2 >= 0 && ptr2 < endTag)                              // There is an attribute here.
                {
                    string attKey = serialStr.Substring(ptr1, ptr2 - ptr1).Trim(); // read attribute key
                    ptr1 = ptr2 + 1;                                            // point to just after "="
                    ptr2 = serialStr.IndexOf('=', ptr1);                        // find next '=', then we have an attribute following this one.
                    if (ptr2 < 0)
                        ptr2 = endTag;                                          // it doesn't exist.
                    int ptr3 = ptr2;                                            // set up a dummy pointer
                    if (ptr3 >= 0 && ptr3 < endTag)                             
                    {   // There is an attribute following this one, we need to locate the space just before this key.
                        // Format:   .... ThisAttName=some data goes here with spaces NextAttName=other data ...
                        // Now, ptr1 is pointing to the "s" after "="; ptr3 is pointing to next "=".
                        // The space just before "NextAttName" is at:
                        string s = serialStr.Substring(0, ptr3);
                        //Console.WriteLine(s);
                        ptr3 = s.LastIndexOf(' ');                              // space just before "NextAttName"
                    }
                    else
                    {   // There is NO attribute following this one.  So attribute value ends with endTag.
                        ptr3 = endTag;
                    }
                    string attValue = serialStr.Substring(ptr1, ptr3 - ptr1).Trim();
                    ptr1 = ptr3 + 1;
                    attributes.Add(attKey, attValue);
                    //Console.WriteLine(" ");
                    //Console.WriteLine(string.Format("{0}={1}", attKey, attValue));
                }//wend
            }
            else
            {   // No attributes found.
                ObjectName = serialStr.Substring(ptr1, endTag - ptr1).Trim();
            }

            // Exit
            startPtr = endTag + 1;                                        // point to spot after the close ">"
            return ObjectName;
        }//ConsumeNextTag()
        //
        */
        //
        //
        #endregion//convert XML -> strings

    }
}

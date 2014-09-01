using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Misty.Lib.IO.Xml
{

    /// <summary>
    /// A filestream reader for IStringifiable objects.
    /// </summary>
    public class StringifiableReader : IDisposable
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // External services
        // 
        //private string m_PathName;                                      // full path name (with extension) of file containing XML.
        private Stream m_Stream = null;                             // current stream that is open

        //
        // Constant characters for XML encoding
        //
        private const byte Ascii_Space = 32;                            // 33 is lowest non-white space char.
        private const byte Ascii_LessThan = 60;
        private const byte Ascii_Equals = 61;
        private const byte Ascii_GreaterThan = 62;
        private const byte Ascii_Slash = 47;
        private const byte Ascii_Exclamation = 33;

        private const byte Ascii_LowestNonWhite = 33;                    // ! is first non-white space char
        private const byte Ascii_HighestNonWhite = 126;                    // ~ is highest ascii char we accept

        //
        // Workspace for TryReadTag()
        //
        private Dictionary<string, string> m_AttributeWorkSpace = new Dictionary<string, string>();
        private List<byte> m_ByteWorkSpace = new List<byte>(512);
        private byte[] readBuffer = new byte[1];
        

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public StringifiableReader(string filePathName)
        {
            m_Stream = new FileStream(filePathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            m_Stream.Seek(0, SeekOrigin.Begin);                                 // set starting point for read.
        }
        public StringifiableReader(Stream stream)
        {
            m_Stream = stream;
            m_Stream.Seek(0, SeekOrigin.Begin);
        }
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *******************************************************************
        // ****                     Read To End()                         ****
        // *******************************************************************
        /// <summary>
        /// Returns list of IStringifiable objects created from the entire XML file.
        /// </summary>
        /// <param name="createNode"></param>
        /// <returns></returns>
        public  List<IStringifiable> ReadToEnd(bool createNode = false)
        {
            List<IStringifiable> newObjectList = new List<IStringifiable>();
            IStringifiable nextObject = null;
            while (TryReadNext(out nextObject, createNode))
            {
                newObjectList.Add(nextObject);
            }
            return newObjectList;
        } // ReadToEnd()
        public List<Node> ReadNodesToEnd()
        {
            List<IStringifiable> newObjectList = ReadToEnd(true);
            List<Node> nodes = new List<Node>(newObjectList.Count);
            foreach (IStringifiable istringified in newObjectList)
                nodes.Add((Node)istringified);
            return nodes;
        }
        // 
        //
        // *******************************************************************
        // ****                         Try Read Next()                   ****
        // *******************************************************************
        /// <summary>
        /// Method attempts to read (as much XML as needed to create) the next ISerializable object in file stream.
        /// This methoc can be called repeatedly to obtain each successive object in file.  Returns a false at the end
        /// of the file stream.
        /// TODO: Implement reflective object creation.
        /// </summary>
        /// <param name="newObject">Newly created object</param>
        /// <param name="createNode">If call is to create stand-in pseudo node object in place of real one.</param>
        /// <returns>True signifies sucessful creation of one object, false if at end of file.</returns>
        public bool TryReadNext(out IStringifiable newObject, bool createNode = false)
        {
            newObject = null;
            List<IStringifiable> parentNodes = new List<IStringifiable>();       // each time a new instrument is created, we move down another level of depth.       
            IStringifiable aNode = null;
            string tagName;
            TagType tagType;
            Type objectType;
            Dictionary<string,string> attributes = new Dictionary<string,string>();
            while (TryReadNextTag(out tagName, out tagType, ref attributes))
            {
                switch (tagType)
                {
                    case TagType.StartTag:
                        if (!TryCreateNode(tagName, ref attributes, createNode, out aNode))
                            return false;
                        if (parentNodes.Count > 0)                                      
                            parentNodes[parentNodes.Count - 1].AddSubElement(aNode);    // Connect this node to it's parent node.
                        parentNodes.Add(aNode);                                         // Push this object onto parent list, in case we next read a child object.                        
                        break;
                    case TagType.EndTag:
                        aNode = parentNodes[parentNodes.Count-1];                       // Get the current parent node
                        if (createNode && tagName == ((Node)aNode).Name)                // 
                            parentNodes.RemoveAt(parentNodes.Count - 1);                // pop out last, since we finished it
                        else if ( Stringifiable.TryGetType(tagName,out objectType) && tagName.Equals(objectType.FullName) )
                            parentNodes.RemoveAt(parentNodes.Count - 1);                // pop out last, since we finished it
                        else
                        {   // Error!  Throw exception?

                            return false;
                        }
                        // Check exit condition.
                        if (parentNodes.Count == 0)
                        {
                            newObject = aNode;
                            return true;
                        }
                        break;
                    case TagType.CompleteTag:
                        if (!TryCreateNode(tagName, ref attributes, createNode, out aNode))
                            return false;
                        if (parentNodes.Count == 0)                                 // Check exit condition
                        {
                            newObject = aNode;
                            return true;                                            // This zero-depth object is complete!  We are done!
                        }               
                        else
                            parentNodes[parentNodes.Count - 1].AddSubElement(aNode);// connect this to its parent.
                        break;
                    case TagType.Comment:
                        break;
                    case TagType.None:
                        break;
                    default:
                        break;
                }// tagType
                // 
                attributes.Clear();
            } // wend 
            parentNodes = null;
            newObject = null;
            return false;
        } // TryReadNext()
        //
        //
        //
        // *****************************************************************
        // ****                     Create Node()                       ****
        // *****************************************************************
        private bool TryCreateNode(string tagName, ref Dictionary<string, string> attributes, bool createNode, out IStringifiable newNode)
        {
            newNode = null;
            Type objectType;
            if (createNode)
                newNode = new Node(tagName, attributes);
            else if (Stringifiable.TryGetType(tagName, out objectType))
            {
                newNode = null;
                System.Reflection.MethodInfo methodInfo = objectType.GetMethod("GetInstance");
                if (methodInfo != null && methodInfo.IsStatic)
                    newNode = (IStringifiable)methodInfo.Invoke(null, new object[0] { }) as IStringifiable;      // create object using GetInstance() 
                else 
                    newNode = (IStringifiable)Activator.CreateInstance(objectType) as IStringifiable;           // create object via constructor.
                
                if ( newNode != null)
                    newNode.SetAttributes(attributes);
            }
            else
            {   // TODO: Throw exception?!
                throw new Exception("Unknown object type.");
                //return false;
            }
            return true;
        }// CreateNode()
        //
        //
        //
        //
        // *********************************************************************
        // ****                     Try Read Next Tag()                     ****
        // *********************************************************************
        public bool TryReadNextTag(out string tagName, out TagType tagType, ref Dictionary<string,string> attributes)//, out string fullTagString)
        {
            // Initialize return variables.
            tagType = TagType.None;
            tagName = string.Empty;                                 // First string after tag starts.
            string debugString = string.Empty;                      // convenience for debugging messages only

            // Create some local variables.
            int ptrStart = 0;
            string str;
            m_ByteWorkSpace.Clear();                                // clear the workspace.            
            m_AttributeWorkSpace.Clear();


            // Start reading file
            TagPhase phase = TagPhase.Unstarted;
            m_Stream.Seek(0, SeekOrigin.Current);
            long position = m_Stream.Position;
            while (position < m_Stream.Length && phase!=TagPhase.Ended && phase!=TagPhase.SyntaxError)
            {
                int byteRead = m_Stream.Read(readBuffer, 0, 1);
                if (byteRead == 0)
                    break;                                          // reached end of file.
                byte b = readBuffer[0];
                if (b.CompareTo(Ascii_LowestNonWhite) >= 0
                    && b.CompareTo(Ascii_HighestNonWhite) <= 0)
                {   
                    switch (phase)
                    {
                        case TagPhase.Unstarted:                    // have not found initial "<" yet.
                            if (b.CompareTo(Ascii_LessThan) == 0)
                                phase = TagPhase.Started;                            
                            else
                                phase = TagPhase.SyntaxError;       // char found before initial "<"!!
                            break;
                        case TagPhase.Started:                      // have just found initial "<".
                            if (b.CompareTo(Ascii_GreaterThan) == 0)
                                phase = TagPhase.SyntaxError;
                            else if (b.CompareTo(Ascii_LessThan) == 0)
                                phase = TagPhase.SyntaxError;
                            else if (b.CompareTo(Ascii_Slash) == 0)
                                tagType = TagType.EndTag;
                            else if (b.CompareTo(Ascii_Exclamation) == 0)
                            {
                                tagType = TagType.Comment;
                                phase = TagPhase.ReadingComment;    // this is a short-circuit for reading until ">" is found.
                            }
                            else
                            {   // We are reading the first (or second) non-white char after "<"
                                if (tagType == TagType.None)
                                    tagType = TagType.StartTag;
                                phase = TagPhase.ReadingName;
                                ptrStart = m_ByteWorkSpace.Count;   // index where name starts
                            }
                            break;
                        case TagPhase.ReadingName:
                            if (b.CompareTo(Ascii_GreaterThan) == 0)
                            {
                                if (tagType == TagType.StartTag && m_ByteWorkSpace[m_ByteWorkSpace.Count - 1].CompareTo(Ascii_Slash) == 0)
                                {
                                    tagType = TagType.CompleteTag;         // special case "<Name/>" can start and terminate in one cmd.
                                    m_ByteWorkSpace.RemoveAt(m_ByteWorkSpace.Count - 1);    // remove trailing slash "/"
                                }
                                phase = TagPhase.Ended;
                                str = ASCIIEncoding.ASCII.GetString(m_ByteWorkSpace.ToArray());
                                tagName = str.Substring(ptrStart);
                            }
                            else if (b.CompareTo(Ascii_LessThan) == 0)
                                phase = TagPhase.SyntaxError;                            
                            break;
                        case TagPhase.ReadingAfterName:
                            if (b.CompareTo(Ascii_GreaterThan) == 0)
                                phase = TagPhase.Ended;
                            else if (b.CompareTo(Ascii_LessThan) == 0)
                                phase = TagPhase.SyntaxError;
                            else
                            {   // We are reading non-white chars inside name, probably attribute starting                                
                                phase = TagPhase.ReadingKey;
                                ptrStart = m_ByteWorkSpace.Count;   // index where name starts
                            }
                            break;
                        case TagPhase.ReadingKey:
                            if (b.CompareTo(Ascii_GreaterThan) == 0)
                                phase = TagPhase.SyntaxError;
                            else if (b.CompareTo(Ascii_LessThan) == 0)
                                phase = TagPhase.SyntaxError;
                            else if (b.CompareTo(Ascii_Equals) == 0)
                                phase = TagPhase.ReadingValue;      // Start reading the attribute value.
                            break;
                        case TagPhase.ReadingValue:
                            if (b.CompareTo(Ascii_GreaterThan) == 0)
                            {   // We have reached the end of the entire tag!
                                phase = TagPhase.Ended;
                                if (tagType == TagType.StartTag && m_ByteWorkSpace[m_ByteWorkSpace.Count - 1].CompareTo(Ascii_Slash) == 0)
                                {
                                    tagType = TagType.CompleteTag;         // special case "<Name Key1=Value1 Key2=Value2/>" can start and terminate in one cmd.
                                    m_ByteWorkSpace.RemoveAt(m_ByteWorkSpace.Count - 1);    // remove trailing "/"
                                }

                                str = ASCIIEncoding.ASCII.GetString(m_ByteWorkSpace.ToArray()).Substring(ptrStart);
                                if (!TryAddAttribute(str, ref m_AttributeWorkSpace))
                                    phase = TagPhase.SyntaxError;
                            }
                            else if (b.CompareTo(Ascii_Equals) == 0)
                            {   // This is a subtle issue.  Attribute values can have spaces embedded in them, so
                                // we might pass multiple spaces, and NOT realize that we have started reading the next key.
                                // But when we pass another "=" sign, we must conclude that we have read the *next* key.
                                // Locate the string just before this "=" .
                                int ptr = m_ByteWorkSpace.Count - 1;        // last char stored.
                                while (ptr > ptrStart && IsWhiteSpace(m_ByteWorkSpace[ptr]))
                                    ptr--;                                  // back up until we find the previous word.
                                while (ptr > ptrStart && !IsWhiteSpace(m_ByteWorkSpace[ptr]))
                                    ptr--;                                  // back up until beginning of the previous word.
                                
                                if (ptrStart < ptr)
                                {
                                    str = ASCIIEncoding.ASCII.GetString(m_ByteWorkSpace.ToArray()).Substring(ptrStart, ptr - ptrStart);
                                    if (!TryAddAttribute(str, ref m_AttributeWorkSpace))
                                        phase = TagPhase.SyntaxError;
                                    ptrStart = ptr;                         // point to start of key word.
                                }
                            }
                            else if (b.CompareTo(Ascii_LessThan) == 0)
                                phase = TagPhase.SyntaxError;                            
                            break;
                        case TagPhase.ReadingComment:
                                if (b.CompareTo(Ascii_GreaterThan) == 0)       // ">" signifies the end of the comment
                                    phase = TagPhase.Ended;
                            break;
                        default:
                            break;
                    }
                    m_ByteWorkSpace.Add(b);
                    debugString = ASCIIEncoding.ASCII.GetString(m_ByteWorkSpace.ToArray());   
                } 
                else if (b.CompareTo(Ascii_Space) == 0)
                {   //
                    // Space
                    //                    
                    switch (phase)
                    {
                        case TagPhase.ReadingName:              // we are currently reading a name.
                            phase = TagPhase.ReadingAfterName;  // a trailing space indicates the end of the name.
                            str = ASCIIEncoding.ASCII.GetString(m_ByteWorkSpace.ToArray());
                            tagName = str.Substring(ptrStart); 
                            break;
                        default:
                            break;
                    }
                    m_ByteWorkSpace.Add(b);
                }
                else
                {   // Strange character detected!

                }
            }
            // Exit.
            if (tagType != TagType.None)
            {
                //fullTagString = ASCIIEncoding.ASCII.GetString(m_ByteWorkSpace.ToArray());
                foreach (string key in m_AttributeWorkSpace.Keys)
                    if (!attributes.ContainsKey(key))
                        attributes.Add(key, m_AttributeWorkSpace[key]);
                    else
                        attributes[key] = m_AttributeWorkSpace[key];
            }
            return (tagType!=TagType.None) && (phase!=TagPhase.SyntaxError);
        } // TryReadTag()
        //
        //
        //
        public void Close()
        {
            m_Stream.Close();
        }
        public void Dispose()
        {
            m_Stream.Dispose();
        }
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        private bool TryAddAttribute(string str, ref Dictionary<string, string> attribute)
        {
            int ptr = str.IndexOf('=');                                
            if ( ptr < 0 || ptr >= str.Length )
                return false;
            string key = str.Substring(0,ptr).Trim();
            string val = (str.Substring(ptr+1,str.Length-(ptr+1))).Trim();
            if ( attribute.ContainsKey(key) )
                attribute[key] = val;
            else
                attribute.Add(key,val);
            return true;
        }// TryAddAttribute()
        //
        //
        //
        //
        //

        //
        //
        private bool IsWhiteSpace(byte b)
        {
            return (b.CompareTo(Ascii_LowestNonWhite) < 0 || b.CompareTo(Ascii_HighestNonWhite) > 0);
        }
        #endregion//Private Methods


        #region Enums
        // *****************************************************************
        // ****                         Enums                           ****
        // *****************************************************************
        //
        public enum TagType
        {
            None                    // what was read contains NO tag.
            ,StartTag
            ,EndTag
            ,CompleteTag
            ,Comment
        }
        //
        //
        private enum TagPhase
        {
            Unstarted
            ,Started
            ,ReadingName
            ,ReadingAfterName
            ,ReadingKey
            ,ReadingValue
            ,Ended
            ,SyntaxError
            ,ReadingComment
        }
        //
        //
        #endregion // Enums


    }//end class
}

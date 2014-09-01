using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.IO.Xml
{
    public interface IStringifiable 
    {

        string GetAttributes();
        List<IStringifiable> GetElements();                     // Can be null.

        void SetAttributes(Dictionary<string, string> attributes);
        void AddSubElement(IStringifiable subElement);

    }
}

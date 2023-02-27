using UdonSharp;

namespace UdonXMLParser
{
    public class UdonXML_Callback : UdonSharpBehaviour // "interface"
    {
        virtual public void OnUdonXMLParseEnd(object[] data, string callbackId) { }
        virtual public void OnUdonXMLIteration(int processing, int total) { }
    }
}

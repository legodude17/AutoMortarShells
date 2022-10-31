using System.Xml;
using Verse;

namespace AutoMortarShellChoice
{
    public class PatchOperationTryAddModExtension : PatchOperationAddModExtension
    {
        public override bool ApplyWorker(XmlDocument xml)
        {
            if (xml.SelectSingleNode(xpath) == null) return true;
            return base.ApplyWorker(xml);
        }
    }
}
﻿using System.Xml;
using Verse;

namespace AutoMortarShellChoice
{
    internal class PatchOperationTryAddModExtension : PatchOperationAddModExtension
    {
        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (xml.SelectSingleNode(xpath) == null) return true;
            return base.ApplyWorker(xml);
        }
    }
}
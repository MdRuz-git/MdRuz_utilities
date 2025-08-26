﻿using System.Linq;
using System.Xml;
using Verse;

namespace MDutility
{
    public class PatchOperationSet : PatchOperationPathed
    {
        XmlContainer value = null;
        Skip skip = Skip.None;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            XmlNode node = value.node;
            bool result = false;
            foreach (var xmlNode in xml.SelectNodes(this.xpath).Cast<XmlNode>())
            {
                result = true;

                foreach (XmlNode childNode in node.ChildNodes)
                {
                    var conflict = xmlNode.ChildNodes
                                    .OfType<XmlNode>()
                                    .FirstOrDefault(n => n.Name == childNode.Name);
                    var import = xmlNode.OwnerDocument.ImportNode(childNode, true);
                    if (conflict != null)
                    {
                        if (skip != Skip.Replacing)
                        {
                            xmlNode.ReplaceChild(import, conflict);
                        }
                    }
                    else if (skip != Skip.Adding)
                    {
                        xmlNode.AppendChild(import);
                    }
                }
            }
            return result;
        }

        private enum Skip
        {
            None,
            Adding,
            Replacing,
        }
    }
}
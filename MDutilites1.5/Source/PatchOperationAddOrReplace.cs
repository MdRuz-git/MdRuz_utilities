using System;
using System.Collections;
using System.Xml;
using Verse;

namespace MDutility
{
    /* this currently lacks major feature, which is comparing and adding more than 1 value node.
    * WARNING read above
    * A custom patch operation to simplify sequence patch operations when defensively adding fields
    * Code by Lanilor (https://github.com/Lanilor)
	* MODIFIED by 'MdRuz' to handle xpath returning null
	* PatchOperationConditional matching no longer necessary
	* you can directly reference modded defNames whether they are present or not
	*
    * This code is provided "as-is" without any warrenty whatsoever. Use it on your own risk.
    */
    public class PatchOperationAddOrReplace : PatchOperationPathed
    {
        protected string key;
        private readonly XmlContainer value;
        protected override bool ApplyWorker(XmlDocument xml)
        {
            XmlNode valNode = value.node;
            bool result = false;
            // Get all nodes using xpath
            XmlNodeList nodeList = xml.SelectNodes(xpath);
            // If no nodes were found, exit early
            if (nodeList == null || nodeList.Count == 0)
            {
                result = true;
                Log.Message("Skipping patch for:" + (this.xpath) + (this.key));
            }
            IEnumerator enumerator = nodeList.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    // Check if this individual xmlNode has a valid or existing XPath
                    XmlNode xmlNode = xml.SelectSingleNode(this.xpath);
                    if (xmlNode != null)
                    {
                        // XPath is valid, perform operations
                        result = true; //tells the game that patch suceeded
                        XmlNode parentNode = enumerator.Current as XmlNode;
                        // Search for the child node with the key
                        XmlNode existingxmlNode = parentNode.SelectSingleNode(key);
                        if (existingxmlNode == null)
                        {
                            // Create the key node if it doesn't exist
                            existingxmlNode = parentNode.OwnerDocument.CreateElement(key);
                            parentNode.AppendChild(existingxmlNode);
                        }
                        else
                        {
                            // Replace existing node's children if it exists
                            existingxmlNode.RemoveAll();
                        }
                        // Add the new value node
                        existingxmlNode.AppendChild(parentNode.OwnerDocument.ImportNode(valNode.FirstChild, true));
                    }
                }
            }
            finally
            {
                // Ensure enumerator is disposed of
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            return result;
        }
    }
}

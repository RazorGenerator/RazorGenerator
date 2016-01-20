using System.Collections.Generic;
using System.Web.Routing;
using HtmlAgilityPack;

namespace RazorGenerator.Testing
{
    /// <summary>
    ///   HtmlNode that contains an ActionUrl
    ///   The RouteValues property allows test code to check what route parameters would have been used
    ///   to generate each attribute
    /// </summary>
    public class ActionUrlNode : HtmlNode
    {
      public ActionUrlNode(HtmlNode htmlNode)
          : base(htmlNode.NodeType, htmlNode.OwnerDocument, -1)
        {
            Name = htmlNode.Name;
            CopyFrom(htmlNode, false);
            RouteValues = new Dictionary<string, RouteValueDictionary>();
        }

        public Dictionary<string, RouteValueDictionary> RouteValues { get; set; }
    }
}
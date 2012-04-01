using System;
using System.Web.Mvc;
using HtmlAgilityPack;

namespace RazorGenerator.Testing
{
    /// <summary>
    ///   HtmlNode representing a view that would be rendered as a partial
    ///   The ViewData and ViewName properties allow test code to check which view would be rendered and with what data
    /// </summary>
    public class ViewNode : HtmlNode
    {
        public ViewNode(HtmlDocument ownerDocument, String viewName, String elementName, ViewDataDictionary viewData)
          : base(HtmlNodeType.Element, ownerDocument, -1)
        {
            ViewData = viewData;
            ViewName = viewName;
            Name = elementName;
        }

        public ViewDataDictionary ViewData { get; private set; }
        public string ViewName { get; private set; }
    }
}
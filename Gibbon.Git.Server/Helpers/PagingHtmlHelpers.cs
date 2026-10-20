using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Gibbon.Git.Server.Models;

namespace Gibbon.Git.Server.Helpers;

public static class PagingHtmlHelpers
{
    public static IHtmlContent PageLinks(this IHtmlHelper htmlHelper, PageInfoModel pageInfo, Func<int, string> PageUrl)
    {
        StringBuilder pagingTags = new();

        // reference unused parameter to clear warning
        _ = htmlHelper;

        // prev page
        if (pageInfo.CurrentPage > 1)
        {
            pagingTags.Append(GetTagString("Prev", PageUrl(pageInfo.CurrentPage - 1)));
        }

        // page numbers
        for (int i = 1; i <= pageInfo.LastPage; i++)
        {
            // skip link for current page # and make it bold
            if (i == pageInfo.CurrentPage)
            {
                pagingTags.Append("<h3 style=\"display: inline-block;padding: 5px;font-weight: bold\">" + i + "</h3>");
            }
            else
            {
                pagingTags.Append(GetTagString(i.ToString(), PageUrl(i)));
            }
        }

        // next page
        if (pageInfo.CurrentPage < pageInfo.LastPage)
        {
            pagingTags.Append(GetTagString("Next", PageUrl(pageInfo.CurrentPage + 1)));
        }

        // paging tags
        return new HtmlString(pagingTags.ToString());
    }

    private static string GetTagString(string innerHtml, string hrefValue)
    {
        // construct an <a> tag
        TagBuilder tag = new("a");

        tag.MergeAttribute("class", "anchorstyle");
        tag.MergeAttribute("style", "display: inline-block;padding: 5px");  // force all pages to same line
        tag.MergeAttribute("href", hrefValue);
        tag.InnerHtml.Append(" " + innerHtml + "  ");

        using StringWriter sw = new();
        tag.WriteTo(sw, HtmlEncoder.Default);

        return sw.ToString();
    }

}
﻿using System.Web.Optimization;

namespace WB.UI.Headquarters
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.UseCdn = false;

            bundles.IgnoreList.Clear();
            bundles.IgnoreList.Ignore("*-vsdoc.js");
            bundles.IgnoreList.Ignore("*intellisense.js");

            bundles.Add(new ScriptBundle("~/Scripts/jqplot-area")
                .Include("~/Scripts/jqPlot/jquery.jqplot.js",
                "~/Scripts/jqPlot/plugins/jqplot.categoryAxisRenderer.min.js",
                "~/Scripts/jqPlot/plugins/jqplot.highlighter.min.js",
                "~/Scripts/jqPlot/plugins/jqplot.canvasTextRenderer.min.js",
                "~/Scripts/jqPlot/plugins/jqplot.canvasAxisTickRenderer.min.js"));
        }
    }
}
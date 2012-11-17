using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.WebPages;

namespace RonnieOverby.MvcAssetManager
{
    public class AssetManager
    {
        private static readonly object RequestKey = typeof(AssetManager);
        private readonly List<Asset> _assets = new List<Asset>();
        private readonly List<Asset> _layoutAssets = new List<Asset>();

        public static AssetManager GetInstance(HttpContextBase httpContext)
        {
            return (AssetManager)(httpContext.Items[RequestKey]
                                   ?? (httpContext.Items[RequestKey] = new AssetManager()));
        }

        public void Require(Asset asset, bool forLayout)
        {
            if (asset == null) throw new ArgumentNullException("asset");

            // no dups
            if (_assets.Any(x => x.Type == asset.Type && x.Text == asset.Text))
                return;

            if (forLayout)
                _layoutAssets.Add(asset);
            else
                _assets.Add(asset);
        }

        public IEnumerable<Asset> GetRenderableAssets(params AssetType[] types)
        {
            foreach (var asset in _layoutAssets.Concat(_assets).Where(x => !x.Rendered && types.Contains(x.Type)))
            {
                asset.Rendered = true;
                yield return asset;
            }
        }

        public class Asset
        {
            public string Text { get; set; }
            public AssetType Type { get; set; }
            public bool Rendered { get; internal set; }

            public Asset(string text, AssetType type)
            {
                if (text == null) throw new ArgumentNullException("text");

                Text = text;
                Type = type;
            }
        }

        public enum AssetType
        {
            Script,
            Style,
            ScriptBundle,
            StyleBundle
        }
    }

    public static class AssetManagerHtmlHelpers
    {
        public static IHtmlString RequireScriptForLayout(this HtmlHelper html, Func<object, HelperResult> script)
        {
            var helperResult = new HelperResult(writer => script(null).WriteTo(writer)).ToString();
            var a = new AssetManager.Asset(helperResult, AssetManager.AssetType.Script);
            AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, true);
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireScript(this HtmlHelper html, Func<object, HelperResult> script)
        {
            var helperResult = new HelperResult(writer => script(null).WriteTo(writer)).ToString();
            var a = new AssetManager.Asset(helperResult, AssetManager.AssetType.Script);
            AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, false);
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireStyleForLayout(this HtmlHelper html, Func<object, HelperResult> style)
        {
            var helperResult = new HelperResult(writer => style(null).WriteTo(writer)).ToString();
            var a = new AssetManager.Asset(helperResult, AssetManager.AssetType.Style);
            AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, true);
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireStyle(this HtmlHelper html, Func<object, HelperResult> style)
        {
            var helperResult = new HelperResult(writer => style(null).WriteTo(writer)).ToString();
            var a = new AssetManager.Asset(helperResult, AssetManager.AssetType.Style);
            AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, false);
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireScriptBundlesForLayout(this HtmlHelper html, params string[] bundles)
        {
            foreach (var bundle in bundles)
            {
                var a = new AssetManager.Asset(bundle, AssetManager.AssetType.ScriptBundle);
                AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, true);
            }
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireScriptBundles(this HtmlHelper html, params string[] bundles)
        {
            foreach (var bundle in bundles)
            {
                var a = new AssetManager.Asset(bundle, AssetManager.AssetType.ScriptBundle);
                AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, false);
            }
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireStyleBundlesForLayout(this HtmlHelper html, params string[] bundles)
        {
            foreach (var bundle in bundles)
            {
                var a = new AssetManager.Asset(bundle, AssetManager.AssetType.StyleBundle);
                AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, true);
            }
            return new MvcHtmlString("");
        }

        public static IHtmlString RequireStyleBundles(this HtmlHelper html, params string[] bundles)
        {
            foreach (var bundle in bundles)
            {
                var a = new AssetManager.Asset(bundle, AssetManager.AssetType.StyleBundle);
                AssetManager.GetInstance(html.ViewContext.HttpContext).Require(a, false);
            }
            return new MvcHtmlString("");
        }

        public static IHtmlString RenderRequiredStyles(this HtmlHelper html)
        {
            var am = AssetManager.GetInstance(html.ViewContext.HttpContext);
            var assets = am.GetRenderableAssets(AssetManager.AssetType.StyleBundle, AssetManager.AssetType.Style)
                .ToArray();

            var sb = new StringBuilder();
            var bundles = new List<AssetManager.Asset>();

            foreach (var asset in assets)
            {
                switch (asset.Type)
                {
                    case AssetManager.AssetType.Style:
                        if (bundles.Any())
                        {
                            sb.AppendLine(Styles.Render(bundles.Select(x => x.Text).ToArray()).ToString());
                            bundles.Clear();
                        }
                        sb.AppendLine(asset.Text);
                        break;

                    case AssetManager.AssetType.StyleBundle:
                        bundles.Add(asset);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            sb.AppendLine(Styles.Render(bundles.Select(x => x.Text).ToArray()).ToString());
            return new MvcHtmlString(sb.ToString());
        }

        public static IHtmlString RenderRequiredScripts(this HtmlHelper html)
        {
            var am = AssetManager.GetInstance(html.ViewContext.HttpContext);
            var assets = am.GetRenderableAssets(AssetManager.AssetType.ScriptBundle, AssetManager.AssetType.Script)
                .ToArray();

            var sb = new StringBuilder();
            var bundles = new List<AssetManager.Asset>();

            foreach (var asset in assets)
            {
                switch (asset.Type)
                {
                    case AssetManager.AssetType.Script:
                        if (bundles.Any())
                        {
                            sb.AppendLine(Scripts.Render(bundles.Select(x => x.Text).ToArray()).ToString());
                            bundles.Clear();
                        }
                        sb.AppendLine(asset.Text);
                        break;

                    case AssetManager.AssetType.ScriptBundle:
                        bundles.Add(asset);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            sb.AppendLine(Scripts.Render(bundles.Select(x => x.Text).ToArray()).ToString());
            return new MvcHtmlString(sb.ToString());
        }

    }
}
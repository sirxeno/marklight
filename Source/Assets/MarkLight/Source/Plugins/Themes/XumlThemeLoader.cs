﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using MarkLight;
using MarkLight.ValueConverters;
using UnityEngine;

namespace Marklight.Themes
{
    public class XumlThemeLoader
    {
        /// <summary>
        /// Loads Theme XUML and returns Theme or null if failed.
        /// </summary>
        public Theme LoadXuml(XElement xumlElement, string xuml, string xumlAssetName)
        {
            var themeNameAttr = xumlElement.Attribute("Name");
            if (themeNameAttr == null)
            {
                Debug.LogError(string.Format("[MarkLight] {0}: Error parsing theme XUML. Name attribute missing.", xumlAssetName));
                return null;
            }

            var baseDirectoryAttr = xumlElement.Attribute("BaseDirectory");
            var unitSizeAttr = xumlElement.Attribute("UnitSize");
            var hasBaseDirectory = baseDirectoryAttr != null;
            var hasUnitSize = unitSizeAttr != null;

            var baseDirectory = baseDirectoryAttr != null ? baseDirectoryAttr.Value : "";
            var unitSize = ParseUnitSize(hasUnitSize ? unitSizeAttr.Value : null, xumlAssetName);

            var styles = new List<Style>();

            // load theme elements
            foreach (var childElement in xumlElement.Elements())
            {
                var idAttr = childElement.Attribute("Id");
                var classNameAttr = childElement.Attribute("Style");
                var basedOnAttr = childElement.Attribute("BasedOn");

                string elementName = null;
                string id = null;
                string className = null;
                string basedOn = null;


                if (childElement.Name.LocalName != "Style")
                    elementName = childElement.Name.LocalName;

                if (idAttr != null)
                    id = idAttr.Value;

                if (classNameAttr != null)
                    className = classNameAttr.Value;

                if (basedOnAttr != null)
                    basedOn = basedOnAttr.Value;

                var style = new Style(elementName, id, className, basedOn);
                var properties = LoadAttributes(childElement);

                var existingIndex = styles.IndexOf(style);
                if (existingIndex != -1)
                {
                    var existing = styles[existingIndex];

                    foreach (var property in style)
                    {
                        existing.Properties.Remove(property);
                        existing.Properties.Add(property);
                    }
                }
                else
                {
                    style.Properties.AddRange(properties);
                    styles.Add(style);
                }
            }

            return new Theme(themeNameAttr.Value,
                                        baseDirectory, unitSize, hasBaseDirectory, hasUnitSize, styles);
        }

        private static IEnumerable<StyleProperty> LoadAttributes(XElement xumlElement) {

            var result = new List<StyleProperty>(10);

            foreach (var attribute in xumlElement.Attributes())
            {
                var name = attribute.Name.LocalName;
                var value = attribute.Value;

                // ignore namespace specification
                if (string.Equals(name, "xmlns", StringComparison.OrdinalIgnoreCase))
                    continue;

                // ignore id specification
                if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
                    continue;

                // ignore basedon specification
                if (string.Equals(name, "basedon", StringComparison.OrdinalIgnoreCase))
                    continue;

                // ignore classname specification
                if (string.Equals(name, "classname", StringComparison.OrdinalIgnoreCase))
                    continue;

                // ignore obsolete style view field
                if (string.Equals(name, "style", StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Add(new StyleProperty(name, value));
            }

            return result;
        }

        private static Vector3 ParseUnitSize(string unitSize, string xumlAssetName) {
            if (string.IsNullOrEmpty(unitSize))
            {
                // use default unit size
                return ViewPresenter.Instance.UnitSize;
            }
            var converter = new Vector3ValueConverter();
            var result = converter.Convert(unitSize);
            if (result.Success)
            {
                return (Vector3) result.ConvertedValue;
            }

            Debug.LogError(string.Format(
                "[MarkLight] {0}: Error parsing theme XUML. Unable to parse UnitSize attribute value \"{1}\".",
                xumlAssetName, unitSize));

            return ViewPresenter.Instance.UnitSize;
        }
    }
}
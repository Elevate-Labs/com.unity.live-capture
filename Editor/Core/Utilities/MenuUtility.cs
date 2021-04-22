using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture
{
    static class MenuUtility
    {
        public static GenericMenu CreateMenu<TMember, TAttribute>(
            IEnumerable<(TMember, TAttribute[])> attributes,
            Func<TMember, bool> isEnabled,
            Action<TMember, TAttribute> menuFunction
        )
            where TAttribute : MenuPathAttribute
        {
            var menu = new GenericMenu();

            var subMenuMinPriorities = new Dictionary<string, int>();
            var subMenuMaxPriorities = new Dictionary<string, int>();

            foreach (var(member, attribute) in attributes
                     .Select(tuple => (tuple.Item1, tuple.Item2.First()))
                     .OrderBy(tuple => tuple.Item2.priority))
            {
                // add separators based on the priority, matching how the MenuItem attribute works
                var subMenu = attribute.itemName.Substring(0, Mathf.Max(0, attribute.itemName.LastIndexOf('/')));

                if (!subMenuMinPriorities.ContainsKey(subMenu))
                {
                    // separate new submenus from the preceding submenu
                    if (subMenuMinPriorities.Count > 0)
                    {
                        var preceding = subMenuMinPriorities.Select(x => (x.Value, x)).Max().x;

                        if (attribute.priority - preceding.Value > 10)
                        {
                            var sharedCharCount = subMenu.Zip(preceding.Key, (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
                            var sharedPath = subMenu.Substring(0, sharedCharCount) + "/";
                            menu.AddSeparator(sharedPath);
                        }
                    }

                    subMenuMinPriorities[subMenu] = attribute.priority;
                }
                else if (attribute.priority - subMenuMaxPriorities[subMenu] > 10)
                {
                    // separate items in the same submenu
                    menu.AddSeparator(subMenu == string.Empty ? string.Empty : subMenu + "/");
                }

                subMenuMaxPriorities[subMenu] = attribute.priority;

                // add the item that creates the device
                var item = new GUIContent(attribute.itemName);

                if (isEnabled(member))
                {
                    menu.AddItem(item, false, () =>
                    {
                        menuFunction?.Invoke(member, attribute);
                    });
                }
                else
                {
                    menu.AddDisabledItem(item);
                }
            }

            return menu;
        }
    }
}

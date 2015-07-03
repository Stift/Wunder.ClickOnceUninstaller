using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace Wunder.ClickOnceUninstaller
{
    public class Uninstaller
    {
        private readonly ClickOnceRegistry _registry;

        public Uninstaller()
            : this(new ClickOnceRegistry())
        {
        }

        public Uninstaller(ClickOnceRegistry registry)
        {
            _registry = registry;
        }

        public void Uninstall(UninstallInfo uninstallInfo)
        {
            var toRemove = FindComponentsToRemove(uninstallInfo.GetPublicKeyToken());

            Console.WriteLine("Components to remove:");
            toRemove.ForEach(Console.WriteLine);
            Console.WriteLine();

            var steps = new List<IUninstallStep>
                            {
                                new RemoveFiles(),
                                new RemoveStartMenuEntry(uninstallInfo),
                                new RemoveRegistryKeys(_registry, uninstallInfo),
                                new RemoveUninstallEntry(uninstallInfo)
                            };

            steps.ForEach(s => s.Prepare(toRemove));
            steps.ForEach(s => s.PrintDebugInformation());
            steps.ForEach(s => s.Execute());

            steps.ForEach(s => s.Dispose());
        }

        private List<string> FindComponentsToRemove(string token)
        {
            var components = WhereKeyContains(_registry.Components, token);

            var toRemove = new List<string>();
            foreach (var component in components)
            {
                toRemove.Add(component.Key);

                foreach (var dependency in component.Dependencies)
                {
                    if (toRemove.Contains(dependency)) continue; // already in the list
                    if (NoKeyEquals(_registry.Components, dependency)) continue; // not a public component

                    var mark = FirstOrDefaultByKey(_registry.Marks, dependency);
                    if (mark != null && AnyNoKeyEquals(mark.Implications, components))
                    {
                        // don't remove because other apps depend on this
                        continue;
                    }

                    toRemove.Add(dependency);
                }
            }

            return toRemove;
        }

        private bool AnyNoKeyEquals(List<ClickOnceRegistry.Implication> implications, List<ClickOnceRegistry.Component> components)
        {
            foreach (var implication in implications)
            {
                if (NoKeyEquals(components, implication.Name))
                    return true;
            }
            return false;
        }

        private ClickOnceRegistry.Mark FirstOrDefaultByKey(List<ClickOnceRegistry.Mark> marks, string dependency)
        {
            foreach (var mark in marks)
            {
                if (mark.Key == dependency)
                    return mark;
            }
            return null;
        }

        private bool NoKeyEquals(List<ClickOnceRegistry.Component> components, string dependency)
        {
            foreach (var component in components)
            {
                if (component.Key == dependency)
                    return false;
            }
            return true;
        }

        private List<ClickOnceRegistry.Component> WhereKeyContains(List<ClickOnceRegistry.Component> components, string token)
        {
            var res = new List<ClickOnceRegistry.Component>();
            foreach (var component in components)
            {
                if (component.Key.Contains(token))
                    res.Add(component);
            }
            return res;
        }
    }
}

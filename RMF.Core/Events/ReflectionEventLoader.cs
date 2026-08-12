using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Events
{
    public static class ReflectionEventLoader
    {
        private static readonly string _namespaceFormat = "{0}.Events.{1}";

        public static (Dictionary<string, Type> Data, int Total) FindEvents(string programSide)
        {
            Assembly? executingAssembly = Assembly.GetExecutingAssembly();
            string? projectName = executingAssembly?.GetName().Name;
            if (executingAssembly == null || string.IsNullOrEmpty(projectName))
            {
                return (new Dictionary<string, Type>(), 0);
            }

            programSide = char.ToUpper(programSide[0]) + programSide.Substring(1).ToLower();  // You can enter the name in any case
            string targetNamespace = string.Format(_namespaceFormat, projectName, programSide);

            Type baseEventType = typeof(BackgroundEvent);
            Type[] foundEvents = executingAssembly
                .GetTypes()
                .Where(t => t.Namespace == targetNamespace && t.IsSubclassOf(baseEventType) && !t.IsInterface && !t.IsAbstract)
                .ToArray();

            Dictionary<string, Type> eventTypes = [];
            foreach (Type t in foundEvents)
            {
                eventTypes.TryAdd(t.Name, t);
            }
            return (eventTypes, foundEvents.Length);
        }
    }
}

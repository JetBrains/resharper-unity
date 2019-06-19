using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    public class MonoBehaviourPropertyValueWithLocation
    {
        public readonly IPsiSourceFile File;
        public readonly MonoBehaviourPropertyValue Value;
        
        public MonoBehaviourPropertyValueWithLocation(IPsiSourceFile file, MonoBehaviourPropertyValue value)
        {
            File = file;
            Value = value;
        }

        protected bool Equals(MonoBehaviourPropertyValueWithLocation other)
        {
            return Equals(File, other.File) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MonoBehaviourPropertyValueWithLocation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (File.GetHashCode() * 397) ^ Value.GetHashCode();
            }
        }

        public string GetSimplePresentation(ISolution solution)
        {
            return Value.GetSimplePresentation(solution, File);
        }
    }

    public class PropertyValueLocalCache
    {
        private readonly OneToCompactCountingSet<MonoBehaviourProperty, object> myUniquePropertyValues =
            new OneToCompactCountingSet<MonoBehaviourProperty, object>();
        
        
        private readonly OneToCompactCountingSet<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation> myPropertyValues =
            new OneToCompactCountingSet<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>();

        public void Add(MonoBehaviourProperty property, MonoBehaviourPropertyValueWithLocation valueWithLocation)
        {
            var value = valueWithLocation.Value.Value;
            if (value != null)
                myUniquePropertyValues.Add(property, value);
            myPropertyValues.Add(property, valueWithLocation);
        }

        public void Remove(MonoBehaviourProperty property, MonoBehaviourPropertyValueWithLocation valueWithLocation)
        {
            var value = valueWithLocation.Value.Value;
            if (value != null)
                myUniquePropertyValues.Remove(property, value);
            myPropertyValues.Remove(property, valueWithLocation);
        }

        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetValues(MonoBehaviourProperty query)
        {
            foreach (var v in myPropertyValues.GetValues(query))
            {
                yield return v;
            }
        }

        public bool HasUniqueValue(MonoBehaviourProperty query)
        {
            return myUniquePropertyValues.GetValues(query).Length == 1;
        }

        public int GetValueCount(MonoBehaviourProperty query, object except)
        {
            var length = myPropertyValues.GetValues(query).Length;
            var exceptCount = myUniquePropertyValues.GetCount(query, except);
            return length - exceptCount;
        }
    }
}
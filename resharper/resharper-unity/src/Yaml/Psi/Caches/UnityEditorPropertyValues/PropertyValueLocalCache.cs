using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
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
        
        public string GetOwnerPresentation(ISolution solution)
        {
            return Value.GetOwnerPresentation(solution, File);
        }
    }

    public class PropertyValueLocalCache
    {
        private sealed class MonoBehaviorPropertyValueBox
        {
            public readonly MonoBehaviourPropertyValueWithLocation BoxedValue;
            private readonly object myHashObj;

            public MonoBehaviorPropertyValueBox(MonoBehaviourPropertyValueWithLocation boxedValue)
            {
                BoxedValue = boxedValue;
                myHashObj = BoxedValue.Value.Value;
            }

            public MonoBehaviorPropertyValueBox(object obj)
            {
                myHashObj = obj;
            }

            private bool Equals(MonoBehaviorPropertyValueBox other)
            {
                return myHashObj.Equals(other.myHashObj);
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj) || obj is MonoBehaviorPropertyValueBox other && Equals(other);
            }

            public override int GetHashCode()
            {
                return myHashObj.GetHashCode();
            }
        }
        
        
        public struct MonoBehaviourPropertyWithFile
        {
            public readonly MonoBehaviourProperty Property;
            public readonly IPsiSourceFile File;

            public MonoBehaviourPropertyWithFile(MonoBehaviourProperty property, IPsiSourceFile file)
            {
                Property = property;
                File = file;
            }
            
            public bool Equals(MonoBehaviourPropertyWithFile other)
            {
                return Property.Equals(other.Property) && File.Equals(other.File);
            }

            public override bool Equals(object obj)
            {
                return obj is MonoBehaviourPropertyWithFile other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Property.GetHashCode() * 397) ^ File.GetHashCode();
                }
            }
        }
        
        public struct MonoBehaviourPropertyWithValue
        {
            public readonly MonoBehaviourProperty Property;
            public readonly object Value;

            public MonoBehaviourPropertyWithValue(MonoBehaviourProperty property, object value)
            {
                Property = property;
                Value = value;
            }

            public bool Equals(MonoBehaviourPropertyWithValue other)
            {
                return Property.Equals(other.Property) && Value.Equals(other.Value);
            }

            public override bool Equals(object obj)
            {
                return obj is MonoBehaviourPropertyWithValue other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Property.GetHashCode() * 397) ^ Value.GetHashCode();
                }
            }
        }
        
        
        // Different indices for queries from local stage. All queries should take O(1)
        private readonly OneToCompactCountingSet<MonoBehaviourPropertyWithFile, object> myValueCountPerPropertyAndFile = 
            new OneToCompactCountingSet<MonoBehaviourPropertyWithFile, object>();
        
        private readonly CountingSet<MonoBehaviourPropertyWithValue> myValuesWhichAreUniqueInWholeFile = 
            new CountingSet<MonoBehaviourPropertyWithValue>();
        
        private readonly OneToCompactCountingSet<MonoBehaviourProperty, MonoBehaviorPropertyValueBox> myUniquePropertyValues =
            new OneToCompactCountingSet<MonoBehaviourProperty, MonoBehaviorPropertyValueBox>();
        
        private readonly OneToCompactCountingSet<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation> myPropertyValues =
            new OneToCompactCountingSet<MonoBehaviourProperty, MonoBehaviourPropertyValueWithLocation>();
        
        private readonly OneToCompactCountingSet<MonoBehaviourProperty, IPsiSourceFile> myPropertyFiles =
            new OneToCompactCountingSet<MonoBehaviourProperty, IPsiSourceFile>();

        public void Add(MonoBehaviourProperty property, MonoBehaviourPropertyValueWithLocation valueWithLocation)
        {
            var value = valueWithLocation.Value.Value;
            if (value != null)
            {
                myUniquePropertyValues.Add(property, new MonoBehaviorPropertyValueBox(valueWithLocation));

                // file changes index
                var query = new MonoBehaviourPropertyWithFile(property, valueWithLocation.File);
                var beforeAddDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(query).Count;

                if (beforeAddDifferentValuesCount == 0)
                {
                    myValueCountPerPropertyAndFile.Add(query, value);
                    
                    myValuesWhichAreUniqueInWholeFile.Add(new MonoBehaviourPropertyWithValue(property, value));
                } else if (beforeAddDifferentValuesCount == 1)
                {
                    var previousValue = myValueCountPerPropertyAndFile.GetOrEmpty(query).First().Key;
                    myValueCountPerPropertyAndFile.Add(query, value);
                    var afterAddDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(query).Count;
                    if (afterAddDifferentValuesCount == 2)
                        myValuesWhichAreUniqueInWholeFile.Remove(new MonoBehaviourPropertyWithValue(property, previousValue));
                }
                else
                {
                    myValueCountPerPropertyAndFile.Add(query, value);
                }
                
                myPropertyFiles.Add(property, valueWithLocation.File);
            }

            myPropertyValues.Add(property, valueWithLocation);
        }

        public void Remove(MonoBehaviourProperty property, MonoBehaviourPropertyValueWithLocation valueWithLocation)
        {
            var value = valueWithLocation.Value.Value;
            if (value != null)
            {
                myUniquePropertyValues.Remove(property, new MonoBehaviorPropertyValueBox(valueWithLocation));
                
                // file changes index
                var query = new MonoBehaviourPropertyWithFile(property, valueWithLocation.File);
                var beforeRemoveDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(query).Count;
                myValueCountPerPropertyAndFile.Remove(query, value);
                var afterRemoveDifferentValuesCount = myValueCountPerPropertyAndFile.GetOrEmpty(query).Count;

                if (beforeRemoveDifferentValuesCount == 2 && afterRemoveDifferentValuesCount == 1)
                {
                    myValuesWhichAreUniqueInWholeFile.Add(new MonoBehaviourPropertyWithValue(property, myValueCountPerPropertyAndFile.GetOrEmpty(query).First().Key));
                } else if (beforeRemoveDifferentValuesCount == 1 && afterRemoveDifferentValuesCount == 0)
                {
                    myValuesWhichAreUniqueInWholeFile.Remove(new MonoBehaviourPropertyWithValue(property, value));
                }

                myPropertyFiles.Remove(property, valueWithLocation.File);
            }

            myPropertyValues.Remove(property, valueWithLocation);
        }

        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetPropertyValues(MonoBehaviourProperty query)
        {
            foreach (var v in myPropertyValues.GetValues(query))
            {
                yield return v;
            }
        }

        public int GetValueCount(MonoBehaviourProperty query, object value)
        {
            return myUniquePropertyValues.GetCount(query, new MonoBehaviorPropertyValueBox(value));
        }

        public IEnumerable<object> GetUniqueValues(MonoBehaviourProperty query)
        {
            foreach (var v in myUniquePropertyValues.GetValues(query))
            {
                yield return v;
            }
        }

        public int GetPropertyValuesCount(MonoBehaviourProperty query)
        {
            return myPropertyValues.GetOrEmpty(query).Count;
        }

        public int GetPropertyUniqueValuesCount(MonoBehaviourProperty query)
        {
            return myUniquePropertyValues.GetOrEmpty(query).Count;
        }
        
        public IEnumerable<MonoBehaviourPropertyValueWithLocation> GetUniqueValuesWithLocation(MonoBehaviourProperty query)
        {
            foreach (var v in myUniquePropertyValues.GetValues(query))
            {
                yield return v.BoxedValue;
            }
        }

        public int GetFilesCountWithoutChanges(MonoBehaviourProperty query, object value)
        {
            return myValuesWhichAreUniqueInWholeFile.GetCount(new MonoBehaviourPropertyWithValue(query, value));
        }

        public int GetFilesWithPropertyCount(MonoBehaviourProperty query)
        {
            return myPropertyFiles.GetOrEmpty(query).Count;
        }
    }
}
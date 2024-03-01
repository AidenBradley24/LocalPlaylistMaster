using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace LocalPlaylistMaster
{
    /// <summary>
    /// Edit the properties of items in a collection at once while preserving unedited differences.
    /// </summary>
    public class CollectionPropertyManager
    {
        private readonly IEnumerable collection;
        private readonly List<(string e, string a, object val)> commons = [];
        private readonly List<(string e, string a)> diffs = [];
        private bool pendingChanges = false;

        public bool PendingChanges { get => pendingChanges; }
        private readonly Type type;
        public Type MyType
        {
            get => type;
        }

        public CollectionPropertyManager(Type type, IEnumerable objs, IEnumerable<string> editProperties, IEnumerable<string> actualProperties)
        {
            if(objs.Cast<LocalPlaylistMaster.Backend.Track>().Count() == 3)
            {
                Trace.WriteLine("WAFFLEs");
            }

            this.type = type;
            collection = new List<object>(objs.Cast<object>());
            foreach (var obj in objs)
            {
                IEnumerator<string> edits = editProperties.GetEnumerator();
                IEnumerator<string> actuals = actualProperties.GetEnumerator();

                while (edits.MoveNext() & actuals.MoveNext())
                {
                    object val = type.GetProperty(actuals.Current)?.GetValue(obj) ?? throw new Exception("HUH");
                    if (diffs.Exists(d => d.a == actuals.Current)) continue; // already in diffs
                    (string e, string a, object val)? toRemove = null;

                    bool alreadyThere = false;

                    foreach (var c in commons)
                    {
                        if (c.a == actuals.Current)
                        {
                            if (c.val.Equals(val))
                            {
                                alreadyThere = true;
                                break;
                            }
                            else
                            {
                                toRemove = c;
                                break;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (toRemove != null)
                    {
                        var remove = toRemove ?? throw new Exception();
                        commons.Remove(remove);
                        diffs.Add((remove.e, remove.a));
                    }
                    else if(!alreadyThere)
                    {
                        commons.Add((edits.Current, actuals.Current, val));
                    }
                }
            }
        }

        public object? GetValue(string editProperty)
        {
            var props = commons.Where(item => item.e == editProperty);
            if (props.Any())
            {
                return props.First().val;
            }
            else
            {
                return null;
            }
        }

        public void SetValue(string editProperty, object value)
        {
            pendingChanges = true;

            if (diffs.Any(item => item.e == editProperty))
            {
                int index = diffs.FindIndex(item => item.e == editProperty);
                var (e, a) = diffs[index];
                diffs.RemoveAt(index);
                var add = (e, a, value);
                commons.Add(add);
            }
            else
            {
                int index = commons.FindIndex(item => item.e == editProperty);
                var (e, a, val) = commons[index];
                commons.RemoveAt(index);
                var add = (e, a, value);
                commons.Add(add);
            }
        }

        public IEnumerable<T> GetCollection<T>()
        {
            return collection.Cast<T>();
        }

        public void ApplyChanges()
        {
            foreach (var (e, a, val) in commons)
            {
                PropertyInfo prop = type.GetProperty(a) ?? throw new Exception("Something went wrong");
                foreach (var obj in collection)
                {
                    prop.SetValue(obj, val);
                }
            }
        }
    }
}

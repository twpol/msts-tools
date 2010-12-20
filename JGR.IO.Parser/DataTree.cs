//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: New BSD License (BSD).
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Jgr.IO.Parser {
	public interface IDataTreeNode {
		bool HasChildNodes();
		IDataTreeNode GetChildNode(object name);
		IDataTreeNode AppendChildNode(IDataTreeNode child);
		IDataTreeNode InsertChildNode(IDataTreeNode child, IDataTreeNode before);
		IDataTreeNode ReplaceChildNode(IDataTreeNode child, object name, IDataTreeNode oldChild);
		IDataTreeNode RemoveChildNode(IDataTreeNode child);
	}

	public interface IDataTreeNode<T> : IDataTreeNode where T : class, IDataTreeNode {
		// This is a generic method so that individual data tree nodes can be more friendly to use. It doesn't help or
		// affect the general case of using a path to set things.
		T Set(params Func<object, object>[] parameters);
	}

	[Immutable]
	public abstract class DataTreeNode<T> : IDataTreeNode<T> where T : class, IDataTreeNode<T> {
		public DataTreeNode() {
			SetUpTypeData();
		}

		// Keys are field names.
		static Dictionary<string, TypeData> TypeDataCache = InitTypeCache();

		[Immutable]
		protected class TypeData {
			public string Name { get; private set; }
			public ConstructorInfo CloneConstructor { get; private set; }
			public IList<string> CloneNames { get; private set; }
			public IDictionary<string, FieldInfo> CloneFields { get; private set; }

			public TypeData(string name, ConstructorInfo cloneConstructor, IList<string> cloneNames, IDictionary<string, FieldInfo> cloneFields) {
				Name = name;
				CloneConstructor = cloneConstructor;
				CloneNames = cloneNames;
				CloneFields = cloneFields;
			}
		}

		static Dictionary<string, TypeData> InitTypeCache() {
			return new Dictionary<string, TypeData>();
		}

		void SetUpTypeData() {
			lock (TypeDataCache) {
				var type = GetType();
				if (TypeDataCache.ContainsKey(type.FullName)) return;
				Debug.WriteLine("SetUpCloneData(" + type.FullName + "):");

				// Collect all public and non-public fields.
				var fields = type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Select(f => new { Name = f.Name.StartsWith("<") && f.Name.EndsWith(">k__BackingField") ? f.Name.Substring(1, f.Name.Length - 17) : f.Name, Type = f.FieldType.FullName, Field = f });
				Debug.WriteLine("  Fields (type):");
				foreach (var field in fields) Debug.WriteLine("    " + field.Name + " (" + field.Type + ")");

				// Find the constructors!
				var ctors = type.GetConstructors(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var ctor in ctors) {
					var parameters = ctor.GetParameters();
					Debug.WriteLine("  Constructor(" + String.Join(", ", parameters.Select(p => p.Name).ToArray()) + ")");

					// Constructor must have the same number of parameters as there are fields.
					if (parameters.Length != fields.Count()) {
						Debug.WriteLine("    Wrong number of parameters.");
						continue;
					}

					// Constructor's parameters must be named the same (modulo case) as the fields.
					if (fields.Select(f => f.Name).Intersect(parameters.Select(p => p.Name), StringComparer.InvariantCultureIgnoreCase).Count() != fields.Count()) {
						Debug.WriteLine("    Wrong parameter names.");
						continue;
					}

					// Set up containers for this constructor.
					var cloneConstructor = ctor;
					var cloneNames = new List<string>(parameters.Length);
					var cloneFields = new Dictionary<string, FieldInfo>();
					//var cloneProperties = new Dictionary<string, PropertyInfo>();
					for (var i = 0; i < parameters.Length; i++) {
						// Find the field for this parameter.
						var field = fields.FirstOrDefault(p => p.Name.Equals(parameters[i].Name, StringComparison.InvariantCultureIgnoreCase));
						// Parameter and field types must match.
						if (field == null) {
							cloneConstructor = null;
							Debug.WriteLine(String.Format("    Parameter '{0}' does not match any fields. This should never happen.'.", i));
							break;
						}
						if (field.Type != parameters[i].ParameterType.FullName) {
							cloneConstructor = null;
							Debug.WriteLine(String.Format("    Parameter '{0}' is not of type '{1}'.", i, field.Type));
							break;
						}
						cloneNames.Add(field.Name);
						cloneFields[field.Name] = field.Field;
					}
					if (cloneConstructor != null) {
						Debug.WriteLine("  Immutable Copy constructor:");
						foreach (var name in cloneNames) {
							Debug.WriteLine("    " + cloneFields[name].Name + " (" + cloneFields[name].FieldType.FullName + ")");
						}
						TypeDataCache[type.FullName] = new DataTreeNode<T>.TypeData(type.FullName, cloneConstructor, cloneNames, cloneFields);
						return;
					}
				}
				throw new InvalidOperationException("Cannot find suitable immutable copy constructor for '" + type.FullName + "'.");
			}
		}

		protected virtual void SetArgument(string name, object value, ref Dictionary<string, object> arguments, ref TypeData typeData) {
			throw new InvalidOperationException("Field '" + name + "' does not exist in '" + typeData.Name + "'.");
		}

		T Set(params DataTreeNodeSet[] parameters) {
			var typeData = TypeDataCache[GetType().FullName];
			// Set up the new data for the clone we'll be returning.
			var arguments = new Dictionary<string, object>(typeData.CloneNames.Count);
			// Fill in the arguments with the values from the object itself.
			foreach (var argument in typeData.CloneNames) {
				arguments[argument] = typeData.CloneFields[argument].GetValue(this);
			}
			// Collect (and verify) the data provided by the caller.
			foreach (var parameter in parameters) {
				var name = parameter.Name;
				if (typeData.CloneFields.ContainsKey(name)) {
					var field = typeData.CloneFields[name];
					arguments[field.Name] = parameter.Func(arguments[field.Name]);
				} else {
					SetArgument(name, parameter.Func(null), ref arguments, ref typeData);
				}
			}
			// Construct the clone!
			return (T)typeData.CloneConstructor.Invoke(typeData.CloneNames.Select(n => arguments[n]).ToArray());
		}

		#region IDataTreeNode Members

		public bool HasChildNodes() {
			return false;
		}

		public IDataTreeNode GetChildNode(object name) {
			Debug.Assert(name is string);

			var type = GetType();
			var fields = type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.Name.Equals((string)name, StringComparison.InvariantCultureIgnoreCase));
			if (fields.Count() != 1) throw new InvalidDataException("Cannot find a single public or private instance field matching '" + name + "'.");
			var field = fields.First();

			if (!typeof(IDataTreeNode).IsAssignableFrom(field.FieldType)) throw new InvalidCastException("Cannot cast field '" + field.Name + "' to 'IDataTreeNode'.");
			return (IDataTreeNode)field.GetValue(this);
		}

		public IDataTreeNode AppendChildNode(IDataTreeNode child) {
			throw new NotImplementedException();
		}

		public IDataTreeNode InsertChildNode(IDataTreeNode child, IDataTreeNode before) {
			throw new NotImplementedException();
		}

		public IDataTreeNode ReplaceChildNode(IDataTreeNode child, object name, IDataTreeNode oldChild) {
			if (name == null) throw new InvalidOperationException("DataTreeNode is not a container and cannot contain children.");
			Debug.Assert(name is string);
			return (IDataTreeNode)Set(new DataTreeNodeSet((string)name, _ => child));
		}

		public IDataTreeNode RemoveChildNode(IDataTreeNode child) {
			throw new NotImplementedException();
		}

		#endregion

		#region IDataTreeNode<T> Members

		public T Set(params Func<object, object>[] parameters) {
			return Set(parameters.Select(p => new DataTreeNodeSet(p.Method.GetParameters()[0].Name, p)).ToArray());
		}

		#endregion
	}

	[Immutable]
	public class DataTreeNodeSet {
		public string Name { get; private set; }
		public Func<object, object> Func { get; private set; }

		public DataTreeNodeSet(string name, Func<object, object> func) {
			Name = name;
			Func = func;
		}
	}

	public static class DataTreeExtensions {
		public static DataTreePath<T> GetPathList<T>(this T self, IEnumerable steps) where T : class, IDataTreeNode {
			var node = (IDataTreeNode)self;
			var path = new List<DataTreePathStep>();
			path.Add(new DataTreePathStep(null, self));
			foreach (var step in steps) {
				node = node.GetChildNode(step);
				path.Add(new DataTreePathStep(step, node));
			}
			return new DataTreePath<T>(path);
		}

		public static DataTreePath<T> GetPath<T>(this T self, params object[] steps) where T : class, IDataTreeNode {
			return self.GetPathList(steps);
		}

		public static DataTreePath<T> AddPathList<T>(this DataTreePath<T> self, IEnumerable steps) where T : class, IDataTreeNode {
			var node = self.Last().Node;
			var path = new List<DataTreePathStep>(self);
			foreach (var step in steps) {
				node = node.GetChildNode(step);
				path.Add(new DataTreePathStep(step, node));
			}
			return new DataTreePath<T>(path);
		}

		public static DataTreePath<T> AddPath<T>(this DataTreePath<T> self, params object[] steps) where T : class, IDataTreeNode {
			return self.AddPathList(steps);
		}
	}

	[Immutable]
	public class DataTreePath<T> : ReadOnlyCollection<DataTreePathStep> {
		public DataTreePath(IList<DataTreePathStep> path)
			: base(path) {
		}

		public T Set<U>(U newValue) where U : class, IDataTreeNode {
			var path = new List<IDataTreeNode>(this.Select(s => s.Node));
			path[Count - 1] = newValue;
			for (var i = Count - 2; i >= 0; i--) {
				path[i] = path[i].ReplaceChildNode(path[i + 1], this[i + 1].Name, this[i + 1].Node);
			}
			return (T)path[0];
		}

		/// <summary>
		/// Appends <paramref name="child"/> to the last step in the <see cref="DataTreePath"/>.
		/// </summary>
		/// <param name="child">The child to append.</param>
		/// <returns>The new root of the tree.</returns>
		public T Append<U>(U child) where U : class, IDataTreeNode {
			var path = new List<IDataTreeNode>(this.Select(s => s.Node));
			path[Count - 1] = path[Count - 1].AppendChildNode(child);
			for (var i = Count - 2; i >= 0; i--) {
				path[i] = path[i].ReplaceChildNode(path[i + 1], this[i + 1].Name, this[i + 1].Node);
			}
			return (T)path[0];
		}

		/// <summary>
		/// Inserts <paramref name="child"/> before the last step in the <see cref="DataTreePath"/>.
		/// </summary>
		/// <param name="child">The child to insert.</param>
		/// <returns>The new root of the tree.</returns>
		public T Insert<U>(U child) where U : class, IDataTreeNode {
			var path = new List<IDataTreeNode>(this.Select(s => s.Node));
			path[Count - 2] = path[Count - 2].InsertChildNode(child, path[Count - 1]);
			for (var i = Count - 3; i >= 0; i--) {
				path[i] = path[i].ReplaceChildNode(path[i + 1], this[i + 1].Name, this[i + 1].Node);
			}
			return (T)path[0];
		}

		/// <summary>
		/// Removes the last step in the <see cref="DataTreePath"/> from its parent.
		/// </summary>
		/// <returns>The new root of the tree.</returns>
		public T Remove() {
			var path = new List<IDataTreeNode>(this.Select(s => s.Node));
			path[Count - 2] = path[Count - 2].RemoveChildNode(path[Count - 1]);
			for (var i = Count - 3; i >= 0; i--) {
				path[i] = path[i].ReplaceChildNode(path[i + 1], this[i + 1].Name, this[i + 1].Node);
			}
			return (T)path[0];
		}
	}

	[Immutable]
	public class DataTreePathStep {
		public object Name { get; private set; }
		public IDataTreeNode Node { get; private set; }

		public DataTreePathStep(object name, IDataTreeNode node) {
			Name = name;
			Node = node;
		}
	}
}

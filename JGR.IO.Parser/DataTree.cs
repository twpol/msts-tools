﻿//------------------------------------------------------------------------------
// Jgr.IO.Parser library, part of MSTS Editors & Tools (http://jgrmsts.codeplex.com/).
// License: Microsoft Public License (Ms-PL).
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;

namespace Jgr.IO.Parser {
	public interface IDataTreeNode {
		bool HasChildNodes();
		IDataTreeNode GetChildNode(object name);
		IDataTreeNode ReplaceChildNode(IDataTreeNode child, object name, IDataTreeNode oldChild);
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
			public readonly string Name;
			public readonly ConstructorInfo CloneConstructor;
			public readonly IList<string> CloneNames;
			public readonly IDictionary<string, FieldInfo> CloneFields;

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
				Console.WriteLine("SetUpCloneData(" + type.FullName + "):");

				var fields = type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				Console.WriteLine("  Fields (type):");
				foreach (var field in fields) Console.WriteLine("    " + field.Name + " (" + field.FieldType.FullName + ")");

				var dataNames = fields.Select(f => f.Name);
				var ctors = type.GetConstructors(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var ctor in ctors) {
					var parameters = ctor.GetParameters();
					// Constructor must have the same number of parameters as there are fields.
					if (parameters.Length != fields.Length) continue;
					// Constructor's parameters must be named the same (modulo case) as the fields.
					if (dataNames.Intersect(parameters.Select(p => p.Name), StringComparer.InvariantCultureIgnoreCase).Count() != fields.Length) continue;
					// Set up containers for this constructor.
					var cloneConstructor = ctor;
					var cloneNames = new List<string>(parameters.Length);
					var cloneFields = new Dictionary<string, FieldInfo>();
					//var cloneProperties = new Dictionary<string, PropertyInfo>();
					for (var i = 0; i < parameters.Length; i++) {
						// Find the field for this parameter.
						var field = fields.FirstOrDefault(p => p.Name.Equals(parameters[i].Name, StringComparison.InvariantCultureIgnoreCase));
						// Parameter and field types must match.
						if ((field == null) || (field.FieldType.FullName != parameters[i].ParameterType.FullName)) {
							cloneConstructor = null;
							break;
						}
						cloneNames.Add(field.Name);
						cloneFields[field.Name] = field;
					}
					if (cloneConstructor != null) {
						Console.WriteLine("  Immutable Copy constructor:");
						foreach (var name in cloneNames) {
							Console.WriteLine("    " + cloneFields[name].Name + " (" + cloneFields[name].FieldType.FullName + ")");
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

		public IDataTreeNode ReplaceChildNode(IDataTreeNode child, object name, IDataTreeNode oldChild) {
			if (name == null) throw new InvalidOperationException("DataTreeNode is not a container and cannot contain children.");
			Debug.Assert(name is string);
			return (IDataTreeNode)Set(new DataTreeNodeSet((string)name, _ => child));
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
		public readonly string Name;
		public readonly Func<object, object> Func;

		public DataTreeNodeSet(string name, Func<object, object> func) {
			Name = name;
			Func = func;
		}
	}

	public static class DataTreeExtensions {
		public static DataTreePath GetPath(this IDataTreeNode self, params object[] steps) {
			var path = new List<DataTreePathStep>(steps.Length);
			path.Add(new DataTreePathStep(null, self));
			foreach (var step in steps) {
				self = self.GetChildNode(step);
				path.Add(new DataTreePathStep(step, self));
			}
			return new DataTreePath(path);
		}
	}

	[Immutable]
	public class DataTreePath : ReadOnlyCollection<DataTreePathStep> {
		public DataTreePath(IList<DataTreePathStep> path)
			: base(path) {
		}

		public object Set<T>(T newValue) where T : class, IDataTreeNode {
			var path = new List<IDataTreeNode>(this.Select(s => s.Node));
			path[Count - 1] = newValue;
			for (var i = Count - 2; i >= 0; i--) {
				path[i] = path[i].ReplaceChildNode(path[i + 1], this[i + 1].Name, this[i + 1].Node);
			}
			return path[0];
		}
	}

	[Immutable]
	public class DataTreePathStep {
		public readonly object Name;
		public readonly IDataTreeNode Node;

		public DataTreePathStep(object name, IDataTreeNode node) {
			Name = name;
			Node = node;
		}
	}
}
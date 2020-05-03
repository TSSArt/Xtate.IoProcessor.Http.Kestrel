﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	[DebuggerTypeProxy(typeof(DebugView))]
	[DebuggerDisplay(value: "Count = {_properties.Count}")]
	public sealed class DataModelObject : IDynamicMetaObjectProvider, IFormattable
	{
		public delegate void ChangedHandler(ChangedAction action, string property, DataModelDescriptor descriptor);

		public enum ChangedAction
		{
			Set,
			Remove
		}

		public static readonly DataModelObject Empty = new DataModelObject(DataModelAccess.Constant, capacity: 0);

		private readonly Dictionary<string, DataModelDescriptor> _properties;

		private DataModelAccess _access;

		public DataModelObject() : this(capacity: 0) { }
		
		public DataModelObject(int capacity) : this(DataModelAccess.Writable, capacity) { }
		
		internal DataModelObject(bool isReadOnly, int capacity) : this(isReadOnly ? DataModelAccess.ReadOnly : DataModelAccess.Writable, capacity) { }

		private DataModelObject(DataModelAccess access, int capacity)
		{
			_access = access;
			_properties = new Dictionary<string, DataModelDescriptor>(capacity);
		}

		public void EnsureCapacity(int capacity)
		{
#if NETSTANDARD2_1
			_properties.EnsureCapacity(capacity);
#endif
		}

		public DataModelAccess Access
		{
			get => _access;

			internal set
			{
				if (value == _access)
				{
					return;
				}

				if (value == DataModelAccess.ReadOnly && _access == DataModelAccess.Writable)
				{
					_access = DataModelAccess.ReadOnly;

					return;
				}

				if (value == DataModelAccess.Constant)
				{
					_access = DataModelAccess.Constant;

					foreach (var pair in _properties)
					{
						pair.Value.Value.MakeDeepConstant();
					}

					return;
				}

				throw new StateMachineInfrastructureException(Resources.Exception_Access_can_t_be_changed);
			}
		}

		public ICollection<string> Properties => _properties.Keys;
		
		public int Count => _properties.Count;

		public DataModelValue this[string property]
		{
			get
			{
				if (property == null) throw new ArgumentNullException(nameof(property));

				return GetDescriptor(property).Value;
			}
			set
			{
				if (property == null) throw new ArgumentNullException(nameof(property));

				if (!CanSet(property))
				{
					throw ObjectCantBeModifiedException();
				}

				SetInternal(property, new DataModelDescriptor(value));
			}
		}

	#region Interface IDynamicMetaObjectProvider

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

	#endregion

	#region Interface IFormattable

		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			var sb = new StringBuilder();

			sb.Append('(');
			foreach (var pair in _properties)
			{
				if (sb.Length > 1)
				{
					sb.Append(',');
				}

				sb.Append(pair.Key).Append('=').Append(pair.Value.Value.ToString(format: null, formatProvider));
			}

			sb.Append(')');

			return sb.ToString();
		}

	#endregion

		public event ChangedHandler? Changed;

		public void MakeReadOnly() => Access = DataModelAccess.ReadOnly;

		public void MakeDeepConstant() => Access = DataModelAccess.Constant;

		public DataModelObject CloneAsWritable() => DeepClone(DataModelAccess.Writable);

		public DataModelObject CloneAsReadOnly() => DeepClone(DataModelAccess.ReadOnly);

		public DataModelObject AsConstant() => DeepClone(DataModelAccess.Constant);

		private static InvalidOperationException ObjectCantBeModifiedException() => new InvalidOperationException(Resources.Exception_Object_can_not_be_modified);

		internal DataModelDescriptor GetDescriptor(string property) => _properties.TryGetValue(property, out var descriptor) ? descriptor : new DataModelDescriptor(DataModelValue.Undefined);

		internal void SetInternal(string property, DataModelDescriptor descriptor)
		{
			if (property == null) throw new ArgumentNullException(nameof(property));

			if (_access == DataModelAccess.Constant)
			{
				throw ObjectCantBeModifiedException();
			}

			if (_properties.TryGetValue(property, out var oldDescriptor))
			{
				Changed?.Invoke(ChangedAction.Remove, property, oldDescriptor);
			}

			_properties[property] = descriptor;

			Changed?.Invoke(ChangedAction.Set, property, descriptor);
		}

		internal void RemoveInternal(string property)
		{
			if (property == null) throw new ArgumentNullException(nameof(property));

			if (_access == DataModelAccess.Constant)
			{
				throw ObjectCantBeModifiedException();
			}

			if (_properties.TryGetValue(property, out var oldDescriptor))
			{
				Changed?.Invoke(ChangedAction.Remove, property, oldDescriptor);
			}

			_properties.Remove(property);
		}

		public bool Contains(string property)
		{
			if (property == null) throw new ArgumentNullException(nameof(property));

			return _properties.ContainsKey(property);
		}

		public bool CanSet(string property) => _access == DataModelAccess.Writable && !(_properties.TryGetValue(property, out var descriptor) && descriptor.IsReadOnly);

		public bool CanRemove(string property) => _access == DataModelAccess.Writable && !(_properties.TryGetValue(property, out var descriptor) && descriptor.IsReadOnly);

		public void Remove(string property)
		{
			if (!CanRemove(property))
			{
				throw ObjectCantBeModifiedException();
			}

			RemoveInternal(property);
		}

		public DataModelObject DeepClone(DataModelAccess targetAccess)
		{
			Dictionary<object, object>? map = null;

			return DeepCloneWithMap(targetAccess, ref map);
		}

		internal DataModelObject DeepCloneWithMap(DataModelAccess targetAccess, ref Dictionary<object, object>? map)
		{
			if (targetAccess == DataModelAccess.Constant)
			{
				if (_properties.Count == 0)
				{
					return Empty;
				}

				if (_access == DataModelAccess.Constant)
				{
					return this;
				}
			}

			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(this, out var val))
			{
				return (DataModelObject) val;
			}

			var clone = new DataModelObject(targetAccess, _properties.Count);

			map[this] = clone;

			foreach (var pair in _properties)
			{
				clone._properties[pair.Key] = new DataModelDescriptor(pair.Value.Value.DeepCloneWithMap(targetAccess, ref map), targetAccess != DataModelAccess.Writable);
			}

			return clone;
		}

		public override string ToString() => ToString(format: null, formatProvider: null);

		[DebuggerDisplay(value: "{" + nameof(_value) + "}", Name = "{" + nameof(_name) + ",nq}")]
		private struct NameValue
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly string _name;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			private readonly DataModelValue _value;

			public NameValue(string name, DataModelValue value)
			{
				_name = name;
				_value = value;
			}
		}

		[PublicAPI]
		[ExcludeFromCodeCoverage]
		private class DebugView
		{
			private readonly DataModelObject _dataModelObject;

			public DebugView(DataModelObject dataModelObject) => _dataModelObject = dataModelObject;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public NameValue[] Items =>
					_dataModelObject
							._properties.OrderBy(p => p.Key)
							.Select(p => new NameValue(p.Key, p.Value.Value))
							.ToArray();
		}

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default!);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelObject) })!;

			private readonly DataModelObject _obj;

			public Dynamic(DataModelObject obj) => _obj = obj;

			public static DynamicMetaObject CreateMetaObject(Expression expression)
			{
				var newExpression = Expression.New(ConstructorInfo, Expression.Convert(expression, typeof(DataModelObject)));
				return Instance.GetMetaObject(newExpression);
			}

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				if (binder == null) throw new ArgumentNullException(nameof(binder));

				result = _obj[binder.Name].ToObject();

				return true;
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				if (binder == null) throw new ArgumentNullException(nameof(binder));

				_obj[binder.Name] = DataModelValue.FromObject(value);

				return true;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
			{
				if (indexes.Length == 1 && indexes[0] is string key)
				{
					result = _obj[key].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
			{
				if (indexes.Length == 1 && indexes[0] is string key)
				{
					_obj[key] = DataModelValue.FromObject(value);

					return true;
				}

				return false;
			}

			public override bool TryConvert(ConvertBinder binder, out object? result)
			{
				if (binder.Type == typeof(DataModelObject))
				{
					result = _obj;

					return true;
				}

				if (binder.Type == typeof(DataModelValue))
				{
					result = new DataModelValue(_obj);

					return true;
				}

				result = default;

				return false;
			}

			public override IEnumerable<string> GetDynamicMemberNames() => _obj.Properties;
		}
	}
}
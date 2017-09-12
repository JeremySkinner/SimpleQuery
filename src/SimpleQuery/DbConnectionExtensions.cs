#region License

// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at https://github.com/JeremySkinner/SimpleQuery

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleQuery
{
	// Single-file version based on IdbConnection ext. methods
	public static class DbConnectionExtensions
	{
		public static T QuerySingle<T>(this IDbConnection conn, string commandText, object parameters = null)
		{
			return Query<T>(conn, commandText, parameters).Single();
		}

		public static T QuerySingleOrDefault<T>(this IDbConnection conn, string commandText, object parameters = null)
		{
			return Query<T>(conn, commandText, parameters).SingleOrDefault();
		}

		public static IEnumerable<T> Query<T>(this IDbConnection conn, string commandText, object parameters = null)
		{
			var results = new List<T>();

			using (var command = conn.CreateCommand())
			{
				command.CommandText = commandText;
				AddParameters(command, parameters);

				if (typeof(T).IsPrimitive)
				{
					using (var reader = command.ExecuteReader()) {
						while (reader.Read())
						{
							if (reader.FieldCount > 0)
							{
								object value = reader.GetValue(0);
								if (value == DBNull.Value)
								{
									results.Add(default(T));
								}
								else
								{
									results.Add((T)value);
								}
							}
						}
					}
				}
				else
				{
					MapComplexType(command, results);
				}
				
			}
			return results;
		}

		private static void MapComplexType<T>(IDbCommand command, List<T> results)
		{
			IEnumerable<string> columnNames = null;

			var mapper = Mapper<T>.Create();

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					if (columnNames == null)
					{
						columnNames = GetColumnNames(reader).ToList();
					}
					results.Add(mapper.Map(reader, columnNames));
				}
			}
		}

		public static void Execute(this IDbConnection conn, string commandText, object parameters = null)
		{
			using (var command = conn.CreateCommand())
			{
				command.CommandText = commandText;
				AddParameters(command, parameters);
				command.ExecuteNonQuery();
			}
		}

		public static int GetLastInsertId(this IDbConnection conn)
		{
			using (var command = conn.CreateCommand())
			{
				command.CommandText = "SELECT cast(@@identity as int)";
				return (int) command.ExecuteScalar();
			}
		}

		private static void AddParameters(IDbCommand command, object parameters)
		{
			if (parameters == null) return;

			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(parameters))
			{
				var parameter = command.CreateParameter();
				parameter.ParameterName = "@" + descriptor.Name;
				parameter.Value = descriptor.GetValue(parameters) ?? DBNull.Value;
				command.Parameters.Add(parameter);
			}
		}

		private static IEnumerable<string> GetColumnNames(IDataRecord record)
		{
			int i = 0;
			while (true)
			{
				if (i >= record.FieldCount)
				{
					yield break;
				}
				yield return record.GetName(i);
				i++;
			}
		}

		private class Mapper<T> {
			private readonly Func<T> factory;
			private readonly Dictionary<string, PropertyMetadata<T>> properties;
			private static readonly Lazy<Mapper<T>> instanceCache = new Lazy<Mapper<T>>(() => new Mapper<T>());

			public static Mapper<T> Create() {
				return instanceCache.Value;
			}

			private Mapper() {
				this.properties = new Dictionary<string, PropertyMetadata<T>>(StringComparer.InvariantCultureIgnoreCase);

				factory = CreateActivatorDelegate();

				// Get all properties that are writeable without a NotMapped attribute.
				var properties = from property in typeof(T).GetProperties()
								 where property.CanWrite
								 select property;

				foreach (var property in properties) {
					var propertyMetadata = new PropertyMetadata<T>(property);
					this.properties[propertyMetadata.PropertyName] = propertyMetadata;
				}
			}

			public T Map(IDataRecord record, IEnumerable<string> columns) {
				var instance = factory();

				foreach (var column in columns) {
					PropertyMetadata<T> property;

					if (properties.TryGetValue(column, out property)) {
						try {
							property.SetValue(instance, record[column]);
						} catch (InvalidCastException e) {
							throw MappingException.InvalidCast(column, e);
						}
					}
				}

				return instance;
			}

			private static Func<T> CreateActivatorDelegate() {
				var constructor = typeof(T).GetConstructor(Type.EmptyTypes);

				// No parameterless constructor found.
				if (constructor == null) {
					return () => { throw MappingException.NoParameterlessConstructor(typeof(T)); };
				}

				return Expression.Lambda<Func<T>>(Expression.New(constructor)).Compile();
			}

			public IEnumerable<PropertyMetadata<T>> GetIdColumns() {
				var cols = this.properties.Where(x => x.Value.IsId).Select(x => x.Value).ToList();

				if (cols.Count == 0) {
					throw new NotSupportedException(string.Format("No PK properties were defined on type {0}.", typeof(T).Name));
				}

				return cols;
			}

			public string ConvertPropertyNameToColumnName(string propertyName) {
				return properties.Where(x => x.Value.Property.Name == propertyName).Select(x => x.Key).SingleOrDefault();
			}
		}

		private class PropertyMetadata<T> {
			private readonly Action<T, object> setter;
			private readonly Func<T, object> getter;

			public PropertyMetadata(PropertyInfo property) {
				this.Property = property;
				this.setter = BuildSetterDelegate(property);
				this.getter = BuildGetterDelegate(property);
				PropertyName = property.Name;
			}

			public bool IsId { get; private set; }

			public PropertyInfo Property { get; }

			public string PropertyName { get; protected set; }

			public bool IsAutoGenerated { get; internal set; }

			public void SetValue(T instance, object value) {
				if (value == DBNull.Value) {
					value = null; //TODO: Handle this more robustly
				}
				setter(instance, value);
			}

			public object GetValue(T instance) {
				return getter(instance);
			}

			private static Action<T, object> BuildSetterDelegate(PropertyInfo prop) {
				var instance = Expression.Parameter(typeof(T), "x");
				var argument = Expression.Parameter(typeof(object), "v");

				var setterCall = Expression.Call(
					instance,
					prop.GetSetMethod(true),
					Expression.Convert(argument, prop.PropertyType));

				return (Action<T, object>)Expression.Lambda(setterCall, instance, argument).Compile();
			}

			private Func<T, object> BuildGetterDelegate(PropertyInfo prop) {
				var param = Expression.Parameter(typeof(T), "x");
				Expression expression = Expression.PropertyOrField(param, prop.Name);

				if (prop.PropertyType.IsValueType)
					expression = Expression.Convert(expression, typeof(object));

				return Expression.Lambda<Func<T, object>>(expression, param)
					.Compile();
			}
		}

		public class MappingException : Exception {
			public MappingException(string message, Exception innerException) : base(message, innerException) {
			}

			public MappingException(string message) : base(message) {
			}

			public static MappingException InvalidCast(string column, Exception innerException) {
				string message = string.Format("Could not map the property '{0}' as its data type does not match the database.",
					column);
				return new MappingException(message, innerException);
			}

			public static MappingException NoParameterlessConstructor(Type type) {
				string message =
					"Could not find a parameterless constructor on the type '{0}'. SimpleQuery can only be used to map types that have a public, parameterless constructor.";
				message = string.Format(message, type.FullName);
				return new MappingException(message);
			}
		}

	}
}
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
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SimpleQuery {
	public class Connection : IDisposable {
		private bool _shouldDisposeConnection = false;

		private readonly IDbConnection _underlyingConnection;
		private TextWriter _log = TextWriter.Null;

		public TextWriter Log {
			get { return _log; }
			set {
				if (value == null) throw new ArgumentNullException("value");
				_log = value;
			}
		}

		public Connection(IDbConnection underlyingConnection) {
			_underlyingConnection = underlyingConnection;
		}

		public static Connection Open(string connectionStringName) {
			var connectionStringElement = ConfigurationManager.ConnectionStrings[connectionStringName];

			var factory = DbProviderFactories.GetFactory(connectionStringElement.ProviderName);
			var underlyingConnection = factory.CreateConnection();

			if (underlyingConnection == null) {
				throw new Exception(string.Format("Could not create connection for connection string '{0}'", connectionStringName));
			}

			underlyingConnection.ConnectionString = connectionStringElement.ConnectionString;
			underlyingConnection.Open();

			return new Connection(underlyingConnection) {_shouldDisposeConnection = true};
		}

		public void Dispose() {
			if (_shouldDisposeConnection)
				_underlyingConnection.Dispose();
		}

		public int Execute(string commandText, params object[] args) {
			using (var command = _underlyingConnection.CreateCommand()) {
				command.CommandText = commandText;
				AddParameters(command, args);
				Log.WriteLine(command.CommandText);
				return command.ExecuteNonQuery();
			}
		}

		public dynamic GetLastInsertId() {
			using (var command = _underlyingConnection.CreateCommand()) {
				command.CommandText = "SELECT @@Identity"; //TODO: Was nice if this was agnostic.
				Log.WriteLine(command.CommandText);
				return command.ExecuteScalar();
			}
		}

		public IEnumerable<T> Query<T>(string commandText, params object[] args) {
			var results = new List<T>();

			using (var command = _underlyingConnection.CreateCommand()) {
				command.CommandText = commandText;
				AddParameters(command, args);
				IEnumerable<string> columnNames = null;
				Log.WriteLine(command.CommandText);

				var mapper = Mapper<T>.Create();

				using (var reader = command.ExecuteReader()) {
					while (reader.Read()) {
						if (columnNames == null) {
							columnNames = GetColumnNames(reader).ToList();
						}
						results.Add(mapper.Map(reader, columnNames));
					}
				}
			}
			return results;
		}

		public IEnumerable<T> FindAll<T>() {
			var mapper = Mapper<T>.Create();
			var query = string.Format("select * from {0}", mapper.TableName);
			return Query<T>(query);
		}

		public T FindById<T>(object id) {
			var mapper = Mapper<T>.Create();

			Dictionary<string, object> idParameters;

			if (mapper.HasCompositeKey) {
				idParameters = new AnonymousTypeDictionary(id).ToDictionary(x => mapper.ConvertPropertyNameToColumnName(x.Key),
				                                                            x => x.Value);
			}
			else {
				idParameters = new Dictionary<string, object>();
				idParameters[mapper.GetIdColumns().Single().PropertyName] = id;
			}

			int paramCount = 0;
			var whereClauses = new List<string>();

			foreach (var pair in idParameters) {
				whereClauses.Add(string.Format("{0} = @{1}", pair.Key, paramCount++));
			}

			string whereClause = string.Join(" AND ", whereClauses);
			var parameters = idParameters.Select(x => x.Value).ToArray();

			var query = string.Format("select * from {0} where {1}", mapper.TableName, whereClause);
			return Query<T>(query, parameters).SingleOrDefault();
		}

		public void Insert<T>(T toInsert) {
			var mapper = Mapper<T>.Create();
			var results = mapper.MapToInsert(toInsert);
			var sql = results.Item1;
			var parameters = results.Item2;

			Execute(sql, parameters);

			if (mapper.HasGeneratedId) {
				var id = (int) GetLastInsertId(); //TODO: Do not assume int. 
				var idColumn = mapper.GetIdColumns().Single();
				idColumn.SetValue(toInsert, id);
			}
		}

		public void Update<T>(T toUpdate) {
			var mapper = Mapper<T>.Create();
			var results = mapper.MapToUpdate(toUpdate);
			var sql = results.Item1;
			var parameters = results.Item2;

			Log.WriteLine(sql);

			Execute(sql, parameters);
		}

		private static void AddParameters(IDbCommand command, object[] args) {
			if (args != null) {
				var parameters = args.Select((arg, index) => {
					var parameter = command.CreateParameter();
					parameter.ParameterName = index.ToString(CultureInfo.InvariantCulture);
					parameter.Value = arg ?? DBNull.Value;
					return parameter;
				});

				foreach (var parameter in parameters) {
					command.Parameters.Add(parameter);
				}
			}
		}

		private static IEnumerable<string> GetColumnNames(IDataRecord record) {
			int i = 0;
			while (true) {
				if (i >= record.FieldCount) {
					yield break;
				}
				yield return record.GetName(i);
				i++;
			}
		}
	}
}
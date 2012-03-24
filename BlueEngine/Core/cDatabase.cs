//----------------------------------------------------------------------------------------------------
// cDatabase
// : 데이터베이스 오브젝트
//  -JHL-2012-03-16
//----------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Data.SqlClient;	//MSSQL Client
using System.Data.OracleClient;	//Oracle Client
using MySql.Data.MySqlClient;	//MySQL Client
using Mono.Data.SqliteClient;	//SQLite Client

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 데이터베이스 오브젝트
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cDatabase : cObject
	{
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 타입
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public enum eType
		{
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// SQLite
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			SQLite,
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// MySQL
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			MySQL,
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// MSSQL
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			MSSQL,
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// Oracle
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			Oracle
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 커넥션 플링 사이즈(최소).
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public const	int	MIN_CONNECTION_POOL_SIZE	=0;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 커넥션 플링 사이즈(최대).
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public const	int	MAX_CONNECTION_POOL_SIZE	= 100;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터베이스 주소.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		string					m_address;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터베이스 이름.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		string					m_dbname;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터베이스 계정 아이디.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		string					m_id;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터베이스 계정 비밀번호.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		string					m_pw;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 타입
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		eType					m_type;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB커넥션
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		DbConnection			m_connection;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB커멘더
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		DbCommand				m_command;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 리더
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		IDataReader				m_reader;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최근 에러메시지
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		string					m_error;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 리더
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public IDataReader		Reader				{get{return m_reader;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB 커넥션
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public DbConnection		Connection			{get{return m_connection;}}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="type">타입</param>
		//----------------------------------------------------------------------------------------------------
		public cDatabase( eType type )
		{
			m_type	= type;
			m_error = "";
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파괴자
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		~cDatabase()
		{
			Close();
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB접속
		/// </summary>
		/// <param name="address">서버주소</param>
		/// <param name="dbname">DB명</param>
		/// <param name="id">ID</param>
		/// <param name="pw">PW</param>
		//----------------------------------------------------------------------------------------------------
		public bool Connect( string address, string dbname, string id, string pw )
		{
			m_address	= address;
			m_dbname	= dbname;
			m_id		= id;
			m_pw		= pw;
			return Connect();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB접속
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public bool Connect()
		{
			try
			{
				if( m_connection == null )
				{
					string connect_string = "Server="+m_address+";Database="+m_dbname+";User ID="+m_id+";Password="+m_pw+";Pooling=true;Min Pool Size="+MIN_CONNECTION_POOL_SIZE+";Max Pool Size="+MAX_CONNECTION_POOL_SIZE+";";
					switch(m_type)
					{
					case eType.SQLite:	m_connection = new SqliteConnection(connect_string);	break;
					case eType.MySQL:	m_connection = new MySqlConnection(connect_string);		break;
					case eType.MSSQL:	m_connection = new SqlConnection(connect_string);		break;
					case eType.Oracle:	m_connection = new OracleConnection(connect_string);	break;
					}
				}
				if( m_connection.State == ConnectionState.Closed )
				{
					m_connection.Open();
				}
				return true;
			}
			catch( Exception ex )
			{
				Log( ex.ToString() );
				m_error = ex.Message;
				Close();
				return false;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 요청 ( SELECT )
		/// </summary>
		/// <param name="sql">SQL문</param>
		/// <returns>성공 유무</returns>
		//----------------------------------------------------------------------------------------------------
		public bool Query( string sql )
		{
			try
			{
				CloseQuery();
				m_command = m_connection.CreateCommand();
				m_command.CommandText = sql;
				m_reader = m_command.ExecuteReader();
				return true;
			}
			catch( Exception ex )
			{
				Log( ex.ToString() );
				m_error = ex.Message;
				CloseQuery();
				return false;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 요청 ( UPDATE, INSERT, DELETE ... )
		/// </summary>
		/// <param name="sql">SQL문</param>
		/// <returns>성공 유무</returns>
		//----------------------------------------------------------------------------------------------------
		public bool NonQuery( string sql )
		{
			try
			{
				CloseQuery();
				m_command = m_connection.CreateCommand();
				m_command.CommandType = CommandType.Text;
				m_command.CommandText = sql;
				m_command.ExecuteNonQuery();
				CloseQuery();
				return true;
			}
			catch( Exception ex )
			{
				Log( ex.ToString() );
				m_error = ex.Message;
				CloseQuery();
				return false;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 요청 시작 ( StoredProcedure )
		/// </summary>
		/// <param name="sql">SQL문</param>
		/// <returns>성공 유무</returns>
		//----------------------------------------------------------------------------------------------------
		public void QueryProcedureStart( string sql )
		{
			CloseQuery();
			m_command = m_connection.CreateCommand();
			m_command.CommandType = CommandType.StoredProcedure;
			m_command.CommandText = sql;
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 파라메타 추가 : 입력
		/// </summary>
		/// <param name="name">필드명</param>
		/// <param name="value">데이터</param>
		/// <param name="type">데이터타입</param>
		/// <param name="size">데이터크기(bytes)</param>
		//----------------------------------------------------------------------------------------------------
		public void AddInputParameter( string name, object value, object type, int size )
		{
			DbParameter param = null;
			switch(m_type)
			{
			case eType.SQLite:	param = new SqliteParameter( name, type );	break;
			case eType.MySQL:	param = new MySqlParameter( name, type );	break;
			case eType.MSSQL:	param = new SqlParameter( name, type );		break;
			case eType.Oracle:	param = new OracleParameter( name, type );	break;
			}
			param.Value		= value;
			param.Size		= size;
			param.Direction	= ParameterDirection.Input;
			m_command.Parameters.Add( param );
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 파라메타 추가 : 출력
		/// </summary>
		/// <param name="name">필드명</param>
		/// <param name="type">데이터타입</param>
		/// <param name="size">데이터크기(bytes)</param>
		//----------------------------------------------------------------------------------------------------
		public void AddOutputParameter( string name, object type, int size )
		{
			DbParameter param = null;
			switch(m_type)
			{
			case eType.SQLite:	param = new SqliteParameter( name, type );	break;
			case eType.MySQL:	param = new MySqlParameter( name, type );	break;
			case eType.MSSQL:	param = new SqlParameter( name, type );		break;
			case eType.Oracle:	param = new OracleParameter( name, type );	break;
			}
			param.Size		= size;
			param.Direction	= ParameterDirection.Output;
			m_command.Parameters.Add( param );
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 요청 종료( StoredProcedure )
		/// </summary>
		/// <returns>성공 유무</returns>
		//----------------------------------------------------------------------------------------------------
		public bool QueryProcedureEnd()
		{
			try
			{
				m_command.ExecuteNonQuery();
				CloseQuery();
				return true;
			}
			catch( Exception ex )
			{
				Log( ex.ToString() );
				m_error = ex.Message;
				CloseQuery();
				return false;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 아답터 생성
		/// </summary>
		/// <param name="table_name">테이블 이름</param>
		/// <returns>데이터어답터 객체</returns>
		//----------------------------------------------------------------------------------------------------
		public DbDataAdapter CreateAdapter( string table_name )
		{
			if( !Connect() ) return null;

			// Select 커멘더 생성
			DbCommand command = m_connection.CreateCommand();
			command.CommandType = CommandType.Text;
			command.CommandText = "SELECT * FROM " + table_name;

			// 어답터 생성
			DbDataAdapter adapter=null;
			switch(m_type)
			{
			case eType.SQLite:	adapter = new SqliteDataAdapter((SqliteCommand)command);	break;
			case eType.MySQL:	adapter = new MySqlDataAdapter((MySqlCommand)command);		break;
			case eType.MSSQL:	adapter = new SqlDataAdapter((SqlCommand)command);			break;
			case eType.Oracle:	adapter = new OracleDataAdapter((OracleCommand)command);	break;
			}

			// Insert,Update,Delete 컴멘더 생성
			DbCommandBuilder builder = null;
			switch(m_type)
			{
			case eType.SQLite:	builder = new SqliteCommandBuilder();	break;
			case eType.MySQL:	builder = new MySqlCommandBuilder();	break;
			case eType.MSSQL:	builder = new SqlCommandBuilder();		break;
			case eType.Oracle:	builder = new OracleCommandBuilder();	break;
			}
			builder.DataAdapter = adapter;
			adapter.InsertCommand = builder.GetInsertCommand();
			adapter.UpdateCommand = builder.GetUpdateCommand();
			adapter.DeleteCommand = builder.GetDeleteCommand();
			return adapter;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 테이블 읽기
		/// </summary>
		/// <param name="table">테이블 객체</param>
		/// <param name="sql">SQL문</param>
		/// <returns>데이터어답터 객체</returns>
		//----------------------------------------------------------------------------------------------------
		public DbDataAdapter ReadTable( DataTable table, string sql )
		{
			if( !Connect() ) return null;

			DbDataAdapter adapter=null;

			try
			{
				// Select 커멘더 생성
				m_command = m_connection.CreateCommand();
				m_command.CommandType = CommandType.Text;
				m_command.CommandText = sql;

				// 어답터 생성
				switch(m_type)
				{
				case eType.SQLite:	adapter = new SqliteDataAdapter((SqliteCommand)m_command);	break;
				case eType.MySQL:	adapter = new MySqlDataAdapter((MySqlCommand)m_command);	break;
				case eType.MSSQL:	adapter = new SqlDataAdapter((SqlCommand)m_command);		break;
				case eType.Oracle:	adapter = new OracleDataAdapter((OracleCommand)m_command);	break;
				}

				// Insert,Update,Delete 컴멘더 생성
				DbCommandBuilder builder = null;
				switch(m_type)
				{
				case eType.SQLite:	builder = new SqliteCommandBuilder();	break;
				case eType.MySQL:	builder = new MySqlCommandBuilder();	break;
				case eType.MSSQL:	builder = new SqlCommandBuilder();		break;
				case eType.Oracle:	builder = new OracleCommandBuilder();	break;
				}
				builder.DataAdapter = adapter;
				adapter.InsertCommand = builder.GetInsertCommand();
				adapter.UpdateCommand = builder.GetUpdateCommand();
				adapter.DeleteCommand = builder.GetDeleteCommand();

				// 테이블 채워 넣기
				adapter.Fill(table);
				return adapter;
			}
			catch( Exception ex )
			{
				Log( ex.ToString() );
				m_error = ex.Message;
				CloseQuery();
				return null;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 테이블 갱신(INSERT,UPDATE,DELETE...)
		/// </summary>
		/// <param name="table">테이블 객체</param>
		/// <param name="adapter">어답터</param>
		/// <returns>성공유무</returns>
		//----------------------------------------------------------------------------------------------------
		public bool UpdateTable( DataTable table, DbDataAdapter adapter )
		{
			DataTable changed_table = table.GetChanges();
			if( changed_table==null ) return true;

			// 업데이트
			DbTransaction tran = (DbTransaction)m_connection.BeginTransaction();
			try
			{
				//adapter.SelectCommand.Transaction = tran;
				adapter.InsertCommand.Transaction = tran;
				adapter.UpdateCommand.Transaction = tran;
				adapter.DeleteCommand.Transaction = tran;

				adapter.Update(changed_table);
				tran.Commit();
			}
			catch( Exception ex )
			{
				tran.Rollback();
				Log( ex.ToString() );
				m_error = ex.Message;
				return false;
			}
			table.AcceptChanges();
			return true;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쿼리 종료
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public void CloseQuery()
		{
			if( m_reader!=null )
			{
				m_reader.Close();
				m_reader = null;
			}
			if( m_command!=null )
			{
				m_command.Dispose();
				m_command = null;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 커넥션 종료
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public void Close()
		{
			CloseQuery();

			if( m_connection!=null )
			{
				m_connection.Close();
				m_connection = null;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 예제
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		void ReadSample()
		{
			if( Connect( "localhost", "test_db", "root", "bluelab" ) )
			{
				if( Query( "SELECT * FROM user_account" ) )
				{
					while( Reader.Read() )
					{
						ulong	id	= (ulong)Reader["id"];
						string	name= (string)Reader["name"];
						Print( id, name );
					}
					CloseQuery();
				}
				if( NonQuery( "INSERT INTO user_account (id,name) VALUES (10,'test')" ) )
				{
					CloseQuery();
				}
				Close();
			}
		}

	}
}

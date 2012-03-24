//----------------------------------------------------------------------------------------------------
// cDataTables
// :  데이터베이스 테이블 객체
//  -JHL-2012-03-02
//----------------------------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.Common;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	///  데이터베이스 테이블 객체
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cDataTables : cObject
	{
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 테이블
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		private		DataTable		m_table;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터베이스 아답터
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		private		DbDataAdapter	m_adapter;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 키 이름
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		string			KeyName;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 키
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		object			Key;


		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="table">테이블 객체</param>
		/// <param name="adapter">아답터 객체</param>
		/// <param name="key_name">키 이름</param>
		//----------------------------------------------------------------------------------------------------
		public cDataTables( DataTable table, DbDataAdapter adapter, string key_name )
		{
			m_table		= table;
			m_adapter	= adapter;
			KeyName		= key_name;
			Key			= null;
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB로 부터 데이터 읽기
		/// </summary>
		/// <param name="db">데이터베이스 객체</param>
		/// <param name="key">키값</param>
		/// <returns>성공유무</returns>
		//----------------------------------------------------------------------------------------------------
		public bool Read( cDatabase db, object key )
		{
			lock( m_table )
			{
				Key = key;
				m_adapter.SelectCommand.CommandText = "SELECT * FROM " + m_table.TableName + " WHERE " + KeyName + "=" + key;
				m_adapter.Fill(m_table);
				return this[KeyName]==key;
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB에 데이터 갱신
		/// </summary>
		/// <param name="db">데이터베이스 객체</param>
		/// <param name="table">데이터테이블 객체</param>
		/// <param name="adapter">아답터 객체</param>
		/// <returns>성공유무</returns>
		//----------------------------------------------------------------------------------------------------
		public static bool Update( cDatabase db, DataTable table, DbDataAdapter adapter )
		{
			lock( table )
			{
				return db.UpdateTable( table, adapter );
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 컬럼 추가
		/// </summary>
		/// <param name="table">데이터테이블</param>
		/// <param name="type">데이터타입</param>
		/// <param name="name">컬럼명</param>
		/// <param name="auto_inc">자동증가유무</param>
		/// <returns>컬럼객체</returns>
		//----------------------------------------------------------------------------------------------------
		public static DataColumn AddColmn( DataTable table, Type type, string name, bool auto_inc )
		{
			DataColumn colmn	= new DataColumn();
			colmn.DataType		= type;
			colmn.ColumnName	= name;
			colmn.ReadOnly		= false;
			colmn.AutoIncrement	= auto_inc;
			table.Columns.Add( colmn );
			return colmn;
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 레코드에 데이터를 읽고 쓰기.
		/// 주의:PrimaryKey를 지정해야 사용할 수 있다.
		/// </summary>
		/// <param name="colmn">컬럼명</param>
		/// <returns>데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public object this[string colmn]
		{
			get
			{
				return GetValue(colmn);
			}
			set
			{
				SetValue(colmn,value);
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 레코드에 데이터를 읽고 쓰기.
		/// 주의:PrimaryKey를 지정해야 사용할 수 있다.
		/// </summary>
		/// <param name="colmn_index">컬럼인덱스</param>
		/// <returns>데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public object this[int colmn_index]
		{
			get
			{
				return GetValue(colmn_index);
			}
			set
			{
				SetValue(colmn_index,value);
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터를 출력한다.
		/// 주의:PrimaryKey를 지정해야 사용할 수 있다.
		/// </summary>
		/// <param name="colmn">컬럼명</param>
		/// <returns>데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public object GetValue( string colmn )
		{
			lock( m_table )
			{
				DataRow row = m_table.Rows.Find(Key);
				if( row==null ) return null;
				return row[colmn];
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터를 출력한다.
		/// 주의:PrimaryKey를 지정해야 사용할 수 있다.
		/// </summary>
		/// <param name="colmn_index">컬럼인덱스</param>
		/// <returns>데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public object GetValue( int colmn_index )
		{
			lock( m_table )
			{
				DataRow row = m_table.Rows.Find(Key);
				if( row==null ) return null;
				return row[colmn_index];
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터를 입력한다.
		/// 주의:PrimaryKey를 지정해야 사용할 수 있다.
		/// </summary>
		/// <param name="colmn">컬럼명</param>
		/// <param name="value">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public void SetValue( string colmn, object value )
		{
			lock( m_table )
			{
				DataRow row = m_table.Rows.Find(Key);
				if( row==null )
				{
					m_table.Rows.Add( GetDefaultValue() );
				}
				row[colmn] = value;
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터를 입력한다.
		/// 주의:PrimaryKey를 지정해야 사용할 수 있다.
		/// </summary>
		/// <param name="colmn_index">컬럼인덱스</param>
		/// <param name="value">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public void SetValue( int colmn_index, object value )
		{
			lock( m_table )
			{
				DataRow row = m_table.Rows.Find(Key);
				if( row==null )
				{
					m_table.Rows.Add( GetDefaultValue() );
				}
				row[colmn_index] = value;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기본값 리턴
		/// </summary>
		/// <returns>기본값</returns>
		//----------------------------------------------------------------------------------------------------
		public object[] GetDefaultValue()
		{
			object[] r = new object[m_table.Rows.Count];
			for( int c=0; c<m_table.Columns.Count; ++c )
			{
				if( KeyName == m_table.Columns[c].ColumnName )
				{
					r[c] = Key;
				}
				else
				if( m_table.Columns[c].DataType == typeof(string) )
				{
					r[c] = "";
				}
				else
				{
					r[c] = 0;
				}
			}
			return r;
		}
	}
}

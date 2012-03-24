//----------------------------------------------------------------------------------------------------
// cDataTable
// :  데이터베이스 테이블 객체
//  -JHL-2012-03-02
//----------------------------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	///  데이터베이스 테이블 객체
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cUserAccount : cDataTables
	{
		//----------------------------------------------------------------------------------------------------
		#region 상수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 계정 아이디
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		const	ulong		MAX_ACCOUNT_ID		= ulong.MaxValue;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 빈 계정 아이디
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		const	ulong		NULL_ACCOUNT_ID		= 0;
		#endregion

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 테이블
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		private		static	DataTable		s_table;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터베이스 아답터
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		private		static DbDataAdapter	s_adapter;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 초기화
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public static void Initialize( cDatabase db )
		{
			if( s_table!=null ) return;

			s_table = new DataTable( "user_account" );

			DataColumn[] key = new DataColumn[1];

			int k=0;
			key[k++] =
			AddColmn( s_table, typeof(long),	"account_id",	true  );
			AddColmn( s_table, typeof(string),	"guid",			false );
			AddColmn( s_table, typeof(string),	"email",		false );
			AddColmn( s_table, typeof(string),	"password",		false );
			AddColmn( s_table, typeof(long),	"neowiz_id",	false );
			AddColmn( s_table, typeof(string),	"fb_id",		false );
			AddColmn( s_table, typeof(string),	"sig",			false );
			AddColmn( s_table, typeof(sbyte),	"access",		false );
			AddColmn( s_table, typeof(long),	"cash",			false );
			AddColmn( s_table, typeof(sbyte),	"max_char",		false );
			AddColmn( s_table, typeof(long),	"active_char",	false );
			AddColmn( s_table, typeof(long),	"access_time",	false );
			AddColmn( s_table, typeof(decimal),	"music",		false );
			AddColmn( s_table, typeof(decimal),	"sound",		false );
			AddColmn( s_table, typeof(sbyte),	"graphic",		false );
			AddColmn( s_table, typeof(short),	"server_id",	false );

			s_table.PrimaryKey = key;

			s_adapter = db.CreateAdapter( s_table.TableName );
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public cUserAccount():base(s_table,s_adapter,"account_id")
		{
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB에 데이터 갱신
		/// </summary>
		/// <param name="db">데이터베이스 객체</param>
		/// <returns>성공유무</returns>
		//----------------------------------------------------------------------------------------------------
		public static bool Update( cDatabase db )
		{
			return Update( db, s_table, s_adapter );
		}
	}
}

//----------------------------------------------------------------------------------------------------
// cLog
// : 로그 클래스
//  -JHL-2012-02-13
//----------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 로그 객체
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cLog
	{
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 로그 헤더 문자열.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static string	s_header	= "BlueEngine";
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파일로 덤프 플래그.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		static bool		s_file_dump	= false;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 로그 헤더 문자열.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		static string	Header		{get{return s_header;}set{s_header=value;}}

		//----------------------------------------------------------------------------------------------------
 		#region Log() : 로그
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 로그 출력.
		/// </summary>
		/// <param name="values">데이터리스트</param>
		//----------------------------------------------------------------------------------------------------
        public static void Log( params object[] values )
        {
			string log = LogToString( 2, values );
			System.Diagnostics.Debug.WriteLine( log );
			if( s_file_dump ) Dump( log );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 로그를 문자열로 얻음.
		/// </summary>
		/// <param name="stack_index">함수 추적을 위한 스택 깊이 단계.</param>
		/// <param name="values">데이터리스트</param>
		//----------------------------------------------------------------------------------------------------
        public static string LogToString( int stack_index, params object[] values )
        {
			//string	func = new System.Diagnostics.StackTrace(true).GetFrame(1).GetMethod().Name;
			//string	file = new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName();
			//int		line = new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileLineNumber();

			StackTrace		stackTrace	= new System.Diagnostics.StackTrace(true);
			StackFrame[]	stackFrames = stackTrace.GetFrames();

			// 호출한 함수 파라메타 값 알아내기(미완성)
			/*
			string str_param="(";
			Type method_type = stackFrames[stack_index].GetMethod().GetType();
			FieldInfo[] method_fields = method_type.GetFields( BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance );
			for( int e=0; e<method_fields.LongLength; ++e )
			{
				Type type = method_fields[e].GetType();
				if( type.BaseType != typeof(object) ) continue;
				object o = method_fields[e].GetValue(type);
				if( e>0 ) str_param += ",";
				str_param += method_fields[e].Name + "=" + o.ToString();
			}
			str_param += ")";
			*/
			string str_param="(";
			str_param += cObject.ValueToString(values);
			str_param += ")";

			string message = cObject.ValueToString( values );

			string	func = stackFrames[stack_index].GetMethod().ReflectedType.Name +"::"+ stackFrames[stack_index].GetMethod().Name+str_param;
			//string	func = new StackTrace( stackFrames[stack_index] ).ToString();
			string	file = stackFrames[stack_index].GetFileName();
			int		line = stackFrames[stack_index].GetFileLineNumber();
			return s_header + " : " + message + " \n " + func + " : " + file + " : " + line;// + "[ " + debug_string + " ]";
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Dump() : 파일에 메시지 덤프
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파일로 메시지 덤프. 파일명 : 헤더.log
		/// </summary>
		/// <param name="message">메시지</param>
		//----------------------------------------------------------------------------------------------------
		public static void Dump( string message )
		{
			System.IO.FileStream file = new System.IO.FileStream(s_header+".log", System.IO.FileMode.Append);
			byte[] info = new System.Text.UTF8Encoding(true).GetBytes(message);
			file.Write(info, 0, info.Length);
		}
		#endregion
	}
}


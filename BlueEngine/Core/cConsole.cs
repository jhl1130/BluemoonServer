//----------------------------------------------------------------------------------------------------
// cConsole
// : 콘솔 클래스
//  -JHL-2012-02-13
//----------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 콘솔 제어를 위한 객체
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cConsole
    {
		//----------------------------------------------------------------------------------------------------
 		#region 메시지 출력
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 콘솔창에 메시지 출력.
		/// </summary>
		/// <param name="message">메시지.</param>
		//----------------------------------------------------------------------------------------------------
        public void Write( string message )
        {
			Console.WriteLine( message ); 
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 콘솔창에 메시지 출력(칼라 지정 가능).
		/// </summary>
		/// <param name="message">메시지.</param>
		/// <param name="fcolor">글자색.</param>
		/// <param name="bcolor">배경색.</param>
        public void WriteColor( string message, ConsoleColor fcolor, ConsoleColor bcolor )
        {
			Console.ForegroundColor = fcolor;
			Console.BackgroundColor = bcolor;
			Console.WriteLine( message ); 
			Console.ResetColor();
		}
		#endregion
    }
}

//----------------------------------------------------------------------------------------------------
// cTime
// : 시간 클래스
//  -JHL-2012-02-25
//----------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 시간 제어를 위한 객체.
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cTime
	{
		//----------------------------------------------------------------------------------------------------
		#region 현재 로컬 시간
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 현재 로컬 시간을 얻는다.
		/// </summary>
		/// <returns>현재 로컬 시간.</returns>
		//----------------------------------------------------------------------------------------------------
		public static double Now()
		{
			TimeSpan time_span = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0);
			return time_span.TotalSeconds;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 현재 유닉스 시간
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 현재 유닉스 시간을 얻는다.(UTC+00:00)
		/// </summary>
		/// <returns>현재 유닉스 시간.</returns>
		//----------------------------------------------------------------------------------------------------
		public static long UnixNow()
		{
			TimeSpan time_span = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
			return (long)time_span.TotalSeconds;
		}
		#endregion
	}
}


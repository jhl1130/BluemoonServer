﻿//----------------------------------------------------------------------------------------------------
// cSkill
// : 스킬 객체
//  -JHL-2012-03-02
//----------------------------------------------------------------------------------------------------
using System;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 스킬 객체
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cSkill : cObject
	{
		//----------------------------------------------------------------------------------------------------
		#region 상수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 아이디
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public const ushort	MAX_SKILL_ID	= 9999;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 동시 타겟 개수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public const byte	MAX_TARGET		= 10;
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 변수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스킬 아이디
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected			ushort		m_skill_id;
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 속성
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스킬 아이디
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				ushort		SkillID			{get{return m_skill_id;}}
		#endregion
	}
}

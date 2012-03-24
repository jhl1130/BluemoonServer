﻿//----------------------------------------------------------------------------------------------------
// cBitSize
// : 비트길이
//  -JHL-2012-02-20
//----------------------------------------------------------------------------------------------------
using System;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 비트길이를 얻기 위한 객체.
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cBitSize
	{
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( sbyte n )
		{
			if( n==0 ) return 1;
			if( sbyte.MinValue == n ) return 8;
			n = Math.Abs(n);
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( byte n )
		{
			if( n==0 ) return 1;
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( short n )
		{
			if( n==0 ) return 1;
			if( short.MinValue == n ) return 16;
			n = Math.Abs(n);
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( ushort n )
		{
			if( n==0 ) return 1;
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( int n )
		{
			if( n==0 ) return 1;
			if( int.MinValue == n ) return 32;
			n = Math.Abs(n);
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( uint n )
		{
			if( n==0 ) return 1;
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( long n )
		{
			if( n==0 ) return 1;
			if( long.MinValue == n ) return 64;
			n = Math.Abs(n);
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기
		/// </summary>
		/// <param name="n">데이터</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( ulong n )
		{
			if( n==0 ) return 1;
			int count=0;
			while(n!=0)	{++count; n>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기.
		/// 주위 : 값이 너무 크거나 소숫점 자리수가 크면 값이 오버될 수 있다.
		/// </summary>
		/// <param name="n">데이터</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( float n, int point )
		{
			ulong i = (ulong)(Math.Abs(n)*Math.Pow(10,point));
			if( i==0 ) return 1;
			int count=0;
			while(i!=0)	{++count; i>>=1;}
			return count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트길이 얻기.
		/// 주위 : 값이 너무 크거나 소숫점 자리수가 크면 값이 오버될 수 있다.
		/// </summary>
		/// <param name="n">데이터</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns>길이</returns>
		//----------------------------------------------------------------------------------------------------
		public static int BitSize( double n, int point )
		{
			ulong i = (ulong)(Math.Abs(n)*Math.Pow(10,point));
			if( i==0 ) return 1;
			int count=0;
			while(i!=0)	{++count; i>>=1;}
			return count;
		}
	}

}

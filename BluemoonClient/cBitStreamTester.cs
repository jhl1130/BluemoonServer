//-------------------------------------------------------------------
// cBitStreamTester
// : 비트스트림 베이스
//  -JHL-2012-02-22
//-------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace BlueEngine
{
	//-------------------------------------------------------------------
	// cBitStreamTester
	//-------------------------------------------------------------------
    public class cBitStreamTester
    {
		static cConsole s_console = new cConsole();

        //-------------------------------------------------------------------
 		#region cBitStreamTester() : 생성자
        //-------------------------------------------------------------------
        public cBitStreamTester()
        {
			cBitStream bits;
	        //-------------------------------------------------------------------
	 		#region Sbyte
			bits = new cBitStream();
			s_console.WriteColor( "cBitStream : string Test Start", ConsoleColor.Green, ConsoleColor.Black );
			{
				cNetwork.WriteOrder( bits, cNetwork.eOrder.CHANNEL_CHAT );
				cNetwork.WriteResult( bits, cNetwork.eResult.SUCCESS );
				cNetwork.WriteString( bits,  "1234567890abcedfghijklmnopqustuvwxyz가나다라마바사아자차카타파하~!@#$%^&*()_+|{}:;<>?'" );
				byte[] data = bits.ToByteArray();
				
				bits = new cBitStream();
				bits.Write( data );
				bits.Position = 0;

				cNetwork.eOrder		order	= cNetwork.ReadOrder( bits );
				cNetwork.eResult	result	= cNetwork.ReadResult( bits );
				string				message	= cNetwork.ReadString( bits );

				s_console.Write( bits.ToString() );
				s_console.Write( order.ToString() );
				s_console.Write( result.ToString() );
				s_console.Write( message );
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : string Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region Sbyte
			s_console.WriteColor( "cBitStream : sbyte Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0;c<=byte.MaxValue;++c )
			{
				sbyte value = (sbyte)(sbyte.MinValue + c);
				sbyte max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				sbyte r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : sbyte Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

	        //-------------------------------------------------------------------
	 		#region byte
			s_console.WriteColor( "cBitStream : byte Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0;c<=byte.MaxValue;++c )
			{
				byte value = (byte)(byte.MinValue + c);
				byte max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				byte r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : byte Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region short
			s_console.WriteColor( "cBitStream : short Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0;c<ushort.MaxValue;++c )
			{
				short value = (short)(short.MinValue + c);
				short max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				short r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : short Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region ushort
			s_console.WriteColor( "cBitStream : ushort Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0;c<ushort.MaxValue;++c )
			{
				ushort value = (ushort)(ushort.MinValue + c);
				ushort max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				ushort r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : ushort Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region int
			s_console.WriteColor( "cBitStream : int Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0;c<100;++c )
			{
				int value = int.MinValue + (int)c;
				int max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				int r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			bits = new cBitStream();
			for( int c=0;c<100;++c )
			{
				int value = int.MaxValue - (int)c;
				int max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				int r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : int Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region uint
			s_console.WriteColor( "cBitStream : uint Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0;c<100;++c )
			{
				uint value = uint.MinValue + (uint)c;
				uint max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				uint r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			bits = new cBitStream();
			for( int c=0;c<100;++c )
			{
				uint value = uint.MaxValue - (uint)c;
				uint max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				uint r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : uint Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region long
			s_console.WriteColor( "cBitStream : long Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0; c<100; ++c )
			{
				long value = long.MinValue + (long)c;
				long max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				long r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			for( int c=0; c<100; ++c )
			{
				long value = long.MaxValue - (long)c;
				long max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				long r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : long Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;
			
			//-------------------------------------------------------------------
	 		#region ulong
			s_console.WriteColor( "cBitStream : ulong Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0; c<100; ++c )
			{
				ulong value = ulong.MinValue + (ulong)c;
				ulong max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				ulong r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			for( int c=0; c<100; ++c )
			{
				ulong value = ulong.MaxValue - (ulong)c;
				ulong max_value = value;
				bits.Write( value, max_value );
				bits.Position = 0;
				ulong r_value = 0;
				bits.Read( out r_value, max_value );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : ulong Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region float
			s_console.WriteColor( "cBitStream : float Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0; c<100; ++c )
			{
				float value = -c*123467.567f;
				float max_value = value;
				bits.Write( value, max_value, 2 );
				bits.Position = 0;
				float r_value = 0;
				bits.Read( out r_value, max_value, 2 );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			for( int c=0; c<100; ++c )
			{
				float value = c*123467.567f;
				float max_value = value;
				bits.Write( value, max_value, 2 );
				bits.Position = 0;
				float r_value = 0;
				bits.Read( out r_value, max_value, 2 );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : float Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;

			//-------------------------------------------------------------------
	 		#region double
			s_console.WriteColor( "cBitStream : double Test Start", ConsoleColor.Green, ConsoleColor.Black );
			bits = new cBitStream();
			for( int c=0; c<10; ++c )
			{
				double value = -c*123467.567f;
				double max_value = value;
				bits.Write( value, max_value, 4 );
				bits.Position = 0;
				double r_value = 0;
				bits.Read( out r_value, max_value, 4 );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			for( int c=0; c<10; ++c )
			{
				double value = c*123467.567f;
				double max_value = value;
				bits.Write( value, max_value, 4 );
				bits.Position = 0;
				double r_value = 0;
				bits.Read( out r_value, max_value, 4 );
				if( value != r_value )
				{
					s_console.Write( "error : value="+value+" > " + r_value );
				}
				bits.Position = 0;
			}
			s_console.WriteColor( "cBitStream : double Test End", ConsoleColor.Green, ConsoleColor.Black );
			#endregion;
		}
		#endregion
    }
}

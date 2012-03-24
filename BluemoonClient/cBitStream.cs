//-------------------------------------------------------------------
// cBitStream
// : 비트스트림
//  -JHL-2012-02-20
//-------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Resources;
using System.Globalization;
using System.ComponentModel;
using System.Reflection;
using System.Net.Sockets;
using System.Security.Cryptography;

using BlueEngine;

namespace nGameNetwork
{
	public class cBitStream : Stream
	{
		//-------------------------------------------------------------------
		// 타입
		public enum eType
		{
			SByte,
			SBytes,
			Byte,
			Bytes,
			Int16,
			Int16s,
			UInt16,
			UInt16s,
			Int32,
			Int32s,
			UInt32,
			UInt32s,
			Int64,
			Int64s,
			UInt64,
			UInt64s,
			Single,
			Singles,
			Double,
			Doubles,
			String,
			Strings,
			Count
		}

		//-------------------------------------------------------------------
		#region 멤버 상수들
		//-------------------------------------------------------------------
		protected const int		c_size_byte					= 8;
		protected const int		c_size_char					= 16;
		protected const int		c_size_uint16				= 16;
		protected const int		c_size_uint32				= 32;
		protected const int		c_size_single				= 32;
		protected const int		c_size_uint64				= 64;
		protected const int		c_size_double				= 64;
		protected const uint	c_bit_buffer_unit_size		= c_size_uint32;	// 저장 단위 크기
		protected const int		c_bit_buffer_unit_size_shift= 5;				// 저장 단위 크기의 쉬프트수
		protected const uint	c_bit_buffer_unit_size_mod	= 31;				// 저장 단위 크기의 비트필터(111111)

		protected static uint [] c_bit_mask	= new uint []
		{
			0x00000000, 
			0x00000001, 0x00000003, 0x00000007, 0x0000000F,
			0x0000001F, 0x0000003F, 0x0000007F, 0x000000FF,
			0x000001FF, 0x000003FF, 0x000007FF, 0x00000FFF,
			0x00001FFF, 0x00003FFF, 0x00007FFF, 0x0000FFFF,
			0x0001FFFF, 0x0003FFFF, 0x0007FFFF, 0x000FFFFF,
			0x001FFFFF, 0x003FFFFF, 0x007FFFFF, 0x00FFFFFF,
			0x01FFFFFF, 0x03FFFFFF, 0x07FFFFFF, 0x0FFFFFFF,
			0x1FFFFFFF, 0x3FFFFFFF, 0x7FFFFFFF, 0xFFFFFFFF,
		};
		
		#endregion

		//-------------------------------------------------------------------
		#region 멤버 변수들
		//-------------------------------------------------------------------
		protected bool		m_open = true;
		protected uint[]	m_buffer;
		protected uint		m_buffer_length;
		protected uint		m_buffer_index;
		protected uint		m_bit_index;

		protected static IFormatProvider s_format_provider = (IFormatProvider)CultureInfo.InvariantCulture;
		#endregion

		//-------------------------------------------------------------------
		#region 속성 변수들
		//-------------------------------------------------------------------
		// 버퍼 길이
		public override long Length		{get{return (long)m_buffer_length;}}
		public virtual long Length8		{get{return (long)(m_buffer_length >> 3) + (long)((m_buffer_length & 7) > 0 ? 1 : 0);}}
		public virtual long Length16	{get{return (long)(m_buffer_length >> 4) + (long)((m_buffer_length & 15) > 0 ? 1 : 0);}}
		public virtual long Length32	{get{return (long)(m_buffer_length >> 5) + (long)((m_buffer_length & 31) > 0 ? 1 : 0);}}
		public virtual long Length64	{get{return (long)(m_buffer_length >> 6) + (long)((m_buffer_length & 63) > 0 ? 1 : 0);}}
		//-------------------------------------------------------------------
		// 버퍼 용량(버퍼길이*저장단위)
		public virtual long Capacity	{get{return ((long)m_buffer.Length) << c_bit_buffer_unit_size_shift;}}
		//-------------------------------------------------------------------
		// 읽기 가능 유무
		public override bool CanRead	{get{return m_open;}}
		//-------------------------------------------------------------------
		// 검색 가능 유무
		public override bool CanSeek	{get{return false;}}
		//-------------------------------------------------------------------
		// 쓰기 가능 유무
		public override bool CanWrite	{get{return m_open;}}
		//-------------------------------------------------------------------
		// 버퍼길이 변경 가능 유무
		public static bool CanSetLength	{get{return false;}}
		//-------------------------------------------------------------------
		// 플러시 가능 유무
		public static bool CanFlush		{get{return false;}}

		//-------------------------------------------------------------------
		#region 현재 버퍼의 비트 인덱스
		public override long Position
		{
			get
			{
				uint pos = (m_buffer_index << c_bit_buffer_unit_size_shift) + m_bit_index;
				return (long)pos;
			}
			set
			{
				uint pos = (uint)value;

				// 포지션값이 범위를 벋어나는지 검사
				if(pos<0)	throw new ArgumentOutOfRangeException("value", "ArgumentOutOfRange_NegativePosition");
				if( m_buffer_length < pos+1 )
							throw new ArgumentOutOfRangeException("value", "ArgumentOutOfRange_InvalidPosition");

				// 버퍼 인덱스 ( 상위 비트 )
				m_buffer_index = pos >> c_bit_buffer_unit_size_shift;
				// 비트 인덱스 ( 하위 비트 )
				if( (pos&c_bit_buffer_unit_size_mod) > 0 ) //현재포지션에서 32비트안에 값이 있으면
				{
					m_bit_index = (pos&c_bit_buffer_unit_size_mod);
				}
				else
				{
					m_bit_index = 0;
				}
			}
		}
		#endregion

		#endregion

		//-------------------------------------------------------------------
		#region 생성자
		//-------------------------------------------------------------------
		public cBitStream()
		{
			m_buffer = new uint[1];
		}
		public cBitStream( long capacity )
		{
			if(capacity <= 0) throw new ArgumentOutOfRangeException("ArgumentOutOfRange_NegativeOrZeroCapacity");
			m_buffer = new uint[(capacity >> c_bit_buffer_unit_size_shift) + ((capacity & c_bit_buffer_unit_size_mod) > 0 ? 1 : 0)];
		}
		public cBitStream( Stream stream ) : this()
		{
			if(stream==null) throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			
			// 바이트 스트림으로 부터 바이트배열로 읽어온다.
			byte[] bits = new byte[stream.Length];
			long stream_pos = stream.Position;
			stream.Position = 0;
			stream.Read( bits, 0, (int)stream.Length );
			stream.Position = stream_pos;

			// 비트 스트림에 바이트 배열을 기록한다.
			Write( bits, 0, (int)stream.Length );
		}

		#endregion

		//-------------------------------------------------------------------
		#region Write() : 비트 기록
		//-------------------------------------------------------------------
		protected void Write( ref uint bits, ref uint index, ref uint count )
		{
			// 현재 포지션을 계산한다.(버퍼인덱스 + 비트인덱스)
			uint position = (m_buffer_index << c_bit_buffer_unit_size_shift) + m_bit_index;
			// 마지막 요소 인덱스를 얻는다.
			uint last_index = (m_buffer_length >> c_bit_buffer_unit_size_shift);
			// 마지막 인덱스를 계산한다.
			uint end_index = index + count;

			int bit_shift = (int)index;

			// 비트가 들어갈 공간을 비운다.
			bits &= c_bit_mask[count]<<bit_shift;

			// 빈공간에 비트를 채운다.
			uint free_bits = c_bit_buffer_unit_size - m_bit_index;
			bit_shift = (int)(free_bits-end_index);

			// 인덱스를 구한다.
			uint indexed = 0;
			if(bit_shift<0)
			{
				indexed = bits >> Math.Abs(bit_shift);
			}
			else
			{
				indexed = bits << bit_shift;
			}

			if( m_buffer_length >= (position+1) )
			{
				int buffer_bit_shift = (int)(free_bits - count);
				uint buffer_bit_mask = 0;
				if(buffer_bit_shift < 0)
					buffer_bit_mask = uint.MaxValue ^ (c_bit_mask[count] >> Math.Abs(buffer_bit_shift));
				else
					buffer_bit_mask = uint.MaxValue ^ (c_bit_mask[count] << buffer_bit_shift);
				m_buffer[m_buffer_index] &= buffer_bit_mask;

				if(last_index == m_buffer_index)
				{
					uint buffer_new_len = 0;
					if(free_bits >= count)
						buffer_new_len = position + count;
					else
						buffer_new_len = position + free_bits;
					if(buffer_new_len > m_buffer_length)
					{
						uint buffer_extra_bits = buffer_new_len - m_buffer_length;
						UpdateLengthForWrite(buffer_extra_bits);
					}
				}
			}
			else
			{
				if( free_bits >= count )
					UpdateLengthForWrite(count);
				else
					UpdateLengthForWrite(free_bits);
			}

			// 값을 기록
			m_buffer[m_buffer_index] |= indexed;

			// 기록후 남은 비트들 처리
			if( free_bits >= count )
			{
				UpdateIndicesForWrite(count);
			}
			else
			{
				UpdateIndicesForWrite(free_bits);
			
				uint uiValue_RemainingBits = count - free_bits;
				uint uiValue_StartIndex = index;
				Write(ref bits, ref uiValue_StartIndex, ref uiValue_RemainingBits);
			}
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : 1비트 기록
		//-------------------------------------------------------------------
		public virtual void Write( bool bit )
		{
			uint data = (uint)(bit?1:0);
			uint index = 0;
			uint count = 1;
			Write( ref data, ref index, ref count );
		}
		public virtual void Write( bool[] bits )
		{
			if(bits==null) throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( bool[] bits, int offset, int count )
		{
			if(bits == null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset<0)		throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)			throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(bits.Length-offset))
								throw new ArgumentException("Argument_InvalidCountOrOffset");

			int end = offset+count;
			for( int c=offset; c<end; ++c )
			{
				Write( bits[c] );
			}
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : 8비트 기록
		//-------------------------------------------------------------------
		public virtual void Write( byte bits)
		{
			Write(bits, 0, c_size_byte);
		}
		public virtual void Write( byte data, int index, int count )
		{
			if(index<0)		throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)		throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(c_size_byte-index))
							throw new ArgumentException("Argument_InvalidCountOrBitIndex_Byte");

			uint bit_data	= (uint)data;
			uint bit_index	= (uint)index;
			uint bit_count	= (uint)count;

			Write( ref bit_data, ref bit_index, ref bit_count );
		}
		public virtual void Write( byte bits, byte max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual void Write( string bits )
		{
			byte[] data = new UTF8Encoding(true).GetBytes( ((String)bits).ToCharArray() );
			Write( (ushort)data.Length );
			Write( data );
		}
		public virtual void Write( byte[] data )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			Write( data, 0, data.Length );
		}
		public override void Write( byte[] data, int offset, int count )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset<0)	throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)		throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(data.Length-offset))
							throw new ArgumentException("Argument_InvalidCountOrOffset");

			int end = offset + count;
			for( int c=offset; c<end; ++c )
			{
				Write( data[c] );
			}
		}
		public virtual void Write( byte[] data, byte max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( byte bits in data )
			{
				Write( bits, max_value );
			}
		}
		public virtual void Write( string[] data )
		{
			foreach( string s in data )
			{
				Write( s );
			}
		}
		public virtual void Write( sbyte data )
		{
			Write( data, 0, c_size_byte );
		}
		public virtual void Write( sbyte bits, int index, int count )
		{
			byte byte_bits = (byte)bits;
			Write( byte_bits, index, count );
		}
		public virtual void Write( sbyte bits, sbyte max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_byte )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
		public virtual void Write( sbyte[] bits )
		{
			if(bits==null) throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			Write( bits, 0, bits.Length );
		}
		public virtual void Write( sbyte[] bits, int offset, int count )
		{
			if(bits==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset<0)	throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)		throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(bits.Length-offset))
							throw new ArgumentException("Argument_InvalidCountOrOffset");

			byte [] abytBits = new byte [count];
			Buffer.BlockCopy( bits, offset, abytBits, 0, count );

			Write(abytBits, 0, count);
		}
		public virtual void Write( sbyte[] data, sbyte max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( sbyte bits in data )
			{
				Write( bits, max_value );
			}
		}
		public override void WriteByte( byte value )
		{
			Write(value);
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : 16비트 기록
		//-------------------------------------------------------------------
		public virtual void Write( char bits )
		{
			Write( bits, 0, c_size_char );
		}
		public virtual void Write( char bits, int index, int count )
		{
			if(index<0)	throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)	throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(c_size_char-index))
						throw new ArgumentException("Argument_InvalidCountOrBitIndex_Char");

			uint bit_data	= (uint)bits;
			uint bit_index	= (uint)index;
			uint bit_count	= (uint)count;

			Write( ref bit_data, ref bit_index, ref bit_count );
		}
		public virtual void Write( char[] bits )
		{
			if(bits == null) throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( char[] bits, int offset, int count )
		{
			if(bits==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset<0)	throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)		throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(bits.Length-offset))
							throw new ArgumentException("Argument_InvalidCountOrOffset");

			int end = offset + count;
			for( int c=offset; c < end; ++c )
			{
				Write( bits[c] );
			}
		}
		public virtual void Write( ushort bits )
		{
			Write( bits, 0, c_size_uint16 );
		}
		public virtual void Write( ushort bits, int bitIndex, int count )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_uint16 - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_UInt16");

			uint uiBits = (uint)bits;
			uint uiBitIndex = (uint)bitIndex;
			uint uiCount = (uint)count;

			Write(ref uiBits, ref uiBitIndex, ref uiCount);
		}
		public virtual void Write( ushort bits, ushort max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual void Write( ushort[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( ushort[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			for(int iUInt16Counter = offset; iUInt16Counter < iEndIndex; iUInt16Counter++)
				Write(bits[iUInt16Counter]);
		}
		public virtual void Write( ushort[] data, ushort max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( ushort bits in data )
			{
				Write( bits, max_value );
			}
		}
		public virtual void Write( short bits)
		{
			Write(bits, 0, c_size_uint16);
		}
		public virtual void Write( short bits, int bitIndex, int count)
		{
			// Convert the value to an UInt16
			ushort usBits = (ushort)bits;
			
			Write(usBits, bitIndex, count);
		}
		public virtual void Write( short bits, short max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_uint16 )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
		public virtual void Write( short[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			Write(bits, 0, bits.Length);
		}
		public virtual void Write( short[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			ushort [] ausBits = new ushort [count];
			Buffer.BlockCopy(bits, offset << 1, ausBits, 0, count << 1);
		
			Write(ausBits, 0, count);
		}
		public virtual void Write( short[] data, short max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( short bits in data )
			{
				Write( bits, max_value );
			}
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : 32비트 기록
		//-------------------------------------------------------------------
		public virtual void Write( uint bits)
		{
			Write(bits, 0, c_size_uint32);
		}
		public virtual void Write( uint bits, int bitIndex, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_uint32 - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_UInt32");

			uint uiBitIndex = (uint)bitIndex;
			uint uiCount = (uint)count;

			Write(ref bits, ref uiBitIndex, ref uiCount);
		}
		public virtual void Write( uint bits, uint max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual void Write( uint[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( uint[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			for(int iUInt32Counter = offset; iUInt32Counter < iEndIndex; iUInt32Counter++)
				Write(bits[iUInt32Counter]);
		}
		public virtual void Write( uint[] data, uint max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( uint bits in data )
			{
				Write( bits, max_value );
			}
		}
		public virtual void Write( int bits)
		{
			Write(bits, 0, c_size_uint32);
		}
		public virtual void Write( int bits, int bitIndex, int count)
		{
			// Convert the value to an UInt32
			uint uiBits = (uint)bits;
			
			Write(uiBits, bitIndex, count);
		}
		public virtual void Write( int bits, int max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_uint32 )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
		public virtual void Write( int[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			Write(bits, 0, bits.Length);
		}
		public virtual void Write( int[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			uint [] auiBits = new uint [count];
			Buffer.BlockCopy(bits, offset << 2, auiBits, 0, count << 2);
		
			Write(auiBits, 0, count);
		}
		public virtual void Write( int[] data, int max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( int bits in data )
			{
				Write( bits, max_value );
			}
		}
		public virtual void Write( float bits)
		{
			Write(bits, 0, c_size_single);
		}
		public virtual void Write( float bits, int bitIndex, int count)
		{
			byte [] abytBits = BitConverter.GetBytes(bits);
			uint uiBits = (uint)abytBits[0] | ((uint)abytBits[1]) << 8 | ((uint)abytBits[2]) << 16 | ((uint)abytBits[3]) << 24;
			Write(uiBits, bitIndex, count);
		}
		public virtual void Write( float bits, float max_value, int point )
		{
			Int32 data = (Int32)(bits*Math.Pow(10,point));
			Int32 max_data = (Int32)(max_value*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_single )
			{
				Write( bits );
			}
			else
			{
				Write( data<0 );
				Write( Math.Abs(data), 0, count );
			}
		}
		public virtual void Write( float[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( float[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			for(int iSingleCounter = offset; iSingleCounter < iEndIndex; iSingleCounter++)
				Write(bits[iSingleCounter]);
		}
		public virtual void Write( float[] data, float max_value, int point )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( float bits in data )
			{
				Write( bits, max_value, point );
			}
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : 64비트 기록
		//-------------------------------------------------------------------
		public virtual void Write( ulong bits)
		{
			Write(bits, 0, c_size_uint64);
		}
		public virtual void Write( ulong bits, int bitIndex, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_uint64 - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_UInt64");

			int iBitIndex1 = (bitIndex >> 5) < 1 ? bitIndex : -1;
			int iBitIndex2 = (bitIndex + count) > 32 ? (iBitIndex1 < 0 ? bitIndex - 32 : 0) : -1;
			int iCount1 = iBitIndex1 > -1 ? (iBitIndex1 + count > 32 ? 32 - iBitIndex1 : count) : 0;
			int iCount2 = iBitIndex2 > -1 ? (iCount1 == 0 ? count : count - iCount1) : 0;

			if(iCount1 > 0)
			{
				uint uiBits1 = (uint)bits;
				uint uiBitIndex1 = (uint)iBitIndex1;
				uint uiCount1 = (uint)iCount1;
				Write(ref uiBits1, ref uiBitIndex1, ref uiCount1);
			}
			if(iCount2 > 0)
			{
				uint uiBits2 = (uint)(bits >> 32);
				uint uiBitIndex2 = (uint)iBitIndex2;
				uint uiCount2 = (uint)iCount2;
				Write(ref uiBits2, ref uiBitIndex2, ref uiCount2);
			}
		}
		public virtual void Write( ulong bits, ulong max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual void Write( ulong[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( ulong[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			for(int iUInt64Counter = offset; iUInt64Counter < iEndIndex; iUInt64Counter++)
				Write(bits[iUInt64Counter]);
		}
		public virtual void Write( ulong[] data, ulong max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( ulong bits in data )
			{
				Write( bits, max_value );
			}
		}
		public virtual void Write( long bits)
		{
			Write(bits, 0, c_size_uint64);
		}
		public virtual void Write( long bits, int bitIndex, int count)
		{
			// Convert the value to an UInt64
			ulong ulBits = (ulong)bits;
			
			Write(ulBits, bitIndex, count);
		}
		public virtual void Write( long bits, long max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_uint64 )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
		public virtual void Write( long[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			Write(bits, 0, bits.Length);
		}
		public virtual void Write( long[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			ulong [] aulBits = new ulong [count];
			Buffer.BlockCopy(bits, offset << 4, aulBits, 0, count << 4);
		
			Write(aulBits, 0, count);
		}
		public virtual void Write( long[] data, long max_value )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( long bits in data )
			{
				Write( bits, max_value );
			}
		}
		public virtual void Write( double bits)
		{
			Write(bits, 0, c_size_double);
		}
		public virtual void Write( double bits, int bitIndex, int count)
		{
			byte [] abytBits = BitConverter.GetBytes(bits);
			ulong ulBits = (ulong)abytBits[0] | ((ulong)abytBits[1]) << 8 | ((ulong)abytBits[2]) << 16 | ((ulong)abytBits[3]) << 24 |
				((ulong)abytBits[4]) << 32 | ((ulong)abytBits[5]) << 40 | ((ulong)abytBits[6]) << 48 | ((ulong)abytBits[7]) << 56;

			Write(ulBits, bitIndex, count);
		}
		public virtual void Write( double bits, double max_value, int point )
		{
			Int64 data = (Int64)(bits*Math.Pow(10,point));
			Int64 max_data = (Int64)(max_value*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_double )
			{
				Write( bits );
			}
			else
			{
				Write( data<0 );
				Write( Math.Abs(data), 0, count );
			}
		}
		public virtual void Write( double[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			Write(bits, 0, bits.Length);
		}
		public virtual void Write( double[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			for(int iDoubleCounter = offset; iDoubleCounter < iEndIndex; iDoubleCounter++)
				Write(bits[iDoubleCounter]);
		}
		public virtual void Write( double[] data, double max_value, int point )
		{
			if(data==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			foreach( double bits in data )
			{
				Write( bits, max_value, point );
			}
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : Object 기록
		//-------------------------------------------------------------------
		public virtual void Write( eType type, object bits, object max_value )
		{
			Write( type, bits, max_value, 2 );
		}
		public virtual void Write( eType type, object bits, object max_value, int point )
		{
			switch( type )
			{
			case eType.SByte:	Write( (sbyte)bits, (sbyte)max_value );				break;
			case eType.SBytes:	Write( (sbyte[])bits, (sbyte)max_value );			break;
			case eType.Byte:	Write( (byte)bits, (byte)max_value );				break;
			case eType.Bytes:	Write( (byte[])bits, (byte)max_value );				break;
			case eType.Int16:	Write( (short)bits, (short)max_value );				break;
			case eType.Int16s:	Write( (short[])bits, (short)max_value );			break;
			case eType.UInt16:	Write( (ushort)bits, (ushort)max_value );			break;
			case eType.UInt16s:	Write( (ushort[])bits, (ushort)max_value );			break;
			case eType.Int32:	Write( (int)bits, (int)max_value );					break;
			case eType.Int32s:	Write( (int[])bits, (int)max_value );				break;
			case eType.UInt32:	Write( (uint)bits, (uint)max_value );				break;
			case eType.UInt32s:	Write( (uint[])bits, (uint)max_value );				break;
			case eType.Int64:	Write( (long)bits, (long)max_value );				break;
			case eType.Int64s:	Write( (long[])bits, (long)max_value );				break;
			case eType.UInt64:	Write( (ulong)bits, (ulong)max_value );				break;
			case eType.UInt64s:	Write( (ulong[])bits, (ulong)max_value );			break;
			case eType.Single:	Write( (float)bits, (float)max_value, point );		break;
			case eType.Singles:	Write( (float[])bits, (float)max_value, point );	break;
			case eType.Double:	Write( (double)bits, (double)max_value, point );	break;
			case eType.Doubles:	Write( (double[])bits, (double)max_value, point );	break;
			case eType.String:	Write( (string)bits );								break;
			case eType.Strings:	Write( (string[])bits );							break;
			}
		}
		#endregion

		//-------------------------------------------------------------------
		#region WriteTo() : 다른 바이트 스트림에 기록
		//-------------------------------------------------------------------
		public virtual void WriteTo( Stream bits )
		{
			if(bits==null) throw new ArgumentNullException("bits", "ArgumentNull_Stream");
		
			byte[] write_bits = ToByteArray();
			bits.Write( write_bits, 0, write_bits.Length );
		}
		#endregion

		//-------------------------------------------------------------------
		#region Read() : 비트 읽기
		//-------------------------------------------------------------------
		protected uint Read( ref uint bits, ref uint index, ref uint count )
		{
			// 비트 포지션 계산
			uint bit_pos = (m_buffer_index << c_bit_buffer_unit_size_shift) + m_bit_index;

			// 실제 읽을 비트수 계산
			uint read_count = count;
			if( m_buffer_length < (bit_pos + read_count) )
			{
				read_count = m_buffer_length - bit_pos;
			}

			// 버퍼에서 비트를 읽을 부분만 얻어온다.
			uint read_value = m_buffer[m_buffer_index];
			int value_shift = (int)(c_bit_buffer_unit_size - (m_bit_index + read_count));

			// 2개의 단위버퍼에서 읽을 경우
			if( value_shift<0 )
			{
				value_shift = Math.Abs(value_shift);
				read_value &= c_bit_mask[read_count] >> value_shift;
				read_value <<= value_shift;
				
				// 다음 단위버퍼에서 비트들을 읽어서 결함한다.
				uint remain_count = (uint)value_shift;
				uint bit_index = 0;
				uint append_value = 0;
				UpdateIndicesForRead( read_count - remain_count );
				Read( ref append_value, ref bit_index, ref remain_count );
				read_value |= append_value;
			}
			else
			// 1개의 단위버퍼에서 읽을 경우
			{
				read_value &= c_bit_mask[read_count] << value_shift;
				read_value >>= value_shift;
				UpdateIndicesForRead( read_count );
			}

			bits = read_value << (int)index;

			return read_count;
		}

		#endregion

		//-------------------------------------------------------------------
		#region Read() : 1비트 읽기
		//-------------------------------------------------------------------
		public virtual int Read( out bool bit)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");

			uint uiBitIndex = 0;
			uint uiCount = 1;
			uint uiBit = 0;
			uint uiBitsRead = Read(ref uiBit, ref uiBitIndex, ref uiCount);
			
			bit = Convert.ToBoolean(uiBit);

			return (int)uiBitsRead;
		}
		public virtual int Read( bool[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( bool[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iBitCounter = offset; iBitCounter < iEndIndex; iBitCounter++)
				iBitsRead += Read(out bits[iBitCounter]);
		
			return iBitsRead;
		}
		
		#endregion

		//-------------------------------------------------------------------
		#region Read() : 8비트 읽기
		//-------------------------------------------------------------------
		public virtual int Read( out byte bits )
		{
			return Read(out bits, 0, c_size_byte);
		}
		public virtual int Read( out byte bits, int bitIndex, int count )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_byte - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_Byte");

			uint uiBitIndex = (uint)bitIndex;
			uint uiCount = (uint)count;
			uint uiBits = 0;
			uint uiBitsRead = Read(ref uiBits, ref uiBitIndex, ref uiCount);
		
			bits = (byte)uiBits;

			return (int)uiBitsRead;
		}
		public virtual int Read( out byte bits, byte max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual int Read( out string bits )
		{
			ushort len;
			int read_count = Read( out len );
			byte[] data = new byte[ len ];
			read_count += Read( data );
			bits = new UTF8Encoding(true).GetString( data );
			return read_count;
		}
		public virtual int Read( byte[] bits )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			return Read(bits, 0, bits.Length);
		}
		public override int Read( byte[] bits, int offset, int count )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int end = offset + count;
			int read_bit_count = 0;
			for( int byte_count = offset; byte_count < end; ++byte_count )
			{
				read_bit_count += Read( out bits[byte_count] );
			}
		
			return read_bit_count;
		}
		public virtual int Read( byte[] bits, byte max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public virtual int Read( string[] bits )
		{
			int read_count = 0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count = Read( out bits[c] );
			}
			return read_count;
		}
		public virtual int Read( out sbyte bits )
		{
			return Read(out bits, 0, c_size_byte);
		}
		public virtual int Read( out sbyte bits, int bitIndex, int count )
		{
			byte bytBits = 0;
			int iBitsRead = Read(out bytBits, bitIndex, count);
			bits = (sbyte)bytBits;
			return iBitsRead;
		}
		public virtual int Read( out sbyte bits, sbyte max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_byte )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				read_count += Read( out bits, 0, count );
				if( sign ) bits = (SByte)(-bits);
				return read_count;
			}
		}
		public virtual int Read( sbyte[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( sbyte[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iSByteCounter = offset; iSByteCounter < iEndIndex; iSByteCounter++)
				iBitsRead += Read(out bits[iSByteCounter]);
		
			return iBitsRead;
		}
		public virtual int Read( sbyte[] bits, sbyte max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public override int ReadByte()
		{
			byte bytBits;
			int iBitsRead = Read(out bytBits);
			
			if(iBitsRead == 0)
				return -1;
			else
				return (int)bytBits;
		}
		public virtual byte[] ToByteArray()
		{
			// 뒤에 남은 비트를 바이트 단위로 채워넣는다.
			int last_bits	= (int)(Length % c_size_byte);
			int add_bits	= c_size_byte - last_bits;
			byte zero_bits = 0;
			Write( zero_bits, 0, add_bits );

			// 데이터를 얻는다.
			long current_pos = Position;
			Position = 0;

			byte[] bits = new byte[Length8];
			Read( bits, 0, (int)Length8 );
			
			if( Position != current_pos )
				Position = current_pos;

			return bits;
		}
		#endregion

		//-------------------------------------------------------------------
		#region Read() : 16비트 읽기
		//-------------------------------------------------------------------
		public virtual int Read( out char bits)
		{
			return Read(out bits, 0, c_size_char);
		}
		public virtual int Read( out char bits, int bitIndex, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_char - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_Char");

			uint uiBitIndex = (uint)bitIndex;
			uint uiCount = (uint)count;
			uint uiBits = 0;
			uint uiBitsRead = Read(ref uiBits, ref uiBitIndex, ref uiCount);
		
			bits = (char)uiBits;

			return (int)uiBitsRead;
		}
		public virtual int Read( char[] bits )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( char[] bits, int offset, int count )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iCharCounter = offset; iCharCounter < iEndIndex; iCharCounter++)
				iBitsRead += Read(out bits[iCharCounter]);
		
			return iBitsRead;
		}
		public virtual int Read( out ushort bits)
		{
			return Read(out bits, 0, c_size_uint16);
		}
		public virtual int Read( out ushort bits, int bitIndex, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_uint16 - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_UInt16");

			uint uiBitIndex = (uint)bitIndex;
			uint uiCount = (uint)count;
			uint uiBits = 0;
			uint uiBitsRead = Read(ref uiBits, ref uiBitIndex, ref uiCount);
		
			bits = (ushort)uiBits;

			return (int)uiBitsRead;
		}
		public virtual int Read( out ushort bits, UInt16 max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual int Read( ushort[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( ushort[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iUInt16Counter = offset; iUInt16Counter < iEndIndex; iUInt16Counter++)
				iBitsRead += Read(out bits[iUInt16Counter]);
		
			return iBitsRead;
		}
		public virtual int Read( ushort[] bits, ushort max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public virtual int Read( out short bits)
		{
			return Read(out bits, 0, c_size_uint16);
		}
		public virtual int Read( out short bits, int bitIndex, int count)
		{
			ushort usBits = 0;
			int iBitsRead = Read(out usBits, bitIndex, count);

			bits = (short)usBits;

			return iBitsRead;
		}
		public virtual int Read( out short bits, short max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_uint16 )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				read_count += Read( out bits, 0, count );
				if( sign ) bits = (Int16)(-bits);
				return read_count;
			}
		}
		public virtual int Read( short[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( short[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iShortCounter = offset; iShortCounter < iEndIndex; iShortCounter++)
				iBitsRead += Read(out bits[iShortCounter]);
			
			return iBitsRead;
		}
		public virtual int Read( short[] bits, short max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		#endregion

		//-------------------------------------------------------------------
		#region Read() : 32비트 읽기
		//-------------------------------------------------------------------
		public virtual int Read( out uint bits )
		{
			return Read( out bits, 0, c_size_uint32 );
		}
		public virtual int Read( out uint bits, int index, int count )
		{
			if(index<0)	throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_NegativeParameter");
			if(count<0)	throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count>(c_size_uint32-index))
						throw new ArgumentException("Argument_InvalidCountOrBitIndex_UInt32");

			uint read_index = (uint)index;
			uint read_count = (uint)count;
			uint read_bits = 0;
			uint readed_count = Read( ref read_bits, ref read_index, ref read_count );
		
			bits = read_bits;
			return (int)readed_count;
		}
		public virtual int Read( out uint bits, uint max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual int Read( uint[] bits )
		{
			if(bits==null)	throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			return Read( bits, 0, bits.Length );
		}
		public virtual int Read( uint[] bits, int offset, int count )
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int end = offset + count;
			int iBitsRead = 0;
			for( int c = offset; c < end; ++c )
			{
				iBitsRead += Read( out bits[c] );
			}
		
			return iBitsRead;
		}
		public virtual int Read( uint[] bits, uint max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public virtual int Read( out int bits)
		{
			return Read(out bits, 0, c_size_uint32);
		}
		public virtual int Read( out int bits, int bitIndex, int count)
		{
			uint uiBits = 0;
			int iBitsRead = Read(out uiBits, bitIndex, count);

			bits = (int)uiBits;

			return iBitsRead;
		}
		public virtual int Read( out int bits, int max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_uint32 )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				read_count += Read( out bits, 0, count );
				if( sign ) bits = (Int32)(-bits);
				return read_count;
			}
		}
		public virtual int Read( int[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( int[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iInt32Counter = offset; iInt32Counter < iEndIndex; iInt32Counter++)
				iBitsRead += Read(out bits[iInt32Counter]);
		
			return iBitsRead;
		}
		public virtual int Read( int[] bits, int max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public virtual int Read( out float bits)
		{
			return Read(out bits, 0, c_size_single);
		}
		public virtual int Read( out float bits, int bitIndex, int count)
		{
			int uiBits = 0;
			int uiBitsRead = Read(out uiBits, bitIndex, count);

			bits = BitConverter.ToSingle(BitConverter.GetBytes(uiBits), 0);

			return (int)uiBitsRead;
		}
		public virtual int Read( out float bits, float max_value, int point )
		{
			Int32 max_data = (Int32)(max_value*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_single )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				Int32 data = 0;

				read_count += Read( out data, 0, count );
				if( sign ) data = (Int32)(-data);
				if( data == 0 )
				{
					bits = 0;
				}
				else
				{
					bits = (Single)(data/Math.Pow(10,point));
				}

				return read_count;
			}
		}
		public virtual int Read( float[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
		
			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( float[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iSingleCounter = offset; iSingleCounter < iEndIndex; iSingleCounter++)
				iBitsRead += Read(out bits[iSingleCounter]);

			return iBitsRead;
		}
		public virtual int Read( float[] bits, float max_value, int point )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value, point );
			}
			return read_count;
		}
		#endregion

		//-------------------------------------------------------------------
		#region Read() : 64비트 읽기
		//-------------------------------------------------------------------
		public virtual int Read( out ulong bits)
		{
			return Read(out bits, 0, c_size_uint64);
		}
		public virtual int Read( out ulong bits, int bitIndex, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bitIndex < 0)
				throw new ArgumentOutOfRangeException("bitIndex", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (c_size_uint64 - bitIndex))
				throw new ArgumentException("Argument_InvalidCountOrBitIndex_UInt64");

			int iBitIndex1 = (bitIndex >> 5) < 1 ? bitIndex : -1;
			int iBitIndex2 = (bitIndex + count) > 32 ? (iBitIndex1 < 0 ? bitIndex - 32 : 0) : -1;
			int iCount1 = iBitIndex1 > -1 ? (iBitIndex1 + count > 32 ? 32 - iBitIndex1 : count) : 0;
			int iCount2 = iBitIndex2 > -1 ? (iCount1 == 0 ? count : count - iCount1) : 0;

			uint uiBitsRead = 0;
			uint uiBits1 = 0;
			uint uiBits2 = 0;
			if(iCount1 > 0)
			{
				uint uiBitIndex1 = (uint)iBitIndex1;
				uint uiCount1 = (uint)iCount1;
				uiBitsRead = Read(ref uiBits1, ref uiBitIndex1, ref uiCount1);
			}
			if(iCount2 > 0)
			{
				uint uiBitIndex2 = (uint)iBitIndex2;
				uint uiCount2 = (uint)iCount2;
				uiBitsRead += Read(ref uiBits2, ref uiBitIndex2, ref uiCount2);
			}

			bits = ((ulong)uiBits2 << 32) | (ulong)uiBits1;

			return (int)uiBitsRead;
		}
		public virtual int Read( out ulong bits, ulong max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		public virtual int Read( ulong[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( ulong[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iUInt64Counter = offset; iUInt64Counter < iEndIndex; iUInt64Counter++)
				iBitsRead += Read(out bits[iUInt64Counter]);
		
			return iBitsRead;
		}
		public virtual int Read( ulong[] bits, ulong max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public virtual int Read( out long bits)
		{
			return Read(out bits, 0, c_size_uint64);
		}
		public virtual int Read( out long bits, int bitIndex, int count)
		{
			ulong ulBits = 0;
			int iBitsRead = Read(out ulBits, bitIndex, count);

			bits = (long)ulBits;

			return iBitsRead;
		}
		public virtual int Read( out long bits, long max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_uint64 )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				read_count += Read( out bits, 0, count );
				if( sign ) bits = (Int64)(-bits);
				return read_count;
			}
		}
		public virtual int Read( long[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( long[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iInt64Counter = offset; iInt64Counter < iEndIndex; iInt64Counter++)
				iBitsRead += Read(out bits[iInt64Counter]);
		
			return iBitsRead;
		}
		public virtual int Read( long[] bits, long max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		public virtual int Read( out double bits)
		{
			return Read(out bits, 0, c_size_double);
		}
		public virtual int Read( out double bits, int bitIndex, int count)
		{
			ulong ulBits = 0;
			int iBitsRead = Read(out ulBits, bitIndex, count);

			bits = BitConverter.ToDouble(BitConverter.GetBytes(ulBits), 0);

			return iBitsRead;
		}
		public virtual int Read( out double bits, double max_value, int point )
		{
			Int64 max_data = (Int64)(max_value*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_double )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				Int64 data = 0;

				read_count += Read( out data, 0, count );
				if( sign ) data = (Int64)(-data);
				if( data == 0 )
				{
					bits = 0;
				}
				else
				{
					bits = (Double)(data/Math.Pow(10,point));
				}

				return read_count;
			}
		}
		public virtual int Read( double[] bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");

			return Read(bits, 0, bits.Length);
		}
		public virtual int Read( double[] bits, int offset, int count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitBuffer");
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset", "ArgumentOutOfRange_NegativeParameter");
			if(count < 0)
				throw new ArgumentOutOfRangeException("count", "ArgumentOutOfRange_NegativeParameter");
			if(count > (bits.Length - offset))
				throw new ArgumentException("Argument_InvalidCountOrOffset");

			int iEndIndex = offset + count;
			int iBitsRead = 0;
			for(int iDoubleCounter = offset; iDoubleCounter < iEndIndex; iDoubleCounter++)
				iBitsRead += Read(out bits[iDoubleCounter]);

			return iBitsRead;
		}
		public virtual int Read( double[] bits, double max_value, int point )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value, point );
			}
			return read_count;
		}
		#endregion

		//-------------------------------------------------------------------
		#region Write() : Object 읽기
		//-------------------------------------------------------------------
		public virtual object Read( eType type, object max_value )
		{
			return Read( type, 1, max_value, 2 );
		}
		public virtual object Read( eType type, int array_size, object max_value, int point )
		{
			int read_count = 0;

			switch( type )
			{
			case eType.SByte:	{ sbyte		data;						read_count = Read( out data, (sbyte)max_value );				return data; }
			case eType.SBytes:	{ sbyte		data;						read_count = Read( out data, (sbyte)max_value );				return data; }
			case eType.Byte:	{ byte		data;						read_count = Read( out data, (byte)max_value );					return data; }
			case eType.Bytes:	{ byte[]	data=new byte[array_size];	read_count = Read( data, (byte)max_value );						return data; }
			case eType.Int16:	{ short		data;						read_count = Read( out data, (short)max_value );				return data; }
			case eType.Int16s:	{ short[]	data=new short[array_size];	read_count = Read( (short[])data, (short)max_value );			return data; }
			case eType.UInt16:	{ ushort	data;						read_count = Read( out data, (ushort)max_value );				return data; }
			case eType.UInt16s:	{ ushort[]	data=new ushort[array_size];read_count = Read( (ushort[])data, (ushort)max_value );			return data; }
			case eType.Int32:	{ int		data;						read_count = Read( out data, (int)max_value );					return data; }
			case eType.Int32s:	{ int[]		data=new int[array_size];	read_count = Read( (int[])data, (int)max_value );				return data; }
			case eType.UInt32:	{ uint		data;						read_count = Read( out data, (uint)max_value );					return data; }
			case eType.UInt32s:	{ uint[]	data=new uint[array_size];	read_count = Read( (uint[])data, (uint)max_value );				return data; }
			case eType.Int64:	{ long		data;						read_count = Read( out data, (long)max_value );					return data; }
			case eType.Int64s:	{ long[]	data=new long[array_size];	read_count = Read( (long[])data, (long)max_value );				return data; }
			case eType.UInt64:	{ ulong		data;						read_count = Read( out data, (ulong)max_value );				return data; }
			case eType.UInt64s:	{ ulong[]	data=new ulong[array_size];	read_count = Read( (ulong[])data, (ulong)max_value );			return data; }
			case eType.Single:	{ float		data;						read_count = Read( out data, (float)max_value, point );			return data; }
			case eType.Singles:	{ float[]	data=new float[array_size];	read_count = Read( (float[])data, (float)max_value, point );	return data; }
			case eType.Double:	{ double	data;						read_count = Read( out data, (double)max_value, point );		return data; }
			case eType.Doubles:	{ double[]	data=new double[array_size];read_count = Read( (double[])data, (double)max_value, point );	return data; }
			case eType.String:	{ string	data;						read_count = Read( out data );									return data; }
			case eType.Strings:	{ string[]	data=new string[array_size];read_count = Read( (string[])data );							return data; }
			}
			
			return null;
		}
		#endregion

		//-------------------------------------------------------------------
		#region 로컬 오퍼레이터
		//-------------------------------------------------------------------
		public virtual cBitStream And( cBitStream bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitStream");
			if(bits.Length != m_buffer_length)
				throw new ArgumentException("Argument_DifferentBitStreamLengths");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint uiWholeUInt32Lengths = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint uiCounter = 0;

			for(uiCounter = 0; uiCounter < uiWholeUInt32Lengths; uiCounter++)
				bstrmNew.m_buffer[uiCounter] = m_buffer[uiCounter] & bits.m_buffer[uiCounter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint uiBitMask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[uiCounter] = m_buffer[uiCounter] & bits.m_buffer[uiCounter] & uiBitMask;
			}

			return bstrmNew;
		}
		public virtual cBitStream Or(cBitStream bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitStream");
			if(bits.Length != m_buffer_length)
				throw new ArgumentException("Argument_DifferentBitStreamLengths");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint uiWholeUInt32Lengths = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint uiCounter = 0;

			for(uiCounter = 0; uiCounter < uiWholeUInt32Lengths; uiCounter++)
				bstrmNew.m_buffer[uiCounter] = m_buffer[uiCounter] | bits.m_buffer[uiCounter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint uiBitMask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[uiCounter] = m_buffer[uiCounter] | bits.m_buffer[uiCounter] & uiBitMask;
			}

			return bstrmNew;
		}
		public virtual cBitStream Xor(cBitStream bits)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitStream");
			if(bits.Length != m_buffer_length)
				throw new ArgumentException("Argument_DifferentBitStreamLengths");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint uiWholeUInt32Lengths = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint uiCounter = 0;

			for(uiCounter = 0; uiCounter < uiWholeUInt32Lengths; uiCounter++)
				bstrmNew.m_buffer[uiCounter] = m_buffer[uiCounter] ^ bits.m_buffer[uiCounter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint uiBitMask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[uiCounter] = m_buffer[uiCounter] ^ bits.m_buffer[uiCounter] & uiBitMask;
			}

			return bstrmNew;
		}
		public virtual cBitStream Not()
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint uiWholeUInt32Lengths = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint uiCounter = 0;

			for(uiCounter = 0; uiCounter < uiWholeUInt32Lengths; uiCounter++)
				bstrmNew.m_buffer[uiCounter] = ~m_buffer[uiCounter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint uiBitMask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[uiCounter] = ~m_buffer[uiCounter] & uiBitMask;
			}

			return bstrmNew;
		}
		#endregion

		//-------------------------------------------------------------------
		#region 비트 쉬프트
		//-------------------------------------------------------------------
		public virtual cBitStream ShiftLeft(long count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");

			// Create a copy of the current stream
			cBitStream bstrmNew = this.Copy();

			uint uiCount = (uint)count;
			uint uiLength = (uint)bstrmNew.Length;

			if(uiCount >= uiLength)
			{
				// Clear out all bits
				bstrmNew.Position = 0;

				for(uint uiBitCounter = 0; uiBitCounter < uiLength; uiBitCounter++)
					bstrmNew.Write(false);
			}
			else // count < Length
			{
				bool blnBit = false;
				for(uint uiBitCounter = 0; uiBitCounter < uiLength - uiCount; uiBitCounter++)
				{
					bstrmNew.Position = uiCount + uiBitCounter;
					bstrmNew.Read(out blnBit);
					bstrmNew.Position = uiBitCounter;
					bstrmNew.Write(blnBit);
				}
			
				// Clear out the last count bits
				for(uint uiBitCounter = uiLength - uiCount; uiBitCounter < uiLength; uiBitCounter++)
					bstrmNew.Write(false);
			}

			bstrmNew.Position = 0;
		
			return bstrmNew;
		}
		public virtual cBitStream ShiftRight(long count)
		{
			if(!m_open)
				throw new ObjectDisposedException("ObjectDisposed_BitStreamClosed");

			// Create a copy of the current stream
			cBitStream bstrmNew = this.Copy();

			uint uiCount = (uint)count;
			uint uiLength = (uint)bstrmNew.Length;

			if(uiCount >= uiLength)
			{
				// Clear out all bits
				bstrmNew.Position = 0;

				for(uint uiBitCounter = 0; uiBitCounter < uiLength; uiBitCounter++)
					bstrmNew.Write(false);
			}
			else // count < Length
			{
				bool blnBit = false;
				for(uint uiBitCounter = 0; uiBitCounter < uiLength - uiCount; uiBitCounter++)
				{
					bstrmNew.Position = uiBitCounter;
					bstrmNew.Read(out blnBit);
					bstrmNew.Position = uiBitCounter + uiCount;
					bstrmNew.Write(blnBit);
				}

				// Clear out the first count bits
				bstrmNew.Position = 0;
				for(uint uiBitCounter = 0; uiBitCounter < uiCount; uiBitCounter++)
					bstrmNew.Write(false);
			}

			bstrmNew.Position = 0;
		
			return bstrmNew;
		}

		#endregion

		//-------------------------------------------------------------------
		#region ToString() : 문자열로 변환
		//-------------------------------------------------------------------
		public override string ToString()
		{
			uint uiWholeUInt32Lengths = m_buffer_length >> 5;
			uint uiCounter = 0;
			int iBitCounter = 0;
			uint ui1 = 1;

			StringBuilder sb = new StringBuilder((int)m_buffer_length);

			for(uiCounter = 0; uiCounter < uiWholeUInt32Lengths; uiCounter++)
			{
				sb.Append("[" + uiCounter.ToString(s_format_provider) +"]:{");
				for(iBitCounter = 31; iBitCounter >= 0; iBitCounter--)
				{
					uint uiBitMask = ui1 << iBitCounter;
					
					if((m_buffer[uiCounter] & uiBitMask) == uiBitMask)
						sb.Append('1');
					else
						sb.Append('0');
				}
				sb.Append("}\r\n");
			}

			// Are there any further bits in the buffer?
			if((m_buffer_length & 31) > 0)
			{
				sb.Append("[" + uiCounter.ToString(s_format_provider) +"]:{");
				int iBitCounterMin = (int)(32 - (m_buffer_length & 31));

				for(iBitCounter = 31; iBitCounter >= iBitCounterMin; iBitCounter--)
				{
					uint uiBitMask = ui1 << iBitCounter;
					
					if((m_buffer[uiCounter] & uiBitMask) == uiBitMask)
						sb.Append('1');
					else
						sb.Append('0');
				}

				for(iBitCounter = iBitCounterMin - 1; iBitCounter >= 0; iBitCounter--)
					sb.Append('.');

				sb.Append("}\r\n");
			}

			return sb.ToString();
		}
		public static string ToString(bool bit)
		{
			string str = "Boolean{" + (bit ? 1 : 0) + "}";
			return str;
		}
		public static string ToString(byte bits)
		{
			StringBuilder sb = new StringBuilder(8);
			uint ui1 = 1;

			sb.Append("Byte{");
			for(int iBitCounter = 7; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((bits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(sbyte bits)
		{
			byte bytBits = (byte)bits;

			StringBuilder sb = new StringBuilder(8);
			uint ui1 = 1;

			sb.Append("SByte{");
			for(int iBitCounter = 7; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((bytBits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(char bits)
		{
			StringBuilder sb = new StringBuilder(16);
			uint ui1 = 1;

			sb.Append("Char{");
			for(int iBitCounter = 15; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((bits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(ushort bits)
		{
			short sBits = (short)bits;

			StringBuilder sb = new StringBuilder(16);
			uint ui1 = 1;

			sb.Append("UInt16{");
			for(int iBitCounter = 15; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((sBits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(short bits)
		{
			StringBuilder sb = new StringBuilder(16);
			uint ui1 = 1;

			sb.Append("Int16{");
			for(int iBitCounter = 15; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((bits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(uint bits)
		{
			StringBuilder sb = new StringBuilder(32);
			uint ui1 = 1;

			sb.Append("UInt32{");
			for(int iBitCounter = 31; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((bits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(int bits)
		{
			uint uiBits = (uint)bits;

			StringBuilder sb = new StringBuilder(32);
			uint ui1 = 1;

			sb.Append("Int32{");
			for(int iBitCounter = 31; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((uiBits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(ulong bits)
		{
			StringBuilder sb = new StringBuilder(64);
			ulong ul1 = 1;

			sb.Append("UInt64{");
			for(int iBitCounter = 63; iBitCounter >= 0; iBitCounter--)
			{
				ulong ulBitMask = ul1 << iBitCounter;

				if((bits & ulBitMask) == ulBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(long bits)
		{
			ulong ulBits = (ulong)bits;

			StringBuilder sb = new StringBuilder(64);
			ulong ul1 = 1;

			sb.Append("Int64{");
			for(int iBitCounter = 63; iBitCounter >= 0; iBitCounter--)
			{
				ulong ulBitMask = ul1 << iBitCounter;

				if((ulBits & ulBitMask) == ulBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(float bits)
		{
			byte [] abytBits = BitConverter.GetBytes(bits);
			uint uiBits = (uint)abytBits[0] | ((uint)abytBits[1]) << 8 | ((uint)abytBits[2]) << 16 | ((uint)abytBits[3]) << 24;

			StringBuilder sb = new StringBuilder(32);
			uint ui1 = 1;

			sb.Append("Single{");
			for(int iBitCounter = 31; iBitCounter >= 0; iBitCounter--)
			{
				uint uiBitMask = ui1 << iBitCounter;

				if((uiBits & uiBitMask) == uiBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		public static string ToString(double bits)
		{
			byte [] abytBits = BitConverter.GetBytes(bits);
			ulong ulBits = (ulong)abytBits[0] | ((ulong)abytBits[1]) << 8 | ((ulong)abytBits[2]) << 16 | ((ulong)abytBits[3]) << 24 |
				((ulong)abytBits[4]) << 32 | ((ulong)abytBits[5]) << 40 | ((ulong)abytBits[6]) << 48 | ((ulong)abytBits[7]) << 56;

			StringBuilder sb = new StringBuilder(64);
			ulong ul1 = 1;

			sb.Append("Double{");
			for(int iBitCounter = 63; iBitCounter >= 0; iBitCounter--)
			{
				ulong ulBitMask = ul1 << iBitCounter;

				if((ulBits & ulBitMask) == ulBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		#endregion

		//-------------------------------------------------------------------
		#region 비공개 함수들
		//-------------------------------------------------------------------

		protected void UpdateLengthForWrite(uint bits)
		{
			// Increment m_buffer_length
			m_buffer_length += bits;
		}
		protected void UpdateIndicesForWrite(uint bits)
		{
			// Increment m_bit_index
			m_bit_index += bits;

			if(m_bit_index == c_bit_buffer_unit_size)
			{
				// Increment m_buffer_index
				m_buffer_index++;

				// Reset the bit index
				m_bit_index = 0;

				// Redimension the bit buffer if necessary
				if(m_buffer.Length == (m_buffer_length >> c_bit_buffer_unit_size_shift))
					m_buffer = ReDimPreserve(m_buffer, (uint)m_buffer.Length << 1);

			}
			else
			if(m_bit_index > c_bit_buffer_unit_size)
			{
				throw new InvalidOperationException("InvalidOperation_BitIndexGreaterThan32");
			}
		}

		protected void UpdateIndicesForRead(uint bits)
		{
			// Increment m_bit_index
			m_bit_index += bits;
			if(m_bit_index == c_bit_buffer_unit_size)
			{
				// Increment m_buffer_index
				m_buffer_index++;

				// Reset the bit index
				m_bit_index = 0;
			}
			else if(m_bit_index > c_bit_buffer_unit_size)
				throw new InvalidOperationException("InvalidOperation_BitIndexGreaterThan32");
		}
		protected static uint [] ReDimPreserve(uint [] buffer, uint newLength)
		{
			uint [] auiNewBuffer = new uint [newLength];

			uint uiBufferLength = (uint)buffer.Length;
			
			if(uiBufferLength < newLength)
				Buffer.BlockCopy(buffer, 0, auiNewBuffer, 0, (int)(uiBufferLength << 2));
			else // buffer.Length >= newLength
				Buffer.BlockCopy(buffer, 0, auiNewBuffer, 0, (int)(newLength << 2));

			// Free the previously allocated buffer
			buffer = null;
			
			return auiNewBuffer;
		}

		#endregion

		//-------------------------------------------------------------------
		#region 공개 함수들
		//-------------------------------------------------------------------
		public override void Close()
		{
			m_open = false;
			// Reset indices
			m_buffer_index = 0;
			m_bit_index = 0;
		}

		public virtual uint [] GetBuffer()
		{
			return m_buffer;
		}
		public virtual cBitStream Copy()
		{
			cBitStream bstrmCopy = new cBitStream(this.Length);

			Buffer.BlockCopy(m_buffer, 0, bstrmCopy.m_buffer, 0, bstrmCopy.m_buffer.Length << 2);

			bstrmCopy.m_buffer_length = this.m_buffer_length;
		
			return bstrmCopy;
		}

		#endregion

		//-------------------------------------------------------------------
		#region 지원하지 않는 함수들
		//-------------------------------------------------------------------
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("NotSupported_AsyncOps");
		}
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("NotSupported_AsyncOps");
		}
		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("NotSupported_AsyncOps");
		}
		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("NotSupported_AsyncOps");
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException("NotSupported_Seek");
		}
		public override void SetLength(long value)
		{
			throw new NotSupportedException("NotSupported_SetLength");
		}
		public override void Flush()
		{
			throw new NotSupportedException("NotSupported_Flush");
		}
		#endregion

		//-------------------------------------------------------------------
		#region Implicit Operators
		//-------------------------------------------------------------------
		public static implicit operator cBitStream(MemoryStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_MemoryStream");

			return new cBitStream((Stream)bits);
		}
		public static implicit operator MemoryStream(cBitStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitStream");

			return new MemoryStream(bits.ToByteArray());
		}
		public static implicit operator cBitStream(FileStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_FileStream");

			return new cBitStream((Stream)bits);
		}
		public static implicit operator cBitStream(BufferedStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BufferedStream");

			return new cBitStream((Stream)bits);
		}
		public static implicit operator BufferedStream(cBitStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_BitStream");

			return new BufferedStream((MemoryStream)bits);
		}
		public static implicit operator cBitStream(NetworkStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_NetworkStream");

			return new cBitStream((Stream)bits);
		}
		public static implicit operator cBitStream(CryptoStream bits)
		{
			if(bits == null)
				throw new ArgumentNullException("bits", "ArgumentNull_CryptoStream");

			return new cBitStream((Stream)bits);
		}

		#endregion

	}
}

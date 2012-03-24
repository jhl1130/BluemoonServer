//----------------------------------------------------------------------------------------------------
// cBitStream
// : 비트스트림
//  -JHL-2012-02-20
//----------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 비트 스트림 객체
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cBitStream : Stream
	{
		//----------------------------------------------------------------------------------------------------
		#region 상수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// byte형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_byte					= 8;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// char형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_char					= 16;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// short형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_short				= 16;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// int형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_int					= 32;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// float형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_float				= 32;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// long형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_long					= 64;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// double형 비트수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_size_double				= 64;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트버퍼 저장 단위 크기
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const uint	c_bit_buffer_unit_size		= c_size_int;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트버퍼 저장 단위 크기의 쉬프트 수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const int		c_bit_buffer_unit_size_shift= 5;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트버퍼 저장 단위 크기의 비트필터(111111)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected const uint	c_bit_buffer_unit_size_mod	= 31;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트 마스트
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected static uint[] c_bit_mask	= new uint[]
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

		//----------------------------------------------------------------------------------------------------
		#region 변수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 오픈 플래그
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected bool		m_open = true;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected uint[]	m_buffer;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 길이
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected uint		m_buffer_length;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 인덱스
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected uint		m_buffer_index;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트 인덱스
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected uint		m_bit_index;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 포맷 프로바이더
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected static IFormatProvider s_format_provider = (IFormatProvider)CultureInfo.InvariantCulture;
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 속성
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 길이(단위:1비트)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public override long Length		{get{return (long)m_buffer_length;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 길이(단위:8비트)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public virtual long Length8		{get{return (long)(m_buffer_length >> 3) + (long)((m_buffer_length & 7) > 0 ? 1 : 0);}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 길이(단위:16비트)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public virtual long Length16	{get{return (long)(m_buffer_length >> 4) + (long)((m_buffer_length & 15) > 0 ? 1 : 0);}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 길이(단위:32비트)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public virtual long Length32	{get{return (long)(m_buffer_length >> 5) + (long)((m_buffer_length & 31) > 0 ? 1 : 0);}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 길이(단위:64비트)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public virtual long Length64	{get{return (long)(m_buffer_length >> 6) + (long)((m_buffer_length & 63) > 0 ? 1 : 0);}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 용량(버퍼길이*저장단위)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public virtual long Capacity	{get{return ((long)m_buffer.Length) << c_bit_buffer_unit_size_shift;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기 가능 유무
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public override bool CanRead	{get{return m_open;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 검색 가능 유무
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public override bool CanSeek	{get{return false;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 쓰기 가능 유무
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public override bool CanWrite	{get{return m_open;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼길이 변경 가능 유무
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public static bool CanSetLength	{get{return false;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 플러시 가능 유무
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public static bool CanFlush		{get{return false;}}

		//----------------------------------------------------------------------------------------------------
		#region 비트 포지션
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비트 포지션
		/// </summary>
		//----------------------------------------------------------------------------------------------------
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

		//----------------------------------------------------------------------------------------------------
		#region 생성자
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public cBitStream()
		{
			m_buffer = new uint[1];
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="capacity">초기용량</param>
		//----------------------------------------------------------------------------------------------------
		public cBitStream( long capacity )
		{
			if(capacity <= 0) throw new ArgumentOutOfRangeException("ArgumentOutOfRange_NegativeOrZeroCapacity");
			m_buffer = new uint[(capacity >> c_bit_buffer_unit_size_shift) + ((capacity & c_bit_buffer_unit_size_mod) > 0 ? 1 : 0)];
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="stream">스트림객체</param>
		//----------------------------------------------------------------------------------------------------
		public cBitStream( Stream stream ) : this()
		{
			if(stream==null) throw new Exception("bits=null");
			
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

		//----------------------------------------------------------------------------------------------------
		#region Write() : 비트 기록
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		//----------------------------------------------------------------------------------------------------
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
				Write( ref bits, ref uiValue_StartIndex, ref uiValue_RemainingBits);
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Write() : 1비트 기록
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bit">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public virtual void Write( bool bit )
		{
			uint data = (uint)(bit?1:0);
			uint index = 0;
			uint count = 1;
			Write( ref data, ref index, ref count );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public virtual void Write( bool[] bits )
		{
			if(bits==null) throw new Exception("bits=null");
			Write(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		//----------------------------------------------------------------------------------------------------
		public virtual void Write( bool[] bits, int offset, int count )
		{
			if(!m_open)			throw new Exception("open=false");
			if(bits==null)		throw new Exception("bits=null");
			if(offset<0)		throw new Exception("param<0");
			if(count<0)			throw new Exception("param<0");
			if(count>(bits.Length-offset))
											throw new Exception("bits.Length<param");

			int end = offset+count;
			for( int c=offset; c<end; ++c )
			{
				Write( bits[c] );
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Write() : 8비트 기록
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public virtual void Write( byte bits)
		{
			Write(bits, 0, c_size_byte);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( byte bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_byte-offset))
							throw new Exception("param>size_Byte");

			uint bit_data	= (uint)bits;
			uint bit_index	= (uint)offset;
			uint bit_count	= (uint)count;

			Write( ref bit_data, ref bit_index, ref bit_count );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( byte bits, byte max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( string bits )
		{
			byte[] data = new UTF8Encoding(true).GetBytes( ((String)bits).ToCharArray() );
			Write( (ushort)data.Length );
			Write( data );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( byte[] bits )
		{
			if(bits==null)	throw new Exception("bits=null");
			Write( bits, 0, bits.Length );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public override void Write( byte[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			for( int c=offset; c<end; ++c )
			{
				Write( bits[c] );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( byte[] bits, byte max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( byte b in bits )
			{
				Write( b, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( string[] bits )
		{
			foreach( string s in bits )
			{
				Write( s );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( sbyte bits )
		{
			Write( bits, 0, c_size_byte );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">시작인덱스</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( sbyte bits, int index, int count )
		{
			byte byte_bits = (byte)bits;
			Write( byte_bits, index, count );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
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
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( sbyte[] bits )
		{
			if(bits==null) throw new Exception("bits=null");
			Write( bits, 0, bits.Length );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( sbyte[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			byte[] abytBits = new byte [count];
			Buffer.BlockCopy( bits, offset, abytBits, 0, count );

			Write(abytBits, 0, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( sbyte[] bits, sbyte max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( sbyte b in bits )
			{
				Write( b, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public override void WriteByte( byte bits )
		{
			Write(bits);
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Write() : 16비트 기록
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public virtual void Write( char bits )
		{
			Write( bits, 0, c_size_char );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( char bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_char-index))
							throw new Exception("param>size_Char");

			uint bit_data	= (uint)bits;
			uint bit_index	= (uint)index;
			uint bit_count	= (uint)count;

			Write( ref bit_data, ref bit_index, ref bit_count );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		//----------------------------------------------------------------------------------------------------
		public virtual void Write( char[] bits )
		{
			if(bits==null) throw new Exception("bits=null");
			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( char[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			for( int c=offset; c < end; ++c )
			{
				Write( bits[c] );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ushort bits )
		{
			Write( bits, 0, c_size_short );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ushort bits, int index, int count )
		{
			if(!m_open)			throw new Exception("open=false");
			if(index<0)			throw new Exception("param<0");
			if(count<0)			throw new Exception("param<0");
			if(count>(c_size_short-index))
								throw new Exception("param>size_UInt16");

			uint uiBits = (uint)bits;
			uint bit_index = (uint)index;
			uint bit_count = (uint)count;

			Write( ref uiBits, ref bit_index, ref bit_count );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ushort bits, ushort max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ushort[] bits)
		{
			if(!m_open)			throw new Exception("open=false");
			if(bits==null)		throw new Exception("bits=null");
		
			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ushort[] bits, int offset, int count )
		{
			if(!m_open)			throw new Exception("open=false");
			if(bits==null)		throw new Exception("bits=null");
			if(offset<0)		throw new Exception("param<0");
			if(count<0)			throw new Exception("param<0");
			if(count>(bits.Length - offset))
								throw new Exception("bits.Length<param");

			int end = offset + count;
			for(int iUInt16Counter = offset; iUInt16Counter < end; iUInt16Counter++)
				Write(bits[iUInt16Counter]);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ushort[] bits, ushort max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( ushort bit in bits )
			{
				Write( bit, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( short bits)
		{
			Write(bits, 0, c_size_short);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( short bits, int index, int count )
		{
			// Convert the value to an UInt16
			ushort usBits = (ushort)bits;
			
			Write(usBits, index, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( short bits, short max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_short )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( short[] bits )
		{
			if(!m_open)			throw new Exception("open=false");
			if(bits==null)		throw new Exception("bits=null");

			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( short[] bits, int offset, int count )
		{
			if(!m_open)			throw new Exception("open=false");
			if(bits==null)		throw new Exception("bits=null");
			if(offset<0)		throw new Exception("param<0");
			if(count<0)			throw new Exception("param<0");
			if(count>(bits.Length-offset))
								throw new Exception("bits.Length<param");

			ushort [] ausBits = new ushort [count];
			Buffer.BlockCopy(bits, offset << 1, ausBits, 0, count << 1);
		
			Write(ausBits, 0, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( short[] bits, short max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( short bit in bits )
			{
				Write( bit, max_value );
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Write() : 32비트 기록
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( uint bits)
		{
			Write(bits, 0, c_size_int);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( uint bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_int-index))
							throw new Exception("param>size_uint");

			uint bit_index = (uint)index;
			uint bit_count = (uint)count;

			Write( ref bits, ref bit_index, ref bit_count );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( uint bits, uint max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( uint[] bits)
		{
			if(!m_open)
							throw new Exception("open=false");
			if(bits==null)
							throw new Exception("bits=null");
		
			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( uint[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			for(int i = offset; i < end; i++)
				Write(bits[i]);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( uint[] bits, uint max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( uint bit in bits )
			{
				Write( bit, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( int bits)
		{
			Write(bits, 0, c_size_int);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( int bits, int index, int count )
		{
			// Convert the value to an uint
			uint uiBits = (uint)bits;
			
			Write(uiBits, index, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( int bits, int max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_int )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( int[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( int[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			uint[] auiBits = new uint [count];
			Buffer.BlockCopy(bits, offset << 2, auiBits, 0, count << 2);
		
			Write(auiBits, 0, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( int[] bits, int max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( int bit in bits )
			{
				Write( bit, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( float bits)
		{
			Write(bits, 0, c_size_float);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( float bits, int index, int count )
		{
			byte[] abytBits = BitConverter.GetBytes(bits);
			uint uiBits = (uint)abytBits[0] | ((uint)abytBits[1]) << 8 | ((uint)abytBits[2]) << 16 | ((uint)abytBits[3]) << 24;
			Write(uiBits, index, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( float bits, float max_value, byte point )
		{
			long data = (long)(Math.Round( (double)bits, (int)point )*Math.Pow(10,point));
			long max_data = (long)(Math.Round( (double)max_value, (int)point )*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_float )
			{
				Write( bits );
			}
			else
			{
				Write( data<0 );
				Write( Math.Abs(data), 0, count );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( float[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( float[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			for(int i = offset; i < end; i++)
				Write(bits[i]);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( float[] bits, float max_value, byte point )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( float bit in bits )
			{
				Write( bit, max_value, point );
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Write() : 64비트 기록
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ulong bits)
		{
			Write(bits, 0, c_size_long);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ulong bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_long-index))
							throw new Exception("param>size_UInt64");

			int iBitIndex1 = (index >> 5) < 1 ? index : -1;
			int iBitIndex2 = (index + count) > 32 ? (iBitIndex1 < 0 ? index - 32 : 0) : -1;
			int iCount1 = iBitIndex1 > -1 ? (iBitIndex1 + count > 32 ? 32 - iBitIndex1 : count) : 0;
			int iCount2 = iBitIndex2 > -1 ? (iCount1 == 0 ? count : count - iCount1) : 0;

			if(iCount1 > 0)
			{
				uint uiBits1 = (uint)bits;
				uint bit_index1 = (uint)iBitIndex1;
				uint bit_count1 = (uint)iCount1;
				Write( ref uiBits1, ref bit_index1, ref bit_count1);
			}
			if(iCount2 > 0)
			{
				uint uiBits2 = (uint)(bits >> 32);
				uint bit_index2 = (uint)iBitIndex2;
				uint bit_count2 = (uint)iCount2;
				Write( ref uiBits2, ref bit_index2, ref bit_count2);
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ulong bits, ulong max_value )
		{
			Write( bits, 0, cBitSize.BitSize(max_value) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ulong[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ulong[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			for(int i = offset; i < end; i++)
				Write(bits[i]);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( ulong[] bits, ulong max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( ulong bit in bits )
			{
				Write( bit, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( long bits)
		{
			Write(bits, 0, c_size_long);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( long bits, int index, int count )
		{
			// Convert the value to an UInt64
			ulong ulBits = (ulong)bits;
			
			Write(ulBits, index, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( long bits, long max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_long )
			{
				Write( bits );
			}
			else
			{
				Write( bits<0 );
				Write( Math.Abs(bits), 0, count );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( long[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( long[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			ulong [] aulBits = new ulong [count];
			Buffer.BlockCopy(bits, offset << 4, aulBits, 0, count << 4);
		
			Write(aulBits, 0, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( long[] bits, long max_value )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( long bit in bits )
			{
				Write( bit, max_value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( double bits)
		{
			Write(bits, 0, c_size_double);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( double bits, int index, int count )
		{
			byte[] abytBits = BitConverter.GetBytes(bits);
			ulong ulBits = (ulong)abytBits[0] | ((ulong)abytBits[1]) << 8 | ((ulong)abytBits[2]) << 16 | ((ulong)abytBits[3]) << 24 |
				((ulong)abytBits[4]) << 32 | ((ulong)abytBits[5]) << 40 | ((ulong)abytBits[6]) << 48 | ((ulong)abytBits[7]) << 56;

			Write(ulBits, index, count);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( double bits, double max_value, byte point )
		{
			long data = (long)(Math.Round( (double)bits, (int)point )*Math.Pow(10,point));
			long max_data = (long)(Math.Round( (double)max_value, (int)point )*Math.Pow(10,point));

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
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( double[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			Write(bits, 0, bits.Length);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( double[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			for(int i = offset; i < end; i++)
				Write(bits[i]);
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( double[] bits, double max_value, byte point )
		{
			if(bits==null)	throw new Exception("bits=null");
			foreach( double bit in bits )
			{
				Write( bit, max_value, point );
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Write() : object 기록
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록
		/// </summary>
		/// <param name="value">데이터</param>
		/// <param name="max_size">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void Write( object value, ulong max_size, byte point )
		{
			Type type = value.GetType();
			if(type==typeof(sbyte) )	{Write( (sbyte)value,	(sbyte)max_size );			return;}
			if(type==typeof(byte) )		{Write( (byte)value,	(byte)max_size );			return;}
			if(type==typeof(short) )	{Write( (short)value,	(short)max_size );			return;}
			if(type==typeof(ushort) )	{Write( (ushort)value,	(ushort)max_size );			return;}
			if(type==typeof(int) )		{Write( (int)value,		(int)max_size );			return;}
			if(type==typeof(uint) )		{Write( (uint)value,	(uint)max_size );			return;}
			if(type==typeof(long) )		{Write( (long)value,	(long)max_size );			return;}
			if(type==typeof(ulong) )	{Write( (ulong)value,	(ulong)max_size );			return;}
			if(type==typeof(float) )	{Write( (float)value,	(float)max_size, point );	return;}
			if(type==typeof(double) )	{Write( (double)value,	(double)max_size, point );	return;}
			if(type==typeof(string) )	{Write( (string)value );							return;}
			if(type==typeof(sbyte[]) )	{Write( (sbyte[])value,	(sbyte)max_size );			return;}
			if(type==typeof(byte[]) )	{Write( (byte[])value,	(byte)max_size );			return;}
			if(type==typeof(short[]) )	{Write( (short[])value, (short)max_size );			return;}
			if(type==typeof(ushort[]) )	{Write( (ushort[])value,(ushort)max_size );			return;}
			if(type==typeof(int[]) )	{Write( (int[])value,	(int)max_size );			return;}
			if(type==typeof(uint[]) )	{Write( (uint[])value,	(uint)max_size );			return;}
			if(type==typeof(long[]) )	{Write( (long[])value,	(long)max_size );			return;}
			if(type==typeof(ulong[]) )	{Write( (ulong[])value, (ulong)max_size );			return;}
			if(type==typeof(float[]) )	{Write( (float[])value, (float)max_size, point );	return;}
			if(type==typeof(double[]) )	{Write( (double[])value,(double)max_size, point );	return;}
			if(type==typeof(string[]) )	{Write( (string[])value);							return;}
		}
		#endregion
	
		//----------------------------------------------------------------------------------------------------
		#region WriteTo() : 다른 바이트 스트림에 기록
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이트 스트림 객체에 기록
		/// </summary>
		/// <param name="bits">스트림 객체</param>
        //----------------------------------------------------------------------------------------------------
		public virtual void WriteTo( Stream bits )
		{
			if(bits==null) throw new Exception("bits=null");
		
			byte[] write_bits = ToByteArray();
			bits.Write( write_bits, 0, write_bits.Length );
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Read() : 비트 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
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

		//----------------------------------------------------------------------------------------------------
		#region Read() : 1비트 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bit">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out bool bit )
		{
			if(!m_open)		throw new Exception("open=false");

			uint bit_index = 0;
			uint bit_count = 1;
			uint uiBit = 0;
			uint uread_bits = Read(ref uiBit, ref bit_index, ref bit_count );
			
			bit = Convert.ToBoolean(uiBit);

			return (int)uread_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( bool[] bits )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( bool[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int i = offset; i < end; i++)
				read_bits += Read(out bits[i]);
		
			return read_bits;
		}
		
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Read() : 8비트 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out byte bits )
		{
			return Read(out bits, 0, c_size_byte);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out byte bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_byte-index))
							throw new Exception("param>size_Byte");

			uint bit_index = (uint)index;
			uint bit_count = (uint)count;
			uint uiBits = 0;
			uint uread_bits = Read(ref uiBits, ref bit_index, ref bit_count );
		
			bits = (byte)uiBits;

			return (int)uread_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out byte bits, byte max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out string bits )
		{
			ushort len;
			int read_count = Read( out len );
			byte[] data = new byte[ len ];
			read_count += Read( data );
			bits = new UTF8Encoding(true).GetString( data );
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( byte[] bits )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public override int Read( byte[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bit_count = 0;
			for( int byte_count = offset; byte_count < end; ++byte_count )
			{
				read_bit_count += Read( out bits[byte_count] );
			}
		
			return read_bit_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( byte[] bits, byte max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( string[] bits )
		{
			int read_count = 0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count = Read( out bits[c] );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out sbyte bits )
		{
			return Read(out bits, 0, c_size_byte);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out sbyte bits, int index, int count )
		{
			byte bytBits = 0;
			int read_bits = Read(out bytBits, index, count);
			bits = (sbyte)bytBits;
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
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
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( sbyte[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( sbyte[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int iSByteCounter = offset; iSByteCounter < end; iSByteCounter++)
				read_bits += Read(out bits[iSByteCounter]);
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( sbyte[] bits, sbyte max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 1바이트 읽기
		/// </summary>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public override int ReadByte()
		{
			byte bytBits;
			int read_bits = Read(out bytBits);
			
			if(read_bits == 0)
				return -1;
			else
				return (int)bytBits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이트배열로 일기
		/// </summary>
		/// <returns>읽은데이터</returns>
		//----------------------------------------------------------------------------------------------------
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

		//----------------------------------------------------------------------------------------------------
		#region Read() : 16비트 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out char bits)
		{
			return Read(out bits, 0, c_size_char);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out char bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_char-index))
							throw new Exception("param>size_Char");

			uint bit_index = (uint)index;
			uint bit_count = (uint)count;
			uint uiBits = 0;
			uint uread_bits = Read(ref uiBits, ref bit_index, ref bit_count );
		
			bits = (char)uiBits;

			return (int)uread_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( char[] bits )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( char[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int iCharCounter = offset; iCharCounter < end; iCharCounter++)
				read_bits += Read(out bits[iCharCounter]);
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out ushort bits)
		{
			return Read(out bits, 0, c_size_short);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out ushort bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_short-index))
							throw new Exception("param>size_UInt16");

			uint bit_index = (uint)index;
			uint bit_count = (uint)count;
			uint uiBits = 0;
			uint uread_bits = Read(ref uiBits, ref bit_index, ref bit_count );
		
			bits = (ushort)uiBits;

			return (int)uread_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out ushort bits, ushort max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( ushort[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( ushort[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int iUInt16Counter = offset; iUInt16Counter < end; iUInt16Counter++)
				read_bits += Read(out bits[iUInt16Counter]);
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( ushort[] bits, ushort max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out short bits)
		{
			return Read(out bits, 0, c_size_short);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out short bits, int index, int count )
		{
			ushort usBits = 0;
			int read_bits = Read(out usBits, index, count);

			bits = (short)usBits;

			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out short bits, short max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_short )
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
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( short[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( short[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int iShortCounter = offset; iShortCounter < end; iShortCounter++)
				read_bits += Read(out bits[iShortCounter]);
			
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
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

		//----------------------------------------------------------------------------------------------------
		#region Read() : 32비트 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out uint bits )
		{
			return Read( out bits, 0, c_size_int );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out uint bits, int index, int count )
		{
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_int-index))
							throw new Exception("param>size_uint");

			uint read_index = (uint)index;
			uint read_count = (uint)count;
			uint read_bits = 0;
			uint readed_count = Read( ref read_bits, ref read_index, ref read_count );
		
			bits = read_bits;
			return (int)readed_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out uint bits, uint max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( uint[] bits )
		{
			if(bits==null)	throw new Exception("bits=null");
			return Read( bits, 0, bits.Length );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( uint[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for( int c = offset; c < end; ++c )
			{
				read_bits += Read( out bits[c] );
			}
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( uint[] bits, uint max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out int bits)
		{
			return Read(out bits, 0, c_size_int);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out int bits, int index, int count )
		{
			uint uiBits = 0;
			int read_bits = Read(out uiBits, index, count);

			bits = (int)uiBits;

			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out int bits, int max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_int )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				read_count += Read( out bits, 0, count );
				if( sign ) bits = (int)(-bits);
				return read_count;
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( int[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( int[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int i = offset; i < end; i++)
				read_bits += Read(out bits[i]);
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( int[] bits, int max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out float bits)
		{
			return Read(out bits, 0, c_size_float);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out float bits, int index, int count )
		{
			int uiBits = 0;
			int uread_bits = Read(out uiBits, index, count);

			bits = BitConverter.ToSingle(BitConverter.GetBytes(uiBits), 0);

			return (int)uread_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out float bits, float max_value, byte point )
		{
			long max_data = (long)(Math.Round( (double)max_value, (int)point )*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_float )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				int data = 0;

				read_count += Read( out data, 0, count );
				if( sign ) data = (int)(-data);
				if( data == 0 )
				{
					bits = 0;
				}
				else
				{
					bits = (float)(data/Math.Pow(10,point));
				}

				return read_count;
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( float[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
		
			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( float[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int i = offset; i < end; i++)
				read_bits += Read(out bits[i]);

			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( float[] bits, float max_value, byte point )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value, point );
			}
			return read_count;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Read() : 64비트 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out ulong bits)
		{
			return Read(out bits, 0, c_size_long);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out ulong bits, int index, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(index<0)		throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(c_size_long-index))
							throw new Exception("param>size_UInt64");

			int iBitIndex1 = (index >> 5) < 1 ? index : -1;
			int iBitIndex2 = (index + count) > 32 ? (iBitIndex1 < 0 ? index - 32 : 0) : -1;
			int iCount1 = iBitIndex1 > -1 ? (iBitIndex1 + count > 32 ? 32 - iBitIndex1 : count) : 0;
			int iCount2 = iBitIndex2 > -1 ? (iCount1 == 0 ? count : count - iCount1) : 0;

			uint uread_bits = 0;
			uint uiBits1 = 0;
			uint uiBits2 = 0;
			if(iCount1 > 0)
			{
				uint bit_index1 = (uint)iBitIndex1;
				uint bit_count1 = (uint)iCount1;
				uread_bits = Read(ref uiBits1, ref bit_index1, ref bit_count1);
			}
			if(iCount2 > 0)
			{
				uint bit_index2 = (uint)iBitIndex2;
				uint bit_count2 = (uint)iCount2;
				uread_bits += Read(ref uiBits2, ref bit_index2, ref bit_count2);
			}

			bits = ((ulong)uiBits2 << 32) | (ulong)uiBits1;

			return (int)uread_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out ulong bits, ulong max_value )
		{
			return Read( out bits, 0, cBitSize.BitSize(max_value) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( ulong[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( ulong[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int i = offset; i < end; i++)
				read_bits += Read(out bits[i]);
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( ulong[] bits, ulong max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out long bits)
		{
			return Read(out bits, 0, c_size_long);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out long bits, int index, int count )
		{
			ulong ulBits = 0;
			int read_bits = Read(out ulBits, index, count);

			bits = (long)ulBits;

			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out long bits, long max_value )
		{
			int count = cBitSize.BitSize(max_value);
			if( count+1 >= c_size_long )
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
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( long[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( long[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int i = offset; i < end; i++)
				read_bits += Read(out bits[i]);
		
			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( long[] bits, long max_value )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value );
			}
			return read_count;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out double bits)
		{
			return Read(out bits, 0, c_size_double);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="index">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out double bits, int index, int count )
		{
			ulong ulBits = 0;
			int read_bits = Read(out ulBits, index, count);

			bits = BitConverter.ToDouble(BitConverter.GetBytes(ulBits), 0);

			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( out double bits, double max_value, byte point )
		{
			long max_data = (long)(Math.Round( max_value, (int)point )*Math.Pow(10,point));
			int count = cBitSize.BitSize(max_data);
			if( count+1 >= c_size_double )
			{
				return Read( out bits );
			}
			else
			{
				bool sign = false;
				int read_count = Read( out sign );
				long data = 0;

				read_count += Read( out data, 0, count );
				if( sign ) data = (long)(-data);
				if( data == 0 )
				{
					bits = 0;
				}
				else
				{
					bits = data/Math.Pow(10,point);
				}

				return read_count;
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( double[] bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");

			return Read(bits, 0, bits.Length);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( double[] bits, int offset, int count )
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(offset<0)	throw new Exception("param<0");
			if(count<0)		throw new Exception("param<0");
			if(count>(bits.Length-offset))
							throw new Exception("bits.Length<param");

			int end = offset + count;
			int read_bits = 0;
			for(int i = offset; i < end; i++)
				read_bits += Read(out bits[i]);

			return read_bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="bits">[출력]데이터</param>
		/// <param name="max_value">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns>읽은비트수</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( double[] bits, double max_value, byte point )
		{
			int read_count=0;
			for( int c=0; c<bits.Length; ++c )
			{
				read_count += Read( out bits[c], max_value, point );
			}
			return read_count;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Read() : object 읽기
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽기
		/// </summary>
		/// <param name="type">타입</param>
		/// <param name="out_value">[출력]데이터</param>
		/// <param name="in_value">[입력]데이터</param>
		/// <param name="max_size">최대값</param>
		/// <param name="point">소숫점 자릿수</param>
		/// <returns></returns>
		//----------------------------------------------------------------------------------------------------
		public virtual int Read( Type type, out object out_value, object in_value, ulong max_size, byte point )
		{
			if(type==typeof(sbyte) )	{sbyte	read;	int read_bytes = Read( out read,			(sbyte)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(byte) )		{byte	read;	int read_bytes = Read( out read,			(byte)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(short) )	{short	read;	int read_bytes = Read( out read,			(short)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(ushort) )	{ushort read;	int read_bytes = Read( out read,			(ushort)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(int) )		{int	read;	int read_bytes = Read( out read,			(int)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(uint) )		{uint	read;	int read_bytes = Read( out read,			(uint)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(long) )		{long	read;	int read_bytes = Read( out read,			(long)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(ulong) )	{ulong	read;	int read_bytes = Read( out read,			(ulong)max_size );			out_value=read;		return read_bytes;}
			if(type==typeof(float) )	{float	read;	int read_bytes = Read( out read,			(float)max_size, point );	out_value=read;		return read_bytes;}
			if(type==typeof(double) )	{double read;	int read_bytes = Read( out read,			(double)max_size, point );	out_value=read;		return read_bytes;}
			if(type==typeof(string) )	{string read;	int read_bytes = Read( out read );										out_value=read;		return read_bytes;}
			if(type==typeof(sbyte[]) )	{				int read_bytes = Read( (sbyte[])in_value,	(sbyte)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(byte[]) )	{				int read_bytes = Read( (byte[])in_value,	(byte)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(short[]) )	{				int read_bytes = Read( (short[])in_value,	(short)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(ushort[]) )	{				int read_bytes = Read( (ushort[])in_value,	(ushort)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(int[]) )	{				int read_bytes = Read( (int[])in_value,		(int)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(uint[]) )	{				int read_bytes = Read( (uint[])in_value,	(uint)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(long[]) )	{				int read_bytes = Read( (long[])in_value,	(long)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(ulong[]) )	{				int read_bytes = Read( (ulong[])in_value,	(ulong)max_size );			out_value=in_value;	return read_bytes;}
			if(type==typeof(float[]) )	{				int read_bytes = Read( (float[])in_value,	(float)max_size, point );	out_value=in_value;	return read_bytes;}
			if(type==typeof(double[]) )	{				int read_bytes = Read( (double[])in_value,	(double)max_size, point );	out_value=in_value;	return read_bytes;}
			if(type==typeof(string[]) )	{				int read_bytes = Read( (string[])in_value );							out_value=in_value;	return read_bytes;}
			out_value = null;																											
			return 0;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 로컬 오퍼레이터
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// AND 연산
		/// </summary>
		/// <param name="bits">비트스트림</param>
		/// <returns>비트스트림</returns>
        //----------------------------------------------------------------------------------------------------
		public virtual cBitStream AND( cBitStream bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(bits.Length!=m_buffer_length)
							throw new Exception("m_buffer_length!=param");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint whole_uint_length = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint bit_counter = 0;

			for(bit_counter = 0; bit_counter < whole_uint_length; bit_counter++)
				bstrmNew.m_buffer[bit_counter] = m_buffer[bit_counter] & bits.m_buffer[bit_counter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint mask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[bit_counter] = m_buffer[bit_counter] & bits.m_buffer[bit_counter] & mask;
			}

			return bstrmNew;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// OR 연산
		/// </summary>
		/// <param name="bits">비트스트림</param>
		/// <returns>비트스트림</returns>
        //----------------------------------------------------------------------------------------------------
		public virtual cBitStream OR(cBitStream bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(bits.Length!=m_buffer_length)
							throw new Exception("m_buffer_length!=param");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint whole_uint_length = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint bit_counter = 0;

			for(bit_counter = 0; bit_counter < whole_uint_length; bit_counter++)
				bstrmNew.m_buffer[bit_counter] = m_buffer[bit_counter] | bits.m_buffer[bit_counter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint mask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[bit_counter] = m_buffer[bit_counter] | bits.m_buffer[bit_counter] & mask;
			}

			return bstrmNew;
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// XOR 연산
		/// </summary>
		/// <param name="bits">비트스트림</param>
		/// <returns>비트스트림</returns>
        //----------------------------------------------------------------------------------------------------
		public virtual cBitStream XOR(cBitStream bits)
		{
			if(!m_open)		throw new Exception("open=false");
			if(bits==null)	throw new Exception("bits=null");
			if(bits.Length!=m_buffer_length)
							throw new Exception("m_buffer_length!=param");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint whole_uint_length = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint bit_counter = 0;

			for(bit_counter = 0; bit_counter < whole_uint_length; bit_counter++)
				bstrmNew.m_buffer[bit_counter] = m_buffer[bit_counter] ^ bits.m_buffer[bit_counter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint mask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[bit_counter] = m_buffer[bit_counter] ^ bits.m_buffer[bit_counter] & mask;
			}

			return bstrmNew;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// NOT 연산
		/// </summary>
		/// <returns>비트스트림</returns>
        //----------------------------------------------------------------------------------------------------
		public virtual cBitStream NOT()
		{
			if(!m_open)		throw new Exception("open=false");

			// Create the new BitStream
			cBitStream bstrmNew = new cBitStream(m_buffer_length);

			uint whole_uint_length = m_buffer_length >> c_bit_buffer_unit_size_shift;
			uint bit_counter = 0;

			for(bit_counter = 0; bit_counter < whole_uint_length; bit_counter++)
				bstrmNew.m_buffer[bit_counter] = ~m_buffer[bit_counter];

			// Are there any further bits in the buffer?
			if((m_buffer_length & c_bit_buffer_unit_size_mod) > 0)
			{
				uint mask = uint.MaxValue << (int)(c_bit_buffer_unit_size - (m_buffer_length & c_bit_buffer_unit_size_mod));
				bstrmNew.m_buffer[bit_counter] = ~m_buffer[bit_counter] & mask;
			}

			return bstrmNew;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 비트 쉬프트
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 좌측 쉬프트
		/// </summary>
		/// <param name="count">개수</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual cBitStream ShiftLeft(long count)
		{
			if(!m_open)		throw new Exception("open=false");

			// Create a copy of the current stream
			cBitStream bstrmNew = this.Copy();

			uint bit_count = (uint)count;
			uint uiLength = (uint)bstrmNew.Length;

			if(bit_count >= uiLength)
			{
				// Clear out all bits
				bstrmNew.Position = 0;

				for(uint ui = 0; ui < uiLength; ui++)
					bstrmNew.Write(false);
			}
			else // count < Length
			{
				bool blnBit = false;
				for(uint ui = 0; ui < uiLength - bit_count; ui++)
				{
					bstrmNew.Position = bit_count + ui;
					bstrmNew.Read(out blnBit);
					bstrmNew.Position = ui;
					bstrmNew.Write(blnBit);
				}
			
				// Clear out the last count bits
				for(uint ui = uiLength - bit_count; ui < uiLength; ui++)
					bstrmNew.Write(false);
			}

			bstrmNew.Position = 0;
		
			return bstrmNew;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 우측 쉬프트
		/// </summary>
		/// <param name="count">개수</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual cBitStream ShiftRight(long count)
		{
			if(!m_open)		throw new Exception("open=false");

			// Create a copy of the current stream
			cBitStream bstrmNew = this.Copy();

			uint bit_count = (uint)count;
			uint uiLength = (uint)bstrmNew.Length;

			if(bit_count >= uiLength)
			{
				// Clear out all bits
				bstrmNew.Position = 0;

				for(uint ui = 0; ui < uiLength; ui++)
					bstrmNew.Write(false);
			}
			else // count < Length
			{
				bool blnBit = false;
				for(uint ui = 0; ui < uiLength - bit_count; ui++)
				{
					bstrmNew.Position = ui;
					bstrmNew.Read(out blnBit);
					bstrmNew.Position = ui + bit_count;
					bstrmNew.Write(blnBit);
				}

				// Clear out the first count bits
				bstrmNew.Position = 0;
				for(uint ui = 0; ui < bit_count; ui++)
					bstrmNew.Write(false);
			}

			bstrmNew.Position = 0;
		
			return bstrmNew;
		}

		#endregion

		//----------------------------------------------------------------------------------------------------
		#region ToString() : 문자열로 출력
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public override string ToString()
		{
			uint whole_uint_length = m_buffer_length >> 5;
			uint bit_counter = 0;
			int i = 0;
			uint ui1 = 1;

			StringBuilder sb = new StringBuilder((int)m_buffer_length);

			for(bit_counter = 0; bit_counter < whole_uint_length; bit_counter++)
			{
				sb.Append("[" + bit_counter.ToString(s_format_provider) +"]:{");
				for(i = 31; i >= 0; i--)
				{
					uint mask = ui1 << i;
					
					if((m_buffer[bit_counter] & mask) == mask)
						sb.Append('1');
					else
						sb.Append('0');
				}
				sb.Append("}\r\n");
			}

			// Are there any further bits in the buffer?
			if((m_buffer_length & 31) > 0)
			{
				sb.Append("[" + bit_counter.ToString(s_format_provider) +"]:{");
				int iMin = (int)(32 - (m_buffer_length & 31));

				for(i = 31; i >= iMin; i--)
				{
					uint mask = ui1 << i;
					
					if((m_buffer[bit_counter] & mask) == mask)
						sb.Append('1');
					else
						sb.Append('0');
				}

				for(i = iMin - 1; i >= 0; i--)
					sb.Append('.');

				sb.Append("}\r\n");
			}

			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bit">bool형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( bool bit )
		{
			string str = "Boolean{" + (bit ? 1 : 0) + "}";
			return str;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">byte형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( byte bits )
		{
			StringBuilder sb = new StringBuilder(8);
			uint ui1 = 1;

			sb.Append("Byte{");
			for(int i = 7; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((bits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">sbyte형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( sbyte bits )
		{
			byte bytBits = (byte)bits;

			StringBuilder sb = new StringBuilder(8);
			uint ui1 = 1;

			sb.Append("SByte{");
			for(int i = 7; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((bytBits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">char형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( char bits )
		{
			StringBuilder sb = new StringBuilder(16);
			uint ui1 = 1;

			sb.Append("Char{");
			for(int i = 15; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((bits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">ushort형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( ushort bits )
		{
			short sBits = (short)bits;

			StringBuilder sb = new StringBuilder(16);
			uint ui1 = 1;

			sb.Append("ushort{");
			for(int i = 15; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((sBits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">short형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( short bits )
		{
			StringBuilder sb = new StringBuilder(16);
			uint ui1 = 1;

			sb.Append("short{");
			for(int i = 15; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((bits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">uint형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( uint bits )
		{
			StringBuilder sb = new StringBuilder(32);
			uint ui1 = 1;

			sb.Append("uint{");
			for(int i = 31; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((bits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">int형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( int bits )
		{
			uint uiBits = (uint)bits;

			StringBuilder sb = new StringBuilder(32);
			uint ui1 = 1;

			sb.Append("int{");
			for(int i = 31; i >= 0; i--)
			{
				uint mask = ui1 << i;

				if((uiBits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">ulong형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( ulong bits )
		{
			StringBuilder sb = new StringBuilder(64);
			ulong ul1 = 1;

			sb.Append("ulong{");
			for(int i = 63; i >= 0; i--)
			{
				ulong ulBitMask = ul1 << i;

				if((bits & ulBitMask) == ulBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">long형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( long bits )
		{
			ulong ulBits = (ulong)bits;

			StringBuilder sb = new StringBuilder(64);
			ulong ul1 = 1;

			sb.Append("long{");
			for(int i = 63; i >= 0; i--)
			{
				ulong ulBitMask = ul1 << i;

				if((ulBits & ulBitMask) == ulBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">float형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( float bits )
		{
			byte[] abytBits = BitConverter.GetBytes(bits);
			uint uiBits = (uint)abytBits[0] | ((uint)abytBits[1]) << 8 | ((uint)abytBits[2]) << 16 | ((uint)abytBits[3]) << 24;

			StringBuilder sb = new StringBuilder(32);
			uint ui1 = 1;

			sb.Append("float{");
			for(int i = 31; i >= 0; i--)
			{
				uint mask = ui1 << i;
				if((uiBits & mask) == mask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열로 출력
		/// </summary>
		/// <param name="bits">double형 데이터</param>
		/// <returns>문자열</returns>
		//----------------------------------------------------------------------------------------------------
		public static string ToString( double bits )
		{
			byte[] abytBits = BitConverter.GetBytes(bits);
			ulong ulBits = (ulong)abytBits[0] | ((ulong)abytBits[1]) << 8 | ((ulong)abytBits[2]) << 16 | ((ulong)abytBits[3]) << 24	| ((ulong)abytBits[4]) << 32 | ((ulong)abytBits[5]) << 40 | ((ulong)abytBits[6]) << 48 | ((ulong)abytBits[7]) << 56;

			StringBuilder sb = new StringBuilder(64);
			ulong ul1 = 1;

			sb.Append("double{");
			for(int i = 63; i >= 0; i--)
			{
				ulong ulBitMask = ul1 << i;

				if((ulBits & ulBitMask) == ulBitMask)
					sb.Append('1');
				else
					sb.Append('0');
			}
			sb.Append("}");
		
			return sb.ToString();
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 비공개 함수들
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록후 길이값 갱신
		/// </summary>
		/// <param name="bits">길이</param>
		//----------------------------------------------------------------------------------------------------
		protected void UpdateLengthForWrite( uint bits )
		{
			m_buffer_length += bits;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 기록후 인덱스값 갱신
		/// </summary>
		/// <param name="bits">인덱스</param>
		//----------------------------------------------------------------------------------------------------
		protected void UpdateIndicesForWrite(uint bits)
		{
			m_bit_index += bits;

			if(m_bit_index == c_bit_buffer_unit_size)
			{
				m_buffer_index++;
				m_bit_index = 0;
				if(m_buffer.Length == (m_buffer_length >> c_bit_buffer_unit_size_shift))
				{
					m_buffer = ReDimPreserve(m_buffer, (uint)m_buffer.Length << 1);
				}
			}
			else
			if(m_bit_index > c_bit_buffer_unit_size)
			{
				throw new InvalidOperationException("InvalidOperation_BitIndexGreaterThan32");
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 읽은후 인덱스값 갱신
		/// </summary>
		/// <param name="bits">인덱스</param>
		//----------------------------------------------------------------------------------------------------
		protected void UpdateIndicesForRead( uint bits )
		{
			m_bit_index += bits;
			if(m_bit_index == c_bit_buffer_unit_size)
			{
				m_buffer_index++;
				m_bit_index = 0;
			}
			else
			if(m_bit_index>c_bit_buffer_unit_size)	throw new InvalidOperationException("InvalidOperation_BitIndexGreaterThan32");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 확장 (기존 데이터 유지)
		/// </summary>
		/// <param name="buffer">기존버퍼</param>
		/// <param name="new_length">변경할 길이</param>
		/// <returns>새버퍼</returns>
		//----------------------------------------------------------------------------------------------------
		protected static uint[] ReDimPreserve( uint[] buffer, uint new_length )
		{
			uint[] new_buf = new uint [new_length];
			uint buf_length = (uint)buffer.Length;
			if(buf_length < new_length)
				Buffer.BlockCopy(buffer, 0, new_buf, 0, (int)(buf_length << 2));
			else // buffer.Length >= new_length
				Buffer.BlockCopy(buffer, 0, new_buf, 0, (int)(new_length << 2));
			buffer = null;
			return new_buf;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 공개 함수들
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 닫힘
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public override void Close()
		{
			m_open = false;
			// Reset indices
			m_buffer_index = 0;
			m_bit_index = 0;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 버퍼 얻기
		/// </summary>
		/// <returns>버퍼</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual uint[] GetBuffer()
		{
			return m_buffer;
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 복제
		/// </summary>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public virtual cBitStream Copy()
		{
			cBitStream bstrmCopy = new cBitStream(this.Length);
			Buffer.BlockCopy(m_buffer, 0, bstrmCopy.m_buffer, 0, bstrmCopy.m_buffer.Length << 2);
			bstrmCopy.m_buffer_length = this.m_buffer_length;
			return bstrmCopy;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 지원하지 않는 함수들
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비동기 읽기 시작
		/// </summary>
		/// <param name="buffer">버퍼</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <param name="callback">콜백</param>
		/// <param name="state">사용자변수</param>
		/// <returns>비동기 결과</returns>
		//----------------------------------------------------------------------------------------------------
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("NotSupported_BeginRead");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비동기 쓰기 시작
		/// </summary>
		/// <param name="buffer">버퍼</param>
		/// <param name="offset">오프셋</param>
		/// <param name="count">개수</param>
		/// <param name="callback">콜백</param>
		/// <param name="state">사용자변수</param>
		/// <returns>비동기 결과</returns>
		//----------------------------------------------------------------------------------------------------
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException("NotSupported_BeginWrite");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비동기 읽기 종료
		/// </summary>
		/// <param name="asyncResult">비동기 결과</param>
		/// <returns>읽은 바이트수</returns>
		//----------------------------------------------------------------------------------------------------
		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("NotSupported_EndRead");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비동기 쓰기 종료
		/// </summary>
		/// <param name="asyncResult">비동기 결과</param>
		/// <returns>읽은 바이트수</returns>
		//----------------------------------------------------------------------------------------------------
		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException("NotSupported_EndWrite");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 포지션 이동
		/// </summary>
		/// <param name="offset">오프셋</param>
		/// <param name="origin">시작위치</param>
		/// <returns>포지션</returns>
		//----------------------------------------------------------------------------------------------------
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException("NotSupported_Seek");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 길이 변경
		/// </summary>
		/// <param name="value">길이값</param>
		//----------------------------------------------------------------------------------------------------
		public override void SetLength(long value)
		{
			throw new NotSupportedException("NotSupported_SetLength");
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 플러시
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public override void Flush()
		{
			throw new NotSupportedException("NotSupported_Flush");
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region Implicit Operators
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">메모리스트림</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator cBitStream(MemoryStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new cBitStream((Stream)bits);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">비트스트림</param>
		/// <returns>메모리스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator MemoryStream(cBitStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new MemoryStream(bits.ToByteArray());
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">파일스트림</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator cBitStream(FileStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new cBitStream((Stream)bits);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">버퍼스트림</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator cBitStream(BufferedStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new cBitStream((Stream)bits);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">비트스트림</param>
		/// <returns>버퍼스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator BufferedStream(cBitStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new BufferedStream((MemoryStream)bits);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">네트워크스트림</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator cBitStream(NetworkStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new cBitStream((Stream)bits);
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스트림 입력
		/// </summary>
		/// <param name="bits">암호화스트림</param>
		/// <returns>비트스트림</returns>
		//----------------------------------------------------------------------------------------------------
		public static implicit operator cBitStream(CryptoStream bits)
		{
			if(bits==null)	throw new Exception("bits=null");
			return new cBitStream((Stream)bits);
		}
		#endregion

	}
}

//----------------------------------------------------------------------------------------------------
// cClient
// : 클라이언트
//  -JHL-2012-02-09
//----------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BlueEngine
{
    //----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 네트워크 수신 이벤트
	/// </summary>
	/// <param name="client">cClient 인스턴스</param>
	/// <param name="data">수신된 데이터 바이트 배열.</param>
	/// <param name="size">수신된 데니터 크기.</param>
    //----------------------------------------------------------------------------------------------------
    public delegate void EventRecv( cClient client, byte[] data, int size );


	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 네트워크 클라이언트 객체.
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cClient : cNetwork
    {
        //----------------------------------------------------------------------------------------------------
 		#region 상수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 클라이언트 아이디
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		const	uint		MAX_CLIENT_ID		= uint.MaxValue;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 누적된 수신 버퍼 크기
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		const	ushort		MAX_RECV_BUFFER		= 1024;
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 변수
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 이벤트
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private		event	EventRecv						EventRecv;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// TCP 클라이언트 객체
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected			TcpClient						m_client;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				uint							m_client_id;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 데이터 버퍼
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected			byte[]							m_recv_buffer;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 데이터 누적 버퍼
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				byte[]							m_total_recv_buffer;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 데이터 누적 버퍼 포지션
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				ushort							m_total_recv_buffer_pos;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 데이터 헤드
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				ushort							m_recv_head;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				byte							m_channel;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				uint							m_party;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티장 플래그
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				bool							m_master;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected			uint							m_stage;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 유저 계정
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				cUserAccount					m_user_account;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 유저 캐릭터
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				cUserCharacter					m_user_character;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 동기화할 cObject 리스트(미사용)
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        private				Dictionary<uint,cObject>		m_sync_cobject;
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 속성
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 네트워크 주소
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public		string					Address				{get{return((IPEndPoint)m_client.Client.RemoteEndPoint).Address.ToString();}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 유저 계정
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		cUserAccount			UserAccount			{get{return m_user_account;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 유저 캐릭터
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		cUserCharacter			UserCharacter		{get{return m_user_character;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 접속중 플래그
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		bool					Connected			{get{return (m_client==null)?false:m_client.Connected;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		uint					ClientID			{get{return m_client_id;}set{m_client_id=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 계정 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		ulong					AccountID			{get{return (ulong)UserAccount.Key;}set{UserAccount.Key=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		byte					Channel				{get{return m_channel;}set{m_channel=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		uint					Party				{get{return m_party;}set{m_party=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		uint					Stage				{get{return m_stage;}set{m_stage=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티장 플래그
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		bool					Master				{get{return m_master;}set{m_master=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 캐릭터 아이디
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		ulong					CharID				{get{return (ulong)UserCharacter.char_id.Value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 캐릭터 이름
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		string					CharName			{get{return (string)UserCharacter.name.Value;}set{UserCharacter.name.Value=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 캐릭터 클래스
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		uint					CharItemInfoId		{get{return (uint)UserCharacter.item_info_id.Value;}set{UserCharacter.item_info_id.Value=value;}}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 장착 아이템
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public		cDBField[]				Equip				{get{return UserCharacter.equip;}set{UserCharacter.equip=value;}}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region cClient() : 생성자
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public cClient():this(cClient.NULL_ID,MAX_RECV_BUFFER,true,null)
        {
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="client_id">클라이언트 아이디</param>
		/// <param name="recv_buf_size">수신버퍼 크기</param>
		/// <param name="use_cryptogram">패킷암호화 사용 유무</param>
		/// <param name="event_recv">수신 이벤트 콜백 함수</param>
        //----------------------------------------------------------------------------------------------------
        public cClient( uint client_id, ushort recv_buf_size, bool use_cryptogram, EventRecv event_recv ):base("cClient","CT")
        {
			UseCryptogram			= use_cryptogram;
            RecvBufSize				= recv_buf_size;
			m_client_id				= client_id;
			m_user_account			= new cUserAccount();
			m_user_character		= new cUserCharacter();
            m_client				= null;
            m_recv_buffer			= new byte[recv_buf_size];
			m_total_recv_buffer		= new byte[MAX_RECV_BUFFER];
			m_total_recv_buffer_pos = 0;
			m_recv_head				= 0;
			EventRecv				= event_recv;
			//EventRecv				+= event_recv;
			Channel					= (byte)cChannel.NULL_ID;
			Party					= (uint)cParty.NULL_ID;
			Stage					= (uint)cStage.NULL_ID;
			Master					= false;
			m_sync_cobject			= new Dictionary<uint,cObject>();
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="client_id">클라이언트 아이디</param>
		/// <param name="client">TcpClient 인스턴스</param>
		/// <param name="recv_buf_size">수신 버퍼 크기</param>
		/// <param name="use_cryptogram">패킷암호화 사용 유무</param>
		/// <param name="event_recv">수신 이벤트 콜백 함수</param>
        //----------------------------------------------------------------------------------------------------
        public cClient( uint client_id, TcpClient client, ushort recv_buf_size, bool use_cryptogram, EventRecv event_recv ):this(client_id,recv_buf_size,use_cryptogram,event_recv)
        {
			Connect( client );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="client_id">클라이언트 아이디</param>
		/// <param name="address">서버 주소</param>
		/// <param name="port">서버 포트번호</param>
		/// <param name="recv_buf_size">수신 버퍼 크기</param>
		/// <param name="use_cryptogram">패킷암호화 사용 유무</param>
		/// <param name="event_recv">수신 이벤트 콜백 함수</param>
        //----------------------------------------------------------------------------------------------------
        public cClient( uint client_id, string address, ushort port, ushort recv_buf_size, bool use_cryptogram, EventRecv event_recv ):this(client_id,new TcpClient(address,port),recv_buf_size,use_cryptogram,event_recv)
        {
        }

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파괴자
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		~cClient()
		{
			Disconnect();
		}

		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region Connect() : 소켓연결
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버에 접속.
		/// </summary>
		/// <param name="address">서버 주소</param>
		/// <param name="port">서버 포트번호</param>
		/// <returns></returns>
        //----------------------------------------------------------------------------------------------------
		public bool Connect( string address, ushort port )
		{
			return Connect( new TcpClient( address, port ) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버에 접속.
		/// </summary>
		/// <param name="client">TcpClient 인스턴스</param>
		/// <returns>결과</returns>
        //----------------------------------------------------------------------------------------------------
		public bool Connect( TcpClient client )
		{
            m_client        = client;
		    m_port			= (ushort)((IPEndPoint)m_client.Client.LocalEndPoint).Port;
            // 읽기모드
            m_client.GetStream().BeginRead( m_recv_buffer, 0, RecvBufSize, new AsyncCallback(DoRecv), null );
			return true;
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 네트워크 접속 종료.
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public void Disconnect()
		{
			if( m_client != null )
			{
				if( m_client.Connected )
				{
					if( Parent != null )
					{
						((cServer)Parent).DisconnectClient(this);
					}
					m_client.Close();
				}
				m_client = null;
				ClientID = cClient.NULL_ID;

				Print( "CLOSE" );
			}
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region Exception() : 예외
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	예외처리를 위한 예외를 발생 시킨다.
		/// </summary>
		/// <returns>예외처리 객체.</returns>
        //----------------------------------------------------------------------------------------------------
        public override Exception Exception()
        {
            return Exception("");
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	예외처리를 위한 예외를 발생 시킨다.
		/// </summary>
		/// <param name="values">데이터리스트.</param>
		/// <returns>예외처리 객체.</returns>
        //----------------------------------------------------------------------------------------------------
        public override Exception Exception( params object[] values )
        {
            return new Exception( cLog.LogToString( 2, Thread.CurrentThread.ManagedThreadId.ToString("d05") + "|" + ShortName + "|" + ClientID + " > " + cObject.ValueToString(values) ) );
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region Log() : 로그
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 메시지 로그를 기록한다.
		/// </summary>
		/// <param name="values">데이터리스트.</param>
        //----------------------------------------------------------------------------------------------------
        public override void Log( params object[] values )
        {
            cLog.Log( Thread.CurrentThread.ManagedThreadId.ToString("d05") + "|" + ShortName + "|" + ClientID + " > " + cObject.ValueToString(values) );
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region Print() : 메시지 출력
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 메시지를 콘솔창에 출력 한다.
		/// </summary>
		/// <param name="values">데이터리스트.</param>
        //----------------------------------------------------------------------------------------------------
        public override void Print( params object[] values )
        {
            Console.Write( Thread.CurrentThread.ManagedThreadId.ToString("d05") + "|" + ShortName + "|" + ClientID + " > " + cObject.ValueToString(values) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 에러메시지를 콘솔창에 출력 한다.
		/// </summary>
		/// <param name="values">데이터리스트.</param>
        //----------------------------------------------------------------------------------------------------
        public override void Error( params object[] values )
        {
            Console.WriteColor( Thread.CurrentThread.ManagedThreadId.ToString("d05") + "|" + ShortName + "|" + ClientID + " > " + cObject.ValueToString(values), ConsoleColor.Red, ConsoleColor.Black );
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region Send() : 데이터를 전송한다.
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 송신.
		/// </summary>
		/// <param name="order">명령코드</param>
		/// <param name="result">결과코드</param>
        //----------------------------------------------------------------------------------------------------
        public void Send( eOrder order, eResult result )
        {
			cBitStream bits = new cBitStream();
			WriteOrder( bits, order );
			WriteResult( bits, result );
			Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 송신.
		/// </summary>
		/// <param name="bits">비트스트림 데이터</param>
        //----------------------------------------------------------------------------------------------------
        public void Send( cBitStream bits )
        {
			if( Connected==false ) return;

			if( bits.Length>0 )
			{
				try
				{
					NetworkStream stream = m_client.GetStream();
					if( stream == null ) return;
					lock( stream )
					{
						BinaryWriter writer = new BinaryWriter( stream );
						if( UseCryptogram )
						{
							byte[] buf = cCryptogram.Encrypt( bits.ToByteArray(), DataIV, DataKey );
							writer.Write( (ushort)buf.Length );
							writer.Write( buf );
						}
						else
						{
							byte[] buf = bits.ToByteArray();
							writer.Write( (ushort)buf.Length );
							writer.Write( buf );
						}
						writer.Flush();
					}
				}
				catch ( Exception ex )
				{
					Log( m_name+">>"+ex );
					throw new Exception("error:Send");
				}
            }
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 송신.
		/// </summary>
		/// <param name="data">바이트 데이터</param>
        //----------------------------------------------------------------------------------------------------
		public void Send( byte[] data )
        {
			if( Connected==false ) return;

			try
			{
				NetworkStream stream = m_client.GetStream();
				if( stream == null ) return;
				lock( stream )
				{
					BinaryWriter writer = new BinaryWriter( stream );
					writer.Write( (ushort)data.Length );
					if( UseCryptogram )	writer.Write( cCryptogram.Encrypt( data, DataIV, DataKey ) );
					else				writer.Write( data );
					writer.Flush();
				}
            }
			catch ( Exception ex )
			{
				Log( m_name+">>"+ex );
				throw new Exception("error:Send");
			}
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 송신.
		/// </summary>
		/// <param name="data">문자열 데이터</param>
        //----------------------------------------------------------------------------------------------------
        public void Send( string data )
        {
			if( Connected==false ) return;

			try
			{
				NetworkStream stream = m_client.GetStream();
				if( stream == null ) return;
				lock( stream )
				{
					StreamWriter writer = new StreamWriter( stream );
					if( UseCryptogram )	writer.Write( cCryptogram.EncryptString( data, DataIV, DataKey ) );
					else				writer.Write( data );
					writer.Flush();
				}
            }
			catch ( Exception ex )
			{
				Log( m_name+">>"+ex );
				throw new Exception("error:Send");
			}
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region DoRecv() : 스트림 데이터를 받는다.
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 수신 처리.
		/// </summary>
		/// <param name="result"></param>
        //----------------------------------------------------------------------------------------------------
        protected virtual void DoRecv( IAsyncResult result )
        {
            ushort	recv_bytes		= 0;	// 수신된 바이트 수
			ushort	recv_buffer_pos = 0;	// 수신된 버퍼 읽는 위치

            try 
            {
				NetworkStream stream = m_client.GetStream();
				if( stream == null )
				{
					Disconnect();
					return;
				}

				lock( stream )
				{
					recv_bytes = (ushort)stream.EndRead(result);
					Log( "READ_BYTES : " + recv_bytes + " : " +Address );
				}

				if( recv_bytes==0 )
				{
					Disconnect();
					return;
				}

				while( recv_bytes>0 )
				{
					// 헤드 읽기
					if( m_recv_head==0 )
					{
						m_total_recv_buffer[m_total_recv_buffer_pos] = m_recv_buffer[recv_buffer_pos];
						--recv_bytes;
						++recv_buffer_pos;
						++m_total_recv_buffer_pos;

						// 헤드를 다 읽음
						if( m_total_recv_buffer_pos == 2 )
						{
							m_recv_head = BitConverter.ToUInt16(m_total_recv_buffer,0);
							m_total_recv_buffer_pos = 0;
						}
					}
					// 바디 읽기
					else
					{
						// 읽어온 데이터 복사
						ushort copy_bytes = (ushort)(m_recv_head-m_total_recv_buffer_pos);
						if( copy_bytes > recv_bytes ) copy_bytes = recv_bytes;

						Buffer.BlockCopy( m_recv_buffer, recv_buffer_pos, m_total_recv_buffer, m_total_recv_buffer_pos, copy_bytes );
						recv_bytes				-= copy_bytes;
						recv_buffer_pos			+= copy_bytes;
						m_total_recv_buffer_pos += copy_bytes;

						// 데이터 모두 읽음
						if( m_recv_head == m_total_recv_buffer_pos )
						{
							// 데이터를 파싱한다.
							EventRecv( this, m_total_recv_buffer, m_recv_head );

							m_recv_head				= 0;
							m_total_recv_buffer_pos = 0;
						}
					}
				}

				// 다음 읽기버퍼를 준비한다.
				lock( stream )
				{
					// stream.DataAvailable를 체그해서 수신된 데이터가 남아 있을 경우 모두 읽어 바로 버퍼를 비울수 있지만
					// 순차적인 처리를 위해 생략 한다.
					//while(stream.DataAvailable)
					{
						stream.BeginRead( m_recv_buffer, 0, RecvBufSize, new AsyncCallback(DoRecv), null );
					}
				}
            } 
            catch( Exception ex )
            {
				if( ex.Message != "error:Send" )
				{
					Log( m_name+">>"+ex );
					Disconnect();
				}
            }
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region OnRecv() : 받은 데이터를 처리한다.
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신된 데이터를 파싱한다.
		/// 주의 : 이 함수는 상속 받아서 클라이언트마다 각자 프로토콜을 정의해서 사용한다.
		/// </summary>
		/// <param name="client">수신 클라이언트</param>
		/// <param name="data">수신 데이터</param>
		/// <param name="size">수신 데이터 크기</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void OnRecv( cClient client, byte[] data, int size )
		{
			cBitStream bits = new cBitStream();
			bits.Write( data, 0, size );

			Print( bits.ToString() );

			eOrder	order	= ReadOrder( bits );
			eResult	result	= ReadResult( bits ); 

            Print( order + " : '"+client.Name+"'" );

            // 파싱
            switch( order )
            {
            case eOrder.OBJECT_UPDATE:
                RecvObjectUpdate( client, result, bits );
                break;
            default:
                Error( client.Name + ":" + order );
                break;
            }
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region RecvObjectUpdate() : 오브젝트를 갱신한다. (미사용)
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 동기화 오브젝트를 파싱.
		/// </summary>
		/// <param name="client">수신 클라이언트</param>
		/// <param name="result">결과값</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		public void RecvObjectUpdate( cClient client, eResult result, cBitStream bits )
		{
			bool host;
			bits.Read( out host );
			bool cobject;
			bits.Read( out cobject );
			uint object_id;
			bits.Read( out object_id, uint.MaxValue );

			if( cobject )
			{
				cObject value;
				if( m_sync_cobject.TryGetValue( object_id, out value ) )
				{
					Type ctype = value.GetType();

					foreach( FieldInfo field in ctype.GetFields(BindingFlags.NonPublic|BindingFlags.Public) )
					{
						if( host )
						{
							HOSTAttribute attrib=GetHostAttribute( field );
							if( attrib != null )
							{
								object data = field.GetValue(value);
								bits.Read( field.GetType(), out data, data, attrib.MaxSize, attrib.MaxPoint );
							}
						}
						else
						{
							GUESTAttribute attrib=GetGuestAttribute( field );
							if( attrib != null )
							{
								object data = field.GetValue(value);
								bits.Read( field.GetType(), out data, data, attrib.MaxSize, attrib.MaxPoint );
							}
						}
					}
				}
			}
			else
			{
				cUnityObject value;
				if( cUnityObject.GetInstance( object_id, out value ) )
				{
					Type ctype = value.GetType();

					foreach( FieldInfo field in ctype.GetFields(BindingFlags.NonPublic|BindingFlags.Public) )
					{
						if( host )
						{
							HOSTAttribute attrib=GetHostAttribute( field );
							if( attrib != null )
							{
								object data = field.GetValue(value);
								bits.Read( field.GetType(), out data, data, attrib.MaxSize, attrib.MaxPoint );
							}
						}
						else
						{
							GUESTAttribute attrib=GetGuestAttribute( field );
							if( attrib != null )
							{
								object data = field.GetValue(value);
								bits.Read( field.GetType(), out data, data, attrib.MaxSize, attrib.MaxPoint );
							}
						}
					}
				}
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
 		#region UpdateObject() : 업데이트 오브젝트 (미사용)
		///----------------------------------------------------------------------------------------------------
		/// <summary>
		///		Unity오브젝트를 갱신한다..(미완성)
		/// </summary>
		///----------------------------------------------------------------------------------------------------
		public void UpdateUnityObject()
		{
			cBitStream bits = new cBitStream();

			WriteOrder( bits, eOrder.OBJECT_UPDATE );
			WriteResult( bits, eResult.SUCCESS );
			bits.Write( m_master );
			bits.Write( false );

			lock( cUnityObject.Instances )
			{
				foreach( KeyValuePair<uint,cUnityObject> entry in cUnityObject.Instances )
				{
					cUnityObject value = entry.Value;
					Type ctype = value.GetType();
					NETCLASSAttribute cattribute = GetNetClassAttribute( ctype );
					if( cattribute != null )
					{
						if( cattribute.IsSync( UnityEngine.Time.time )==false ) return;
					}

					FieldInfo finfo = ctype.GetField("m_id",BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);
					uint object_id = (uint)finfo.GetValue(value);
					bits.Write( object_id, MAX_OBJECT_ID );

					foreach( FieldInfo field in ctype.GetFields(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance) )
					{
						if( m_master )
						{
							HOSTAttribute attrib=GetHostAttribute( field );
							if( attrib != null )
							{
								bits.Write( field.GetValue(value), attrib.MaxSize, attrib.MaxPoint );
							}
						}
						else
						{
							GUESTAttribute attrib=GetGuestAttribute( field );
							if( attrib != null )
							{
								bits.Write( field.GetValue(value), attrib.MaxSize, attrib.MaxPoint );
							}
						}
					}
				}
			}

			if( bits.Length > 0 )
			{
				Send( bits );
			}
		}
        ///----------------------------------------------------------------------------------------------------
		/// <summary>
		///		cObject를 갱신한다. (미완성)
		/// </summary>
		///----------------------------------------------------------------------------------------------------
		public void UpdateObject()
		{
			/*
			cBitStream bits = new cBitStream();

			WriteOrder( bits, eOrder.OBJECT_UPDATE );
			WriteResult( bits, eResult.SUCCESS );
			bits.Write( m_host );
			bits.Write( true );

			lock( cObject.Instances )
			{
				foreach( KeyValuePair<uint,cObject> entry in cObject.Instances )
				{
					cObject value = entry.Value;
					Type ctype = value.GetType();
					NETCLASSAttribute cattribute = GetNetClassAttribute( ctype );
					if( cattribute != null )
					{
						if( cattribute.IsSync( UnityEngine.Time.time )==false ) return;
					}

					FieldInfo finfo = ctype.GetField("m_id",BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance);
					uint object_id = (uint)finfo.GetValue(value);
					bits.Write( object_id, MAX_OBJECT_ID );

					foreach( FieldInfo field in ctype.GetFields(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance) )
					{
						if( m_host )
						{
							HOSTAttribute attrib=GetHostAttribute( field );
							if( attrib != null )
							{
								bits.Write( field.GetValue(value), attrib.MaxSize, attrib.MaxPoint );
							}
						}
						else
						{
							GUESTAttribute attrib=GetGuestAttribute( field );
							if( attrib != null )
							{
								bits.Write( field.GetValue(value), attrib.MaxSize, attrib.MaxPoint );
							}
						}
					}
				}
			}

			if( bits.Length > 0 )
			{
				Send( bits );
			}
			*/
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
 		#region 특성
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 네트워크 클래스 특성값 얻기
		/// </summary>
		/// <param name="member">멤버</param>
		/// <returns></returns>
        //----------------------------------------------------------------------------------------------------
		public static NETCLASSAttribute GetNetClassAttribute( MemberInfo member )
		{
			foreach( object attribute in member.GetCustomAttributes(true) )
			{
				if( attribute is NETCLASSAttribute )
				{
					return (NETCLASSAttribute)attribute;
				}
			}
			return null;
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 네트워크 특성값 얻기
		/// </summary>
		/// <param name="member">멤버</param>
		/// <returns></returns>
        //----------------------------------------------------------------------------------------------------
		public static NETAttribute GetNetAttribute( MemberInfo member )
		{
			foreach( object attribute in member.GetCustomAttributes(true) )
			{
				if( attribute is NETAttribute )
				{
					return (NETAttribute)attribute;
				}
			}
			return null;
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 호스트 특성값 얻기
		/// </summary>
		/// <param name="member">멤버</param>
		/// <returns></returns>
        //----------------------------------------------------------------------------------------------------
		public static HOSTAttribute GetHostAttribute( MemberInfo member )
		{
			foreach( object attribute in member.GetCustomAttributes(true) )
			{
				if( attribute is HOSTAttribute )
				{
					return (HOSTAttribute)attribute;
				}
			}
			return null;
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 게스트 특성값 얻기
		/// </summary>
		/// <param name="member">멤버</param>
		/// <returns></returns>
        //----------------------------------------------------------------------------------------------------
		public static GUESTAttribute GetGuestAttribute( MemberInfo member )
		{
			foreach( object attribute in member.GetCustomAttributes(true) )
			{
				if( attribute is GUESTAttribute )
				{
					return (GUESTAttribute)attribute;
				}
			}
			return null;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
 		#region 서버 요청/보고
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 서버 : 로그인
		/// </summary>
		/// <param name="client_version">클라이언트 버전</param>
		/// <param name="member_id">회원 아이디</param>
		/// <param name="member_pw">회원 비밀번호</param>
        //----------------------------------------------------------------------------------------------------
        public void SendServerLogin( string client_version, string member_id, string member_pw )
        {
			cBitStream bits = new cBitStream();
			WriteOrder( bits, eOrder.SERVER_LOGIN );
			WriteString( bits, VERSION );
			WriteString( bits, client_version );
			WriteString( bits, member_id );
			WriteString( bits, member_pw );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 서버 : 입장
		/// </summary>
		/// <param name="client_version">클라이언트 버전</param>
        //----------------------------------------------------------------------------------------------------
        public void SendServerIn( string client_version )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.SERVER_IN );
			WriteString( bits, VERSION );
			WriteString( bits, client_version );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 서버 : 퇴장
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void SendServerOut()
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.SERVER_OUT	);
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 클라이언트 : 정보 : 기본
		/// </summary>
		/// <param name="client_id">클라이언트 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendClientInfoDefault( uint client_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.CLIENT_INFO_DEFAULT );
			WriteClientId( bits, client_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 채널 : 리스트
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void SendChannelList()
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.CHANNEL_LIST );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 채널 : 입장
		/// </summary>
		/// <param name="channel_id">채널ID.(NULL_ID이면 자동지정)</param>
        //----------------------------------------------------------------------------------------------------
        public void SendChannelIn( byte channel_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.CHANNEL_IN );
			WriteChannelId(	bits, channel_id );
			WriteString( bits, CharName );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 채널 : 퇴장
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void SendChannelOut()
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.CHANNEL_OUT );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 채널 : 채팅
		/// </summary>
		/// <param name="message">메시지</param>
        //----------------------------------------------------------------------------------------------------
        public void SendChannelChat( string message )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.CHANNEL_CHAT );
			WriteString(	bits, message );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 파티 : 채팅
		/// </summary>
		/// <param name="message">메시지</param>
        //----------------------------------------------------------------------------------------------------
        public void SendPartyChat( string message )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.PARTY_CHAT );
			WriteString( bits, message );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 리스트
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void SendStageList()
        {
			cBitStream bits = new cBitStream();
			WriteOrder(	bits, eOrder.STAGE_LIST );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 입장 : 요청
		/// </summary>
		/// <param name="stage_id">스테이지 아이디</param>
		/// <param name="max_user">최대 파티원 수</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageInRequest( uint stage_id, byte max_user )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(				bits, eOrder.STAGE_IN_REQUEST );
			WriteStageId(			bits, stage_id );
			WritePartyUserCount(	bits, max_user );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 입장
		/// </summary>
		/// <param name="equip_items">장착 아이템 리스트(임시)</param>
		/// <param name="stage_pos">스테이지 좌표</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserIn( uint[] equip_items, cVector3 stage_pos )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(			bits, eOrder.STAGE_USER_IN );
			WriteItemInfoIds(	bits, equip_items );
			WriteStagePos(		bits, stage_pos );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 퇴장
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserOut()
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_OUT );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 이동
		/// </summary>
		/// <param name="stage_pos">스테이지 좌표</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserMove( cVector3 stage_pos )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_MOVE );
			WriteStagePos(	bits, stage_pos );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 공격 : 몬스터
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserAttackMonster( ushort monster_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_ATTACK_MONSTER );
			WriteMonsterId(	bits, monster_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 스킬 사용 : 자신
		/// </summary>
		/// <param name="skill_id">스킬 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserSkillSelf( ushort skill_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_SKILL_SELF );
			WriteSkillId(	bits, skill_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 스킬 사용 : 몬스터
		/// </summary>
		/// <param name="skill_id">스킬 아이디</param>
		/// <param name="monster_ids">타겟 몬스터 아이디 리스트</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserSkillMonster( ushort skill_id, ushort[] monster_ids )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(				bits, eOrder.STAGE_USER_SKILL_MONSTER );
			WriteSkillId(			bits, skill_id );
			WriteSkillTargetCount(	bits, (byte)monster_ids.Length );
			foreach( ushort monster_id in monster_ids )
			{
				WriteMonsterId(		bits, monster_id );
			}
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 스킬 사용 : 좌표
		/// </summary>
		/// <param name="skill_id">스킬 아이디</param>
		/// <param name="stage_pos">스테이지 좌표</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserSkillPos( ushort skill_id, cVector3 stage_pos )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_SKILL_POS );
			WriteSkillId(	bits, skill_id );
			WriteStagePos(	bits, stage_pos );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 데미지
		/// </summary>
		/// <param name="damage">데미지 값</param>
		/// <param name="death">죽음 유무 플래그</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserDemage( uint damage, bool death )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_DAMAGE );
			WriteDamage(	bits, damage );
			WriteFlag(		bits, death );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 아이템 사용 : 자신
		/// </summary>
		/// <param name="item_id">아이템 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserItemUseSelf( ulong	item_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_ITEM_USE_SELF );
			WriteItemId(	bits, item_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 유저 : 트리거 작동
		/// </summary>
		/// <param name="trigger_id">트리거 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageUserTriggerOn( ushort trigger_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_USER_TRIGGER_ON );
			WriteTriggerId(	bits, trigger_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 몬스터 : 이동 : (파티장 권한 필요)
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
		/// <param name="stage_pos">스테이지 좌표</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageMonMove( ushort monster_id, cVector3 stage_pos )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_MON_MOVE );
			WriteMonsterId(	bits, monster_id );
			WriteStagePos(	bits, stage_pos );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 몬스터 : 공격 : 유저 : (파티장 권한 필요)
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
		/// <param name="client_id">클라이언트 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageMonAttackUser( ushort monster_id, uint client_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_MON_ATTACK_USER );
			WriteMonsterId(	bits, monster_id );
			WriteClientId(	bits, client_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 몬스터 : 스킬 사용 : 자신 : (파티장 권한 필요)
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
		/// <param name="skill_id">스킬 아이디</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageMonSkillSelf( ushort monster_id, ushort skill_id )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_MON_SKILL_SELF );
			WriteMonsterId(	bits, monster_id );
			WriteSkillId(	bits, skill_id );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 몬스터 : 스킬 사용 : 유저 : (파티장 권한 필요)
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
		/// <param name="skill_id">스킬 아이디</param>
		/// <param name="client_ids">타겟 클라이언트 아이디 리스트</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageMonSkillUser( ushort monster_id, ushort skill_id, uint[] client_ids )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_MON_SKILL_SELF );
			WriteMonsterId(	bits, monster_id );
			WriteSkillId(	bits, skill_id );
			WriteSkillTargetCount(	bits, (byte)client_ids.Length );
			foreach( uint client_id in client_ids )
			{
				WriteClientId(		bits, client_id );
			}
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 몬스터 : 스킬 사용 : 좌표 : (파티장 권한 필요)
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
		/// <param name="skill_id">스킬 아이디</param>
		/// <param name="stage_pos">스테이지 좌표</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageMonSkillPos( ushort monster_id, ushort skill_id, cVector3 stage_pos )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_MON_SKILL_SELF );
			WriteMonsterId(	bits, monster_id );
			WriteSkillId(	bits, skill_id );
			WriteStagePos(	bits, stage_pos );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 몬스터 : 데미지 : (파티장 권한 필요)
		/// </summary>
		/// <param name="monster_id">몬스터 아이디</param>
		/// <param name="damage">데미지 값</param>
		/// <param name="death">죽음 유무 플래그</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageMonDamage( ushort monster_id, uint damage, bool death )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_MON_SKILL_SELF );
			WriteMonsterId(	bits, monster_id );
			WriteDamage(	bits, damage );
			WriteFlag(		bits, death );
            Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 송신 : 스테이지 : 커스텀 데이터
		/// </summary>
		/// <param name="in_bits">커스텀 데이터</param>
        //----------------------------------------------------------------------------------------------------
        public void SendStageData( cBitStream in_bits )
        {
			cBitStream bits = new cBitStream();
			WriteOrder(		bits, eOrder.STAGE_DATA );
			bits.Write( in_bits.ToByteArray() );
            Send( bits );
        }
		#endregion
	}
}
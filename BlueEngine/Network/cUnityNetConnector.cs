//----------------------------------------------------------------------------------------------------
// cUnityNetConnector
// : 유니티전용 네트워크 커넥터
//  -JHL-2012-02-23
//----------------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 유니티전용 네트워크 커넥터
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	[AddComponentMenu("@BlueEngine/Network/Network Connector")]
    public class cUnityNetConnector : MonoBehaviour
	{
		//----------------------------------------------------------------------------------------------------
		#region 변수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 초기화 플래그.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	bool						s_init				= false;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 버전
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		static string						s_version;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 버전
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		string								m_version;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 루프 쓰레드.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	Thread						s_thread_connect;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 뮤텍스. 
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		//Mutex는 WebPlayer에서 오류를 리턴하여 만들어 사용한다.
		//protected	static	Mutex						s_mutex; 
		protected	static	cMutex						s_mutex;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 객체.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	cClient						s_client;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 주소.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	string						s_server_address;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 주소(입력용).
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				string						m_server_address;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 주소 목록.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				string[]					m_server_addresses;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 포트.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	ushort						s_server_port;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 포트(입력용).
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				int							m_server_port;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 포트 목록.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				ushort[]					m_server_ports;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 버퍼 크기.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	ushort						s_recv_buf_size;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 버퍼 크기(입력용).
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				int							m_recv_buf_size;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// Polity 서버 포트.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	ushort						s_policy_port;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 패킷 암호화 사용 플래그.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	bool						s_use_cryptogram;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 패킷 암호화 사용 플래그(입력용).
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				bool						m_use_cryptogram;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 암호화 초기화 벡터.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	string						s_cryptogram_iv;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 암호화 초기화 벡터.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				string						m_cryptogram_iv;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 암호화 비밀키.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static	string						s_cryptogram_key;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 암호화 비밀키.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				string						m_cryptogram_key;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 리스트(key:클라이언트ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        protected	static	Dictionary<uint,cClient>	s_clients;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 리스트(key:채널ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        private		static	Dictionary<byte,cChannel>	s_channels;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지별 파티 개수(key:스테이지ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        private		static	Dictionary<uint,ushort>		s_stage_party_count;
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 속성
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 객체.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		static	cClient						Client			{get{return s_client;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 리스트(key:클라이언트ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				Dictionary<uint,cClient>	Clients			{get{return s_clients;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 리스트(key:채널ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				Dictionary<byte,cChannel>	Channels		{get{return s_channels;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지별 파티 개수(key:스테이지ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public				Dictionary<uint,ushort>		StagePartyCount	{get{return s_stage_party_count;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 접속중 플래그
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		static	bool						Connected		{get{return (s_client==null)?false:s_client.Connected;}}
		#endregion

        //----------------------------------------------------------------------------------------------------
		#region 접속
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 접속 쓰레드
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		void ThreadConnect()
		{
			Print("ThreadConnect");
			/*
			try
			{
				if( !cPolicyClient.RequestPolicyWithUnity( s_server_address, s_policy_port ) )
				{
					throw Exception("Policy서버 접속 실패.");
				}
			}
			catch( Exception ex )
			{
				if( (ex is ThreadAbortException)==false )
				{
					Error( ex.ToString() );
					OnConnectError( cNetwork.eResult.FAIL_CLIENT_CONNECT_SERVER_POLICY, ex.Message );
					return;
				}
			}
			*/

			try
			{
				if( ! s_client.Connect( s_server_address, s_server_port ) )
				{
					throw Exception("게임서버 접속 실패.");
				}
			}
			catch( Exception ex )
			{
				if( (ex is ThreadAbortException)==false )
				{
					Error( ex.ToString() );
					OnConnectError( cNetwork.eResult.FAIL_CLIENT_CONNECT_SERVER_GAME, ex.Message );
					return;
				}
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최초 초기화 할때 호출됨
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected virtual void Reset()
		{
			m_version			= "0.1.0";
			m_recv_buf_size		= 1024;
			m_server_address	= "localhost";
			m_server_port		= 12345;
			m_use_cryptogram	= true;
			m_cryptogram_iv		= "1234567890abcdef";
			m_cryptogram_key	= "1234567890abcdef1234567890abcdef";
			m_server_addresses	= new string[3]{"grashs.iptime.org","192.168.0.30","192.168.0.18"};
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최초 활성화될때 호출됨
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected virtual void Awake()
		{
			if( s_init==false )
			{
				s_init					= true;
				s_version				= m_version;
				s_server_address		= m_server_address;
				s_server_port			= (ushort)m_server_port;
				s_recv_buf_size			= (ushort)m_recv_buf_size;
				s_use_cryptogram		= m_use_cryptogram;
				s_cryptogram_iv			= m_cryptogram_iv;
				s_cryptogram_key		= m_cryptogram_key;
				cNetwork.SetCryptogram( m_cryptogram_iv, m_cryptogram_key );

				s_policy_port			= 843;
				//s_mutex					= new Mutex(true);
				s_mutex					= new cMutex(true);
				s_client				= new cClient( cClient.NULL_ID, (ushort)m_recv_buf_size, m_use_cryptogram, DoRecv );
				s_clients				= new Dictionary<uint,cClient>();
				s_channels				= new Dictionary<byte,cChannel>();
				s_stage_party_count		= new Dictionary<uint,ushort>();
			}
			else
			{
				m_version			= s_version;
				m_server_address	= s_server_address;
				m_server_port		= s_server_port;
				m_recv_buf_size		= s_recv_buf_size;
				m_use_cryptogram	= s_use_cryptogram;
				m_cryptogram_iv		= s_cryptogram_iv;
				m_cryptogram_key	= s_cryptogram_key;
			}
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최초 업데이트 될때 호출됨.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected virtual void Start()
		{
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 업데이트.
		/// 주의 : 상속 받은 곳에서 반드시 base.Update()를 호출해 주어야 한다.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected virtual bool Update()
		{
			if( Connected==false )
			{
				Thread.Sleep(0);
				return false;
			}

			// 데이터 수신 처리 구간
			s_mutex.ReleaseMutex();
			Thread.Sleep(0);
			s_mutex.WaitOne();

			return true;
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 어플리케이션 종료.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected virtual void OnApplicationQuit()
		{
			if( s_client != null )
			{
				s_client.Disconnect();
				s_client = null;
			}

			/*
			if( s_thread_connect!=null )
			{
				s_thread_connect.Abort();
				s_thread_connect = null;
			}
			*/
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 접속
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public void Connect()
		{
			if( s_thread_connect!=null )
			{
				s_thread_connect.Abort();
				s_thread_connect = null;
			}
			s_thread_connect = new Thread(ThreadConnect);
			s_thread_connect.Start();
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 접속 에러
		/// </summary>
		/// <param name="result">결과코드</param>
		/// <param name="error_msg">에러메시지</param>
		//----------------------------------------------------------------------------------------------------
		protected virtual void OnConnectError( cNetwork.eResult result, string error_msg )
		{
			Print( result, error_msg );
		}

        //----------------------------------------------------------------------------------------------------
		#region 클라이언트 리스트
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	클라이언트 추가.
		/// </summary>
		/// <param name="id">클라이언트 아이디.</param>
		/// <param name="value">클라이언트 인스턴스.</param>
        //----------------------------------------------------------------------------------------------------
		public void AddClient( uint id, cClient value )
		{
			lock(s_clients)
			{
				if( s_clients.ContainsKey( id )==false )
				{
					s_clients.Add( id, value );
				}
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	클라이언트 삭제.
		/// </summary>
		/// <param name="id">클라이언트 아이디.</param>
        //----------------------------------------------------------------------------------------------------
		public void RemoveClient( uint id )
		{
			lock(s_clients)
			{
				s_clients.Remove( id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 모두 삭제.
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public void RemoveAllClient()
		{
			lock( s_clients )
			{
				s_clients.Clear();
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	클라이언트를 얻어온다.
		/// </summary>
		/// <param name="id">클라이언트 아이디.</param>
		/// <param name="value">클라이언트 객체.</param>
		/// <returns>성공 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool GetClient( uint id, out cClient value )
		{
			lock(s_clients)
			{
				return s_clients.TryGetValue( id, out value );
			}
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
		#region 채널 리스트
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널 추가.
		/// </summary>
		/// <param name="id">채널 아이디.</param>
		/// <param name="value">채널 인스턴스.</param>
        //----------------------------------------------------------------------------------------------------
		public void AddChannel( byte id, cChannel value )
		{
			lock(s_channels)
			{
				s_channels.Add( id, value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널 삭제.
		/// </summary>
		/// <param name="id">채널 아이디.</param>
        //----------------------------------------------------------------------------------------------------
		public void RemoveChannel( byte id )
		{
			lock(s_channels)
			{
				s_channels.Remove( id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 모두 삭제.
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public void RemoveAllChannel()
		{
			lock( s_channels )
			{
				s_channels.Clear();
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널를 얻어온다.
		/// </summary>
		/// <param name="id">채널 아이디.</param>
		/// <param name="value">[출력]채널 객체.</param>
		/// <returns>성공 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool GetChannel( byte id, out cChannel value )
		{
			lock(s_channels)
			{
				return s_channels.TryGetValue( id, out value );
			}
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
		#region 스테이지 파티 카운트
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	스테이지 파티 카운트 입력.
		/// </summary>
		/// <param name="id">스테이지 아이디.</param>
		/// <param name="party_count">파티 개수.</param>
        //----------------------------------------------------------------------------------------------------
		public void SetStagePartyCount( uint id, ushort party_count )
		{
			lock(s_stage_party_count)
			{
				s_stage_party_count[id] = party_count;
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지 파티 카운트 모두 삭제.
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		public void RemoveAllStagePartyCount()
		{
			lock(s_stage_party_count)
			{
				s_stage_party_count.Clear();
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
        public virtual Exception Exception()
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
        public virtual Exception Exception( params object[] values )
        {
            return new Exception( cLog.LogToString( 2, cObject.ValueToString(values) ) );
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
        public virtual void Log( params object[] values )
        {
            Debug.Log( "NC > " + cObject.ValueToString(values) );
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
        public virtual void Print( params object[] values )
        {
            Debug.Log( "NC > " + cObject.ValueToString(values) );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 에러메시지를 콘솔창에 출력 한다.
		/// </summary>
		/// <param name="values">데이터리스트.</param>
        //----------------------------------------------------------------------------------------------------
        public virtual void Error( params object[] values )
        {
            Debug.LogError( "error : NC > " + cObject.ValueToString(values) );
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 데이터 수신
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 수신 처리
		/// </summary>
		/// <param name="client">cClient 인스턴스</param>
		/// <param name="data">수신 데이터</param>
		/// <param name="size">수신 데이터 크기</param>
        //----------------------------------------------------------------------------------------------------
		protected void DoRecv( cClient client, byte[] data, int size )
		{
			// 다른쓰레드 작업이 종료될때까지 기다린다.
			s_mutex.WaitOne();

			// 받은 데이터를 처리한다.
			OnRecv( client, data, size );

			// 다른쓰래드로 프로세스를 넘긴다.
			s_mutex.ReleaseMutex();
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 수신 처리
		/// </summary>
		/// <param name="client">cClient 인스턴스</param>
		/// <param name="data">수신 데이터</param>
		/// <param name="size">수신 데이터 크기</param>
        //----------------------------------------------------------------------------------------------------
		void OnRecv( cClient client, byte[] data, int size )
		{
			if( client.UseCryptogram ) data = cCryptogram.Decrypt( data, size, client.DataIV, client.DataKey );

			cBitStream bits = new cBitStream();
			bits.Write( data, 0, size );
			bits.Position = 0;

			cNetwork.eOrder order	= cNetwork.ReadOrder( bits );
			cNetwork.eResult result = cNetwork.ReadResult( bits );

            Print( order + " : " + result + " : \n" + bits.ToString() );

			switch( order )
			{
            case cNetwork.eOrder.SERVER_LOGIN:				RecvServerLogin( result, bits );			break;
            case cNetwork.eOrder.SERVER_IN:					RecvServerIn( result, bits );				break;
            case cNetwork.eOrder.SERVER_OUT:				RecvServerOut( result, bits );				break;
            case cNetwork.eOrder.CLIENT_INFO_DEFAULT:		RecvClientInfoDefault( result, bits );		break;
            case cNetwork.eOrder.CHANNEL_LIST:				RecvChannelList( result, bits );			break;
            case cNetwork.eOrder.CHANNEL_IN:				RecvChannelIn( result, bits );				break;
            case cNetwork.eOrder.CHANNEL_OUT:				RecvChannelOut( result, bits );				break;
            case cNetwork.eOrder.CHANNEL_CHAT:				RecvChannelChat( result, bits );			break;
            case cNetwork.eOrder.PARTY_CHAT:				RecvPartyChat( result, bits );				break;
            case cNetwork.eOrder.STAGE_LIST:				RecvStageList( result, bits );				break;
            case cNetwork.eOrder.STAGE_IN_REQUEST:			RecvStageInRequest( result, bits );			break;
            case cNetwork.eOrder.STAGE_IN_ACCEPT:			RecvStageInAccept( result, bits );			break;
            case cNetwork.eOrder.STAGE_USER_IN:				RecvStageUserIn( result, bits );			break;
            case cNetwork.eOrder.STAGE_USER_OUT:			RecvStageUserOut( result, bits );			break;
            case cNetwork.eOrder.STAGE_USER_MOVE:			RecvStageUserMove( result, bits );			break;
            case cNetwork.eOrder.STAGE_USER_ATTACK_MONSTER:	RecvStageUserAttackMonster( result, bits );	break;
            case cNetwork.eOrder.STAGE_USER_SKILL_SELF:		RecvStageUserSkillSelf( result, bits );		break;
            case cNetwork.eOrder.STAGE_USER_SKILL_MONSTER:	RecvStageUserSkillMonster( result, bits );	break;
            case cNetwork.eOrder.STAGE_USER_SKILL_POS:		RecvStageUserSkillPos( result, bits );		break;
            case cNetwork.eOrder.STAGE_USER_DAMAGE:			RecvStageUserDemage( result, bits );		break;
            case cNetwork.eOrder.STAGE_USER_ITEM_USE_SELF:	RecvStageUserItemUseSelf( result, bits );	break;
            case cNetwork.eOrder.STAGE_USER_TRIGGER_ON:		RecvStageUserTriggerOn( result, bits );		break;
            case cNetwork.eOrder.STAGE_MON_IN:				RecvStageMonIn( result, bits );				break;
            case cNetwork.eOrder.STAGE_MON_MOVE:			RecvStageMonMove( result, bits );			break;
            case cNetwork.eOrder.STAGE_MON_ATTACK_USER:		RecvStageMonAttackUser( result, bits );		break;
            case cNetwork.eOrder.STAGE_MON_SKILL_SELF:		RecvStageMonSkillSelf( result, bits );		break;
            case cNetwork.eOrder.STAGE_MON_SKILL_ACTOR:		RecvStageMonSkillUser( result, bits );		break;
            case cNetwork.eOrder.STAGE_MON_SKILL_POS:		RecvStageMonSkillPos( result, bits );		break;
            case cNetwork.eOrder.STAGE_MON_DAMAGE:			RecvStageMonDemage( result, bits );			break;
            case cNetwork.eOrder.STAGE_DATA:				RecvStageData( result, bits );				break;
            default:
				client.Disconnect();
                break;
			}
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 수신 데이터 파싱
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 서버 : 로그인
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        protected virtual void RecvServerLogin( cNetwork.eResult result, cBitStream bits )
        {
			if( result == cNetwork.eResult.FAIL_SERVER_LOGIN_REFUSE )
			{
				// 기존 접속 과 연결 시도....
			}
			if( result != cNetwork.eResult.SUCCESS )
			{
				Print( "FAILED : " + result );
				return;
			}

			Client.AccountID = cNetwork.ReadAccountId( bits );
			Print( Client.ClientID, Client.AccountID );

			// 서버 입장
			Client.SendServerIn( s_version );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 서버 : 입장
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        protected virtual void RecvServerIn( cNetwork.eResult result, cBitStream bits )
        {
			if( result != cNetwork.eResult.SUCCESS ) return;

			Client.ClientID = cNetwork.ReadClientId( bits );

			Print( "ClientID:"+Client.ClientID );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 서버 : 퇴장
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        protected virtual void RecvServerOut( cNetwork.eResult result, cBitStream bits )
        {
			if( result != cNetwork.eResult.SUCCESS ) return;

			Client.Disconnect();
			Print();
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 클라이언트 : 정보 : 기본
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        protected virtual void RecvClientInfoDefault( cNetwork.eResult result, cBitStream bits )
        {
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id		= cNetwork.ReadClientId( bits );
			string	character_name	= cNetwork.ReadString( bits );
			byte	channel_id		= cNetwork.ReadChannelId( bits );
			uint	stage_id		= cNetwork.ReadStageId( bits );

			Print( "client_id="+client_id, "character_name="+character_name, "channel_id="+channel_id, "stage_id="+stage_id );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 리스트
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvChannelList( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			RemoveAllChannel();
			byte channel_count = cNetwork.ReadChannelCount( bits );
			string print = "[" + channel_count + "]{";
			for( int c=0; c<channel_count; ++c )
			{
				byte channel_id = cNetwork.ReadChannelId( bits );
				AddChannel( channel_id, new cChannel(channel_id) );
				print += channel_id + ",";
			}
			print += "}";
			Print( print );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 입장
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvChannelIn( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			byte	channel_id		= cNetwork.ReadChannelId( bits );
			ushort	user_count		= cNetwork.ReadChannelUserCount( bits );
			uint	client_id		= cNetwork.ReadClientId( bits );
			string	char_name		= cNetwork.ReadString( bits );
			uint	stage_id		= cNetwork.ReadStageId( bits );
			Print( "channel_id="+channel_id, "user_count="+user_count, "client_id="+client_id, "char_name="+char_name, "stage_id="+stage_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 퇴장
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvChannelOut( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint client_id = cNetwork.ReadClientId(	bits );
			Print( "client_id="+client_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 채팅
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvChannelChat( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			string	message		= cNetwork.ReadString( bits );
			Print( client_id, message );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 파티 : 채팅.
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvPartyChat( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			string	message		= cNetwork.ReadString( bits );
			Print( client_id, message );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 리스트
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageList( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			RemoveAllStagePartyCount();
			ushort stage_count = cNetwork.ReadStageCount( bits );
			string print = "Stages[" + stage_count + "]{";
			for( int c=0; c<stage_count; ++c )
			{
				uint	stage_id	= cNetwork.ReadStageId( bits );
				ushort	party_count	= cNetwork.ReadPartyCount( bits );

				SetStagePartyCount( stage_id, party_count );

				print += stage_id + "=" + party_count + ",";
			}
			print += "}";
			Print( print );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 입장 : 요청
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageInRequest( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			Client.Party = cNetwork.ReadPartyId( bits );

			Print( Client.Party );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 입장 : 승락
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageInAccept( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			Client.Stage = cNetwork.ReadStageId( bits );
			Print( Client.Stage );

			// 스테이지 입장
			// 장착아이템 입력(임시)
			uint[] equip_items = new uint[(int)cUserCharacter.eEquipSlot.MAX_EQUIPSLOT];
			for( int c=0; c<equip_items.Length; ++c )
			{
				equip_items[c] = 0;
			}
			Client.SendStageUserIn( equip_items, new cVector3(0,0,0) ); 
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 입장
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserIn( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult	.SUCCESS ) return;

			// 캐릭터 정보 받음
			bool	master				= cNetwork.ReadFlag(bits);
			uint	client_id			= cNetwork.ReadClientId( bits );
			string	char_name			= cNetwork.ReadString(bits);
			uint	char_item_info_id	= cNetwork.ReadItemInfoId(bits);
			uint[]	equip_items			= cNetwork.ReadItemInfoIds(bits,(int)cUserCharacter.eEquipSlot.MAX_EQUIPSLOT);
			cVector3 stage_pos			= cNetwork.ReadStagePos( bits );
	
			Print( client_id, master, char_name, stage_pos );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 퇴장
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserOut( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			uint	master_id	= cNetwork.ReadClientId( bits );
			Print( client_id, master_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 이동
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserMove( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			cVector3 stage_pos	= cNetwork.ReadStagePos( bits );
			Print( client_id, stage_pos );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 공격 : 몬스터
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserAttackMonster( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			ushort	monster_id	= cNetwork.ReadMonsterId( bits );
			Print( client_id, monster_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 스킬 사용 : 자신
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserSkillSelf( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			ushort	skill_id	= cNetwork.ReadSkillId( bits );
			Print( client_id, skill_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 스킬 사용 : 몬스터
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserSkillMonster( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id		= cNetwork.ReadClientId( bits );
			ushort	skill_id		= cNetwork.ReadSkillId( bits );
			byte	target_count	= cNetwork.ReadSkillTargetCount( bits );
			ushort[]	monster_ids	= new ushort[target_count];
			string print = "";
			print += "client_id="+client_id;
			print += ",skill_id="+skill_id;
			print += ",monster_ids{";
			for( byte c=0; c<target_count; ++c )
			{
				monster_ids[c]	= cNetwork.ReadMonsterId( bits );
				print += monster_ids[c]+",";
			}
			print += "}";
			Print( print );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 스킬 사용 : 좌표
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserSkillPos( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint		client_id	= cNetwork.ReadClientId( bits );
			ushort		skill_id	= cNetwork.ReadSkillId( bits );
			cVector3	stage_pos	= cNetwork.ReadStagePos( bits );
			Print( client_id, skill_id, stage_pos );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 데미지
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserDemage( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint		client_id	= cNetwork.ReadClientId( bits );
			uint		damage		= cNetwork.ReadDamage( bits );
			bool		death		= cNetwork.ReadFlag( bits );
			Print( client_id, damage, death );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 아이템 사용 : 자신
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserItemUseSelf( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			ulong	item_id		= cNetwork.ReadItemId( bits );
			Print( client_id, item_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 트리거 작동
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageUserTriggerOn( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			uint	client_id	= cNetwork.ReadClientId( bits );
			ushort	trigger_id	= cNetwork.ReadTriggerId( bits );
			Print( client_id, trigger_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지  : 몬스터 : 입장 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonIn( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort		monster_id		= cNetwork.ReadMonsterId( bits );
			uint		monster_info_id	= cNetwork.ReadMonsterInfoId( bits );
			cVector3	stage_pos		= cNetwork.ReadStagePos( bits );
			Print( monster_id, monster_info_id, stage_pos );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지  : 몬스터 : 이동 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonMove( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort		monster_id	= cNetwork.ReadMonsterId( bits );
			cVector3	stage_pos	= cNetwork.ReadStagePos( bits );
			Print( monster_id, stage_pos );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 공격 : 유저 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonAttackUser ( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort	monster_id	= cNetwork.ReadMonsterId( bits );
			uint	client_id	= cNetwork.ReadClientId( bits );
			Print( monster_id, client_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 스킬 사용 : 자신 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonSkillSelf( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort monster_id = cNetwork.ReadMonsterId( bits );
			ushort skill_id = cNetwork.ReadSkillId( bits );
			Print( monster_id, skill_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 스킬 사용 : 유저 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonSkillUser( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort	monster_id		= cNetwork.ReadMonsterId( bits );
			ushort	skill_id		= cNetwork.ReadSkillId( bits );
			byte	target_count	= cNetwork.ReadSkillTargetCount( bits );
			uint[]	client_ids		= new uint[target_count];
			string print = "";
			print += "client_id="+monster_id;
			print += ",skill_id="+skill_id;
			print += ",client_ids{";
			for( byte c=0; c<target_count; ++c )
			{
				client_ids[c]		= cNetwork.ReadClientId( bits );
			}
			Print( monster_id, skill_id );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 스킬 사용 : 좌표 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonSkillPos( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort		monster_id	= cNetwork.ReadMonsterId( bits );
			ushort		skill_id	= cNetwork.ReadSkillId( bits );
			cVector3	stage_pos	= cNetwork.ReadStagePos( bits );
			Print( monster_id, skill_id, stage_pos );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 데미지 : (파티장 권한 필요)
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageMonDemage( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;

			ushort		monster_id	= cNetwork.ReadMonsterId( bits );
			uint		damage		= cNetwork.ReadDamage( bits );
			bool		death		= cNetwork.ReadFlag( bits );
			Print( monster_id, damage, death );
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 커스텀 데이터
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected virtual void RecvStageData( cNetwork.eResult result, cBitStream bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;
			Print();
		}
		#endregion
    }
}

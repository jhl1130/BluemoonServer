//----------------------------------------------------------------------------------------------------
// cServer
// : 서버 베이스
//  -JHL-2012-02-09
//----------------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 서버 베이스
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cServer : cNetwork
    {
		//----------------------------------------------------------------------------------------------------
 		#region 상태 정의
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 상태 정의
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public enum eState
		{
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// 비활성
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			InActive,
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// 비활성중
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			InActivating,
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// 활성중
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			Activating,
			//----------------------------------------------------------------------------------------------------
			/// <summary>
			/// 활성
			/// </summary>
			//----------------------------------------------------------------------------------------------------
			Active
		}
		#endregion

		#region 상수
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 변수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 유저수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	static ushort					s_max_user			= 5000;			
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// TCP 소켓 리스너
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected	TcpListener						m_listener			= null;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 메인 쓰래드
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected	Thread							m_thread_main		= null;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 현재 상태
		/// </summary>
        protected	eState							m_state				= eState.InActive;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 리스트(key:클라이언트ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        protected	Dictionary<uint,cClient>		m_clients			= new Dictionary<uint,cClient>();
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 리스트(key:채널ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        protected	Dictionary<byte,cChannel>		m_channels			= new Dictionary<byte,cChannel>();
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 리스트(key:파티ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        protected	Dictionary<uint,cParty>			m_parties			= new Dictionary<uint,cParty>();
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지 개수(key:StageID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
        protected	Dictionary<uint,cStage>			m_stages			= new Dictionary<uint,cStage>();
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 속성
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 최대 유저수
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		static	ushort					MaxUser				{get{return s_max_user;}set{s_max_user=value;}}			
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 현재 상태
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public		eState							State				{get{return m_state;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 리스트(key:클라이언트ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		Dictionary<uint,cClient>		Clients				{get{return m_clients;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널 리스트(key:채널ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		Dictionary<byte,cChannel>		Channels			{get{return m_channels;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 리스트(key:파티ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		Dictionary<uint,cParty>			Parties				{get{return m_parties;}}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 스테이지 리스트(key:스테이지ID)
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public		Dictionary<uint,cStage>			Stages				{get{return m_stages;}}
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 생성자
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public cServer():base("Server","SR")
        {
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="name">이름</param>
		/// <param name="short_name">짧은이름</param>
        //----------------------------------------------------------------------------------------------------
        public cServer( string name, string short_name ):base(name,short_name)
        {
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 서버 시작
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 시작
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void Start()
        {
			Start( m_port );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 시작
		/// </summary>
		/// <param name="port">리슨 포트</param>
        //----------------------------------------------------------------------------------------------------
        public virtual void Start( ushort port )
        {
            if( m_state == eState.InActive )
            {
				m_state = eState.Activating;
				m_port = port;
                m_thread_main		= new Thread( DoListen );
                m_thread_main.Start();
                Print( "SERVER_START" );
                m_state = eState.Active;
            }
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 서버 정지
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 정지
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public void Stop()
        {
            if( m_state == eState.Active )
            {
                m_state = eState.InActivating;
                m_listener.Stop();
				// 정지될때까지 대기한다.
				m_thread_main.Join();
                Print( "SERVER_STOP" );
                m_state = eState.InActive;
            }
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트 접속 종료
		/// </summary>
		/// <param name="client"></param>
        //----------------------------------------------------------------------------------------------------
		public void DisconnectClient( cClient client )
		{
			if( client != null )
			{
				OutChannel( client.Channel, client );
				RemoveClient( client.ClientID );
			}
		}

        //----------------------------------------------------------------------------------------------------
 		#region 리슨 프로세스
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 리슨 프로세스
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected virtual void DoListen()
        {
            try
            {
                m_listener = new TcpListener( System.Net.IPAddress.Any, m_port );
                m_listener.Start();

                while(true)
                {
                    Print( "WAIT_CLIENT" );
                    cClient client = new cClient( cClient.UniqueID, m_listener.AcceptTcpClient(), RecvBufSize, UseCryptogram, OnRecv );
					client.Parent = this;
                    Print( "CONNECT : " + client.Address );
                }
            }
            catch( Exception ex )
            {
				Error( ex.Message );
				Log( "error:"+ex );
            }
        }
		#endregion


		//----------------------------------------------------------------------------------------------------
 		#region 데이터 수신
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트로 부터 받은 패킷처리
		/// 주의 : 이 함수를 상속받아서 수신 데이터 처리를 요함
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		/// <param name="size"></param>
        protected virtual void OnRecv( cClient client, byte[] data, int size )  {}
		#endregion

		//----------------------------------------------------------------------------------------------------
 		#region 데이터 송신
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 모든 클라이언트에 데이터 송신.
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
		/// 모든 클라이언트에 데이터 송신.
		/// </summary>
		/// <param name="bits">송신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		public void Send( cBitStream bits )
        {
			lock( m_clients )
			{
				foreach( KeyValuePair<uint,cClient> entry  in m_clients )
				{
					((cClient)entry.Value).Send( bits );
				}
			}
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 모든 클라이언트에 데이터 송신.
		/// </summary>
		/// <param name="bits">송신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		public void Send( byte[] bits )
        {
			lock( m_clients )
			{
				foreach( KeyValuePair<uint,cClient> entry  in m_clients )
				{
					((cClient)entry.Value).Send( bits );
				}
			}
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널에 소속된 클라이언트에 데이터 송신.
		/// </summary>
		/// <param name="channel_id">채널 아이디</param>
		/// <param name="order">명령코드</param>
		/// <param name="result">결과코드</param>
        //----------------------------------------------------------------------------------------------------
        public void SendChannel( byte channel_id, eOrder order, eResult result )
        {
			cBitStream bits = new cBitStream();
			WriteOrder( bits, order );
			WriteResult( bits, result );
			SendChannel( channel_id, bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 채널에 소속된 클라이언트에 데이터 송신.
		/// </summary>
		/// <param name="channel_id">채널 아이디</param>
		/// <param name="bits">송신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		public void SendChannel( byte channel_id, cBitStream bits )
        {
			lock( m_channels )
			{
				cChannel channel=null;
				if( m_channels.TryGetValue( channel_id, out channel ) )
				{
					lock( channel.Children )
					{
						foreach( KeyValuePair<uint,cObject> entry in channel.Children )
						{
							((cClient)entry.Value).Send( bits );
						}
					}
				}
			}
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티에 소속된 클라이언트에 데이터 송신.
		/// </summary>
		/// <param name="party_id">파티 아이디</param>
		/// <param name="order">명령코드</param>
		/// <param name="result">결과코드</param>
        //----------------------------------------------------------------------------------------------------
        public void SendParty( uint party_id, eOrder order, eResult result )
        {
			cBitStream bits = new cBitStream();
			WriteOrder( bits, order );
			WriteResult( bits, result );
			SendParty( party_id, bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티에 소속된 클라이언트에 데이터 송신.
		/// </summary>
		/// <param name="party_id">파티 아이디</param>
		/// <param name="bits">송신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		public void SendParty( uint party_id, cBitStream bits )
        {
			lock( m_parties )
			{
				cParty party=null;
				if( m_parties.TryGetValue( party_id, out party ) )
				{
					lock( party.Children )
					{
						foreach( KeyValuePair<uint,cObject> entry in party.Children )
						{
							if( entry.Value != null )
							{
								((cClient)entry.Value).Send( bits );
							}
							else
							{
								cClient c = (cClient)entry.Value;
							}
						}
					}
				}
			}
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
		#region 클라이언트 리스트
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	클라이언트 추가.
		/// </summary>
		/// <param name="client_id">클라이언트 아이디.</param>
		/// <param name="value">클라이언트 인스턴스.</param>
        //----------------------------------------------------------------------------------------------------
		public void AddClient( uint client_id, cClient value )
		{
			lock(m_clients)
			{
				m_clients.Add( client_id, value );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	클라이언트 삭제.
		/// </summary>
		/// <param name="client_id">클라이언트 아이디.</param>
        //----------------------------------------------------------------------------------------------------
		public void RemoveClient( uint client_id )
		{
			lock(m_clients)
			{
				m_clients.Remove( client_id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	클라이언트를 얻어온다.
		/// </summary>
		/// <param name="client_id">클라이언트 아이디.</param>
		/// <param name="value">클라이언트 객체.</param>
		/// <returns>성공 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool GetClient( uint client_id, out cClient value )
		{
			lock(m_clients)
			{
				return m_clients.TryGetValue( client_id, out value );
			}
		}
		#endregion

        //----------------------------------------------------------------------------------------------------
		#region 채널 리스트
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널 추가.
		/// </summary>
		/// <returns>채널 객체</returns>
        //----------------------------------------------------------------------------------------------------
		public cChannel AddChannel()
		{
			lock(m_channels)
			{
				cChannel channel = new cChannel();
				m_channels.Add( channel.ChannelID, channel );
				return channel;
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널 삭제.
		/// </summary>
		/// <param name="channel_id">삭제할 채널 아이디.</param>
		/// <returns>성공유무</returns>
        //----------------------------------------------------------------------------------------------------
		public bool RemoveChannel( byte channel_id )
		{
			lock(m_channels)
			{
				return m_channels.Remove( channel_id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널를 얻어온다.
		/// </summary>
		/// <param name="channel_id">채널 아이디.</param>
		/// <param name="channel">[출력]채널 객체.</param>
		/// <returns>성공 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool GetChannel( byte channel_id, out cChannel channel )
		{
			lock(m_channels)
			{
				return m_channels.TryGetValue( channel_id, out channel );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	입장 가능한 채널을 얻어온다.
		/// </summary>
		/// <returns>채널 객체.</returns>
        //----------------------------------------------------------------------------------------------------
		public cChannel GetChannelIn()
		{
			lock(m_channels)
			{
				foreach( KeyValuePair<byte,cChannel> entry in m_channels )
				{
					if( entry.Value.Children.Count < cChannel.MaxUser )
					{
						return entry.Value;
					}
				}
				return null;
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널를 존재 확인.
		/// </summary>
		/// <param name="channel_id">채널 아이디.</param>
		/// <returns>존재 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool IsChannel( byte channel_id )
		{
			lock(m_channels)
			{
				return m_channels.ContainsKey( channel_id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널 입장.
		/// </summary>
		/// <param name="channel_id">채널 아이디.</param>
		/// <param name="client">클라이언트 객체.</param>
		/// <returns>결과</returns>
        //----------------------------------------------------------------------------------------------------
		public bool InChannel( byte channel_id, cClient client )
		{
			lock(m_channels)
			{
				cChannel channel;
				if( m_channels.TryGetValue( channel_id, out channel ) )
				{
					return channel.AddClient( client );
				}
			}
			return false;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	채널 퇴장.
		/// </summary>
		/// <param name="channel_id">채널 아이디.</param>
		/// <param name="client">클라이언트 객체.</param>
        //----------------------------------------------------------------------------------------------------
		public void OutChannel( byte channel_id, cClient client )
		{
			// 스테이지 퇴장
			OutStage( client.Stage, client );

			// 채널 퇴장
			lock(m_channels)
			{
				cChannel channel;
				if( m_channels.TryGetValue( channel_id, out channel ) )
				{
					channel.RemoveClient( client );

					//-----------------------------------------------
					// Response
					cBitStream		bits = new cBitStream();
					WriteOrder(		bits, cNetwork.eOrder.CHANNEL_OUT );
					WriteResult(	bits, eResult.SUCCESS );
					WriteClientId(	bits, client.ClientID );
					client.Send( bits );
					SendChannel( channel_id, bits );
				}
			}
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 파티 리스트
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 : 추가
		/// </summary>
		/// <returns>파티 객체</returns>
		//----------------------------------------------------------------------------------------------------
		public cParty AddParty( byte max_user )
		{
			lock(m_parties)
			{
				cParty party = new cParty(max_user);
				m_parties.Add( party.PartyID, party );
				return party;
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 : 삭제
		/// </summary>
		/// <param name="party_id"></param>
		/// <returns>성공유무</returns>
		//----------------------------------------------------------------------------------------------------
		public bool RemoveParty( uint party_id )
		{
			lock(m_parties)
			{
				return m_parties.Remove( party_id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	파티를 얻어온다.
		/// </summary>
		/// <param name="party_id">얻어올 파티 아이디.</param>
		/// <param name="party">출력될 파티 객체.</param>
		/// <returns>성공 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool GetParty( uint party_id, out cParty party )
		{
			lock(m_parties)
			{
				return m_parties.TryGetValue( party_id, out party );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	파티를 존재 확인.
		/// </summary>
		/// <param name="party_id">파티 아이디.</param>
		/// <returns>존재 유무.</returns>
        //----------------------------------------------------------------------------------------------------
		public bool IsParty( uint party_id )
		{
			lock(m_parties)
			{
				return m_parties.ContainsKey( party_id );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	파티 입장.
		/// </summary>
		/// <param name="party_id">파티 아이디.</param>
		/// <param name="client">클라이언트 객체.</param>
		/// <returns>결과</returns>
        //----------------------------------------------------------------------------------------------------
		public bool InParty( uint party_id, cClient client )
		{
			lock(m_parties)
			{
				cParty party;
				if( m_parties.TryGetValue( party_id, out party ) )
				{
					if( party.LockIn==false )
					{
						if( party.AddChild( client.ClientID, client ) )
						{
							int count = party.Children.Count;
							client.Master = (count==1);
							client.Party = party_id;
							if( count >= party.MaxUser )
							{
								party.LockIn = true;
							}
							return true;
						}
					}
				}
			}
			return false;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	파티 퇴장.
		/// </summary>
		/// <param name="party_id">파티 아이디.</param>
		/// <param name="client">클라이언트 객체.</param>
        //----------------------------------------------------------------------------------------------------
		public void OutParty( uint party_id, cClient client )
		{
			lock(m_parties)
			{
				cParty party;
				if( m_parties.TryGetValue( party_id, out party ) )
				{
					party.RemoveChild( client.ClientID );
					client.Party = cParty.NULL_ID;
					if( party.Children.Count == 0 )
					{
						m_parties.Remove( party_id );
					}
					else
					{
						// 파티장 교체 : 첫번째 유저
						if( client.Master )
						{
							foreach( KeyValuePair<uint,cObject> entry in party.Children )
							{
								cClient new_master=(cClient)entry.Value;
								new_master.Master = true;
								break;
							}
						}
					}
				}
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티원수 구하기
		/// </summary>
		/// <param name="party_id">파티 아이디</param>
		/// <returns>파티원수</returns>
        //----------------------------------------------------------------------------------------------------
		public byte GetPartyUserCount( uint party_id )
		{
			lock(m_parties)
			{
				cParty party;
				if( m_parties.TryGetValue( party_id, out party ) )
				{
					return (byte)(party.Children.Count);
				}
			}
			return 0;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티장 구하기
		/// </summary>
		/// <param name="party_id">파티 아이디</param>
		/// <returns>파티장아이디</returns>
        //----------------------------------------------------------------------------------------------------
		public uint GetPartyMaster( uint party_id )
		{
			lock(m_parties)
			{
				cParty party;
				if( m_parties.TryGetValue( party_id, out party ) )
				{
					foreach( KeyValuePair<uint,cObject> entry in party.Children )
					{
						if( entry.Value != null )
						{
							cClient client = (cClient)(entry.Value);
							if( client.Master ) return client.ClientID;
						}
					}
				}
			}
			return cClient.NULL_ID;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 파티 : 잠금 : 입장
		/// </summary>
		/// <param name="party_id"></param>
		/// <returns>잠금유무</returns>
        //----------------------------------------------------------------------------------------------------
		public bool PartyLockIn( uint party_id )
		{
			lock(m_parties)
			{
				cParty party;
				if( m_parties.TryGetValue( party_id, out party ) )
				{
					return party.LockIn;
				}
			}
			return false;
		}
		#endregion

		//----------------------------------------------------------------------------------------------------
		#region 스테이지 리스트
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	스테이지 입장.(추가+입장)
		/// </summary>
		/// <param name="stage_id">스테이지 아이디.</param>
		/// <param name="client">클라이언트 객체.</param>
		/// <param name="max_user">스테이지 최대 입장객수.</param>
		/// <returns>파티 아이디</returns>
        //----------------------------------------------------------------------------------------------------
		public uint InStage( uint stage_id, byte max_user, cClient client )
		{
			lock(m_stages)
			{
				// 스테이지가 없으면 생성
				cStage stage=null;
				if( m_stages.TryGetValue( stage_id, out stage )==false )
				{
					stage = new cStage( stage_id );
					m_stages.Add( stage_id, stage );
				}

				// 파티중 빈 자리에 먼저 입장한다. 
				cParty party=null;
				foreach( KeyValuePair<uint,cObject> entry in stage.Children )
				{
					party = (cParty)entry.Value;
					// 파티 입장
					if( InParty( party.PartyID, client ) )
					{
						client.Stage = stage_id;
						return party.PartyID;
					}
				}

				// 빈자리가 없으면 새로 파티를 만든다.

				// 파티 생성
				party = AddParty(max_user);
				if( party != null )
				{
					// 파티 등록
					if( stage.AddChild( party.PartyID, party ) )
					{
						// 파티 입장
						if( InParty( party.PartyID, client ) )
						{
							client.Stage = stage_id;
							return party.PartyID;
						}
					}
				}
			}
			return cParty.NULL_ID;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		///	스테이지 퇴장.
		/// </summary>
		/// <param name="stage_id">스테이지 아이디.</param>
		/// <param name="client">클라이언트 객체.</param>
        //----------------------------------------------------------------------------------------------------
		public void OutStage( uint stage_id, cClient client )
		{
			lock(m_stages)
			{
				// 스테이지 퇴장
				cStage stage=null;
				if( m_stages.TryGetValue( stage_id, out stage ) )
				{
					client.Stage = cStage.NULL_ID;
				}

				// 파티 퇴장
				uint party_id = client.Party;
				if( party_id != cParty.NULL_ID )
				{
					lock(m_parties)
					{
						// 파티 퇴장
						OutParty( party_id, client );

						// 등록된 파티 제거
						if( IsParty( party_id )==false )
						{
							if( stage != null )
							{
								stage.RemoveChild( party_id );
							}
						}

						//-----------------------------------------------
						// Response
						cBitStream bits = new cBitStream();
						WriteOrder(	bits, cNetwork.eOrder.STAGE_USER_OUT );
						WriteResult( bits, eResult.SUCCESS );
						WriteClientId( bits, client.ClientID );
						WriteClientId( bits, GetPartyMaster( party_id ) );
						client.Send( bits );
						SendParty( party_id, bits );
					}
				}
			}
		}
		#endregion
    }
}

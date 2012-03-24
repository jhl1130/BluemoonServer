//----------------------------------------------------------------------------------------------------
// cGameServer
// : 게임서버
//  -JHL-2012-02-09
//----------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 게임서버
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cGameServer : cServer
    {
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 요청 객체
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public class cRequest
		{
			/// <summary>
			/// 클라이언트 객체
			/// </summary>
			public cClient		client;
			/// <summary>
			/// 요청명령
			/// </summary>
			public eOrder		order;
			/// <summary>
			/// 파라메타
			/// </summary>
			public cBitStream	bits;
		}

		//----------------------------------------------------------------------------------------------------
 		#region 변수
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 베이스
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected	cDatabase						m_db				= new cDatabase(cDatabase.eType.MySQL);
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB 로드 쓰래드
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected	Thread							m_thread_db_load	= null;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB 저장 쓰래드
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected	Thread							m_thread_db_save	= null;
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 요청 리스트
		/// </summary>
        //----------------------------------------------------------------------------------------------------
		protected	Queue<cRequest>					m_requests			= new Queue<cRequest>();
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 생성자
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public cGameServer():this("GameServer","GS")
        {
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="name">이름</param>
		/// <param name="short_name">짧은이름</param>
        //----------------------------------------------------------------------------------------------------
        public cGameServer( string name, string short_name ):base(name,short_name)
        {
			// 채널 생성(임시 채널 10개 생성)
			for( byte c=0; c<cChannel.MaxChannel; ++c )
			{
				AddChannel();
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버 시작
		/// </summary>
		/// <param name="port">리슨 포트</param>
        //----------------------------------------------------------------------------------------------------
        public override void Start( ushort port )
        {
            if( m_state == eState.InActive )
            {
				m_state = eState.Activating;
				m_port = port;
                m_thread_main		= new Thread( DoListen );
                m_thread_db_load	= new Thread( DoDBLoad );
                m_thread_db_save	= new Thread( DoDBSave );
                m_thread_main.Start();
                m_thread_db_load.Start();
                m_thread_db_save.Start();
                Print( "SERVER_START" );
                m_state = eState.Active;
            }
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 요청 명령 추가
		/// </summary>
		/// <param name="client">요청자</param>
		/// <param name="order">명령코드</param>
		/// <param name="bits">파라메타</param>
        //----------------------------------------------------------------------------------------------------
		protected void AddRequest( cClient client, eOrder order, cBitStream bits )
		{
			cRequest req = new cRequest();
			req.client	= client;
			req.order	= order;
			req.bits	= bits;
			lock( m_requests )
			{
				m_requests.Enqueue( req );
			}
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 요청 하나 읽기. 읽은건 삭제
		/// </summary>
		/// <returns></returns>
        //----------------------------------------------------------------------------------------------------
		protected cRequest ReadRequest()
		{
			lock(m_requests)
			{
				if( m_requests.Count>0 )
				{
					return m_requests.Dequeue();
				}
			}
			return null;
		}

        //----------------------------------------------------------------------------------------------------
 		#region DB Load 프로세스
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB Load 프로세스
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected virtual void DoDBLoad()
        {
            try
            {
                while(true)
                {
					if( m_db.Connection == null )
					{
						Print( "DBConnecting" );
						if( m_db.Connect( "192.168.0.30", "bluemoon", "bluelab", "bluemoon" ) )
						{
							Print( "DBConnected" );
						}
						Thread.Sleep(100);
					}
					else
					{
						switch( m_db.Connection.State )
						{
						case System.Data.ConnectionState.Closed:
							Print( "DBConnecting" );
							if( m_db.Connect( "192.168.0.30", "bluemoon", "bluelab", "bluemoon" ) )
							{
								Print( "DBConnected" );
							}
							Thread.Sleep(100);
							break;
						case System.Data.ConnectionState.Open:
							{
								cRequest req = ReadRequest();
								if( req == null )
								{
								}
								else
								{
									//OnRecv( client, 
								}
								Thread.Sleep(0);
							}
							break;
						default:
							Thread.Sleep(100);
							break;
						}
					}
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
 		#region DB Save 프로세스
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// DB Save 프로세스
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected virtual void DoDBSave()
        {
            try
            {
                while(true)
                {
					Thread.Sleep(100);
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
 		#region OnRecv() : 클라이언트로 부터 받은 패킷처리
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트로 부터 받은 패킷처리
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="data">수신 데이터</param>
		/// <param name="size">수신 데이터 크기</param>
        //----------------------------------------------------------------------------------------------------
        protected override void OnRecv( cClient client, byte[] data, int size )
        {
			if( client.UseCryptogram ) data = cCryptogram.Decrypt( data, size, client.DataIV, client.DataKey );

			cBitStream bits = new cBitStream();
			bits.Write( data, 0, size );
			bits.Position = 0;

			//Print( "\n"+bits.ToString() );

			eOrder order = ReadOrder( bits );

            Print( client.ClientID + " : " + order + " : \n" + bits.ToString() );

			// 로그 기록 ( 별도 스래드를 사용 한다. )
			//string log = order.ToString() + client.AccountID, client.AccountID, client.CharID, bits.ToByteArray(), CurrentTime; 전투/채팅 로그는 제외
			// 각종 코드는 값이 변경될수 있으므로 문자열로 남긴다.

			//OnRecv( client, order, bits, false );

            // 파싱
            switch( order )
            {
			case eOrder.SERVER_LOGIN:				RecvServerLogin( client, order, bits );				break;
			case eOrder.SERVER_IN:					RecvServerIn( client, order, bits );				break;
			case eOrder.SERVER_OUT:					RecvServerOut( client, order, bits );				break;
			case eOrder.CLIENT_INFO_DEFAULT:		RecvClientInfoDefault( client, order, bits );		break;
			case eOrder.CHANNEL_LIST:				RecvChannelList( client, order, bits );				break;
			case eOrder.CHANNEL_IN:					RecvChannelIn( client, order, bits );				break;
			case eOrder.CHANNEL_OUT:				RecvChannelOut( client, order, bits );				break;
			case eOrder.CHANNEL_CHAT:				RecvChannelChat( client, order, bits );				break;
			case eOrder.PARTY_CHAT:					RecvPartyChat( client, order, bits );				break;
			case eOrder.STAGE_LIST:					RecvStageList( client, order, bits );				break;
			case eOrder.STAGE_IN_REQUEST:			RecvStageInRequest( client, order, bits );			break;
			case eOrder.STAGE_USER_IN:				RecvStageUserIn( client, order, bits );				break;
			case eOrder.STAGE_USER_OUT:				RecvStageUserOut( client, order, bits );			break;
			case eOrder.STAGE_USER_MOVE:			RecvStageUserMove( client, order, bits );			break;
			case eOrder.STAGE_USER_ATTACK_MONSTER:	RecvStageUserAttackMonster( client, order, bits );	break;
			case eOrder.STAGE_USER_SKILL_SELF:		RecvStageUserSkillSelf( client, order, bits );		break;
			case eOrder.STAGE_USER_SKILL_MONSTER:	RecvStageUserSkillMonster( client, order, bits );	break;
			case eOrder.STAGE_USER_SKILL_POS:		RecvStageUserSkillPos( client, order, bits );		break;
			case eOrder.STAGE_USER_DAMAGE:			RecvStageUserDemage( client, order, bits );			break;
			case eOrder.STAGE_USER_ITEM_USE_SELF:	RecvStageUserItemUseSelf( client, order, bits );	break;
			case eOrder.STAGE_USER_TRIGGER_ON:		RecvStageUserTriggerOn( client, order, bits );		break;
			case eOrder.STAGE_MON_IN:				RecvStageMonIn( client, order, bits );				break;
			case eOrder.STAGE_MON_MOVE:				RecvStageMonMove( client, order, bits );			break;
			case eOrder.STAGE_MON_ATTACK_USER:		RecvStageMonAttackUser( client, order, bits );		break;
			case eOrder.STAGE_MON_SKILL_SELF:		RecvStageMonSkillSelf( client, order, bits );		break;
			case eOrder.STAGE_MON_SKILL_ACTOR:		RecvStageMonSkillUser( client, order, bits );		break;
			case eOrder.STAGE_MON_SKILL_POS:		RecvStageMonSkillPos( client, order, bits );		break;
			case eOrder.STAGE_MON_DAMAGE:			RecvStageMonDemage( client, order, bits );			break;
			case eOrder.STAGE_DATA:					RecvStageData( client, order, bits );				break;
			default:
				Error( client.ClientID + " : " + order + " : \n" + bits.ToString() );
				client.Disconnect();
				break;
            }

			//LogDB에 로그 기록 ( 별도 스래드를 사용 한다. )
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 클라이언트로 부터 받은 패킷처리
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="data">수신 데이터</param>
		/// <param name="size">수신 데이터 크기</param>
        //----------------------------------------------------------------------------------------------------
        protected void OnRecv( cClient client, eOrder order, cBitStream bits, bool async )
        {
            // 파싱
			if( async )
			{
				switch( order )
				{
				case eOrder.SERVER_LOGIN:				RecvServerLogin( client, order, bits );				break;
				case eOrder.SERVER_IN:					RecvServerIn( client, order, bits );				break;
				case eOrder.SERVER_OUT:					RecvServerOut( client, order, bits );				break;
				case eOrder.CLIENT_INFO_DEFAULT:		RecvClientInfoDefault( client, order, bits );		break;
				case eOrder.CHANNEL_LIST:				RecvChannelList( client, order, bits );				break;
				case eOrder.CHANNEL_IN:					RecvChannelIn( client, order, bits );				break;
				case eOrder.CHANNEL_OUT:				RecvChannelOut( client, order, bits );				break;
				case eOrder.CHANNEL_CHAT:				RecvChannelChat( client, order, bits );				break;
				case eOrder.PARTY_CHAT:					RecvPartyChat( client, order, bits );				break;
				case eOrder.STAGE_LIST:					RecvStageList( client, order, bits );				break;
				case eOrder.STAGE_IN_REQUEST:			RecvStageInRequest( client, order, bits );			break;
				case eOrder.STAGE_USER_IN:				RecvStageUserIn( client, order, bits );				break;
				case eOrder.STAGE_USER_OUT:				RecvStageUserOut( client, order, bits );			break;
				case eOrder.STAGE_USER_MOVE:			RecvStageUserMove( client, order, bits );			break;
				case eOrder.STAGE_USER_ATTACK_MONSTER:	RecvStageUserAttackMonster( client, order, bits );	break;
				case eOrder.STAGE_USER_SKILL_SELF:		RecvStageUserSkillSelf( client, order, bits );		break;
				case eOrder.STAGE_USER_SKILL_MONSTER:	RecvStageUserSkillMonster( client, order, bits );	break;
				case eOrder.STAGE_USER_SKILL_POS:		RecvStageUserSkillPos( client, order, bits );		break;
				case eOrder.STAGE_USER_DAMAGE:			RecvStageUserDemage( client, order, bits );			break;
				case eOrder.STAGE_USER_ITEM_USE_SELF:	RecvStageUserItemUseSelf( client, order, bits );	break;
				case eOrder.STAGE_USER_TRIGGER_ON:		RecvStageUserTriggerOn( client, order, bits );		break;
				case eOrder.STAGE_MON_IN:				RecvStageMonIn( client, order, bits );				break;
				case eOrder.STAGE_MON_MOVE:				RecvStageMonMove( client, order, bits );			break;
				case eOrder.STAGE_MON_ATTACK_USER:		RecvStageMonAttackUser( client, order, bits );		break;
				case eOrder.STAGE_MON_SKILL_SELF:		RecvStageMonSkillSelf( client, order, bits );		break;
				case eOrder.STAGE_MON_SKILL_ACTOR:		RecvStageMonSkillUser( client, order, bits );		break;
				case eOrder.STAGE_MON_SKILL_POS:		RecvStageMonSkillPos( client, order, bits );		break;
				case eOrder.STAGE_MON_DAMAGE:			RecvStageMonDemage( client, order, bits );			break;
				case eOrder.STAGE_DATA:					RecvStageData( client, order, bits );				break;
				default:
					Error( client.ClientID + " : " + order + " : \n" + bits.ToString() );
					client.Disconnect();
					break;
				}
			}
			else
			{
				switch( order )
				{
				case eOrder.SERVER_LOGIN:				RecvServerLogin( client, order, bits );				break;
				}
			}

			//LogDB에 로그 기록 ( 별도 스래드를 사용 한다. )
        }
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region 수신 데이터 파싱
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 서버 : 로그인
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        void RecvServerLogin( cClient client, eOrder order, cBitStream bits )
        {
			string network_version	= ReadString( bits );
			string client_version	= ReadString( bits );
			string	member_id		= ReadString( bits );
			string	member_pw		= ReadString( bits );

			if( network_version != VERSION ) { client.Send( order, eResult.FAIL_NETWORK_VERSION ); return; }

			//string db_client_version = "0.1.0";
			//if( client_version != db_client_version ) { client.Send( order, eResult.FAIL_CLIENT_VERSION ); return; }

			// 데이터베이스에 인증
			// 계정 아이디 얻어옴
			client.AccountID = 1234;

			// 이미 로그인중 알림 ( 이 메시지로 클라이언트에서 재접속 요청을 할 수 있음 )
            //if(false){ client.Send( order, eResult.FAIL_SERVER_LOGIN_REFUSE ); return; }

            Print( client.ClientID + " : " + order + " : " + client.AccountID );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(	bits, order );
			WriteResult( bits, eResult.SUCCESS );
			WriteAccountId(	bits, client.AccountID );
            client.Send( bits );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 서버 : 입장
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        void RecvServerIn( cClient client, eOrder order, cBitStream bits )
        {
			string network_version	= ReadString( bits );
			string client_version	= ReadString( bits );
			ulong account_id		= ReadAccountId( bits );

			if( network_version != VERSION ) { client.Send( order, eResult.FAIL_NETWORK_VERSION ); return; }
			//string db_client_version = "0.1.0";
			//if( client_version != db_client_version ) { client.Send( order, eResult.FAIL_CLIENT_VERSION ); return; }

			// 데이터베이스에 인증
			// ...
			// 이미 로그인중 알림 ( 이 메시지로 클라이언트에서 재접속 요청을 할 수 있음 )
            //if(false){ client.Send( order, eResult.FAIL_SERVER_LOGIN_REFUSE ); return; }

			// 입장 허락
			AddClient( client.ClientID, client );
            Print( client.ClientID + " : " + order + " : " + client.AccountID );

			// 캐릭터 정보를 세팅한다.

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder( bits, order );
			WriteResult( bits, eResult.SUCCESS );
			WriteClientId( bits, client.ClientID );
            client.Send( bits );
        }

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 서버 : 퇴장
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        void RecvServerOut( cClient client, eOrder order, cBitStream bits )
        {
			// 파티 퇴장
			if( client.Party!=cParty.NULL_ID )
			{
				RecvStageUserOut( client, order, bits );
			}
			// 채널 퇴장
			if( client.Channel!=cChannel.NULL_ID )
			{
				RecvChannelOut( client, order, bits );
			}
			// 클라이언트 퇴장
			RemoveClient( client.ClientID );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder( bits, order );
			WriteResult( bits, eResult.SUCCESS );
            client.Send( bits );
        }

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 클라이언트 : 정보 : 기본
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
        void RecvClientInfoDefault( cClient client, eOrder order, cBitStream bits )
        {
			uint user_id = ReadClientId(bits);
			cClient user=null;
			if( GetClient( user_id, out user )==false )	{ client.Send( order, eResult.FAIL_CLIENT_ID ); return;	}

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder( bits, order );
			WriteResult( bits, eResult.SUCCESS );
			WriteClientId( bits, user.ClientID );
			WriteString( bits, user.CharName );
			WriteChannelId( bits, user.Channel );
			WriteStageId( bits, user.Stage );
			//...
            client.Send( bits );
        }

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 리스트
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvChannelList( cClient client, eOrder order, cBitStream bits )
		{
			//-----------------------------------------------
			// Response
			lock( m_channels )
			{
				bits = new cBitStream();
				WriteOrder(			bits, order );
				WriteResult(		bits, eResult.SUCCESS );
				WriteChannelCount(	bits, (byte)m_channels.Count );
				foreach( KeyValuePair<byte,cChannel> entry in m_channels )
				{
					WriteChannelId( bits, entry.Value.ChannelID );
				}
				client.Send( bits );
			}
		}	
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 입장
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvChannelIn( cClient client, eOrder order, cBitStream bits )
		{
			byte channel_id	= ReadChannelId( bits );
			ushort channel_count = 0;
			lock(m_channels)
			{
				cChannel channel;

				// 자동 지정
				if( channel_id == cChannel.NULL_ID )
				{
					channel = GetChannelIn();
					if( channel == null )								{ client.Send( order, eResult.FAIL_CHANNEL_MAX_USER ); return; }
					channel_id = channel.ChannelID;
				}
				else
				{
					// 채널 유무 확인
					if( GetChannel( channel_id, out channel )==false )	{ client.Send( order, eResult.FAIL_CHANNEL_FIND_FAIL ); return; }

					// 최대 채널 유저수 검사
					if( channel.Children.Count>=cChannel.MaxUser )		{ client.Send( order, eResult.FAIL_CHANNEL_MAX_USER ); return; }
				}

				// 채널 입장.
				if( InChannel( channel_id, client )==false )			{ client.Send( order, eResult.FAIL_CHANNEL_IN_USER ); return; }

				channel_count = (ushort)channel.Children.Count;
			}

			// 캐릭터 명.
			string	char_name	= ReadString( bits );
			client.CharName		= char_name;

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(				bits, order );
			WriteResult(			bits, eResult.SUCCESS );
			WriteChannelId(			bits, channel_id );
			WriteChannelUserCount(	bits, channel_count );
			WriteClientId(			bits, client.ClientID );
			WriteString(			bits, char_name );
			WriteStageId(			bits, client.Stage );
			SendChannel( channel_id, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 퇴장
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvChannelOut( cClient client, eOrder order, cBitStream bits )
		{
			byte channel_id = client.Channel;

			// 채널 퇴장
			OutChannel( channel_id, client );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 채널 : 채팅
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvChannelChat( cClient client, eOrder order, cBitStream bits )
		{
			string	message		= ReadString( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteString(	bits, message );
			SendChannel( client.Channel, bits );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 파티 : 채팅.
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvPartyChat( cClient client, eOrder order, cBitStream bits )
		{
			string	message		= ReadString( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteString(	bits, message );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 리스트
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageList( cClient client, eOrder order, cBitStream bits )
		{
			//-----------------------------------------------
			// Response
			lock( m_parties )
			{
				bits = new cBitStream();
				WriteOrder(	bits, order );
				WriteResult(bits, eResult.SUCCESS );

				WriteStageCount( bits, (ushort)m_stages.Count );
				foreach( KeyValuePair<uint,cStage> entry in m_stages )
				{
					WriteStageId( bits, entry.Key );
					WritePartyCount( bits, (ushort)(entry.Value.Children.Count) );
				}
				client.Send( bits );
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 입장 : 요청
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageInRequest( cClient client, eOrder order, cBitStream bits )
		{
			uint stage_id = ReadStageId( bits );
			byte max_user = ReadPartyUserCount( bits );

			// 입장
			uint party_id = InStage( stage_id, max_user, client );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WritePartyId(	bits, party_id );
			client.Send(	bits );

			//-----------------------------------------------
			// 입장 승락
			if( PartyLockIn(party_id) )
			{
				//-----------------------------------------------
				// Response
				bits = new cBitStream();
				WriteOrder(		bits, eOrder.STAGE_IN_ACCEPT );
				WriteResult(	bits, eResult.SUCCESS );
				WriteStageId(	bits, stage_id );
				SendParty( client.Party, bits );
			}
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 입장
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserIn( cClient client, eOrder order, cBitStream bits )
		{
			if( client==null ) return;

			uint[]		equip_items		= ReadItemInfoIds( bits, (int)cUserCharacter.eEquipSlot.MAX_EQUIPSLOT );
			cVector3	stage_pos		= ReadStagePos( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(			bits, order );
			WriteResult(		bits, eResult.SUCCESS );
			WriteFlag(			bits, client.Master );
			WriteClientId(		bits, client.ClientID );
			WriteString(		bits, client.CharName );
			WriteItemInfoId(	bits, client.CharItemInfoId );
			WriteItemInfoIds(	bits, equip_items );
			WriteStagePos(		bits, stage_pos );

			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 퇴장
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserOut( cClient client, eOrder order, cBitStream bits )
		{
			uint party_id = client.Party;

			// 스테이지 + 파티 퇴장
			OutStage( client.Stage, client );

			/*
			// 새로운 파티장 구함
			uint master_id = GetPartyMaster( party_id );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(	bits, order );
			WriteResult( bits, eResult.SUCCESS );
			WriteClientId( bits, client.ClientID );
			WriteClientId( bits, master_id );
			client.Send( bits );
			SendParty( party_id, bits );
			*/
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 이동
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserMove( cClient client, eOrder order, cBitStream bits )
		{
			cVector3 stage_pos = ReadStagePos( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteStagePos(	bits, stage_pos );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 공격 : 몬스터
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserAttackMonster( cClient client, eOrder order, cBitStream bits )
		{
			ushort monster_id = ReadMonsterId( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteMonsterId(	bits, monster_id );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 스킬 사용 : 자신
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserSkillSelf( cClient client, eOrder order, cBitStream bits )
		{
			ushort skill_id = ReadSkillId( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteSkillId(	bits, skill_id );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 스킬 사용 : 몬스터
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserSkillMonster( cClient client, eOrder order, cBitStream bits )
		{
			ushort		skill_id		= ReadSkillId( bits );
			byte		target_count	= ReadSkillTargetCount( bits );
			ushort[]	monster_ids		= new ushort[target_count];
			for( byte c=0; c<target_count; ++c )
			{
				monster_ids[c]			= ReadMonsterId( bits );
			}

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(				bits, order );
			WriteResult(			bits, eResult.SUCCESS );
			WriteClientId(			bits, client.ClientID );
			WriteSkillId(			bits, skill_id );
			WriteSkillTargetCount(	bits, target_count );
			foreach( ushort monster_id in monster_ids )
			{
				WriteMonsterId(		bits, monster_id );
			}
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 스킬 사용 : 좌표
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserSkillPos( cClient client, eOrder order, cBitStream bits )
		{
			ushort		skill_id	= ReadSkillId( bits );
			cVector3	stage_pos	= ReadStagePos( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteSkillId(	bits, skill_id );
			WriteStagePos(	bits, stage_pos );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 데미지
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserDemage( cClient client, eOrder order, cBitStream bits )
		{
			uint		damage	= ReadDamage( bits );
			bool		death	= ReadFlag( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteDamage(	bits, damage );
			WriteFlag(		bits, death );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 아이템 사용 : 자신
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserItemUseSelf( cClient client, eOrder order, cBitStream bits )
		{
			ulong	item_id	= ReadItemId( bits );

			// 아이템 종류에 따라서 분류....

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteItemId(	bits, item_id );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 유저 : 트리거 작동
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageUserTriggerOn( cClient client, eOrder order, cBitStream bits )
		{
			ushort	trigger_id	= ReadTriggerId( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteClientId(	bits, client.ClientID );
			WriteTriggerId(	bits, trigger_id );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지  : 몬스터 : 입장 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonIn( cClient client, eOrder order, cBitStream bits )
		{
			ushort		monster_id		= ReadMonsterId( bits );
			uint		monster_info_id	= ReadMonsterInfoId( bits );
			cVector3	stage_pos	= ReadStagePos( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(			bits, order );
			WriteResult(		bits, eResult.SUCCESS );
			WriteMonsterId(		bits, monster_id );
			WriteMonsterInfoId(	bits, monster_info_id );
			WriteStagePos(	bits, stage_pos );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지  : 몬스터 : 이동 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonMove( cClient client, eOrder order, cBitStream bits )
		{
			ushort		monster_id	= ReadMonsterId( bits );
			cVector3	stage_pos	= ReadStagePos( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteMonsterId(	bits, monster_id );
			WriteStagePos(	bits, stage_pos );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 공격 : 유저 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonAttackUser ( cClient client, eOrder order, cBitStream bits )
		{
			ushort	monster_id	= ReadMonsterId( bits );
			uint	client_id	= ReadClientId( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteMonsterId(	bits, monster_id );
			WriteClientId(	bits, client_id );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 스킬 사용 : 자신 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonSkillSelf( cClient client, eOrder order, cBitStream bits )
		{
			ushort monster_id	= ReadMonsterId( bits );
			ushort skill_id		= ReadSkillId( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteMonsterId(	bits, monster_id );
			WriteSkillId(	bits, skill_id );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 스킬 사용 : 유저 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonSkillUser( cClient client, eOrder order, cBitStream bits )
		{
			ushort	monster_id		= ReadMonsterId( bits );
			ushort	skill_id		= ReadSkillId( bits );
			byte	target_count	= ReadSkillTargetCount( bits );
			uint[]	client_ids		= new uint[target_count];
			for( byte c=0; c<target_count; ++c )
			{
				client_ids[c]		= ReadClientId( bits );
			}

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(				bits, order );
			WriteResult(			bits, eResult.SUCCESS );
			WriteMonsterId(			bits, monster_id );
			WriteSkillId(			bits, skill_id );
			WriteSkillTargetCount(	bits, target_count );
			foreach( uint client_id in client_ids )
			{
				WriteClientId(		bits, client_id );
			}
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 스킬 사용 : 좌표 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonSkillPos( cClient client, eOrder order, cBitStream bits )
		{
			ushort		monster_id	= ReadMonsterId( bits );
			ushort		skill_id	= ReadSkillId( bits );
			cVector3	stage_pos	= ReadStagePos( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteMonsterId(	bits, monster_id );
			WriteSkillId(	bits, skill_id );
			WriteStagePos(	bits, stage_pos );
			SendParty( client.Party, bits );
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 몬스터 : 데미지 : (파티장 권한 필요)
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageMonDemage( cClient client, eOrder order, cBitStream bits )
		{
			ushort		monster_id	= ReadMonsterId( bits );
			uint		damage		= ReadDamage( bits );
			bool		death		= ReadFlag( bits );

			//-----------------------------------------------
			// Response
			bits = new cBitStream();
			WriteOrder(		bits, order );
			WriteResult(	bits, eResult.SUCCESS );
			WriteMonsterId(	bits, monster_id );
			WriteDamage(	bits, damage );
			WriteFlag(		bits, death );
			SendParty( client.Party, bits );
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 커스텀 데이터
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="order">수신 명령코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		void RecvStageData( cClient client, eOrder order, cBitStream bits )
		{
			//-----------------------------------------------
			// Response
			cBitStream		out_bits = new cBitStream();
			WriteOrder(		out_bits, order );
			WriteResult(	out_bits, eResult.SUCCESS );
			out_bits.Write(	bits.ToByteArray() );
			SendParty( client.Party, out_bits );
		}
		#endregion
	}
}

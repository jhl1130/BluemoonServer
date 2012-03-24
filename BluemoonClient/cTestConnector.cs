//----------------------------------------------------------------------------------------------------
// cTestConnector
// : 테스트 커넥터
//  -JHL-2012-02-23
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
	/// 테스트 커넥터
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cTestConnector : cNetConnector
    {
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 진행 프로세스 단계
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		int m_step;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public cTestConnector( string version, string address, ushort port, ushort recv_buf_size, bool use_cryptogram ):base(version,address,port,recv_buf_size,use_cryptogram)
		{
			Client.CharName	= "Test01";
			m_step = 0;
		}

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 메인 루프.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		protected override void MainLoop()
		{
			/*
			switch(m_step)
			{
			case 0: Client.SendServerLogin( "test01", "1234" ); ++m_step; break;
			case 1: break;
			case 2: Client.SendServerIn(); ++m_step; break;
			case 3: break;
			case 4: Client.SendChannelList(); ++m_step; break;
			case 5: break;
			case 6: Client.SendChannelIn( (byte)cChannel.NULL_ID ); ++m_step; break;
			case 7: break;
			case 8: 
				Client.SendChannelChat( "채널채팅 메시지1" );
				Client.SendChannelChat( "채널채팅 메시지2" );
				Client.SendChannelChat( "채널채팅 메시지3" );
				Client.SendChannelChat( "채널채팅 메시지4" );
				++m_step;
				break;
			case 9: break;
			case 10: Client.SendPartyAdd( 100 ); ++m_step; break;
			case 11: break;
			case 12: Client.SendPartyIn( Client.Party ); ++m_step; break;
			case 13: break;
			case 14: 
				Client.SendPartyChat( "파티채팅 메시지1" );
				Client.SendPartyChat( "파티채팅 메시지2" );
				Client.SendPartyChat( "파티채팅 메시지3" );
				Client.SendPartyChat( "파티채팅 메시지4" );
				++m_step;
				break;
			case 15: Client.SendStageUserIn( new cVector3(1000.0f,0,1000.0f) ); ++m_step; break;
			case 16: break;
			case 17:
				Client.SendStageUserMove( new cVector3(1230.0f,0,1230.0f) );
				Client.SendStageUserAttackMonster( 200 );
				Client.SendStageUserSkillSelf( 500 );
				Client.SendStageUserSkillMonster( 501, new ushort[]{200,201,202} );
				Client.SendStageUserSkillPos( 502, new cVector3(1001.0f,0.0f,2003.0f) );
				Client.SendStageUserDemage( 10, false );
				Client.SendStageUserItemUseSelf( 3000 );
				Client.SendStageUserTriggerOn( 40 );
				Client.SendStageMonMove( 900, new cVector3(1001.0f,0.0f,2003.0f) );
				Client.SendStageMonAttackUser( 900, Client.ClientID );
				Client.SendStageMonSkillSelf( 900, 500 );
				Client.SendStageMonSkillUser( 900, 501, new uint[]{200,201,202} );
				Client.SendStageMonSkillPos( 900, 502, new cVector3(1001.0f,0.0f,2003.0f) );
				Client.SendStageMonDamage( 900, 300, true );
				++m_step;
				break;
			case 18: break;
			case 19: Client.SendStageUserOut(); ++m_step; break;
			case 20: break;
			case 21: Client.SendPartyOut(); ++m_step; break;
			case 22: break;
			case 23: Client.SendPartySub( 100 ); ++m_step; break;
			case 24: break;
			case 25: Client.SendChannelOut(); ++m_step; break;
			case 26: break;
			case 27: Client.SendServerOut(); ++m_step; break;
			}
			*/
		}

        protected override void RecvServerLogin( cNetwork.eResult result, cBitStream bits )
        {
			base.RecvServerLogin( result, bits );
			++m_step;
        }
        protected override void RecvServerIn( cNetwork.eResult result, cBitStream bits )
        {
			base.RecvServerIn( result, bits );
			++m_step;
        }
        protected override void RecvServerOut( cNetwork.eResult result, cBitStream bits )
        {
			base.RecvServerOut( result, bits );
			++m_step;
        }
        protected override void RecvChannelList( cNetwork.eResult result, cBitStream bits )
        {
			base.RecvChannelList( result, bits );
			++m_step;
        }
		protected override void RecvChannelIn( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvChannelIn( result, bits );
			++m_step;
		}
		protected override void RecvChannelOut( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvChannelOut( result, bits );
			++m_step;
		}
		protected override void RecvChannelChat( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvChannelChat( result, bits );
			++m_step;
		}
		protected override void RecvPartyChat( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvChannelChat( result, bits );
			++m_step;
		}
		protected override void RecvStageUserIn( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserIn( result, bits );
			++m_step;
		}
		protected override void RecvStageUserOut( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserOut( result, bits );
			++m_step;
		}
		protected override void RecvStageUserMove( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserMove( result, bits );
			++m_step;
		}
		protected override void RecvStageUserAttackMonster( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserAttackMonster( result, bits );
			++m_step;
		}
		protected override void RecvStageUserSkillSelf( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserSkillSelf( result, bits );
			++m_step;
		}
		protected override void RecvStageUserSkillMonster( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserSkillMonster( result, bits );
			++m_step;
		}
		protected override void RecvStageUserSkillPos( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserSkillPos( result, bits );
			++m_step;
		}
		protected override void RecvStageUserDemage( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserDemage( result, bits );
			++m_step;
		}
		protected override void RecvStageUserItemUseSelf( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserItemUseSelf( result, bits );
			++m_step;
		}
		protected override void RecvStageUserTriggerOn( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageUserTriggerOn( result, bits );
			++m_step;
		}
		protected override void RecvStageMonMove( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageMonMove( result, bits );
			++m_step;
		}
		protected override void RecvStageMonAttackUser ( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageMonAttackUser( result, bits );
			++m_step;
		}
		protected override void RecvStageMonSkillSelf( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageMonSkillSelf( result, bits );
			++m_step;
		}
		protected override void RecvStageMonSkillUser( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageMonSkillSelf( result, bits );
			++m_step;
		}
		protected override void RecvStageMonSkillPos( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageMonSkillPos( result, bits );
			++m_step;
		}
		protected override void RecvStageMonDemage( cNetwork.eResult result, cBitStream bits )
		{
			base.RecvStageMonDemage( result, bits );
			++m_step;
		}

        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 : 스테이지 : 커스텀 데이터
		/// </summary>
		/// <param name="result">수신 결과코드</param>
		/// <param name="bits">수신 데이터</param>
        //----------------------------------------------------------------------------------------------------
		protected override void RecvStageData( cNetwork.eResult result, cBitStream in_bits )
		{
			if( result != cNetwork.eResult.SUCCESS ) return;
		}
	}
}

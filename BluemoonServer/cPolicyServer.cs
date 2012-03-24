//----------------------------------------------------------------------------------------------------
// cPolicyServer
// : 크로스도메인정책 서버
//  -JHL-2012-02-14
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
	/// 크로스도메인정책 서버
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cPolicyServer : cServer
    {
		//----------------------------------------------------------------------------------------------------
 		#region 맴버 변수들
		//----------------------------------------------------------------------------------------------------
		#endregion

        //----------------------------------------------------------------------------------------------------
 		#region cPolicyServer() : 생성자
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        public cPolicyServer():this("PolicyServer","PS")
        {
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="name">이름</param>
		/// <param name="short_name">짧은이름</param>
        //----------------------------------------------------------------------------------------------------
        public cPolicyServer( string name, string short_name ):base(name,short_name)
        {
			m_port = 843;
			UseCryptogram = false;
		}
		#endregion

        //-------------------------------------------------------------------------------------------------------------------------------------
 		#region 리슨 프로세스
        //-------------------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// 리슨 프로세스
		/// </summary>
        //-------------------------------------------------------------------------------------------------------------------------------------
        protected override void DoListen()
        {
            try
            {
                m_listener = new TcpListener( System.Net.IPAddress.Any, m_port );
                m_listener.Start();

                while(true)
                {
                    Print( "WAIT_CLIENT" );
                    cPolicyClient client = new cPolicyClient( m_listener.AcceptTcpClient(), OnRecv );
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
 		#region OnRecv() : 클라이언트로부터 받은 패킷처리
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 데이터 파싱
		/// </summary>
		/// <param name="client">클라이언트 객체</param>
		/// <param name="data">데이터</param>
		/// <param name="size">데이터크기</param>
        //----------------------------------------------------------------------------------------------------
        protected override void OnRecv( cClient client, byte[] data, int size )
        {
            string str_data = Encoding.ASCII.GetString( data, 0, size-1 );
            switch( str_data )
            {
            case "<policy-file-request/>":
	            client.Send( "<?xml version=\"1.0\"?><cross-domain-policy><allow-access-from domain=\"*\"/></cross-domain-policy>" );
				Print( "RES_POLICY : "+client.Address );
                break;
            default:
                Error( client.Name+":"+str_data+"<" );
                break;
            }
        }
		#endregion
		
    }
}

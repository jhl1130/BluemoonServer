//----------------------------------------------------------------------------------------------------
// cPolicyClient
// : Policy 전용 클라이언트
//  -JHL-2012-03-09
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
	/// 문자열 전용 클라이언트 객체.
	/// </summary>
	//----------------------------------------------------------------------------------------------------
    public class cPolicyClient : cClient
    {
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 수신 이벤트
		/// </summary>
        //----------------------------------------------------------------------------------------------------
        protected	event	EventRecv	EventRecvPolity;

        //----------------------------------------------------------------------------------------------------
 		#region cPolicyClient() : 생성자
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="event_recv">수신 이벤트 콜백 함수</param>
        //----------------------------------------------------------------------------------------------------
        public cPolicyClient( EventRecv event_recv ):base(cClient.UniqueID,MAX_RECV_BUFFER,false,null)
        {
			EventRecvPolity	= event_recv;
		}
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="client">TcpClient 인스턴스</param>
		/// <param name="event_recv">수신 이벤트 콜백 함수</param>
        //----------------------------------------------------------------------------------------------------
        public cPolicyClient( TcpClient client, EventRecv event_recv ):this(event_recv)
        {
			Connect( client );
        }
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 생성자
		/// </summary>
		/// <param name="address">서버 주소</param>
		/// <param name="port">서버 포트번호</param>
		/// <param name="event_recv">수신 이벤트 콜백 함수</param>
        //----------------------------------------------------------------------------------------------------
        public cPolicyClient( string address, ushort port, EventRecv event_recv ):this(new TcpClient(address,port),event_recv)
        {
        }
		#endregion

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버에 Polity 요청.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		//----------------------------------------------------------------------------------------------------
		public static bool RequestPolicy( string address, ushort port )
		{
            TcpClient client= new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            System.IO.StreamWriter writer = new System.IO.StreamWriter( stream );
            writer.Write( "<policy-file-request/>"+0 );
            writer.Flush();
			stream.ReadTimeout = 10;
            byte[] bytes = new byte[MAX_RECV_BUFFER];
            int read_bytes = stream.Read(bytes, 0, MAX_RECV_BUFFER);
            stream.Close();
            client.Close();
            string data = Encoding.UTF8.GetString(bytes);
            cLog.Log(data);
			return data.Contains( "<cross-domain-policy>" );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 서버에 Polity 요청.(UnityEngine 사용)
		/// </summary>
		/// <param name="address"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		//----------------------------------------------------------------------------------------------------
		public static bool RequestPolicyWithUnity( string address, ushort port )
		{
			return UnityEngine.Security.PrefetchSocketPolicy( address, port );
		}

		//----------------------------------------------------------------------------------------------------
 		#region DoRecv() : 스트림 데이터를 받는다.
        //----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 데이터 수신 처리.
		/// </summary>
		/// <param name="result"></param>
        //----------------------------------------------------------------------------------------------------
        protected override void DoRecv( IAsyncResult result )
        {
            int	recv_bytes		= 0;	// 수신된 바이트 수

            try 
            {
				NetworkStream stream = m_client.GetStream();
				lock( stream )
				{
					recv_bytes = stream.EndRead(result);
					Log( "READ_BYTES : " + recv_bytes + " : " +Address );
				}

				if( recv_bytes==0 )
				{
					Disconnect();
					return;
				}

				// 데이터를 파싱한다.
				EventRecvPolity( this, m_recv_buffer, recv_bytes );

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
				Log( m_name+">>"+ex );
				Disconnect();
            }
        }
		#endregion
	}
}
//-------------------------------------------------------------------
// cMain
// : 메인 프로세스
//  -JHL-2012-02-09
//-------------------------------------------------------------------

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using System.Data;

namespace BlueEngine
{
    //-------------------------------------------------------------------
    // Program : 메인 프로세스
    //-------------------------------------------------------------------
    class cMain
    {
        //-------------------------------------------------------------------
        // 정적 비공개 맴버들
        //-------------------------------------------------------------------
		static cConsole			s_console		= new cConsole();
		static bool				s_loop_order	= true;
		static bool				s_read_order	= true;
		static cPolicyServer	s_policy_server;
		static cGameServer		s_game_server;

        //-------------------------------------------------------------------
        // Main() : 메인 프로세스 시작
        //-------------------------------------------------------------------
        static void Main( string[] args )
        {

			// DB 테스트
			/*
			cDatabase db = new cDatabase(cDatabase.eType.MySQL);
			if( db.Connect( "192.168.0.30", "bluemoon", "bluelab", "bluemoon" ) )
			{
				if( db.Query( "SELECT * FROM user_account" ) )
				{
					while( db.Reader.Read() )
					{
						long	account_id	= (long)db.Reader["account_id"];
						string	email= (string)db.Reader["email"];
					}
					db.CloseQuery();
				}

				{
					cUserAccount.Initialize(db);
					cUserAccount account1 = new cUserAccount();
					account1.Read( db, (long)9 );
					string guid1		= (string)account1["guid"];
					string email1		= (string)account1["email"];
					float music1		= Convert.ToSingle(account1["music"]);
					account1["fb_id"]	= "400";

					cUserAccount account2 = new cUserAccount();
					account2.Read( db, (long)10 );
					string guid			= (string)account2["guid"];
					string email		= (string)account2["email"];
					float music			= Convert.ToSingle(account2["music"]);
					account2["fb_id"]	= "500";

					cUserAccount.Update( db );
				}

				db.Close();
			}
			*/


			// 서버 객체 생성
			s_policy_server		= new cPolicyServer();
			s_game_server		= new cGameServer();

			ushort auto_start	= 0;
			ushort ps_port		= s_policy_server.Port;
			ushort gs_port		= 12345;

			Option();

			foreach( string arg in args )
			{
				//Console.WriteLine( arg );
				string[] param = arg.Split( ':' );
				switch( param[0].ToLower() )
				{
				case "/?":
					Option();
					return;
				case "/autostart":
					auto_start = ushort.Parse(param[1]);
					break;
				case "/ps_port":
					ps_port = ushort.Parse(param[1]);
					break;
				case "/gs_port":
					gs_port = ushort.Parse(param[1]);
					break;
				}
			}

			s_game_server.RecvBufSize = 1024;
			s_game_server.UseCryptogram = true;

			s_console.WriteColor(
				 "\n[ INPUT ]"
				+"\n-----------------------------------------------------------------------"
				+"\n Auto Start Server = " + auto_start
				+"\n Policy Server Port = " + ps_port + ", Cryptogram = " + s_policy_server.UseCryptogram + ", recv_buf_size = " + s_policy_server.RecvBufSize
				+"\n Game Server Port= " + gs_port + ", Cryptogram = " + s_game_server.UseCryptogram + ", recv_buf_size = " + s_game_server.RecvBufSize
				+"\n-----------------------------------------------------------------------"
				+"\n"
				,ConsoleColor.Yellow, ConsoleColor.Black );

			// 암호화 키 세팅
			cNetwork.SetCryptogram( "1234567890abcdef", "1234567890abcdef1234567890abcdef" );

			if( auto_start==1 )
			{
				ServerStart( ps_port, gs_port );
				s_read_order = false;
			}
			else
			{
				s_read_order = true;
			}


            while( s_loop_order )
            {
				// 명령입력
				if( s_read_order )
				{
					Console.Write("order > ");
					switch( Console.ReadLine().ToLower() )
					{
					// 서버 시작
					case "start":
						ServerStart( ps_port, gs_port );
						s_read_order = false;
						break;
					case "stop":
						s_policy_server.Stop();
						s_game_server.Stop();
						s_read_order = false;
						break;
					case "exit":
						s_policy_server.Stop();
						s_game_server.Stop();
						s_loop_order = false;
						break;
					default:
						break;
					}
				}
				else
				// 일반모드
				{
					Thread.Sleep(100);
					ConsoleKeyInfo kinfo = Console.ReadKey(false);
					if( kinfo.Key == ConsoleKey.Enter )
					{
						s_read_order = true;
					}
				}
            }

			s_console.Write("Program End.");
			Console.ReadLine();
        }

        //-------------------------------------------------------------------
        // Option() : 옵션 출력
        //-------------------------------------------------------------------
		static void Option()
		{
			s_console.WriteColor(
			 "\n[ OPTION ]"
			+"\n-----------------------------------------------------------------------"
			+"\n /autostart:[0,1] : Auto Start Server."
			+"\n /ps_port:[port_number] : Policy Server port number."
			+"\n /gs_port:[port_number] : Game Server port number."
			+"\n-----------------------------------------------------------------------"
			, ConsoleColor.Green, ConsoleColor.Black );
		}

		static void ServerStart( ushort ps_port, ushort gs_port )
		{
			s_policy_server.Start( ps_port );
			s_game_server.Start( gs_port );
		}
    }
}

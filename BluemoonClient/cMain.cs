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

namespace BlueEngine
{
    //-------------------------------------------------------------------
    // Program : 메인 프로세스
    //-------------------------------------------------------------------
    class cMain
    {
        //-------------------------------------------------------------------
        // Main() : 메인 프로세스 시작
        //-------------------------------------------------------------------
        static void Main( string[] args )
        {
			// 비트스트림 테스트
			/*
			{
				cBitStreamTester btester = new cBitStreamTester();
			}
			*/

			// 암호화 테스트
			/*
			{
				string iv	= "1234567890abcdef";
				string key	= "1234567890abcdef1234567890abcdef";
				string data = "abcdefghijklmnopqrstuvwxyz가나다라마바사아자차카타파하~!@#$%^&*()_+|{}:;<>?'";

				// 문자열 데이터 -> 비트스트림
				cBitStream bits = new cBitStream();
				bits.Write( data );
				// 비트스트림 -> 암호화 데이터
				byte[] s_data = cCryptogram.Encrypt( bits.ToByteArray(), iv, key );

				// 암호화 데이터 -> 비트스트림
				cBitStream out_bits = new cBitStream();
				out_bits.Write( cCryptogram.Decrypt( s_data, s_data.Length, iv, key ) );
				// 비트스트림 -> 문자열 데이터
				string r_data;
				out_bits.Position = 0;
				out_bits.Read( out r_data );

				// 데이터 확인
				Console.Write( data + "\n" );
				Console.Write( r_data + "\n" );

				Console.ReadLine();
				return;
			}
			*/

			// 암호화 키 세팅
			cNetwork.SetCryptogram( "1234567890abcdef", "1234567890abcdef1234567890abcdef" );

			cTestConnector connector = new cTestConnector( "0.1.0", "localhost", (ushort)12345, (ushort)1024, true );

			bool loop=true;
			while(loop)
			{
				connector.Loop();
			}
			Console.ReadLine();

			connector.OnApplicationQuit();
        }
    }
}

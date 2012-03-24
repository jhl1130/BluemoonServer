//----------------------------------------------------------------------------------------------------
// cBitSize
// : 비트크기
//  -JHL-2012-02-20
//----------------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace BlueEngine
{
	//----------------------------------------------------------------------------------------------------
	/// <summary>
	/// 암호화 객체 : 라인델(Rijndael) 알고리즘 사용.
	/// </summary>
	//----------------------------------------------------------------------------------------------------
	public class cCryptogram
	{
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 초기화 벡터 크기.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public	const	byte	SIZE_IV		=	16;
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 비밀키 크기.
		/// </summary>
		//----------------------------------------------------------------------------------------------------
		public	const	byte	SIZE_KEY	=	32;

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열 데이터 암호화
		/// </summary>
		/// <param name="data">데이터</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(16자)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(32자)</param>
		/// <returns>암호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static string EncryptString( string data, string iv, string key )
        {
			byte[] encrypt = EncryptString( data, Hex2Bin(iv), Hex2Bin(key) );
			return Bin2Hex( encrypt );
        }
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열 데이터 암호화
		/// </summary>
		/// <param name="data">데이터</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(16바이트)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(32바이트)</param>
		/// <returns>암호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static byte[] EncryptString( string data, byte[] iv, byte[] key )
        {
			// 파라메터 체크
            if( data == null || data.Length <= 0 )	throw new ArgumentNullException("data");
            if( iv	 == null || iv.Length	!= 16 )	throw new ArgumentNullException("iv");
            if( key	 == null || key.Length	!= 32 )	throw new ArgumentNullException("key");

			byte[] ret_data;

			// 라인텔 알고리즘 세팅
            using( RijndaelManaged algorithm = new RijndaelManaged() )
			{
				//algorithm.Padding		= 디폴트값 PaddingMode.PKCS7;
				//algorithm.Mode		= 디폴트값 CipherMode.CBC;
				//algorithm.BlockSize	= 디폴트값 128;
				algorithm.Key		= key;
				algorithm.IV		= iv;
				ICryptoTransform encryptor = algorithm.CreateEncryptor( algorithm.Key, algorithm.IV );

				// 암호화 기록
				using( MemoryStream memory_stream = new MemoryStream() )
				{
					using( CryptoStream crypto_stream = new CryptoStream( memory_stream, encryptor, CryptoStreamMode.Write ) )
					{
						using( StreamWriter writer = new StreamWriter( crypto_stream ) )
						{
							writer.Write(data);
						}
						ret_data = memory_stream.ToArray();
					}
				}
			}
			return ret_data;
        }
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이너리 데이터 암호화
		/// </summary>
		/// <param name="data">데이터</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(Hex코드 16자)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(Hex코드 32자)</param>
		/// <returns>암호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static byte[] Encrypt( byte[] data, string iv, string key )
        {
			return Encrypt( data, Hex2Bin(iv), Hex2Bin(key) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이너리 데이터 암호화
		/// </summary>
		/// <param name="data">데이터</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(16바이트)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(32바이트)</param>
		/// <returns>암호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static byte[] Encrypt( byte[] data, byte[] iv, byte[] key )
        {
			// 파라메터 체크
            if( data == null || data.Length <= 0 )	throw new ArgumentNullException("data");
            if( iv	 == null || iv.Length	!= 16 )	throw new ArgumentNullException("iv");
            if( key	 == null || key.Length	!= 32 )	throw new ArgumentNullException("key");

			byte[] ret_data;

			// 라인텔 알고리즘 세팅
            using( RijndaelManaged algorithm = new RijndaelManaged() )
			{
				//algorithm.Padding		= 디폴트값 PaddingMode.PKCS7;
				//algorithm.Mode		= 디폴트값 CipherMode.CBC;
				//algorithm.BlockSize	= 디폴트값 128;
				algorithm.Key		= key;
				algorithm.IV		= iv;
				ICryptoTransform encryptor = algorithm.CreateEncryptor( algorithm.Key, algorithm.IV );

				// 암호화 기록
				using( MemoryStream memory_stream = new MemoryStream() )
				{
					using( CryptoStream crypto_stream = new CryptoStream( memory_stream, encryptor, CryptoStreamMode.Write ) )
					{
						using( BinaryWriter writer = new BinaryWriter( crypto_stream ) )
						{
							writer.Write(data);
							//crypto_stream.Write( data, 0, data.Length );
						}
						ret_data = memory_stream.ToArray();
					}
				}
			}
			return ret_data;
        }

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열 데이터 복호화
		/// </summary>
		/// <param name="data">암호화된 데이터</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(Hex코드 16자)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(Hex코드 32자)</param>
		/// <returns>복호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static string DecryptString( string data, string iv, string key )
        {
			return DecryptString( Hex2Bin(data), Hex2Bin(iv), Hex2Bin(key) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 문자열 데이터 복호화
		/// </summary>
		/// <param name="data">암호화된 데이터</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(16바이트)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(32바이트)</param>
		/// <returns>복호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static string DecryptString( byte[] data, byte[] iv, byte[] key )
		{
			// 파라메터 체크
            if( data == null || data.Length <= 0 )	throw new ArgumentNullException("data");
            if( iv	 == null || iv.Length	!= 16 )	throw new ArgumentNullException("iv");
            if( key	 == null || key.Length	!= 32 )	throw new ArgumentNullException("key");

			string ret_data = null;
			// 라인텔 알고리즘 세팅
            using( RijndaelManaged algorithm = new RijndaelManaged() )
			{
				//algorithm.Padding		= 디폴트값 PaddingMode.PKCS7;
				//algorithm.Mode		= 디폴트값 CipherMode.CBC;
				//algorithm.BlockSize	= 디폴트값 128;
				algorithm.Key		= key;
				algorithm.IV		= iv;
				ICryptoTransform decryptor = algorithm.CreateDecryptor( algorithm.Key, algorithm.IV );

				// 메모리에 기록
				using( MemoryStream memory_stream = new MemoryStream(data) )
				{
					using( CryptoStream crypto_stream = new CryptoStream( memory_stream, decryptor, CryptoStreamMode.Read ) )
					{
						using( StreamReader reader = new StreamReader( crypto_stream ) )
						{
							ret_data = reader.ReadToEnd();
						}
					}
				}
			}
			return ret_data;
        }
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이너리 데이터 복호화
		/// </summary>
		/// <param name="data">암호화된 데이터</param>
		/// <param name="data_size">암호화된 데이터 길이</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(Hex코드 16자)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(Hex코드 32자)</param>
		/// <returns>복호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static byte[] Decrypt( byte[] data, int data_size, string iv, string key )
        {
			return Decrypt( data, data_size, Hex2Bin(iv), Hex2Bin(key) );
		}
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이너리 데이터 복호화
		/// </summary>
		/// <param name="data">암호화된 데이터</param>
		/// <param name="data_size">암호화된 데이터 길이</param>
		/// <param name="iv">라인텔 알고리즘 초기화 벡터(16바이트)</param>
		/// <param name="key">라인텔 알고리즘 비밀키(32바이트)</param>
		/// <returns>복호화된 데이터</returns>
		//----------------------------------------------------------------------------------------------------
		public static byte[] Decrypt( byte[] data, int data_size, byte[] iv, byte[] key )
		{
			// 파라메터 체크
            if( data == null || data.Length	<= 0  )	throw new ArgumentNullException("data");
            if( iv	 == null || iv.Length	!= 16 )	throw new ArgumentNullException("iv");
            if( key	 == null || key.Length	!= 32 )	throw new ArgumentNullException("key");

			// 라인텔 알고리즘 세팅
            using( RijndaelManaged algorithm = new RijndaelManaged() )
			{
				//algorithm.Padding		= 디폴트값 PaddingMode.PKCS7;
				//algorithm.Mode		= 디폴트값 CipherMode.CBC;
				//algorithm.BlockSize	= 디폴트값 128;
				algorithm.Key		= key;
				algorithm.IV		= iv;
				ICryptoTransform decryptor = algorithm.CreateDecryptor( algorithm.Key, algorithm.IV );

				using( MemoryStream memory_stream = new MemoryStream(data,0,data_size) )
				{
					using( CryptoStream crypto_stream = new CryptoStream( memory_stream, decryptor, CryptoStreamMode.Read ) )
					{
						//using( BinaryReader reader = new BinaryReader( crypto_stream ) )
						{
							byte[] buf = new byte[data_size];
							//reader.Read( buf, 0, buf.Length );
							crypto_stream.Read( buf, 0, data_size );
							return buf;
						}
					}
				}
			}
        }

		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// HEX코드를 바이너리코드로 변환
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		//----------------------------------------------------------------------------------------------------
        public static byte[] Hex2Bin( string hex )
        {
            int len = hex.Length;
            byte[] bytes = new byte[len];
            for( int i=0; i<len; ++i )
			{
                bytes[i] = Convert.ToByte( hex.Substring(i,1), 16 );
			}
            return bytes;
        }
		//----------------------------------------------------------------------------------------------------
		/// <summary>
		/// 바이너리코드를 HEX코드로 변환
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		//----------------------------------------------------------------------------------------------------
        public static string Bin2Hex( byte[] bytes )
        {
            StringBuilder hex = new StringBuilder( bytes.Length*2 );
            foreach( byte b in bytes )
			{
                hex.AppendFormat( "{0:x2}", b );
			}
            return hex.ToString();
        }
	}
}

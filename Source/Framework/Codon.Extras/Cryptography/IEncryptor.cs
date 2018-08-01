﻿using System.IO;
using System.Threading.Tasks;

namespace Codon.Cryptography
{
	/// <summary>
	/// An <c>IEncryptor</c> is used to encrypt and decrypt byte streams.
	/// </summary>
	public interface IEncryptor
	{
		/// <summary>
		/// Encrypt data from <c>plainStream</c> to <c>outputStream</c>
		/// using the specified password.
		/// </summary>
		/// <param name="plainStream">The unencrypted data stream
		/// that requires encrypting.</param>
		/// <param name="password">A password that is used to encrypt the data
		/// and can later be used to unencrypt the data.</param>
		/// <param name="outputStream">The resulting encrypted bytes are placed
		/// in the <c>outputStream</c>.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Occurs if any of the arguments are null.</exception>
		Task EncryptAsync(Stream plainStream, string password, Stream outputStream);

		/// <summary>
		/// Decrypts data from <c>encryptedStream</c> to <c>outputStream</c>
		/// using the specified password.
		/// </summary>
		/// <param name="encryptedStream">The encrypted data stream
		/// that requires decryption.</param>
		/// <param name="password">A password that is used to decrypt the data.</param>
		/// <param name="outputStream">The resulting decrypted bytes are placed
		/// in the <c>outputStream</c>.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Occurs if any of the arguments are null.</exception>
		Task DecryptAsync(Stream encryptedStream, string password, Stream outputStream);
	}
}
﻿using Akeeba.Unarchiver.EventArgs;
using Akeeba.Unarchiver.EventArgs;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akeeba.Unarchiver.DataWriter;
using System.Threading;
using Akeeba.Unarchiver.Encrypt;
using Akeeba.Unarchiver.Resources;
using ICSharpCode.SharpZipLib.BZip2;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Core;
using ProgressEventArgs = Akeeba.Unarchiver.EventArgs.ProgressEventArgs;

namespace Akeeba.Unarchiver.Format
{
    internal class JPS : Unarchiver
    {
        /// <summary>
        /// A combined structure for the JPS standard header and the JPS End of Archive header.
        /// </summary>
        private struct JpsHeaderData
        {
            // Standard Header (8 bytes)
            public string Signature;
            public byte majorVersion;
            public byte minorVersion;
            public byte spannedArchive;
            public ushort ExtraHeaderLength;

            // End of Archive Header (17 bytes)
            public string EOASignature;
            public ushort NumberOfParts;
            public ulong NumberOfFiles;
            public ulong UncompressedSize;
            public ulong CompressedSize;

            // Total size of all backup archive parts. Not part of the header but we need it anyway.
            public ulong TotalSize;
        }

        /// <summary>
        /// The unencrypted part of the JPS entity block header
        /// </summary>
        private struct JpsEntityDescriptionBlockHeader
        {
            public string Signature;
            public ushort EncryptedSize;
            public ushort DecryptedSize;
        }

        /// <summary>
        /// The encrypted part of the JPS entity block header
        /// </summary>
        private struct JpsEntityDescriptionBlockData
        {
            public ushort PathLength;
            public string Path;
            public TEntityType EntityType;
            public TCompressionType CompressionType;
            public ulong UncompressedSize;
            public ulong Permissions;
            public ulong FileModificationTime;
        }

        private byte[] _key;

        private string _password;

        public string Password
        {
            set
            {
                _password = value;

                // Derive the legacy encryption key
                _key = AesCounter.makeKey(_password);
            }
        }

        private bool _useLegacyKey = true;

        /// <summary>
        /// Inherit the constructor from the base class
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="password">The password for extracting the archive. Currently only implemented for JPS archives.</param>
        public JPS(string filePath, string password = "") : base()
        {
            SupportedExtension = "jps";

            ArchivePath = filePath;
            Password = password;
        }

        /// <summary>
        /// TODO Implement me!
        ///
        /// Implements the Extract method which extracts a backup archive. A DataWriter must be already assigned and configured or an
        /// exception will be raised.
        /// </summary>
        public override void Extract(CancellationToken token)
        {
            // Initialize
            Close();
            Progress.FilePosition = 0;
            Progress.RunningCompressed = 0;
            Progress.RunningUncompressed = 0;
            Progress.Status = ExtractionStatus.Running;
            Progress.LastException = null;
            ProgressEventArgs args;

            JpsHeaderData archiveHeader;

            try
            {
                // Read the archive header
                archiveHeader = ReadArchiveHeader();

                // Invoke event at start of extraction
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                // The end of archive is 18 bytes before the end of the archive due to the End of Archive record
                while ((CurrentPartNumber != null) && (Progress.FilePosition < (archiveHeader.TotalSize - 18)))
                {
                    JpsEntityDescriptionBlockData fileHeader = ReadFileHeader();

                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    ProcessDataBlock(fileHeader, token);

                    args = new ProgressEventArgs(Progress);
                    OnProgressEvent(args);
                }
            }
            catch (OperationCanceledException cancelledException)
            {
                // The operation was cancelled. Set the state to Idle and reset the extraction.
                Close();
                Progress.FilePosition = 0;
                Progress.RunningCompressed = 0;
                Progress.RunningUncompressed = 0;
                Progress.Status = ExtractionStatus.Idle;
                Progress.LastException = cancelledException;

                // Invoke an event notifying susbcribers about the cancelation.
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                return;
            }
            catch (Exception errorException)
            {
                // Any other exception. Close the option file and set the status to error.
                Close();
                Progress.Status = ExtractionStatus.Error;
                Progress.LastException = errorException;

                // Invoke an event notifying of the error state
                args = new ProgressEventArgs(Progress);
                OnProgressEvent(args);

                return;
            }

            // Invoke an event signaling the end of extraction
            Progress.Status = ExtractionStatus.Finished;
            args = new ProgressEventArgs(Progress);
            OnProgressEvent(args);
        }

        /// <summary>
        /// Reads the archive file's standard and end of archive headers and makes sure it's a valid JPS archive.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidArchiveException"></exception>
        private JpsHeaderData ReadArchiveHeader()
        {
            // Open the first part
            Close();
            Open(1);

            // Read the file signature. Must be "JPS"
            JpsHeaderData headerData;
            headerData.Signature = ReadAsciiString(3);

            if (headerData.Signature != "JPS")
            {
                throw new InvalidArchiveException(
                    String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_FILE_TYPE_SIGNATURE"), "JPS"));
            }

            // Read the rest of the header
            headerData.majorVersion = ReadByte();
            headerData.minorVersion = ReadByte();
            headerData.spannedArchive = ReadByte();
            headerData.ExtraHeaderLength = ReadUShort();

            // Make sure it's a supported JPS version
            bool oneNine = (headerData.majorVersion == 1) && (headerData.minorVersion == 9);
            bool oneTen = (headerData.majorVersion == 1) && (headerData.minorVersion == 10);

            if (!oneNine && !oneTen)
            {
                throw new InvalidArchiveException(String.Format(
                    Language.ResourceManager.GetString("ERR_FORMAT_JPS_INVALID_VERSION"), headerData.majorVersion,
                    headerData.minorVersion
                ));
            }

            // Versions 1.9 and 1.10 must not have any extra header data.
            if ((oneNine || oneTen) && (headerData.ExtraHeaderLength > 0))
            {
                throw new InvalidArchiveException(String.Format(
                    Language.ResourceManager.GetString("ERR_FORMAT_JPS_INVALID_EXTRA_HEADER_FOR_VERSION"),
                    headerData.majorVersion,
                    headerData.minorVersion
                ));
            }

            // TODO Version 2.0. Read extra header data and decide how to handle key derivation.
            // TODO Also set _useLegacyKey = false in version 2

            // Open the last part and read the End Of Archive header data
            Close();
            Open(Parts);
            InputStream.Seek(-17, SeekOrigin.End);

            headerData.EOASignature = ReadAsciiString(3);

            if (headerData.EOASignature != "JPE")
            {
                throw new InvalidArchiveException(
                    String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_FILE_TYPE_SIGNATURE"), "JPS"));
            }

            // Read the rest of the end of archive header data
            headerData.NumberOfParts = ReadUShort();
            headerData.NumberOfFiles = ReadULong();
            headerData.UncompressedSize = ReadULong();
            headerData.CompressedSize = ReadULong();

            // Now we can reopen the first part and go past the header
            Open(1);
            SkipBytes(8 + headerData.ExtraHeaderLength);

            // Invoke the archiveInformation event. We need to do some work to get there, through...
            ArchiveInformation info = new ArchiveInformation();
            info.ArchiveType = ArchiveType.Jps;

            // -- Get the total archive size by looping all of its parts
            info.ArchiveSize = 0;

            for (int i = 1; i <= Parts; i++)
            {
                FileInfo fi = new FileInfo(ArchivePath);
                info.ArchiveSize += (ulong) fi.Length;
            }

            headerData.TotalSize = info.ArchiveSize;

            // -- Incorporate bits from the file header
            info.CompressedSize = headerData.CompressedSize;
            info.UncompressedSize = headerData.UncompressedSize;
            info.FileCount = headerData.NumberOfFiles;

            // -- Create the event arguments object
            ArchiveInformationEventArgs args = new ArchiveInformationEventArgs(info);
            // -- Finally, invoke the event
            OnArchiveInformationEvent(args);

            return headerData;
        }

        /// <summary>
        /// Reads, decrypts and returns the header of an entity
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidArchiveException"></exception>
        private JpsEntityDescriptionBlockData ReadFileHeader()
        {
            JpsEntityDescriptionBlockHeader preamble = new JpsEntityDescriptionBlockHeader();
            JpsEntityDescriptionBlockData fileHeader = new JpsEntityDescriptionBlockData();

            // Read the entity description block header
            preamble.Signature = ReadAsciiString(3);

            if (preamble.Signature != "JPF")
            {
                throw new InvalidArchiveException(
                    String.Format(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_HEADER_AT_POSITION"),
                        CurrentPartNumber, Progress.FilePosition)
                );
            }

            preamble.EncryptedSize = ReadUShort();
            preamble.DecryptedSize = ReadUShort();

            // Read the encrypted data
            using (MemoryStream encryptedSteam = ReadIntoStream(preamble.EncryptedSize))
            {
                using (MemoryStream decryptedStream = Decrypt(encryptedSteam))
                {
                    if (decryptedStream.Length != preamble.DecryptedSize)
                    {
                        throw new InvalidArchiveException(
                            String.Format(
                                Language.ResourceManager.GetString("ERR_FORMAT_JPS_DECRYPTED_SIZE_DIFFERENCE"),
                                preamble.DecryptedSize, decryptedStream.Length)
                        );
                    }

                    fileHeader.PathLength = ReadUShort(decryptedStream);
                    fileHeader.Path = ReadUtf8String(fileHeader.PathLength, decryptedStream);

                    switch (ReadByte(decryptedStream))
                    {
                        case 0:
                            fileHeader.EntityType = TEntityType.Directory;
                            break;

                        case 1:
                            fileHeader.EntityType = TEntityType.File;
                            break;

                        case 2:
                            fileHeader.EntityType = TEntityType.Symlink;
                            break;

                        default:
                            throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_ENTITY_TYPE"));
                            //break;
                    }

                    switch (ReadByte(decryptedStream))
                    {
                        case 0:
                            fileHeader.CompressionType = TCompressionType.Uncompressed;
                            break;

                        case 1:
                            fileHeader.CompressionType = TCompressionType.GZip;
                            break;

                        case 2:
                            fileHeader.CompressionType = TCompressionType.BZip2;
                            break;

                        default:
                            throw new InvalidArchiveException(Language.ResourceManager.GetString("ERR_FORMAT_INVALID_COMPRESSION_METHOD"));
                            //break;
                    }

                    fileHeader.UncompressedSize = ReadULong(decryptedStream);
                    fileHeader.Permissions = ReadULong(decryptedStream);
                    fileHeader.FileModificationTime = ReadULong(decryptedStream);
                }
            }

            // Invoke the OnEntityEvent event. We need to do some work to get there, through...
            // -- Create a new archive information record
            EntityInformation info = new EntityInformation();
            // -- Incorporate bits from the file header
            info.CompressedSize = 0; // JPS does not report the compressed size. We have to read a chunk at a time.
            info.UncompressedSize = fileHeader.UncompressedSize;
            info.StoredName = fileHeader.Path;
            // -- Get the absolute path of the file
            info.AbsoluteName = "";

            if (DataWriter != null)
            {
                info.AbsoluteName = DataWriter.GetAbsoluteFilePath(fileHeader.Path);
            }
            // -- Get some bits from the currently open archive file
            info.PartNumber = CurrentPartNumber ?? 1;
            info.PartOffset = InputStream.Position;
            // -- Create the event arguments object
            EntityEventArgs args = new EntityEventArgs(info);
            // -- Finally, invoke the event
            OnEntityEvent(args);

            return fileHeader;
        }


        /// <summary>
        /// Processes a data block in a JPA file located in the current file position
        /// </summary>
        /// <param name="dataBlockHeader">The header of the block being processed</param>
        /// <param name="token">A cancellation token, allowing the called to cancel the processing</param>
        private void ProcessDataBlock(JpsEntityDescriptionBlockData dataBlockHeader, CancellationToken token)
        {
            // Update the archive's progress record
            Progress.FilePosition = (ulong) (SizesOfPartsAlreadyRead + InputStream.Position);
            Progress.Status = ExtractionStatus.Running;

            // Create the event arguments we'll use when invoking the event
            ProgressEventArgs args = new ProgressEventArgs(Progress);

            switch (dataBlockHeader.EntityType)
            {
                // Just a directory? Create it and return.
                case TEntityType.Directory:
                    if (DataWriter != null)
                    {
                        DataWriter.MakeDirRecursive(DataWriter.GetAbsoluteFilePath(dataBlockHeader.Path));
                    }
                    break;

                // Symlink? Read the encrypted target and create the symlink
                case TEntityType.Symlink:
                    using (MemoryStream source = ReadAndDecryptNextDataChunkBlock())
                    {
                        string symlinkTarget = ReadUtf8String((int) dataBlockHeader.UncompressedSize);

                        if (DataWriter != null)
                        {
                            DataWriter.MakeSymlink(symlinkTarget, DataWriter.GetAbsoluteFilePath(dataBlockHeader.Path));
                        }
                    }

                    Progress.RunningUncompressed += dataBlockHeader.UncompressedSize;
                    break;

                // We have a file. Now it's more complicated. We have to read through and process each chunk until
                // we have reached the decompressed size.
                case TEntityType.File:
                    ulong currentRunningDecompressed = 0;

                    if (DataWriter != null)
                    {
                        DataWriter.StartFile(dataBlockHeader.Path);
                    }

                    while (currentRunningDecompressed < dataBlockHeader.UncompressedSize)
                    {
                        // First chance to cancel: before decompressing data
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        using (MemoryStream decryptedStream = ReadAndDecryptNextDataChunkBlock())
                        {
                            // Second chance to cancel: after decompressing data, before decompressing / writing data
                            if (token.IsCancellationRequested)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            switch (dataBlockHeader.CompressionType)
                            {
                                case TCompressionType.GZip:
                                    using (DeflateStream decompressStream = new DeflateStream(decryptedStream, CompressionMode.Decompress))
                                    {
                                        // We need to decompress the data to get its length
                                        using (MemoryStream sourceStream = new MemoryStream())
                                        {
                                            decompressStream.CopyTo(sourceStream);

                                            ulong sourceStreamLength = (ulong) sourceStream.Length;
                                            currentRunningDecompressed += sourceStreamLength;

                                            if (DataWriter != null)
                                            {
                                                DataWriter.WriteData(sourceStream);
                                            }
                                        }
                                    }
                                    break;

                                case TCompressionType.BZip2:
                                    using (BZip2InputStream decompressStream = new BZip2InputStream(decryptedStream))
                                    {
                                        // We need to decompress the data to get its length
                                        using (MemoryStream sourceStream = new MemoryStream())
                                        {
                                            decompressStream.CopyTo(sourceStream);

                                            ulong sourceStreamLength = (ulong) sourceStream.Length;
                                            currentRunningDecompressed += sourceStreamLength;

                                            if (DataWriter != null)
                                            {
                                                DataWriter.WriteData(sourceStream);
                                            }
                                        }
                                    }
                                    break;

                                case TCompressionType.Uncompressed:
                                    currentRunningDecompressed += (ulong) decryptedStream.Length;

                                    if (DataWriter != null)
                                    {
                                        DataWriter.WriteData(decryptedStream);
                                    }
                                    break;
                            }
                        }
                    }

                    Progress.RunningUncompressed += currentRunningDecompressed;

                    if (DataWriter != null)
                    {
                        DataWriter.StopFile();
                    }

                    break;
            }
        }

        /// <summary>
        /// Decrypts the data in memory, up to DecryptedSize bytes, and returns a memory stream with the decrypted
        /// results.
        /// </summary>
        /// <param name="encryptedSteam"></param>
        /// <returns></returns>
        /// <exception cref="InvalidArchiveException"></exception>
        private MemoryStream Decrypt(Stream encryptedSteam)
        {
            // Get the correct key to use with the Rijndael algorithm
            byte[] key = _key;

            if (!_useLegacyKey)
            {
                // TODO Implement version 2.0 logic
            }

            // How many bytes to trim pff the input stream before decompressing
            int trimBytes = 4;

            // Do I have an IV?
            encryptedSteam.Seek(-24, SeekOrigin.End);
            string IVSignature = ReadAsciiString(4, encryptedSteam);
            byte[] IV;

            if (IVSignature == "JPIV")
            {
                trimBytes += 20;
                IV = ReadBytes(16, encryptedSteam);
            }
            else
            {
                encryptedSteam.Seek(0, SeekOrigin.Begin);
                IV = key;
            }

            // Read the decrypted data size at the end of the block
            encryptedSteam.Seek(-4, SeekOrigin.End);
            ulong decryptedSize = ReadULong(encryptedSteam);
            encryptedSteam.Seek(0, SeekOrigin.Begin);
            // Trim the encrypted footer placed after the encrypted data
            encryptedSteam.SetLength(encryptedSteam.Length - trimBytes);

            try
            {
                // Set up the Rijndael algorithm
                using (RijndaelManaged algo = new RijndaelManaged())
                {
                    algo.Key = key;
                    algo.IV = IV;
                    algo.BlockSize = 128;
                    algo.Mode = CipherMode.CBC;
                    algo.Padding = PaddingMode.Zeros;

                    // Create a decryptor
                    using (ICryptoTransform decryptor = algo.CreateDecryptor())
                    {
                        // Create a decrypting stream
                        using (CryptoStream csDecrypt = new CryptoStream(encryptedSteam, decryptor, CryptoStreamMode.Read))
                        {
                            // Create a memory stream and put all the decrypted data into it
                            MemoryStream result = ReadIntoStream((int) decryptedSize, csDecrypt);

                            if (result.Length != (long) decryptedSize)
                            {
                                long length = result.Length;

                                result.Close();
                                result.Dispose();

                                throw new InvalidArchiveException(
                                    String.Format(
                                        Language.ResourceManager.GetString("ERR_FORMAT_JPS_DECRYPTED_SIZE_DIFFERENCE"),
                                        decryptedSize, length)
                                );
                            }

                            result.Seek(0, SeekOrigin.Begin);

                            return result;
                        }
                    }
                }
            }
            catch (AbandonedMutexException e)
            {
                throw new InvalidArchiveException(
                    Language.ResourceManager.GetString("ERR_FORMAT_JPS_DECRYPTION_FAILURE")
                );
            }
        }

        private MemoryStream ReadAndDecryptNextDataChunkBlock()
        {
            ulong EncryptedSize = ReadULong();
            ulong DecryptedSize = ReadULong();

            Progress.RunningCompressed += EncryptedSize;

            // Read the encrypted data
            using (MemoryStream encryptedSteam = ReadIntoStream((int)EncryptedSize))
            {
                MemoryStream decryptedStream = Decrypt(encryptedSteam);

                if (decryptedStream.Length != (long)DecryptedSize)
                {
                    throw new InvalidArchiveException(
                        String.Format(
                            Language.ResourceManager.GetString("ERR_FORMAT_JPS_DECRYPTED_SIZE_DIFFERENCE"),
                            DecryptedSize, decryptedStream.Length)
                    );
                }

                return decryptedStream;
            }
        }
    }
}